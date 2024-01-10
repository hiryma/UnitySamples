using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 世界全体を持ってるクラス
public class 世界 : MonoBehaviour
{
	[SerializeField] 設定 設定;
	[SerializeField] Camera カメラ;
	[SerializeField] SpriteRenderer タイトル画面;
	[SerializeField] ステージ[] ステージリスト;

	void Update()
	{
		左 = Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A);
		右 = Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.S);

		ジャンプ = Input.GetKey(KeyCode.Space) || Input.GetMouseButton(0);

		if (今のステージ == null) // タイトル画面
		{
			if (ジャンプ || 左 || 右)
			{
				タイトル画面.gameObject.SetActive(false);
				ステージを初期化();
			}
		}
		else
		{
			var 時間ステップ  = Time.deltaTime;

			if (今のステージ.クリアしてる)
			{
				今のステージ番号++;
				ステージを初期化();
			}
			else if (今のステージ.死んだ)
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
						ステージを初期化();
					}
				}
			}
			else if (今のステージ.自分.落ちた)
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
						ステージを初期化();
					}
				}
			}
		}
	}

	void FixedUpdate()
	{
		if (今のステージ != null) // タイトル画面
		{
			var 時間ステップ  = Time.fixedDeltaTime;

			今のステージ.更新(時間ステップ, 左, 右, ジャンプ);

			// 仮にカメラを自キャラ位置に固定
			カメラ更新();
		}
	}
	
	ステージ 今のステージ;
	int 今のステージ番号 = 0;
	bool 左;
	bool 右;
	bool ジャンプ;
	float 死後タイマー = 0f;

	void ステージを初期化()
	{
		if (今のステージ != null)
		{
			Destroy(今のステージ.gameObject);
		}

		var ステージのプレハブ = ステージリスト[今のステージ番号];
		今のステージ = Instantiate(ステージのプレハブ, transform, false);
		今のステージ.初期化(設定);
		死後タイマー = 0f;

		カメラ更新();
	}

	void カメラ更新()
	{
		var 自分x = 今のステージ.自分.transform.position.x;
		カメラ.transform.position = new Vector3(自分x, 5f, -10f);
	}
}
