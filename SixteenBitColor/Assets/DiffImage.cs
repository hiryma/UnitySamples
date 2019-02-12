using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Kayac
{
	public class DiffImage : MaskableGraphic
	{
		[SerializeField]
		Texture2D _mainTexture;
		[SerializeField]
		Texture2D _refTexture;
		[SerializeField]
		Shader _shader;
		[SerializeField]
		float _scale = 32f;
		[SerializeField]
		float _blend = -1f;

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
			Shader shader = _shader;
			if (shader == null)
			{
				shader = Shader.Find("UI/Diff");
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
			}
		}

		void SetTexturesToMaterial()
		{
			_material.mainTexture = _mainTexture;
			_material.SetTexture("_RefTex", _refTexture);
			_material.SetFloat("_Scale", _scale);
			_material.SetFloat("_Blend", _blend);
		}

		public override void SetNativeSize()
		{
			if (_mainTexture == null)
			{
				Debug.LogError("Texture刺さってないのでできない");
				return;
			}
			rectTransform.sizeDelta = new Vector2(_mainTexture.width, _mainTexture.height);
		}

#if UNITY_EDITOR

		[CustomEditor(typeof(DiffImage), true)]
		public class Inspector : Editor
		{
			bool _blendEnabled = false;
			float _blend = -1f;

			public override void OnInspectorGUI()
			{
				var self = (DiffImage)target;

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
				GUILayout.Label("RefTexture");
				var newUvTexture = (Texture2D)EditorGUILayout.ObjectField(self._refTexture, typeof(Texture2D), false);
				if (newUvTexture != self._refTexture) // カスタムエディタだと勝手にOnValidateが呼ばれない
				{
					self._refTexture = newUvTexture;
					self.OnTextureChange();
					self.SetMaterialDirty();
				}
				EditorGUILayout.EndHorizontal();

				EditorGUILayout.BeginHorizontal();
				GUILayout.Label("Shader");
				var newShader = (Shader)EditorGUILayout.ObjectField(self._shader, typeof(Shader), false);
				if (newShader != self._shader) // カスタムエディタだと勝手にOnValidateが呼ばれない
				{
					self._shader = newShader;
					self.CreateMaterial();
					self.SetMaterialDirty();
				}
				EditorGUILayout.EndHorizontal();

				EditorGUILayout.BeginHorizontal();
				GUILayout.Label("Scale");
				var newScale = EditorGUILayout.FloatField(self._scale);
				if (newScale != self._scale) // カスタムエディタだと勝手にOnValidateが呼ばれない
				{
					self._scale = newScale;
					self.OnTextureChange();
					self.SetMaterialDirty();
				}
				EditorGUILayout.EndHorizontal();

				EditorGUILayout.BeginHorizontal();
				GUILayout.Label("Blend");
				var newBlendEnabled = EditorGUILayout.Toggle(_blendEnabled);
				var newBlend = EditorGUILayout.Slider(_blend, 0f, 1f);
				if ((newBlendEnabled != _blendEnabled) || (newBlend != _blend))
				{
					_blendEnabled = newBlendEnabled;
					_blend = newBlend;
					var blend = -1f;
					if (_blendEnabled)
					{
						blend = _blend;
					}
					self._blend = blend;
					self.OnTextureChange();
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