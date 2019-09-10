using UnityEngine;

namespace Kayac
{

    public class SkinnedInstancingRenderer : MonoBehaviour
    {
        [SerializeField] SkinnedMeshRenderer myRenderer;
        [SerializeField] Mesh originalMesh;
        [SerializeField] int count;

        Matrix4x4[] poses;

        public Matrix4x4[] BeginUpdatePoses()
        {
            return poses;
        }

        public void EndUpdatePoses()
        {
            myRenderer.sharedMesh.bindposes = poses;
        }
        

        public void ManualStart(Mesh overrideMesh = null, int overrideCount = 0)
        {
            if (overrideMesh != null)
            {
                originalMesh = overrideMesh;
            }
            if (overrideCount > 0)
            {
                count = overrideCount;
            }
            var transforms = new Transform[count];
            for (int i = 0; i < count; i++)
            {
                transforms[i] = gameObject.transform;
            }
            myRenderer.bones = transforms; // 全部同じの
            var mesh = new Mesh()
            {
                name = "generated"
            };
            var origVn = originalMesh.vertexCount;
            var indicesSrc = originalMesh.triangles;
            var origIn = indicesSrc.Length;
            var verticesSrc = originalMesh.vertices;
            var normalsSrc = originalMesh.normals;
            var uvsSrc = originalMesh.uv;
            var vertices = new Vector3[count * origVn];
            var normals = new Vector3[count * origVn];
            var uvs = new Vector2[count * origVn];
            var indices = new int[count * origIn];
            var weights = new BoneWeight[count * origVn];
            poses = new Matrix4x4[count];
            for (int i = 0; i < count; i++)
            {
                poses[i] = Matrix4x4.identity;
                System.Array.Copy(verticesSrc, 0, vertices, i * origVn, origVn);
                System.Array.Copy(normalsSrc, 0, normals, i * origVn, origVn);
                System.Array.Copy(uvsSrc, 0, uvs, i * origVn, origVn);
                for (int j = 0; j < origIn; j++)
                {
                    indices[(i * origIn) + j] = indicesSrc[j] + (origVn * i);
                }
                for (int j = 0; j < origVn; j++)
                {
                    weights[(i * origVn) + j].boneIndex0 = i;
                    weights[(i * origVn) + j].weight0 = 1f;
                }
            }
            mesh.bindposes = poses;
            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.triangles = indices;
            mesh.boneWeights = weights;
            myRenderer.sharedMesh = mesh;
        }
    }

}