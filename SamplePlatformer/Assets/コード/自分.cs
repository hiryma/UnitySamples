using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class 自分 : MonoBehaviour
{
	[SerializeField] Rigidbody2D 物理;
	[SerializeField] new SpriteRenderer renderer;

	public void 初期化(設定 設定)
	{
		this.設定 = 設定;
	}

	public void 更新(float 時間ステップ, bool 左, bool 右, bool ジャンプ)
	{
		物理.gravityScale = 設定.重力;

		var 力 = Vector2.zero;
		if (左)
		{
			力 += Vector2.left * 設定.移動加速度;
		}

		if (右)
		{
			力 += Vector2.right * 設定.移動加速度;
		}
		物理.AddForce(力);

		if (ジャンプ)
		{
			物理.AddForce(Vector2.up * 設定.ジャンプ力, ForceMode2D.Impulse);
		}
	}

	//
	設定 設定;
}
