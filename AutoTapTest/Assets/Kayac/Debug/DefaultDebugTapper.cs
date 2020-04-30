using UnityEngine;

namespace Kayac
{
	public class DefaultDebugTapper : DebugTapper
	{
		public new void ManualStart(
			int tapCount,
			Sprite markSprite,
			int logSize = DebugTapper.DefaultLogSize)
		{
			base.ManualStart(tapCount, markSprite, logSize);
		}

		protected override void UpdateTap(int tapIndex)
		{
			const float durationMedian = 0.1f;
			const float durationLog10Sigma = 0.5f; // 3SDで1.5==3.3秒
			const float distanceMedian = 0.01f;
			const float distanceLog10Sigma = 2; // 上下100倍
			var fromPosition = new Vector2(
				Random.Range(0f, (float)Screen.width),
				Random.Range(0f, (float)Screen.height));
			var distanceLog = NormalDistributionRandom();
			distanceLog *= distanceLog10Sigma;
			var distance = Mathf.Pow(10f, distanceLog) * distanceMedian * Mathf.Max(Screen.width, Screen.height);
			var rad = Mathf.PI * 2f * Random.value;
			var v = new Vector2(
				Mathf.Cos(rad) * distance,
				Mathf.Sin(rad) * distance);
			var toPosition = fromPosition + v;
			var durationLog = NormalDistributionRandom();
			durationLog *= durationLog10Sigma;
			var duration = Mathf.Pow(10f, durationLog) * durationMedian;
			Fire(tapIndex, fromPosition, toPosition, duration);
		}

		protected override bool ToBeIgnored(GameObject gameObject)
		{
			var transform = gameObject.transform;
			while (transform != null)
			{
				if (transform.gameObject.name.Contains("DebugUi"))
				{
					return true;
				}
				transform = transform.parent;
			}
			return false;
		}

		// private -----------------

		// 0中心の正規分布を返す Box-Muller法 https://ja.wikipedia.org/wiki/%E3%83%9C%E3%83%83%E3%82%AF%E3%82%B9%EF%BC%9D%E3%83%9F%E3%83%A5%E3%83%A9%E3%83%BC%E6%B3%95
		float normalDistributionCosine = float.MaxValue; // 無駄になってもいいから、捨てていいなら面倒なので捨てるのだが...
		float NormalDistributionRandom()
		{
			float ret;
			if (normalDistributionCosine != float.MaxValue) // 前に作った奴が残っていれば返す
			{
				ret = normalDistributionCosine;
				normalDistributionCosine = float.MaxValue;
			}
			else
			{
				var x = Random.value;
				var y = Random.value;
				var t0 = Mathf.Sqrt(-2f * Mathf.Log(x));
				var t1 = Mathf.PI * 2f * y;
				normalDistributionCosine = t0 * Mathf.Cos(t1);
				ret = t0 * Mathf.Sin(t1);
			}
			return ret;
		}
	}
}