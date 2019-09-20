using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Kayac
{
	// コルーチンをMonoBehaviourなしで駆動するためのもの。MoveNextを自分で書かないと進まず、実行順を厳密に制御できる。
	// IEnumeratorを渡せば、MoveNextする度にyieldまで進む。
	// CustomYieldInstructionをyieldすればそれで待ち合わせを行い、
	// IEnumeratorをyieldすれば、そちらの実行終了待ち合わせを行う。
	public class ManualCoroutine : IDisposable
	{
		public ManualCoroutine()
		{
			Initialize();
		}

		// Startを呼ぶだけ
		public ManualCoroutine(IEnumerator root)
		{
			Initialize();
			Start(root);
		}

		public void Dispose()
		{
			stack = null;
			next = null;
		}

		public bool Idle
		{
			get
			{
				return (stack.Count == 0);
			}
		}

		public bool Disposed
		{
			get
			{
				return (stack == null);
			}
		}

		public void Start(IEnumerator root)
		{
			Debug.Assert(!Disposed);
			Debug.Assert(next == null);
			Debug.Assert(stack.Count == 0);
			stack.Push(root);
			MoveNext(); // 初回実行
		}

		// 現在のものが終わったら自動遷移するものをセットしておく
		public void SetNext(IEnumerator root)
		{
			Debug.Assert(!Disposed);
			Debug.Assert(next == null);
			if (stack.Count == 0) // すでに空なら開始
			{
				stack.Push(root);
			}
			else
			{
				next = root;
			}
		}

		public bool MoveNext()
		{
			Debug.Assert(!Disposed);
			if (stack.Count <= 0)
			{
				return false;
			}
			var top = stack.Peek();

			// 待ち合わせできるものは待つ
			var current = top.Current;
			if (current is AsyncOperation)
			{
				var asyncOperation = current as AsyncOperation;
				if (!asyncOperation.isDone) // 終わってないので待つ
				{
					return true;
				}
			}
			else if (current is CustomYieldInstruction)
			{
				var customYieldInstruction = current as CustomYieldInstruction;
				if (customYieldInstruction.keepWaiting)
				{
					return true;
				}
			}
			else
			{
				// WaitForSecondsとかは未対応。方法が見つからないというかたぶん無理。
				Debug.Assert(
					!(current is YieldInstruction),
					"ManualCoroutine doesn't support YieldInstruction(WaitForSeconds, Coroutine, WaitUntil). use ManualCoroutine.Wait. executing:" + ExecutingTypeName());
			}

			if (top.MoveNext())
			{
				var child = top.Current as IEnumerator;
				if (child != null)
				{
					stack.Push(child);
					MoveNext(); // 初回実行
				}
			}
			else
			{
				stack.Pop();
				MoveNext(); // 継続実行
			}

			// なくなったら次へ行く
			if ((stack.Count == 0) && (next != null))
			{
				stack.Push(next);
				next = null;
				MoveNext(); // 初回実行
			}
			return (stack.Count > 0);
		}

		// デバグ用。現在実行中のIEnumeratorの内部的な型名が帰る。例えば<CoOrder>c__Iterator1
		public string ExecutingTypeName()
		{
			if (stack.Count > 0)
			{
				return stack.Peek().GetType().Name;
			}
			else
			{
				return "[None]";
			}
		}

		public class Wait : CustomYieldInstruction
		{
			public delegate bool ConditionFunc();

			public Wait(float seconds, bool realTime = false)
			{
				this.realTime = realTime;
				if (realTime)
				{
					this.until = Time.realtimeSinceStartup + seconds;
				}
				else
				{
					this.until = Time.time + seconds;
				}
			}

			public Wait(System.DateTime until)
			{
				realTime = true;
				this.until = Time.realtimeSinceStartup + (float)((until - DateTime.Now).TotalSeconds);
			}

			public Wait(ConditionFunc untilFunc)
			{
				Debug.Assert(untilFunc != null);
				this.untilFunc = untilFunc;
			}

			public override bool keepWaiting
			{
				get
				{
					if (untilFunc != null)
					{
						return !untilFunc();
					}
					else if (realTime)
					{
						return (Time.realtimeSinceStartup < until);
					}
					else
					{
						return (Time.time < until);
					}
				}
			}
			// non public ---------------
			ConditionFunc untilFunc;
			float until;
			bool realTime;
		}

		// non public -----------------
		Stack<IEnumerator> stack;
		IEnumerator next;

		void Initialize()
		{
			stack = new Stack<IEnumerator>();
		}
	}

	// 実行終了確認なし、中断なしで投げっぱなしにするための便利クラス
	public class ManualCoroutineRunner
	{
		public ManualCoroutineRunner()
		{
			actives = new List<ManualCoroutine>();
			idles = new Stack<ManualCoroutine>();
		}

		public void Start(IEnumerator root)
		{
			ManualCoroutine coroutine = null;
			if (idles.Count > 0)
			{
				coroutine = idles.Pop();
				Debug.Assert(coroutine.Idle);
			}
			else
			{
				coroutine = new ManualCoroutine();
			}
			actives.Add(coroutine);
			coroutine.Start(root);
		}

		public void MoveNext()
		{
			int dst = 0;
			for (int i = 0; i < actives.Count; i++)
			{
				actives[dst] = actives[i];
				if (actives[dst].MoveNext())
				{
					dst++;
				}
				else
				{
					Debug.Assert(actives[dst].Idle);
					idles.Push(actives[dst]);
				}
			}
			actives.RemoveRange(dst, actives.Count - dst);
		}

		// non public -----------------
		List<ManualCoroutine> actives;
		Stack<ManualCoroutine> idles;
	}
}
