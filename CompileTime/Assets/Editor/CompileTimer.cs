using UnityEditor;
using UnityEngine;
using System;

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
		const string prevCompilingKey = "kayac_prevCompiling";
		const string compileStartTimeKey = "kayac_compileStartTime";
		const string lastCompileTimeKey = "kayac_lastCompileTime";

		void OnGUI()
		{
			var compiling = EditorApplication.isCompiling;
			bool prevCompiling = EditorPrefs.GetBool(prevCompilingKey, false);
			int compileStartTime = EditorPrefs.GetInt(compileStartTimeKey, 0);
			float lastCompileTime = EditorPrefs.GetFloat(lastCompileTimeKey, 0f);
			var baseTime = new DateTime(2019, 1, 1);
			var currentCompileTime = 0f;
			if (prevCompiling)
			{
				var startTime = baseTime.AddSeconds(compileStartTime);
				if (compiling)
				{
					currentCompileTime = (float)(DateTime.Now - startTime).TotalSeconds;
				}
				else
				{
					lastCompileTime = (float)(DateTime.Now - startTime).TotalSeconds;
					EditorPrefs.SetFloat(lastCompileTimeKey, lastCompileTime);
					EditorPrefs.SetInt(compileStartTimeKey, 0);
				}
			}
			else if (compiling)
			{
				var startTime = (int)(DateTime.Now - baseTime).TotalSeconds;
				EditorPrefs.SetInt(compileStartTimeKey, startTime);
			}

			EditorGUILayout.LabelField("Compiling:", compiling ? currentCompileTime.ToString("F2") : "No");
			EditorGUILayout.LabelField("LastCompileTime:", lastCompileTime.ToString("F2"));
			EditorPrefs.SetBool(prevCompilingKey, compiling);

			this.Repaint();
		}
	}
}
