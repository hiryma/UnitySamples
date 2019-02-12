using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Kayac
{
	public class YuvImageShaderHolder : MonoBehaviour
	{
		[SerializeField]
		Shader _dummy;
		[SerializeField]
		Shader _yuv;

		static YuvImageShaderHolder _instance;
		public static Shader dummy
		{
			get
			{
				if (_instance != null)
				{
					return _instance._dummy;
				}
				else
				{
					return Shader.Find("UI/YuvDummy");
				}
			}
		}

		public static Shader yuv
		{
			get
			{
				if (_instance != null)
				{
					return _instance._yuv;
				}
				else
				{
					return Shader.Find("UI/Yuv");
				}
			}
		}

		void Awake()
		{
			// 2個目がAwakeされても無視
			if (_instance == null)
			{
				_instance = this;
			}
		}
	}
}