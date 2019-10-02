using System.Collections.Generic;
using System;

namespace Logic
{
    public class PieceParameters
    {
        public float attack; // 秒間あたりの攻撃力
        public float hp; // 体力
    }
    public class Piece
    {
        public enum Type
        {
            Lion = 0,
            Giraffe,
            Elephant,
            Chick,
            Count,
        }
        public Type type;
        public float hp;
        public float maxHp;
        public int playerIndex;
        public bool flipped; // 成
        public int moveToX;
        public int moveToY;
        public bool onBoard;
    }

    public class Player
    {
        public void EnqueueAction(Action action)
        {
            action.time = DateTime.Now;
            actions.Enqueue(action);
        }
        public Action DequeueAction()
        {
            if (actions.Count == 0)
            {
                return null;
            }
            else
            {
                return actions.Dequeue();
            }
        }

        public List<Piece> pieces;
        public float actionTimer;
        public Queue<Action> actions;
    }

    public class State
    {
        public Piece[,] board;
        public Player[] players;
        public int winnerPlayerIndex = -1;
    }

    public class Action
    {
        public Piece piece;
        public int moveToX;
        public int moveToY;
        public DateTime time;
    }
}