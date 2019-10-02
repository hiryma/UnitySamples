using System.Collections.Generic;
using System;

namespace Logic
{

    public class Updator
    {
        PieceParameters[] pieceParams;
        public Updator()
        {
            pieceParams = new PieceParameters[(int)Piece.Type.Count];
            pieceParams[(int)Piece.Type.Lion] = new PieceParameters()
            {
                attack = 10f,
                hp = 100f,
            };
            pieceParams[(int)Piece.Type.Giraffe] = new PieceParameters()
            {
                attack = 20f,
                hp = 100f,
            };
            pieceParams[(int)Piece.Type.Elephant] = new PieceParameters()
            {
                attack = 15f,
                hp = 150f,
            };
            pieceParams[(int)Piece.Type.Chick] = new PieceParameters()
            {
                attack = 30f,
                hp = 70f,
            };
        }

        public void Start(State state)
        {
            state.players = new Player[2];
            state.players[0] = new Player();
            state.players[1] = new Player();
            StartPlayer(state.players[0], 0);
            StartPlayer(state.players[1], 1);
            state.board = new Piece[4, 3];
            PutPiece(state, 0, 0, 0, 1);
            PutPiece(state, 0, 1, 0, 0);
            PutPiece(state, 0, 2, 0, 2);
            PutPiece(state, 1, 1, 0, 3);
            PutPiece(state, 3, 2, 1, 1);
            PutPiece(state, 3, 1, 1, 0);
            PutPiece(state, 3, 0, 1, 2);
            PutPiece(state, 2, 1, 1, 3);
        }

        void PutPiece(State state, int x, int y, int playerIndex, int pieceIndex)
        {
            var piece = state.players[playerIndex].pieces[pieceIndex];
            state.board[x, y] = piece;
            piece.moveToX = x;
            piece.moveToY = y;
        }

        void StartPlayer(Player player, int index)
        {
            player.pieces = new List<Piece>();
            player.actions = new Queue<Action>();
            player.pieces.Add(new Piece()
            {
                type = Piece.Type.Lion,
                hp = pieceParams[(int)Piece.Type.Lion].hp,
                maxHp = pieceParams[(int)Piece.Type.Lion].hp,
                playerIndex = index,
                onBoard = true,
            });
            player.pieces.Add(new Piece()
            {
                type = Piece.Type.Elephant,
                hp = pieceParams[(int)Piece.Type.Elephant].hp,
                maxHp = pieceParams[(int)Piece.Type.Elephant].hp,
                playerIndex = index,
                onBoard = true,
            });
            player.pieces.Add(new Piece()
            {
                type = Piece.Type.Giraffe,
                hp = pieceParams[(int)Piece.Type.Giraffe].hp,
                maxHp = pieceParams[(int)Piece.Type.Giraffe].hp,
                playerIndex = index,
                onBoard = true,
            });
            player.pieces.Add(new Piece()
            {
                type = Piece.Type.Chick,
                hp = pieceParams[(int)Piece.Type.Chick].hp,
                maxHp = pieceParams[(int)Piece.Type.Chick].hp,
                playerIndex = index,
                onBoard = true,
            });
        }

        Action PopAction(State state, int playerIndex)
        {
            var action = state.players[playerIndex].DequeueAction();
            if (action == null)
            {
                return null;
            }
            return ValidateAction(state, action, playerIndex) ? action : null;
        }

        bool ValidateAction(State state, Action action, int playerIndex)
        {
            // そもそも動かす駒がないぞ
            if (action.piece == null)
            {
                return false;
            }
            // それは自分のか?
            if (action.piece.playerIndex != playerIndex)
            {
                UnityEngine.Debug.Log("D");
                return false;
            }
            // 行き先は範囲内か?
            int toX = action.moveToX;
            int toY = action.moveToY;
            if ((toX < 0) || (toX >= state.board.GetLength(0)))
            {
                UnityEngine.Debug.Log("E");
                return false;
            }
            if ((toY < 0) || (toY >= state.board.GetLength(1)))
            {
                UnityEngine.Debug.Log("F");
                return false;
            }
            // それはこのタイプで移動可能か?
            int x, y;
            FindPiece(out x, out y, state, action.piece);
            int dx = toX - x;
            int dy = toY - y;
            if (!CheckMovable(action.piece, dx, dy))
            {
                return false;
            }
            return true; 
        }

        void FindPiece(out int xOut, out int yOut, State state, Piece piece)
        {
            // 現行動解決
            for (int x = 0; x < 4; x++)
            {
                for (int y = 0; y < 3; y++)
                {
                    if (state.board[x, y] == piece)
                    {
                        xOut = x;
                        yOut = y;
                        return;
                    }
                }
            }
            xOut = yOut = int.MinValue;
        }

        public bool CheckMovable(Piece piece, int dx, int dy)
        {
            // 自マスは常に可
            if ((dx == 0) && (dy == 0))
            {
                return true;
            }

            var type = piece.type;
            var ret = false;
            switch (type)
            {
                case Piece.Type.Lion:
                    ret = ((dx * dx) + (dy * dy)) <= 2;
                    break;
                case Piece.Type.Giraffe:
                    if (dx == 0) //yのみ+-1
                    {
                        ret =Math.Abs(dy) == 1;
                    }
                    else if (dy == 0) //xのみ+-1
                    {
                        ret = Math.Abs(dx) == 1;
                    }
                    break;
                case Piece.Type.Elephant:
                    ret = ((dx * dx) + (dy * dy)) == 2;
                    break;
                case Piece.Type.Chick:
                    if (piece.flipped)
                    {
                        // 全列挙
                        if (piece.playerIndex == 0)
                        {
                            if (dx == 1)
                            {
                                ret = (dy == -1) || (dy == 0) || (dy == 1);
                            }
                            else if (dx == 0)
                            {
                                ret = (dy == -1) || (dy == 1);
                            }
                            if (dx == -1)
                            {
                                ret = dy == 0;
                            }
                        }
                        else if (piece.playerIndex == 1)
                        {
                            if (dx == -1)
                            {
                                ret = (dy == -1) || (dy == 0) || (dy == 1);
                            }
                            else if (dx == 0)
                            {
                                ret = (dy == -1) || (dy == 1);
                            }
                            else if (dx == 1)
                            {
                                ret = dy == 0;
                            }
                        }
                    }
                    else
                    {
                        if (piece.playerIndex == 0)
                        {
                            ret = (dx == 1) && (dy == 0);
                        }
                        else if (piece.playerIndex == 1)
                        {
                            ret = (dx == -1) && (dy == 0);
                        }
                    }
                    break;
            }
            UnityEngine.Debug.Log(piece.type + " " + dx + " " + dy + " " + ret);
            return ret;
        }

        void Update(State state, int x, int y, float deltaTime)
        {
            var piece = state.board[x, y];
            if (piece != null)
            {
                var toX = piece.moveToX;
                var toY = piece.moveToY;
                // 空いていれば移動
                var toPiece = state.board[toX, toY];
                if (toPiece == null)
                {
                    state.board[x, y] = null;
                    state.board[toX, toY] = piece;
                }
                else // 誰かいるなら攻撃
                {
                    if (toPiece.playerIndex != piece.playerIndex) // 敵なら攻撃を行う
                    {
                        toPiece.hp -= pieceParams[(int)piece.type].attack * deltaTime;
                        if (toPiece.hp <= 0f) // 死亡
                        {
                            if (toPiece.type == Piece.Type.Lion) // 勝敗確定
                            {
                                state.winnerPlayerIndex = piece.playerIndex;
                            }
                            else
                            {
                                toPiece.playerIndex = 1 - toPiece.playerIndex; // 寝返り
                                toPiece.hp = pieceParams[(int)toPiece.type].hp; // 全快
                                toPiece.onBoard = false;
                                state.board[x, y] = null; // 移動
                                state.board[toX, toY] = piece;
                            }
                        }
                    }
                }
            }

        }

        public void Update(State state, float deltaTime)
        {
            if (state.winnerPlayerIndex >= 0)
            {
                return;
            }
            // 現行動解決
            for (int x = 0; x < 4; x++)
            {
                for (int y = 0; y < 3; y++)
                {
                    Update(state, x, y, deltaTime);
                }
            }

            // 一回に1つの行動しか処理しない
            Action action0 = PopAction(state, 0);
            Action action1 = PopAction(state, 1);
            Action action;
            if (action0 == null)
            {
                action = action1;
            }
            else if (action1 == null)
            {
                action = action0;
            }
            else if (action0.time < action1.time)
            {
                action = action0;
            }
            else if (action0.time > action1.time)
            {
                action = action1;
            }
            else
            {
                action = (UnityEngine.Random.value < 0.5) ? action0 : action1;
            }
            // 実行するactionが決定
            if (action != null)
            {
                var fromPiece = action.piece;
                fromPiece.moveToX = action.moveToX;
                fromPiece.moveToY = action.moveToY;
            }
        }
    }
}