using UnityEngine;
using Kayac;

public class ParticleRenderer : MonoBehaviour
{
	[SerializeField] MeshRenderer meshRenderer;
	[SerializeField] MeshFilter meshFilter;
	[SerializeField] Shader particleShader;

	public DebugPrimitiveRenderer3D Renderer{ get; private set; }
	public ParticleMesh Mesh{ get; private set; }

	public void Initialize(Camera camera, int triangleCapacity)
	{
		Mesh = new ParticleMesh(
			particleShader,
			meshRenderer,
			meshFilter,
			triangleCapacity);
	}
}
