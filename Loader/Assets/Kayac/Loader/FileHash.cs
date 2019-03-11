using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Kayac
{
	/// Hash128が別スレッドから使えない問題+ToStringでGCAllocしまくる問題に対処するためにやむをえず自作。
	public struct FileHash
	{
		/// Unity側から変換。GCAllocするので最小にしたい。
		public FileHash(Hash128 hash128)
		{
			var s = hash128.ToString(); // 32桁の16進が得られる前提で書いている
			Debug.Assert(s.Length == 32, "Hash128.ToString() return unexpected string. length != 32.");
			v0 = Parse(s, 0);
			v1 = Parse(s, 8);
			v2 = Parse(s, 16);
			v3 = Parse(s, 24);
		}

		/// バージョン番号からテキトーに生成する。new FileHash(version.ToString("x32"))と同一の結果。
		public FileHash(int version)
		{
			v0 = v1 = v2 = 0;
			v3 = (uint)version;
		}

		// 32桁の16進数文字列の一部から生成する。32文字ない場合は中途半端な値になる。死にはしない。
		public FileHash(string s, int start = 0)
		{
			v0 = Parse(s, start + 0);
			v1 = Parse(s, start + 8);
			v2 = Parse(s, start + 16);
			v3 = Parse(s, start + 24);
		}

		public override int GetHashCode() // 手抜き
		{
			return (int)(v0 + v1 + v2 + v3);
		}

		/// メモリを汚すので極力使わないこと
		public override string ToString()
		{
			return string.Format("{0:x8}{1:x8}{2:x8}{3:x8}", v0, v1, v2, v3);
		}

		public void AppendString(System.Text.StringBuilder sb)
		{
			AppendUint(sb, v0);
			AppendUint(sb, v1);
			AppendUint(sb, v2);
			AppendUint(sb, v3);
		}

		public static bool operator ==(FileHash a, FileHash b)
		{
			return (a.v0 == b.v0)
				&& (a.v1 == b.v1)
				&& (a.v2 == b.v2)
				&& (a.v3 == b.v3);
		}
		public static bool operator !=(FileHash a, FileHash b)
		{
			return !(a == b);
		}

		public override bool Equals(object o)
		{
			var ret = false;
			if (o is FileHash)
			{
				var another = (FileHash)o;
				ret = (v0 == another.v0)
					&& (v1 == another.v1)
					&& (v2 == another.v2)
					&& (v3 == another.v3);
			}
			return ret;
		}

		static void AppendUint(System.Text.StringBuilder sb, uint x)
		{
			int shift = 28;
			for (int i = 0; i < 8; i++)
			{
				var digit = (x >> shift) & 0xf;
				shift -= 4;
				char c;
				if (digit < 10)
				{
					c = (char)(digit + '0');
				}
				else
				{
					c = (char)((digit - 10) + 'a');
				}
				sb.Append(c);
			}
		}

		static uint Parse(string s, int start)
		{
			int x = 0;
			for (int i = 0; i < 8; i++)
			{
				if ((start + i) >= s.Length)
				{
					break;
				}
				var c = s[start + i];
				x |= ParseHex(c) << ((7 - i) * 4);
			}
			return (uint)x;
		}

		/// charの0-9、a-f、A-Fを0-15のintに変換する
		static int ParseHex(char c)
		{
			int ret = 0;
			if ((c >= 'a') && (c <= 'f'))
			{
				ret = (c - 'a') + 10;
			}
			else if ((c >= 'A') && (c <= 'F'))
			{
				ret = (c - 'A') + 10;
			}
			else if ((c >= '0') && (c <= '9'))
			{
				ret = c - '0';
			}
			return ret;
		}
		uint v0, v1, v2, v3;
	}
}
