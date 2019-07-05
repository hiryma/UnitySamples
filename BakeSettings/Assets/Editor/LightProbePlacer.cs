using UnityEngine;
using UnityEditor;

namespace Kayac
{
	public class LightProbePlacer : EditorWindow
	{
		[MenuItem("Kayac/Rendering/PlaceLightProbes")]
		static void Create()
		{
			GetWindow<LightProbePlacer>(typeof(LightProbePlacer).Name);
		}

		Vector3 min, max;
		Vector3Int div;

		void OnGUI()
		{
			min = EditorGUILayout.Vector3Field("Min", min);
			max = EditorGUILayout.Vector3Field("Max", max);
			div = EditorGUILayout.Vector3IntField("Div", div);
			if (GUILayout.Button("Generate"))
			{
				Generate();
			}
		}

		void Generate()
		{
			var go = new GameObject("LightProbeGroup");
			var group = go.AddComponent<LightProbeGroup>();
			div.x = Mathf.Max(div.x, 1);
			div.y = Mathf.Max(div.y, 1);
			div.z = Mathf.Max(div.z, 1);

			var p = new Vector3[(div.x + 1) * (div.y + 1) * (div.z + 1)];
			var x = min.x;
			var xStep = (max.x - min.x) / (float)div.x;
			var yStep = (max.y - min.y) / (float)div.y;
			var zStep = (max.z - min.z) / (float)div.z;
			Debug.Log("LightProbe Step:" + xStep + ", " + yStep + ", " + zStep);
			var i = 0;
			for (int xi = 0; xi <= div.x; xi++)
			{
				var y = min.y;
				for (int yi = 0; yi <= div.y; yi++)
				{
					var z = min.z;
					for (int zi = 0; zi <= div.z; zi++)
					{
						p[i] = new Vector3(x, y, z);
						i++;
						z += zStep;
					}
					y += yStep;
				}
				x += xStep;
			}
			group.probePositions = p;
		}
	}
}