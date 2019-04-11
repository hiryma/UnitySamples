using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MainScript : MonoBehaviour
{
	[SerializeField]
	Transform _cellsRoot;
	[SerializeField]
	Text _titleText;
	[SerializeField]
	float _InitialFallInterval = 1f;
	[SerializeField]
	GameObject _cellPrefab;
	[SerializeField]
	TouchDetector _touchDetector;
	[SerializeField]
	Camera _camera3d;
	[SerializeField]
	CanvasScaler _canvasScaler;
	[SerializeField]
	Toggle _projectionToggle;
	[SerializeField]
	Toggle _modeToggle;
	[SerializeField]
	Toggle _benchmarkToggle;
	[SerializeField]
	Toggle _heavyModeToggle;
	[SerializeField]
	GameObject _controlUiRoot;
	[SerializeField]
	Benchmark _benchmark;
	[SerializeField]
	Text _fpsText;
	[SerializeField]
	RawImage _benchmarkImage;
	[SerializeField]
	Kayac.AzimuthalEquidistantProjector _projector;
	[SerializeField]
	Slider _fovSlider;
	[SerializeField]
	Toggle _zoomEffectToggle;

	// 設定
	int _width = 12;
	int _height = 20;
	IFallingBlockPuzzle _puzzle;
	enum SubScene
	{
		Demo,
		Title,
		Game,
		GameOver,
	}
	SubScene _subScene;
	List<GameObject> _cells;
	float _cameraRotationX;
	float _cameraRotationY;
	bool _left;
	bool _right;
	bool _rotation;
	bool _down;
	bool _fall;
	float _fovBackup;

	void Start()
	{
		_fovSlider.value = (_camera3d.fieldOfView - 1f) / 178f;
		_projectionToggle.onValueChanged.AddListener((unused) =>
		{
			if (_projectionToggle.isOn)
			{
				_projector.enabled = true;
			}
			else
			{
				_projector.enabled = false;
			}
		});
		_modeToggle.onValueChanged.AddListener((unused) =>
		{
			if (_modeToggle.isOn)
			{
				StartTitle();
			}
			else
			{
				StartDemo();
			}
		});
		_benchmarkToggle.onValueChanged.AddListener((unused) =>
		{
			if (_benchmarkToggle.isOn)
			{
				_benchmarkImage.enabled = true;
				_benchmark.Run();
				_cellsRoot.gameObject.SetActive(false);
			}
			else
			{
				_benchmarkImage.enabled = false;
				_benchmark.Stop();
				_cellsRoot.gameObject.SetActive(true);
			}
		});
		_zoomEffectToggle.onValueChanged.AddListener((unused) =>
		{
			if (_zoomEffectToggle.isOn)
			{
				_fovBackup = _fovSlider.value;
				_fovSlider.value = 179f;
			}
			else
			{
				_fovSlider.value = _fovBackup;
			}
		});
		_heavyModeToggle.onValueChanged.AddListener((unused) =>
		{
			_benchmark.heavyMode = _heavyModeToggle.isOn;
		});
		_touchDetector.Initialize(_canvasScaler.referenceResolution.x);
		_cells = new List<GameObject>();
		StartDemo();
	}

	void DestroyCells()
	{
		for (int i = 0; i < _cells.Count; i++)
		{
			Destroy(_cells[i]);
		}
		_cells.Clear();
	}

	void StartDemo()
	{
		_controlUiRoot.SetActive(false);
		_subScene = SubScene.Demo;
		_titleText.text = "";
		DestroyCells();
		_camera3d.transform.localRotation = Quaternion.identity;
		const int N = 9;
		for (int x = 0; x < N; x++)
		{
			for (int y = 0; y < N; y++)
			{
				for (int z = 0; z < N; z++)
				{
					var cell = Instantiate(_cellPrefab, _cellsRoot, false);
					cell.transform.localPosition = new Vector3(
						(x - (N * 0.5f)) * 4f,
						(y - (N * 0.5f)) * 4f,
						(z - (N * 0.5f)) * 4f);
					_cells.Add(cell);
				}
			}
		}
	}

	void UpdateDemo()
	{
		_camera3d.transform.localRotation = Quaternion.Euler(Time.realtimeSinceStartup * 10f, Time.realtimeSinceStartup * 10f, 0f);
	}

	void StartTitle()
	{
		DestroyCells();
		_camera3d.transform.localRotation = Quaternion.identity;
		_subScene = SubScene.Title;
		_titleText.text = "";
		_titleText.fontSize = 20;
		for (int i = 0; i < _cells.Count; i++)
		{
			_cells[i].SetActive(false);
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
		_controlUiRoot.SetActive(true);
		_puzzle = new AFallingBlockPuzzle(_width, _height, new int[]{ 4 }, false, loopX: true);
		_subScene = SubScene.Game;
		_titleText.text = "";
		_puzzle.Reset();
	}

	void Update()
	{
		switch (_subScene)
		{
			case SubScene.Demo: UpdateDemo(); break;
			case SubScene.Title: UpdateTitle(); break;
			case SubScene.Game: UpdateGame(); break;
			case SubScene.GameOver: UpdateGameOver(); break;
		}
		_fpsText.text = (_benchmark.averageFrameTime * 1000f).ToString("F2") + " " + _benchmark.count.ToString("F2");
		_camera3d.fieldOfView = 1f + (_fovSlider.value * 178f);
		if (_zoomEffectToggle.isOn)
		{
			_fovSlider.value += (_fovBackup - _fovSlider.value) * 4f * Time.deltaTime;
		}
		_heavyModeToggle.gameObject.SetActive(_benchmarkToggle.isOn);
	}

	void UpdateTitle()
	{
		// 描画
		var text = "A Falling Block Puzzle\n";
		_titleText.text = text;

		if (_touchDetector.clicked)
		{
			StartGame();
		}
	}

	void UpdateGameOver()
	{
		if (_touchDetector.clicked)
		{
			StartTitle();
		}
	}

	void UpdateGame()
	{
		_puzzle.fallInterval = _InitialFallInterval;
		if (Input.GetKeyDown(KeyCode.LeftArrow) || _left)
		{
			_puzzle.MoveLeft();
		}
		else if (Input.GetKeyDown(KeyCode.RightArrow) || _right)
		{
			_puzzle.MoveRight();
		}

		if (Input.GetKeyDown(KeyCode.UpArrow) || _rotation)
		{
			_puzzle.RotateClockwise();
		}

		if (Input.GetKeyDown(KeyCode.DownArrow) || _down)
		{
			_puzzle.MoveDown();
		}
		else if (Input.GetKeyDown(KeyCode.Return) || _fall)
		{
			_puzzle.Land();
		}
		_puzzle.Update(Time.deltaTime);
		DrawGame();
		if (_puzzle.isGameOver)
		{
			StartGameOver();
		}
		var drag = _touchDetector.dragMilliMeter;
		_cameraRotationX += drag.x;
		_cameraRotationY += drag.y;
		_camera3d.transform.localRotation = Quaternion.Euler(_cameraRotationY, -_cameraRotationX, 0f);
		_touchDetector.ClearInput();
		_left = _right = _rotation = _down = _fall = false;
	}

	// 以下描画関連
	void DrawGame()
	{
		var renderWidth = _width + 2; // 壁
		var renderHeight = _height + 1; // 底
		float cellSize = 1f;
		int cellIndex = 0;

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
		var xOffset = -(float)(_width + 2) * 1.1f * 0.5f;
 		var yOffset = 0f;
		if (cellIndex >= _cells.Count)
		{
			cell = Instantiate(_cellPrefab, _cellsRoot, false);
			_cells.Add(cell);
		}
		else
		{
			cell = _cells[cellIndex];
		}
		cell.SetActive(true);
		float radUnit = Mathf.PI * 2f / (float)_width;
		float r = (float)_width / Mathf.PI * 0.5f * 1.4f;
		cell.transform.localPosition = new Vector3(
			Mathf.Sin(radUnit * (float)x) * r,
			yOffset + (y * (cellSize * 1.1f)),
			Mathf.Cos(radUnit * (float)x) * r);
		cell.transform.localRotation = Quaternion.Euler(0f, radUnit * (float)x * Mathf.Rad2Deg, 0f);
	}

	public void OnClickRotation()
	{
		_rotation = true;
	}

	public void OnClickLeft()
	{
		_left = true;
	}

	public void OnClickRight()
	{
		_right = true;
	}

	public void OnClickDown()
	{
		_down = true;
	}

	public void OnClickFall()
	{
		_fall = true;
	}
}
