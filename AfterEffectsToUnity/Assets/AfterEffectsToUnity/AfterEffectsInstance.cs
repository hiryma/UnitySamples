using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

namespace AfterEffectsToUnity
{
	public class AfterEffectsInstance
	{
		private const float _endTimeEpsilon = (1f / 128f); //フレーム数16bit+7bitでfloatの精度23bit。
		private AfterEffectsResource _resource;

		private float _frame;
		private int _currentLoopCount;
		private Action _onEnd;
		private Action _onLoop;
		private bool _isPlaying;
		private bool _nextIsEnd;
		private AfterEffectsResource.Cut _cut;
		private CallbackTimeline _callbackTimeline;
		private float _lastAppliedFrame; // 最後に計算を適応したframe。変わってなければスルーする。

		public void AddCallback(int frame, System.Action action)
		{
			if (_callbackTimeline == null)
			{
				_callbackTimeline = new CallbackTimeline();
			}
			_callbackTimeline.Add(frame, action);
		}

		public string currentCutName
		{
			get
			{
				return _cut.name;
			}
		}

		// 1で元の速度。0.1なら1/10のスロー再生。
		public float speed { get; set; }

		public float currentFrame
		{
			get
			{
				return _frame;
			}

			set
			{
				_frame = value;
			}
		}

		public float normalizedTime
		{
			get
			{
				return (_frame - _cut.start) / _cut.duration;
			}

			set
			{
				var tmp = value;
				tmp *= _cut.duration;
				tmp += _cut.start;
				_frame = tmp;
			}
		}

		private struct TransformCurve
		{
			public TransformCurve(
				Transform transform,
				int index,
				AfterEffectsCurveSet.ValueType type = AfterEffectsCurveSet.ValueType.Unknown)
			{
				this.transform = transform;
				this.index = index;
				this.type = type;
			}
			public Transform transform;
			public int index;
			public AfterEffectsCurveSet.ValueType type;
		}

		private struct RectTransformCurve
		{
			public RectTransformCurve(RectTransform transform, int index)
			{
				this.transform = transform;
				this.index = index;
			}
			public RectTransform transform;
			public int index;
		}

		private struct GraphicCurve
		{
			public GraphicCurve(Graphic graphic, int index)
			{
				this.graphic = graphic;
				this.index = index;
			}
			public Graphic graphic;
			public int index;
		}

		private struct CanvasGroupCurve
		{
			public CanvasGroupCurve(CanvasGroup canvasGroup, int index)
			{
				this.canvasGroup = canvasGroup;
				this.index = index;
			}
			public CanvasGroup canvasGroup;
			public int index;
		}

		private struct SpriteRendererCurve
		{
			public SpriteRendererCurve(SpriteRenderer spriteRenderer, int index)
			{
				this.spriteRenderer = spriteRenderer;
				this.index = index;
			}
			public SpriteRenderer spriteRenderer;
			public int index;
		}

		private struct GameObjectCurve
		{
			public GameObjectCurve(GameObject gameObject, int index)
			{
				this.gameObject = gameObject;
				this.index = index;
			}
			public GameObject gameObject;
			public int index;
		}

		private List<TransformCurve> _scaleCurves;
		private List<TransformCurve> _rotationCurves;
		private List<RectTransformCurve> _graphicPositionCurves;
		private List<RectTransformCurve> _graphicSizeCurves;
		private List<TransformCurve> _spriteRendererPositionCurves;
		private List<GraphicCurve> _graphicOpacityCurves;
		private List<CanvasGroupCurve> _canvasGroupOpacityCurves;
		private List<SpriteRendererCurve> _spriteRendererOpacityCurves;
		private List<GraphicCurve> _graphicVisibilityCurves;
		private List<SpriteRendererCurve> _spriteRendererVisibilityCurves;
		private List<GameObjectCurve> _gameObjectVisibilityCurves;

		public AfterEffectsInstance(AfterEffectsResource resource)
		{
			SetResource(resource);
			speed = 1f;
			// デフォルトセット
			_cut = _resource.defaultCut;
			_lastAppliedFrame = -(float.MaxValue);
		}

		public void Dispose()
		{
			_resource = null;
			_cut = null;
			_onEnd = null;
			_onLoop = null;
			_scaleCurves = null;
			_rotationCurves = null;
			_graphicPositionCurves = null;
			_graphicSizeCurves = null;
			_spriteRendererPositionCurves = null;
			_graphicOpacityCurves = null;
			_canvasGroupOpacityCurves = null;
			_spriteRendererOpacityCurves = null;
			_graphicVisibilityCurves = null;
			_spriteRendererVisibilityCurves = null;
			_gameObjectVisibilityCurves = null;
			_callbackTimeline = null;
		}

		public bool IsDisposed()
		{
			return (_resource == null);
		}

		public void SetResource(AfterEffectsResource resource)
		{
			_resource = resource;
		}

		// 区間の最初に戻す。ループカウントも初期化。カット名が与えられればそのカットに変更
		public void Rewind(string cutName = null, Action onEnd = null, Action onLoop = null)
		{
			if (cutName != null)
			{
				var cut = _resource.FindCut(cutName);
				if (cut != null)
				{
					_cut = cut;
				}
				else
				{
					Debug.LogError("Rewind failed. " + cutName + " is not found.");
				}
			}
			_frame = _cut.start;
			_currentLoopCount = 0;
			_nextIsEnd = false;
			_onEnd = onEnd;
			_onLoop = onLoop;
		}

		// 現在カットを再生
		public void Play()
		{
			_isPlaying = true;
		}

		public void Pause()
		{
			_isPlaying = false;
		}

		public bool isPlaying
		{
			get
			{
				return _isPlaying;
			}
		}

		// FloatかVector2どちらかなので、両方探す
		public AfterEffectsInstance BindScale(Transform transform, string name)
		{
			var type = AfterEffectsCurveSet.ValueType.Float;
			int index = _resource.FindIndex(type, name);
			if (index < 0)
			{
				type = AfterEffectsCurveSet.ValueType.Vector2;
				index = _resource.FindIndex(type, name);
			}
			if (index >= 0)
			{
				if (_scaleCurves == null)
				{
					_scaleCurves = new List<TransformCurve>(8);
				}
				var curve = new TransformCurve(transform, index, type);
				_scaleCurves.Add(curve);
			}
			else
			{
				Debug.LogError(name + " not found in CurveSet");
			}
			return this;
		}

		public AfterEffectsInstance BindRotation(Transform transform, string name)
		{
			int index = _resource.FindIndex(AfterEffectsCurveSet.ValueType.Float, name);
			if (index >= 0)
			{
				if (_rotationCurves == null)
				{
					_rotationCurves = new List<TransformCurve>(8);
				}
				var curve = new TransformCurve(transform, index);
				_rotationCurves.Add(curve);
			}
			else
			{
				Debug.LogError(name + " not found in CurveSet");
			}
			return this;
		}

		public AfterEffectsInstance BindPosition(RectTransform transform, string name)
		{
			int index = _resource.FindIndex(AfterEffectsCurveSet.ValueType.Vector2, name);
			if (index >= 0)
			{
				if (_graphicPositionCurves == null)
				{
					_graphicPositionCurves = new List<RectTransformCurve>(8);
				}
				var curve = new RectTransformCurve(transform, index);
				_graphicPositionCurves.Add(curve);
			}
			else
			{
				Debug.LogError(name + " not found in CurveSet");
			}
			return this;
		}

		public AfterEffectsInstance BindSize(RectTransform transform, string name)
		{
			int index = _resource.FindIndex(AfterEffectsCurveSet.ValueType.Vector2, name);
			if (index >= 0)
			{
				if (_graphicSizeCurves == null)
				{
					_graphicSizeCurves = new List<RectTransformCurve>(8);
				}
				var curve = new RectTransformCurve(transform, index);
				_graphicSizeCurves.Add(curve);
			}
			else
			{
				Debug.LogError(name + " not found in CurveSet");
			}
			return this;
		}

		public AfterEffectsInstance BindPosition(Transform transform, string name)
		{
			int index = _resource.FindIndex(AfterEffectsCurveSet.ValueType.Vector2, name);
			if (index >= 0)
			{
				if (_spriteRendererPositionCurves == null)
				{
					_spriteRendererPositionCurves = new List<TransformCurve>(8);
				}
				var curve = new TransformCurve(
					transform,
					index);
				_spriteRendererPositionCurves.Add(curve);
			}
			else
			{
				Debug.LogError(name + " not found in CurveSet");
			}
			return this;
		}

		public AfterEffectsInstance BindOpacity(Graphic graphic, string name)
		{
			int index = _resource.FindIndex(AfterEffectsCurveSet.ValueType.Float, name);
			if (index >= 0)
			{
				if (_graphicOpacityCurves == null)
				{
					_graphicOpacityCurves = new List<GraphicCurve>(8);
				}
				var curve = new GraphicCurve(graphic, index);
				_graphicOpacityCurves.Add(curve);
			}
			else
			{
				Debug.LogError(name + " not found in CurveSet");
			}
			return this;
		}

		public AfterEffectsInstance BindOpacity(CanvasGroup canvasGroup, string name)
		{
			int index = _resource.FindIndex(AfterEffectsCurveSet.ValueType.Float, name);
			if (index >= 0)
			{
				if (_canvasGroupOpacityCurves == null)
				{
					_canvasGroupOpacityCurves = new List<CanvasGroupCurve>(8);
				}
				var curve = new CanvasGroupCurve(canvasGroup, index);
				_canvasGroupOpacityCurves.Add(curve);
			}
			else
			{
				Debug.LogError(name + " not found in CurveSet");
			}
			return this;
		}

		public AfterEffectsInstance BindOpacity(SpriteRenderer spriteRenderer, string name)
		{
			int index = _resource.FindIndex(AfterEffectsCurveSet.ValueType.Float, name);
			if (index >= 0)
			{
				if (_spriteRendererOpacityCurves == null)
				{
					_spriteRendererOpacityCurves = new List<SpriteRendererCurve>(8);
				}
				var curve = new SpriteRendererCurve(spriteRenderer, index);
				_spriteRendererOpacityCurves.Add(curve);
			}
			else
			{
				Debug.LogError(name + " not found in CurveSet");
			}
			return this;
		}

		public AfterEffectsInstance BindVisibility(Graphic graphic, string name)
		{
			int index = _resource.FindIndex(AfterEffectsCurveSet.ValueType.Bool, name);
			if (index >= 0)
			{
				if (_graphicVisibilityCurves == null)
				{
					_graphicVisibilityCurves = new List<GraphicCurve>(8);
				}
				var curve = new GraphicCurve(graphic, index);
				_graphicVisibilityCurves.Add(curve);
			}
			else
			{
				Debug.LogError(name + " not found in CurveSet");
			}
			return this;
		}

		public AfterEffectsInstance BindVisibility(SpriteRenderer spriteRenderer, string name)
		{
			int index = _resource.FindIndex(AfterEffectsCurveSet.ValueType.Bool, name);
			if (index >= 0)
			{
				if (_spriteRendererVisibilityCurves == null)
				{
					_spriteRendererVisibilityCurves = new List<SpriteRendererCurve>(8);
				}
				var curve = new SpriteRendererCurve(spriteRenderer, index);
				_spriteRendererVisibilityCurves.Add(curve);
			}
			else
			{
				Debug.LogError(name + " not found in CurveSet");
			}
			return this;
		}

		public AfterEffectsInstance BindVisibility(GameObject gameObject, string name)
		{
			int index = _resource.FindIndex(AfterEffectsCurveSet.ValueType.Bool, name);
			if (index >= 0)
			{
				if (_gameObjectVisibilityCurves == null)
				{
					_gameObjectVisibilityCurves = new List<GameObjectCurve>(8);
				}
				var curve = new GameObjectCurve(gameObject, index);
				_gameObjectVisibilityCurves.Add(curve);
			}
			else
			{
				Debug.LogError(name + " not found in CurveSet");
			}
			return this;
		}

		public virtual void UpdatePerFrame(float deltaTime)
		{
			// 再生中でなく、「もう一回」がないなら、抜ける
			if (!_isPlaying)
			{
				return;
			}

			if (_lastAppliedFrame != _frame)
			{
				_lastAppliedFrame = _frame;
				Vector2 tmpVector2;
				// 計算するよ
				if (_scaleCurves != null)
				{
					foreach (var curve in _scaleCurves)
					{
						// スケールは1値の場合と2値の場合がある
						if (curve.type == AfterEffectsCurveSet.ValueType.Vector2)
						{
							_resource.GetVector2(out tmpVector2, curve.index, _frame);
							curve.transform.localScale = new Vector3(tmpVector2.x, tmpVector2.y, 1f);
						}
						else
						{
							var v = _resource.GetFloat(curve.index, _frame);
							curve.transform.localScale = new Vector3(v, v, 1f);
						}
					}
				}

				if (_rotationCurves != null)
				{
					foreach (var curve in _rotationCurves)
					{
						float v = _resource.GetFloat(curve.index, _frame);
						curve.transform.localRotation = Quaternion.Euler(0f, 0f, -v); //角度逆
					}
				}

				if (_graphicPositionCurves != null)
				{
					foreach (var curve in _graphicPositionCurves)
					{
						_resource.GetVector2(out tmpVector2, curve.index, _frame);
						tmpVector2.y = -tmpVector2.y; // Yは反転。下プラス。
						curve.transform.anchoredPosition = tmpVector2;
					}
				}

				if (_graphicSizeCurves != null)
				{
					foreach (var curve in _graphicSizeCurves)
					{
						_resource.GetVector2(out tmpVector2, curve.index, _frame);
						curve.transform.sizeDelta = tmpVector2;
					}
				}

				if (_spriteRendererPositionCurves != null)
				{
					foreach (var curve in _spriteRendererPositionCurves)
					{
						_resource.GetVector2(out tmpVector2, curve.index, _frame);
						// zはそのまま保つ
						var p = curve.transform.localPosition;
						p.x = tmpVector2.x;
						p.y = -tmpVector2.y; //yは反転。下プラス。
						curve.transform.localPosition = p;
					}
				}

				if (_graphicOpacityCurves != null)
				{
					foreach (var curve in _graphicOpacityCurves)
					{
						float v = _resource.GetFloat(curve.index, _frame);
						var c = curve.graphic.color;
						c.a = v;
						curve.graphic.color = c;
					}
				}

				if (_canvasGroupOpacityCurves != null)
				{
					foreach (var curve in _canvasGroupOpacityCurves)
					{
						float v = _resource.GetFloat(curve.index, _frame);
						curve.canvasGroup.alpha = v;
					}
				}

				if (_spriteRendererOpacityCurves != null)
				{
					foreach (var curve in _spriteRendererOpacityCurves)
					{
						float v = _resource.GetFloat(curve.index, _frame);
						var c = curve.spriteRenderer.color;
						c.a = v;
						curve.spriteRenderer.color = c;
					}
				}

				if (_graphicVisibilityCurves != null)
				{
					foreach (var curve in _graphicVisibilityCurves)
					{
						var v = _resource.GetBool(curve.index, _frame);
						curve.graphic.enabled = v;
					}
				}

				if (_spriteRendererVisibilityCurves != null)
				{
					foreach (var curve in _spriteRendererVisibilityCurves)
					{
						var v = _resource.GetBool(curve.index, _frame);
						curve.spriteRenderer.enabled = v;
					}
				}

				if (_gameObjectVisibilityCurves != null)
				{
					foreach (var curve in _gameObjectVisibilityCurves)
					{
						var v = _resource.GetBool(curve.index, _frame);
						var obj = curve.gameObject;
						if (v)
						{
							if (obj.activeSelf == false)
							{
								curve.gameObject.SetActive(true);
							}
						}
						else
						{
							if (obj.activeSelf)
							{
								curve.gameObject.SetActive(false);
							}
						}
					}
				}
			}

			if (_nextIsEnd)  //最終フレーム反映終了。完全に止める
			{
				_isPlaying = false;
				_nextIsEnd = false;
				if (_onEnd != null)
				{
					_onEnd();
				}
			}
			else
			{
				var oldFrame = _frame;
				_frame += deltaTime * _resource.frameRate * speed;
				if (_callbackTimeline != null)
				{
					_callbackTimeline.Execute(oldFrame, _frame);
				}

				// ループ処理
				float loopEnd = _cut.loopStart + _cut.loopDuration;
				while ((_frame > loopEnd) && (_currentLoopCount < _cut.loopCount))
				{
					_frame -= _cut.loopDuration; //startに戻すわけではない。
					if (_onLoop != null)
					{
						_onLoop();
					}
					_currentLoopCount++;
				}

				// 終了判定
				float end = _cut.start + _cut.duration;
				if (_frame > end) // ピッタリは終わらせない。最終フレームでspeed0で滞留、みたいなのを許す。
				{
					bool nextExist = false;
					if (_cut.nextCutName != null) // 自動遷移が設定されている
					{
						var cut = _resource.FindCut(_cut.nextCutName);
						if (cut != null)
						{
							_cut = cut;
							_frame = _cut.start + (_frame - end); // 余り分進めておく
							_currentLoopCount = 0;
							nextExist = true;
						}
						else
						{
							Debug.LogError("auto transition failed. " + _cut.nextCutName + " is not found.");
						}
					}

					if (!nextExist)
					{
						_nextIsEnd = true;
						_frame = end - _endTimeEpsilon; // 最終時刻寸前に合わせる
					}
				}
			}
		}
	}
}
