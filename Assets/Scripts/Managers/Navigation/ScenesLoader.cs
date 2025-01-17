using System.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;
using Unity.Scenes;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.Template.CompetitiveActionMultiplayer
{
    /// <summary>
    /// This utility class is handling Unity Scenes and Subscenes loading/unloading for a gameplay session.
    /// </summary>
    static class ScenesLoader
    {
        public static async Task LoadGameplayAsync(World server, World client)
        {
            await LoadGameplayScenesAsync();
            if (server != null)
                await WaitForAllSubScenesToLoadAsync(server, LoadingData.LoadingSteps.LoadServer);
            if (client != null)
                await WaitForAllSubScenesToLoadAsync(client, LoadingData.LoadingSteps.LoadClient);
        }

        public static async Task UnloadGameplayScenesAsync()
        {
            LoadingData.Instance.UpdateLoading(LoadingData.LoadingSteps.UnloadingWorld);

            var gameplay = SceneManager.GetSceneByName(GameManager.GameSceneName);
            if (gameplay.IsValid() && gameplay != SceneManager.GetActiveScene())
            {
                var unloadScene = SceneManager.UnloadSceneAsync(gameplay);
                UpdateLoadingStateAsync(LoadingData.LoadingSteps.UnloadingGameScene, unloadScene);
                await unloadScene;
            }
            var resource = SceneManager.GetSceneByName(GameManager.ResourcesSceneName);
            if (resource.IsValid())
            {
                var unloadScene = SceneManager.UnloadSceneAsync(resource);
                UpdateLoadingStateAsync(LoadingData.LoadingSteps.UnloadingResourcesScene, unloadScene);
                await unloadScene;
            }
        }

        static async Task LoadGameplayScenesAsync()
        {
            await LoadSceneAsync(GameManager.GameSceneName, LoadingData.LoadingSteps.LoadGameScene);
            await LoadSceneAsync(GameManager.ResourcesSceneName, LoadingData.LoadingSteps.LoadResourcesScene);
        }

        static async Task LoadSceneAsync(string sceneName, LoadingData.LoadingSteps step)
        {
            if(SceneManager.GetSceneByName(sceneName).isLoaded)
                return;
            var sceneLoading = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            UpdateLoadingStateAsync(step, sceneLoading);
            await sceneLoading;
        }

        static async Task WaitForAllSubScenesToLoadAsync(World world, LoadingData.LoadingSteps step)
        {
            if (world == null)
                return;

            LoadingData.Instance.UpdateLoading(step);

            using var scenesQuery = world.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<SceneReference>());
            using var scenesLeftToLoad = scenesQuery.ToEntityListAsync(Allocator.Persistent, out var handle);
            handle.Complete();

            float count = scenesLeftToLoad.Length;
            while (scenesLeftToLoad.Length > 0)
            {
                for (var i = 0; i < scenesLeftToLoad.Length; i++)
                {
                    var sceneEntity = scenesLeftToLoad[i];
                    if (SceneSystem.IsSceneLoaded(world.Unmanaged, sceneEntity))
                    {
                        scenesLeftToLoad.RemoveAt(i);
                        var numLoaded = count - scenesLeftToLoad.Length;
                        var loadingProgress = numLoaded / count;
                        LoadingData.Instance.UpdateLoading(step, loadingProgress);
                        i--;
                    }
                }

                await Awaitable.NextFrameAsync();
            }
        }

        static async void UpdateLoadingStateAsync(LoadingData.LoadingSteps step, AsyncOperation loadingTask)
        {
            while (loadingTask != null && !loadingTask.isDone)
            {
                LoadingData.Instance.UpdateLoading(step, loadingTask.progress);
                await Awaitable.NextFrameAsync();
            }
        }
    }
}
