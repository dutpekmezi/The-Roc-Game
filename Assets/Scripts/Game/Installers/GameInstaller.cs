using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Utils.LogicTimer;
using Utils.Scene;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Game.Installers
{
    public class GameInstaller : MonoBehaviour, ISceneObject
    {
        private bool _initialized;

        private readonly List<IDisposable> _disposables = new();

            
        private readonly List<IInitializable> _initializables = new();

        private LogicTimer _logicTimer;

        public Task Initialize()
        {
            if (_initialized)
                return Task.CompletedTask;

            _initialized = true;


            _logicTimer = BindDisposable(new LogicTimer(OnLogicTick));
            _logicTimer.Start();

#if UNITY_EDITOR
            EditorApplication.pauseStateChanged += OnEditorPause;
#endif

            return Task.CompletedTask;
        }

        private void FixedUpdate()
        {
            _logicTimer?.Update();
        }

        public Task Clear()
        {
            for (int i = 0; i < _initializables.Count; i++)
                _initializables[i].Dispose();
            _initializables.Clear();

            for (int i = 0; i < _disposables.Count; i++)
                _disposables[i].Dispose();
            _disposables.Clear();

#if UNITY_EDITOR
            EditorApplication.pauseStateChanged -= OnEditorPause;
#endif

            return Task.CompletedTask;
        }

        private void OnLogicTick()
        {
  
        }

        private T BindDisposable<T>(T obj)
        {
            if (obj is IDisposable disposable)
                _disposables.Add(disposable);

            return obj;
        }

        private T InitializeInitializable<T>(T initializable) where T : IInitializable
        {
            initializable.Initialize();
            _initializables.Add(initializable);
            return initializable;
        }

        private void OnApplicationPause(bool pause)
        {
            HandlePause(pause);
        }

#if UNITY_EDITOR
        private void OnEditorPause(PauseState pauseState)
        {
            HandlePause(pauseState == PauseState.Paused);
        }
#endif

        private void HandlePause(bool pause)
        {
            if (pause) _logicTimer?.Pause();
            else _logicTimer?.Resume();
        }
    }
}
