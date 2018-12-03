using UnityEngine;

namespace Kayac
{
	public class FrameTimeGauge : DebugUiDualGauge
	{
		private FrameTimeWatcher _watcher;
		private bool _watcherIsMine;

		public FrameTimeGauge(
			float width,
			float height,
			FrameTimeWatcher watcher) : base(0f, 66.667f, width, height, asInteger: true)
		{
			if (watcher == null)
			{
				_watcher = new FrameTimeWatcher();
				_watcherIsMine = true;
			}
			else
			{
				_watcher = watcher;
			}
		}

		protected override void OnEnable()
		{
			if (_watcherIsMine)
			{
				_watcher.Reset();
			}
		}

		public override void Update()
		{
			base.Update();
			if (_watcherIsMine)
			{
				_watcher.Update();
			}
			primaryValue = _watcher.averageFrameTime * 0.001f;
			secondaryValue = _watcher.maxFrameTime * 0.001f;
		}
	}
}