using UnityEngine;

namespace Utils.Touch
{
    public static class Input
    {
        public static int GetTouchCount()
        {
            return UnityEngine.Input.touchCount;
        }

        public static void GetTouch(int index, out int id, out Vector2 position, out float pressure, out bool set)
        {
            var touch = UnityEngine.Input.GetTouch(index);

            id = touch.fingerId;
            position = touch.position;
            pressure = touch.pressure;
            set =
                touch.phase == TouchPhase.Began ||
                touch.phase == TouchPhase.Stationary ||
                touch.phase == TouchPhase.Moved;
        }

        public static Vector2 GetMousePosition()
        {
            return UnityEngine.Input.mousePosition;
        }

        public static bool GetMouseWentDown(int index)
        {
            return UnityEngine.Input.GetMouseButtonDown(index);
        }

        public static bool GetMouseIsHeld(int index)
        {
            return UnityEngine.Input.GetMouseButton(index);
        }

        public static bool GetMouseWentUp(int index)
        {
            return UnityEngine.Input.GetMouseButtonUp(index);
        }

        public static float GetMouseWheelDelta()
        {
            return UnityEngine.Input.mouseScrollDelta.y;
        }

        public static bool GetMouseExists()
        {
            return UnityEngine.Input.mousePresent;
        }
    }
}