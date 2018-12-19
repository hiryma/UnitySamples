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
		private int _inVertexCount;
		private int _outVertexCount;
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
				var p0 = vertices[dstVertexIndex + 0].position;
				var p1 = vertices[dstVertexIndex + 1].position;
				var dx = p1.x - p0.x;
				var dy = p1.y - p0.y;
				if (((dx * dx) + (dy * dy)) > 0f)
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
#if UNITY_EDITOR
				_inVertexCount = _outVertexCount = 0;
#endif
				return;
			}
			vh.GetUIVertexStream(_verticesCache);
#if UNITY_EDITOR
			_inVertexCount = _verticesCache.Count;
#endif
			Process(_verticesCache);
#if UNITY_EDITOR
			_outVertexCount = _verticesCache.Count;
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
				var text = string.Format("頂点数: {0} -> {1}", self._inVertexCount, self._outVertexCount);
				UnityEditor.EditorGUILayout.LabelField(text);
			}
		}
#endif
	}
}
