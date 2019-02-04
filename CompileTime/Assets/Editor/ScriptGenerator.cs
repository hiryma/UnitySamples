using UnityEditor;
using UnityEngine;
using System;

public class ScriptGenerator
{
	const int FileCount = 1000;
	const int MethodCount = 1000;
	[MenuItem("Sample/ClearGenerated")]
	static void ClearGenerated()
	{
		// AssetDatabaseを使うと泣くほど遅い
		var paths = System.IO.Directory.GetFiles("Assets/Generated/");
		foreach (var path in paths)
		{
			System.IO.File.Delete(path);
		}
	}

	[MenuItem("Sample/GenerateIndependent")]
	static void GenerateIndependent()
	{
		for (int i = 0; i < FileCount; i++)
		{
			GenerateIndependentScript(i);
		}
	}

	static void GenerateIndependentScript(int i)
	{
		Debug.Log("Generate " + i + " th file.");
		var file = new System.IO.StreamWriter("Assets/Generated/Hoge" + i + ".generated.cs");
		file.WriteLine("using UnityEngine;");
		file.WriteLine("public class Hoge" + i + " : MonoBehaviour{");
		file.WriteLine("\tvoid Start(){");
		file.WriteLine("\t\tDebug.Log(Hoge" + i.ToString() + ".GetIndex());");
		file.WriteLine("\t}");
		file.WriteLine("\tvoid Update(){");
		file.WriteLine("\t}");
		file.WriteLine("\tpublic static int GetIndex(){ return " + i + "; }");
		file.WriteLine("}");
		file.Close();
	}

	[MenuItem("Sample/GenerateDependent")]
	static void GenerateDependent()
	{
		for (int i = 0; i < FileCount; i++)
		{
			GenerateDependentScript(i);
		}
	}

	static void GenerateDependentScript(int i)
	{
		Debug.Log("Generate " + i + " th file.");
		var file = new System.IO.StreamWriter("Assets/Generated/Hoge" + i + ".generated.cs");
		file.WriteLine("using UnityEngine;");
		file.WriteLine("public class Hoge" + i + " : MonoBehaviour{");
		file.WriteLine("\tvoid Start(){");
		int next = i + 1;
		if (next >= FileCount)
		{
			next = 0;
		}
		file.WriteLine("\t\tDebug.Log(Hoge" + next.ToString() + ".GetIndex());");
		file.WriteLine("\t}");
		file.WriteLine("\tvoid Update(){");
		file.WriteLine("\t}");
		file.WriteLine("\tpublic static int GetIndex(){ return " + i + "; }");
		file.WriteLine("}");
		file.Close();
	}

	[MenuItem("Sample/GeneratePartial")]
	static void GeneratePartial()
	{
		GeneratePartialMain();
		for (int i = 0; i < FileCount; i++)
		{
			GeneratePartialSub(i);
		}
	}

	static void GeneratePartialMain()
	{
		var file = new System.IO.StreamWriter("Assets/Generated/Hoge.generated.cs");
		file.WriteLine("using UnityEngine;");
		file.WriteLine("public partial class Hoge : MonoBehaviour{");
		file.WriteLine("\tvoid Start(){");
		file.WriteLine("\t}");
		file.WriteLine("\tvoid Update(){");
		file.WriteLine("\t}");
		file.WriteLine("}");
		file.Close();
	}

	static void GeneratePartialSub(int i)
	{
		Debug.Log("Generate " + i + " th file.");
		var file = new System.IO.StreamWriter("Assets/Generated/Hoge." + i + ".generated.cs");
		file.WriteLine("using UnityEngine;");
		file.WriteLine("public partial class Hoge : MonoBehaviour{");
		file.WriteLine("\tpublic void Foo" + i + "(){");
		file.WriteLine("\t}");
		file.WriteLine("\tpublic void Bar" + i + "(){");
		file.WriteLine("\t}");
		file.WriteLine("\tpublic void Baz" + i + "(){");
		file.WriteLine("\t}");
		file.WriteLine("}");

		file.WriteLine("public class Hoge" + i + " : MonoBehaviour{");
		file.WriteLine("\tvoid Start(){");
		file.WriteLine("\t}");
		file.WriteLine("\tvoid Update(){");
		file.WriteLine("\t}");
		file.WriteLine("\tpublic static int GetIndex(){ return " + i + "; }");
		file.WriteLine("}");
		file.Close();
	}

	[MenuItem("Sample/GenerateHuge")]
	static void GenerateHuge()
	{
		for (int i = 0; i < FileCount; i++)
		{
			GenerateHugeScript(i);
		}
	}

	static void GenerateHugeScript(int i)
	{
		Debug.Log("Generate " + i + " th file.");
		var file = new System.IO.StreamWriter("Assets/Generated/Hoge" + i + ".generated.cs");
		file.WriteLine("using UnityEngine;");
		file.WriteLine("public class Hoge" + i + " : MonoBehaviour{");
		file.WriteLine("\tvoid Start(){");
		file.WriteLine("\t\tDebug.Log(Hoge" + i.ToString() + ".GetIndex());");
		file.WriteLine("\t}");
		file.WriteLine("\tvoid Update(){");
		file.WriteLine("\t}");
		file.WriteLine("\tpublic static int GetIndex(){ return " + i + "; }");
		for (int j = 0; j < MethodCount; j++)
		{
			file.WriteLine("\tpublic int GetIndex" + j + "(){ return " + j + "; }");
		}
		file.WriteLine("}");
		file.Close();
	}
}
