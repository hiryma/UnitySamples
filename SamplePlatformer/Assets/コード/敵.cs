using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class 敵 : MonoBehaviour
{
	[SerializeField] float 重力 = 1f;
	[SerializeField] float 移動力 = 16f;
	[SerializeField] float つぶれアニメ速さ = 1f;
	[SerializeField] float 踏みバウンド力 = 4;
	[SerializeField] Rigidbody2D 物理;
	[SerializeField] Collider2D コライダ;

	public float Get踏みバウンド力()
	{
		return 踏みバウンド力;
	}

	public void 初期化()
	{
		
	}

	public void 更新(float 時間ステップ)
	{
		if (コライダ.enabled) // 生きてる
 		{
			物理.gravityScale = 重力;

			var 力 = (移動は左 ? Vector2.left : Vector2.right) * 移動力;
			物理.AddForce(力);
		}
		else // 死んでる
		{
			var scale = transform.localScale;
			scale.y *= 1f - (つぶれアニメ速さ * 時間ステップ);
			transform.localScale = scale;
			var y = 死んだ場所.y - (0.5f * (1f - scale.y));
			transform.position = new Vector3(死んだ場所.x, y, 死んだ場所.z);
		}
	}

	public void 踏まれて死ぬ()
	{
		コライダ.enabled = false;
		物理.isKinematic = true;
		死んだ場所 = transform.position;
	}

	void OnCollisionEnter2D(Collision2D collision)
	{
		if ((collision.gameObject.GetComponent<壁>() != null) || (collision.gameObject.GetComponent<敵>() != null))
		{
			if (collision.contacts[0].normal.x < -0.5f) // 右に壁
			{
				移動は左 = true;
			}
			else if (collision.contacts[0].normal.x > 0.5f) // 左に壁
			{
				移動は左 = false;
			}
		}
	}

	void OnCollisionStay2D(Collision2D collision)
	{
		OnCollisionEnter2D(collision);
	}

	//
	bool 移動は左;
	Vector3 死んだ場所;
}
