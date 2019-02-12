using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Kayac
{
	public class YuvImage : MaskableGraphic
	{
		[SerializeField]
		Texture2D _mainTexture;
		[SerializeField]
		Texture2D _uvTexture;
		[SerializeField]
		Texture2D _alphaTexture;
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
			if (_uvTexture != null)
			{
				shader = YuvImageShaderHolder.yuv;
				if (_alphaTexture != null)
				{
					separateAlpha = true;
				}
			}
			if (shader == null)
			{
				shader = YuvImageShaderHolder.dummy;
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
			if (_uvTexture != null)
			{
				_material.SetTexture("_UvTex", _uvTexture);
			}
			if (_alphaTexture != null)
			{
				_material.SetTexture("_AlphaTex", _alphaTexture);
			}
		}

		public override void SetNativeSize()
		{
			if (_mainTexture == null)
			{
				Debug.LogError("Texture刺さってないのでできない");
				return;
			}
			int w;
			if (_uvTexture != null)
			{
				w = _mainTexture.width;
			}
			else
			{
				w = _mainTexture.width * 2;
			}
			rectTransform.sizeDelta = new Vector2(w, _mainTexture.height);
		}

#if UNITY_EDITOR

		[CustomEditor(typeof(YuvImage), true)]
		public class Inspector : Editor
		{
			public override void OnInspectorGUI()
			{
				var self = (YuvImage)target;

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
				GUILayout.Label("UvTexture");
				var newUvTexture = (Texture2D)EditorGUILayout.ObjectField(self._uvTexture, typeof(Texture2D), false);
				if (newUvTexture != self._uvTexture) // カスタムエディタだと勝手にOnValidateが呼ばれない
				{
					self._uvTexture = newUvTexture;
					self.CreateMaterial();
					self.SetMaterialDirty();
				}
				EditorGUILayout.EndHorizontal();

				EditorGUILayout.BeginHorizontal();
				GUILayout.Label("AlphaTexture");
				var newAlphaTexture = (Texture2D)EditorGUILayout.ObjectField(self._alphaTexture, typeof(Texture2D), false);
				if (newAlphaTexture != self._alphaTexture) // カスタムエディタだと勝手にOnValidateが呼ばれない
				{
					self._alphaTexture = newAlphaTexture;
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