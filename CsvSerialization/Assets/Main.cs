using System.Collections.Generic;
using UnityEngine;

// Json.netのJsonPropertyの偽物
namespace Newtonsoft.Json
{
    class JsonPropertyAttribute : System.Attribute
    {
        public JsonPropertyAttribute(string propertyName)
        {
            PropertyName = propertyName;
        }
        public string PropertyName { get; private set; }
    }
}

public class Main : MonoBehaviour
{
    class NonSerializedType
    {
        public void SetRandom()
        {
            x = Random.Range(0, 10);
            y = Random.value;
            z = Random.Range(0, 100).ToString();
        }
        public int x;
        public float y;
        [SerializeField] string z;
    }

    [System.Serializable]
    class SerializedType
    {
        public void SetRandom()
        {
            x = Random.Range(0, 10);
            y = Random.value;
            z = Random.Range(0, 100).ToString();
        }
        [SerializeField] int x;
        [SerializeField] float y;
        [SerializeField] string z;
    }

    [System.Serializable]
    class Data
    {
        public void SetRandom(int id)
        {
            this.id = id;
            BoolValue = (Random.Range(0, 2) % 2) == 0;
            CharValue = (char)Random.Range(32, 127);
            sbyteValue = (sbyte)Random.Range(-0x7f, 0x7f);
            byteValue = (byte)Random.Range(0, 0xff);
            shortValue = (short)Random.Range(-0x7fff, 0x7fff);
            ushortValue = (ushort)Random.Range(0, 0xffff);
            intValue = (int)Random.Range(-0x7fffffff, 0x7fffffff);
            uintValue = (uint)Random.Range(0, 0x7fffffff);
            longValue = (long)Random.Range(-0x7fffffff, 0x7fffffff) | ((long)Random.Range(-0x7fffffff, 0x7fffffff) << 32);
            ulongValue = (ulong)Random.Range(0, 0x7fffffff);
            ulongValue |= (ulong)Random.Range(0, 0x7fffffff) << 32;
            floatValue = Random.value;
            doubleValue = (double)Random.value + ((double)Random.value / 16777216.0);
            stringValue = Random.value.ToString();
            nonSerializedTypeValue = new NonSerializedType();
            nonSerializedTypeValue.SetRandom();
            serializedTypeValue = new SerializedType();
            serializedTypeValue.SetRandom();
            arrayValue = new int[Random.Range(1, 5)];
            for (int i = 0; i < arrayValue.Length; i++)
            {
                arrayValue[i] = Random.Range(-5, 5);
            }
            listValue = new List<int>();
            var n = Random.Range(1, 5);
            for (int i = 0; i < n; i++)
            {
                listValue.Add(Random.Range(-5, 5));
            }
            nonSerializedPrivateField = 20;
            nonSerializedPublicField = 30;
        }
        public int id;
        [Newtonsoft.Json.JsonProperty("boolValue")] public bool BoolValue { get; set; }
        [Newtonsoft.Json.JsonProperty("charValue")] public char CharValue { get; set; }
        public sbyte sbyteValue;
        public byte byteValue;
        public short shortValue;
        public ushort ushortValue;
        public int intValue;
        public uint uintValue;
        public long longValue;
        public ulong ulongValue;
        public float floatValue;
        [SerializeField] double doubleValue;
        [SerializeField] string stringValue;
        public NonSerializedType nonSerializedTypeValue;
        public SerializedType serializedTypeValue;
        public int[] arrayValue;
        public List<int> listValue;
        int nonSerializedPrivateField;
        [System.NonSerialized] public int nonSerializedPublicField;
    }


    // Start is called before the first frame update
    void Start()
    {
        var dataArray = new Data[10];
        for (int i = 0; i < dataArray.Length; i++)
        {
            dataArray[i] = new Data();
            dataArray[i].SetRandom(i);
        }
        var csv = Kayac.CsvSerializer.ToCsv(dataArray);
        System.IO.File.WriteAllText("serialized.csv", csv);
        var deserialized = Kayac.CsvSerializer.FromCsv<Data>(csv);
        var csv2 = Kayac.CsvSerializer.ToCsv(deserialized);
        System.IO.File.WriteAllText("serialized2.csv", csv2);
        Debug.Assert(csv == csv2);
    }
}
