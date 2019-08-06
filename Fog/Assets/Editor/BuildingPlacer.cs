using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

public class BuildingPlacer
{
	[MenuItem("Sample/PlaceBuildings")]
	public static void Execute()
	{
		var guids = AssetDatabase.FindAssets("Building");
		var path = AssetDatabase.GUIDToAssetPath(guids[0]);
		var prefab = AssetDatabase.LoadAssetAtPath(path, typeof(GameObject));
		var scene = SceneManager.GetActiveScene();
		var roots = scene.GetRootGameObjects();
		GameObject buildingRoot = null;
		foreach (var root in roots)
		{
			if (root.name == "BuildingRoot")
			{
				buildingRoot = root;
				break;
			}
		}
		for (int x = -10; x <= 10; x++)
		{
			for (int z = 0; z <= 40; z++)
			{
				var h = LogNormalDist();
				var go = GameObject.Instantiate(prefab, new Vector3(x, h * 0.5f, z), Quaternion.identity, buildingRoot.transform) as GameObject;
				go.transform.localScale = new Vector3(1f, h, 1f);
			}
		}
	}

	public static float LogNormalDist()
	{
		var log = 0f;
		for (int i = 0; i < 12; i++)
		{
			log += Random.value;
		}
		log -= 6f;
		log *= 1f;
		return Mathf.Pow(2f, log) * 1f;
	}
}
