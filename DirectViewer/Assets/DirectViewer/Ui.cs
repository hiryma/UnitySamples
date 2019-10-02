using UnityEngine;
using Kayac;
using Kayac.Debug;
using Kayac.Debug.Ui;

public class Ui
{
    LogWindow logWindow;
    Menu menu;
    Button resoButton;
    Button ipButton;
    int initialWidth;
    int initialHeight;

    public Ui(
        DebugUiManager manager,
        int serverPort,
        System.Action onClickPlay,
        System.Action onClickStop,
        System.Action onClickNext)
    {
        initialWidth = Screen.width;
        initialHeight = Screen.height;

        manager.ManualStart();
        // ログできるだけ速く欲しいので、こいつのコンストラクトだけ先にやる
        logWindow = new LogWindow(12f, 400f, 736f, borderEnabled: true, captureUnityLog: true);
        logWindow.Enabled = false;

        var frameTimeGauge = new FrameTimeGauge(100f, 15f, null);
        manager.Add(frameTimeGauge, 0f, 0f, AlignX.Right, AlignY.Bottom);
        menu = new Menu(100f, 40f, Direction.Down, "DebugMenu");
        manager.Add(menu);
        menu.AddItem("LogWindow", () =>
        {
            logWindow.Enabled = !logWindow.Enabled;
        });
        manager.Add(logWindow, 0f, 0f, AlignX.Center, AlignY.Center);

        var ip = DebugServerUtil.GetLanIpAddress();
        if (ip == null)
        {
            ip = "NO NETWORK";
        }
        ipButton = menu.AddItem(ip, () =>
        {
            var url = string.Format("http://{0}:{1}/", DebugServerUtil.GetLanIpAddress(), serverPort);
            Application.OpenURL(url);
        });
        menu.AddItem("Play", onClickPlay);
        menu.AddItem("Stop", onClickStop);
        menu.AddItem("Next", onClickNext);
        var text = string.Format("{0}x{1}", Screen.width, Screen.height);
        resoButton = menu.AddItem(text, () =>
        {
            // sqrt(0.5)倍していく。描画面積は半分になる
            var w = Screen.width * 707 / 1000;
            var h = Screen.height * 707 / 1000;
            if (w < (16 * 9))
            {
                w = initialWidth;
                h = initialHeight;
            }
            Screen.SetResolution(w, h, false, 60);
            resoButton.Text = w.ToString() + "x" + h.ToString();
        });
    }

    public void ManualUpdate()
    {
        if (Application.internetReachability == UnityEngine.NetworkReachability.NotReachable)
        {
            ipButton.Text = "NO NETWORK";
        }
        else if ((Time.frameCount % 64) == 0)
        {
            var ip = DebugServerUtil.GetLanIpAddress();
            if (ip == null)
            {
                ipButton.Text = "NO WiFi";
            }
            else
            {
                ipButton.Text = ip;
            }
        }
    }
}
