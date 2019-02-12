using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Kayac
{
	public class PackedYuvImage : MaskableGraphic
	{
		[SerializeField]
		Texture2D _mainTexture;
		[SerializeField]
		bool _bilinearFilter;
		[SerializeField]
		bool _hasAlpha;

		Material _material;
		public override Texture mainTexture { get { return _mainTexture; } }
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
			bool separateAlpha = false;
			if (_bilinearFilter)
			{
				if (_hasAlpha)
				{
					shader = PackedYuvImageShaderHolder.alphaBilinear;
				}
				else
				{
					shader = PackedYuvImageShaderHolder.bilinear;
				}
			}
			else
			{
				if (_hasAlpha)
				{
					shader = PackedYuvImageShaderHolder.alphaPoint;
				}
				else
				{
					shader = PackedYuvImageShaderHolder.point;
				}
			}
			if (shader == null)
			{
				shader = PackedYuvImageShaderHolder.dummy;
			}
			if (shader == null)
			{
				Debug.LogError("YuvImageShaderHolder not exists. can't render IndexedRawImage.");
				return;
			}
			if ((_material == null) || (_material.shader.name != shader.name))
			{
				_material = new Material(shader);
			}
			SetTexturesToMaterial();
			if (separateAlpha)
			{
				_material.EnableKeyword("HAS_ALPHA");
			}
		}

		void OnTextureChange()
		{
			if (_material != null)
			{
				SetTexturesToMaterial();
			}
		}

		void SetTexturesToMaterial()
		{
			_material.mainTexture = _mainTexture;
		}

		public override void SetNativeSize()
		{
			if (_mainTexture == null)
			{
				Debug.LogError("Texture刺さってないのでできない");
				return;
			}
			int w = _mainTexture.width * 2;
			rectTransform.sizeDelta = new Vector2(w, _mainTexture.height);
		}

#if UNITY_EDITOR

		[CustomEditor(typeof(PackedYuvImage), true)]
		public class Inspector : Editor
		{
			public override void OnInspectorGUI()
			{
				var self = (PackedYuvImage)target;

				EditorGUILayout.BeginHorizontal();
				GUILayout.Label("MainTexture");
				var newMainTexture = (Texture2D)EditorGUILayout.ObjectField(self._mainTexture, typeof(Texture2D), false);
				if (newMainTexture != self._mainTexture) // カスタムエディタだと勝手にOnValidateが呼ばれない
				{
					self._mainTexture = newMainTexture;
					self.OnTextureChange();
					self.SetMaterialDirty();
				}
				EditorGUILayout.EndHorizontal();

				EditorGUILayout.BeginHorizontal();
				GUILayout.Label("BilinearFilter");
				var newBilinearFilter = GUILayout.Toggle(self._bilinearFilter, "");
				if (newBilinearFilter != self._bilinearFilter)
				{
					self._bilinearFilter = newBilinearFilter;
					self.CreateMaterial();
					self.SetMaterialDirty();
				}
				EditorGUILayout.EndHorizontal();

				EditorGUILayout.BeginHorizontal();
				GUILayout.Label("HasAlpha");
				var newHasAlpha = GUILayout.Toggle(self._hasAlpha, "");
				if (newHasAlpha != self._hasAlpha)
				{
					self._hasAlpha = newHasAlpha;
					self.CreateMaterial();
					self.SetMaterialDirty();
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