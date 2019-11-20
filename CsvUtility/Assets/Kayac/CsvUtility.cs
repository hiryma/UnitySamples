#define USE_UNITY_SERIALIZE_FIELD_ATTRIBUTE // UnityEngine.SerializeFieldを見るなら有効に。無効だとprivateでも気にせず吐く。
#define USE_NEWTONSOFT_JSON_PROPERTY_ATTRIBUTE // Newtonsoft.Json.JsonPropertyを見るなら有効に。名前を差し換える。これがなければそもそもプロパティは吐かない。

using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System;

#if USE_UNITY_SERIALIZE_FIELD_ATTRIBUTE
using UnityEngine;
#endif

#if USE_NEWTONSOFT_JSON_PROPERTY_ATTRIBUTE
using Newtonsoft.Json;
#endif

namespace Kayac
{
    public class CsvUtility
    {
        public static string ToCsv<T>(IEnumerable<T> items, IEnumerable<string> columnOrder = null)
        {
            var instance = new CsvUtility();
            return instance.Serialize(items, columnOrder);
        }

        public static List<T> FromCsv<T>(string csv) where T : new()
        {
            var instance = new CsvUtility();
            return instance.Deserialize<T>(csv);
        }

        // non-public -----------------
        CsvUtility()
        {
        }

        string Serialize<T>(IEnumerable<T> items, IEnumerable<string> columnOrder)
        {
            var sb = new StringBuilder();
            // フィールド列挙
            Type type = typeof(T);
            var members = ExtractMembers(type);
            if (columnOrder != null) // 列指定があるのでその通りに並べる
            {
                WriteHeader(sb, columnOrder);
                // members.Valuesを書き出し順に配列に並べる
                var memberInfos = new List<MemberInfo>();
                foreach (var key in columnOrder)
                {
                    MemberInfo memberInfo;
                    members.TryGetValue(key, out memberInfo);
                    memberInfos.Add(memberInfo); // もし見つからなくてもnullを入れる。
                }
                foreach (var item in items)
                {
                    WriteRecord(sb, memberInfos, item);
                }
            }
            else // 自動吐き出し
            {
                WriteHeader(sb, members.Keys);
                foreach (var item in items)
                {
                    WriteRecord(sb, members.Values, item);
                }
            }
            return sb.ToString();
        }

        static SortedDictionary<string, MemberInfo> ExtractMembers(Type type)
        {
            var ret = new SortedDictionary<string, MemberInfo>();
            // 吐き出し
            while (type != null)
            {
                ExtractMembers(ret, type);
                type = type.BaseType;
            }
            return ret;
        }

        static void ExtractMembers(SortedDictionary<string, MemberInfo> dic, Type type)
        {
            var members = type.GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            // 吐き出し
            for (int i = 0; i < members.Length; i++)
            {
                var member = members[i];
                string name = null;
                if (member is FieldInfo)
                {
                    var field = member as FieldInfo;
                    if (field.IsNotSerialized)
                    {
                        continue;
                    }
#if USE_UNITY_SERIALIZE_FIELD_ATTRIBUTE // UnityEngine.SerializeFieldがなければ非publicを出さない
                    if (!field.IsPublic)
                    {
                        if (member.GetCustomAttributes(typeof(SerializeField), true).Length == 0)
                        {
                            continue;
                        }
                    }
#endif
                    name = field.Name;
                }
                else if (member is PropertyInfo)
                {
                    var property = member as PropertyInfo;
                    if (!property.CanRead || !property.CanWrite) // 読み書きできないものは捨てる。一方通行なものは出さない。
                    {
                        continue;
                    }
#if USE_NEWTONSOFT_JSON_PROPERTY_ATTRIBUTE // JsonPropertyがあれば名前を差し換える
                    var attrs = member.GetCustomAttributes(typeof(JsonPropertyAttribute), true);
                    if (attrs.Length > 0)
                    {
                        name = (attrs[0] as JsonPropertyAttribute).PropertyName;
                    }
                    else // アトリビュートなければ出さない
                    {
                        continue;
                    }
#else
                    continue; // そもそもプロパティは出さない
#endif
                }
                else // フィールドでもプロパティでもないので捨てる
                {
                    continue;
                }
                Debug.Assert(name != null);
                if (!dic.ContainsKey(name))
                {
                    dic.Add(name, member);
                }
            }
        }

        void WriteHeader(StringBuilder sb, IEnumerable<string> keys)
        {
            bool first = true;
            foreach (var key in keys)
            {
                if (!first)
                {
                    sb.Append(',');
                }
                sb.Append(key);
                first = false;
            }
            sb.Append("\r\n");
        }

        void WriteRecord<T>(StringBuilder sb, IEnumerable<MemberInfo> members, T item)
        {
            bool firstMember = true;
            foreach (var member in members)
            {
                if (member == null) // 吐いてくれと言われたメンバがない場合、カラムをスキップせず空カラムを吐き出す
                {
                    if (!firstMember)
                    {
                        sb.Append(',');
                    }
                    firstMember = false;
                }
                else if (WriteMember<T>(sb, member, item, firstMember))
                {
                    firstMember = false;
                }
            }
            sb.Append("\r\n");
        }

        bool WriteMember<T>(StringBuilder sb, MemberInfo member, T item, bool firstMember)
        {
            bool ret = false;
            Type type = null;
            object value = null;
            if (member is FieldInfo)
            {
                var field = member as FieldInfo;
                value = field.GetValue(item);
                type = field.FieldType;
            }
            else if (member is PropertyInfo)
            {
                var property = member as PropertyInfo;
                value = property.GetValue(item, null);
                type = property.PropertyType;
            }
            string text;
            if ((value != null) && (type != null)) // メンバが吐き出し不能な型の場合、
            {
                text = TryConvertSimpleTypeToString(type, value);
                // 単純型でなければ複合型を処理
                if (text == null)
                {
                    if (type.IsArray) // 配列
                    {
                        text = JsonizeIList(value);
                    }
                    else if (type.IsGenericType)
                    {
                        var def = type.GetGenericTypeDefinition();
                        if (def == typeof(List<>)) // Listだから面倒見る
                        {
                            text = JsonizeIList(value);
                        }
                    }
                    else // 複合型はJSONにして保存
                    {
                        text = JsonUtility.ToJson(value);
                        text = Quote(text);
                    }
                }
                if (!firstMember)
                {
                    sb.Append(',');
                }
                sb.Append(text);
                ret = true;
            }
            return ret;
        }

        string TryConvertSimpleTypeToString(Type type, object value)
        {
            string text = null;
            if (type.IsPrimitive)
            {
                if (value is float)
                {
                    text = ((float)value).ToString("F8");
                }
                else if (value is double)
                {
                    text = ((double)value).ToString("F18");
                }
                else if (value is bool)
                {
                    text = ((bool)value) ? "true" : "false"; // ただToStringさせるとTrueとFalseになるので、jsonに合わせて小文字にしておく
                }
                else if (value is char)
                {
                    var c = (char)value;
                    if (c == '"') //"はエスケープが必要。
                    {
                        text = new string(c, 4);
                    }
                    else if ((c == ',') || (c < ' ')) //,と制御文字は""で囲む
                    {
                        text = "\",\"";
                    }
                    else
                    {
                        text = new string(c, 1);
                    }
                }
                else
                {
                    text = value.ToString();
                }
            }
            else if (value is string)
            {
                text = Quote((string)value);
            }
            return text;
        }

        string JsonizeIList(object arrayObject)
        {
            var sb = new StringBuilder();
            var array = arrayObject as IList;
            sb.Append('[');
            bool firstItem = true;
            if (array != null)
            {
                foreach (var item in array)
                {
                    var type = item.GetType();
                    if (type.IsPrimitive || (type == typeof(string)))
                    {
                        var text = TryConvertSimpleTypeToString(type, item);
                        if (!string.IsNullOrEmpty(text))
                        {
                            if (firstItem)
                            {
                                firstItem = false;
                            }
                            else
                            {
                                sb.Append(',');
                            }
                            sb.Append(text);
                        }
                    }
                }
            }
            sb.Append(']');
            return Quote(sb.ToString());
        }

        string Quote(string text)
        {
            var sb = new StringBuilder();
            sb.Append('"');
            foreach (var c in text)
            {
                if (c == '"')
                {
                    sb.Append('"');
                }
                sb.Append(c);
            }
            sb.Append('"');
            return sb.ToString();
        }

        List<T> Deserialize<T>(string csv) where T : new()
        {
            var deserializer = new Deserializer(csv);
            return deserializer.Deserialize<T>();
        }

        struct Deserializer
        {
            public Deserializer(string csv)
            {
                tmpSb = new StringBuilder();
                tmpList = new List<object>();
                tokenizer = new Tokenizer(csv);
                this.csv = csv;
            }
            public List<T> Deserialize<T>() where T : new()
            {
                var recordType = typeof(T);
                var memberDic = CsvUtility.ExtractMembers(recordType);
                var members = ReadHeader(memberDic);
                // 本体解釈
                return ReadRecords<T>(members);
            }

            // non-public ----------------
            Tokenizer tokenizer;
            StringBuilder tmpSb;
            List<object> tmpList;
            string csv;

            List<MemberInfo> ReadHeader(SortedDictionary<string, MemberInfo> memberDic)
            {
                var token = new Token();
                var members = new List<MemberInfo>();
                while (true)
                {
                    if (tokenizer.GetToken(ref token))
                    {
                        if (token.type == Token.Type.Field)
                        {
                            var name = token.GetField(csv, tmpSb);
                            // 辞書から探す
                            MemberInfo member;
                            memberDic.TryGetValue(name, out member);
                            members.Add(member); // nullならばそれで良い。nullも加える必要がある
                        }
                        else
                        {
                            break;
                        }
                    }
                    else
                    {
                        break;
                    }
                }
                return members;
            }

            List<T> ReadRecords<T>(List<MemberInfo> members) where T : new()
            {
                var records = new List<T>();
                var recordIsValueType = typeof(T).IsValueType;
                while (true)
                {
                    var record = ReadRecord<T>(members, recordIsValueType);
                    if (record != null)
                    {
                        records.Add(record);
                    }
                    else
                    {
                        break;
                    }
                }
                return records;
            }

            T ReadRecord<T>(List<MemberInfo> members, bool recordIsValueType) where T : new()
            {
                var token = new Token();
                int memberIndex = 0;
                T instance = default(T);
                while (true)
                {
                    if (tokenizer.GetToken(ref token))
                    {
                        if (token.type == Token.Type.Field)
                        {
                            if (!recordIsValueType && (instance == null))
                            {
                                instance = new T();
                            }
                            var text = token.GetField(csv, tmpSb);
                            var memberInfo = members[memberIndex];
                            if (memberInfo is FieldInfo)
                            {
                                var fieldInfo = memberInfo as FieldInfo;
                                var memberType = fieldInfo.FieldType;
                                var value = TryParseValue(memberType, text);
                                if (value != null)
                                {
                                    fieldInfo.SetValue(instance, value);
                                }
                            }
                            else if (memberInfo is PropertyInfo)
                            {
                                var propertyInfo = memberInfo as PropertyInfo;
                                var memberType = propertyInfo.PropertyType;
                                var value = TryParseValue(memberType, text);
                                if (value != null)
                                {
                                    propertyInfo.SetValue(instance, value, index: null);
                                }
                            }
                            memberIndex++;
                        }
                        else
                        {
                            break;
                        }
                    }
                    else
                    {
                        break;
                    }
                }
                return instance;
            }

            object TryParseValue(Type type, string valueText)
            {
                object ret = null;
                if (type.IsPrimitive)
                {
                    ret = TryParsePrimitive(type, valueText);
                }
                else if (type == typeof(string))
                {
                    ret = valueText;
                }
                else if (type.IsArray)
                {
                    tmpList.Clear();
                    var elementType = type.GetElementType();
                    ParseJsonArray(tmpList, elementType, valueText);

                    var array = Array.CreateInstance(elementType, tmpList.Count);
                    for (int i = 0; i < tmpList.Count; i++)
                    {
                        array.SetValue(tmpList[i], i);
                    }
                    ret = array;
                }
                else if (type.IsGenericType)
                {
                    var def = type.GetGenericTypeDefinition();
                    if (def == typeof(List<>))
                    {
                        var elementTypes = type.GetGenericArguments();
                        var elementType = elementTypes[0];
                        var listType = def.MakeGenericType(elementType);
                        var constructor = listType.GetConstructor(Type.EmptyTypes);
                        var list = constructor.Invoke(null);
                        if (list != null)
                        {
                            var ilist = (IList)list;

                            tmpList.Clear();
                            ParseJsonArray(tmpList, elementType, valueText);

                            for (int i = 0; i < tmpList.Count; i++)
                            {
                                ilist.Add(tmpList[i]);
                            }
                            ret = ilist;
                        }
                    }
                }
                else // ユーザ定義型
                {
                    try
                    {
                        ret = JsonUtility.FromJson(valueText, type);
                    }
                    catch
                    {
                        Debug.LogWarning("JsonDeserialization Failed. text: " + valueText); // TODO: 実地では警告抑制できた方がいいし、逆にエラーにしてはじく選択肢も欲しい
                        // スルー
                    }
                }
                return ret;
            }

            static void ParseJsonArray(List<object> elements, Type elementType, string json)
            {
                elements.Clear();
                // 字句解析を行う
                int mode = 0;
                int pos = 0;
                int elementBegin = 0;
                /*
                0 [より前
                1 要素開始
                2 非quote要素の値の中
                3 quote要素の値の中
                4 quote要素の値の中で\
                5 要素の値終了後
                6 ]より後
                */
                while (pos < json.Length)
                {
                    var c = json[pos];
                    switch (mode)
                    {
                        case 0:
                            if (c == '[')
                            {
                                mode = 1;
                            }
                            break;
                        case 1:
                            if (c == '"') // quoted
                            {
                                mode = 3;
                                elementBegin = pos + 1;
                            }
                            else if (IsJsonWhiteSpace(c))
                            {
                                ; // JSON空白なら続行
                            }
                            else // それ以外なら値の開始
                            {
                                mode = 2;
                                elementBegin = pos;
                            }
                            break;
                        case 2:
                            if (c == ']')
                            {
                                AddElement(elements, elementType, json, elementBegin, pos);
                                mode = 6;
                            }
                            else if (c == ',')
                            {
                                AddElement(elements, elementType, json, elementBegin, pos);
                                mode = 1;
                            }
                            break;
                        case 3:
                            if (c == '"')
                            {
                                AddElement(elements, elementType, json, elementBegin, pos);
                                mode = 5;
                            }
                            else if (c == '\\')
                            {
                                mode = 4;
                            }
                            break;
                        case 4: // なんであれ1字はそのまま使う
                            mode = 3;
                            break;
                        case 5:
                            if (c == ',')
                            {
                                mode = 1;
                            }
                            else if (c == ']')
                            {
                                mode = 6;
                            }
                            break;
                        case 6:
                            break;
                    }
                    pos++;
                }
            }

            static void AddElement(
                List<object> elements,
                Type elementType,
                string valueText,
                int elementBegin,
                int elementEnd)
            {
                var element = TryParsePrimitive(elementType, valueText.Substring(elementBegin, elementEnd - elementBegin));
                if (element != null)
                {
                    elements.Add(element);
                }
                else
                {
                    elements.Add(Activator.CreateInstance(elementType));
                }
            }

            static bool IsJsonWhiteSpace(char c)
            {
                return (c == ' ') || (c == '\t') || (c == '\r') || (c == '\n');
            }

            static object TryParsePrimitive(Type type, string valueText)
            {
                object ret = null;
                if (type == typeof(sbyte))
                {
                    sbyte t;
                    if (sbyte.TryParse(valueText, out t))
                    {
                        ret = t; //boxing
                    }
                }
                else if (type == typeof(byte))
                {
                    byte t;
                    if (byte.TryParse(valueText, out t))
                    {
                        ret = t; //boxing
                    }
                }
                else if (type == typeof(short))
                {
                    short t;
                    if (short.TryParse(valueText, out t))
                    {
                        ret = t; //boxing
                    }
                }
                else if (type == typeof(ushort))
                {
                    ushort t;
                    if (ushort.TryParse(valueText, out t))
                    {
                        ret = t; //boxing
                    }
                }
                else if (type == typeof(int))
                {
                    int t;
                    if (int.TryParse(valueText, out t))
                    {
                        ret = t; //boxing
                    }
                }
                else if (type == typeof(uint))
                {
                    uint t;
                    if (uint.TryParse(valueText, out t))
                    {
                        ret = t; //boxing
                    }
                }
                else if (type == typeof(long))
                {
                    long t;
                    if (long.TryParse(valueText, out t))
                    {
                        ret = t; //boxing
                    }
                }
                else if (type == typeof(ulong))
                {
                    ulong t;
                    if (ulong.TryParse(valueText, out t))
                    {
                        ret = t; //boxing
                    }
                }
                else if (type == typeof(float))
                {
                    float t;
                    if (float.TryParse(valueText, out t))
                    {
                        ret = t; //boxing
                    }
                }
                else if (type == typeof(double))
                {
                    double t;
                    if (double.TryParse(valueText, out t))
                    {
                        ret = t; //boxing
                    }
                }
                else if (type == typeof(bool))
                {
                    if (valueText == "true")
                    {
                        ret = true; //boxing
                    }
                    else if (valueText == "false")
                    {
                        ret = false; //boxing
                    }
                }
                else if (type == typeof(char))
                {
                    if (valueText.Length > 0)
                    {
                        if ((valueText[0] == '"') && (valueText.Length >= 2)) // ""でくくられてるケース
                        {
                            ret = valueText[1]; // boxing
                        }
                        else
                        {
                            ret = valueText[0]; // boxing
                        }
                    }
                }
                return ret;
            }
        }

        struct Token
        {
            public enum Type
            {
                Unknown,
                Field,
                LineEnd,
            }
            public string GetField(string csv, StringBuilder tmpSb)
            {
                if (!quoted)
                {
                    return csv.Substring(fieldBegin, fieldLength);
                }
                else
                {
                    return Dequote(csv, fieldBegin, fieldLength, tmpSb);
                }
            }

            public string Dequote(string csv, int begin, int length, StringBuilder tmpSb)
            {
                tmpSb.Length = 0;
                bool prevDQuote = false;
                for (int i = 0; i < length; i++)
                {
                    var c = csv[i + begin];
                    if (prevDQuote)
                    {
                        if (c == '"') // 2個続いて初めて出力
                        {
                            tmpSb.Append(c);
                            prevDQuote = false;
                        }
                        else // "の後に"でないものがあったならばエラーだが、ここで中断する
                        {
                            break;
                        }
                    }
                    else
                    {
                        if (c == '"')
                        {
                            prevDQuote = true;
                        }
                        else
                        {
                            tmpSb.Append(c);
                        }
                    }
                }
                return tmpSb.ToString();
            }
            public int fieldBegin;
            public int fieldLength;
            public Type type;
            public bool quoted;
        }

        struct Tokenizer
        {
            public Tokenizer(string csv)
            {
                this.csv = csv;
                pos = 0;
            }

            /* 字句解析
            0 フィールド開始
            1 フィールド中
            2 Quotされたフィールド中
            3 Quotされたフィールド中で"を踏んだ後
            4 Quotされたフィールドが"で終わった後
            5 Quotされてないcr
            */
            public bool GetToken(ref Token token)
            {
                int mode = 0;
                token.fieldBegin = pos;
                token.fieldLength = 0;
                token.type = Token.Type.Unknown;
                token.quoted = false;
                bool written = false;
                while (!written && (pos < csv.Length))
                {
                    char c = csv[pos];
                    switch (mode)
                    {
                        case 0: // 開始
                            switch (c)
                            {
                                case '"':
                                    mode = 2;
                                    token.fieldBegin = pos + 1; // 次から始まる
                                    token.quoted = true;
                                    break;
                                case '\r':
                                    mode = 5;
                                    break;
                                case '\n':
                                    token.type = Token.Type.LineEnd;
                                    written = true;
                                    break;
                                case ',':
                                    token.type = Token.Type.Field;
                                    written = true;
                                    break;
                                default:
                                    mode = 1;
                                    break;
                            }
                            break;
                        case 1: // フィールド中
                            switch (c)
                            {
                                case '\r':
                                case '\n':
                                    token.type = Token.Type.Field;
                                    token.fieldLength = pos - token.fieldBegin;
                                    written = true;
                                    pos--; // 次回はこの改行文字からなので戻す
                                    break;
                                case ',':
                                    token.type = Token.Type.Field;
                                    token.fieldLength = pos - token.fieldBegin;
                                    written = true;
                                    break;
                            }
                            break;
                        case 2: // ""に囲まれたフィールド
                            switch (c)
                            {
                                case '"':
                                    mode = 3;
                                    break;
                            }
                            break;
                        case 3: // ""に囲まれたフィールドの中で"を踏んだ直後
                            switch (c)
                            {
                                case '"': // もう一回"があれば、これはエスケープされているので続行
                                    mode = 2;
                                    break;
                                case ',':
                                case '\r':
                                case '\n':
                                    token.type = Token.Type.Field;
                                    token.fieldLength = (pos - 1) - token.fieldBegin; // 前の文字が"
                                    written = true;
                                    break;
                                default: // フィールドの値取得は終わったのだが、ゴミがあるので終了文字まで流す
                                    mode = 4;
                                    token.type = Token.Type.Field;
                                    token.fieldLength = (pos - 1) - token.fieldBegin; // 前の文字が"
                                    break;
                            }
                            break;
                        case 4: // フィールドの解釈が終わったが、,も改行も来ていない時。ゴミを飛ばす
                            switch (c)
                            {
                                case '\r':
                                case '\n':
                                case ',':
                                    written = true;
                                    break;
                            }
                            break;
                        case 5: // cr
                            switch (c)
                            {
                                case '\n': // CRLF
                                    token.type = Token.Type.LineEnd;
                                    written = true;
                                    break;
                                default: // 前のCRで改行し、1字戻す
                                    token.type = Token.Type.LineEnd;
                                    written = true;
                                    pos--; // 一字戻す
                                    break;
                            }
                            break;
                    }
                    pos++;
                }
                return written;
            }

            // non-public --------------
            string csv;
            int pos;
        }
    }
}

