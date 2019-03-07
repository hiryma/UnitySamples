using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Threading;

namespace Kayac.LoaderImpl
{
	public class StorageCache
	{
		public const string temporaryFilePostfix = ".tmp";

		public StorageCache(string root, Loader.IAssetFileDatabase database)
		{
			_database = database;
			if (root[root.Length - 1] == '/') // スラッシュ終わり
			{
				_root = root.Substring(0, root.Length - 1);
			}
			else // スラッシュ終わりでない
			{
				_root = root;
			}

			_stringBuilder = new System.Text.StringBuilder();
			StartScan();
		}

		/// 初期化処理、クリア、等が走っておらずブロックせずに利用可能な状態かを返す。
		public bool ready { get { return (_entries != null) && (_thread == null); } }

		void StartScan()
		{
			JoinThread();
			_scanner = new StorageCacheScanner();
			_thread = new Thread(ScanThreadEntryPoint);
			_thread.Start();
		}

		void ScanThreadEntryPoint()
		{
			var t0 = System.DateTime.Now;
			_scanner.Scan(_root, _database);
			var t1 = System.DateTime.Now;
			Debug.Log("Kayac.Lorder: ScanStorageCache take " + (t1 - t0).TotalSeconds + " sec.");
		}

		public void StartClear()
		{
			JoinThread();
			_thread = new Thread(ClearThreadEntryPoint);
			_thread.Start();
		}

		void ClearThreadEntryPoint()
		{
			var t0 = System.DateTime.Now;
			Clear(_root, _entries);
			var t1 = System.DateTime.Now;
			Debug.Log("Kayac.Lorder: ClearStorageCache take " + (t1 - t0).TotalSeconds + " sec.");
		}

		void JoinThread()
		{
			if (_thread != null)
			{
				var t0 = Time.realtimeSinceStartup;
				_thread.Join();
				_thread = null;
				var t1 = Time.realtimeSinceStartup;
				var time = t1 - t0;
				if (time >= 0.01f) // 10ms秒以上止まったら警告
				{
					Debug.LogWarning("Kayac.Loader(StorageCache): init or clear blocking main thread " + (t1 - t0) + " sec.");
				}
				if (_scanner != null) // スキャン
				{
					_entries = _scanner.GetResult();
					_scanner = null;
				}
				else // クリアー
				{
					_entries.Clear();
				}
			}
		}

		public bool Has(string name, ref FileHash hash)
		{
			JoinThread(); // スレッド実行待ちでブロック
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
			JoinThread(); // スレッド実行待ちでブロック
			UnityEngine.Profiling.Profiler.BeginSample("Kayac.LoaderImpl.StorageCache.Dump");
			var sb = stringBuilderToAppend;
			lock (_entries)
			{
				sb.AppendFormat("[StorageCache] count:{0}\n", _entries.Count);
				sb.AppendFormat("root: {0}\n", _root);
				if (!summaryOnly)
				{
					foreach (var item in _entries)
					{
						sb.AppendFormat("{0} ({1})\n", item.Key, item.Value.hash);
					}
				}
			}
			UnityEngine.Profiling.Profiler.EndSample();
		}

		public bool TryDelete(string name)
		{
			JoinThread();
			var ret = false;
			lock (_entries)
			{
				Entry entry;
				if (_entries.TryGetValue(name, out entry))
				{
					string cachePath = MakeCachePath(name, ref entry.hash, absolute: true);
					FileUtility.DeleteFile(cachePath);
					ret = true;
				}
			}
			return ret;
		}

		public void OnFileSaved(string name, ref FileHash hash)
		{
			JoinThread(); // スレッド実行待ちでブロック
			// すでにあれば、古いファイルを消す。
			TryDelete(name);
			// テーブル更新
			Entry entry;
			entry.hash = hash;
			_entries[name] = entry;
		}

		public string MakeCachePath(
			string name,
			ref FileHash hash,
			bool absolute)
		{
			return MakeCachePath(
				_stringBuilder,
				name,
				ref hash,
				absolute ? _root : null);
		}

		public static string MakeCachePath(
			System.Text.StringBuilder sb,
			string name,
			ref FileHash hash,
			string root)
		{
			int extIndex = name.LastIndexOf('.');
			sb.Length = 0;
			if (root != null)
			{
				sb.Append(root);
				sb.Append('/');
			}
			sb.Append(name, 0, extIndex);
			sb.Append('.');
			hash.AppendString(sb);
			sb.Append('.');
			sb.Append(name, extIndex + 1, name.Length - extIndex - 1);
			return sb.ToString();
		}

		public static void Clear(string root, Dictionary<string, StorageCache.Entry> entries)
		{
			var stringBuilder = new System.Text.StringBuilder();
			// ファイル個別削除
			foreach (var item in entries)
			{
				var entry = item.Value;
				var cachePath = StorageCache.MakeCachePath(stringBuilder, item.Key, ref entry.hash, root);
				FileUtility.DeleteFile(cachePath);
			}
			FileUtility.RemoveEmptyDirectories(root);
		}

		public struct Entry // 生成時刻を足す可能性は高い。性能のためにパスを加えることもありうる。
		{
			public FileHash hash;
		}

		string _root; // スラッシュが後ろについてない状態で持つ
		Dictionary<string, Entry> _entries;
		System.Text.StringBuilder _stringBuilder; // 文字列処理用StringBuilder
		Loader.IAssetFileDatabase _database;
		Thread _thread;
		StorageCacheScanner _scanner;
	}
}
