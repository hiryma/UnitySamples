using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 世界全体を持ってるクラス
public class 世界 : MonoBehaviour
{
	[SerializeField] 設定 設定;
	[SerializeField] Camera カメラ;
	[SerializeField] ステージ ステージ;

	void Start()
	{
		ステージ.初期化(設定);
		死後タイマー = 0f;

		カメラ更新();
	}

	void Update()
	{
		左 = Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A);
		右 = Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.S);

		ジャンプ = Input.GetKey(KeyCode.Space) || Input.GetMouseButton(0);

		var 時間ステップ  = Time.deltaTime;

		if (ステージ.クリアしてる)
		{
			リセット();
		}
		else if (ステージ.死んだ)
		{
			if (死後タイマー <= 0f)
			{
				死後タイマー = 設定.死後待ち時間;
			}
			else if (死後タイマー > 0f)
			{
				死後タイマー -= 時間ステップ;
				if (死後タイマー <= 0f)
				{
					リセット();
				}
			}
		}
		else if (ステージ.自分.落ちた)
		{
			if (死後タイマー <= 0f)
			{
				死後タイマー = 設定.落下後待ち時間;
			}
			else if (死後タイマー > 0f)
			{
				死後タイマー -= 時間ステップ;
				if (死後タイマー <= 0f)
				{
					リセット();
				}
			}
		}
	}

	void FixedUpdate()
	{
		if (ステージ != null) // タイトル画面
		{
			var 時間ステップ  = Time.fixedDeltaTime;

			ステージ.更新(時間ステップ, 左, 右, ジャンプ);

			// 仮にカメラを自キャラ位置に固定
			カメラ更新();
		}
	}
	
	bool 左;
	bool 右;
	bool ジャンプ;
	float 死後タイマー = 0f;

	void リセット()
	{
		UnityEngine.SceneManagement.SceneManager.LoadScene(0);
	}

	void カメラ更新()
	{
		var 自分x = ステージ.自分.transform.position.x;
		カメラ.transform.position = new Vector3(自分x, 5f, -10f);
	}
}
