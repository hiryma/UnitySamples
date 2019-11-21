using UnityEngine;
using System;

public class Main : MonoBehaviour
{
    const int count = 14;
    int index;
    string message;
    GameObject instance;
    [SerializeField] GameObject prefab;

    void OnGUI()
    {
        GUI.Label(new Rect(0f, 0f, 600f, 50f), message);
        if (GUI.Button(new Rect(0f, 100f, 200f, 100f), "Instantiate" + index))
        {
            Resources.UnloadUnusedAssets();
            if (instance)
            {
                Destroy(instance);
            }
            var t0 = DateTime.Now;
            prefab = Resources.Load<GameObject>(index.ToString());
            var t1 = DateTime.Now;
            instance = Instantiate(prefab, transform, false);
            var t2 = DateTime.Now;

            var msPerTick = 1000.0 / (double)TimeSpan.TicksPerSecond;
            var t10 = (double)(t1 - t0).Ticks * msPerTick;
            var t21 = (double)(t2 - t1).Ticks * msPerTick;
            message = index + " : Load: " + t10.ToString("F3") + " Instantiate: " + t21.ToString("F3");
            index++;
            if (index >= count)
            {
                index = 0;
            }
        }
    }
}
