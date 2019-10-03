using UnityEngine;

namespace Kayac
{
    [CreateAssetMenu(fileName = "LightPostProcessorAsset", menuName = "Kayac/LightPostProcessorAsset", order = 1)]
    public class LightPostProcessorAsset : ScriptableObject
    {
        [SerializeField] Shader bloomCombineShader;
        [SerializeField] Shader brightnessExtractionShader;
        [SerializeField] Shader compositionShader;
        [SerializeField] Shader gaussianBlurShader;

        public Shader BloomCombineShader { get { return bloomCombineShader; } }
        public Shader BrightnessExtractionShader { get { return brightnessExtractionShader; } }
        public Shader CompositionShader { get { return compositionShader; } }
        public Shader GaussianBlurShader { get { return gaussianBlurShader; } }
    }
}
