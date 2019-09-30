using UnityEngine;
using UnityEngine.UI;

public class Main : MonoBehaviour
{
    [SerializeField] Ui.Row[] uiRows;
    [SerializeField] float timeScale = 1f;
    [SerializeField] Text resultText;

    Logic.State state;
    Logic.Updator updator;
    
    void Start()
    {
        updator = new Logic.Updator();
        Restart();
    }

    void Restart()
    {
        state = new Logic.State();
        updator.Start(state);
    }

    public void OnClickResultText()
    {
        Restart();
    }

    void Update()
    {
        if (state.winnerPlayerIndex >= 0)
        {
            resultText.enabled = true;
            resultText.text = string.Format("Winner is player{0}", state.winnerPlayerIndex);
        }
        else
        {
            resultText.enabled = false;
        }
        updator.Update(state, Time.deltaTime * timeScale);

        int dragged = -1;
        int dropped = -1;
        int clicked = -1;
        int id = 0;
        for (int i = 0; i < uiRows.Length; i++)
        {
            var row = uiRows[i];
            for (int j = 0; j < row.Length; j++)
            {
                var cell = row[j];
                if (cell.Clicked)
                {
                    clicked = id;
                }
                if (cell.Dragged)
                {
                    dragged = id;
                }
                if (cell.Dropped)
                {
                    dropped = id;
                }
                var logicPiece = state.board[id % 4, id / 4];
                cell.ManualUpdate(logicPiece);
                id++;
            }
        }
        if ((dragged >= 0) || (clicked >= 0) || (dropped >= 0))
        {
            Debug.Log("Click " + clicked + " Drag: " + dragged + " -> " + dropped);
            var action = new Logic.Action();
            var piece = state.board[dragged % 4, dragged / 4];
            action.x = dragged % 4;
            action.y = dragged / 4;
            action.moveToX = dropped % 4;
            action.moveToY = dropped / 4;
            state.players[piece.playerIndex].EnqueueAction(action);
        }
    }
}
