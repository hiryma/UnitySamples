using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 世界全体を持ってるクラス
public class 世界 : MonoBehaviour
{
	[SerializeField] 設定 設定;
	[SerializeField] Camera カメラ;
	[SerializeField] ステージ[] ステージリスト;

	void Start()
	{
		ステージを初期化();
	}

	void FixedUpdate()
	{
		var 時間ステップ  = Time.fixedDeltaTime;

		var 左 = Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A);
		var 右 = Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.S);
		var ジャンプ = Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0);

		今のステージ.更新(時間ステップ, 左, 右, ジャンプ);

		// 仮にカメラを自キャラ位置に固定
		カメラ.transform.position = 今のステージ.自分.transform.position + new Vector3(0f, 0f, -10f);

		if (今のステージ.クリアしてる())
		{
			今のステージ番号++;
			ステージを初期化();
		}
		else if (今のステージ.死んだ())
		{
			ステージを初期化();
		}
	}
	
	ステージ 今のステージ;
	int 今のステージ番号 = 0;

	void ステージを初期化()
	{
		var ステージのプレハブ = ステージリスト[今のステージ番号];
		今のステージ = Instantiate(ステージのプレハブ, transform, false);
		今のステージ.初期化(設定);
	}
}
