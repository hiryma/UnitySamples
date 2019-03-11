using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System;
using System.IO;
using UnityEngine; // Debug, Hash128を使っている

namespace Kayac.LoaderImpl
{
	// このクラスのコードは全て別スレッド実行
	public class StorageCacheScanner
	{
		public void Build(string root)
		{
			Debug.Assert(root[root.Length - 1] != '/'); // スラッシュ終わりなら不正
			_root = root;

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
							StorageCache.MakeCachePath(_stringBuilder, name, ref oldEntry.hash, _root);
							FileUtility.DeleteFile(_stringBuilder.ToString());
						}
						else
						{
							_entries.Add(name, newEntry);
Debug.Log(name + " " + file);
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
			// hoge/fuga/fileName.0123456701234567012345671234567.ext.tmp
			// の形式。fileNameまでをname、数字羅列部分をhashとして取り出したい。
			// name内に.があることは許したいのと、tmpがあったりなかったりするので、
			// 後ろから順に.を見つけて範囲を確定していくという手間のかかる状態になっている。
			// regexの方が楽なのだが、そこそこ回数呼ぶので速度を重視して手で書いた。
			int i, nameEnd, hashBegin, hashEnd, extBegin, extEnd;
			nameEnd = hashBegin = hashEnd = extBegin = extEnd = int.MinValue;
			// 最後が.tmpで終わればテンポラリファイル。
			if (path.EndsWith(StorageCache.temporaryFilePostfix))
			{
				isTemporary = true;
				i = path.Length - 1 - StorageCache.temporaryFilePostfix.Length;
			}
			else
			{
				isTemporary = false;
				i = path.Length - 1;
			}
			extEnd = i + 1;

			// 次のピリオドを探す。スラッシュが出てくればそれ以降は見ない。
			while ((i >= 0) && (path[i] != '/'))
			{
				if (path[i] == '.')
				{
					extBegin = i + 1;
					hashEnd = i;
					i--;
					break;
				}
				i--;
			}

			// 次のピリオドを探す。スラッシュが出てくればそれ以降は見ない。
			while ((i >= 0) && (path[i] != '/'))
			{
				if (path[i] == '.')
				{
					hashBegin = i + 1;
					nameEnd = i;
					i--;
					break;
				}
				i--;
			}

			if ((hashEnd < 0) || (nameEnd < 0)) // 認識できない。失敗
			{
				name = null;
				hash = new FileHash();
				return false;
			}

			hash = new FileHash(path, hashBegin);
			_stringBuilder.Length = 0;
			_stringBuilder.Append(path, _root.Length + 1, nameEnd - _root.Length - 1); // rootはslashを含まないのでその次から
			_stringBuilder.Append('.');
			_stringBuilder.Append(path, extBegin, extEnd - extBegin);
			name = _stringBuilder.ToString();
			return true;
		}

		string _root;
		Loader.IAssetFileDatabase _database;
		Dictionary<string, StorageCache.Entry> _entries;
		System.Text.StringBuilder _stringBuilder;
	}
}
