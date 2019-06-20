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

- 文法ミス耐性を測るテストをしてない
- 正規のyamlをつっこんだ時にどれくらい耐えるかテストしてない

*/

/*
Yaml風の何かを読み書きするもの。あくまでYaml「風」

実装を小さくするためにYamlを強く機能制限している

- 型定義がないとデシリアライズできない
    - 型にない変数は全て無視されるだけ
- 括弧を使うJSON風の記法はない
- 文字列のクォーテーションはない(さすがに後で足すかも)
- 複数行文字列用の'|'と'>'はない(後で足すかも)
- '---'による文書分割はない。...によるファイル終了もない。
- &と*はない
- !!による型指定はない
- 実体参照がない
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
		var ret = sb.ToString();
		return ret;
	}

	public static T Deserialize<T>(string text) where T : class, new()
	{
		if (tmpArrayList == null)
		{
			tmpArrayList = new ArrayList();
		}
		var instance = new T();
		var line = new Line();
		DeserializeUserType(ref line, instance, typeof(T), text, indent: -1); // ルートのインデントは-1とする
		return instance;
	}

	// non public ---------------------------

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
			if (fieldName != null)
			{
				sb.AppendFormat("{0}: null\n", fieldName);
			}
			else
			{
				sb.Append("null\n");
			}
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

	static FieldInfo GetField(Type type, string name)
	{
		var field = type.GetField(name,
			BindingFlags.Instance
			| BindingFlags.Public
			| BindingFlags.NonPublic);
		return field;
	}

	static ArrayList tmpArrayList;

	struct Line
	{
		public void Reset()
		{
			Debug.Assert(!valid);
			isArrayElement = false;
			indent = 0;
			nameBegin = valueBegin = int.MinValue;
			nameLength = valueLength = 0;
			// endPosはいじらない。
		}

		// 使用済みの印をつける
		public void Invalidate()
		{
			valid = false;
		}

		/* 字句解析
		*, '\n' -> End

		00, ' ' -> 00 インデント数える
		00, '-' -> 50
		00, '#' -> 40
		00, id_char -> 10 もぐるかもぐらないか判定

		10, ':' -> 20
		10, id_char -> 10

		20, \s -> 20
		20, id_char -> 30

		30, id_char -> 30
		30, \s -> 31
		31, '#' -> 40
		31, \s -> 31
		31, id_char -> 30

		40, * -> 40

		50, \s -> 50
		50, id_char -> 30 配列要素値

		00: 行頭
		10: 名前
		20: :を踏んだ後
		30,31: 値
		40: コメント
		50: -を踏んだ後
		*/

		public bool TryParse(string text)
		{
			if (valid) // まだ使ってないデータがあるのですぐ終わる
			{
				return true;
			}
			int pos = endPos;
			Reset();

			int mode = 0;
			while (pos < text.Length)
			{
				char c = text[pos];
				if ((c == '\n') || (c == '\r')) // 改行は問答無用で終わり
				{
					// 名前/値終わりを検出
					if ((nameBegin >= 0) && (nameLength == 0))
					{
						nameLength = pos - nameBegin;
					}
					else if ((valueBegin >= 0) && (valueLength == 0))
					{
						valueLength = pos - valueBegin;
					}
					pos++; // TODO: pos++一箇所しか書きたくない。
					break;
				}
				switch (mode)
				{
					case 0:
						if (c == ' ')
						{
							indent++;
						}
						else if (c == '-')
						{
							isArrayElement = true;
							mode = 50;
						}
						else if (c == '#')
						{
							mode = 40;
						}
						else
						{
							nameBegin = pos;
							mode = 10;
						}
						break;
					case 10:
						if (c == ':')
						{
							nameLength = pos - nameBegin;
							mode = 20;
						}
						else
						{
							; // 継続
						}
						break;
					case 20:
						if (Char.IsWhiteSpace(c))
						{
							; // 継続
						}
						else
						{
							valueBegin = pos;
							mode = 30;
						}
						break;
					case 30:
						if (Char.IsWhiteSpace(c))
						{
							mode = 31;
						}
						else
						{
							; // 継続
						}
						break;
					case 31:
						if (c == '#')
						{
							valueLength = pos - valueBegin;
							mode = 40;
						}
						else if (Char.IsWhiteSpace(c))
						{
							; // 継続
						}
						else
						{
							mode = 30;
						}
						break;
					case 40: // 全て無視
						break;
					case 50:
						if (Char.IsWhiteSpace(c))
						{
							; // 継続
						}
						else
						{
							valueBegin = pos;
							mode = 30;
						}
						break;

				}
				pos++;
			}
			if (pos > endPos)
			{
				endPos = pos;
				valid = true;
				return true;
			}
			else
			{
				return false;
			}
		}

		// TODO: 以下二つはGCAlloc源になっているので、char[]で扱って整数型等は自前でparseした方が良い。
		public string GetName(string text)
		{
			if (nameLength > 0)
			{
				return text.Substring(nameBegin, nameLength);
			}
			else
			{
				return null;
			}
		}
		public string GetValueString(string text)
		{
			if (valueLength > 0)
			{
				return text.Substring(valueBegin, valueLength);
			}
			else
			{
				return null;
			}
		}
		public bool IsValueExplicitNull(string text)
		{
			return (valueBegin >= 0)
				&& (valueLength == 4)
				&& (text[valueBegin + 0] == 'n')
				&& (text[valueBegin + 1] == 'u')
				&& (text[valueBegin + 2] == 'l')
				&& (text[valueBegin + 3] == 'l');
		}
		public bool isArrayElement;
		public int indent;
		int endPos;
		bool valid;
		int nameBegin;
		int nameLength;
		int valueBegin;
		int valueLength;
	}

	static void DeserializeUserType(
		ref Line line,
		object instance,
		Type type,
		string text,
		int indent)
	{
		while (line.TryParse(text))
		{
			// インデントレベルが下がるか同じなら、抜ける。親のメンバだ。
			if (line.indent <= indent)
			{
				break;
			}
			line.Invalidate(); // この行は使用済み
			string name = line.GetName(text);
			if (name != null) // 名前があって
			{
				// リフレクションしてこの名前がこの型にあるのか調べる
				var field = GetField(type, name);
				if (field != null) // そのフィールドがあれば
				{
					var value = TryReadValue(ref line, name, field.FieldType, text, indent);
					if (value != null)
					{
						field.SetValue(instance, value);
					}
				}
			}
		}
	}

	static object TryReadValue(
		ref Line line,
		string name,
		Type type,
		string text,
		int indent)
	{
		object ret = null;
		if (line.IsValueExplicitNull(text)) // nullと明示的に書いてあるなら何もしないでnull返す
		{
			// 何もしない
		}
		else if (type.IsPrimitive)
		{
			var valueString = line.GetValueString(text);
			if (valueString != null)
			{
				ret = TryParsePrimitive(type, valueString, name);
			}
		}
		else if (type == typeof(string))
		{
			ret = line.GetValueString(text);
		}
		else if (type.IsArray)
		{
			tmpArrayList.Clear();
			while (line.TryParse(text))
			{
				if (!line.isArrayElement || (line.indent <= indent)) // 配列であり、インデントレベルが上がっている限り
				{
					break;
				}
				line.Invalidate(); // この行は使用済み
				var element = TryReadValue(ref line, null, type.GetElementType(), text, line.indent);
				tmpArrayList.Add(element);
			}
			var array = System.Array.CreateInstance(type.GetElementType(), tmpArrayList.Count);
			for (int i = 0; i < tmpArrayList.Count; i++)
			{
				array.SetValue(tmpArrayList[i], i);
			}
			ret = array;
		}
		else if (type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(List<>)))
		{
			var elementTypes = type.GetGenericArguments();
			Debug.Assert(elementTypes.Length == 1);
			var genericTypeDef = type.GetGenericTypeDefinition();
			var elementType = elementTypes[0];
			var listType = genericTypeDef.MakeGenericType(elementType);
			var constructor = listType.GetConstructor(Type.EmptyTypes);
			var list = constructor.Invoke(null);
			var ilist = (IList)list;
			while (line.TryParse(text))
			{
				if (!line.isArrayElement || (line.indent <= indent)) // 配列であり、インデントレベルが上がっている限り
				{
					break;
				}
				line.Invalidate(); // この行は使用済み
				var element = TryReadValue(ref line, null, elementType, text, line.indent);
				ilist.Add(element);
			}
			ret = list;
		}
		else // 独自型
		{
			var constructor = type.GetConstructor(Type.EmptyTypes);
			ret = constructor.Invoke(null);
			DeserializeUserType(ref line, ret, type, text, indent);
		}
		return ret;
	}

	// TODO: これ、遅くせずにもっと短く書く手ないの?
	static object TryParsePrimitive(Type type, string valueString, string name)
	{
		object ret = null;
		if (type == typeof(sbyte))
		{
			sbyte t;
			if (sbyte.TryParse(valueString, out t))
			{
				ret = t; //boxing
			}
		}
		else if (type == typeof(byte))
		{
			byte t;
			if (byte.TryParse(valueString, out t))
			{
				ret = t; //boxing
			}
		}
		else if (type == typeof(short))
		{
			short t;
			if (short.TryParse(valueString, out t))
			{
				ret = t; //boxing
			}
		}
		else if (type == typeof(ushort))
		{
			ushort t;
			if (ushort.TryParse(valueString, out t))
			{
				ret = t; //boxing
			}
		}
		else if (type == typeof(int))
		{
			int t;
			if (int.TryParse(valueString, out t))
			{
				ret = t; //boxing
			}
		}
		else if (type == typeof(uint))
		{
			uint t;
			if (uint.TryParse(valueString, out t))
			{
				ret = t; //boxing
			}
		}
		else if (type == typeof(long))
		{
			long t;
			if (long.TryParse(valueString, out t))
			{
				ret = t; //boxing
			}
		}
		else if (type == typeof(ulong))
		{
			ulong t;
			if (ulong.TryParse(valueString, out t))
			{
				ret = t; //boxing
			}
		}
		else if (type == typeof(float))
		{
			float t;
			if (float.TryParse(valueString, out t))
			{
				ret = t; //boxing
			}
		}
		else if (type == typeof(double))
		{
			double t;
			if (double.TryParse(valueString, out t))
			{
				ret = t; //boxing
			}
		}
		else if (type == typeof(bool))
		{
			if (valueString == "true")
			{
				ret = true; //boxing
			}
			else if (valueString == "false")
			{
				ret = false; //boxing
			}
		}

		if (ret == null)
		{
			var s = string.Format("PseudoYaml.Deserialize: read {0}.{1}({2}) from {3}", type.Name, name, type.Name, valueString);
		}
		return ret;
	}
}
