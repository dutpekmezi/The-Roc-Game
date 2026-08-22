using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameLift.Save
{
    public class SaveService : ISaveService
    {
        private readonly ISaveHandler _saveHandler;
        private readonly Dictionary<Type, object> _repositories = new();
        private readonly PrimitiveSaveHelper _primitiveHelper;
        public PrimitiveSaveHelper Raw => _primitiveHelper;

        public static SaveService Instance { get; private set; }

        public SaveService(ISaveHandler saveHandler)
        {
            if (Instance != null)
                throw new System.Exception("Save Service Already Has an Instance");

            Instance = this;

            this._saveHandler = saveHandler;
            this._primitiveHelper = new PrimitiveSaveHelper(saveHandler);
        }

        public void Register<T>(string key) where T : ISaveable, new()
        {
            if (_repositories.ContainsKey(typeof(T)))
                return;

            var repo = new SaveRepository<T>(_saveHandler, key);
            _repositories.Add(typeof(T), repo);

            Debug.Log($"[SaveService] Registered type {typeof(T).Name} with key {key}");
        }

        public SaveRepository<T> GetRepository<T>() where T : ISaveable, new()
        {
            if (_repositories.TryGetValue(typeof(T), out var repo))
                return repo as SaveRepository<T>;

            Debug.LogError($"[SaveService] Repository for {typeof(T).Name} not registered!");
            return null;
        }
    }
}
