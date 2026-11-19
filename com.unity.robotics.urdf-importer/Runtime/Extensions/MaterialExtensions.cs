using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Unity.Robotics
{
    public static class MaterialExtensions
    {
        public enum RenderPipelineType 
        {
            Standard,
            URP,
            HDRP,
        }
        
        private static string[] standardShaders = { "Standard" };
        private static string[] hdrpShaders = { "HDRP/Lit" };
        private static string[] urpShaders = { "Universal Render Pipeline/Lit" };
        public static Material CreateBasicMaterial()
        {
            try
            {
                string[] shadersToTry = standardShaders;
                if (GetRenderPipelineType() == RenderPipelineType.HDRP)
                {
                    shadersToTry = hdrpShaders;
                }
                else if (GetRenderPipelineType() == RenderPipelineType.URP)
                {
                    shadersToTry = urpShaders;
                }

                foreach (var shaderName in shadersToTry)
                {
                    Shader shader = Shader.Find(shaderName);
                    if (shader != null)
                    {
                        var material = new Material(shader);
                        material.SetFloat("_Metallic", 0.75f);
                        material.SetFloat("_Glossiness", 0.75f);
                        return material;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogAssertion(ex.ToString());
            }
            return null;
        }

        // Modified (bada0817 fork): GraphicsSettings.renderPipelineAsset is obsolete in Unity 6 (the API Updater
        // rewrites it to defaultRenderPipeline). Pick the property per editor version so 2020.2+ keeps compiling.
        static RenderPipelineAsset DefaultPipelineAsset
        {
#if UNITY_6000_0_OR_NEWER
            get { return GraphicsSettings.defaultRenderPipeline; }
#else
            get { return GraphicsSettings.renderPipelineAsset; }
#endif
        }

        /// Checks if current render pipeline is HDRP 
        /// Used for creating the proper default material.
        public static RenderPipelineType GetRenderPipelineType()
        {
            var pipeline = DefaultPipelineAsset;
            if (pipeline != null) 
            {
                if (pipeline.GetType().ToString().Contains("HighDefinition"))
                {
                    return RenderPipelineType.HDRP;
                }
                else if (pipeline.GetType().ToString().Contains("Universal"))
                {
                    return RenderPipelineType.URP;
                }
            }
            return RenderPipelineType.Standard;
        }

        // Modified (bada0817 fork): use the color property the material actually has, so highlight colors also work
        // for materials whose shader does not follow the pipeline default (e.g. glTFast materials use "baseColorFactor").
        static readonly string[] k_ColorProperties = { "_BaseColor", "_Color", "baseColorFactor" };

        static string ColorPropertyOf(Material material)
        {
            string preferred = GetRenderPipelineType() != RenderPipelineType.Standard ? "_BaseColor" : "_Color";
            if (material == null || material.HasProperty(preferred))
            {
                return preferred;
            }
            foreach (var name in k_ColorProperties)
            {
                if (material.HasProperty(name))
                {
                    return name;
                }
            }
            return null;
        }

        public static void SetMaterialColor(Material material, Color color)
        {
            string property = ColorPropertyOf(material);
            if (property != null)
            {
                material.SetColor(property, color);
            }
        }

        public static Color GetMaterialColor(Material material)
        {
            string property = ColorPropertyOf(material);
            return property != null ? material.GetColor(property) : Color.white;
        }

        public static void SetMaterialEmissionColor(Material material, Color color)
        {
            // Assuming both shaders use the _EmissionColor property. Not tested for HDRP and URP.
            //Library/PackageCache/com.unity.render-pipelines.universal@10.3.2/Shaders/Lit.shader 
            material.SetColor(GetRenderPipelineType() != RenderPipelineType.Standard ? "_EmissionColor" : "_EmissionColor", color);
            material.EnableKeyword("_EMISSION");
        }

        public static Color GetMaterialColor(Renderer renderer)
        {
            // Modified (bada0817 fork): read the shared material (no instance created) with the property fallback above
            return GetMaterialColor(renderer.sharedMaterial);
        }
    }
}