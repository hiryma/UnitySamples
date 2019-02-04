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
			this.Repaint();
		}
	}
}
