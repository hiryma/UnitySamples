using UnityEngine;

namespace Kayac.Debug
{
    [CreateAssetMenu(fileName = "DebugRendererAsset", menuName = "Kayac/Debug/RendererAsset", order = 1)]
    public class DebugRendererAsset : ScriptableObject
    {
        [SerializeField] Shader textShader;
        [SerializeField] Shader texturedShader;
        [SerializeField] Font font;

        public Shader TextShader { get { return textShader; } }
        public Shader TexturedShader { get { return texturedShader; } }
        public Font Font { get { return font; } }
    }
}
