using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using System.IO;
using Kayac;

public class Main : MonoBehaviour
{
	[SerializeField]
	Text text;
	[SerializeField]
	bool jsonMode;

	[System.Serializable]
	class Node
	{
		public float? nullableFloat;
		public int id;
		public string name;
		public List<Node> children;
		public string[] files;
		[System.NonSerialized] float magic = 1.2345f; // こいつは出てこないはず
	}
	Node root;
	string serialized;
	bool showYaml;
	int nextId = 0;

	void Start()
	{
		root = new Node();
		root.id = nextId;
		nextId++;

		// root構築
//		var path = ".";
		var path = Application.dataPath;
		Scan(root, path);

		var sb = new System.Text.StringBuilder();
		ToString(sb, root, 0);
		text.text = sb.ToString();
	}

	void Scan(Node node, string path)
	{
		var dirs = Directory.GetDirectories(path);
		if (dirs.Length > 0)
		{
			node.children = new List<Node>();
			node.nullableFloat = 9999.9999f;
			foreach (var dir in dirs)
			{
				var child = new Node();
				child.name = System.IO.Path.GetFileName(dir);
				child.id = nextId;
				nextId++;
				node.children.Add(child);
				Scan(child, dir);
			}
		}
		var files = Directory.GetFiles(path);
		if (files.Length > 0)
		{
			if (node.files == null)
			{
				node.files = new string[files.Length];
			}
			for (int i = 0; i < files.Length; i++)
			{
				node.files[i] = System.IO.Path.GetFileName(files[i]);
			}
		}
	}

	void Update()
	{
		if (Input.anyKeyDown)
		{
			showYaml = !showYaml;
			if (showYaml)
			{
				if (jsonMode)
				{
					UnityEngine.Profiling.Profiler.BeginSample("JsonUtility.ToJson");
					serialized = JsonUtility.ToJson(root, prettyPrint: true);
				}
				else
				{
					UnityEngine.Profiling.Profiler.BeginSample("PseudoYaml.Serialize");
					serialized = PseudoYaml.Serialize(root);
				}
				UnityEngine.Profiling.Profiler.EndSample();
				File.WriteAllText("output.yaml", serialized);
				text.text = serialized;
			}
			else
			{
				if (jsonMode)
				{
					UnityEngine.Profiling.Profiler.BeginSample("JsonUtility.FromJson");
					root = JsonUtility.FromJson<Node>(serialized);
				}
				else
				{
					UnityEngine.Profiling.Profiler.BeginSample("PseudoYaml.Deerialize");
					root = PseudoYaml.Deserialize<Node>(serialized);
				}
				UnityEngine.Profiling.Profiler.EndSample();
				if (root == null)
				{
					text.text = "BUG!!! Deserialization Failed!!!";
				}
				else
				{
					var sb = new System.Text.StringBuilder();
					ToString(sb, root, 0);
					text.text = sb.ToString();
				}
			}
		}
	}

	void ToString(System.Text.StringBuilder sb, Node node, int level)
	{
		sb.Append('\t', level);
		sb.Append(node.name);
		sb.Append('\n');
		if (node.files != null)
		{
			foreach (var file in node.files)
			{
				sb.Append('\t', level + 1);
				sb.Append(file);
				sb.Append('\n');
			}
		}
		if (node.children != null)
		{
			foreach (var child in node.children)
			{
				ToString(sb, child, level + 1);
			}
		}
	}
}
