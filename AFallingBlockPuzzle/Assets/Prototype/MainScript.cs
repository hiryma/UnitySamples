using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainScript : MonoBehaviour
{
	[SerializeField]
	Transform _cellsRoot;
	[SerializeField]
	TextMesh _titleText;
	[SerializeField]
	TextMesh _moneyText;
	[SerializeField]
	GameObject _cellPrefab;
	[SerializeField]
	float _InitialFallInterval = 1f;

	// 設定
	int _blockSize = 4;
	bool _cornerAllowed = true;
	int _width = 10;
	int _height = 20;
	int _settingPosition = 0;
	IFallingBlockPuzzle _puzzle;
	enum SubScene
	{
		Title,
		Game,
		GameOver,
	}
	SubScene _subScene;
	Macopay _macopay;
	List<GameObject> _cells;

	void Start()
	{
		_macopay = new Macopay("2p32moff24erpn3s5e58vfpt06otjv36", "http://localhost:8080/");
		_moneyText.text = "unknown";
		_cells = new List<GameObject>();
		StartTitle();
		// お金取得開始
		StartCoroutine(_macopay.CoAcount(OnMacopayApiComplete));
	}

	void StartTitle()
	{
		_subScene = SubScene.Title;
		_titleText.text = "";
		for (int i = 0; i < _cells.Count; i++)
		{
			_cells[i].SetActive(false);
		}
	}

	void StartGameOver()
	{
		_subScene = SubScene.GameOver;
		_titleText.text = "<color=#ff0000>GameOver</color>";
	}

	void StartGame()
	{
		_puzzle = new AFallingBlockPuzzle(_width, _height, new int[]{ _blockSize }, _cornerAllowed);
		_subScene = SubScene.Game;
		_titleText.text = "";
		_puzzle.Reset();
	}

	void OnMacopayApiComplete()
	{
		_moneyText.text = _macopay.currency.ToString();
	}

	void Update()
	{
		switch (_subScene)
		{
			case SubScene.Title: UpdateTitle(); break;
			case SubScene.Game: UpdateGame(); break;
			case SubScene.GameOver: UpdateGameOver(); break;
		}
	}

	void UpdateTitle()
	{
		if (Input.GetKeyDown(KeyCode.UpArrow))
		{
			_settingPosition--;
			if (_settingPosition < 0)
			{
				_settingPosition = 3;
			}
		}
		if (Input.GetKeyDown(KeyCode.DownArrow))
		{
			_settingPosition++;
			if (_settingPosition >= 4)
			{
				_settingPosition = 0;
			}
		}

		int settingChange = 0;
		if (Input.GetKeyDown(KeyCode.LeftArrow))
		{
			settingChange = -1;
		}
		if (Input.GetKeyDown(KeyCode.RightArrow))
		{
			settingChange = 1;
		}
		if (settingChange != 0)
		{
			if (_settingPosition == 0)
			{
				_blockSize += settingChange;
				if (_blockSize < 1)
				{
					_blockSize = 1;
				}
				else if (_blockSize >= 8)
				{
					_blockSize = 8;
				}
				if (_width < _blockSize)
				{
					_width = _blockSize;
				}
				if (_height < _blockSize)
				{
					_height = _blockSize;
				}
			}
			else if (_settingPosition == 1)
			{
				if (settingChange > 0)
				{
					_cornerAllowed = true;
				}
				else
				{
					_cornerAllowed = false;
				}
			}
			else if (_settingPosition == 2)
			{
				_width += settingChange;
				if (_width < _blockSize)
				{
					_width = _blockSize;
				}
				else if (_width >= 20)
				{
					_width = 20;
				}
			}
			else if (_settingPosition == 3)
			{
				_height += settingChange;
				if (_height < _blockSize)
				{
					_height = _blockSize;
				}
				else if (_height >= 40)
				{
					_height = 40;
				}
			}
		}
		// 描画
		var text = "A Falling Block Puzzle\n";
		if (_settingPosition == 0)
		{
			text += "<color=#ff0000>";
		}
		else
		{
			text += "<color=#ffffff>";
		}
		text += "blockSize: " + _blockSize + "</color>\n";
		if (_settingPosition == 1)
		{
			text += "<color=#ff0000>";
		}
		else
		{
			text += "<color=#ffffff>";
		}
		text += "cornerAllowed: " + _cornerAllowed + "</color>\n";
		if (_settingPosition == 2)
		{
			text += "<color=#ff0000>";
		}
		else
		{
			text += "<color=#ffffff>";
		}
		text += "width: " + _width + "</color>\n";
		if (_settingPosition == 3)
		{
			text += "<color=#ff0000>";
		}
		else
		{
			text += "<color=#ffffff>";
		}
		text += "height: " + _height + "</color>\n";
		_titleText.text = text;

		if (Input.GetKeyDown(KeyCode.Return))
		{
			StartGame();
		}
	}

	void UpdateGameOver()
	{
		if (Input.GetKeyDown(KeyCode.Return))
		{
			StartTitle();
		}
	}

	void UpdateGame()
	{
		_puzzle.fallInterval = _InitialFallInterval;
		if (Input.GetKeyDown(KeyCode.LeftArrow))
		{
			_puzzle.MoveLeft();
		}
		else if (Input.GetKeyDown(KeyCode.RightArrow))
		{
			_puzzle.MoveRight();
		}

		if (Input.GetKeyDown(KeyCode.UpArrow))
		{
			_puzzle.RotateClockwise();
		}

		if (Input.GetKeyDown(KeyCode.DownArrow))
		{
			_puzzle.MoveDown();
		}
		else if (Input.GetKeyDown(KeyCode.Return))
		{
			_puzzle.Land();
		}
		_puzzle.Update(UnityEngine.Time.deltaTime);
		DrawGame();
		if (_puzzle.isGameOver)
		{
			StartGameOver();
		}

		var erasedLines = _puzzle.erasedYList.Count;
		if (erasedLines > 0) // 消えた!
		{
			var charge = erasedLines * erasedLines * 100;
			StartCoroutine(_macopay.CoWalletCurrency(charge, OnMacopayApiComplete));
		}
	}

	// 以下描画関連
	void DrawGame()
	{
		var renderWidth = _width + 2; // 壁
		var renderHeight = _height + 1; // 底
		var cellSize = Mathf.Min(432f / renderWidth, 768f / renderHeight);
		int cellIndex = 0;

		for (int y = 1; y < _height; y++)
		{
			// 左壁
			DrawCell(0, y, cellSize, cellIndex);
			cellIndex++;
			// 右壁
			DrawCell(renderWidth - 1, y, cellSize, cellIndex);
			cellIndex++;
		}

		// 底
		for (int x = 0; x < renderWidth; x++)
		{
			DrawCell(x, 0, cellSize, cellIndex);
			cellIndex++;
		}

		// 積もったマス
		for (int y = 0; y < _height; y++)
		{
			for (int x = 0; x < _width; x++)
			{
				if (!_puzzle.IsVacant(x, y))
				{
					DrawCell(x + 1, y + 1, cellSize, cellIndex);
					cellIndex++;
				}
			}
		}

		var activeCells = _puzzle.GetActiveCells();
		foreach (var cell in activeCells)
		{
			DrawCell(cell.x + 1, cell.y + 1, cellSize, cellIndex);
			cellIndex++;
		}

		// 余りを無効化
		for (int i = cellIndex; i < _cells.Count; i++)
		{
			_cells[i].SetActive(false);
		}
	}

	void DrawCell(int x, int y, float cellSize, int cellIndex)
	{
		GameObject cell = null;
		if (cellIndex >= _cells.Count)
		{
			cell = Instantiate(_cellPrefab, _cellsRoot, false);
			_cells.Add(cell);
		}
		else
		{
			cell = _cells[cellIndex];
		}
		var xOffset = (cellSize * 0.5f) - 216f;
		var yOffset = (cellSize * 0.5f) - 384f;
		cell.SetActive(true);
		cell.transform.localScale = new Vector3(cellSize - 2f, 1f, cellSize - 2f);
		cell.transform.localPosition = new Vector3(xOffset + (x * cellSize), yOffset + (y * cellSize), 0f);
	}
}
