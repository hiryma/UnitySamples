using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Kayac
{
	public class AzimuthalEquidistantProjector : MonoBehaviour
	{
		[SerializeField]
		Shader _shaderFragmentVersion;
		[SerializeField]
		Shader _shaderVertexVersion;
		[SerializeField]
		Camera _camera;
		[SerializeField]
		float _outputFieldOfViewY = 0f;
		[SerializeField]
		bool _useFragmentCalculation;
		[SerializeField]
		int _meshDivision = 48;
		[SerializeField]
		bool _viewportDebugEnabled;

		Material _material;
		bool _viewportDebugEnabledApplied;
		Mesh _mesh;
		bool _currentMaterialIsFragmentVersion;

		void TrySetup()
		{
			if ((_material == null) ||
				(_currentMaterialIsFragmentVersion != _useFragmentCalculation))
			{
				if (_useFragmentCalculation)
				{
					_material = new Material(_shaderFragmentVersion);
				}
				else
				{
					_material = new Material(_shaderVertexVersion);
					TryCreateMesh();
				}
				_viewportDebugEnabledApplied = false;
				_currentMaterialIsFragmentVersion = _useFragmentCalculation;
			}
			SetViewportDebugEnable(_viewportDebugEnabled);
		}

		void TryCreateMesh()
		{
			if (_meshDivision <= 0)
			{
				return;
			}
			var vertexCount = (_meshDivision + 1) * (_meshDivision + 1);
			if (_mesh == null)
			{
				_mesh = new Mesh();
			}
			else if (_mesh.vertexCount != vertexCount)
			{
				_mesh.Clear();
			}
			else
			{
				return; // やることない
			}
			var vertices = new Vector3[vertexCount];
			int i = 0;
			for (int yi = 0; yi <= _meshDivision; yi++)
			{
				float y = (float)((yi * 2) - _meshDivision) / (float)_meshDivision;
				for (int xi = 0; xi <= _meshDivision; xi++)
				{
					float x = (float)((xi * 2) - _meshDivision) / (float)_meshDivision;
					vertices[i] = new Vector3(x, y, 0f);
					i++;
				}
			}
			var indices = new int[_meshDivision * _meshDivision * 6];
			int indexPosition = 0;
			for (int yi = 0; yi < _meshDivision; yi++)
			{
				int vOffsetY0 = yi * (_meshDivision + 1);
				int vOffsetY1 = (yi + 1) * (_meshDivision + 1);
				for (int xi = 0; xi < _meshDivision; xi++)
				{
					indices[indexPosition + 0] = vOffsetY0 + xi + 0;
					indices[indexPosition + 1] = vOffsetY0 + xi + 1;
					indices[indexPosition + 2] = vOffsetY1 + xi + 0;
					indices[indexPosition + 3] = vOffsetY1 + xi + 0;
					indices[indexPosition + 4] = vOffsetY0 + xi + 1;
					indices[indexPosition + 5] = vOffsetY1 + xi + 1;
					indexPosition += 6;
				}
			}
			_mesh.vertices = vertices;
			_mesh.triangles = indices;
		}

		public void SetViewportDebugEnable(bool value)
		{
			if (value != _viewportDebugEnabledApplied)
			{
				if (_material != null)
				{
					if (value)
					{
						_material.EnableKeyword("VIEWPORT_DEBUG");
					}
					else
					{
						_material.DisableKeyword("VIEWPORT_DEBUG");
					}
				}
				_viewportDebugEnabled = value;
				_viewportDebugEnabledApplied = value;
			}
		}

		public bool viewportDebugEnabled { get { return _viewportDebugEnabledApplied; } }

		float CalcMaxOutputFieldOfViewYRadian()
		{
			float aspect = _camera.aspect;
			float diagFactor = Mathf.Sqrt(1f + (aspect * aspect));
			float diag = diagFactor * Mathf.Tan(_camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
			float thetaDiag = Mathf.Atan(diag);
			return thetaDiag / diagFactor;
		}

		void OnRenderImage(RenderTexture source, RenderTexture destination)
		{
			TrySetup();
			float outputHalfFieldOfViewYRadian = 0f;
			if (_outputFieldOfViewY <= 0f)
			{
				outputHalfFieldOfViewYRadian = CalcMaxOutputFieldOfViewYRadian();
			}
			else
			{
				outputHalfFieldOfViewYRadian = _outputFieldOfViewY * Mathf.Deg2Rad * 0.5f;
			}

			var inputHalfFieldOfViewYRadian = _camera.fieldOfView * 0.5f * Mathf.Deg2Rad;
			var tanHalfFovY = Mathf.Tan(inputHalfFieldOfViewYRadian);
			_material.SetFloat("_TanSrcHalfFovY", tanHalfFovY);
			_material.SetFloat("_DstHalfFovY", outputHalfFieldOfViewYRadian);
			if (_useFragmentCalculation)
			{
				Graphics.Blit(source, destination, _material);
			}
			else
			{
				TryCreateMesh();
				_material.SetPass(0);
				_material.SetTexture("_MainTex", source);
				Graphics.SetRenderTarget(destination);
				GL.Clear(false, true, new Color(0f, 0f, 0f, 0f));
				Graphics.DrawMeshNow(_mesh, Vector3.zero, Quaternion.identity);
			}
		}
#if UNITY_EDITOR
		void OnValidate()
		{
			TrySetup();
			SetViewportDebugEnable(_viewportDebugEnabled);
		}

		[UnityEditor.CustomEditor(typeof(AzimuthalEquidistantProjector), true)]
		class Editor : UnityEditor.Editor
		{
			public override void OnInspectorGUI()
			{
				base.OnInspectorGUI();
				var self = (AzimuthalEquidistantProjector)target;
				if (self._outputFieldOfViewY <= 0f)
				{
					GUILayout.Label("Auto OutputFieldOfViewY: " + self.CalcMaxOutputFieldOfViewYRadian() * 2f * Mathf.Rad2Deg);
				}
			}
		}
#endif
	}
}