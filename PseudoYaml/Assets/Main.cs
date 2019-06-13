using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using System.IO;

public class Main : MonoBehaviour
{
	[SerializeField]
	Text text;

	class Node
	{
		public int id;
		public string name;
		public List<Node> children;
		[System.NonSerialized]
		float magic = 1.2345f; // こいつは出てこないはず
	}
	Node root;
	string yaml;
	bool showYaml;
	int nextId = 0;

	void Start()
	{
		var dir = Application.dataPath;
		root = new Node();
		root.name = dir;
		root.id = nextId;
		nextId++;

		// root構築
		Scan(root);

		var sb = new System.Text.StringBuilder();
		ToString(sb, root, 0);
		text.text = sb.ToString();
	}

	void Scan(Node node)
	{
		var dirs = Directory.GetDirectories(node.name);
		if (dirs.Length > 0)
		{
			node.children = new List<Node>();
			foreach (var dir in dirs)
			{
				var child = new Node();
				child.name = dir;
				child.id = nextId;
				nextId++;
				node.children.Add(child);
				Scan(child);
			}
		}
		var files = Directory.GetFiles(node.name);
		if (files.Length > 0)
		{
			if (node.children == null)
			{
				node.children = new List<Node>();
			}
			foreach (var file in files)
			{
				var child = new Node();
				child.name = file;
				child.id = nextId;
				nextId++;
				node.children.Add(child);
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
				yaml = PseudoYaml.Serialize(root);
				Debug.Log(yaml);
				text.text = yaml;
			}
			else
			{
				root = PseudoYaml.Deserialize<Node>(yaml);
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
		sb.Append(node.name.Remove(0, Application.dataPath.Length));
		sb.Append('\n');
		if (node.children != null)
		{
			foreach (var child in node.children)
			{
				ToString(sb, child, level + 1);
			}
		}
	}
}
