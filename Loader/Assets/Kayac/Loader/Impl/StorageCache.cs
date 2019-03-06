using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;

namespace Kayac.LoaderImpl
{
	public class StorageCache
	{
		public StorageCache(string root, Loader.IAssetFileDatabase database)
		{
			string rootWithoutSlash = null;
			if (root[root.Length - 1] == '/') // スラッシュ終わりならスラッシュのないもので存在確認
			{
				rootWithoutSlash = root.Substring(0, root.Length - 1);
				_root = root;
			}
			else // スラッシュ終わりでない
			{
				rootWithoutSlash = root;
				_root = root + "/";
			}

			_entries = new Dictionary<string, Entry>();
			_stringBuilder = new System.Text.StringBuilder();
			// キャッシュフォルダないじゃん。作るよ。
			if (!Directory.Exists(rootWithoutSlash)) // TODO: ここそのうち別スレ化
			{
				Directory.CreateDirectory(rootWithoutSlash);
			}
			else
			{
				var t0 = Time.realtimeSinceStartup;
				Scan(rootWithoutSlash, database);
				var t1 = Time.realtimeSinceStartup;
				Debug.Log("Kayac.Lorder : CacheFileScan take " + (t1 - t0) + " sec.");
			}
		}

		void Scan(string path, Loader.IAssetFileDatabase database)
		{
			var dirs = Directory.GetDirectories(path);
			foreach (var dir in dirs)
			{
				Scan(dir, database);
			}
			var files = Directory.GetFiles(path);
			foreach (var file in files)
			{
				Entry newEntry, oldEntry;
				string name;
				bool isTemporary;
				if (!ParseCachePath(out name, out newEntry.hash, out isTemporary, file))
				{
					// 認識できないファイルなので無視。誰かが置いたのだろう。
				}
				else if (isTemporary) // 書き込み中が見つかれば問答無用で削除
				{
					DeleteFile(file);
				}
				else if (_entries.TryGetValue(name, out oldEntry)) // 二個あった場合、両方消して解決を図る。どちらが正か判断がつかない。
				{
					_entries.Remove(name);
					DeleteFile(file);
					string oldPath = MakeCachePath(name, ref oldEntry.hash);
					DeleteFile(oldPath);
				}
				else
				{
					Hash128 hash;
					if (database.GetFileMetaData(out hash, name))
					{
						_entries.Add(name, newEntry);
					}
					else // メタデータがないので削除する
					{
						DeleteFile(file);
					}
				}
			}
		}

		void DeleteFile(string path)
		{
			try
			{
				File.Delete(path);
			}
			catch (Exception e)
			{
				Debug.LogError("File.Delete() failed: " + path + " Exception:" + e);
			}
		}

		void MoveFile(string from, string to)
		{
			try
			{
				File.Move(from, to);
			}
			catch (Exception e)
			{
				Debug.LogError("File.Move() failed: " + from + " -> " + to + " Exception:" + e);
			}
		}


		public void Clear()
		{
			// ファイル個別削除
			foreach (var item in _entries)
			{
				var entry = item.Value;
				var cachePath = MakeCachePath(item.Key, ref entry.hash);
				DeleteFile(cachePath);
			}
			// 空ディレクトリ再帰削除
			RemoveIfEmpty(_root);
			_entries.Clear();
		}

		bool RemoveIfEmpty(string path)
		{
			// 子優先再帰
			var dirs = Directory.GetDirectories(path);
			int childDirCount = dirs.Length;
			foreach (var dir in dirs)
			{
				if (RemoveIfEmpty(dir))
				{
					childDirCount--;
				}
			}
			// 子が空になって、ファイルも空なら自分を削除
			var files = Directory.GetFiles(path);
			if ((childDirCount == 0) && (files.Length == 0))
			{
				Directory.Delete(path);
				return true;
			}
			else
			{
				return false;
			}
		}

		public bool Has(string name, ref Hash128 hash)
		{
			Entry entry;
			if (_entries.TryGetValue(name, out entry))
			{
				if (entry.hash == hash)
				{
					return true;
				}
			}
			return false;
		}

		public void Dump(System.Text.StringBuilder stringBuilderToAppend, bool summaryOnly = false)
		{
			UnityEngine.Profiling.Profiler.BeginSample("Kayac.LoaderImpl.StorageCache.Dump");
			var sb = stringBuilderToAppend;
			sb.AppendFormat("[StorageCache] count:{0}\n", _entries.Count);
			sb.AppendFormat("root: {0}\n", _root);
			if (!summaryOnly)
			{
				foreach (var item in _entries)
				{
					sb.AppendFormat("{0} ({1})\n", item.Key, item.Value.hash);
				}
			}
			UnityEngine.Profiling.Profiler.EndSample();
		}

		public void OnFileSaved(string relativeTemporaryPath, string name, ref Hash128 hash)
		{
			Entry entry;
			bool exists = _entries.TryGetValue(name, out entry);
			// すでにあれば、古いファイルを消す。
			if (exists)
			{
				string oldCachePath = MakeCachePath(name, ref entry.hash);
				DeleteFile(oldCachePath);
			}
			// ここでアプリが落ちた場合、ダウンロードし直しになるがゴミが残るよりは良い
			// moveする TODO: 別スレ化
			string newCachePath = MakeCachePath(name, ref hash);
			MoveFile(_root + relativeTemporaryPath, newCachePath);

			entry.hash = hash;
			// テーブル更新
			_entries[name] = entry;
		}

		bool ParseCachePath(out string name, out Hash128 hash, out bool isTemporary, string path)
		{
			// hoge/fuga/fileName.0123456701234567012345671234567.ext.tmp
			// の形式。fileNameまでをname、数字羅列部分をhashとして取り出したい。
			// name内に.があることは許したいのと、tmpがあったりなかったりするので、
			// 後ろから順に.を見つけて範囲を確定していくという手間のかかる状態になっている。
			// regexの方が楽なのだが、そこそこ回数呼ぶので速度を重視して手で書いた。
			int i, nameEnd, hashBegin, hashEnd, extBegin, extEnd;
			nameEnd = hashBegin = hashEnd = extBegin = extEnd = int.MinValue;
			// 最後が.tmpで終わればテンポラリファイル。
			if (path.EndsWith(".tmp"))
			{
				isTemporary = true;
				i = path.Length - 5;
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
				hash = new Hash128();
				return false;
			}

			hash = Hash128.Parse(path.Substring(hashBegin, hashEnd - hashBegin)); // TODO: ここのSubstring削ってGCAlloc減らしたいね!
			// substringしてGCAllocしたくない
			_stringBuilder.Length = 0;
			_stringBuilder.Append(path, _root.Length, nameEnd - _root.Length);
			_stringBuilder.Append('.');
			_stringBuilder.Append(path, extBegin, extEnd - extBegin);
			name = _stringBuilder.ToString();
			return true;
		}

		public string MakeCachePath(string name, ref Hash128 hash, bool absolute = true)
		{
			int extIndex = name.LastIndexOf('.');
			_stringBuilder.Length = 0;
			if (absolute)
			{
				_stringBuilder.Append(_root);
			}
			_stringBuilder.Append(name, 0, extIndex);
			_stringBuilder.Append('.');
			_stringBuilder.Append(hash.ToString());
			_stringBuilder.Append('.');
			_stringBuilder.Append(name, extIndex + 1, name.Length - extIndex - 1);
			return _stringBuilder.ToString();
		}

		// root相対のパスを返す。FileWriterにも同じものが渡っているはずなので
		public string MakeRelativeTemporaryPath(string name, ref Hash128 hash)
		{
			return MakeCachePath(name, ref hash, absolute: false) + ".tmp";
		}

		struct Entry // 生成時刻を足す可能性は高い。性能のためにパスを加えることもありうる。
		{
			public Hash128 hash;
		}

		string _root;
		Dictionary<string, Entry> _entries;
		System.Text.StringBuilder _stringBuilder; // 文字列処理用StringBuilder
	}
}
