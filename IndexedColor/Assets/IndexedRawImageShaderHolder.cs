using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Kayac
{
	public class IndexedRawImageShaderHolder : MonoBehaviour
	{
		[SerializeField]
		Shader _dummy;
		[SerializeField]
		Shader _indexed256;
		[SerializeField]
		Shader _indexed256Bilinear;
		[SerializeField]
		Shader _indexed16;
		[SerializeField]
		Shader _indexed16Bilinear;

		static IndexedRawImageShaderHolder _instance;
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
					return Shader.Find("UI/IndexedDummy");
				}
			}
		}

		public static Shader indexed256
		{
			get
			{
				if (_instance != null)
				{
					return _instance._indexed256;
				}
				else
				{
					return Shader.Find("UI/Indexed256");
				}
			}
		}

		public static Shader indexed256Bilinear
		{
			get
			{
				if (_instance != null)
				{
					return _instance._indexed256Bilinear;
				}
				else
				{
					return Shader.Find("UI/Indexed256Bilinear");
				}
			}
		}

		public static Shader indexed16
		{
			get
			{
				if (_instance != null)
				{
					return _instance._indexed16;
				}
				else
				{
					return Shader.Find("UI/Indexed16");
				}
			}
		}

		public static Shader indexed16Bilinear
		{
			get
			{
				if (_instance != null)
				{
					return _instance._indexed16Bilinear;
				}
				else
				{
					return Shader.Find("UI/Indexed16Bilinear");
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