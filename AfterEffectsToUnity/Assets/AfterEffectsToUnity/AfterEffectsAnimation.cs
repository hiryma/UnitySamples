using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AfterEffectsToUnity
{
	public abstract class AfterEffectsAnimation : MonoBehaviour
	{
		public const int Infinity = AfterEffectsResource.Infinity;

		[SerializeField]
		private string _name;

		public string currentCutName
		{
			get
			{
				// 未初期化の状態で呼ばれてしまった場合、この場で初期化する。スパイクするので避けて欲しいが。
				if (isInitialized == false)
				{
					Initialize();
				}
				return _instance.currentCutName;
			}
		}

		new public string name
		{
			get
			{
				if (string.IsNullOrEmpty(_name))
				{
					_name = GetType().Name;
				}
				return _name;
			}
		}

		public bool updateEnabled{ get; set; }

		public float currentFrame
		{
			get
			{
				if (_instance != null)
				{
					return _instance.currentFrame;
				}
				else
				{
					return 0f;
				}
			}
		}

		protected abstract void InitializeInstance(AfterEffectsInstance instance);
		protected abstract void SetFirstFrame();
		protected abstract AfterEffectsResource GetResource();

		private AfterEffectsInstance _instance;
		public AfterEffectsInstance instance { get { return _instance; } }
		private int _lastUpdateFrame;
		private bool isInitialized { get { return _instance != null; } }

		private void Awake()
		{
			updateEnabled = true;
		}

		public void AddCallback(int frame, System.Action action)
		{
			Debug.Assert(_instance != null);
			_instance.AddCallback(frame, action);
		}

		/// Awakeのタイミングで負荷をかけたくない場合、任意のタイミングで呼ぶことができる
		public void Initialize()
		{
			if (isInitialized)
			{
				return;
			}
			_lastUpdateFrame = -0x7fffffff;
			var resource = GetResource();
			_instance = new AfterEffectsInstance(resource);
			InitializeInstance(_instance);
		}

		public virtual void Dispose()
		{
			_instance = null;
			_name = null;
		}

		public bool IsDisposed()
		{
			return (_instance == null);
		}

		// 現カットの時刻を指定して飛ぶ。0が始まりで1が終わり。終わりとは、「最後のフレーム」
		public void SetNormalizedTime(float normalizedTime)
		{
			_instance.normalizedTime = normalizedTime;
		}

		// 派生で追加処理があれば書く
		protected virtual void OnPlay()
		{
		}

		/// 現カットを最初から再生
		public void Play(Action onEnd = null, Action onLoop = null)
		{
			Play(null, onEnd, onLoop);
		}

		// カットを指定して再生開始。cutNameがnullなら現カットを最初から再生
		public void Play(string cutName, Action onEnd = null, Action onLoop = null)
		{
			Initialize();
			_instance.Rewind(cutName, onEnd, onLoop);
			OnPlay();
			_instance.Play();
			UpdateManually(0f); // ゴミが出る前に初回更新(したくないよー)
		}

		/// ポーズがかかっていればfalseが返ることに注意
		public bool isPlaying
		{
			get
			{
				return (_instance != null) ? _instance.isPlaying : false;
			}
		}

		private void Update()
		{
			if (updateEnabled)
			{
				UpdateManually(Time.deltaTime);
			}
		}

		protected virtual void OnPreUpdate(float deltaTime)
		{
		}

		public virtual void UpdateManually(float deltaTime)
		{
			int frame = Time.frameCount;
			if ((_instance != null) && _instance.isPlaying && (frame > _lastUpdateFrame)) // 同じフレームならUpdateとUpdatePerFrameはどちらかしか呼ばれない
			{
				_lastUpdateFrame = frame;
				OnPreUpdate(deltaTime);
				_instance.UpdatePerFrame(deltaTime);
				OnPostUpdate(deltaTime);
			}
		}

		protected virtual void OnPostUpdate(float deltaTime)
		{
		}

		public void Pause()
		{
			if (_instance != null)
			{
				_instance.Pause();
			}
		}

#if UNITY_EDITOR
		// エディタでも初期配置だけはする
		public void OnValidate()
		{
			if (gameObject.transform.childCount > 0)
			{
				SetFirstFrame();
			}
		}

		protected virtual void BuildHierarchy()
		{
			Debug.LogWarning("BuildHierarchy for " + GetType().FullName + " is Not Implemented.");
		}

		[CustomEditor(typeof(AfterEffectsAnimation), true)]
		public class AfterEffectsAnimationInspector : Editor
		{
			private string _cutName = "";
			private int _frame = 0;
			private int _speedPercent = 100;
			public override void OnInspectorGUI()
			{
				base.OnInspectorGUI();
				var self = (AfterEffectsAnimation)target;
				EditorGUILayout.Space();

				if (GUILayout.Button("初期化"))
				{
					self.Initialize();
				}
				if (GUILayout.Button("第一フレームセット"))
				{
					self.SetFirstFrame();
				}
				var instance = self._instance;
				if (instance != null)
				{
					_cutName = EditorGUILayout.TextField("カット名", _cutName);
					var newFrame = EditorGUILayout.IntField("フレーム", _frame);
					if (newFrame != _frame)
					{
						instance.currentFrame = newFrame;
						_frame = newFrame;
					}
					_speedPercent = EditorGUILayout.IntField("再生速度%", _speedPercent);
					instance.speed = (float)_speedPercent * 0.01f;
					EditorGUILayout.LabelField("再生中カット名：フレーム", instance.currentCutName + " " + instance.currentFrame.ToString("N1"));

					if (GUILayout.Button("再生"))
					{
						self.Play(_cutName);
					}
					if (GUILayout.Button("ポーズ"))
					{
						instance.Pause();
					}
				}

				if (GUILayout.Button("オブジェクト生成"))
				{
					if (self.gameObject.transform.childCount > 0)
					{
						Debug.LogWarning("hierarchy is not empty. ignore BuildHierarchy().");
					}
					else
					{
						Debug.LogWarning("Call BuildHierarchy()");
						self.BuildHierarchy();
					}
				}
			}
		}
#endif
	}
}
