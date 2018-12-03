using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AfterEffectsToUnity
{
	public class CallbackTimelinePlayer
	{
		private CallbackTimeline _timeline;
		private float _prevFrame;
		private float _frameRate;
		private bool _timelineIsMine;

		public CallbackTimelinePlayer(CallbackTimeline timeline, float frameRate = 30f)
		{
			Debug.Assert(timeline != null);
			_timeline = timeline;
			_frameRate = frameRate;
		}

		public CallbackTimelinePlayer(bool loop = false, float frameRate = 30f)
		{
			_timeline = new CallbackTimeline(loop);
			_frameRate = frameRate;
			_timelineIsMine = true;
		}

		public void Dispose()
		{
			_timeline = null;
		}

		public bool IsDisposed()
		{
			return _timeline == null;
		}

		public CallbackTimelinePlayer Add(int frame, Action callback)
		{
			Debug.Assert(_timelineIsMine);
			if (_timelineIsMine)
			{
				_timeline.Add(frame, callback);
			}
			return this;
		}

		public void SetFrame(float frame)
		{
			_prevFrame = frame;
		}

		public float frame
		{
			get
			{
				return _prevFrame;
			}
		}

		public void UpdatePerFrame(float deltaTime)
		{
			float currentFrame = _prevFrame + (deltaTime * _frameRate);
			_timeline.Execute(_prevFrame, currentFrame);
			_prevFrame = currentFrame;
		}
	}
}
