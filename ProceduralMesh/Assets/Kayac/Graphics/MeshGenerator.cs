using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MeshGenerator
{
	// 任意詳細度で半径1の球を作る。頂点数は(div+1)*(div+1)*6。三角形数はdiv*div*2*6
	// TODO: 重複除けばもっと頂点減らせて、歪めたりいろいろなことがしやすくなる
	public static void GenerateSphere(
		out Vector3[] vertices,
		out Vector3[] normals,
		out int[] indices,
		int div)
	{
		var vcFace = (div + 1) * (div + 1);
		var vc = vcFace * 6;
		var icFace = div * div * 2 * 3;
		var ic = icFace * 6;
		vertices = new Vector3[vc];
		normals = new Vector3[vc];
		indices = new int[ic];
		// まずX-の1面を生成
		int vi = 0;
		for (int yi = 0; yi <= div; yi++)
		{
			float y = ((float)yi / (float)div) - 0.5f;
			for (int zi = 0; zi <= div; zi++)
			{
				float z = ((float)zi / (float)div) - 0.5f;
				vertices[vi].Set(-0.5f, y, z);
				vertices[vi].Normalize();
				normals[vi] = vertices[vi];
				vi++;
			}
		}
		Debug.Assert(vi == vcFace);
		// 残りの5面に回転をかけながらコピー
		for (int i = 0; i < vcFace; i++)
		{
			var v = vertices[i];
			var v1 = new Vector3(-v.x, v.y, -v.z); //X+
			var v2 = new Vector3(-v.y, v.x, v.z); //Y-
			var v3 = new Vector3(v.y, -v.x, v.z); //Y+
			var v4 = new Vector3(-v.z, v.y, v.x); //Z-
			var v5 = new Vector3(v.z, v.y, -v.x); //Z+
			var i1 = vcFace + i;
			var i2 = i1 + vcFace;
			var i3 = i2 + vcFace;
			var i4 = i3 + vcFace;
			var i5 = i4 + vcFace;
			vertices[i1] = normals[i1] = v1;
			vertices[i2] = normals[i2] = v2;
			vertices[i3] = normals[i3] = v3;
			vertices[i4] = normals[i4] = v4;
			vertices[i5] = normals[i5] = v5;
		}
		// X-のインデクス
		for (int yi = 0; yi < div; yi++)
		{
			int linePos = yi * div * 2 * 3;
			for (int zi = 0; zi < div; zi++)
			{
				int i0 = (yi * (div + 1)) + zi;
				int i1 = (yi * (div + 1)) + (zi + 1);
				int i2 = ((yi + 1) * (div + 1)) + zi;
				int i3 = i2;
				int i4 = i1;
				int i5 = ((yi + 1) * (div + 1)) + (zi + 1);
				indices[(icFace * 0) + linePos + 0] = i0;
				indices[(icFace * 0) + linePos + 1] = i1;
				indices[(icFace * 0) + linePos + 2] = i2;
				indices[(icFace * 0) + linePos + 3] = i3;
				indices[(icFace * 0) + linePos + 4] = i4;
				indices[(icFace * 0) + linePos + 5] = i5;
				linePos += 2 * 3;
			}
		}
		// 残りの5面はオフセット付きコピー
		for (int i = 0; i < icFace; i++)
		{
			int idx = indices[i];
			indices[(icFace * 1) + i] = idx + (vcFace * 1);
			indices[(icFace * 2) + i] = idx + (vcFace * 2);
			indices[(icFace * 3) + i] = idx + (vcFace * 3);
			indices[(icFace * 4) + i] = idx + (vcFace * 4);
			indices[(icFace * 5) + i] = idx + (vcFace * 5);
		}
	}

	public static void GenerateTetrahedron(
		out Vector3[] vertices,
		out Vector3[] normals,
		out int[] indices)
	{
		vertices = new Vector3[4];
		normals = new Vector3[4];
		indices = new int[12];
		vertices[0] = normals[0] = new Vector3(0f, 1f, 0f);
		vertices[1] = normals[1] = new Vector3(Mathf.Sqrt(8f / 9f), -1f / 3f, 0f);
		vertices[2] = normals[2] = new Vector3(-Mathf.Sqrt(2f / 9f), -1f / 3f, Mathf.Sqrt(2f / 3f));
		vertices[3] = normals[3] = new Vector3(-Mathf.Sqrt(2f / 9f), -1f / 3f, -Mathf.Sqrt(2f / 3f));
		indices[0] = 0;
		indices[1] = 2;
		indices[2] = 1;
		indices[3] = 0;
		indices[4] = 3;
		indices[5] = 2;
		indices[6] = 0;
		indices[7] = 1;
		indices[8] = 3;
		indices[9] = 1;
		indices[10] = 2;
		indices[11] = 3;
	}
}
