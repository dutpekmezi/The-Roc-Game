using System.Collections.Generic;
using UnityEngine;

namespace Utils.Touch
{
	public class Snapshot
    {
        public static List<Snapshot> InactiveSnapshots = new(1000);

        public float Age;
        public Vector2 ScreenPosition;

        public static Snapshot Pop()
        {
            if (InactiveSnapshots.Count > 0)
            {
                var index = InactiveSnapshots.Count - 1;
                var snapshot = InactiveSnapshots[index];

                InactiveSnapshots.RemoveAt(index);

                return snapshot;
            }

            return new Snapshot();
        }

        public static int GetLowerIndex(List<Snapshot> snapshots, float targetAge)
        {
            if (snapshots != null)
            {
                var count = snapshots.Count;

                if (count > 0)
                    for (var i = count - 1; i >= 0; i--)
                        if (snapshots[i].Age <= targetAge)
                            return i;

                return 0;
            }

            return -1;
        }
    }
}