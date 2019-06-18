using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Kayac
{
	[CreateAssetMenu(menuName = "Kayac/Rendering/BasicPipeline")]
	public class BasicRenderPipelineAsset : RenderPipelineAsset
	{
		protected override RenderPipeline CreatePipeline()
		{
			return new BasicRenderPipeline();
		}
	}

}