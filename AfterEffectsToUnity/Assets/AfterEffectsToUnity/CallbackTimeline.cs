using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AfterEffectsToUnity
{
	public class CallbackTimeline : EventTimeline<Action>
	{
		public CallbackTimeline(bool loop = false) : base(loop)
		{
		}

		public new CallbackTimeline Add(int frame, Action callback)
		{
			base.Add(frame, callback);
			return this;
		}

		public void Execute(float prevFrame, float currentFrame)
		{
			foreach (var callback in EnumerateFired(prevFrame, currentFrame))
			{
				if (callback != null)
				{
					callback();
				}
			}
		}
	}
}
