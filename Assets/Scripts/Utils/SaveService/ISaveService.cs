namespace Utils.Save
{
    public interface ISaveService
    {
        public PrimitiveSaveHelper Raw {  get; }

        void Register<T>(string key) where T : ISaveable, new();

        SaveRepository<T> GetRepository<T>() where T : ISaveable, new();
    }
}