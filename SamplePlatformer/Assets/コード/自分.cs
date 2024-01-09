using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class 自分 : MonoBehaviour
{
	[SerializeField] Rigidbody2D 物理;
	[SerializeField] new SpriteRenderer renderer;

	public bool 死んだ { get; private set; }
	public bool 落ちた { get; private set; }

	public void 初期化(設定 設定)
	{
		this.設定 = 設定;
	}

	public void 更新(float 時間ステップ, bool 左, bool 右, bool ジャンプ)
	{
		物理.gravityScale = 設定.重力;

		var 力 = Vector2.zero;
		var 加速度 = 接地中 ? 設定.移動加速度 : 設定.空中加速度;
		if (左)
		{
			力 += Vector2.left * 加速度;
		}

		if (右)
		{
			力 += Vector2.right * 加速度;
		}
		物理.AddForce(力);

		if (接地中 && ジャンプ && !前のジャンプ入力)
		{
			物理.AddForce(Vector2.up * 設定.初期ジャンプ力, ForceMode2D.Impulse);
		}
		else if (!接地中 && ジャンプ)
		{
			物理.AddForce(Vector2.up * 設定.追加ジャンプ力);
		}

		接地中 = false;

		// 落下死判定
		if (物理.position.y < -1.5f)
		{
			落ちた = true;
		}

		前のジャンプ入力 = ジャンプ;
	}

	void OnCollisionEnter2D(Collision2D collision)
	{
//Debug.Log("E " + collision.gameObject.name + " " + collision.contacts[0].normal.y);
		var 相手 = collision.gameObject;

		if (相手.GetComponent<壁>() != null)
		{
			if (collision.contacts[0].normal.y > 0f)
			{
				接地中 = true;
			}
		}
		else if (相手.GetComponent<敵>() != null)
		{
			死ぬ();
		}
	}

	void OnCollisionStay2D(Collision2D collision)
	{
		OnCollisionEnter2D(collision);
	}


	// ---- 以下外から見えないよ ----
	設定 設定;
	bool 接地中;
	bool 前のジャンプ入力;

	void 死ぬ()
	{
		死んだ = true;
		renderer.color = Color.red;
		物理.AddForce(new Vector2(Random.Range(-1.5f, 0.5f), Random.Range(0.5f, 1.5f)) * 5f, ForceMode2D.Impulse);
		物理.constraints = RigidbodyConstraints2D.None;
		物理.AddTorque(1.5f, ForceMode2D.Impulse);
	}
}
