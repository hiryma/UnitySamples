using UnityEngine;
using System;

public class Main : MonoBehaviour
{
    int callDepth;
    TimeSpan time;
    int count;

    void OnGUI()
    {
        var us = (double)time.Ticks / (double)(TimeSpan.TicksPerMillisecond * count);
        GUI.Label(new Rect(0f, 0f, 400f, 50f), us.ToString("F3"));
        var newCallDepth = (int)GUI.HorizontalSlider(new Rect(0f, 50f, 400f, 50f), (float)callDepth, 0, 200);
        if (newCallDepth != callDepth)
        {
            callDepth = newCallDepth;
            count = 0;
            time = TimeSpan.FromTicks(0);
        }
    }

    private void Update()
    {
        var t0 = DateTime.Now;
        LogAtDepth(callDepth, 0);
        var t1 = DateTime.Now;
        time += (t1 - t0);
        count++;
    }

    void LogAtDepth(int targetDepth, int currentDepth)
    {
        if (currentDepth >= targetDepth)
        {
            Debug.Log("Log() called " + targetDepth);
        }
        else
        {
            LogAtDepth(targetDepth, currentDepth + 1);
        }
    }
}
