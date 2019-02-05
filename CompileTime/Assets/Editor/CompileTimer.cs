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
				root.Build("Assets");
				root.Optimize();
				var sb = new System.Text.StringBuilder();
				root.Write(sb, 0);

				var file = new StreamWriter("codeAmount.txt");
				file.Write(sb.ToString());
				file.Close();
			}
			this.Repaint();
		}

		class Node
		{
			public void Build(string path)
			{
				nodes = new List<Node>();
				name = System.IO.Path.GetFileName(path);

				var directories = Directory.GetDirectories(path);
				foreach (var item in directories)
				{
					var child = new Node();
					child.Build(item);
					size += child.size;
					nodes.Add(child);
				}
				var files = Directory.GetFiles(path);
				foreach (var item in files)
				{
					var ext = System.IO.Path.GetExtension(item);
					if (ext.ToLower() == ".cs")
					{
						size += new System.IO.FileInfo(item).Length;
					}
				}
			}

			// フォルダが一個しかなければ、子を親に統合する
			public void Optimize()
			{
				// サイズが0のノードを削除
				int dst = 0;
				for (int i = 0; i < nodes.Count; i++)
				{
					if (nodes[i].size > 0)
					{
						nodes[i].Optimize(); // サイズが非0なら下流呼び出し
						nodes[dst] = nodes[i];
						dst++;
					}
				}
				nodes.RemoveRange(dst, nodes.Count - dst);

				// 子が1つなら自分に統合
				if (dst == 1)
				{
					size += nodes[0].size;
					nodes = nodes[0].nodes;
				}
			}

			public void Write(System.Text.StringBuilder sb, int level)
			{
				if (size > 0)
				{
					for (int i = 0; i < level; i++)
					{
						sb.Append('\t');
					}
					sb.AppendFormat("{0}\t{1}\n", name, size);
					foreach (var item in nodes)
					{
						item.Write(sb, level + 1);
					}
				}
			}

			public string name;
			public List<Node> nodes;
			public long size;
		}
	}
}
