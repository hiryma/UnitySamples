using UnityEngine;
using System.Threading;
using System.Collections.Generic;
using UnityEngine.UI;

public class RandomComparison : MonoBehaviour
{
	[SerializeField]
	RawImage _image;
	[SerializeField]
	Text _randomTypeText;
	[SerializeField]
	Text _testTypeText;

	Texture2D _texture;
	IRandom[] _randoms;
	ThreadPool _threadPool;
	const int ThreadCount = 4;
	enum RandomType
	{
		None,
		XorShift32,
		XorShift64,
		XorShift128,
		Mwc32,
		Mwc64,
		Standard,
		BadLcg,
		PopularLcg,
		MinStd,
	}
	enum TestType
	{
		Fill,
		Gorilla
	}

	RandomType _randomType;
	TestType _testType;
	Color32[][] _pixels;
	const int Width = 512;
	const int Height = 256;
	const int PixelsPerUnit = 100000;
	bool[][,] _boxes;
	double[][] _counts;
	int _collisionTestFrameCount;

	void Start()
	{
		_threadPool = new ThreadPool(ThreadCount);
		_randoms = new IRandom[ThreadCount];

#if false //UnityEngine.Randomを他のスレッドから呼んでみる
		TestUnityRandomFromOtherThread();
#endif

#if false // 速度計測
		Benchmark();
#endif

		_texture = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
		_image.texture = _texture;
		_image.rectTransform.sizeDelta = new Vector2(Width, Height);
		_pixels = new Color32[ThreadCount][];
		_testType = TestType.Fill;
		_testTypeText.text = _testType.ToString();
		SetRandomType(RandomType.XorShift32);
		for (int i = 0; i < ThreadCount; i++)
		{
			_pixels[i] = new Color32[Width * Height / ThreadCount];
		}
		Clear();
	}

	void OnDestroy()
	{
		_threadPool.Dispose();
	}

	System.Collections.IEnumerator CoGorillaTest()
	{
		// https://www.jstatsoft.org/article/view/v007i03/tuftests.pdf

		// まず必要な回数乱数を呼んで溜める
		const int Shortening = 0;
		const int stringLength = 26 - Shortening; // 生成する「文字列」のビット長
		const int idealMean = 24687971 >> Shortening; // 論文から。自分で計算していない(2^26/e)
		const int idealStandardDeviation = 4170 >> Shortening; // 論文から。自分で計算していない。
		const int bitsPerValue = 16;
		const int stringCount = 1 << stringLength; // 2^26通りの文字列が生成されうるので、そのうちどれだけ出てこないかを調べる。
		const int stringMask = stringCount - 1;
		// 乱数だけ先に別スレで呼んで並列化して高速化
		var randValueCount = stringCount + stringLength - 1;
		var values = new int[randValueCount];
		var unit = randValueCount / ThreadCount;
		var offset = 0;
		for (int i = 0; i < ThreadCount; i++)
		{
			int threadIndexCaptured = i;
			int countCaptured = (i == ThreadCount - 1) ? randValueCount : unit;
			int offsetCaptured = offset;
			_threadPool.AddJob(() =>
			{
				for (int j = 0; j < countCaptured; j++)
				{
					values[offsetCaptured + j] = _randoms[threadIndexCaptured].Next();
				}
			});
			randValueCount -= countCaptured;
			offset += countCaptured;
		}
		// 終了待ち
		while (!_threadPool.IsComplete())
		{
			yield return null;
		}

		// ビットごとに並列させる
		var counts = new int[bitsPerValue];
		int completeCount = 0;
		for (int bitIndex = 0; bitIndex < bitsPerValue; bitIndex++)
		{
			int bitIndexCaptured = bitIndex;
			_threadPool.AddJob(() =>
			{
				int str = 0;
				// プロローグ(文字列長-1ビット生成)
				for (int position = 0; position < (stringLength - 1); position++)
				{
					var value = values[position];
					str <<= 1;
					str |= (value >> bitIndexCaptured) & 0x1;
				}
				// 文字列生成ループ。生成された文字列にtrueをつけて回る
				var appearFlags = new bool[stringCount];
				for (int stringIndex = 0; stringIndex < stringCount; stringIndex++)
				{
					var value = values[stringIndex + stringLength - 1];
					str <<= 1; // 1ビット送って
					str |= (value >> bitIndexCaptured) & 0x1; // 新しいビットを足し
					str &= stringMask; // 範囲外を消す
					appearFlags[str] = true;
				}
				// 集計
				int count = 0;
				for (int stringIndex = 0; stringIndex < stringCount; stringIndex++)
				{
					if (appearFlags[stringIndex])
					{
						count++;
					}
				}
				lock (counts) // 配列の要素アクセスがメモリ壊すんじゃないかと心配なので同期
				{
					counts[bitIndexCaptured] = count;
					completeCount++;
					Debug.Log("Gorilla test ... " + completeCount + "/" + bitsPerValue);
				}
			});
		}

		// 終了待ち
		while (!_threadPool.IsComplete())
		{
			yield return null;
		}

		// 数えて色塗る
		int yCount = Height / 4;
		int xCount = Width / 4;
		for (int bitIndex = 0; bitIndex < bitsPerValue; bitIndex++)
		{
			int count = counts[bitIndex];
			int yStart = (bitIndex / 4) * yCount;
			int xStart = (bitIndex % 4) * xCount;
			count = stringCount - count; // 現れなかった数に変換
			float sdRatio = (float)(count - idealMean) / (float)idealStandardDeviation;
			Debug.Log("[gorilla test result] bitIndex: " + bitIndex + " count: " + count + "/" + stringCount + " sd: " + sdRatio);
			var color = new Color(0.5f, 0.5f, 0.5f, 1f);
			color.r += sdRatio / 8f;
			color.g -= sdRatio / 8f;
			for (int pixelY = 0; pixelY < yCount; pixelY++)
			{
				for (int pixelX = 0; pixelX < xCount; pixelX++)
				{
					_texture.SetPixel(pixelX + xStart, pixelY + yStart, color);
				}
			}
		}
		_texture.Apply();
	}

	void Fill(int index)
	{
		var pixels = _pixels[index];
		var DividedHeight = Height / ThreadCount;
		for (int i = 0; i < PixelsPerUnit; i++)
		{
			var x = _randoms[index].Next() % Width;
			var y = _randoms[index].Next() % DividedHeight;
			var color = _randoms[index].Next();
			var r = ((color >> 10) & 0x1f) << 3;
			var g = ((color >> 5) & 0x1f) << 3;
			var b = ((color >> 0) & 0x1f) << 3;
			var offset = (y * Width) + x;
			pixels[offset].r = (byte)r;
			pixels[offset].g = (byte)g;
			pixels[offset].b = (byte)b;
		}
	}

	void Clear()
	{
		for (int i = 0; i < ThreadCount; i++)
		{
			for (int j = 0; j < _pixels[i].Length; j++)
			{
				_pixels[i][j] = new Color32(0, 0, 0, 0xff);
			}
		}
		CopyToTexture();
	}

	public void OnRandomTypeButonClick()
	{
		Clear();
		// タイプ変更
		RandomType next = RandomType.None;
		switch (_randomType)
		{
			case RandomType.XorShift128: next = RandomType.XorShift64; break;
			case RandomType.XorShift64: next = RandomType.XorShift32; break;
			case RandomType.XorShift32: next = RandomType.Mwc32; break;
			case RandomType.Mwc32: next = RandomType.Mwc64; break;
			case RandomType.Mwc64: next = RandomType.Standard; break;
			case RandomType.Standard: next = RandomType.BadLcg; break;
			case RandomType.BadLcg: next = RandomType.PopularLcg; break;
			case RandomType.PopularLcg: next = RandomType.MinStd; break;
			case RandomType.MinStd: next = RandomType.XorShift128; break;
		}
		SetRandomType(next);
		if (_testType == TestType.Gorilla)
		{
			StartCoroutine(CoGorillaTest());
		}
	}

	void SetRandomType(RandomType next)
	{
		_randomType = next;
		_randomTypeText.text = next.ToString();
		for (int i = 0; i < ThreadCount; i++)
		{
			switch (_randomType)
			{
				case RandomType.XorShift128: _randoms[i] = new XorShift128(i); break;
				case RandomType.XorShift64: _randoms[i] = new XorShift64(i); break;
				case RandomType.XorShift32: _randoms[i] = new XorShift32(i); break;
				case RandomType.Mwc32: _randoms[i] = new Mwc32(i); break;
				case RandomType.Mwc64: _randoms[i] = new Mwc64(i); break;
				case RandomType.Standard: _randoms[i] = new Standard(i); break;
				case RandomType.BadLcg: _randoms[i] = new BadLcg(i); break;
				case RandomType.PopularLcg: _randoms[i] = new PopularLcg(i); break;
				case RandomType.MinStd: _randoms[i] = new MinStd(i); break;
			}
		}
	}

	public void OnTestTypeButonClick()
	{
		Clear();
		// タイプ変更
		switch (_testType)
		{
			case TestType.Fill: _testType = TestType.Gorilla; break;
			case TestType.Gorilla: _testType = TestType.Fill; break;
		}
		_testTypeText.text = _testType.ToString();
		if (_testType == TestType.Gorilla)
		{
			StartCoroutine(CoGorillaTest());
		}
	}

	void Update()
	{
		if (_testType == TestType.Fill)
		{
			for (int i = 0; i < ThreadCount; i++)
			{
				var tmpI = i;
				_threadPool.AddJob(() =>
				{
					Fill(tmpI);
				});
			}
			_threadPool.Wait();
			// ピクセル充填
			CopyToTexture();
		}
	}

	void CopyToTexture()
	{
		var blockHeight = Height / ThreadCount;
		for (int i = 0; i < ThreadCount; i++)
		{
			var yOffset = blockHeight * i;
			_texture.SetPixels32(0, yOffset, Width, blockHeight, _pixels[i]);
		}
		_texture.Apply();
	}

	void TestUnityRandomFromOtherThread()
	{
		int value = 0;
		var thread = new Thread(() =>
		{
			value = Random.Range(0, 100);
		});
		thread.Start();
		thread.Join();
		Debug.Log("Thread End! " + value);
	}

	interface IRandom
	{
		int Next(); // 16bitの乱数を返す(実用ではそんなに削る必要はないが、今回は合わせておいた)
	}

	class XorShift128 : IRandom //https://en.wikipedia.org/wiki/Xorshift
	{
		uint _x, _y, _z, _w;
		public XorShift128(int seed)
		{
			_x = 0xffff0000 | (uint)(seed & 0xffff);
			_y = _z = _w = 0;
		}
		public int Next()
		{
			var t = _w;
			t ^= t << 11;
			t ^= t >> 8;
			_w = _z;
			_z = _y;
			_y = _x;
			var s = _x;
			t ^= s;
			t ^= s >> 19;
			_x = t;
			return (int)(_x & 0xffff);
		}
	}

	class XorShift64 : IRandom
	{
		ulong _x;
		public XorShift64(int seed)
		{
			_x = 0xffff0000 | (uint)(seed & 0xffff);
		}
		public int Next() // numerical recipes
		{
			_x ^= _x << 21;
			_x ^= _x >> 35;
			_x ^= _x << 4;
			return (int)(_x & 0xffff);
		}
	}

	class XorShift32 : IRandom
	{
		uint _x;
		public XorShift32(int seed)
		{
			_x = 0xffff0000 | (uint)(seed & 0xffff);
		}
		public int Next()
		{
			_x ^= _x << 13;
			_x ^= _x >> 17;
			_x ^= _x << 5;
			return (int)(_x & 0xffff);
		}
	}

	class Mwc32 : IRandom
	{
		uint _x;
		public Mwc32(int seed)
		{
			_x = 0xffff0000 | (uint)(seed & 0xffff);
		}
		public int Next()
		{
			_x = ((_x & 0xffff) * 62904) + (_x >> 16); // Numerical Recipes.
			return (int)(_x & 0xffff);
		}
	}

	class Mwc64 : IRandom
	{
		ulong _x;
		public Mwc64(int seed)
		{
			_x = 0xffff0000 | (uint)(seed & 0xffff);
		}
		public int Next()
		{
			_x = ((_x & 0xffffffff) * 4294957665) + (_x >> 32); // Numerical Recipes.
			return (int)(_x & 0xffff);
		}
	}

	class Standard : IRandom
	{
		System.Random _rand;
		public Standard(int seed)
		{
			_rand = new System.Random(seed);
		}
		public int Next()
		{
			return _rand.Next() & 0xffff;
		}
	}

	class BadLcg : IRandom // 悪名高きRANDU。 https://en.wikipedia.org/wiki/RANDU
	{
		// こいつが流行ったのは、65539の乗算が(x << 16) + (x << 1) + xで書けて昔の機械で速かったためだろうか。
		uint _x;
		public BadLcg(int seed)
		{
			_x = 0x7ff80001 | ((uint)(seed & 0xffff) << 1); // 奇数を強制
		}
		public int Next()
		{
			_x = (65539 * _x) & 0x7fffffff;
			return (int)(_x & 0xffff);
		}
	}

	class PopularLcg : IRandom
	{
		uint _x;
		public PopularLcg(int seed)
		{
			_x = (uint)seed;
		}
		public int Next()
		{
			_x = (1664525 * _x) + 1013904223; // Numerical Recipes
			return (int)(_x & 0xffff);
		}
	}

	class MinStd : IRandom // https://en.wikipedia.org/wiki/Lehmer_random_number_generator
	{
		uint _x;
		public MinStd(int seed)
		{
			_x = 0xffff0000 | (uint)(seed & 0xffff);
		}
		public int Next()
		{
			ulong t = _x;
			t *= 48271;
			t %= 0x7fffffff;
			_x = (uint)(t & 0xffffffff);
			return (int)(_x & 0xffff);
		}
	}

	void Benchmark()
	{
		SetRandomType(RandomType.XorShift32);
		BenchmarkSub();

		SetRandomType(RandomType.XorShift64);
		BenchmarkSub();

		SetRandomType(RandomType.XorShift128);
		BenchmarkSub();

		SetRandomType(RandomType.Mwc32);
		BenchmarkSub();

		SetRandomType(RandomType.Mwc64);
		BenchmarkSub();

		SetRandomType(RandomType.Standard);
		BenchmarkSub();

		SetRandomType(RandomType.BadLcg);
		BenchmarkSub();

		SetRandomType(RandomType.PopularLcg);
		BenchmarkSub();

		SetRandomType(RandomType.MinStd);
		BenchmarkSub();
	}

	void BenchmarkSub()
	{
		const int N = 1000 * 1000 * 100; // 1億
		var t0 = Time.realtimeSinceStartup;
		int sum = 0; // 結果を何かに使わないと最適化で消されそうなので用意
		var rand = _randoms[0];
		for (int i = 0; i < N; i++)
		{
			sum += rand.Next();
		}
		var t1 = Time.realtimeSinceStartup;
		Debug.Log(_randomType.ToString() + " " + (t1 - t0) + " sum:" + sum);
	}
}
