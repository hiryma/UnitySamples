using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Kayac
{
	public class GrbImage : MaskableGraphic
	{
		[SerializeField]
		Texture2D _mainTexture;
		[SerializeField]
		Texture2D _rbTexture;
		[SerializeField]
		Shader _shader;

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
			if ((_material == null) || (_material.shader.name != _shader.name))
			{
				_material = new Material(_shader);
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
			if (_rbTexture != null)
			{
				_material.SetTexture("_RbTex", _rbTexture);
			}
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

		[CustomEditor(typeof(GrbImage), true)]
		public class Inspector : Editor
		{
			public override void OnInspectorGUI()
			{
				var self = (GrbImage)target;

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
				GUILayout.Label("rbTexture");
				var newRbTexture = (Texture2D)EditorGUILayout.ObjectField(self._rbTexture, typeof(Texture2D), false);
				if (newRbTexture != self._rbTexture) // カスタムエディタだと勝手にOnValidateが呼ばれない
				{
					self._rbTexture = newRbTexture;
					self.OnTextureChange();
					self.SetMaterialDirty();
				}
				EditorGUILayout.EndHorizontal();

				EditorGUILayout.BeginHorizontal();
				GUILayout.Label("shader");
				var newShader = (Shader)EditorGUILayout.ObjectField(self._shader, typeof(Shader), false);
				if (newShader != self._shader) // カスタムエディタだと勝手にOnValidateが呼ばれない
				{
					self._shader = newShader;
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