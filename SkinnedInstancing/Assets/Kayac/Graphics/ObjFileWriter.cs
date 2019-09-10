using System.Collections.Generic;
using UnityEngine;
using System.Text;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Kayac
{
    public static class ObjFileWriter
    {
        public static string ToText(Mesh mesh, int subMeshIndex)
        {
            var sb = new StringBuilder();
            var uv = mesh.uv;
            bool hasUv = (uv != null) && (uv.Length == mesh.vertexCount);
            ToTextVertices(sb, mesh.vertices, uv, mesh.normals);
            ToTextFaces(sb, mesh.GetIndices(subMeshIndex), hasUv, subMeshIndex.ToString(), vertexIndexOffset: 1); // objは1から
            return sb.ToString();
        }

        // サブメッシュ結合書き出し
        public static string ToText(Mesh mesh)
        {
            var sb = new StringBuilder();
            var uv = mesh.uv;
            bool hasUv = (uv != null) && (uv.Length == mesh.vertexCount);
            ToTextVertices(sb, mesh.vertices, uv, mesh.normals);
            for (int i = 0; i < mesh.subMeshCount; i++)
            {
                ToTextFaces(sb, mesh.GetIndices(i), hasUv, i.ToString(), vertexIndexOffset: 1); // objは1から
            }
            return sb.ToString();
        }

        public static string ToText(
            IList<Vector3> positions,
            IList<Vector2> uvs,
            IList<Vector3> normals,
            IList<int> indices)
        {
            var sb = new StringBuilder();
            bool hasUv = (uvs != null) && (uvs.Count == positions.Count);
            ToTextVertices(sb, positions, uvs, normals);
            ToTextFaces(sb, indices, hasUv, "unnamed", vertexIndexOffset: 1); // objは1から
            return sb.ToString();
        }

#if UNITY_EDITOR
        [MenuItem("GameObject/Save .obj recursive", false, 20)]
        public static void SaveObjRecursive()
        {
            var selected = Selection.activeObject;
            var gameObject = selected as GameObject;
            Write("Assets", gameObject, importImmediately: true);
        }

        [MenuItem("GameObject/Save .obj recursive", true)]
        public static bool ValidateSaveObjRecursive()
        {
            var ret = false;
            var active = Selection.activeObject;
            if (active != null)
            {
                ret = (active is GameObject);
            }
            return ret;
        }

        [MenuItem("Assets/Save .obj")]
        public static void Save()
        {
            var selected = Selection.activeObject;
            var mesh = selected as Mesh;
            if (mesh == null)
            {
                Debug.LogError("selected object is not mesh. type=" + selected.GetType().Name);
                return;
            }
            var originalPath = AssetDatabase.GetAssetPath(mesh);
            var dir = System.IO.Path.GetDirectoryName(originalPath);
            Write(dir, mesh, importImmediately: true);
        }

        [MenuItem("Assets/Save .obj", true)]
        public static bool ValidateSave()
        {
            var ret = false;
            var active = Selection.activeObject;
            if (active != null)
            {
                ret = (active is Mesh);
            }
            return ret;
        }

        [MenuItem("CONTEXT/MeshFilter/Save .obj")]
        public static void SaveObjFromMeshFilter(MenuCommand menuCommand)
        {
            var meshFilter = menuCommand.context as MeshFilter;
            if (meshFilter != null)
            {
                var mesh = meshFilter.sharedMesh;
                if (mesh != null)
                {
                    Write("Assets", mesh, importImmediately: true);
                }
            }
        }

        [MenuItem("CONTEXT/SkinnedMeshRenderer/Save .obj")]
        public static void SaveObjFromSkinnedMeshRenderer(MenuCommand menuCommand)
        {
            var renderer = menuCommand.context as SkinnedMeshRenderer;
            if (renderer != null)
            {
                var mesh = renderer.sharedMesh;
                if (mesh != null)
                {
                    Write("Assets", mesh, importImmediately: true);
                }
            }
        }

        public static bool Write(
            string directory,
            Mesh mesh,
            bool importImmediately = false)
        {
            Debug.Assert(mesh != null);
            var name = mesh.name;
            if (string.IsNullOrEmpty(name))
            {
                name = "unnamed.obj";
            }
            else
            {
                name += ".obj";
            }
            var path = System.IO.Path.Combine(directory, name);
            var text = ToText(mesh);
            return Write(path, text, importImmediately);
        }

        public static bool Write(
            string directory,
            Mesh mesh,
            int subMeshIndex,
            bool importImmediately = false)
        {
            Debug.Assert(mesh != null);
            var name = mesh.name;
            if (string.IsNullOrEmpty(name))
            {
                name = "unnamed";
            }
            name += "_" + subMeshIndex + ".obj";
            var path = System.IO.Path.Combine(directory, name);
            var text = ToText(mesh);
            return Write(path, text, importImmediately);
        }

        public static bool Write(
            string directory,
            GameObject gameObject,
            bool importImmediately = false)
        {
            Debug.Assert(gameObject != null);
            var items = new List<MergedItem>();
            if (gameObject != null)
            {
                CollectMeshesRecursive(items, gameObject, Matrix4x4.identity);
            }
            var name = gameObject.name;
            if (string.IsNullOrEmpty(name))
            {
                name = "unnamed";
            }
            name += ".obj";
            var path = System.IO.Path.Combine(directory, name);
            var text = ToText(items);
            return Write(path, text, importImmediately);
        }

        public static bool Write(
            string path,
            IList<Vector3> positions,
            IList<Vector2> uvs,
            IList<Vector3> normals,
            IList<int> indices,
            bool importImmediately = false)
        {
            var text = ToText(positions, uvs, normals, indices);
            return Write(path, text, importImmediately);
        }

        // おまけ。 TODO: Objと何の関係もないので、別ファイルが望ましい。
        [MenuItem("CONTEXT/MeshFilter/Save .asset")]
        public static void SaveAssetFromInspector(MenuCommand menuCommand)
        {
            var meshFilter = menuCommand.context as MeshFilter;
            if (meshFilter != null)
            {
                var mesh = meshFilter.sharedMesh;
                if (mesh != null)
                {
                    var path = string.Format("Assets/{0}.asset", mesh.name);
                    AssetDatabase.CreateAsset(mesh, path);
                    AssetDatabase.SaveAssets(); // これがないと中身が空になる仕様らしい
                }
            }
        }
#endif

        // non-public ------------------
        struct MergedItem
        {
            public Mesh mesh;
            public Matrix4x4 matrix;
        }

        static void ToTextVertices(
            StringBuilder sb,
            IList<Vector3> positions,
            IList<Vector2> uvs,
            IList<Vector3> normals)
        {
            Debug.Assert(positions != null);
            sb.AppendFormat("Generated by Kayac.ObjFileWriter. {0} vertices\n", positions.Count);
            sb.AppendLine("# positions");
            foreach (var item in positions)
            {
                sb.AppendFormat("v {0} {1} {2}\n",
                    item.x.ToString("F8"), //精度指定しないとfloat精度の全体を吐かないので劣化してしまう。10進8桁必要
                    item.y.ToString("F8"),
                    item.z.ToString("F8"));
            }

            bool hasUv = (uvs != null) && (uvs.Count > 0);
            if (hasUv)
            {
                Debug.Assert(uvs.Count == positions.Count);
                sb.AppendLine("\n# texcoords");
                foreach (var item in uvs)
                {
                    sb.AppendFormat("vt {0} {1}\n",
                        item.x.ToString("F8"),
                        item.y.ToString("F8"));
                }
            }

            Debug.Assert(normals != null);
            sb.AppendLine("\n# normals");
            foreach (var item in normals)
            {
                sb.AppendFormat("vn {0} {1} {2}\n",
                    item.x.ToString("F8"),
                    item.y.ToString("F8"),
                    item.z.ToString("F8"));
            }
        }

        static void ToTextFaces(
            StringBuilder sb,
            IList<int> indices,
            bool hasUv,
            string materialName,
            int vertexIndexOffset)
        {
            Debug.Assert(indices != null);
            Debug.Assert((indices.Count % 3) == 0);
            sb.AppendLine("\nusemtl " + materialName);
            sb.AppendLine("# triangle faces");
            for (var i = 0; i < indices.Count; i += 3)
            {
                var i0 = indices[i + 0] + vertexIndexOffset; // 1 based index.
                var i1 = indices[i + 1] + vertexIndexOffset;
                var i2 = indices[i + 2] + vertexIndexOffset;
                if (hasUv)
                {
                    sb.AppendFormat("f {0}/{0}/{0} {1}/{1}/{1} {2}/{2}/{2}\n",
                        i0,
                        i1,
                        i2);
                }
                else
                {
                    sb.AppendFormat("f {0}//{0} {1}//{1} {2}//{2}\n",
                        i0,
                        i1,
                        i2);
                }
            }
        }

        static void CollectMeshesRecursive(List<MergedItem> items, GameObject gameObject, Matrix4x4 matrix)
        {
            var transform = gameObject.transform;
            var localMatrix = Matrix4x4.TRS(
                transform.localPosition,
                transform.localRotation,
                transform.localScale);
            matrix *= localMatrix;
            var meshFilter = gameObject.GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                MergedItem item;
                item.mesh = meshFilter.sharedMesh;
                item.matrix = matrix;
                items.Add(item);
            }
            for (int i = 0; i < transform.childCount; i++)
            {
                CollectMeshesRecursive(items, transform.GetChild(i).gameObject, matrix);
            }
        }

        // メッシュマージしつつ書き出し
        static string ToText(IList<MergedItem> items)
        {
            var sb = new StringBuilder();
            // まず全メッシュ全頂点結合。オフセットテーブル生成
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var normals = new List<Vector3>();
            var vertexIndexOffsets = new int[items.Count + 1]; // 特別処理を避けるために+1
            vertexIndexOffsets[0] = 1; // objは1から
            var hasUv = true;
            for (int i = 0; i < items.Count; i++)
            {
                TransformVertices(vertices, normals, items[i]);
                var mesh = items[i].mesh;
                var meshUv = mesh.uv;
                if ((meshUv == null) || (meshUv.Length < mesh.vertexCount))
                {
                    hasUv = false;
                    uvs = null;
                }
                if (hasUv)
                {
                    uvs.AddRange(meshUv);
                }
                vertexIndexOffsets[i + 1] = vertexIndexOffsets[i] + mesh.vertexCount;
            }

            ToTextVertices(sb, vertices, uvs, normals);
            for (int meshIndex = 0; meshIndex < items.Count; meshIndex++)
            {
                var item = items[meshIndex];
                var mesh = item.mesh;
                var meshName = mesh.name;
                if (string.IsNullOrEmpty(meshName))
                {
                    meshName = meshIndex.ToString() + "_";
                }
                for (int subMeshIndex = 0; subMeshIndex < mesh.subMeshCount; subMeshIndex++)
                {
                    ToTextFaces(
                        sb,
                        mesh.GetIndices(subMeshIndex),
                        hasUv,
                        meshName + subMeshIndex.ToString(),
                        vertexIndexOffsets[meshIndex]);
                }
            }
            return sb.ToString();
        }

        static void TransformVertices(List<Vector3> verticesOut, List<Vector3> normalsOut, MergedItem item)
        {
            var mesh = item.mesh;
            var vertexCount = mesh.vertexCount;
            var vertices = mesh.vertices;
            var normals = mesh.normals;
            var matrix = item.matrix;
            for (int i = 0; i < vertexCount; i++)
            {
                var v = matrix.MultiplyPoint3x4(vertices[i]);
                var n = matrix.MultiplyVector(normals[i]);
                verticesOut.Add(v);
                normalsOut.Add(n);
            }
        }

#if UNITY_EDITOR
        static bool Write(string path, string objFileText, bool importImmediately)
        {
            bool ret = false;
            try
            {
                System.IO.File.WriteAllText(path, objFileText);
                if (importImmediately)
                {
                    UnityEditor.AssetDatabase.ImportAsset(path, UnityEditor.ImportAssetOptions.Default);
                }
                ret = true;
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
            }
            return ret;
        }
#endif
    }
}