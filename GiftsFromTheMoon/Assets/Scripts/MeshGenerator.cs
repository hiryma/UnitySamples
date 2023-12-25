using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MeshGenerator
{
	public static Mesh GenerateSphere(int horizontalDiv, int verticalDiv, float radius, Vector2 uvOffset, Vector3 uvScale)
	{
		var mesh = new Mesh();
		mesh.name = "Sphere";

		var vertices = new List<Vector3>();
		var normals = new List<Vector3>();
		var uvs = new List<Vector2>();
		var indices = new List<int>();

		// 頂点座標詰める Z-,X+,Z+,X-が左→右。Vは下から上にプラス
		for (var hi = 0; hi <= horizontalDiv; hi++)
		{
			var hRatio = (float)hi / (float)horizontalDiv;
			var hRad = Mathf.PI * 2f * hRatio; // z-から
			var zBase = -Mathf.Cos(hRad);
			var xBase = Mathf.Sin(hRad);
			var u = (hRatio * uvScale.x) + uvOffset.x;
			for (var vi = 0; vi <= verticalDiv; vi++)
			{
				var vRatio = (float)vi / (float)verticalDiv;
				var vRad = Mathf.PI * vRatio;
				var y = radius * -Mathf.Cos(vRad);
				var xzLength = Mathf.Sin(vRad);
				var x = radius * xBase * xzLength;
				var z = radius * zBase * xzLength;
				vertices.Add(new Vector3(x, y, z));
				normals.Add(new Vector3(x, y, z).normalized);

				var v = (vRatio * uvScale.y) + uvOffset.y;
				uvs.Add(new Vector2(u, v));
			}
		}

		// インデクス詰める
		for (var hi = 0; hi < horizontalDiv; hi++)
		{
			for (var vi = 0; vi < verticalDiv; vi++)
			{
				var i0 = hi * (verticalDiv + 1) + vi;
				var i1 = i0 + 1;
				var i2 = i0 + (verticalDiv + 1);
				var i3 = i2 + 1;
				indices.Add(i0);
				indices.Add(i1);
				indices.Add(i2);
				indices.Add(i1);
				indices.Add(i3);
				indices.Add(i2);
			}
		}

		mesh.SetVertices(vertices);
		mesh.SetNormals(normals);
		mesh.SetUVs(0, uvs);
		mesh.SetIndices(indices.ToArray(), MeshTopology.Triangles, 0);

		return mesh;
	}
}
