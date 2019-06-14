using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Kayac
{
	// 型指定しないobject版はabstractにする。型安全を優先。
	public abstract class CoroutineReturnValue
	{
		public bool IsDone{ get; protected set; }
		public System.Exception Exception{ get; private set; }
		public void Fail(System.Exception exception = null)
		{
			IsDone = true;
			Exception = exception;
		}
	}

	public class CoroutineReturnValue<T> : CoroutineReturnValue
	{
		public T Value{ get; private set; }
		public void Succeed(T value)
		{
			Value = value;
			IsDone = true;
		}
	}
}
