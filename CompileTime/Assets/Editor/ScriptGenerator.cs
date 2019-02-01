using UnityEditor;
using UnityEngine;
using System;

public class ScriptGenerator
{
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
		for (int i = 0; i < 10000; i++)
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
		file.WriteLine("\t}");
		file.WriteLine("\tvoid Update(){");
		file.WriteLine("\t}");
		file.WriteLine("}");
		file.Close();
	}

	[MenuItem("Sample/GeneratePartial")]
	static void GeneratePartial()
	{
		GeneratePartialMain();
		for (int i = 0; i < 4000; i++)
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
		file.WriteLine("}");
		file.Close();
	}
}
