using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Kayac
{
	public class ProceduralMesh : MonoBehaviour
	{
		[SerializeField] MeshFilter meshFilter;
		[SerializeField] MeshAsset meshAsset;

		public MeshFilter MeshFilter { get { return meshFilter; } }
		public MeshAsset MeshAsset { get { return meshAsset; } }

		void Start()
		{
			LoadAsset();
		}

		public void SaveAsset()
		{
			meshAsset.Set(meshFilter.sharedMesh);
		}

		public void LoadAsset()
		{
			meshFilter.sharedMesh = meshAsset.GenerateMesh();
		}

		[CustomEditor(typeof(ProceduralMesh))]
		class MyEditor : Editor
		{
			public override void OnInspectorGUI()
			{
				var self = target as ProceduralMesh;
				base.OnInspectorGUI();
				GUILayout.Space(8);
				if (GUILayout.Button("Save"))
				{
					self.SaveAsset();
				}
				if (GUILayout.Button("Load"))
				{
					self.LoadAsset();
				}
			}
		}
	}
}