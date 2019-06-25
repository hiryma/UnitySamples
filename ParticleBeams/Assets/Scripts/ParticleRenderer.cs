using UnityEngine;
using Kayac;

public class ParticleRenderer : MonoBehaviour
{
	[SerializeField] MeshRenderer meshRenderer;
	[SerializeField] MeshFilter meshFilter;
	[SerializeField] Shader particleShader;
	[SerializeField] Shader textShader;
	[SerializeField] Shader texturedShader;
	[SerializeField] Font font;

	public DebugPrimitiveRenderer3D Renderer{ get; private set; }
	public ParticleMesh Mesh{ get; private set; }

	public void Initialize(Camera camera, int triangleCapacity)
	{
/*
		Renderer = new DebugPrimitiveRenderer3D(
			textShader,
			texturedShader,
			font,
			camera,
			meshRenderer,
			meshFilter,
			triangleCapacity);
*/
		Mesh = new ParticleMesh(
			particleShader,
			meshRenderer,
			meshFilter,
			triangleCapacity);
	}
}
