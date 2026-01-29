using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Utils.Logger;

namespace Utils.Touch
{
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    [AddComponentMenu("Touch")]
    public class Touch : MonoBehaviour
    {
        public const int MouseFingerIndex = -1;

        public const int HoverFingerIndex = -42;

        private const int DefaultReferenceDPI = 200;

        private const int DefaultGUILayers = 1 << 5;

        private const float DefaultTapThreshold = 0.2f;

        private const float DefaultSwipeThreshold = 100.0f;

        private const float DefaultRecordLimit = 10.0f;

        public static readonly List<Touch> Instances = new();

        public static readonly List<Finger> Fingers = new(10);

        public static readonly List<Finger> InactiveFingers = new(10);

        private static readonly List<RaycastResult> TempRaycastResults = new(10);
        private static readonly List<Finger> FilteredFingers = new(10);

        private static PointerEventData tempPointerEventData;
        private static EventSystem tempEventSystem;

        private static readonly HashSet<Finger> MissingFingers = new();

        private static readonly List<Finger> TempFingers = new();

        [SerializeField] private float tapThreshold = DefaultTapThreshold;

        [SerializeField] private float swipeThreshold = DefaultSwipeThreshold;

        [SerializeField] private int referenceDpi = DefaultReferenceDPI;

        [SerializeField] private LayerMask guiLayers = DefaultGUILayers;

        private readonly float recordThreshold = 5.0f;

        private static int CurrentReferenceDpi =>
            Instances.Count > 0 ? Instances[0].referenceDpi : DefaultReferenceDPI;

        public static LayerMask CurrentGuiLayers => Instances.Count > 0 ? Instances[0].guiLayers : DefaultGUILayers;

        public static float ScalingFactor
        {
            get
            {
                var dpi = Screen.dpi;
                if (dpi <= 0) return 1.0f;
                return CurrentReferenceDpi / dpi;
            }
        }

        protected virtual void Update()
        {
            if (Instances[0] == this) UpdateFingers(Time.unscaledDeltaTime, true);
        }

        protected virtual void OnEnable()
        {
            Instances.Add(this);

            UpdateMouseEmulation();
        }

        protected virtual void OnDisable()
        {
            OnFingerDown = default;
            OnFingerUpdate = default;
            OnFingerUp = default;
            OnFingerOld = default;
            OnFingerTap = default;
            OnFingerSwipe = default;
            OnGesture = default;
            OnFingerExpired = default;
            OnFingerInactive = default;
            
            Instances.Remove(this);
        }

        public static event Action<Finger> OnFingerDown;
        public static event Action<Finger> OnFingerUpdate;
        public static event Action<Finger> OnFingerUp;
        public static event Action<Finger> OnFingerOld;
        public static event Action<Finger> OnFingerTap;
        public static event Action<Finger> OnFingerSwipe;
        public static event Action<List<Finger>> OnGesture;
        public static event Action<Finger> OnFingerExpired;

        public static event Action<Finger> OnFingerInactive;
        public event Action OnSimulateFingers;

        public static EventSystem GetEventSystem()
        {
            var currentEventSystem = EventSystem.current;

            if (currentEventSystem == null) currentEventSystem = FindObjectOfType<EventSystem>();

            return currentEventSystem;
        }

        public static bool PointOverGui(Vector2 screenPosition)
        {
            return RaycastGui(screenPosition).Count > 0;
        }

        public static List<RaycastResult> RaycastGui(Vector2 screenPosition)
        {
            return RaycastGui(screenPosition, CurrentGuiLayers);
        }

        public static List<RaycastResult> RaycastGui(Vector2 screenPosition, LayerMask layerMask)
        {
            TempRaycastResults.Clear();

            var currentEventSystem = GetEventSystem();

            if (currentEventSystem != null)
            {
                if (currentEventSystem != tempEventSystem)
                {
                    tempEventSystem = currentEventSystem;

                    if (tempPointerEventData == null)
                        tempPointerEventData = new PointerEventData(tempEventSystem);
                    else
                        tempPointerEventData.Reset();
                }

                tempPointerEventData.position = screenPosition;

                currentEventSystem.RaycastAll(tempPointerEventData, TempRaycastResults);

                if (TempRaycastResults.Count > 0)
                    for (var i = TempRaycastResults.Count - 1; i >= 0; i--)
                    {
                        var raycastResult = TempRaycastResults[i];
                        var raycastLayer = 1 << raycastResult.gameObject.layer;

                        if ((raycastLayer & layerMask) == 0) TempRaycastResults.RemoveAt(i);
                    }
            }
            else
            {
                GameLogger.LogError(
                    "Failed to RaycastGui because your scene doesn't have an event system! To add one, go to: GameObject/UI/EventSystem");
            }

            return TempRaycastResults;
        }

        public void UpdateMouseEmulation()
        {
            UnityEngine.Input.simulateMouseWithTouches = false;
        }

        private void UpdateFingers(float deltaTime, bool poll)
        {
            BeginFingers(deltaTime);

            if (poll) PollFingers();

            EndFingers(deltaTime);
            UpdateEvents();
        }

        private void BeginFingers(float deltaTime)
        {
            for (var i = InactiveFingers.Count - 1; i >= 0; i--)
            {
                var inactiveFinger = InactiveFingers[i];

                inactiveFinger.Age += deltaTime;

                if (inactiveFinger.Expired == false && inactiveFinger.Age > tapThreshold)
                {
                    inactiveFinger.Expired = true;

                    if (OnFingerExpired != null) OnFingerExpired(inactiveFinger);
                }
            }

            for (var i = Fingers.Count - 1; i >= 0; i--)
            {
                var finger = Fingers[i];

                if (finger.Up || finger.Set == false)
                {
                    Fingers.RemoveAt(i);
                    InactiveFingers.Add(finger);

                    finger.Age = 0.0f;

                    // Pool old snapshots
                    finger.ClearSnapshots();

                    if (OnFingerInactive != null) OnFingerInactive(finger);
                }
                else
                {
                    finger.LastSet = finger.Set;

                    finger.Set = false;
                    finger.Tap = false;
                    finger.Swipe = false;
                }
            }

            MissingFingers.Clear();

            foreach (var finger in Fingers) MissingFingers.Add(finger);
        }

        private void EndFingers(float deltaTime)
        {
            TempFingers.Clear();

            TempFingers.AddRange(MissingFingers);

            foreach (var finger in TempFingers) AddFinger(finger.Index, finger.ScreenPosition, finger.Pressure, false);

            // Update fingers
            foreach (var finger in Fingers)
                // Up?
                if (finger.Up)
                {
                    // Tap or Swipe?
                    if (finger.Age <= tapThreshold)
                    {
                        if (finger.SwipeScreenDelta.magnitude * ScalingFactor < swipeThreshold)
                            finger.Tap = true;
                        else
                            finger.Swipe = true;
                    }
                }
                // Down?
                else if (finger.Down == false)
                {
                    // Age it
                    finger.Age += deltaTime;

                    // Too old?
                    if (finger.Age > tapThreshold && finger.Old == false)
                    {
                        finger.Old = true;

                        if (OnFingerOld != null) OnFingerOld(finger);
                    }
                }
        }

        // Read new hardware finger data
        private void PollFingers()
        {
            if (Input.GetTouchCount() > 0)
                for (var i = 0; i < Input.GetTouchCount(); i++)
                {
                    int id;
                    Vector2 position;
                    float pressure;
                    bool set;

                    Input.GetTouch(i, out id, out position, out pressure, out set);

                    AddFinger(id, position, pressure, set);
                }

            if (Input.GetMouseExists())
            {
                var mousePosition = Input.GetMousePosition();
                var hoverFinger = AddFinger(HoverFingerIndex, mousePosition, 0.0f, true);

                hoverFinger.LastSet = true;
            }

            if (Input.GetMouseExists())
            {
                var mouseSet = false;
                var mouseUp = false;

                for (var i = 0; i < 5; i++)
                {
                    mouseSet |= Input.GetMouseIsHeld(i);
                    mouseUp |= Input.GetMouseWentUp(i);
                }

                if (mouseSet || mouseUp)
                {
                    var mousePosition = Input.GetMousePosition();
                    AddFinger(MouseFingerIndex, mousePosition, 1.0f, mouseSet);
                }
            }

            if (OnSimulateFingers != null)
                OnSimulateFingers.Invoke();
        }

        private void UpdateEvents()
        {
            var fingerCount = Fingers.Count;

            if (fingerCount > 0)
            {
                for (var i = 0; i < fingerCount; i++)
                {
                    var finger = Fingers[i];

                    if (finger.Tap && OnFingerTap != null) OnFingerTap(finger);
                    if (finger.Swipe && OnFingerSwipe != null) OnFingerSwipe(finger);
                    if (finger.Down && OnFingerDown != null) OnFingerDown(finger);
                    if (OnFingerUpdate != null) OnFingerUpdate(finger);
                    if (finger.Up && OnFingerUp != null) OnFingerUp(finger);
                }

                if (OnGesture != null)
                {
                    FilteredFingers.Clear();
                    FilteredFingers.AddRange(Fingers);

                    OnGesture(FilteredFingers);
                }
            }
        }

        public Finger AddFinger(int index, Vector2 screenPosition, float pressure, bool set)
        {
            var finger = FindFinger(index);

            if (finger != null)
            {
                MissingFingers.Remove(finger);
            }
            else
            {
                if (set == false) return null;

                var inactiveIndex = FindInactiveFingerIndex(index);

                // Use inactive finger?
                if (inactiveIndex >= 0)
                {
                    finger = InactiveFingers[inactiveIndex];
                    InactiveFingers.RemoveAt(inactiveIndex);

                    // Reset values
                    finger.Age = 0.0f;
                    finger.Old = false;
                    finger.Set = false;
                    finger.LastSet = false;
                    finger.Tap = false;
                    finger.Swipe = false;
                    finger.Expired = false;
                }
                else
                {
                    // Create new finger?
                    if (finger == null)
                    {
                        finger = new Finger();

                        finger.Index = index;
                    }
                }

                finger.StartScreenPosition = screenPosition;
                PointOverGui(screenPosition);

                Fingers.Add(finger);
            }

            finger.Set = set;
            finger.ScreenPosition = screenPosition;
            finger.Pressure = pressure;

            if (DefaultRecordLimit > 0.0f)
                if (finger.SnapshotDuration > DefaultRecordLimit)
                {
                    var removeCount = Snapshot.GetLowerIndex(finger.Snapshots, finger.Age - DefaultRecordLimit);

                    finger.ClearSnapshots(removeCount);
                }

            // Record snapshot?
            if (recordThreshold > 0.0f)
            {
                if (finger.Snapshots.Count == 0 || finger.LastSnapshotScreenDelta.magnitude >= recordThreshold)
                    finger.RecordSnapshot();
            }
            else
            {
                finger.RecordSnapshot();
            }

            return finger;
        }

        private Finger FindFinger(int index)
        {
            foreach (var finger in Fingers)
                if (finger.Index == index)
                    return finger;

            return null;
        }

        private int FindInactiveFingerIndex(int index)
        {
            for (var i = InactiveFingers.Count - 1; i >= 0; i--)
                if (InactiveFingers[i].Index == index)
                    return i;

            return -1;
        }
    }
}