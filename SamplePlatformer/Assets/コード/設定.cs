using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class 設定
{
	public float 重力 = 1f;	
	public float 移動加速度 = 1f;
	public float 空中加速度 = 0.5f;
	public float 初期ジャンプ力 = 1f;
	public float 追加ジャンプ力 = 1f;
	public float 死後待ち時間 = 2f;
	public float 落下後待ち時間 = 0.5f;
}
