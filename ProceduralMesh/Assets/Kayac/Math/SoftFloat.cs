using System;
using System.Runtime.InteropServices;

namespace Kayac
{
	public struct SoftFloat : IEquatable<SoftFloat>, IComparable<SoftFloat>
	{
		public SoftFloat(float a)
		{
			var u = new Union();
			u.f = a;
			x = u.i;
		}

		public float ToFloat()
		{
			var u = new Union();
			u.i = x;
			return u.f;
		}

		public string ToStringBinary()
		{
			return "0x" + x.ToString("X");
		}

		public override string ToString()
		{
			return ToFloat().ToString();
		}

		public string ToString(string format)
		{
			return ToFloat().ToString(format);
		}

		// 比較
		public bool Equals(SoftFloat a)
		{
			return Compare(x, a.x) == 0;
		}

		public int CompareTo(SoftFloat a)
		{
			return Compare(x, a.x);
		}

		// 便利関数群
		public static SoftFloat Abs(SoftFloat a)
		{
			a.x = Abs(a.x);
			return a;
		}

		public static SoftFloat Min(SoftFloat a, SoftFloat b)
		{
			return (a.CompareTo(b) < 0) ? a : b;
		}

		public static SoftFloat Max(SoftFloat a, SoftFloat b)
		{
			return (a.CompareTo(b) > 0) ? a : b;
		}

		public static SoftFloat Sqrt(SoftFloat a)
		{
			var x = Rsqrt(a.x);
			x = Mul(x, a.x);
			a.x = x;
			return a;
		}

		public static SoftFloat Exp(SoftFloat a)
		{
			throw new System.NotImplementedException();
			return new SoftFloat(0f);
		}

		public static SoftFloat Log(SoftFloat a)
		{
			throw new System.NotImplementedException();
			return new SoftFloat(0f);
		}

		public static SoftFloat Log2(SoftFloat a)
		{
			throw new System.NotImplementedException();
			return new SoftFloat(0f);
		}

		public static SoftFloat Exp2(SoftFloat a)
		{
			throw new System.NotImplementedException();
			return new SoftFloat(0f);
		}

		public static SoftFloat Exp10(SoftFloat a)
		{
			throw new System.NotImplementedException();
			return new SoftFloat(0f);
		}

		public static SoftFloat Log10(SoftFloat a)
		{
			throw new System.NotImplementedException();
			return new SoftFloat(0f);
		}

		public static SoftFloat Pow(SoftFloat a, SoftFloat e)
		{
			throw new System.NotImplementedException();
			return new SoftFloat(0f);
		}

		public static SoftFloat Sin(SoftFloat a)
		{
			throw new System.NotImplementedException();
			return new SoftFloat(0f);
		}

		public static SoftFloat Cos(SoftFloat a)
		{
			throw new System.NotImplementedException();
			return new SoftFloat(0f);
		}

		public static SoftFloat Tan(SoftFloat a)
		{
			throw new System.NotImplementedException();
			return new SoftFloat(0f);
		}

		public static SoftFloat Asin(SoftFloat a)
		{
			throw new System.NotImplementedException();
			return new SoftFloat(0f);
		}

		public static SoftFloat Acos(SoftFloat a)
		{
			throw new System.NotImplementedException();
			return new SoftFloat(0f);
		}

		public static SoftFloat Atan(SoftFloat a)
		{
			throw new System.NotImplementedException();
			return new SoftFloat(0f);
		}

		public static SoftFloat Atan2(SoftFloat y, SoftFloat x)
		{
			throw new System.NotImplementedException();
			return new SoftFloat(0f);
		}

		public static SoftFloat Fmod(SoftFloat a, SoftFloat b)
		{
			throw new System.NotImplementedException();
			return new SoftFloat(0f);
		}

		// 演算子群
		public static SoftFloat operator +(SoftFloat a)
		{
			return a;
		}

		public static SoftFloat operator -(SoftFloat a)
		{
			a.x = Neg(a.x);
			return a;
		}

		public static SoftFloat operator +(SoftFloat a, SoftFloat b)
		{
			a.x = Add(a.x, b.x);
			return a;
		}

		public static SoftFloat operator -(SoftFloat a, SoftFloat b)
		{
			a.x = Add(a.x, Neg(b.x));
			return a;
		}

		public static SoftFloat operator *(SoftFloat a, SoftFloat b)
		{
			a.x = Mul(a.x, b.x);
			return a;
		}

		public static SoftFloat operator /(SoftFloat a, SoftFloat b)
		{
			a.x = Mul(a.x, Reciprocal(b.x));
			return a;
		}

		public static bool operator <(SoftFloat a, SoftFloat b)
		{
			return Compare(a.x, b.x) < 0;
		}

		public static bool operator >(SoftFloat a, SoftFloat b)
		{
			return Compare(a.x, b.x) > 0;
		}

		public static bool operator <=(SoftFloat a, SoftFloat b)
		{
			return Compare(a.x, b.x) <= 0;
		}

		public static bool operator >=(SoftFloat a, SoftFloat b)
		{
			return Compare(a.x, b.x) >= 0;
		}

		public static bool operator ==(SoftFloat a, SoftFloat b)
		{
			return Compare(a.x, b.x) == 0;
		}

		public static bool operator !=(SoftFloat a, SoftFloat b)
		{
			return Compare(a.x, b.x) == 0;
		}

		public override int GetHashCode()
		{
			return x.GetHashCode();
		}

		public static SoftFloat One
		{
			get
			{
				SoftFloat r;
				r.x = oneBits;
				return r;
			}
		}

		// non-public --------------

		// static Members
		public static readonly uint quarterBits; //0.25
		public static readonly uint halfBits; //0.5
		public static readonly uint oneBits; //1
		public static readonly uint twoBits; //2
		public static readonly uint half3Bits; // 1.5
		public static readonly uint threeBits; //3
		public static readonly uint quarter5Bits; // 1.25
		static SoftFloat()
		{
			// 2羃
			quarterBits = ComposeBits(0, -2, 0);
			halfBits = ComposeBits(0, -1, 0);
			oneBits = ComposeBits(0, 0, 0);
			twoBits = ComposeBits(0, 1, 0);

			// 3系
			half3Bits = ComposeBits(0, 0, 1 << (mantissaBits - 1));
			threeBits = ComposeBits(0, 1, 1 << (mantissaBits - 1));

			// 5系
			quarter5Bits = ComposeBits(0, 0, 1 << (mantissaBits - 2));
		}

		// 基幹になるビット表現直関数群
		static uint Add(uint a, uint b)
		{
			// 指数部取り出し
			var e0 = GetExp(a);
			var e1 = GetExp(b);
			// 大きい方に合わせて小さい方の仮数部をシフト
			var m0 = GetMantissa(a) + hiddenBit;
			var m1 = GetMantissa(b) + hiddenBit;
			var s0 = GetSign(a);
			var s1 = GetSign(b);
			int e; // 最終指数
			if (e0 > e1)
			{
				e = e0;
				m1 >>= (e - e1);
			}
			else
			{
				e = e1;
				m0 >>= (e - e0);
			}
			// 符号加味
			m0 = (s0 != 0) ? -m0 : m0;
			m1 = (s1 != 0) ? -m1 : m1;
			// 仮数部加算
			int m = m0 + m1;
			uint s = 0;
			if (m < 0) // 負数の場合、符号反転
			{
				m = -m;
				s = signMask;
			}

			var msb = FindMsbIndex((uint)m);
			var actualMantissaBits = mantissaBits + 1;
			if (msb > actualMantissaBits)
			{
				var shift = msb - actualMantissaBits;
				e += shift;
				m >>= shift;
			}
			else
			{
				var shift = actualMantissaBits - msb;
				e -= shift;
				m <<= shift;
			}
			// 合成
			var x = ComposeBits(s, e, m & (int)mantissaMask);
			return x;
		}

		static uint Neg(uint x) // 符号ビット反転
		{
			x = (~x & signMask) | (x & (~signMask));
			return x;
		}

		static uint Abs(uint x) // 符号ビットクリア
		{
			return x & ~signMask;
		}

		static uint Mul(uint a, uint b)
		{
			// 指数部取り出し
			var e0 = GetExp(a);
			var e1 = GetExp(b);
			// 大きい方に合わせて小さい方の仮数部をシフト
			var m0 = GetMantissa(a) + hiddenBit;
			var m1 = GetMantissa(b) + hiddenBit;
			var s0 = GetSign(a);
			var s1 = GetSign(b);
			var e = e0 + e1;

			var m = (ulong)m0 * (ulong)m1; // 64bit使っちゃうよ。もういいよね。
			var msb = FindMsbIndex(m);

			// 24bit * 24bitで、最上位ビットはどちらも立っている(隠れビット)ので、
			// 答えは47bitか48bitのいずれか。
			m >>= mantissaBits; // これで24bitか25bitになる
			if (m > actualMantissaMax) // 25bitだった場合、さらに半分
			{
				m >>= 1;
				e++;
			}
			if (m < hiddenBit)
			{
				throw new System.Exception();
			}
			var s = (s0 == s1) ? 0 : signMask;
			// 合成
			var x = ComposeBits(s, e, (int)m & (int)mantissaMask);
			return x;
		}

		static uint Reciprocal(uint x)
		{
			/*
			1から2に正規化してニュートン法、
			しかる後に指数部を調整して反転、
			符号を復元すれば良い。
			*/
			var eOffset = GetExp(x);
			var s = GetSign(x);
			var m = GetMantissa(x);
			var a = ComposeBits(0, 0, m); // [1,2)

			// 初期値を設定する。1であれば1、2であれば0.5なので、間を線形補間する。
			// TODO: もっと改良できる
			x = Mul(halfBits, a); // 0.5a
			x = Neg(x); // -0.5a
			x = Add(half3Bits, x); // 1.5 - 0.5a
			for (int i = 0; i < 4; i++) // TODO: 回数は後で調整
			{
				var t = Mul(a, x); // ax
				t = Neg(t); //-ax
				t = Add(twoBits, t); //2-ax
				x = Mul(x, t); //x(2-ax)
			}
			var e = GetExp(x) - eOffset;
			x = ComposeBits(s, e, GetMantissa(x));
			return x;
		}

		// 1 / sqrt(a)
		static uint Rsqrt(uint x)
		{
			/*
			1から4に正規化してニュートン法、
			しかる後に指数部を調整して反転、
			符号を復元すれば良い。
			x = x(1.5 - 0.5*ax^2)
			*/
			var eOffset = GetExp(x);
			var s = GetSign(x);
			var m = GetMantissa(x);
			var ae = 0;
			if ((eOffset & 1) != 0) // 奇数である場合、偶数にするために1を引く
			{
				eOffset--;
				ae++;
			}
			var a = ComposeBits(0, ae, m); // [1,4)

			// 初期値を設定する。1であれば1、4であれば0.5
			// ini = 1.5 - 0.25xで、x=1で1.25、x=4で0.5となる
			//間は線形補間する。TODO: もっと改良できる
			x = Mul(quarterBits, a); // 0.25a
			x = Neg(x); // -0.25a
			x = Add(half3Bits, x); // 1.5 - 0.25a
			for (int i = 0; i < 4; i++) // TODO: 回数は後で調整
			{
				var t = halfBits;
				t = Mul(t, a); // 0.5a
				t = Mul(t, x); // 0.5ax
				t = Mul(t, x); // 0.5ax^2
				t = Neg(t); // -0.5ax^2
				t = Add(half3Bits, t); // 1.5 - 0.5ax^2
				x = Mul(x, t); // x(1.5 - 0.5ax^2)
			}
			var e = GetExp(x) - (eOffset / 2); // 指数は半減させる(4を乗じる所を2を乗じる)
			x = ComposeBits(s, e, GetMantissa(x));
			return x;
		}

		static int Compare(uint a, uint b)
		{
			var s0 = a & signMask;
			var s1 = b & signMask;
			var a0 = a & ~signMask;
			var a1 = b & ~signMask;
			if ((a0 == 0) && (a1 == 0)) // ゼロなら等しい
			{
				return 0;
			}
			else if (s0 == 0)
			{
				if (s1 == 0) // 両方正
				{
					return a0.CompareTo(a1);
				}
				else // 自分が正、相手が負なので1
				{
					return 1;
				}
			}
			else if (s1 == 0) // 自分が負、相手が正
			{
				return -1;
			}
			else // 両方負
			{
				return a1.CompareTo(a0);
			}
		}

		static string ToString(uint x)
		{
			var u = new Union();
			u.i = x;
			return u.f.ToString();
		}

		static int GetExp(uint x)
		{
			return (int)((x & expMask) >> expShift) - expBias;
		}

		static int GetMantissa(uint x)
		{
			return (int)(x & mantissaMask);
		}

		static uint GetSign(uint x)
		{
			return x & signMask;
		}

		static uint ComposeBits(uint sign, int exp, int mantissa)
		{
			uint r = (sign != 0) ? signMask : 0;
			r |= (uint)((exp + expBias) << expShift);
			r |= (uint)mantissa;
			return r;
		}

		static int CountBitOne(uint x)
		{
			x = (x & 0x55555555) + ((x >> 1) & 0x55555555);
			x = (x & 0x33333333) + ((x >> 2) & 0x33333333);
			x = (x & 0x0f0f0f0f) + ((x >> 4) & 0x0f0f0f0f);
			x = (x & 0x00ff00ff) + ((x >> 8) & 0x00ff00ff);
			x = (x & 0x0000ffff) + ((x >> 16) & 0x0000ffff);
			return (int)x;
		}

		static uint MakeAll1UnderMsb(uint x)
		{
			x |= x >> 1; // 上2bitが1になる
			x |= x >> 2; // 上4bitが1になる
			x |= x >> 4; // 上8bitが1になる
			x |= x >> 8; // 上16bitが1になる
			x |= x >> 16; // 上32bitが1になる
			return x;
		}

		static int FindMsbIndex(uint x)
		{
			var t = MakeAll1UnderMsb(x);
			var ret = CountBitOne(t);
			return ret;
		}

		static int CountBitOne(ulong x)
		{
			x = (x & 0x5555555555555555) + ((x >> 1) & 0x5555555555555555);
			x = (x & 0x3333333333333333) + ((x >> 2) & 0x3333333333333333);
			x = (x & 0x0f0f0f0f0f0f0f0f) + ((x >> 4) & 0x0f0f0f0f0f0f0f0f);
			x = (x & 0x00ff00ff00ff00ff) + ((x >> 8) & 0x00ff00ff00ff00ff);
			x = (x & 0x0000ffff0000ffff) + ((x >> 16) & 0x0000ffff0000ffff);
			x = (x & 0x00000000ffffffff) + ((x >> 32) & 0x00000000ffffffff);
			return (int)x;
		}

		static ulong MakeAll1UnderMsb(ulong x)
		{
			x |= x >> 1; // 上2bitが1になる
			x |= x >> 2; // 上4bitが1になる
			x |= x >> 4; // 上8bitが1になる
			x |= x >> 8; // 上16bitが1になる
			x |= x >> 16; // 上32bitが1になる
			x |= x >> 32; // 上64bitが1になる
			return x;
		}

		static int FindMsbIndex(ulong x)
		{
			var t = MakeAll1UnderMsb(x);
			var ret = CountBitOne(t);
			return ret;
		}

		uint x; // 32bit整数として保持
		const int mantissaBits = 23;
		const int expBits = 8;
		const int expShift = mantissaBits;
		const int signShift = expShift + expBits;
		const uint mantissaMask = (1U << mantissaBits) - 1U; // 仮数部
		const uint expMask = (1U << signShift) - 1U - mantissaMask; // 指数部
		const uint signMask = 1U << signShift;
		const int hiddenBit = (1 << mantissaBits);
		const int expBias = (1 << (expBits - 1)) - 1;
		const int actualMantissaMax = (1 << (mantissaBits + 1)) - 1; // 隠れビット込みの仮数部最大値。1677215

		[StructLayout(LayoutKind.Explicit)]
		struct Union
		{
			[System.Runtime.InteropServices.FieldOffset(0)]
			public uint i;
			[System.Runtime.InteropServices.FieldOffset(0)]
			public float f;
		}

		public static void Test(System.Action<string> logFunc)
		{
			System.Random random = new System.Random();
			for (int i = 0; i < 100; i++)
			{
				var a = random.NextDouble() - 0.5;
				var b = random.NextDouble() - 0.5;
				a = System.Math.Exp(a * 10.0);
				a = System.Math.Exp(b * 10.0);
				var af = (float)a;
				var bf = (float)b;
				var asf = new SoftFloat(af);
				var bsf = new SoftFloat(bf);
				logFunc(string.Format("{0} + {1} = {2} {3} {4}", a, b, (a + b).ToString("F9"), (af + bf).ToString("F8"), (asf + bsf).ToString("F8")));
				logFunc(string.Format("{0} - {1} = {2} {3} {4}", a, b, (a - b).ToString("F9"), (af - bf).ToString("F8"), (asf - bsf).ToString("F8")));
				logFunc(string.Format("{0} * {1} = {2} {3} {4}", a, b, (a * b).ToString("F9"), (af * bf).ToString("F8"), (asf * bsf).ToString("F8")));
				logFunc(string.Format("{0} / {1} = {2} {3} {4}", a, b, (a / b).ToString("F9"), (af / bf).ToString("F8"), (asf / bsf).ToString("F8")));
				logFunc(string.Format("Sqrt {0} = {1} {2} {3}", a, Math.Sqrt(a), (float)Math.Sqrt(af), SoftFloat.Sqrt(asf)));
			}
		}
	}
}