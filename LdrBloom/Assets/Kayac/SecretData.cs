//#define AS_SINGLETON
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System;

namespace Kayac
{
    public class SecretData
    {
#if AS_SINGLETON
        static SecretData instance;
        public static SecretData Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new SecretData();
                }
                return instance;
            }
        }
#endif
        public Exception Exception { get; private set; }

        public SecretData()
        {
            data = new Dictionary<string, string>();
        }

        public string Get(string name)
        {
            string ret;
            data.TryGetValue(name, out ret);
            return ret;
        }

        public IEnumerator CoLoad()
        {
            var streamingAssetsPath = Application.streamingAssetsPath;
            var url = string.Format("file:///" + streamingAssetsPath + "/Kayac/secretData_MUST_BE_GITIGNORED.json");
            var req = UnityWebRequest.Get(url);
            yield return req.SendWebRequest();
            if (req.error != null)
            {
                Exception = new Exception("Can't load json. This is not error on CloudBuild.");
            }
            else if (req.downloadHandler.text == null)
            {
                Exception = new Exception("some load error. BUG?");
            }
            else
            {
                var json = req.downloadHandler.text;
                var src = JsonUtility.FromJson<Data>(json);
                if (src == null)
                {
                    Exception = new Exception("json is invalid");
                }
                else if (src.items == null)
                {
                    Exception = new Exception("json does not contain 'items' field.");
                }
                else
                {
                    // 辞書化する
                    foreach (var item in src.items)
                    {
                        data.Add(item.name, item.value);
                    }
                }
            }
        }

        // non-public ---------------------
        [Serializable]
        class Item
        {
            public string name;
            public string value;
        }

        [Serializable]
        class Data
        {
            public Item[] items;
        }
        Dictionary<string, string> data;
    }
}