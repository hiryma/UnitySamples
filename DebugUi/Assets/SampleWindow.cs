using UnityEngine;
using Kayac;
using System.Collections;
using System.Collections.Generic;

namespace Kayac
{
	public class SampleWindow : DebugUiWindow
	{
		DebugUiLogWindow _log;
		DebugUiGraph _graph;
		FrameTimeWatcher _frameTimeWatcher;

		public SampleWindow(DebugUiManager manager) : base(manager, "SampleWindow")
		{
			_frameTimeWatcher = new FrameTimeWatcher();

			var button = new DebugUiButton("ボタン", 100f);
			button.onClick = () =>
			{
				_log.Add("ボタンが押された!");
			};
			AddAuto(button);

			var toggleGroup = new DebugUiToggleGroup();
			var toggles = new DebugUiToggle[2];
			toggles[0] = new DebugUiToggle("トグルA", 100f, 50f, toggleGroup);
			toggles[0].onChangeToOn = () =>
			{
				_log.Add("Aが有効になった");

			};
			AddAuto(toggles[0]);
			toggles[1] = new DebugUiToggle("トグルB", 100f, 50f, toggleGroup);
			toggles[1].onChangeToOn = () =>
			{
				_log.Add("Bが有効になった");
			};
			AddAuto(toggles[1]);

			var text = new DebugUiText("テキスト", fontSize: 20f, width: 80f, height: 25f);
			AddAuto(text);

			BreakLine();

			_log = new DebugUiLogWindow(
				fontSize: 20f,
				width: 600f,
				height: 220f,
				borderEnabled: true,
				captureUnityLog: true); // Unityのログも出しちゃうよ
			AddAuto(_log);

			_graph = new DebugUiGraph(5, 200f, 220f);
			_graph.AddSeries(new Color32(255, 64, 64, 255));
			AddAuto(_graph);

			BreakLine();

			var frameTimeGauge = new FrameTimeGauge(200f, 30f, _frameTimeWatcher);
			AddAuto(frameTimeGauge);

			var slider = new DebugUiSlider("スライダー", -100f, 100f, 400f);
			slider.onDragEnd = () =>
			{
				_log.Add("スライダーが" + slider.value + "に変更された");
			};
			AddAuto(slider);

			BreakLine();

			var table = new DebugUiTable(
				16f,
				new List<float>(){ 80f, 80f, 120f },
				3,
				20f);
			table.cells[0, 0] = "列A";
			table.cells[0, 1] = "列B";
			table.cells[0, 2] = "列C";
			table.cells[1, 0] = "データ10";
			table.cells[1, 1] = "データ11";
			table.cells[1, 2] = "データ12";
			table.cells[2, 0] = "データ20";
			table.cells[2, 1] = "データ21";
			table.cells[2, 2] = "データ23";
			AddAuto(table);

			FitSize();
		}

		public override void UpdateWindow()
		{
			_frameTimeWatcher.Update();
			_graph.AddData(0, _frameTimeWatcher.fps);
		}
	}
}
