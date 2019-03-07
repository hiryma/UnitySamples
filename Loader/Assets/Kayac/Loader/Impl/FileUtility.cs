using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;

namespace Kayac.LoaderImpl
{
	/// このクラスの関数は全て例外を投げない(できるだけ)。戻り値で返すこともある。
	public static class FileUtility
	{
		public static void RemoveEmptyDirectories(string root)
		{
			var dirs = GetDirectoriesNoThrow(root);
			foreach (var dir in dirs)
			{
				RemoveIfEmptyRecursive(dir);
			}
		}

		static bool RemoveIfEmptyRecursive(string path) // 別スレッド実行
		{
			// 子優先再帰
			var dirs = GetDirectoriesNoThrow(path);
			int childDirCount = dirs.Length;
			foreach (var dir in dirs)
			{
				if (RemoveIfEmptyRecursive(dir))
				{
					childDirCount--;
				}
			}
			// 子が空になって、ファイルも空なら自分を削除
			var files = GetFilesNoThrow(path);
			if ((childDirCount == 0) && (files.Length == 0))
			{
				DeleteDirectory(path);
				return true;
			}
			else
			{
				return false;
			}
		}

		/// 例外を投げない
		static string[] GetDirectoriesNoThrow(string path)
		{
			string[] ret = null;
			try
			{
				ret = Directory.GetDirectories(path);
			}
			catch (Exception e)
			{
				Debug.LogException(e);
				ret = new string[0];
			}
			return ret;
		}

		/// 例外を投げない
		static string[] GetFilesNoThrow(string path)
		{
			string[] ret = null;
			try
			{
				ret = Directory.GetFiles(path);
			}
			catch (Exception e)
			{
				Debug.LogException(e);
				ret = new string[0];
			}
			return ret;
		}

		/// 例外を投げない
		static Exception DeleteDirectory(string path)
		{
			Exception ret = null;
			try
			{
				Directory.Delete(path);
			}
			catch (Exception e)
			{
				Debug.LogException(e);
				ret = e;
			}
			return ret;
		}

		/// 例外を投げない
		public static Exception DeleteFile(string path)
		{
			Exception ret = null;
			try
			{
				File.Delete(path);
			}
			catch (Exception e)
			{
				Debug.LogException(e);
				ret = e;
			}
			return ret;
		}

		// 何かやって例外出た時に、調べられる限り原因を調べる
		public static Exception InspectIoError(string path, string toPath, Exception originalException)
		{
			Exception ret = originalException;
			try
			{
				var info = new FileInfo(path);
				if (!info.Exists) // ファイルなくなってる
				{
					ret = new FileNotFoundException("File not found: " + path);
				}
				else if (toPath != null)
				{
					var toInfo = new FileInfo(toPath);
					if (toInfo.Exists)
					{
						ret = new IOException("destination path already exists. " + toPath);
					}
					else if (!toInfo.Directory.Exists)
					{
						ret = new DirectoryNotFoundException("destination directory not found: " + toPath);
					}
				}
			}
			catch (Exception e)
			{
				ret = e;
			}
			return ret;
		}
	}
}