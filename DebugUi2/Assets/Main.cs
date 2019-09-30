using UnityEngine;
using Kayac.Debug;
using Kayac.Debug.Ui;

public class Main : MonoBehaviour
{
    [SerializeField] Camera mainCamera;
    [SerializeField] DebugUiManager debugUiManagerPrefab;
    SampleWindow sampleWindow;
    Menu menu;

    void Start()
    {
        // プレハブをInstantiate
        var manager = Instantiate(debugUiManagerPrefab, null, false);
        // 描画カメラを指定して手動で開始
        manager.ManualStart(mainCamera);
        sampleWindow = new SampleWindow(manager);
        manager.Add(sampleWindow, 0, 0, AlignX.Right, AlignY.Bottom);

        menu = new Menu(100, 40);
        var subA = new SubMenu("SubA", 100, 40, Direction.Down);
        subA.AddItem("A1", () => Debug.Log("A1"));
        subA.AddItem("A2", () => Debug.Log("A2"));
        var subB = new SubMenu("SubA", 100, 40, Direction.Down);
        subB.AddItem("B1", () => Debug.Log("B1"));
        subB.AddItem("B2", () => Debug.Log("B2"));
        subA.AddSubMenu(subB, Direction.Right);
        menu.AddSubMenu(subA, Direction.Down);
        menu.AddItem("1", () => Debug.Log("1"));
        menu.AddItem("2", () => Debug.Log("2"));
        manager.Add(menu, 0, 0);
    }
}
