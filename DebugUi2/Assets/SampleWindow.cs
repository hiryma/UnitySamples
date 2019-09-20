using UnityEngine;
using System.Collections.Generic;
using Kayac.Debug;
using Kayac.Debug.Ui;

public class SampleWindow : Window
{
    readonly LogWindow log;
    readonly Graph graph;
    readonly FrameTimeWatcher frameTimeWatcher;

    public SampleWindow(DebugUiManager manager) : base(manager, "SampleWindow")
    {
        frameTimeWatcher = new FrameTimeWatcher();

        var button = new Button("ボタン", 100f)
        {
            OnClick = () =>
            {
                log.Add("ボタンが押された!");
            }
        };
        AddAuto(button);

        var toggleGroup = new ToggleGroup();
        var toggles = new Toggle[2];
        toggles[0] = new Toggle("トグルA", 100f, 50f, toggleGroup)
        {
            OnChangeToOn = () =>
            {
                log.Add("Aが有効になった");

            }
        };
        AddAuto(toggles[0]);
        toggles[1] = new Toggle("トグルB", 100f, 50f, toggleGroup)
        {
            OnChangeToOn = () =>
            {
                log.Add("Bが有効になった");
            }
        };
        AddAuto(toggles[1]);

        var text = new Text("テキスト", fontSize: 20f, width: 80f, height: 25f);
        AddAuto(text);

        BreakLine();

        log = new LogWindow(
            fontSize: 20f,
            width: 600f,
            height: 220f,
            borderEnabled: true,
            captureUnityLog: true); // Unityのログも出しちゃうよ
        AddAuto(log);

        graph = new Graph(5, 200f, 220f);
        graph.AddSeries(new Color32(255, 64, 64, 255));
        AddAuto(graph);

        BreakLine();

        var frameTimeGauge = new FrameTimeGauge(200f, 30f, frameTimeWatcher);
        AddAuto(frameTimeGauge);

        var slider = new Slider("スライダー", -100f, 100f, 400f);
        slider.OnDragEnd = () =>
        {
            log.Add("スライダーが" + slider.Value + "に変更された");
        };
        AddAuto(slider);

        BreakLine();

        var table = new Table(
            16f,
            new List<float>() { 80f, 80f, 120f },
            3,
            20f);
        table.Cells[0, 0] = "列A";
        table.Cells[0, 1] = "列B";
        table.Cells[0, 2] = "列C";
        table.Cells[1, 0] = "データ10";
        table.Cells[1, 1] = "データ11";
        table.Cells[1, 2] = "データ12";
        table.Cells[2, 0] = "データ20";
        table.Cells[2, 1] = "データ21";
        table.Cells[2, 2] = "データ23";
        AddAuto(table);

        FitSize();
    }

    public override void UpdateWindow()
    {
        frameTimeWatcher.Update();
        graph.AddData(0, frameTimeWatcher.Fps);
    }
}
