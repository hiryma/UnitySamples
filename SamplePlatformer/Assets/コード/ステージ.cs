using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ステージ : MonoBehaviour
{
	public 自分 自分 { get => 自分_; }
	public 敵[] 敵リスト { get => 敵リスト_; }

	public void 初期化(設定 設定)
	{
		自分_ = gameObject.GetComponentInChildren<自分>();
		自分_.初期化(設定);

		敵リスト_ = gameObject.GetComponentsInChildren<敵>();
		foreach (var 敵 in 敵リスト_)
		{
			敵.初期化();
		}
	}

	public void 更新(float 時間ステップ, bool 左, bool 右, bool ジャンプ)
	{
		自分_.更新(時間ステップ, 左, 右, ジャンプ);
		foreach (var 敵 in 敵リスト_)
		{
			敵.更新(時間ステップ);
		}
	}

	public bool クリアしてる()
	{
		return false;
	}

	public bool 死んだ()
	{
		return false;
	}

	// ---- ここから下は外からは見えないよ ----

	自分 自分_;
	敵[] 敵リスト_;
}
