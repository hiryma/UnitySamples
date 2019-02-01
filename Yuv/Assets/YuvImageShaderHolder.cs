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
		Shader _bilinear;
		[SerializeField]
		Shader _point;
		[SerializeField]
		Shader _alphaBilinear;
		[SerializeField]
		Shader _alphaPoint;
		[SerializeField]
		Shader _separate;

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

		public static Shader point
		{
			get
			{
				if (_instance != null)
				{
					return _instance._point;
				}
				else
				{
					return Shader.Find("UI/YuvPoint");
				}
			}
		}

		public static Shader bilinear
		{
			get
			{
				if (_instance != null)
				{
					return _instance._bilinear;
				}
				else
				{
					return Shader.Find("UI/YuvBilinear");
				}
			}
		}

		public static Shader alphaPoint
		{
			get
			{
				if (_instance != null)
				{
					return _instance._alphaPoint;
				}
				else
				{
					return Shader.Find("UI/YuvAlphaPoint");
				}
			}
		}

		public static Shader alphaBilinear
		{
			get
			{
				if (_instance != null)
				{
					return _instance._alphaBilinear;
				}
				else
				{
					return Shader.Find("UI/YuvAlphaBilinear");
				}
			}
		}

		public static Shader separate
		{
			get
			{
				if (_instance != null)
				{
					return _instance._separate;
				}
				else
				{
					return Shader.Find("UI/YuvSeparate");
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