using UnityEngine;
using System.Collections.Generic;

namespace Kayac
{
    class MeshModel
    {
        public class Vertex
        {
            public Vertex(Vector3 position, Vector3 normal, Vector3 uv, int id)
            {
                this.position = position;
                this.normal = normal;
                this.uv = uv;
                this.id = id;
            }
            public Vector3 position;
            public Vector3 normal;
            public Vector2 uv;
            public HalfEdge edge;
            public int id;
        }

        public class HalfEdge
        {
            public Vertex vertex;
            public Face face;
            public HalfEdge pair;
            public HalfEdge next;
            public HalfEdge prev;
            public int id;
        }

        public class Face
        {
            public HalfEdge edge;
            public int id;
        }

        public MeshModel(
            IList<Vector3> verticesIn,
            IList<Vector3> normalsIn,
            IList<Vector2> uvsIn,
            IList<int> indices)
        {
            // まず頂点なめ
            var vn = verticesIn.Count;
            Debug.Assert(vn == normalsIn.Count);
            bool hasUv = (uvsIn != null) && (uvsIn.Count < 0);
            if (hasUv)
            {
                Debug.Assert(vn == uvsIn.Count);
            }
            vertices = new List<Vertex>(vn);
            for (int i = 0; i < vn; i++)
            {
                var uv = hasUv ? uvsIn[i] : Vector2.zero;
                var v = new Vertex(verticesIn[i], normalsIn[i], uv, i);
                vertices.Add(v);
            }
            // 頂点番号の組からハーフエッジを検索する辞書
            var vToE = new Dictionary<ulong, HalfEdge>();

            // 面をなめながら構造構築
            halfEdges = new List<HalfEdge>();
            faces = new List<Face>();
            Debug.Assert((indices.Count % 3) == 0);
            for (int i = 0; i < indices.Count; i += 3)
            {
                var v0 = vertices[indices[i + 0]]; // 始点
                var v1 = vertices[indices[i + 1]]; // 始点
                var v2 = vertices[indices[i + 2]]; // 始点
                                                   // インデクスの数だけhalfEdgeが存在するのでnew
                var e0 = new HalfEdge()
                {
                    vertex = v0, // 始点
                    id = halfEdges.Count + 0
                };
                v0.edge = e0; // 逆リンク(上書きしてしまってかまわない)
                var e1 = new HalfEdge
                {
                    vertex = v1, // 始点
                    id = halfEdges.Count + 1
                };
                v1.edge = e1; // 逆リンク(上書きしてしまってかまわない)
                var e2 = new HalfEdge
                {
                    vertex = v2, // 始点
                    id = halfEdges.Count + 2
                };
                v2.edge = e2; // 逆リンク(上書きしてしまってかまわない)
                e0.prev = e2;

                e0.next = e1;
                e1.prev = e0;
                e1.next = e2;
                e2.prev = e1;
                e2.next = e0;
                halfEdges.Add(e0);
                halfEdges.Add(e1);
                halfEdges.Add(e2);
                // 面を生成してリンク
                var f = new Face
                {
                    edge = e0, // 始点に追加
                    id = faces.Count
                };
                e0.face = f;
                e1.face = f;
                e2.face = f;
                faces.Add(f);
                // 辞書に登録
                var k01 = MakeKey(v0, v1);
                var k12 = MakeKey(v1, v2);
                var k20 = MakeKey(v2, v0);
                TryAddEdgePair(vToE, k01, e0);
                TryAddEdgePair(vToE, k12, e1);
                TryAddEdgePair(vToE, k20, e2);
            }
            Debug.Assert(vToE.Count == 0);
            Validate();
        }

        public string ToString(bool outputFaces = true)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendFormat("[Vertices:{0}]\n", vertices.Count);
            foreach (var v in vertices)
            {
                sb.AppendFormat("{0}: {1} {2} {3}\n", v.id, v.position.ToString("F2"), v.normal.ToString("F2"), v.uv.ToString("F2"));
            }
            sb.AppendFormat("[HalfEdges:{0}]\n", halfEdges.Count);
            foreach (var e in halfEdges)
            {
                sb.AppendFormat("{0}: prev:{1} next:{2} pair:{3} v:{4} f:{5}\n",
                    e.id,
                    (e.prev != null) ? e.prev.id : -1,
                    (e.next != null) ? e.next.id : -1,
                    (e.pair != null) ? e.pair.id : -1,
                    (e.vertex != null) ? e.vertex.id : -1,
                    (e.face != null) ? e.face.id : -1);
            }
            if (outputFaces)
            {
                var visited = new HashSet<HalfEdge>();
                sb.AppendFormat("[Faces:{0}]\n", faces.Count);
                foreach (var f in faces)
                {
                    var startE = f.edge;
                    sb.Append(f.id + " ");
                    var e = startE;
                    visited.Clear();
                    while (true)
                    {
                        visited.Add(e);
                        sb.AppendFormat("E:{0}(V:{1}) ", e.id, e.vertex.id);
                        if (visited.Contains(e))
                        {
                            sb.Append(" cyclic link. INVALID EDGE CONNECTION.");
                            break;
                        }
                        e = e.next;
                        if (e == null)
                        {
                            sb.Append(" NULL link. INVALID EDGE CONNECTION.");
                            break;
                        }
                        else if (e == startE)
                        {
                            break;
                        }
                    }
                    sb.Append("\n");
                }
            }
            return sb.ToString();
        }

        // 戻り値は頂点ごとに「何番目の破片の頂点か」を返す。追加するので、空にする責任は呼び出し側
        public void Cut(List<int> partsIdOut, Plane plane)
        {
            // faceごとに頂点と平面の符号つき距離を計算、正と負に割れた場合に頂点を生成する。
            // 交点が二つできた場合に限って処理を行う。
            for (int i = 0; i < faces.Count; i++)
            {
                // 辺でループしながら頂点の距離を測定、符号変更時に切断エッジを記録
                // 2つ目が発見されたらそこで切断。多角形は凸である前提。
                // 2辺を分割し、裏の辺も分割。分割線を複製して分離し、
                // 二つの分割線の1辺づつを保存しておく。

            }
        }

        public void SubDivide()
        {
            // 全edgeの中点を生成して頂点を生成
            var edgeCount = halfEdges.Count;
            var processedEdges = new HashSet<HalfEdge>();
            for (int i = 0; i < edgeCount; i++)
            {
                var e0 = halfEdges[i];
                if (processedEdges.Contains(e0))
                {
                    continue;
                }
                processedEdges.Add(e0.pair);
                var e2 = e0.next;
                Debug.Assert(e2 != null);
                var pair0 = e0.pair;
                Debug.Assert(pair0 != null);
                var pair2 = pair0.next;
                // 頂点を生成
                var v0 = e0.vertex;
                var v2 = e2.vertex;
                Debug.Assert(pair0.vertex == v2);
                Debug.Assert(pair2.vertex == v0);
                var p = (v0.position + v2.position) * 0.5f;
                var n = (v0.normal + v2.normal).normalized;
                var t = (v0.uv + v2.uv) * 0.5f;
                var v1 = new Vertex(p, n, t, vertices.Count);
                // e0-e2の間にe1を挿入、pair0-pair2の間にpair1を挿入
                var e1 = new HalfEdge()
                {
                    vertex = v1,
                    face = e0.face,
                    prev = e0,
                    next = e2,
                    id = halfEdges.Count
                };
                var pair1 = new HalfEdge()
                {
                    vertex = v1,
                    face = pair0.face,
                    prev = pair0,
                    next = pair2,
                    id = halfEdges.Count + 1
                };
                e1.pair = pair0;
                pair0.pair = e1;
                pair1.pair = e0;
                e0.pair = pair1;

                e0.next = e1;
                e2.prev = e1;
                pair0.next = pair1;
                pair2.prev = pair1;

                vertices.Add(v1);
                halfEdges.Add(e1);
                halfEdges.Add(pair1);
            }
            // 面の分割を行う。
            var faceCount = faces.Count; // ループ内で増えていくので古い値を使う
            for (int i = 0; i < faceCount; i++)
            {
                var face = faces[i];
                var e0 = face.edge;
                var e1 = e0.next;
                var e2 = e1.next;
                var e3 = e2.next;
                var e4 = e3.next;
                var e5 = e4.next;
                Debug.Assert(e5.next == e0);

                var e15 = DevideTriangle(e5);
                var e31 = DevideTriangle(e1);
                var e53 = DevideTriangle(e3);

                // 中央
                var pair31 = new HalfEdge()
                {
                    vertex = e1.vertex,
                    face = face,
                    pair = e31,
                    id = halfEdges.Count
                };
                halfEdges.Add(pair31);
                var pair53 = new HalfEdge()
                {
                    vertex = e3.vertex,
                    face = face,
                    pair = e53,
                    id = halfEdges.Count
                };
                halfEdges.Add(pair53);
                var pair15 = new HalfEdge()
                {
                    vertex = e5.vertex,
                    face = face,
                    pair = e15,
                    id = halfEdges.Count
                };
                halfEdges.Add(pair15);
                face.edge = pair31;
                pair31.next = pair53;
                pair53.prev = pair31;
                pair53.next = pair15;
                pair15.prev = pair53;
                pair15.next = pair31;
                pair31.prev = pair15;
                e31.pair = pair31;
                e53.pair = pair53;
                e15.pair = pair15;
            }
            Validate();
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
            var visited = new HashSet<HalfEdge>();
            for (int i = 0; i < faces.Count; i++) // 各面についてインデクスを生成
            {
                MakeIndices(ret, i * 3, faces[i], visited);
            }
            return ret;
        }
        public IList<Vertex> Vertices { get { return vertices; } }

        // non-public ---------------------
        void TryAddEdgePair(Dictionary<ulong, HalfEdge> map, ulong key, HalfEdge e)
        {
            HalfEdge another;
            if (map.TryGetValue(key, out another))
            {
                another.pair = e;
                e.pair = another;
                map.Remove(key);
            }
            else
            {
                map.Add(key, e);
            }
        }


        // 辺が分割された状態で始点を呼ぶ。始点は分割中点
        HalfEdge DevideTriangle(HalfEdge e0)
        {
            var e1 = e0.next;
            var f = new Face() // 新しい面
            {
                edge = e0,
                id = faces.Count
            };
            var e = new HalfEdge()
            {
                vertex = e1.next.vertex,
                prev = e1,
                next = e0,
                face = f,
                id = halfEdges.Count
            };
            e1.next = e0.prev = e;
            e0.face = e1.face = e.face = f;
            halfEdges.Add(e);
            faces.Add(f);
            return e;
        }

        void MakeIndices(int[] indices, int indexOffset, Face face, HashSet<HalfEdge> visited)
        {
            visited.Clear();
            var eStart = face.edge; // このハーフエッジから頂点を列挙する
            visited.Add(eStart);
            Vertex v0 = null;
            Vertex v1 = null;
            var e = eStart;
            while (true)
            {
                if (v0 == null)
                {
                    v0 = e.vertex;
                }
                else if (v1 == null)
                {
                    v1 = e.vertex;
                }
                else
                {
                    var v2 = e.vertex;
                    // 法線とのチェック
                    var cross = Vector3.Cross(v1.position - v0.position, v2.position - v0.position);
                    var dp = Vector3.Dot(cross, v0.normal);
                    if (dp < 0f) // 法線が合わないので反転
                    {
                        var tmp = v1;
                        v1 = v2;
                        v2 = tmp;
                    }
                    indices[indexOffset + 0] = v0.id;
                    indices[indexOffset + 1] = v1.id;
                    indices[indexOffset + 2] = v2.id;
                    v0 = v1;
                    v1 = v2;
                    e = e.next;
                    if (e == null)
                    {
                        break;
                    }
                    else if (visited.Contains(e))
                    {
                        break;
                    }
                    else if (e == eStart)
                    {
                        break;
                    }
                    visited.Add(e);
                }
            }

        }

        void Validate()
        {
            for (int i = 0; i < vertices.Count; i++)
            {
                Debug.Assert(vertices[i].id == i);
            }
            var visited = new HashSet<HalfEdge>();
            for (int i = 0; i < faces.Count; i++)
            {
                ValidateFace(faces[i], visited);
            }
            for (int i = 0; i < halfEdges.Count; i++)
            {
                ValidateEdgePair(halfEdges[i]);
            }
        }

        void ValidateFace(Face face, HashSet<HalfEdge> visited)
        {
            visited.Clear();
            var startE = face.edge;
            visited.Add(startE);
            var e = startE;
            Debug.Assert(e != null);
            while (true)
            { 
                var next = e.next;
                var prev = e.prev;
                Debug.Assert(next != null);
                Debug.Assert(prev != null);
                Debug.Assert(next.prev == e);
                Debug.Assert(prev.next == e);
                if (face != null)
                {
                    Debug.Assert(e.face == face);
                }
                e = next;
                if (e == null)
                {
                    Debug.Assert(false);
                    break;
                }
                else if (e == startE)
                {
                    break;
                }
                else if (visited.Contains(e))
                {
                    Debug.Assert(false);
                    break;
                }
                visited.Add(e);
            }
        }

        void ValidateEdgePair(HalfEdge e)
        {
            if (e.pair != null)
            {
                Debug.Assert(e.pair.pair == e);
                if (e.vertex != e.pair.next.vertex)
                {
                    Debug.LogError(e.id + "-" + e.pair.next.vertex.id);
                }
                if (e.next.vertex != e.pair.vertex)
                {
                    Debug.LogError(string.Format("Edge{0} : Vertex {1}({2}) - {3}({4})", e.id, e.next.id, e.next.vertex.id, e.pair.id, e.pair.vertex.id));
                }
            }
        }

        ulong MakeKey(Vertex v0, Vertex v1)
        {
            ulong ret;
            uint id0 = (uint)v0.id;
            uint id1 = (uint)v1.id;
            if (id0 < id1)
            {
                ret = ((ulong)id0 << 32) | (ulong)id1;
            }
            else
            {
                Debug.Assert(id0 != id1);
                ret = ((ulong)id1 << 32) | (ulong)id0;
            }
            return ret;
        }
        List<Vertex> vertices;
        List<Face> faces;
        List<HalfEdge> halfEdges;
    }
}
