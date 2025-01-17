using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.Template.CompetitiveActionMultiplayer.Server.Editor
{
    /// <summary>
    /// This class implements some BuildPlayer callbacks that optimize the Linux Dedicated Server build time and size.
    /// </summary>
    public class DedicatedServerBuildOptimization : IPreprocessComputeShaders, IPreprocessShaders, IProcessSceneWithReport
    {
        public int callbackOrder { get; }

        /// <summary>
        /// When building for a Dedicated server, the lights are not needed in the gameplay scene.
        /// This callback is finding the lights settings and light objects in the scene and remove them from the build.
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="report"></param>
        public void OnProcessScene(Scene scene, BuildReport report)
        {
            if (BuildPipeline.isBuildingPlayer &&
                EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneLinux64 &&
                EditorUserBuildSettings.standaloneBuildSubtarget == StandaloneBuildSubtarget.Server)
            {
                LightmapSettings.lightmaps = null;
                LightmapSettings.lightProbes = null;
                var lights = GameObject.Find("Lighting");
                if (lights != null)
                {
                    Object.DestroyImmediate(lights);
                }
            }
        }

        /// <summary>
        /// When building for a Dedicated server, no rendering is required.
        /// This method is removing all shader variants from the build because they will not be used.
        /// </summary>
        /// <param name="shader"></param>
        /// <param name="snippet"></param>
        /// <param name="data"></param>
        public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
        {
            if (BuildPipeline.isBuildingPlayer &&
                EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneLinux64 &&
                EditorUserBuildSettings.standaloneBuildSubtarget == StandaloneBuildSubtarget.Server)
            {
                data.Clear();
            }
        }

        /// <summary>
        /// When building for a Dedicated server, no rendering is required.
        /// This method is removing all shader compute variants from the build because they will not be used.
        /// </summary>
        /// <param name="shader"></param>
        /// <param name="kernelName"></param>
        /// <param name="data"></param>
        public void OnProcessComputeShader(ComputeShader shader, string kernelName, IList<ShaderCompilerData> data)
        {
            if (BuildPipeline.isBuildingPlayer &&
                EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneLinux64 &&
                EditorUserBuildSettings.standaloneBuildSubtarget == StandaloneBuildSubtarget.Server)
            {
                data.Clear();
            }
        }
    }
}
