using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
namespace Utils.Scene
{
    public interface ISceneService
    {
        public Dictionary<string, GameObject> LoadedScenes { get; }

        Task<GameObject> LoadScene(string sceneType);
        Task RemoveScene(string scene);

        void Clear();
    }
}