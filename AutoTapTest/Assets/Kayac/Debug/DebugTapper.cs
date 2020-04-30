using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Profiling;

namespace Kayac
{
	public abstract class DebugTapper : MonoBehaviour
	{
		[SerializeField] Sprite markSprite;
		[SerializeField] bool autoStartEnabled;
		[SerializeField] int tapCount = 8;
		[SerializeField] int logSize = DefaultLogSize;
		[SerializeField] float doubleClickThreshold = 0.3f; // 標準実装準拠

		public struct LogItem
		{
			public enum EventType
			{
				Down,
				Up,
				Click,
				BeginDrag,
				Drag,
				EndDrag,
				Drop,
				Enter,
				Exit,
			}
			public readonly System.DateTime Time;
			public readonly int PointerId;
			public readonly EventType Type;
			public readonly Vector2 Position;
			public readonly GameObject GameObject;

			public LogItem(
				int pointerId,
				EventType type,
				Vector2 position,
				GameObject gameObject)
			{
				this.Time = System.DateTime.Now; // 場合によっては時刻取得をカスタマイズしたくなるかもしれない
				this.Type = type;
				this.PointerId = pointerId;
				this.Position = position;
				this.GameObject = gameObject;
			}

			public override string ToString()
			{
				return string.Format("{0}\t{1}\t{2}\t{3}\t{4}",
					Time.ToString("HH:mm:ss.fff"),
					PointerId,
					Type,
					Position,
					GameObject.name);
			}
		}
		public const int DefaultLogSize = 8192;

		void Start()
		{
			if (autoStartEnabled)
			{
				Debug.Assert(markSprite != null, "markSprite hasn't been set.");
				ManualStart(tapCount, markSprite, logSize);
			}
		}

		public void ManualStart(
			int tapCount,
			Sprite markSprite,
			int logSize = DefaultLogSize)
		{
			logItems = new LogItem[logSize];
			nextLogPosition = 0;

			taps = new Tap[tapCount];
			var canvas = gameObject.AddComponent<Canvas>();
			canvas.sortingOrder = 32767;
			canvas.renderMode = RenderMode.ScreenSpaceOverlay;
			for (int i = 0; i < tapCount; i++)
			{
				var obj = new GameObject("TapMark" + i);
				obj.transform.SetParent(gameObject.transform, false);
				var image = obj.AddComponent<Image>();
				image.rectTransform.anchorMin = Vector2.zero;
				image.rectTransform.anchorMax = Vector2.zero;
				image.enabled = false;
				image.sprite = markSprite;
				image.SetNativeSize();
				taps[i] = new Tap(image, 100 + i);
			}
			tmpRaycastResults = new List<RaycastResult>();
			upSampler = CustomSampler.Create("OnPointerUp");
			downSampler = CustomSampler.Create("OnPointerDown");
			clickSampler = CustomSampler.Create("OnPointerClick");
			beginDragSampler = CustomSampler.Create("OnBeginDrag");
			endDragSampler = CustomSampler.Create("OnEndDrag");
			dragSampler = CustomSampler.Create("OnDrag");
			enterSampler = CustomSampler.Create("OnPointerEnter");
			exitSampler = CustomSampler.Create("OnPointerExit");
			dropSampler = CustomSampler.Create("OnDrop");
		}

		public IEnumerable<LogItem> LogItems
		{
			get
			{
				for (int i = 0; i < logItems.Length; i++)
				{
					var index = nextLogPosition + i;
					if (index >= logItems.Length)
					{
						index -= logItems.Length;
					}
					if (logItems[index].GameObject != null)
					{
						yield return logItems[index];
					}
				}
			}
		}

		public string LogText
		{
			get
			{
				var sb = new System.Text.StringBuilder();
				foreach (var item in LogItems)
				{
					sb.AppendLine(item.ToString());
				}
				return sb.ToString();
			}
		}


		// Protected --------------------

		/// タップするか、どこでするか、を決定するカスタマイズ可能な関数。Fireを中で呼ぶ
		protected abstract void UpdateTap(int tapIndex);
		/// tapIndexは0からTapCount-1
		protected int TapCount { get { return taps.Length; } }

		/// 除外判定のカスタマイズを行う。デフォルトは除外なし
		protected virtual bool ToBeIgnored(GameObject gameObject)
		{
			return false;
		}

		protected void Fire(
			int tapIndex,
			Vector2 fromScreenPosition,
			Vector2 toScreenPosition,
			float duration)
		{
			var tap = taps[tapIndex];
			tap.pointerEvent.position = fromScreenPosition;
			tap.dragFrom = fromScreenPosition;
			tap.dragTo = toScreenPosition;
			tap.mark.rectTransform.anchoredPosition = tap.pointerEvent.position;
			tap.duration = duration;

			var hitObject = Raycast(tap.pointerEvent);
			if (hitObject != null)
			{
				Down(tap, hitObject);
			}
			tap.mark.enabled = true;
		}

		// Private ----------------------
		class Tap
		{
			public Tap(Image mark, int pointerId)
			{
				this.mark = mark;
				time = float.MaxValue;
				pointerEvent = new PointerEventData(EventSystem.current);
				pointerEvent.pointerId = pointerId;
			}
			public Image mark;
			public Vector2 dragFrom;
			public Vector2 dragTo;
			public PointerEventData pointerEvent;
			public float time;
			public float duration;
		}
		Tap[] taps;
		List<RaycastResult> tmpRaycastResults;
		CustomSampler upSampler;
		CustomSampler downSampler;
		CustomSampler clickSampler;
		CustomSampler beginDragSampler;
		CustomSampler dragSampler;
		CustomSampler endDragSampler;
		CustomSampler enterSampler;
		CustomSampler exitSampler;
		CustomSampler dropSampler;
		LogItem[] logItems; // リングバッファ
		int nextLogPosition;

		GameObject Raycast(PointerEventData pointerEventData)
		{
			tmpRaycastResults.Clear();
			EventSystem.current.RaycastAll(pointerEventData, tmpRaycastResults); // 一個でいいんだけどな

			// 現状の実装を見る限り、resultsはソートされており、最初しか見ない
			for (int i = 0; i < tmpRaycastResults.Count; i++)
			{
				if (!ToBeIgnored(tmpRaycastResults[i].gameObject))
				{
					pointerEventData.pointerCurrentRaycast = tmpRaycastResults[i];
					return tmpRaycastResults[i].gameObject;
				}
			}
			return null;
		}

		void OnDestroy()
		{
			if (taps != null)
			{
				for (int i = 0; i < taps.Length; i++)
				{
					Destroy(taps[i].mark.gameObject);
				}
			}
		}

		void Update()
		{
			if (taps == null) // 未初期化
			{
				return;
			}

			if (EventSystem.current == null) // nullを返すことがある!!緊急回避!!
			{
				return;
			}

			var deltaTime = Time.deltaTime;
			for (int i = 0; i < taps.Length; i++)
			{
				UpdateTap(i, deltaTime);
			}
		}

		void OnDisable()
		{
			if (taps != null)
			{
				foreach (var tap in taps)
				{
					tap.mark.enabled = false;
				}
			}
		}

		void UpdateTap(int tapIndex, float deltaTime)
		{
			var tap = taps[tapIndex];
			if (tap.time < tap.duration) // 押してる間はここで処理
			{
				tap.time += deltaTime;
				float normalizedTime;
				if (tap.duration <= 0f)
				{
					normalizedTime = 1f; // いきなり終了
				}
				else
				{
					normalizedTime = tap.time / tap.duration;
				}
				normalizedTime = Mathf.Clamp01(normalizedTime);
				var newPos = Vector2.Lerp(tap.dragFrom, tap.dragTo, normalizedTime);
				var diff = newPos - tap.pointerEvent.position;
				tap.pointerEvent.delta = diff;
				tap.mark.rectTransform.anchoredPosition = newPos;
				tap.pointerEvent.position = newPos;

				var hitObject = Raycast(tap.pointerEvent);
				// 到着判定
				if (normalizedTime >= 1f) // 到着したので指を離す
				{
					Up(tap, hitObject);
				}
				else if (tap.pointerEvent.dragging) // すでにドラッグ中なのでドラッグ
				{
					Drag(tap, hitObject);
				}
				else // ドラグ開始判定
				{
					var distanceSq = (newPos - tap.dragFrom).sqrMagnitude;
					var threshold = EventSystem.current.pixelDragThreshold;
					if (distanceSq >= (threshold * threshold))
					{
						BeginDrag(tap);
					}
				}
			}
			else // 押してなければでなければユーザ処理に回す
			{
				Debug.Assert(tap.pointerEvent.pointerDrag == null);
				Debug.Assert(tap.pointerEvent.rawPointerPress == null);
				// ドラッグ中なのにpointerPressがnull → オブジェクト破棄で消滅したケース
				if (tap.pointerEvent.dragging)
				{
					EndDrag(tap, null);
				}
				UpdateTap(tapIndex);
			}
		}

		void Log(LogItem.EventType type, PointerEventData ev, GameObject gameObject)
		{
			logItems[nextLogPosition] = new LogItem(ev.pointerId, type, ev.position, gameObject);
			nextLogPosition++;
			if (nextLogPosition >= logItems.Length)
			{
				nextLogPosition = 0;
			}
		}

		void Down(Tap tap, GameObject hitObject)
		{
			var ev = tap.pointerEvent;
			Debug.Assert(hitObject != null);
			Debug.Assert(ev.rawPointerPress == null);

			ev.delta = Vector2.zero;
			ev.dragging = false;
			ev.useDragThreshold = true;
			ev.pressPosition = ev.position;
			ev.pointerPressRaycast = ev.pointerCurrentRaycast;
			ev.rawPointerPress = hitObject;

			var fired = ExecuteEvents.ExecuteHierarchy<IPointerDownHandler>(hitObject, ev, (handler, data) =>
			{
				downSampler.Begin();
				handler.OnPointerDown((PointerEventData)data);
				downSampler.End();
			});
			if (fired != null)
			{
				ev.pointerPress = fired;
				Log(LogItem.EventType.Down, ev, fired);
			}
			else // Downが見つからなかい場合もClickを持っているものがあれば、clickCountの処理を行う
			{
				fired = ExecuteEvents.GetEventHandler<IPointerClickHandler>(hitObject);
			}
			// 同じ物にdownが連続した場合の処理
			var time = Time.unscaledTime;
			int newClickCount = 1;
			if (fired == ev.lastPress)
			{
				var diffTime = time - ev.clickTime;
				if (diffTime < doubleClickThreshold)
				{
					newClickCount = ev.clickCount + 1;
				}
			}
			ev.clickTime = time;
			ev.clickCount = newClickCount;
			tap.time = 0f;
		}

		void Up(Tap tap, GameObject hitObject)
		{
			var ev = tap.pointerEvent;
			var pressed = ev.rawPointerPress; // 押された時に実際に差していたもの(downイベントが発生したものと違う可能性はある)
			if (pressed != null) // 押されたものが死んでなければUpを発火。nullになっている可能性がある。
			{
				var fired = ExecuteEvents.ExecuteHierarchy<IPointerUpHandler>(pressed, ev, (handler, data) =>
				{
					upSampler.Begin();
					handler.OnPointerUp((PointerEventData)data);
					upSampler.End();
				});
				if (fired != null)
				{
					Log(LogItem.EventType.Up, ev, fired);
				}

				// 押したものと、今ヒットしているオブジェクトが等しければクリックも発火
				if (pressed == hitObject)
				{
					Click(tap);
				}
				ev.pointerPress = null;
				ev.rawPointerPress = null;
			}
			// ドラグ中ならドラグ終了
			if (ev.dragging)
			{
				EndDrag(tap, hitObject);
			}
			tap.time = float.MaxValue;
			tap.mark.enabled = false;
		}

		void Click(Tap tap)
		{
			var ev = tap.pointerEvent;
			var pressed = ev.rawPointerPress; // 押された時に実際に差していたもの(downイベントが発生したものと違う可能性はある)
			var fired = ExecuteEvents.ExecuteHierarchy<IPointerClickHandler>(pressed, ev, (handler, data) =>
			{
				clickSampler.Begin();
				handler.OnPointerClick((PointerEventData)data);
				clickSampler.End();
			});
			if (fired != null)
			{
				Log(LogItem.EventType.Click, ev, fired);
			}
		}

		void BeginDrag(Tap tap)
		{
			var ev = tap.pointerEvent;
			// ドラグ中でないはず
			Debug.Assert(!ev.dragging);
			Debug.Assert(ev.pointerDrag == null);
			var pressed = ev.rawPointerPress;
			if (pressed == null) // すでに破棄されている場合、代わりにdownが発生したオブジェクトを使う
			{
				pressed = ev.pointerPress; // これがそもそもない場合や死んでいる場合もありうる
			}

			if (pressed != null)
			{
				ev.dragging = true;
				ev.useDragThreshold = true;
				var fired = ExecuteEvents.ExecuteHierarchy<IBeginDragHandler>(pressed, ev, (handler, data) =>
				{
					beginDragSampler.Begin();
					handler.OnBeginDrag((PointerEventData)data);
					beginDragSampler.End();
				});
				if (fired != null)
				{
					Log(LogItem.EventType.BeginDrag, ev, fired);
				}
				else // BeginDragを受け取らなくても、Drag,EndDragを受け取る場合にはpointerDragに受け取り手を入れておく
				{
					fired = ExecuteEvents.GetEventHandler<IDragHandler>(pressed);
					if (fired == null)
					{
						fired = ExecuteEvents.GetEventHandler<IEndDragHandler>(pressed);
					}
				}
				ev.pointerDrag = fired;
			}
		}

		void Drag(Tap tap, GameObject hitObject)
		{
			var ev = tap.pointerEvent;
			Debug.Assert(ev.dragging);
			var dragged = ev.pointerDrag;
			if (dragged == null) // 破棄されちゃった。離したことにする。
			{
				Up(tap, hitObject);
			}
			else
			{
				var fired = ExecuteEvents.ExecuteHierarchy<IDragHandler>(dragged, ev, (handler, data) =>
				{
					dragSampler.Begin();
					handler.OnDrag((PointerEventData)data);
					dragSampler.End();
				});
				if (fired != null)
				{
					Log(LogItem.EventType.Drag, ev, dragged);
				}
			}
		}

		void EndDrag(Tap tap, GameObject hitObject)
		{
			var ev = tap.pointerEvent;
			// ドラグ中のはず
			Debug.Assert(ev.dragging);
			// まずドロップ
			Drop(tap, hitObject);
			// 元オブジェクトが生きていればEndDrag発火。
			var dragged = ev.pointerDrag;
			if (dragged != null)
			{
				var fired = ExecuteEvents.ExecuteHierarchy<IEndDragHandler>(dragged, ev, (handler, data) =>
				{
					endDragSampler.Begin();
					handler.OnEndDrag((PointerEventData)data);
					endDragSampler.End();
				});
				if (fired != null)
				{
					Log(LogItem.EventType.EndDrag, ev, dragged);
				}
			}
			ev.dragging = false;
			ev.pointerDrag = null;
			ev.useDragThreshold = false;
		}

		void Drop(Tap tap, GameObject hitObject)
		{
			var ev = tap.pointerEvent;
			var fired = ExecuteEvents.ExecuteHierarchy<IDropHandler>(hitObject, ev, (handler, data) =>
			{
				dropSampler.Begin();
				handler.OnDrop((PointerEventData)data);
				dropSampler.End();
			});
			if (fired != null)
			{
				Log(LogItem.EventType.Drop, ev, fired);
			}
		}
	}
}