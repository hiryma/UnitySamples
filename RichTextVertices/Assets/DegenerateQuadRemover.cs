using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Kayac
{
	public class DegenerateQuadRemover : BaseMeshEffect
	{
		private static List<UIVertex> _verticesCache; // 使い回し

#if UNITY_EDITOR
		private int _originalVertexCount;
		private int _processedVertexCount;
#endif
		static DegenerateQuadRemover()
		{
			_verticesCache = new List<UIVertex>();
		}

		public static void Process(List<UIVertex> vertices)
		{
			int letterCount = vertices.Count / 6;
			Debug.Assert((letterCount * 6) == vertices.Count); // 6で割れない頂点数であれば前提が崩れている
			int srcVertexIndex = 0;
			int dstVertexIndex = 0;
			for (int letterIndex = 0; letterIndex < letterCount; letterIndex++)
			{
				vertices[dstVertexIndex + 0] = vertices[srcVertexIndex + 0];
				vertices[dstVertexIndex + 1] = vertices[srcVertexIndex + 1];
				vertices[dstVertexIndex + 2] = vertices[srcVertexIndex + 2];
				vertices[dstVertexIndex + 3] = vertices[srcVertexIndex + 3];
				vertices[dstVertexIndex + 4] = vertices[srcVertexIndex + 4];
				vertices[dstVertexIndex + 5] = vertices[srcVertexIndex + 5];
				var d01 = vertices[dstVertexIndex + 0].position - vertices[dstVertexIndex + 1].position;
				if (d01.sqrMagnitude > 0f)
				{
					dstVertexIndex += 6;
				}
				srcVertexIndex += 6;
			}
			vertices.RemoveRange(dstVertexIndex, vertices.Count - dstVertexIndex);
			Debug.Assert(vertices.Count == dstVertexIndex);
		}

		public override void ModifyMesh(VertexHelper vh)
		{
			if (!enabled)
			{
				return;
			}
			vh.GetUIVertexStream(_verticesCache);
#if UNITY_EDITOR
			_originalVertexCount = _verticesCache.Count;
#endif
			Process(_verticesCache);
#if UNITY_EDITOR
			_processedVertexCount = _verticesCache.Count;
#endif
			vh.Clear();
			vh.AddUIVertexTriangleStream(_verticesCache);
		}

#if UNITY_EDITOR
		[UnityEditor.CustomEditor(typeof(DegenerateQuadRemover), true)]
		public class DegenerateQuadRemoverInspector : UnityEditor.Editor
		{
			public override void OnInspectorGUI()
			{
				base.OnInspectorGUI();
				var self = (DegenerateQuadRemover)target;
				var text = string.Format("頂点数: {0} -> {1}", self._originalVertexCount, self._processedVertexCount);
				UnityEditor.EditorGUILayout.LabelField(text);
			}
		}
#endif
	}
}
