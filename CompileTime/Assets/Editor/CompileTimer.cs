using UnityEditor;
using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;

namespace Kayac
{
	public class CompileTimer : EditorWindow
	{
		[MenuItem("Kayac/CompileTimer")]
		static void Init()
		{
			EditorWindow window = GetWindowWithRect(typeof(CompileTimer), new Rect(0, 0, 200f, 100f));
			window.Show();
		}

		// コンパイル前後で変数を保持できないのでEditorPrefsに入れる必要がある
		const string compileStartTimeKey = "kayac_compileStartTime";
		const string lastCompileTimeKey = "kayac_lastCompileTime";

		void OnGUI()
		{
			DateTime startTime = new DateTime();
			var compiling = EditorApplication.isCompiling;
			bool prevCompiling = false;
			if (EditorPrefs.HasKey(compileStartTimeKey))
			{
				string str = EditorPrefs.GetString(compileStartTimeKey);
				if (!String.IsNullOrEmpty(str))
				{
					startTime = DateTime.Parse(str);
					prevCompiling = true;
				}
			}
			float lastTime = 0f;
			lastTime = EditorPrefs.GetFloat(lastCompileTimeKey);
			var currentTime = 0f;
			if (prevCompiling)
			{
				currentTime = (float)(DateTime.Now - startTime).TotalSeconds;
				if (compiling)
				{
					currentTime = (float)(DateTime.Now - startTime).TotalSeconds;
				}
				else
				{
					lastTime = (float)(DateTime.Now - startTime).TotalSeconds;
					EditorPrefs.SetFloat(lastCompileTimeKey, lastTime);
					EditorPrefs.DeleteKey(compileStartTimeKey);
				}
			}
			else if (compiling)
			{
				EditorPrefs.SetString(compileStartTimeKey, DateTime.Now.ToString());
			}
			else if (EditorPrefs.HasKey(compileStartTimeKey))
			{
				EditorPrefs.DeleteKey(compileStartTimeKey);
			}
			EditorGUILayout.LabelField("Compiling:", compiling ? currentTime.ToString("F2") : "No");
			EditorGUILayout.LabelField("LastCompileTime:", lastTime.ToString("F2"));
			if (GUILayout.Button("コード量分析"))
			{
				var root = new Node();
				MeasureCode(root, "Assets");

				var sb = new System.Text.StringBuilder();
				sb.Append("[CodeAmount]\n");
				WriteCodeTreeSize(root, sb, 0);

				var file = new StreamWriter("codeAmount.txt");
				file.Write(sb.ToString());
				file.Close();
			}
			this.Repaint();
		}

		class Node
		{
			public string name;
			public List<Node> nodes;
			public long size;
		}

		static void MeasureCode(Node node, string path)
		{
			node.nodes = new List<Node>();
			node.name = System.IO.Path.GetFileName(path);

			var directories = Directory.GetDirectories(path);
			foreach (var item in directories)
			{
				var childNode = new Node();
				MeasureCode(childNode, item);
				node.size += childNode.size;
				node.nodes.Add(childNode);
			}
			var files = Directory.GetFiles(path);
			foreach (var item in files)
			{
				var ext = System.IO.Path.GetExtension(item);
				if (ext.ToLower() == ".cs")
				{
					node.size += new System.IO.FileInfo(item).Length;
				}
			}
		}

		static void WriteCodeTreeSize(Node node, System.Text.StringBuilder sb, int level)
		{
			if (node.size > 0)
			{
				for (int i = 0; i < level; i++)
				{
					sb.Append('\t');
				}
				sb.AppendFormat("{0}\t{1}\n", node.name, node.size);
				foreach (var item in node.nodes)
				{
					WriteCodeTreeSize(item, sb, level + 1);
				}
			}
		}
	}
}
