using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Kayac.LoaderImpl
{
	public class LoadHandleHolder : MonoBehaviour
	{
		List<LoadHandle> _handles;

		public int handleCount
		{
			get
			{
				return (_handles == null) ? 0 : _handles.Count;
			}
		}

		public void Add(LoadHandle handle)
		{
			if (_handles == null)
			{
				_handles = new List<LoadHandle>();
			}
			_handles.Add(handle);
		}
	}
}