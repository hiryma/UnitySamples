using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Kayac
{
	public class Edge
	{
		public Edge(int v0, int v1)
		{
			this.v0 = v0;
			this.v1 = v1;
		}
		public int v0, v1;
	}
	public class Face
	{
		public Face(int e0, int e1, int e2)
		{
			this.e0 = e0;
			this.e1 = e1;
			this.e2 = e2;
		}
		public int e0, e1, e2;
	}
	public class Vertex
	{
		public Vertex(Vector3 position, Vector3 normal, Vector3 uv)
		{
			this.position = position;
			this.normal = normal;
			this.uv = uv;
		}
		public Vector3 position;
		public Vector3 normal;
		public Vector2 uv;
	}
	public class VertexEdgeFaceTable
	{
		public VertexEdgeFaceTable()
		{
			vertices = new List<Vertex>();
			edges = new List<Edge>();
			faces = new List<Face>();
		}

		public VertexEdgeFaceTable(
			IList<Vector3> positions,
			IList<Vector3> normals,
			IList<Vector2> uvs,
			IList<int> indices)
		{
			vertices = new List<Vertex>();
			edges = new List<Edge>();
			faces = new List<Face>();

			// まず頂点充填
			Debug.Assert(positions.Count == normals.Count);
			if (uvs != null)
			{
				Debug.Assert(positions.Count == uvs.Count);
				for (int i = 0; i < positions.Count; i++)
				{
					vertices.Add(new Vertex(positions[i], normals[i], uvs[i]));
				}
			}
			else
			{
				for (int i = 0; i < positions.Count; i++)
				{
					vertices.Add(new Vertex(positions[i], normals[i], Vector2.zero));
				}
			}

			// 次に辺を充填。HashSetで重複を回避する。
			var edgeSet = new HashSet<uint>();
			for (int i = 0; i < indices.Count; i += 3)
			{
				var vi0 = indices[i + 0];
				var vi1 = indices[i + 1];
				var vi2 = indices[i + 2];
				var e01 = EdgeKey(vi0, vi1);
				var e12 = EdgeKey(vi1, vi2);
				var e20 = EdgeKey(vi2, vi0);
				if (!edgeSet.Contains(e01))
				{
					edgeSet.Add(e01);
				}
				if (!edgeSet.Contains(e12))
				{
					edgeSet.Add(e12);
				}
				if (!edgeSet.Contains(e20))
				{
					edgeSet.Add(e20);
				}
			}

			// 辺セットを配列に充填しつつ、頂点インデクス→辺インデクスの辞書を用意
			var edgeMap = new Dictionary<uint, int>();
			foreach (var edgeKey in edgeSet)
			{
				var vi0 = (int)(edgeKey >> 16);
				var vi1 = (int)(edgeKey & 0xffff);
				edgeMap.Add(edgeKey, edges.Count);
				edges.Add(new Edge(vi0, vi1));
			}

			// 面を充填開始
			for (int i = 0; i < indices.Count; i += 3)
			{
				var vi0 = indices[i + 0];
				var vi1 = indices[i + 1];
				var vi2 = indices[i + 2];
				var e01 = EdgeKey(vi0, vi1);
				var e12 = EdgeKey(vi1, vi2);
				var e20 = EdgeKey(vi2, vi0);
				var ei01 = edgeMap[e01];
				var ei12 = edgeMap[e12];
				var ei20 = edgeMap[e20];
				faces.Add(new Face(ei01, ei12, ei20));
			}
		}

		public uint EdgeKey(int vi0, int vi1)
		{
			Debug.Assert(vi0 <= 0xffff);
			Debug.Assert(vi1 <= 0xffff);
			if (vi0 > vi1)
			{
				var tmp = vi0;
				vi0 = vi1;
				vi1 = tmp;
			}
			return (uint)((vi0 << 16) | vi1);
		}

		public override string ToString()
		{
			var sb = new System.Text.StringBuilder();
			sb.AppendLine("[Vertices]");
			for (int i = 0; i < vertices.Count; i++)
			{
				var v = vertices[i];
				sb.AppendFormat("\t{0}: {1} {1} {2}\n", i, v.position, v.normal, v.uv);
			}
			sb.AppendLine("[Edges]");
			for (int i = 0; i < edges.Count; i++)
			{
				sb.AppendFormat("\t{0}: {1} {2}\n", i, edges[i].v0, edges[i].v1);
			}
			sb.AppendLine("[Faces]");
			for (int i = 0; i < faces.Count; i++)
			{
				sb.AppendFormat("\t{0}: {1} {2} {3}\n", i, faces[i].e0, faces[i].e1, faces[i].e2);
			}
			return sb.ToString();
		}

		public void SubDivide()
		{
			var oldVn = vertices.Count;
			var oldEn = edges.Count;
			var oldFn = faces.Count;
			// 全edgeの中点を生成して追加
			for (int i = 0; i < oldEn; i++)
			{
				var edge = edges[i];
				var v0 = vertices[edge.v0];
				var v1 = vertices[edge.v1];
				var midPoint = (v0.position + v1.position) * 0.5f;
				var midNormal = (v0.normal + v1.normal).normalized;
				var midUv = (v0.uv + v1.uv) * 0.5f;
				vertices.Add(new Vertex(midPoint, midNormal, midUv));
			}

			// faceの分割。edgeが古いうちにやる
			for (int i = 0; i < oldFn; i++)
			{
				var face = faces[i];
				var e0 = edges[face.e0];
				var e1 = edges[face.e1];
				var e2 = edges[face.e2];
				// 4分割する
				// 関連する辺は3 -> 9
				// e0の中点とe1の中点からなる新エッジ = oldEn + (i * 3) + 0
				// e1の中点とe2の中点からなる新エッジ = oldEn + (i * 3) + 1
				// e2の中点とe0の中点からなる新エッジ = oldEn + (i * 3) + 2
				// e0は、e0とoldEn + (oldFn * 3) + e0 に分割
				// e1は、e1とoldEn + (oldFn * 3) + e1 に分割
				// e2は、e2とoldEn + (oldFn * 3) + e2 に分割

				var newFace0 = MakeDividedFace(i, 0, oldEn, oldFn, face.e0, face.e1);
				var newFace1 = MakeDividedFace(i, 1, oldEn, oldFn, face.e1, face.e2);
				var newFace2 = MakeDividedFace(i, 2, oldEn, oldFn, face.e2, face.e0);
				faces.Add(newFace0);
				faces.Add(newFace1);
				faces.Add(newFace2);
				// 辺を生成
				var newEdge0 = new Edge(oldVn + face.e0, oldVn + face.e1);
				var newEdge1 = new Edge(oldVn + face.e1, oldVn + face.e2);
				var newEdge2 = new Edge(oldVn + face.e2, oldVn + face.e0);
				edges.Add(newEdge0);
				edges.Add(newEdge1);
				edges.Add(newEdge2);
				// 自分は中点3点からなる面に変換
				face.e0 = oldEn + (i * 3) + 0;
				face.e1 = oldEn + (i * 3) + 1;
				face.e2 = oldEn + (i * 3) + 2;
			}

			// edgeの分割
			for (int i = 0; i < oldEn; i++)
			{
				var edge = edges[i];
				// 新しい頂点を使ってedgeを分割
				var midVi = oldVn + i;
				var newEdge = new Edge(midVi, edge.v1);
				edges.Add(newEdge);
				// 自分は終点を新しい点に変更
				edge.v1 = midVi;
			}
		}

		Face MakeDividedFace(int fi, int divIndex, int oldEn, int oldFn, int oldEi0, int oldEi1)
		{
			// 新しく面を生成
			int ei0 = oldEn + (fi * 3) + divIndex;
			int ei1, ei2;
			Edge e0 = edges[oldEi0];
			Edge e1 = edges[oldEi1];
			if ((e0.v0 == e1.v0) || (e0.v0 == e1.v1)) // e0.v0が頂点
			{
				ei1 = oldEi0;
				if (e0.v0 == e1.v0)
				{
					ei2 = oldEi1;
				}
				else
				{
					ei2 = oldEn + (oldFn * 3) + oldEi1;
				}
			}
			else if ((e0.v1 == e1.v0) || (e0.v1 == e1.v1)) // e0.v1が頂点
			{
				ei1 = oldEn + (oldFn * 3) + oldEi0;
				if (e0.v1 == e1.v0)
				{
					ei2 = oldEi1;
				}
				else
				{
					ei2 = oldEn + (oldFn * 3) + oldEi1;
				}
			}
			else // バグ
			{
				Debug.Assert(false);
				ei1 = ei2 = int.MaxValue; // 死ぬべき
			}
			var newFace = new Face(ei0, ei1, ei2);
			return newFace;
		}

		public void GetArrays(
			out Vector3[] verticesOut,
			out Vector3[] normalsOut,
			out Vector2[] uvsOut,
			out int[] indicesOut)
		{
			verticesOut = new Vector3[vertices.Count];
			normalsOut = new Vector3[vertices.Count];
			uvsOut = new Vector2[vertices.Count];
			for (int i = 0; i < vertices.Count; i++)
			{
				verticesOut[i] = vertices[i].position;
				normalsOut[i] = vertices[i].normal;
				uvsOut[i] = vertices[i].uv;
			}
			indicesOut = MakeIndices();
		}

		public void GetArrays(
			out Vector3[] verticesOut,
			out Vector3[] normalsOut,
			out int[] indicesOut)
		{
			Vector2[] uvsUnused;
			GetArrays(out verticesOut, out normalsOut, out uvsUnused, out indicesOut);
		}

		public int[] MakeIndices()
		{
			var ret = new int[faces.Count * 3]; // 数は決まっている
			for (int i = 0; i < faces.Count; i++) // 各面についてインデクスを生成
			{
				int vi0, vi1, vi2;
				MakeIndices(out vi0, out vi1, out vi2, i);
				// 法線とのチェック
				var v0 = vertices[vi0].position;
				var v1 = vertices[vi1].position;
				var v2 = vertices[vi2].position;
				var cross = Vector3.Cross(v1 - v0, v2 - v0);
				var dp = Vector3.Dot(cross, vertices[vi0].normal);
				if (dp < 0f) // 法線が合わないので反転
				{
					var tmp = vi1;
					vi1 = vi2;
					vi2 = tmp;
				}
				ret[(i * 3) + 0] = vi0;
				ret[(i * 3) + 1] = vi1;
				ret[(i * 3) + 2] = vi2;
			}
			return ret;
		}

		void MakeIndices(out int vi0, out int vi1, out int vi2, int fi)
		{
			var f = faces[fi];
			var e0 = edges[f.e0];
			var e1 = edges[f.e1];
			var e2 = edges[f.e2];
			vi0 = e0.v0;
			vi1 = e0.v1;
			if ((e1.v0 != vi0) && (e1.v0 != vi1))
			{
				vi2 = e1.v0;
			}
			else
			{
				vi2 = e1.v1;
			}
			// e2の頂点は両方すでに見つかっているはず
			Debug.Assert((e2.v0 == vi0) || (e2.v0 == vi1) || (e2.v0 == vi2));
			Debug.Assert((e2.v1 == vi0) || (e2.v1 == vi1) || (e2.v1 == vi2));
		}

		public List<Vertex> vertices;
		public List<Edge> edges;
		public List<Face> faces;
	}

	public static class MeshGenerator
	{
		public static bool GenerateSphere(
			out Vector3[] verticesOut,
			out Vector3[] normalsOut,
			out int[] indicesOut,
			int subdivision)
		{
			if (subdivision > 6)
			{
				verticesOut = normalsOut = null;
				indicesOut = null;
				return false;
			}
			GenerateSmoothCube(out verticesOut, out normalsOut, out indicesOut);
			var table = new VertexEdgeFaceTable(verticesOut, normalsOut, null, indicesOut);
			for (int i = 0; i < subdivision; i++)
			{
				table.SubDivide();
			}
			// 全頂点を球面に移動
			var vertices = table.vertices;
			for (int i = 0; i < vertices.Count; i++)
			{
				var v = vertices[i];
				v.position.Normalize();
				v.position *= 0.5f; // 半径は0.5に
				v.normal = v.position;
			}
			table.GetArrays(out verticesOut, out normalsOut, out indicesOut);
			return true;
		}

		public static bool GenerateSphere(
			Mesh mesh,
			int subdivision)
		{
			Vector3[] vertices;
			Vector3[] normals;
			int[] indices;
			var ret = false;
			if (GenerateSphere(out vertices, out normals, out indices, subdivision))
			{
				FillMesh(mesh, vertices, normals, null, indices);
				ret = true;
			}
			return ret;
		}

		static void FillMesh(
			Mesh mesh,
			Vector3[] vertices,
			Vector3[] normals,
			Vector2[] uvs,
			int[] indices)
		{
			mesh.Clear();
			mesh.vertices = vertices;
			mesh.normals = normals;
			if (uvs != null)
			{
				mesh.uv = uvs;
			}
			mesh.SetIndices(indices, MeshTopology.Triangles, 0);
		}

		public static bool GenerateCylinderSide(
			Mesh mesh,
			float height,
			float radius,
			int subdivision)
		{
			Vector3[] vertices;
			Vector3[] normals;
			int[] indices;
			var ret = false;
			if (GenerateCylinderSide(out vertices, out normals, out indices, height, radius, subdivision))
			{
				FillMesh(mesh, vertices, normals, null, indices);
				ret = true;
			}
			return ret;
		}

		// 円柱。divは面分割数。最低2だが、2だとただの面になる。
		public static bool GenerateCylinderSide(
			out Vector3[] vertices,
			out Vector3[] normals,
			out int[] indices,
			float height,
			float radius,
			int subdivision)
		{
			int div = 4 << subdivision;
			if ((div < 2) || (div >= 0x8000))
			{
				vertices = normals = null;
				indices = null;
				return false;
			}
			vertices = new Vector3[div * 2];
			normals = new Vector3[div * 2];
			var indexCount = div * 6;
			indices = new int[indexCount];
			var hHeight = height * 0.5f;
			int vi = 0;
			for (int i = 0; i < div; i++)
			{
				var angle = 2f * Mathf.PI * (float)i / (float)div;
				var x = Mathf.Cos(angle) * radius;
				var z = Mathf.Sin(angle) * radius;
				vertices[vi].x = x;
				vertices[vi].z = z;
				vertices[vi].y = -hHeight;
				normals[vi].x = x;
				normals[vi].z = z;
				normals[vi].y = 0f;
				vi++;
				vertices[vi].x = x;
				vertices[vi].z = z;
				vertices[vi].y = hHeight;
				normals[vi].x = x;
				normals[vi].z = z;
				normals[vi].y = 0f;
				vi++;
			}
			var start = 0;
			vi = 0;
			for (int i = 0; i < (div - 1); i++) // 最後は別扱い
			{
				start = SetQuad(indices, start, vi, vi + 1, vi + 3, vi + 2);
				vi += 2;
			}
			start = SetQuad(indices, start, vi, vi + 1, 1, 0);
			return true;
		}


		// 角で法線共有しているために滑らか。基本再分割してより滑らかな図形を作るための種として使う。
		public static void GenerateSmoothCube(
			out Vector3[] vertices,
			out Vector3[] normals,
			out int[] indices)
		{
			vertices = new Vector3[8];
			indices = new int[36];
			vertices[0] = new Vector3(-0.5f, -0.5f, -0.5f);
			vertices[1] = new Vector3(-0.5f, -0.5f, 0.5f);
			vertices[2] = new Vector3(-0.5f, 0.5f, -0.5f);
			vertices[3] = new Vector3(-0.5f, 0.5f, 0.5f);
			vertices[4] = new Vector3(0.5f, -0.5f, -0.5f);
			vertices[5] = new Vector3(0.5f, -0.5f, 0.5f);
			vertices[6] = new Vector3(0.5f, 0.5f, -0.5f);
			vertices[7] = new Vector3(0.5f, 0.5f, 0.5f);
			normals = vertices;
			var start = 0;
			start = SetQuad(indices, start, 0, 1, 3, 2);
			start = SetQuad(indices, start, 4, 5, 7, 6);
			start = SetQuad(indices, start, 0, 4, 6, 2);
			start = SetQuad(indices, start, 1, 5, 7, 3);
			start = SetQuad(indices, start, 0, 1, 5, 4);
			start = SetQuad(indices, start, 2, 3, 7, 6);
		}

		// 角で法線共有しているために滑らか。基本再分割してより滑らかな図形を作るための種として使う。
		public static void GenerateSmoothTetrahedron(
			out Vector3[] vertices,
			out Vector3[] normals,
			out int[] indices)
		{
			vertices = new Vector3[4];
			indices = new int[12];
			vertices[0] = new Vector3(0f, 1f, 0f);
			vertices[1] = new Vector3(Mathf.Sqrt(8f / 9f), -1f / 3f, 0f);
			vertices[2] = new Vector3(-Mathf.Sqrt(2f / 9f), -1f / 3f, Mathf.Sqrt(2f / 3f));
			vertices[3] = new Vector3(-Mathf.Sqrt(2f / 9f), -1f / 3f, -Mathf.Sqrt(2f / 3f));
			normals = vertices;
			var start = 0;
			start = SetTriangle(indices, start, 0, 2, 1);
			start = SetTriangle(indices, start, 0, 3, 2);
			start = SetTriangle(indices, start, 0, 1, 3);
			start = SetTriangle(indices, start, 1, 2, 3);
		}

		static int SetTriangle(int[] indices, int start, int v0, int v1, int v2)
		{
			indices[start + 0] = v0;
			indices[start + 1] = v1;
			indices[start + 2] = v2;
			return start + 3;
		}

		static int SetQuad(int[] indices, int start, int v0, int v1, int v2, int v3)
		{
			indices[start + 0] = v0;
			indices[start + 1] = v1;
			indices[start + 2] = v2;
			indices[start + 3] = v2;
			indices[start + 4] = v3;
			indices[start + 5] = v0;
			return start + 6;
		}
	}
}