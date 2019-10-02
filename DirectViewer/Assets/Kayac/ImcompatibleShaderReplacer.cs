using UnityEngine;
using System.Collections.Generic;

namespace Kayac
{
    public class IncompatibleShaderReplacer
    {
		const int DefaultCacheSize = 256;
#if UNITY_EDITOR
		public IncompatibleShaderReplacer(int cacheSize = DefaultCacheSize)
        {
            materialCache = new Material[cacheSize];
            tmpMaterials = new List<Material>();
        }

        public void Replace(Transform transform)
        {
            // Rendererを見つける
            var gameObject = transform.gameObject;
            var renderer = gameObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.GetSharedMaterials(tmpMaterials);
                foreach (var material in tmpMaterials)
                {
                    var hashIndex = CalcCacheIndex(material);
                    if (materialCache[hashIndex] != material)
                    {
                        material.shader = Shader.Find(material.shader.name);
                        materialCache[hashIndex] = material;
                    }
                }
            }
            // 再帰
            int childCount = transform.childCount;
            for (int i = 0; i < childCount; i++)
            {
                var child = transform.GetChild(i);
                Replace(child);
            }
        }

        // non-public ---------------
        Material[] materialCache;
        List<Material> tmpMaterials;

        int CalcCacheIndex(Material material)
        {
            int hashValue = material.GetHashCode();
            return hashValue % materialCache.Length;
        }
#else // エディタでなければ何もしない
		public IncompatibleShaderReplacer(int cacheSize = DefaultCacheSize)
        {
        }
        public void Replace(Transform transform)
        {
        }
#endif
    }
}