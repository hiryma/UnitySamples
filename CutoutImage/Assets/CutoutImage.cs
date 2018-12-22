using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Kayac
{
	public class CutoutImage : MaskableGraphic
	{
		[SerializeField]
		Sprite _sprite;
		[SerializeField]
		Vector2[] _overrideVertices;
		[SerializeField]
		bool _vertexOverrideEnabled;

		List<Vector2> _localVertices; // ローカル座標に射影した頂点位置(OnPopulateMeshで生成する)
		static Vector2[] _tmpTexcoords; // テンポラリUV計算配列
		Matrix23 _positionToTexcoordTransform; // 手動頂点UV決定用行列

		public Sprite sprite
		{
			get
			{
				return _sprite;
			}
			set
			{
				_sprite = value;
				OnSpriteChange();
			}
		}
		public bool vertexOverrideEnabled
		{
			get
			{
				return _vertexOverrideEnabled;
			}
			set
			{
				_vertexOverrideEnabled = value;
				if (value)
				{
					OnVertexOverrideEnable();
				}
				OnSpriteChange();
			}
		}
		public override Texture mainTexture { get { return (_sprite != null) ? _sprite.texture : null; } }
		public override void SetNativeSize()
		{
			if (_sprite == null)
			{
				Debug.LogError("sprite刺さってないのでできない");
				return;
			}
			var rect = _sprite.rect;
			rectTransform.sizeDelta = new Vector2(
				rect.width,
				rect.height);
		}

		void OnVertexOverrideEnable()
		{
			if ((_overrideVertices == null) || (_overrideVertices.Length == 0))
			{
				_overrideVertices = new Vector2[4];
				_overrideVertices[0] = new Vector2(0f, 0f);
				_overrideVertices[1] = new Vector2(0f, 1f);
				_overrideVertices[2] = new Vector2(1f, 1f);
				_overrideVertices[3] = new Vector2(1f, 0f);
			}
		}

		protected override void Awake()
		{
			base.Awake();
			OnSpriteChange();
		}

		// 同一直線上の3点を取ってしまうことを避けるためのもの。
		static void SelectIndependentVertices(out int i0, out int i1, out int i2, Vector2[] vertices)
		{
			// 0番は必ず使う
			i0 = 0;
			var v0 = vertices[i0];
			// 1番は0番と異なれば何でも良い
			i1 = 1;
			for (int i = i1; i < vertices.Length; i++)
			{
				if ((v0 - vertices[i]).sqrMagnitude > 0f)
				{
					i1 = i;
					break;
				}
			}
			// 2番は0,1となす外積の絶対値が最大になるものを選ぶ
			var v1 = vertices[i1];
			i2 = i1 + 1;
			float maxAbsCross = 0f;
			for (int i = i2; i < vertices.Length; i++)
			{
				var d0 = v0 - vertices[i];
				var d1 = v1 - vertices[i];
				float absCross = Mathf.Abs((d0.x * d1.y) - (d0.y * d1.x));
				if (absCross > maxAbsCross)
				{
					maxAbsCross = absCross;
					i2 = i;
				}
			}
		}

		void CreatePositionToTexcoordTransform()
		{
			if (_sprite == null)
			{
				return;
			}
			/*
			UVと位置の関係を求めるために、位置にある行列を乗ずるとアトラス内のUVになる、
			というような「ある行列」を求める。
			3頂点の位置p0,p1,p2及びUVt0,t1,t2を列ベクタとして並べ、行列P及びTを作る。
			「ある行列」をXとする。すると、T=XP
			要素ごとに書けば以下のようになる
			|t0x t1x t2x|   |m00 m01 m02| |p0x p1x p2x|
			|t0y t1y t2y| = |m10 m11 m12| |p0y p1y p2y|
			|1     1   1|   |  0   0   1| |  1   1   1|

			ここで、右からPの逆行列P^を乗ずれば、TP^=XPP^=Xとなり、Xが求まる
			*/
			int i0, i1, i2;
			SelectIndependentVertices(out i0, out i1, out i2, _sprite.vertices);
			var matT = new Matrix23(ref _sprite.uv[i0], ref _sprite.uv[i1], ref _sprite.uv[i2]); // T
			var matP = new Matrix33(ref _sprite.vertices[i0], ref _sprite.vertices[i1], ref _sprite.vertices[i2]); // P
			if (!matP.Invert()) // 三角形が縮退していて逆行列を作れない
			{
				return;
			}
			var spriteRect = _sprite.rect;
			var spriteWidth = spriteRect.width;
			var spriteHeight = spriteRect.height;
			matT.Multiply(ref matP); // matTにTP^=Xが入った
			matT.Scale( // さらにスプライトの頂点座標をワールドからスプライト内座標に変換する
				spriteWidth / _sprite.pixelsPerUnit,
				spriteHeight / _sprite.pixelsPerUnit);
			matT.Translate( // ピボット分ずらし
				-_sprite.pivot.x / spriteWidth,
				-_sprite.pivot.y / spriteHeight);
			_positionToTexcoordTransform = matT;
		}

		protected override void OnPopulateMesh(VertexHelper vh)
		{
			vh.Clear();
			if (_localVertices == null)
			{
				_localVertices = new List<Vector2>();
			}
			CalcLocalVertices(
				_localVertices,
				_sprite,
				rectTransform,
				_vertexOverrideEnabled ? _overrideVertices : null);

			Vector2[] texcoords;
			ushort[] indices;
			if (_sprite != null)
			{
				if (_vertexOverrideEnabled)
				{
					if ((_tmpTexcoords == null) || (_tmpTexcoords.Length < _localVertices.Count))
					{
						_tmpTexcoords = new Vector2[_localVertices.Count];
					}
					for (int i = 0; i < _overrideVertices.Length; i++)
					{
						_positionToTexcoordTransform.Multiply(out _tmpTexcoords[i], ref _overrideVertices[i]);
					}
					texcoords = _tmpTexcoords;
					indices = null;
				}
				else
				{
					texcoords = _sprite.uv;
					Debug.Assert(_localVertices.Count == texcoords.Length);
					indices = _sprite.triangles;
				}
			}
			else
			{
				texcoords = _overrideVertices; // なんでもいいのでそのまま使う
				indices = null;
			}

			for (int i = 0; i < _localVertices.Count; i++)
			{
				vh.AddVert(_localVertices[i], this.color, texcoords[i]);
			}

			if (indices != null)
			{
				for (int i = 0; i < indices.Length; i += 3)
				{
					vh.AddTriangle(indices[i + 0], indices[i + 1], indices[i + 2]);
				}
			}
			else
			{
				for (int i = 2; i < _overrideVertices.Length; i++)
				{
					vh.AddTriangle(0, i - 1, i);
				}
			}
		}

		static void CalcLocalVertices(
			List<Vector2> verticesOut,
			Sprite sprite,
			RectTransform rectTransform,
			Vector2[] overrideVertices)
		{
			verticesOut.Clear();

			Vector2 spritePivot;
			float spriteWidth, spriteHeight;
			Vector2[] positions;
			float pixelsPerUnitX, pixelsPerUnitY;
			if (sprite != null)
			{
				var spriteRect = sprite.rect;
				spriteWidth = spriteRect.width;
				spriteHeight = spriteRect.height;
				if (overrideVertices != null)
				{
					positions = overrideVertices;
					pixelsPerUnitX = spriteWidth;
					pixelsPerUnitY = spriteHeight;
					spritePivot = new Vector2(0f, 0f);
				}
				else
				{
					spritePivot = sprite.pivot;
					spritePivot.x /= spriteWidth;
					spritePivot.y /= spriteHeight; // [0,1]に正規化
					positions = sprite.vertices;
					pixelsPerUnitX = pixelsPerUnitY = sprite.pixelsPerUnit;
				}
			}
			else
			{
				spriteWidth = spriteHeight = 1f;
				spritePivot = new Vector2(0.5f, 0.5f);
				positions = overrideVertices;
				pixelsPerUnitX = pixelsPerUnitY = 1f;
			}
			Matrix23NoRotation positionTransform = new Matrix23NoRotation();
			positionTransform.SetIdentity();
			var rectSizeDelta = rectTransform.sizeDelta;
			positionTransform.ScaleFromLeft(
 				pixelsPerUnitX * rectSizeDelta.x / spriteWidth,
				pixelsPerUnitY * rectSizeDelta.y / spriteHeight);
			var rectPivot = rectTransform.pivot;
			positionTransform.TranslateFromLeft(
				(spritePivot.x - rectPivot.x) * rectSizeDelta.x,
				(spritePivot.y - rectPivot.y) * rectSizeDelta.y);
			for (int i = 0; i < positions.Length; i++)
			{
				Vector2 pos;
				positionTransform.Multiply(out pos, ref positions[i]);
				verticesOut.Add(pos);
			}
		}

		void OnSpriteChange()
		{
			CreatePositionToTexcoordTransform();
#if UNITY_EDITOR
			CreateGizmoEdgeTable();
#endif
		}

#if UNITY_EDITOR
		static List<Vector3> _worldVertices; // キャッシュ用
		static bool _vertexGuiEditEnabled; // 頂点ハンドル有効
		HashSet<int> _gizmoEdges; // 上16bitと下16bitを分割して頂点番号とし、それで辺を描画する

		protected override void OnValidate() // sprite変更時だけで良い
		{
			base.OnValidate();
			if (_vertexOverrideEnabled)
			{
				OnVertexOverrideEnable();
			}
			OnSpriteChange();
		}

		void CreateGizmoEdgeTable()
		{
			if (_gizmoEdges == null)
			{
				_gizmoEdges = new HashSet<int>();
			}
			_gizmoEdges.Clear();
			if (_vertexOverrideEnabled)
			{
				int prev = _overrideVertices.Length - 1;
				for (int i = 0; i < _overrideVertices.Length; i++)
				{
					_gizmoEdges.Add((i << 16) | prev);
					prev = i;
				}
			}
			else if (_sprite != null)
			{
				var indices = _sprite.triangles;
				for (int i = 0; i < indices.Length; i += 3)
				{
					var i0 = indices[i + 0];
					var i1 = indices[i + 1];
					var i2 = indices[i + 2];
					var key0 = (i0 > i1) ? ((i0 << 16) | i1) : ((i1 << 16) | i0);
					var key1 = (i1 > i2) ? ((i1 << 16) | i2) : ((i2 << 16) | i1);
					var key2 = (i2 > i0) ? ((i2 << 16) | i0) : ((i0 << 16) | i2);
					_gizmoEdges.Add(key0);
					_gizmoEdges.Add(key1);
					_gizmoEdges.Add(key2);
				}
			}
		}

		void OnDrawGizmosSelected()
		{
			// 全頂点のローカル座標をワールド座標に射影して描画する。
			if (_localVertices == null)
			{
				_localVertices = new List<Vector2>();
			}
			CalcLocalVertices(
				_localVertices,
				_sprite,
				rectTransform,
				_vertexOverrideEnabled ? _overrideVertices : null);
			// ワールド変換
			if (_worldVertices == null)
			{
				_worldVertices = new List<Vector3>();
			}
			_worldVertices.Clear();
			var toWorld = rectTransform.localToWorldMatrix;
			for (int i = 0; i < _localVertices.Count; i++)
			{
				var p = new Vector3(
					_localVertices[i].x,
					_localVertices[i].y,
					rectTransform.anchoredPosition3D.z);
				p = toWorld.MultiplyPoint3x4(p);
				_worldVertices.Add(p);
			}

			Gizmos.color = new Color(0f, 1f, 0f, 1f);
			foreach (var item in _gizmoEdges)
			{
				var i0 = item & 0xffff;
				var i1 = item >> 16;
				Gizmos.DrawLine(_worldVertices[i0], _worldVertices[i1]);
			}
		}

		[CustomEditor(typeof(CutoutImage), true)]
		public class CutoutImageInspector : Editor
		{
			public override void OnInspectorGUI()
			{
				base.OnInspectorGUI();
				var self = (CutoutImage)target;

				CutoutImage._vertexGuiEditEnabled = GUILayout.Toggle(CutoutImage._vertexGuiEditEnabled, "GUI Vertex Editing");
				if (GUILayout.Button("Set Native Size"))
				{
					self.SetNativeSize();
				}
			}

			private void OnSceneGUI()
			{
				if (!CutoutImage._vertexGuiEditEnabled) // 頂点編集モードでないなら抜ける
				{
					return;
				}

				var self = target as CutoutImage;
				var vertices = self._overrideVertices;
				if ((vertices == null) || (vertices.Length == 0))
				{
					return;
				}

				// 頂点ごとにローカル座標を導出し、ワールド座標に変換して描画。
				// ハンドルの入力を取得し、動いていればローカル化して更新。
				var rectTransform = self.rectTransform;
				var dstSizeDelta = rectTransform.sizeDelta;
				Vector2 newLocalPos = Vector2.zero;
				Handles.matrix = rectTransform.localToWorldMatrix;
				bool dirty = false;
				for (int i = 0; i < vertices.Length; i++)
				{
					if (DrawAndCheckHandle(out newLocalPos, ref vertices[i], ref dstSizeDelta))
					{
						Undo.RecordObject(self, "Modify Override Vertex");
						vertices[i] = newLocalPos;
						dirty = true;
					}
				}
				if (dirty)
				{
					self.SetVerticesDirty();
				}
			}

			bool DrawAndCheckHandle(
				out Vector2 newNormalizedLocalPos,
				ref Vector2 normalizedLocalPos,
				ref Vector2 sizeDelta)
			{
				var local3d = new Vector3(
					normalizedLocalPos.x,
					normalizedLocalPos.y,
					0f);
				local3d.x -= 0.5f;
				local3d.y -= 0.5f;
				local3d.x *= sizeDelta.x;
				local3d.y *= sizeDelta.y; // 正規化座標→ピクセル座標

				var handleSize = HandleUtility.GetHandleSize(local3d) * 0.2f;
				var newLocal3d = Handles.FreeMoveHandle(
					local3d,
					Quaternion.identity,
					handleSize,
					new Vector3(1f, 1f, 0f),
					Handles.CircleHandleCap);
				newNormalizedLocalPos.x = (newLocal3d.x / sizeDelta.x) + 0.5f;
				newNormalizedLocalPos.y = (newLocal3d.y / sizeDelta.y) + 0.5f;
				// 汚ない数が出てくると鬱陶しいので量子化
				newNormalizedLocalPos.x = (int)(newNormalizedLocalPos.x * 1024f) / 1024f;
				newNormalizedLocalPos.y = (int)(newNormalizedLocalPos.y * 1024f) / 1024f;
				var ret = false;
				if ((newNormalizedLocalPos - normalizedLocalPos).sqrMagnitude > 1e-4f)
				{
					ret = true;
				}
				return ret;
			}
		}
#endif

		struct Matrix23NoRotation
		{
			public float m00, m02;
			public float m11, m12;

			// アフィン拡張された3ベクタから生成
			public void SetIdentity()
			{
				m00 = m11 = 1f;
				m02 = m12 = 0f;
			}

			public void TranslateFromLeft(float x, float y)
			{
				/*
				|1 0 x| |m00  0  m02|   |m00   0 m02+x|
				|0 1 y| |  0 m11 m12| = |  0 m11 m12+y|
				*/
				m02 += x;
				m12 += y;
			}

			public void TranslateFromLeft(ref Vector2 v)
			{
				TranslateFromLeft(v.x, v.y);
			}

			public void Translate(float x, float y)
			{
				/*
				|m00  0  m02||1 0 x|    |m00   0 m00*x+m02|
				|  0 m11 m12||0 1 y|  = |  0 m11 m11*y+m12|
				*/
				m02 += (m00 * x);
				m12 += (m11 * y);
			}

			public void Translate(ref Vector2 v)
			{
				Translate(v.x, v.y);
			}

			public void ScaleFromLeft(float x, float y)
			{
				/*
				|x 0 0| |m00  0  m02|   |x*m00     0 x*m02|
				|0 y 0| |  0 m11 m12| = |    0 y*m11 y*m12|
				*/
				m00 *= x;
				m02 *= x;
				m11 *= y;
				m12 *= y;
			}

			public void ScaleFromLeft(ref Vector2 v)
			{
				ScaleFromLeft(v.x, v.y);
			}

			public void Scale(float x, float y)
			{
				/*
				|m00  0  m02||x 0 0|    |m00*x     0 m02|
				|  0 m11 m12||0 y 0|  = |    0 m11*y m12|
				*/
				m00 *= x;
				m11 *= y;
			}

			public void Scale(ref Vector2 v)
			{
				Scale(v.x, v.y);
			}

			// z=1にアフィン拡張して乗算
			public void Multiply(out Vector2 to, ref Vector2 from)
			{
				float tx = (m00 * from.x) + m02;
				to.y = (m11 * from.y) + m12;
				to.x = tx;
			}

			public void Dump() // デバグ用
			{
				string t = "| " + m00 + "\t0\t" + m02 + "|\n";
				t += "|0\t" + m11 + "\t" + m12 + "|";
				Debug.Log(t);
			}
		}

		struct Matrix23
		{
			public float m00, m01, m02;
			public float m10, m11, m12;

			// アフィン拡張された3ベクタから生成
			public Matrix23(ref Vector2 colVector0, ref Vector2 colVector1, ref Vector2 colVector2)
			{
				m00 = colVector0.x;
				m10 = colVector0.y;
				m01 = colVector1.x;
				m11 = colVector1.y;
				m02 = colVector2.x;
				m12 = colVector2.y;
			}

			public void Multiply(ref Matrix33 a)
			{
				var t00 = (m00 * a.m00) + (m01 * a.m10) + (m02 * a.m20);
				var t01 = (m00 * a.m01) + (m01 * a.m11) + (m02 * a.m21);
				var t02 = (m00 * a.m02) + (m01 * a.m12) + (m02 * a.m22);

				var t10 = (m10 * a.m00) + (m11 * a.m10) + (m12 * a.m20);
				var t11 = (m10 * a.m01) + (m11 * a.m11) + (m12 * a.m21);
				var t12 = (m10 * a.m02) + (m11 * a.m12) + (m12 * a.m22);

				m00 = t00;
				m01 = t01;
				m02 = t02;
				m10 = t10;
				m11 = t11;
				m12 = t12;
			}

			// a*bで初期化
			public Matrix23(ref Matrix23 a, ref Matrix33 b)
			{
				var t00 = (a.m00 * b.m00) + (a.m01 * b.m10) + (a.m02 * b.m20);
				var t01 = (a.m00 * b.m01) + (a.m01 * b.m11) + (a.m02 * b.m21);
				var t02 = (a.m00 * b.m02) + (a.m01 * b.m12) + (a.m02 * b.m22);

				var t10 = (a.m10 * b.m00) + (a.m11 * b.m10) + (a.m12 * b.m20);
				var t11 = (a.m10 * b.m01) + (a.m11 * b.m11) + (a.m12 * b.m21);
				var t12 = (a.m10 * b.m02) + (a.m11 * b.m12) + (a.m12 * b.m22);

				m00 = t00;
				m01 = t01;
				m02 = t02;
				m10 = t10;
				m11 = t11;
				m12 = t12;
			}

			public void TranslateFromLeft(float x, float y)
			{
				/*
				|1 0 x| |m00 m01 m02|   |m00 m01 m02+x|
				|0 1 y| |m10 m11 m12| = |m10 m11 m12+y|
				*/
				m02 += x;
				m12 += y;
			}

			public void TranslateFromLeft(ref Vector2 v)
			{
				TranslateFromLeft(v.x, v.y);
			}

			public void Translate(float x, float y)
			{
				/*
				|m00 m01 m02||1 0 x|    |m00 m01 m00*x+m01*y+m02|
				|m10 m11 m12||0 1 y|  = |m10 m11 m10*x+m11*y+m12|
				*/
				m02 += (m00 * x) + (m01 * y);
				m12 += (m10 * x) + (m11 * y);
			}

			public void Translate(ref Vector2 v)
			{
				Translate(v.x, v.y);
			}

			public void ScaleFromLeft(float x, float y)
			{
				/*
				|x 0 0| |m00 m01 m02|   |x*m00 x*m01 x*m02|
				|0 y 0| |m10 m11 m12| = |y*m10 y*m11 y*m12|
				*/
				m00 *= x;
				m01 *= x;
				m02 *= x;
				m10 *= y;
				m11 *= y;
				m12 *= y;
			}

			public void ScaleFromLeft(ref Vector2 v)
			{
				ScaleFromLeft(v.x, v.y);
			}

			public void Scale(float x, float y)
			{
				/*
				|m00 m01 m02||x 0 0|    |m00*x m01*y m02|
				|m10 m11 m12||0 y 0|  = |m10*x m11*y m12|
				*/
				m00 *= x;
				m01 *= y;
				m10 *= x;
				m11 *= y;
			}

			public void Scale(ref Vector2 v)
			{
				Scale(v.x, v.y);
			}

			// z=1にアフィン拡張して乗算
			public void Multiply(out Vector2 to, ref Vector2 from)
			{
				float tx = (m00 * from.x) + (m01 * from.y) + m02;
				to.y = (m10 * from.x) + (m11 * from.y) + m12;
				to.x = tx;
			}

			public void Dump() // デバグ用
			{
				string t = "| " + m00 + "\t" + m01 + "\t" + m02 + "|\n";
				t += "| " + m10 + "\t" + m11 + "\t" + m12 + "|";
				Debug.Log(t);
			}
		}

		struct Matrix33
		{
			public float m00, m01, m02;
			public float m10, m11, m12;
			public float m20, m21, m22;

			// アフィン拡張された3ベクタから生成
			public Matrix33(ref Vector2 colVector0, ref Vector2 colVector1, ref Vector2 colVector2)
			{
				m00 = colVector0.x;
				m10 = colVector0.y;
				m01 = colVector1.x;
				m11 = colVector1.y;
				m02 = colVector2.x;
				m12 = colVector2.y;
				m20 = m21 = m22 = 1f;
			}

			public void Dump() // デバグ用
			{
				string t = "| " + m00 + "\t" + m01 + "\t" + m02 + "|\n";
				t += "| " + m10 + "\t" + m11 + "\t" + m12 + "|\n";
				t += "| " + m20 + "\t" + m21 + "\t" + m22 + "|";
				Debug.Log(t);
			}

			public bool Invert()
			{
				/*
				|m00 m01 m02|
				|m10 m11 m12|
				|m20 m21 m22|
				の逆行列。
				*/
				// t__は余因子
				var t00 = (m11 * m22) - (m12 * m21);
				var t01 = (m12 * m20) - (m22 * m10);
				var t02 = (m10 * m21) - (m11 * m20);
				var t10 = (m21 * m02) - (m22 * m01);
				var t11 = (m22 * m00) - (m20 * m02);
				var t12 = (m20 * m01) - (m21 * m00);
				var t20 = (m01 * m12) - (m02 * m11);
				var t21 = (m02 * m10) - (m00 * m12);
				var t22 = (m00 * m11) - (m01 * m10);

				var det = (m00 * t00) + (m01 * t01) + (m02 * t02);
				if (det == 0f)
				{
					return false;
				}

				var rcpDet = 1f / det;

				// 余因子を転置しつつ行列式で除して完成
				m00 = t00 * rcpDet;
				m01 = t10 * rcpDet;
				m02 = t20 * rcpDet;
				m10 = t01 * rcpDet;
				m11 = t11 * rcpDet;
				m12 = t21 * rcpDet;
				m20 = t02 * rcpDet;
				m21 = t12 * rcpDet;
				m22 = t22 * rcpDet;
				return true;
			}
		}
	}
}
