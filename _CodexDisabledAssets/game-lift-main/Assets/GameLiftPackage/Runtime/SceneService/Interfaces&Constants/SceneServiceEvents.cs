using GameLift.Signal;

namespace GameLift.Scene
{
    public class OnSceneTransitionStarted : Signal<SceneConfig> { }
    public class OnSceneTransitionEnded : Signal<SceneConfig> { }
}