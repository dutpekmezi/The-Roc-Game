using System.Threading.Tasks;
using UnityEngine;

namespace Utils.Scene
{
    public interface ISceneObject
    {
        Transform transform { get; }
        Task Initialize();
        Task Clear();
    }
}