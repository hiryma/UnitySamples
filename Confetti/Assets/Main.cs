using UnityEngine;

public class Main : MonoBehaviour
{
    [SerializeField] Confetti confetti;

    float ShowLogSlider(string name, float value, float y, bool isInteger, float logMin, float logMax)
    {
        var format = isInteger ? "{0}: {1:F0}" : "{0}: {1:F2}";
        var label = string.Format(format, name, value);
        GUI.Label(new Rect(0f, y, 120f, 30f), label);

        var logValue = Mathf.Log10(value);
        var newLogValue = GUI.HorizontalSlider(
            new Rect(120f, y, 200f, 30f),
            logValue,
            logMin,
            logMax);
        return Mathf.Pow(10f, newLogValue);
    }

    void OnGUI()
    {
        int newPieceCount = (int)ShowLogSlider("WholeCount", (int)confetti.PieceCount, 0f, true, 0f, 4f);
        if (newPieceCount != (int)confetti.PieceCount)
        {
            confetti.ManualStart(newPieceCount);
            confetti.StartEmission();
        }

        confetti.EmitPiecePerSecond = ShowLogSlider("EmitSpeed", confetti.EmitPiecePerSecond, 30f, false, 0f, 3f);

        float gravity = ShowLogSlider("Gravity", -confetti.Gravity.y, 60f, false, -2f, 2f);
        confetti.Gravity = new Vector3(0f, -gravity, 0f);

        float wind = ShowLogSlider("Wind", confetti.Wind.y, 90f, false, -2f, 2f);
        confetti.Wind = new Vector3(wind, 0f, 0f);

        confetti.Resistance = ShowLogSlider("Resistance", confetti.Resistance, 120f, false, -3f, 2f);
        confetti.NormalBendRatio = ShowLogSlider("NormalBend", confetti.NormalBendRatio, 150f, false, -2f, 1f);
    }
}
