using Utils.Signal;

namespace Utils.Scene
{
    public class OnSceneTransitionStarted : Signal<SceneConfig> { }
    public class OnSceneTransitionEnded : Signal<SceneConfig> { }
}