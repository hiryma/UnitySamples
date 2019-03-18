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

		public StorageCache(string root, bool useHash)
		{
			if (root[root.Length - 1] == '/') // スラッシュ終わり
			{
				this.root = root.Substring(0, root.Length - 1);
			}
			else // スラッシュ終わりでない
			{
				this.root = root;
			}
			_useHash = useHash;

			_stringBuilder = new System.Text.StringBuilder();
			StartScan();
		}

		public void Start(Loader.IAssetFileDatabase database, bool update = true)
		{
			_database = database;
			if (update)
			{
				StartUpdate();
			}
		}

		/// 初期化処理、クリア、等が走っておらずブロックせずに利用可能な状態かを返す。
		public bool ready { get { return (_database != null) && (_entries != null) && (_thread == null); } }

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
			_scanner.Build(this.root, _useHash);
			var t1 = System.DateTime.Now;
			Debug.Log("Kayac.Lorder: ScanStorageCache take " + (t1 - t0).TotalMilliseconds + " msec.");
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
			Clear(this.root, _entries, _useHash);
			var t1 = System.DateTime.Now;
			Debug.Log("Kayac.Lorder: ClearStorageCache take " + (t1 - t0).TotalMilliseconds + " msec.");
		}

		static void Clear(
			string root,
			Dictionary<string, StorageCache.Entry> entries,
			bool useHash)
		{
			var stringBuilder = new System.Text.StringBuilder();
			// ファイル個別削除
			foreach (var item in entries)
			{
				var entry = item.Value;
				var cachePath = StorageCache.MakeCachePath(stringBuilder, item.Key, ref entry.hash, root, useHash);
				FileUtility.DeleteFile(cachePath);
			}
			FileUtility.RemoveEmptyDirectories(root);
		}


		void StartUpdate()
		{
			JoinThread();
			_thread = new Thread(UpdateThreadEntryPoint);
			_thread.Start();
		}

		void UpdateThreadEntryPoint()
		{
			var t0 = System.DateTime.Now;
			Update(_entries, this.root, _database, _useHash);
			var t1 = System.DateTime.Now;
			Debug.Log("Kayac.Lorder: UpdateStorageCache take " + (t1 - t0).TotalMilliseconds + " msec.");
		}

		static void Update(
			Dictionary<string, Entry> entries,
			string root,
			Loader.IAssetFileDatabase database,
			bool useHash)
		{
			var sb = new System.Text.StringBuilder();
			var removeNames = new List<string>();
			lock (entries)
			{
				foreach (var item in entries)
				{
					var name = item.Key;
					FileHash refHash;
					var entry = item.Value;
					bool match = false;
					int sizeBytes;
					if (database.GetFileMetaData(out refHash, out sizeBytes, name))
					{
						if (!useHash || (refHash == entry.hash)) // useHash==falseならハッシュは見ない。項目があれば良しとする
						{
							match = true;
						}
					}
					if (!match) // Databaseにない、あるいはハッシュが異なる→削除
					{
						sb.Length = 0;
						MakeCachePath(sb, name, ref entry.hash, root, useHash);
						var path = sb.ToString();
						FileUtility.DeleteFile(path);
						removeNames.Add(name);
					}
				}
			}
			foreach (var name in removeNames)
			{
				entries.Remove(name);
			}
		}

		void JoinThread()
		{
			if (_thread != null)
			{
				var t0 = Time.realtimeSinceStartup;
				_thread.Join();
				_thread = null;
				var t1 = Time.realtimeSinceStartup;
				var msec = (t1 - t0) * 1000f;
				if (msec >= 10f) // 10ms秒以上止まったら警告
				{
					Debug.LogWarning("Kayac.Loader(StorageCache): init or clear blocking main thread " + msec + " msec.");
				}
				if (_scanner != null) // スキャン
				{
					_entries = _scanner.GetResult();
					_scanner = null;
				}
				else if (_started) // 開始済みならクリア
				{
					_entries.Clear();
				}
				else // まだStartが終わっていないならUpdateによるスレッドなのでstart済みとする
				{
					_started = true;
				}
			}
		}

		public bool Has(string name, ref FileHash hash)
		{
//Debug.Log("Has: " + name + " " + hash);
			JoinThread(); // スレッド実行待ちでブロック
			Entry entry;
			if (_entries.TryGetValue(name, out entry))
			{
				if (!_useHash || (entry.hash == hash))
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
				sb.AppendFormat("root: {0}\n", this.root);
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
				absolute ? this.root : null,
				_useHash);
		}

		static int LastIndexOf(string s, int start, int length, char letter)
		{
			for (int i = (length - 1); i >= 0; i--)
			{
				int pos = start + i;
				if (s[pos] == letter)
				{
					return pos;
				}
			}
			return int.MinValue;
		}

		public static string MakeCachePath(
			System.Text.StringBuilder sb,
			string name,
			ref FileHash hash,
			string root,
			bool useHash)
		{
			sb.Length = 0;
			if (root != null)
			{
				sb.Append(root);
				sb.Append('/');
			}

			int extIndex = name.LastIndexOf('.');
			if (extIndex >= 0) // 拡張子がある場合
			{
				sb.Append(name, 0, extIndex);
				sb.Append('.');
				if (useHash)
				{
					hash.AppendString(sb);
					sb.Append('.');
				}
				sb.Append(name, extIndex + 1, name.Length - extIndex - 1);
			}
			else // 拡張子がない場合
			{
				sb.Append(name);
				if (useHash)
				{
					sb.Append('.');
					hash.AppendString(sb);
				}
			}
			return sb.ToString();
		}

		public struct Entry // 生成時刻を足す可能性は高い。性能のためにパスを加えることもありうる。
		{
			public FileHash hash;
		}

		public string root { get; private set; } // スラッシュが後ろについてない状態で持つ
		Dictionary<string, Entry> _entries;
		System.Text.StringBuilder _stringBuilder; // 文字列処理用StringBuilder
		Loader.IAssetFileDatabase _database;
		Thread _thread;
		StorageCacheScanner _scanner;
		bool _started;
		bool _useHash;
	}
}
