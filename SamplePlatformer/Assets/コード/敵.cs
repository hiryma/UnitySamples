using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class 敵 : MonoBehaviour
{
    [SerializeField] float 重力 = 1f;
    [SerializeField] float 移動力 = 16f;
	[SerializeField] Rigidbody2D 物理;

	public void 初期化()
	{
		
	}

	public void 更新(float 時間ステップ)
	{
		物理.gravityScale = 重力;

        var 力 = (移動は左 ? Vector2.left : Vector2.right) * 移動力;
        物理.AddForce(力);
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
}
