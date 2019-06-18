using UnityEngine;
using UnityEngine.Rendering;

namespace Kayac
{
	public class BasicRenderPipeline : RenderPipeline
	{
		CullingResults cullingResults;
		CommandBuffer commandBuffer;
		int mainLightColorId;
		int mainLightDirectionId;

		void Initialize()
		{
			commandBuffer = new CommandBuffer();
			commandBuffer.name = "Kayac.BasicRenderPipeline";
			mainLightColorId = Shader.PropertyToID("_MainLightColor");
			mainLightDirectionId = Shader.PropertyToID("_MainLightDirection");
		}

		protected override void Render(
			ScriptableRenderContext context,
			Camera[] cameras)
		{
			if (commandBuffer == null)
			{
				Initialize();
				commandBuffer = new CommandBuffer();
				commandBuffer.name = "Kayac.BasicRenderPipeline";
			}

			foreach (var camera in cameras)
			{
				Render(context, camera);
			}
		}

		void Render(
			ScriptableRenderContext context,
			Camera camera)
		{
			ScriptableCullingParameters cullingParameters;
			if (!camera.TryGetCullingParameters(out cullingParameters))
			{
				return;
			}
#if UNITY_EDITOR
			if (camera.cameraType == CameraType.SceneView)
			{
//				ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
			}
#endif
			cullingResults = context.Cull(ref cullingParameters);
			context.SetupCameraProperties(camera, stereoSetup: false);

			FillCommandBuffer(camera);
			context.ExecuteCommandBuffer(commandBuffer);
			commandBuffer.Clear();

			var tagId = new ShaderTagId("Main");
			var drawingSettings = new DrawingSettings(tagId, new SortingSettings(camera));
			drawingSettings.mainLightIndex = 0;
			var filteringSettings = new FilteringSettings();
//			filteringSettings.excludeMotionVectorObjects = false;
			filteringSettings.layerMask = -1;
			filteringSettings.renderQueueRange = RenderQueueRange.opaque;
			filteringSettings.renderingLayerMask = 0xffffffff;
//			filteringSettings.sortingLayerRange = SortingLayerRange.all;
			context.DrawRenderers(
				cullingResults,
				ref drawingSettings,
				ref filteringSettings);

			// 空は不透明パスの最後
			context.DrawSkybox(camera);

			// 透明パス
			filteringSettings.renderQueueRange = RenderQueueRange.transparent;
			context.DrawRenderers(
				cullingResults,
				ref drawingSettings,
				ref filteringSettings);

			context.Submit();
		}

		void FillCommandBuffer(Camera camera)
		{
			commandBuffer.BeginSample("Kayac.BasicRenderPipeline");
			var clearFlags = camera.clearFlags;
			commandBuffer.ClearRenderTarget(
				(clearFlags & CameraClearFlags.Depth) != 0,
				(clearFlags & CameraClearFlags.SolidColor) != 0,
				camera.backgroundColor,
				1f);
			var lights = cullingResults.visibleLights;
			Color lightColor = Color.black;
			Vector4 lightDirection = Vector3.up;
			foreach (var light in lights)
			{
				if (light.lightType == LightType.Directional)
				{
					lightColor = light.finalColor;
					lightDirection = light.localToWorldMatrix.GetColumn(2);
					lightDirection.x = -lightDirection.x;
					lightDirection.y = -lightDirection.y;
					lightDirection.z = -lightDirection.z;
					break; // ライト一個しか見ない!!
				}
			}
			commandBuffer.SetGlobalColor(mainLightColorId, lightColor);
			commandBuffer.SetGlobalVector(mainLightDirectionId, lightDirection);
			commandBuffer.EndSample("Kayac.BasicRenderPipeline");
		}
	}
}