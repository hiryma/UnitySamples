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

namespace Kayac
{
	public static class PseudoYaml
	{
		public static string Serialize(object root)
		{
			var serializer = new Serializer(root);
			return serializer.ToString();
		}

		public static T Deserialize<T>(string text) where T : class, new()
		{
			var deserializer = new Deserializer(text, typeof(T));
			return deserializer.RootObject as T;
		}

		// non public ---------------------------
		struct Serializer
		{
			System.Text.StringBuilder sb;

			public Serializer(object root)
			{
				sb = new System.Text.StringBuilder();
				Debug.Assert(root != null);
				var type = root.GetType();
				WriteObject(root, type, 0, false);
			}

			void WriteObject(
				object parent,
				Type type,
				int level,
				bool noIndent)
			{
				var fields = Util.GetFields(type);
				foreach (var field in fields)
				{
					if (IsSerialized(field))
					{
						var child = field.GetValue(parent);
						if (Write(child, field.Name, field.FieldType, level, noIndent))
						{
							noIndent = false;
						}
					}
				}
			}

			public override string ToString()
			{
				return sb.ToString();
			}

			bool Write(
				object o,
				string fieldName, // nullなら配列
				Type type,
				int level,
				bool noIndent)
			{
				// 先にnull判定。名前付きで値がnullなら省略する
				if ((o == null) && (fieldName != null))
				{
					return false;
				}
				if (noIndent)
				{
					if (fieldName == null) // 配列
					{
						sb.Append("- ");
					}
				}
				else
				{
					if (fieldName == null) // 配列
					{
						sb.Append(' ', (level * 2) - 2);
						sb.Append("- ");
					}
					else
					{
						sb.Append(' ', level * 2);
					}
				}

				if (o == null) // 配列要素はnullでも吐く。数が変わってしまっては困る。
				{
					if (fieldName != null)
					{
						sb.AppendFormat("{0}: ", fieldName);
					}
					sb.Append("null\n");
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
						Write(item, null, elementType, level + 1, false);
					}
				}
				else if (type.IsGenericType)
				{
					var def = type.GetGenericTypeDefinition();
					if (def == typeof(List<>))
					{
						sb.AppendFormat("{0}:\n", fieldName);
						var list = o as IList;
						foreach (var item in list)
						{
							Write(item, null, item.GetType(), level + 1, false);
						}
					}
					else if (def == typeof(Nullable<>))
					{
						if (fieldName != null)
						{
							sb.AppendFormat("{0}: ", fieldName);
						}
						sb.AppendFormat("{0}\n", o.ToString());
					}
				}
				else
				{
					bool fieldNoIndent = false;
					int childLevel = level;
					if (fieldName != null) // 配列要素でない
					{
						sb.AppendFormat("{0}:", fieldName);
						sb.Append('\n');
						childLevel++;
					}
					else // 配列要素はインデント一段少なくする
					{
						fieldNoIndent = true;
					}
					WriteObject(o, type, childLevel, fieldNoIndent);
				}
				return true;
			}

			// 後でいじりそうなので出しておく
			static bool IsSerialized(FieldInfo field)
			{
				return (!field.IsNotSerialized);
			}
		}

		static class Util
		{
			public static FieldInfo[] GetFields(Type type)
			{
				var fields = type.GetFields(
					BindingFlags.Instance
					| BindingFlags.Public
					| BindingFlags.NonPublic);
				return fields;
			}

			public static FieldInfo GetField(Type type, string name)
			{
				var field = type.GetField(name,
					BindingFlags.Instance
					| BindingFlags.Public
					| BindingFlags.NonPublic);
				return field;
			}
		}

		struct Deserializer
		{
			public object RootObject { get; private set; }

			public Deserializer(string text, Type type)
			{
#if true
				var testTokenizer = new Tokenizer(text);
				Token token = new Token();
				var sb = new System.Text.StringBuilder();
				while (testTokenizer.Get(ref token))
				{
					token.ToString(sb, text);
				}
				Debug.Log(sb.ToString());
#endif
				var tokenizer = new Tokenizer(text);
				RootObject = type.GetConstructor(Type.EmptyTypes).Invoke(null);
				var parser = new Parser(RootObject, tokenizer, type);
			}
		}

		struct Parser
		{
			public Parser(object root, Tokenizer tokenizer, Type type)
			{
				ReadObject(root, tokenizer, type);
			}

			void ReadObject(object root, Tokenizer tokenizer, Type type)
			{
			}

			void ReadArray()
			{

			}

			void ReadField()
			{

			}

			void ReadValue()
			{

			}
		}

		struct Tokenizer
		{
			public Tokenizer(string text)
			{
				tokens = new Token[bufferSize];
				head = count = offset = 0;
				this.text = text;
				pos = -1;
				nextPos = 0;
				mode = 0;
				indent = 0;
				scalarBegin = int.MinValue;
				prevChar = '\0';
				exception = null;
				line = 0;
			}

			// non public ----------------------

			const int bufferSize = 8;
			Token[] tokens;
			int head;
			int count;
			int offset; // ungetされるとoffsetが1つ下がる
			string text;
			// 解析状態
			int mode;
			int pos;
			int nextPos; // 次のループの文字位置
			int indent;
			int scalarBegin;
			char prevChar;
			Exception exception;
			int line;

			public bool Get(ref Token token)
			{
				if (offset == count)
				{
					Read();
				}
				if (exception != null) // 例外こいてるので抜ける
				{
					return false;
				}
				if (offset >= count) // Readしたのに取れなかった。終わり。
				{
					return false;
				}
				else
				{
					var index = head + offset;
					if (index >= tokens.Length)
					{
						index -= tokens.Length;
					}
					token = tokens[index];
Debug.Log("Get: " + index + " " + offset + " " + head + " " + count + " " + token.type);
					offset++;
					return true;
				}
			}

			public void Unget()
			{
				Debug.Assert(offset > 0);
				offset--;
			}


			bool IsWhiteSpace(char c)
			{
				return (c == ' ') || (c == '\t');
			}

			int NextTokenIndex()
			{
				var index = int.MinValue;
				if (count >= tokens.Length) // 満タンになっていれば
				{
					Debug.Assert(offset > 0);
					index = head + count;
					if (index >= tokens.Length)
					{
						index -= tokens.Length;
					}
					head++;
					if (head >= tokens.Length)
					{
						head = 0;
					}
					offset--;
				}
				else
				{
					Debug.Assert(head == 0);
					index = count;
					count++;
				}
				Debug.Assert(index >= 0 && index < tokens.Length, "馬鹿な " + index + " " + tokens.Length + " " + offset + " " + head);
				return index;
			}

			void AddToken(TokenType type)
			{
				Debug.Assert(scalarBegin < 0); // 吐いてない文字列がある!!
				var index = NextTokenIndex();
				tokens[index].type = type;
				tokens[index].indent = indent;
				Debug.Log("Token: " + type + " -> " + index + " " + offset + " " + head + " " + count);
			}

			void AddScalarToken(int scalarEnd)
			{
				Debug.Assert(scalarBegin >= 0);
				int index = NextTokenIndex();
				tokens[index].type = TokenType.Scalar;
				tokens[index].scalarBegin = scalarBegin;
				tokens[index].scalarLength = scalarEnd - scalarBegin;
				tokens[index].indent = indent;
				Debug.Log("Token: " + TokenType.Scalar + " " + scalarBegin + " " + (pos - scalarBegin) + " -> " + index + " " + offset + " " + head + " " + count);
				scalarBegin = int.MinValue;
			}

			void OnNewLine()
			{
				scalarBegin = int.MinValue;
				indent = 0;
			}

			/* 字句解析

			\s -> ' ' | '\t'
			\n -> '\n' | '\r' | '\r\n'

			[0x 行頭]
			00, ' ' -> 00 インデント数える
			00, \t -> Error
			00, '-' -> 10
			00, ':' -> 20
			00, '#' -> 50
			00, '%' -> 50 後でdirectiveを読む必要があればコメントと分離する
			00, '\n' -> 00 reset
			00, * -> 30

			[1x 行頭-]
			10, \s -> 60 SequenceEntry出力
			10, \n -> 00 SequenceEntry出力
			10, '-' -> 11
			10, * -> 30
			11, '-' -> 50 とりあえずコメント扱いでスルー
			11, * -> 30

			[2x 行頭:]
			20, \s -> 60 MappingValue出力
			20, \n -> 00 MappingValue出力
			20, * -> 30

			[3x scalar]
			30, ':' -> 31
			30, \s -> 32
			30, \n -> 00
			30, * -> 30
			31, \s -> 40 Scalar, MappingValue出力
			31, \n -> 00 Scalar, MappingValue出力
			31, * -> 30
			32, '#' -> 50
			32, '\n' -> 00
			32, * -> 30

			[4x in-line-space]
			40, \s -> 40
			40, \n -> 00
			40, '#' -> 50
			40, * -> 30

			[5x #]
			50, \n -> 00
			50, * -> 50

			[6x インデントカウント中になりうる非行頭空白]
			60, \s -> 60
			60, \n -> 00
			60, '#' -> 50
			60, * -> 30
			*/
			void Read()
			{
				var written = false;
				while (!written && (nextPos < text.Length))
				{
					char c = GetNextChar();
Debug.Log(mode + " " + pos + " " + nextPos + " / " + text.Length + " " + c + " sb: " + scalarBegin);
					if (c == '\0') // 保留
					{
						continue;
					}
					switch (mode)
					{
						case 0:
							if (c == ' ')
							{
								indent++;
							}
							else if (c == '\t')
							{
								exception = new System.Exception("line head contains tab char. line: " + line);
								return;
							}
							else if (c == '-') // 配列要素始まる?
							{
								indent++;
								scalarBegin = pos;
								mode = 10;
							}
							else if (c == ':')
							{
								indent++;
								scalarBegin = pos;
								mode = 20;
							}
							else if (c == '#')
							{
								mode = 50;
							}
							else if (c == '\n')
							{
								OnNewLine();
								mode = 0;
							}
							else
							{
								scalarBegin = pos;
								mode = 30;
							}
							break;
						case 10:
							if (IsWhiteSpace(c))
							{
								AddToken(TokenType.SequenceEntry);
								indent++;
								written = true;
								mode = 60;
							}
							else if (c == '\t')
							{
								exception = new System.Exception("line head contains tab char. line: " + line);
								return;
							}
							else if (c == '\n')
							{
								AddToken(TokenType.SequenceEntry);
								indent++;
								written = true;
								OnNewLine();
								mode = 0;
							}
							else if (c == '-') // --
							{
								mode = 11;
							}
							else
							{
								mode = 30;
							}
							break;
						case 11:
							if (c == '-')  //---
							{
								mode = 50;
							}
							else if (c == '\t')
							{
								exception = new System.Exception("line head contains tab char. line: " + line);
								return;
							}
							else
							{
								mode = 30;
							}
							break;
						case 20:
							if (IsWhiteSpace(c))
							{
								AddToken(TokenType.MappingValue);
								indent++;
								written = true;
								mode = 60;
							}
							else if (c == '\n')
							{
								AddToken(TokenType.MappingValue);
								indent++;
								written = true;
								OnNewLine();
								mode = 0;
							}
							else if (c == '\t')
							{
								exception = new System.Exception("line head contains tab char. line: " + line);
								return;
							}
							else
							{
								mode = 30;
							}
							break;
						case 30:
							if (c == ':')
							{
								mode = 31;
							}
							else if (IsWhiteSpace(c))
							{
								mode = 32;
							}
							else if (c == '\n')
							{
								AddScalarToken(pos);
								OnNewLine();
								mode = 0;
							}
							else
							{
								mode = 30;
							}
							break;
						case 31: // スカラ中で:を踏んだ
							if (IsWhiteSpace(c))
							{
								AddScalarToken(pos - 1); // :を含めない
								AddToken(TokenType.MappingValue);
								written = true;
								mode = 40;
							}
							else if (c == '\n')
							{
								AddScalarToken(pos - 1); // :を含めない
								AddToken(TokenType.MappingValue);
								OnNewLine();
								written = true;
								mode = 0;
							}
							else
							{
								mode = 30;
							}
							break;
						case 32: // スカラ中で' 'を踏んだ；
							if (c == '#')
							{
								AddScalarToken(pos - 1); // 最後の' 'を含めない
								written = true;
								mode = 50;
							}
							else if (c == '\n')
							{
								AddScalarToken(pos - 1); // 最後の' 'を含めない
								written = true;
								OnNewLine();
								mode = 0;
							}
							else
							{
								mode = 30;
							}
							break;
						case 40: // 文中スペース後
							if (IsWhiteSpace(c))
							{
								mode = 40;
							}
							else if (c == '\n')
							{
								OnNewLine();
								mode = 0;
							}
							else if (c == '#')
							{
								mode = 50;
							}
							else
							{
								scalarBegin = pos;
								mode = 30;
							}
							break;
						case 50:
							if (c == '\n')
							{
								OnNewLine();
								mode = 0;
							}
							else
							{
								mode = 50;
							}
							break;
						case 60: // インデントカウントしうる非行頭空白
							if (IsWhiteSpace(c))
							{
								indent++;
								mode = 60;
							}
							else if (c == '\n')
							{
								OnNewLine();
								mode = 0;
							}
							else if (c == '#')
							{
								mode = 50;
							}
							else if (c == '\t')
							{
								exception = new System.Exception("line head contains tab char. line: " + line);
								return;
							}
							else
							{
								scalarBegin = pos;
								mode = 30;
							}
							break;
					}
				}

				if (!written) // 吐いていない状態で出てくれば、最終スカラトークンを吐く
				{
					Debug.Assert(nextPos >= text.Length, pos + " " + nextPos + " " + text.Length);
					if (scalarBegin >= 0)
					{
						AddScalarToken(pos);
					}
				}
			}

			// crが出た時は処理を保留する
			char GetNextChar()
			{
				pos = nextPos;
				char c = text[pos];
				nextPos = pos + 1;
				// 改行文字の正規化
				var prevIsCr = prevChar == '\r';
				prevChar = c;
				if (prevIsCr)
				{
					if (c == '\n') //cr-lf 完成。lfを出力
					{
					}
					else if (c == '\r') // cr, cr。前のcrをlfとして出力
					{
						c = '\n';
					}
					else // crの代わりにlfを出力してposを据置き。次のループで今回出た文字を出す
					{
						c = '\n';
						nextPos = pos;
					}
				}
				else if (c == '\n') // 単にlfを出力
				{
				}
				else if (c == '\r') // 出力保留。次のループで処理
				{
					c = '\0';
				}
				else // 通常通り出力
				{
				}
				return c;
			}
		}
	}

	enum TokenType
	{
		Unknown,
		SequenceEntry, // '- '
		Scalar,
		MappingValue, // ': '
	}

	struct Token
	{
		public Token(TokenType type)
		{
			this.type = type;
			this.scalarBegin = 0;
			this.scalarLength = 0;
			this.indent = 0;
		}

		public void ToString(System.Text.StringBuilder sb, string text) // デバグ用
		{
			sb.Append(' ', indent);
			if (type == TokenType.Scalar)
			{
				sb.AppendFormat(
					"{0} {1} ({2}-{3})\n",
					type,
					text.Substring(scalarBegin, scalarLength),
					scalarBegin,
					scalarLength);
			}
			else
			{
				sb.AppendFormat("{0}\n", type);
			}
		}
		public TokenType type;
		public int scalarBegin;
		public int scalarLength;
		public int indent;
	}
}

#if false


		var line = new Line();
		line.Initialize();
		if (line.Get(text))
		{
			DeserializeUserType(ref line, instance, typeof(T), text, indent: -1); // ルートのインデントは-1とする
		}
		return instance;
		}

		if (tmpArrayList == null)
		{
			tmpArrayList = new ArrayList();
		}
		var instance = new T();
		var line = new Line();
		line.Initialize();
		if (line.Get(text))
		{
			DeserializeUserType(ref line, instance, typeof(T), text, indent: -1); // ルートのインデントは-1とする
		}



static ArrayList tmpArrayList;

struct Line
{
	public void Initialize()
	{
		prevArrayElementIndent = int.MinValue;
	}
	public void Reset()
	{
		Debug.Assert(!ungotten);
		isArrayElement = false;
		indent = 0;
		nameBegin = valueBegin = int.MinValue;
		nameLength = valueLength = 0;
		// posはいじらない。
	}

	public void Unget()
	{
		Debug.Log("Unget " + pos);
		ungotten = true;
	}
	int G;
	// 中身が何か入るまで進む
	public bool Get(string text)
	{
		Debug.Log("Get " + pos);
		G++;
		if (G >= 100)
		{
			return false;
		}
		if (ungotten) // まだ使ってないデータがあるのですぐ終わる
		{
			ungotten = false;
			return true;
		}

		while (true)
		{
			if (pos >= text.Length) // 終端に達したのでもうデータは取れない
			{
				return false;
			}
			var p0 = pos;
			Parse(text);
			var p1 = pos;
			if (p0 == p1)
			{
				Debug.LogError("BAKANA! " + pos);
				return false;
			}
			if (Filled()) // 中身があれば抜ける
			{
				return true;
			}
		}
	}

	bool Filled()
	{
		return isArrayElement // 配列であるとわかっていれば空ではない
			|| (nameBegin >= 0); // 名前があるとわかっていれば空ではない
	}
	/* 字句解析
	*, '\n' -> End
	*, '#' AND prevIsSpace -> 40
	*, '\s' -> * [prevIsSpace = true]

	00, ' ' -> 00 インデント数える
	00, '-' -> 50
	00, ':' -> 20 名前は""
	00, id_char -> 10 もぐるかもぐらないか判定

	10, ':' -> 20
	10, id_char -> 10

	20, \s -> 20
	20, id_char -> 30

	30, id_char -> 30

	40, * -> 40

	50, \s -> 50
	50, id_char -> 30 配列要素値

	60, ':' -> 20 値でなく名前と判明
	60, id_char -> 60

	00: 行頭
	10: 名前
	20: :を踏んだ後
	30: 値
	40: コメント
	50: -を踏んだ後
	60: -の後にある識別子(この段階では名前か値かわからない。:が出てくれば名前だったとわかる)
	*/

	public void Parse(string text)
	{
		Reset();

		int mode = 0;
		if (prevArrayElementIndent >= 0)
		{
			mode = 50;
			indent = prevArrayElementIndent + 2; // 一段下げる
			prevArrayElementIndent = int.MinValue;
		}
		int tmpNameBegin = int.MinValue; // 名前は:が見つかるまで保存しないのでまずはテンポラリ
		bool isSpace = true; // 最初はtrueにしておく
		while (pos < text.Length)
		{
			char c = text[pos];
			// --- モード非依存な処理 ---
			if (isSpace && (c == '#'))
			{
				mode = 40;
			}
			isSpace = false;

			if ((c == '\n') || (c == '\r')) // 改行は問答無用で終わり
			{
				// 値が始まっていれば終わらせる
				if ((valueBegin >= 0) && (valueLength == 0))
				{
					valueLength = pos - valueBegin;
				}
				pos++; // TODO: pos++一箇所しか書きたくない。
				break;
			}
			else if (char.IsWhiteSpace(c))
			{
				isSpace = true;
			}

			// --- モード別の処理 ---
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
						prevArrayElementIndent = indent;
						pos++;
						return;
						mode = 50;
					}
					else if (c == ':')
					{
						nameBegin = nameLength = 0;
						mode = 20;
					}
					else
					{
						tmpNameBegin = pos;
						mode = 10;
					}
					break;
				case 10:
					if (c == ':')
					{
						nameBegin = tmpNameBegin;
						nameLength = pos - nameBegin;
						mode = 20;
					}
					else
					{
						; // 継続
					}
					break;
				case 20:
					if (isSpace)
					{
						; // 継続
					}
					else
					{
						valueBegin = pos;
						mode = 30;
					}
					break;
				case 30: // コメントにならない限り最後まで値
					break;
				case 40: // 全て無視
					break;
				case 50:
					if (isSpace)
					{
						; // 継続
					}
					else
					{
						valueBegin = pos; // まだ名前か値かわからないので仮で値としておく。行末までに
						mode = 60;
					}
					break;
				case 60:
					if (c == ':') // 値でなく名前であると判明
					{
						nameBegin = valueBegin;
						nameLength = pos - nameBegin;
						mode = 20;
					}
					else
					{
						; // 継続
					}
					break;
			}
			pos++;
		}
		// エラーチェック
		if ((tmpNameBegin >= 0) && (nameBegin < 0)) // 名前らしき文字列が見つかっているのに:がないケースはエラー
		{
			Debug.LogError("PseudoYaml.Deserialize : parse error. ':' not found.");
		}
		Debug.Log(pos + " " + GetName(text) + " " + GetValueString(text) + " " + isArrayElement + " " + indent);
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
	public bool ungotten;
	public bool isArrayElement;
	public int indent;
	int pos;
	int nameBegin;
	int nameLength;
	int valueBegin;
	int valueLength;
	int prevArrayElementIndent;
}

static void DeserializeUserType(
	ref Line line,
	object instance,
	Type type,
	string text,
	int indent)
{
	Debug.Log("Des: " + type.Name + " " + line.isArrayElement + " " + line.GetName(text) + " " + line.GetValueString(text));
	while (true) // 一個getした状態で来ること
	{
		// インデントレベルが下がるなら、抜ける。親のメンバだ。
		if (line.indent <= indent)
		{
			Debug.Log("DesUserObj end: " + line.isArrayElement + " " + line.GetName(text) + " " + line.GetValueString(text));
			break;
		}
		string name = line.GetName(text);
		if (name != null) // 名前があって
		{
			// リフレクションしてこの名前がこの型にあるのか調べる
			var field = Util.GetField(type, name);
			if (field != null) // そのフィールドがあれば
			{
				var value = TryReadValue(ref line, name, field.FieldType, text, indent);
				if (value != null)
				{
					Debug.Log("SetValue: " + type.Name + "." + name + " = " + line.GetValueString(text));
					field.SetValue(instance, value);
				}
			}
		}
		if (!line.Get(text))
		{
			break;
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
		while (line.Get(text))
		{
			if (!line.isArrayElement || (line.indent <= indent)) // 配列であり、インデントレベルが上がっている限り
			{
				line.Unget(); // 取りすぎた
				break;
			}
			if (line.Get(text))
			{
				var element = TryReadValue(ref line, null, type.GetElementType(), text, indent);
				tmpArrayList.Add(element);
			}
		}
		var array = System.Array.CreateInstance(type.GetElementType(), tmpArrayList.Count);
		for (int i = 0; i < tmpArrayList.Count; i++)
		{
			array.SetValue(tmpArrayList[i], i);
		}
		ret = array;
	}
	else if (type.IsGenericType)
	{
		var def = type.GetGenericTypeDefinition();
		var elementTypes = type.GetGenericArguments();
		Debug.Assert(elementTypes.Length == 1);
		if (def == typeof(List<>))
		{
			Debug.Log("Array Begin");
			var elementType = elementTypes[0];
			var listType = def.MakeGenericType(elementType);
			var constructor = listType.GetConstructor(Type.EmptyTypes);
			var list = constructor.Invoke(null);
			var ilist = (IList)list;
			while (line.Get(text))
			{
				if (!line.isArrayElement || (line.indent <= indent)) // 配列であり、インデントレベルが上がっている限り
				{
					line.Unget(); // 取りすぎた
					break;
				}
				if (line.Get(text))
				{
					Debug.Log("ArrayItem " + line.isArrayElement + " " + line.GetName(text) + " " + line.GetValueString(text));
					var element = TryReadValue(ref line, null, elementType, text, indent);
					ilist.Add(element);
				}
			}
			ret = list;
		}
		else if (def == typeof(Nullable<>))
		{
			var elementType = elementTypes[0];
			var valueString = line.GetValueString(text);
			if (valueString != null)
			{
				ret = TryParsePrimitive(elementType, valueString, name);
			}
		}
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
#endif