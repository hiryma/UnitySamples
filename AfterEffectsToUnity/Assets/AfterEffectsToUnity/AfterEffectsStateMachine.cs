using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AfterEffectsToUnity
{
	public class AfterEffectsStateMachine : MonoBehaviour
	{
		[SerializeField]
		private AfterEffectsAnimation[] _animations;
		private Dictionary<string, AfterEffectsAnimation> _map;
		private Dictionary<AfterEffectsAnimation, AfterEffectsAnimation> _autoTransitions;

		// 現在再生中のもの
		private AfterEffectsAnimation _current;

		// 遷移予定のもの。nullなら現状維持
		private AfterEffectsAnimation _next;

		// 停止予定かどうか
		private bool _stopRequest;
		private bool _initialized;

		protected string currentName
		{
			get
			{
				return (_current != null) ? _current.name : null;
			}
		}

		protected AfterEffectsAnimation currentAnimation
		{
			get
			{
				return _current;
			}
		}

		public void Initialize()
		{
			// 二度はしない
			if (!_initialized)
			{
				if (_map == null)
				{
					_map = new Dictionary<string, AfterEffectsAnimation>();
					foreach (var item in _animations)
					{
						item.gameObject.SetActive(false); // 寝かせて開始
						_map.Add(item.name, item);
					}
				}
				if (_autoTransitions == null)
				{
					_autoTransitions = new Dictionary<AfterEffectsAnimation, AfterEffectsAnimation>();
				}

				foreach (var item in _animations)
				{
					item.Initialize();
				}
				_initialized = true;
			}
		}

		public virtual void Dispose()
		{
			foreach (var item in _animations)
			{
				item.Dispose();
			}
			_animations = null;
			_map = null;
			_autoTransitions = null;
			_current = null;
			_next = null;
		}

		public bool IsDisposed()
		{
			return (_animations == null);
		}

		protected void Play(string name)
		{
			RequestMove(name, false);
		}

		protected void Stop(bool waitLoopOrEnd = true)
		{
			RequestMove(null, waitLoopOrEnd);
		}

		protected void RequestMove(string name, bool waitLoopOrEnd = false)
		{
			// nullなら終了への遷移とする
			if (name == null)
			{
				_next = null;
				_stopRequest = true;
			}
			else
			{
				// 多重初期化は中でチェックしている
				Initialize();

				// 見つからなければそもそも遷移を行わない
				if (_map.ContainsKey(name))
				{
					_next = _map[name];
				}
			}

			if (!waitLoopOrEnd // ループ/終了を待たないなら即座
				|| (waitLoopOrEnd && (_current == null))) // _currentがnullなら即座
			{
				Move(false, false);
			}
		}

		// 自動遷移設定。自動遷移するアニメは連結して一つとみなすので、RequestMoveしても連結先が終わるまで行かない
		protected void AddAutoTransition(string from, string to)
		{
			Initialize();
			if (_map.ContainsKey(from) && _map.ContainsKey(to))
			{
				_autoTransitions.Add(_map[from], _map[to]);
			}
		}

		private void Move(bool currentIsLooping, bool useAutoTransition)
		{
			AfterEffectsAnimation to = null;
			if (useAutoTransition && !currentIsLooping) // ループでなければ自動遷移を参照
			{
				if (_current != null)
				{
					// 自動遷移が設定されていれば、
					if (_autoTransitions.ContainsKey(_current))
					{
						to = _autoTransitions[_current];
					}
				}
			}

			if (to == null) // 自動遷移が設定されてない場合_nextを使用
			{
				to = _next;
			}

			if (to == _current) // 行き先が今と同じなら単に巻き戻し
			{
				_current.SetNormalizedTime(0f);
			}
			else
			{
				// 停止
				if (_current != null)
				{
					if ((to != null) // 行き先が存在する場合
						|| _stopRequest // 強制停止を要求された場合
						|| !currentIsLooping) // アニメが終了している(=ループでない)
					{
						_current.gameObject.SetActive(false);
						_current = null;
						_next = null;
						_stopRequest = false;
					}
				}

				// 新規再生
				if (to != null)
				{
					_current = to;
					if (!_current.gameObject.activeSelf)
					{
						_current.gameObject.SetActive(true);
					}
					_current.Play(OnEnd, OnLoop);
				}
			}
		}

		private void OnLoop()
		{
			Move(true, useAutoTransition: true);
		}

		private void OnEnd()
		{
			Move(false, useAutoTransition: true);
		}

#if UNITY_EDITOR
		[CustomEditor(typeof(AfterEffectsStateMachine), true)]
		public class AfterEffectsStateMachineInspector : Editor
		{
			private string _animationName;
			public override void OnInspectorGUI()
			{
				base.OnInspectorGUI();
				var self = (AfterEffectsStateMachine)target;
				EditorGUILayout.Space();

				_animationName = EditorGUILayout.TextField("アニメ名", _animationName);
				EditorGUILayout.LabelField("再生中アニメ名", self.currentName);

				if (GUILayout.Button("再生"))
				{
					self.Play(_animationName);
				}
			}
		}
#endif
	}
}
