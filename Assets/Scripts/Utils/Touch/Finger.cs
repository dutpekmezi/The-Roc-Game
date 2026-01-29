using System.Collections.Generic;
using UnityEngine;

namespace Utils.Touch
{
    public class Finger
    {
        public float Age;
        public bool Expired;
        public int Index;
        public bool LastSet;
        public bool Old;
        public float Pressure;
        public Vector2 ScreenPosition;
        public bool Set;

        public List<Snapshot> Snapshots = new(1000);
        public Vector2 StartScreenPosition;
        public bool Swipe;
        public bool Tap;

        public float SnapshotDuration
        {
            get
            {
                if (Snapshots.Count > 0) return Age - Snapshots[0].Age;

                return 0.0f;
            }
        }

        public bool IsOverGui => Touch.PointOverGui(ScreenPosition);

        public bool Down => Set && LastSet == false;

        public bool Up => Set == false && LastSet;

        public Vector2 LastSnapshotScreenDelta
        {
            get
            {
                var snapshotCount = Snapshots.Count;

                if (snapshotCount <= 0) return Vector2.zero;
                var snapshot = Snapshots[snapshotCount - 1];

                if (snapshot != null) return ScreenPosition - snapshot.ScreenPosition;

                return Vector2.zero;
            }
        }

        public Vector2 SwipeScreenDelta => ScreenPosition - StartScreenPosition;
        

        public void ClearSnapshots(int count = -1)
        {
            // Clear old ones only?
            if (count > 0 && count <= Snapshots.Count)
            {
                for (var i = 0; i < count; i++) Snapshot.InactiveSnapshots.Add(Snapshots[i]);

                Snapshots.RemoveRange(0, count);
            }
            else if (count < 0)
            {
                Snapshot.InactiveSnapshots.AddRange(Snapshots);

                Snapshots.Clear();
            }
        }

        public void RecordSnapshot()
        {
            // Get an unused snapshot and set it up
            var snapshot = Snapshot.Pop();

            snapshot.Age = Age;
            snapshot.ScreenPosition = ScreenPosition;

            // Add to list
            Snapshots.Add(snapshot);
        }
    }
}