using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainScript : MonoBehaviour
{
	[SerializeField]
	Transform _cellsRoot;
	[SerializeField]
	UnityEngine.UI.Text _titleText;
	[SerializeField]
	UnityEngine.UI.Text _moneyText;
	[SerializeField]
	float _InitialFallInterval = 1f;

	// 設定
	int _blockSize = 4;
	bool _cornerAllowed = false;
	int _width = 8;
	int _height = 16;
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
	List<UnityEngine.UI.Image> _cellImages;

	void Start()
	{
		_macopay = new Macopay("2p32moff24erpn3s5e58vfpt06otjv36", "http://localhost:8080/");
		_moneyText.text = "unknown";
		_cellImages = new List<UnityEngine.UI.Image>();
		StartTitle();
		// お金取得開始
		StartCoroutine(_macopay.CoAcount(OnMacopayApiComplete));
	}

	void StartTitle()
	{
		_subScene = SubScene.Title;
		_titleText.text = "";
		_titleText.fontSize = 20;
		for (int i = 0; i < _cellImages.Count; i++)
		{
			_cellImages[i].enabled = false;
		}
	}

	void StartGameOver()
	{
		_subScene = SubScene.GameOver;
		_titleText.text = "<color=#ff0000>GameOver</color>";
		_titleText.fontSize = 40;
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
		if (Input.anyKeyDown)
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
		int imageIndex = 0;
		Color color;

		for (int y = 1; y < _height; y++)
		{
			// 左壁
			DrawCell(0, y, new Color(0.7f, 0.7f, 0.7f, 1f), cellSize, imageIndex);
			imageIndex++;
			// 右壁
			DrawCell(renderWidth - 1, y, new Color(0.7f, 0.7f, 0.7f, 1f), cellSize, imageIndex);
			imageIndex++;
		}

		// 底
		for (int x = 0; x < renderWidth; x++)
		{
			DrawCell(x, 0, new Color(0.7f, 0.7f, 0.7f, 1f), cellSize, imageIndex);
			imageIndex++;
		}

		// 積もったマス
		for (int y = 0; y < _height; y++)
		{
			for (int x = 0; x < _width; x++)
			{
				if (_puzzle.IsVacant(x, y))
				{
					color = new Color(0f, 0f, 0f, 0f);
				}
				else
				{
					color = new Color(1f, 1f, 1f, 1f);
				}
				DrawCell(x + 1, y + 1, color, cellSize, imageIndex);
				imageIndex++;
			}
		}

		var activeCells = _puzzle.GetActiveCells();
		foreach (var cell in activeCells)
		{
			DrawCell(cell.x + 1, cell.y + 1, new Color(1f, 0f, 0f, 1f), cellSize, imageIndex);
			imageIndex++;
		}

		// 余りを無効化
		for (int i = imageIndex; i < _cellImages.Count; i++)
		{
			_cellImages[i].enabled = false;
		}
	}

	void DrawCell(int x, int y, Color color, float cellSize, int imageIndex)
	{
		UnityEngine.UI.Image image = null;
		if (imageIndex >= _cellImages.Count)
		{
			var gameObject = new GameObject("cellImage");
			image = gameObject.AddComponent<UnityEngine.UI.Image>();
			_cellImages.Add(image);
			image.rectTransform.SetParent(_cellsRoot, false);
		}
		else
		{
			image = _cellImages[imageIndex];
		}
		var xOffset = (cellSize * 0.5f) - 216f;
		var yOffset = (cellSize * 0.5f) - 384f;
		image.enabled = true;
		image.color = color;
		image.rectTransform.anchoredPosition = new Vector2(xOffset + (x * cellSize), yOffset + (y * cellSize));
		image.rectTransform.sizeDelta = new Vector2(cellSize - 2f, cellSize - 2f);
	}
}
