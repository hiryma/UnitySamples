using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Kayac
{
	// 型指定しないobject版は用意しない
	public abstract class CoRetVal
	{
		public bool isDone{ get; protected set; }
		public System.Exception exception{ get; private set; }
		public void Fail(System.Exception exception = null)
		{
			this.isDone = true;
			this.exception = exception;
		}
	}

	public class CoRetVal<T> : CoRetVal
	{
		public T value{ get; private set; }
		public void Succeed(T value)
		{
			this.value = value;
			this.isDone = true;
		}
	}
}
