using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Reflection;

/* TODO:

# 全般

- 何を吐いて何を吐かないかをどう制御する?
    - 現状NonSerializedがついていなければ全て。Unityに合わせるべきか?

# シリアライズ

- Enumテストしてない。たぶん実装も必要
- nullableどうなるのかテストしてない。特別扱いするならたぶん実装も必要
- 改行入り文字列 -> なんかあったよね書式
- エスケープしないといけない文字列 -> 面倒なら制約として放置しても...ダメかな...
- 最初がスペースな文字列 -> ''でくくってしまえばこの問題は消えるが、くくらなくていいものはくくりたくない。
- nullの要素は吐かなくてもいいんじゃないか? -> nullableを扱う際に考える

# デシリアライズ

- 何も作ってない

*/

/*
Yaml風の何かを読み書きするもの。あくまでYaml「風」

実装を小さくするためにYamlを強く機能制限している

- 括弧を使うJSON風の記法はない
- 文字列のクォーテーションはない(さすがに後で足すかも)
- 複数行文字列用の'|'と'>'はない(後で足すかも)
- '---'による文書分割はない。...によるファイル終了もない。
- &と*はない
- !!による型指定はない
- ~がない(後で足すかも)
- 実体参照がない
-
*/
public static class PseudoYaml
{
	public static string Serialize(object root)
	{
		Debug.Assert(root != null);
		var sb = new System.Text.StringBuilder();
		var type = root.GetType();
		var fields = GetFields(type);
		foreach (var field in fields)
		{
			if (IsSerialized(field))
			{
				var child = field.GetValue(root);
				Serialize(sb, child, field.Name, field.FieldType, 0, false);
			}
		}
		return sb.ToString();
	}

	static void Serialize(
		System.Text.StringBuilder sb,
		object o,
		string fieldName,
		Type type,
		int level,
		bool arrayElement)
	{
		sb.Append(' ', level * 2);
		if (arrayElement)
		{
			sb.Append("- ");
		}

		if (o == null)
		{
			Debug.Assert(fieldName != null);
			sb.AppendFormat("{0}: null\n", fieldName);
		}
		else if (type.IsPrimitive || (type == typeof(string)))
		{
			if (fieldName != null)
			{
				sb.AppendFormat("{0}: ", fieldName);
			}
			sb.AppendFormat("{0}\n", o.ToString());
		}
		else if (type.IsArray)
		{
			sb.AppendFormat("{0}:\n", fieldName);
			var array = o as Array;
			var elementType = type.GetElementType();
			foreach (var item in array)
			{
				Serialize(sb, item, null, elementType, level + 1, true);
			}
		}
		else if (type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(List<>)))
		{
			sb.AppendFormat("{0}:\n", fieldName);
			var list = o as IList;
			foreach (var item in list)
			{
				Serialize(sb, item, null, item.GetType(), level + 1, true);
			}
		}
		else
		{
			if (!string.IsNullOrEmpty(fieldName))
			{
				sb.AppendFormat("{0}:", fieldName);
			}
			sb.Append('\n');
			var fields = GetFields(type);
			foreach (var field in fields)
			{
				if (IsSerialized(field))
				{
					var child = field.GetValue(o);
					Serialize(sb, child, field.Name, field.FieldType, level + 1, false);
				}
			}
		}
	}

	// 後でいじりそうなので出しておく
	static bool IsSerialized(FieldInfo field)
	{
		return (!field.IsNotSerialized);
	}

	// 後でいじりそうなので出しておく
	static FieldInfo[] GetFields(Type type)
	{
		var fields = type.GetFields(
			BindingFlags.Instance
			| BindingFlags.Public
			| BindingFlags.NonPublic);
		return fields;
	}

	public static T Deserialize<T>(string text) where T: class, new()
	{
		return null;
	}
}
