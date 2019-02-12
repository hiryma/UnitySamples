using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Kayac
{
	public class PackedYuvImageShaderHolder : MonoBehaviour
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

		static PackedYuvImageShaderHolder _instance;
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
					return Shader.Find("UI/PackedYuvDummy");
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
					return Shader.Find("UI/PackedYuvPoint");
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
					return Shader.Find("UI/PackedYuvBilinear");
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
					return Shader.Find("UI/PackedYuvAlphaPoint");
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
					return Shader.Find("UI/PackedYuvAlphaBilinear");
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