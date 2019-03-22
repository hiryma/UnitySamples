using UnityEngine; //assertのためだけ

namespace Kayac
{
	public interface IIntrusiveLinkedListNode // 継承して使ってね!
	{
		IIntrusiveLinkedListNode prev{ get; set; }
		IIntrusiveLinkedListNode next{ get; set; }
	}

#if false // 番兵なし版

	// こいつのためにnewしたくないだろうしstruct
	public struct IntrusiveLinkedList<T> where T : IIntrusiveLinkedListNode
	{
		public IIntrusiveLinkedListNode first{ get; private set; }
		public IIntrusiveLinkedListNode last{ get; private set; }

		public void AddLast(T newNode)
		{
			Debug.Assert(newNode != null); // nullはダメだよ
			if (this.last == null) // 一個もない
			{
				Debug.Assert(this.first == null); // firstもnullのはずだよ
				this.first = this.last = newNode;
			}
			else
			{
				this.last.next = newNode;
				newNode.prev = this.last;
				this.last = newNode;
			}
		}

		public void AddFirst(T newNode)
		{
			Debug.Assert(newNode != null); // nullはダメだよ
			if (this.first == null) // 一個もない
			{
				Debug.Assert(this.last == null); // lastもnullのはずだよ
				this.first = this.last = newNode;
			}
			else
			{
				this.first.prev = newNode;
				newNode.next = this.first;
				this.first = newNode;
			}
		}

		public void AddBefore(T insertPoint, T newNode)
		{
			Debug.Assert(newNode != null); // nullはダメだよ
			Debug.Assert(insertPoint != null); // これもnullはダメだよ
			var prev = insertPoint.prev;
			Debug.Assert(prev != null); // これがnullなのはバグだよ
			insertPoint.prev = newNode;
			newNode.next = insertPoint;
			prev.next = newNode;
			newNode.prev = prev;
		}

		public void AddAfter(T insertPoint, T newNode)
		{
			Debug.Assert(newNode != null); // nullはダメだよ
			Debug.Assert(insertPoint != null); // これもnullはダメだよ
			var next = insertPoint.next;
			Debug.Assert(next != null); // これがnullなのはバグだよ
			insertPoint.next = newNode;
			newNode.prev = insertPoint;
			next.prev = newNode;
			newNode.next = next;
		}

		public void Remove(T node)
		{
			RemoveImpl(node);
		}

		void RemoveImpl(IIntrusiveLinkedListNode node)
		{
			Debug.Assert(node != null); // nullはダメだよ
			// 先頭の場合、末尾の場合、先頭かつ末尾の場合、それ以外、の4場合分けが必要
			if (node == this.first)
			{
				Debug.Assert(node.prev == null); // 先頭ならprevはnullのはずだよ
				if (node == this.last) // 唯一要素の場合
				{
					Debug.Assert(node.next == null); // 末尾ならnextはnullのはずだよ
					this.first = this.last = null;
				}
				else
				{
					var next = node.next;
					Debug.Assert(node.next != null); // 末尾じゃないからnextはnullじゃないよ
					next.prev = null;
					this.first = next;
				}
			}
			else if (node == this.last)
			{
				Debug.Assert(node.next == null); // 末尾ならnextはnullのはずだよ
				var prev = node.prev;
				Debug.Assert(node.prev != null); // これがnullなら_firstなので、ここには来ないよ。バグってるよ
				prev.next = null;
				this.last = prev;
			}
			else
			{
				var prev = node.prev;
				var next = node.next;
				Debug.Assert(prev != null); // 先頭じゃないからprevはnullじゃないよ
				Debug.Assert(next != null); // 末尾じゃないからnextはnullじゃないよ
				prev.next = next;
				next.prev = prev;
			}
			node.next = node.prev = null; // 安全のため切っておく
		}

		public void RemoveFirst()
		{
			if (this.first != null)
			{
				RemoveImpl(this.first);
			}
		}

		public void RemoveLast()
		{
			if (this.last != null)
			{
				RemoveImpl(this.last);
			}
		}
	}

#else // 番兵あり版

	public struct IntrusiveLinkedList<T> where T : IIntrusiveLinkedListNode
	{
		class Sentinel : IIntrusiveLinkedListNode // 番兵クラス
		{
			public IIntrusiveLinkedListNode prev{ get; set; }
			public IIntrusiveLinkedListNode next{ get; set; }
		}
		Sentinel _firstSentinel;
		Sentinel _lastSentinel;
		public IIntrusiveLinkedListNode first{ get{ return _firstSentinel.next; } }
		public IIntrusiveLinkedListNode last{ get{ return _lastSentinel.prev; } }

		public void Init() // 番兵生成のために必要
		{
			_firstSentinel = new Sentinel();
			_lastSentinel = new Sentinel();
			_firstSentinel.next = _lastSentinel;
			_lastSentinel.prev = _firstSentinel;
		}

		public void AddLast(T newNode)
		{
			AddBeforeImpl(_lastSentinel, newNode);
		}

		public void AddFirst(T newNode)
		{
			AddAfterImpl(_firstSentinel, newNode);
		}

		public void AddBefore(T insertPoint, T newNode)
		{
			AddBeforeImpl(insertPoint, newNode);
		}

		// 全然関係ない型をつっこまれる危険を減らすために面倒なことをしている
		void AddBeforeImpl(IIntrusiveLinkedListNode insertPoint, T newNode)
		{
			Debug.Assert(newNode != null); // nullはダメだよ
			Debug.Assert(insertPoint != null); // これもnullはダメだよ
			var prev = insertPoint.prev;
			Debug.Assert(prev != null); // これがnullなのはバグだよ
			insertPoint.prev = newNode;
			newNode.next = insertPoint;
			prev.next = newNode;
			newNode.prev = prev;
		}

		public void AddAfter(T insertPoint, T newNode)
		{
			AddAfterImpl(insertPoint, newNode);
		}

		void AddAfterImpl(IIntrusiveLinkedListNode insertPoint, T newNode)
		{
			Debug.Assert(newNode != null); // nullはダメだよ
			Debug.Assert(insertPoint != null); // これもnullはダメだよ
			var next = insertPoint.next;
			Debug.Assert(next != null); // これがnullなのはバグだよ
			insertPoint.next = newNode;
			newNode.prev = insertPoint;
			next.prev = newNode;
			newNode.next = next;
		}

		public void Remove(T node)
		{
			RemoveImpl(node);
		}

		void RemoveImpl(IIntrusiveLinkedListNode node)
		{
			Debug.Assert(node != null); // nullはダメだよ
			Debug.Assert(node.next != null); // nextはnullじゃないよ
			Debug.Assert(node.prev != null); // prevはnullじゃないよ
			var next = node.next;
			var prev = node.prev;
			next.prev = prev;
			prev.next = next;
			node.next = node.prev = null; // 安全のため切っておく
		}

		public void RemoveFirst()
		{
			if (this.first != null)
			{
				RemoveImpl(this.first);
			}
		}

		public void RemoveLast()
		{
			if (this.last != null)
			{
				RemoveImpl(this.last);
			}
		}
	}
#endif
}