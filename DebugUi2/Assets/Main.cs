using UnityEngine;
using Kayac.DebugUi;

public class Main : MonoBehaviour
{
    [SerializeField] Camera mainCamera;
    [SerializeField] Kayac.DebugUiManager debugUiManagerPrefab;
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

        var subB = new SubMenu("SubA", 100, 40, Direction.Right);
        for (var i = 0; i < 10; i++)
        {
            subB.AddItem("B" + i.ToString(), () => Debug.Log("B" + i.ToString()));
        }

        subA.AddSubMenu(subB, Direction.Right);
        for (var i = 0; i < 10; i++)
        {
            subA.AddItem("A" + i.ToString(), () => Debug.Log("A" + i.ToString()));
        }

        menu.AddSubMenu(subA, Direction.Down);
        for (var i = 0; i < 10; i++)
        {
            menu.AddItem(i.ToString(), () => Debug.Log(i.ToString()));
        }
        manager.Add(menu, 0, 0);
    }
}
