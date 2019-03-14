using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System;
using System.IO;
using UnityEngine; // Debugだけ使っている

namespace Kayac.LoaderImpl
{
	// このクラスのコードは全て別スレッド実行
	public class StorageCacheScanner
	{
		public void Build(string root, bool useHash)
		{
			Debug.Assert(root[root.Length - 1] != '/'); // スラッシュ終わりなら不正
			_root = root;
			_useHash = useHash;

			_entries = new Dictionary<string, StorageCache.Entry>();
			_stringBuilder = new System.Text.StringBuilder();
			if (Directory.Exists(_root))
			{
				BuildRecursive(_root);
			}
			else // フォルダがなければ作る。スキャンの必要はない。
			{
				Directory.CreateDirectory(_root);
			}
		}

		void BuildRecursive(string path) // 別スレッド実行
		{
			var dirs = Directory.GetDirectories(path);
			foreach (var dir in dirs)
			{
				BuildRecursive(dir);
			}
			var files = Directory.GetFiles(path);
			foreach (var file in files)
			{
				StorageCache.Entry newEntry;
				string name;
				bool isTemporary;
				if (!ParseCachePath(out name, out newEntry.hash, out isTemporary, file))
				{
					// 認識できないファイルなので無視。誰かが置いたのだろう。
				}
				else if (isTemporary) // 書き込み中が見つかれば問答無用で削除
				{
					FileUtility.DeleteFile(file);
				}
				else
				{
					lock (_entries)
					{
						StorageCache.Entry oldEntry;
						if (_entries.TryGetValue(name, out oldEntry)) // すでに辞書に入ってる。バグによるものと思われ、判断がつかないので両方消す。
						{
							FileUtility.DeleteFile(file);
							_stringBuilder.Length = 0;
							StorageCache.MakeCachePath(_stringBuilder, name, ref oldEntry.hash, _root, _useHash);
							FileUtility.DeleteFile(_stringBuilder.ToString());
						}
						else
						{
							_entries.Add(name, newEntry);
						}
					}
				}
			}
		}

		public Dictionary<string, StorageCache.Entry> GetResult()
		{
			return _entries;
		}

		bool ParseCachePath(out string name, out FileHash hash, out bool isTemporary, string path)
		{
			/*
			hoge/fuga/fileName.0123456701234567012345671234567.ext.tmp
			みたいな形式。
			*/
			// まず最後のperiodの後ろが.tmpであるかを識別する。
			int pathEnd;
			if (path.EndsWith(StorageCache.temporaryFilePostfix))
			{
				isTemporary = true;
				pathEnd = path.Length - StorageCache.temporaryFilePostfix.Length;
			}
			else
			{
				isTemporary = false;
				pathEnd = path.Length;
			}
			// ここからさかのぼりながら.を最大2つ探す
			int periodPosBits = 0; //15bitづつ2分割して使う。配列を使いたいがnewしたくない。変数2つ用意するのも嫌だ。
			int periodCount = 0;
			int i = pathEnd - 1;
			while ((i >= 0) && (path[i] != '/'))
			{
				if (path[i] == '.')
				{
					periodPosBits |= (i << (periodCount * 15));
					periodCount++;
					if (periodCount == 2)
					{
						break;
					}
				}
				i--;
			}

			int nameEnd, hashBegin, hashEnd, extBegin, extEnd;
			if (_useHash) // ハッシュ値がファイル名に挿入されている場合
			{
				if (periodCount >= 2) // 拡張子とハッシュが両方存在する
				{
					extEnd = pathEnd;
					hashEnd = ((periodPosBits >> 0) & 0x7fff);
					extBegin = hashEnd + 1;
					nameEnd = ((periodPosBits >> 15) & 0x7fff);
					hashBegin = nameEnd + 1;
				}
				else if (periodCount == 1) // 拡張子がなくハッシュのみ存在すると解釈する
				{
					extBegin = extEnd = int.MinValue;
					hashEnd = pathEnd;
					nameEnd = ((periodPosBits >> 0) & 0x7fff);
					hashBegin = nameEnd + 1;
				}
				else
				{
					extBegin = extEnd = hashBegin = hashEnd = nameEnd = int.MinValue;
				}
			}
			else // ハッシュ値がない場合
			{
				hashBegin = hashEnd = int.MinValue;
				if (periodCount >= 1) // 最後のピリオドの後ろを拡張子とし、その前は単にファイル名の一部と解釈する
				{
					extEnd = pathEnd;
					nameEnd = ((periodPosBits >> 0) & 0x7fff);
					extBegin = nameEnd + 1;
				}
				else if (periodCount == 0) // 拡張子がない
				{
					extBegin = extEnd = int.MinValue;
					nameEnd = pathEnd;
				}
				else
				{
					extBegin = extEnd = nameEnd = int.MinValue;
				}
			}

			bool ret;
			if (nameEnd < 0) // 認識不能
			{
				ret = false;
				name = null;
				hash = new FileHash();
			}
			else // 認識はできている
			{
				ret = true;
				if (_useHash)
				{
					hash = new FileHash(path, hashBegin);
				}
				else
				{
					hash = new FileHash();
				}
				_stringBuilder.Length = 0;
				_stringBuilder.Append(path, _root.Length + 1, nameEnd - _root.Length - 1); // rootはslashを含まないのでその次から
				if (extBegin >= 0)
				{
					_stringBuilder.Append('.');
					_stringBuilder.Append(path, extBegin, extEnd - extBegin);
				}
				name = _stringBuilder.ToString();
			}
//Debug.Log("ParseCachePath " + path + " -> " + name + " + " + hash.ToString());
			return ret;
		}

		string _root;
		bool _useHash;
		Dictionary<string, StorageCache.Entry> _entries;
		System.Text.StringBuilder _stringBuilder;
	}
}
