using UnityEngine;

public class FillRenderer : MonoBehaviour
{
    [SerializeField] MeshFilter meshFilter;
    [SerializeField] MeshRenderer meshRenderer;

    public void SetCount(float count)
    {
        newCount = count;
    }
    public void SetMaterial(Material material)
    {
        meshRenderer.material = material;
    }
    float currentCount;
    float newCount;
    Mesh mesh;
    Vector3 quadVertices;
    int[] indices;
    Vector3[] vertices;
    Vector2[] uv;

    public void ManualStart()
    {
        mesh = new Mesh();
        vertices = new Vector3[6]{
            new Vector3(-1f, -1f, 0f),
            new Vector3(-1f, 1f, 0f),
            new Vector3(1f, -1f, 0f),
            new Vector3(1f, 1f, 0f),
			// 以下2つは動的に書き換える
			new Vector3(1f, -1f, 0f),
            new Vector3(1f, 1f, 0f),
        };
        uv = new Vector2[6]{
            new Vector2(0f, 0f),
            new Vector2(0f, 1f),
            new Vector2(1f, 0f),
            new Vector2(1f, 1f),
			// 以下2つは動的に書き換える
			new Vector2(1f, 0f),
            new Vector2(1f, 1f),
        };
        meshFilter.mesh = mesh;
    }

    public void ManualUpdate()
    {
        int newCountCeil = Mathf.CeilToInt(newCount);
        // 基本小数部だが、ピッタリ整数だった場合は1にする
        float u1 = newCount - (float)(newCountCeil - 1);
        var x1 = (u1 * 2f) - 1f;
        // 頂点を修正
        vertices[4] = new Vector3(x1, -1f, 0f);
        vertices[5] = new Vector3(x1, 1f, 0f);
        mesh.vertices = vertices;
        uv[4] = new Vector2(u1, 0f);
        uv[5] = new Vector2(u1, 1f);
        mesh.uv = uv;

        int currentCountCeil = Mathf.CeilToInt(currentCount);
        if (newCountCeil != currentCountCeil) // 数が変わった時には配列を再確保
        {
            indices = new int[newCountCeil * 6];
            var pos = 0;
            for (int i = 0; i < (newCountCeil - 1); i++)
            {
                indices[pos + 0] = 0;
                indices[pos + 1] = 1;
                indices[pos + 2] = 2;
                indices[pos + 3] = 2;
                indices[pos + 4] = 1;
                indices[pos + 5] = 3;
                pos += 6;
            }
            if (newCountCeil > 0)
            {
                // 最後の矩形は2つ目の矩形頂点を参照
                indices[pos + 0] = 0;
                indices[pos + 1] = 1;
                indices[pos + 2] = 4;
                indices[pos + 3] = 4;
                indices[pos + 4] = 1;
                indices[pos + 5] = 5;
            }
        }
        mesh.triangles = indices;
        currentCount = newCount;
    }
}
