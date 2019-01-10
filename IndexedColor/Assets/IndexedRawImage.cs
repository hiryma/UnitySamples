using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Kayac
{
	public class IndexedRawImage : MaskableGraphic
	{
		[SerializeField]
		Texture2D _indexTexture;
		[SerializeField]
		Texture2D _tableTexture;
		[SerializeField]
		bool _bilinearFilter;

		Material _material;
		public override Texture mainTexture { get { return _indexTexture; } }
		public override Material material
		{
			get
			{
				if (_material == null)
				{
					CreateMaterial();
				}
				return _material;
			}
		}

		void CreateMaterial()
		{
			Shader shader = null;
			if (_tableTexture != null)
			{
				switch (_tableTexture.width)
				{
					case 256:
						if (_bilinearFilter)
						{
							shader = IndexedRawImageShaderHolder.indexed256Bilinear;
						}
						else
						{
							shader = IndexedRawImageShaderHolder.indexed256;
						}
						break;
					case 16:
						if (_bilinearFilter)
						{
							shader = IndexedRawImageShaderHolder.indexed16Bilinear;
						}
						else
						{
							shader = IndexedRawImageShaderHolder.indexed16;
						}
						break;
				}
			}
			if (shader == null)
			{
				shader = IndexedRawImageShaderHolder.dummy;
			}
			if (shader == null)
			{
				Debug.LogError("IndexedRawImageShaderHolder not exists. can't render IndexedRawImage.");
				return;
			}
			if ((_material == null) || (_material.shader.name != shader.name))
			{
				_material = new Material(shader);
			}
			SetTexturesToMaterial();
		}

		void OnTextureChange()
		{
			if (_material != null)
			{
				SetTexturesToMaterial();
				SetMaterialDirty();
			}
		}

		void SetTexturesToMaterial()
		{
			_material.mainTexture = _indexTexture;
			_material.SetTexture("_TableTex", _tableTexture);
		}

		public override void SetNativeSize()
		{
			if (_indexTexture == null)
			{
				Debug.LogError("indexTexture刺さってないのでできない");
				return;
			}
			if (this.material.shader.name.Contains("UI_Indexed16")) // 16色なら幅2倍が本当の幅
			{
				rectTransform.sizeDelta = new Vector2(_indexTexture.width * 2, _indexTexture.height); // 幅2倍(TODO: 元が奇数だと1px大きくなる!!)
			}
			else
			{
				rectTransform.sizeDelta = new Vector2(_indexTexture.width, _indexTexture.height);
			}
		}

#if UNITY_EDITOR

		[CustomEditor(typeof(IndexedRawImage), true)]
		public class Inspector : Editor
		{
			public override void OnInspectorGUI()
			{
				var self = (IndexedRawImage)target;

				EditorGUILayout.BeginHorizontal();
				GUILayout.Label("IndexTexture");
				var newIndexTexture = (Texture2D)EditorGUILayout.ObjectField(self._indexTexture, typeof(Texture2D), false);
				if (newIndexTexture != self._indexTexture) // カスタムエディタだと勝手にOnValidateが呼ばれない
				{
					self._indexTexture = newIndexTexture;
					self.OnTextureChange();
				}
				EditorGUILayout.EndHorizontal();

				EditorGUILayout.BeginHorizontal();
				GUILayout.Label("TableTexture");
				var newTableTexture = (Texture2D)EditorGUILayout.ObjectField(self._tableTexture, typeof(Texture2D), false);
				if (newTableTexture != self._tableTexture)
				{
					self._tableTexture = newTableTexture;
					self.CreateMaterial();
				}
				EditorGUILayout.EndHorizontal();

				EditorGUILayout.BeginHorizontal();
				GUILayout.Label("BilinearFilter");
				var newBilinearFilter = GUILayout.Toggle(self._bilinearFilter, "");
				if (newBilinearFilter != self._bilinearFilter)
				{
					self._bilinearFilter = newBilinearFilter;
					self.CreateMaterial();
				}
				EditorGUILayout.EndHorizontal();

				EditorGUILayout.BeginHorizontal();
				GUILayout.Label("RaycastTarget");
				self.raycastTarget = GUILayout.Toggle(self.raycastTarget, "");
				EditorGUILayout.EndHorizontal();

				if (GUILayout.Button("Set Native Size"))
				{
					self.SetNativeSize();
				}
			}
		}
#endif
	}
}