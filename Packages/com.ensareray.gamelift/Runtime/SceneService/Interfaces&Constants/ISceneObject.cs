using System.Threading.Tasks;
using UnityEngine;

namespace GameLift.Scene
{
    public interface ISceneObject
    {
        Transform transform { get; }
        Task Initialize();
        Task Clear();
    }
}