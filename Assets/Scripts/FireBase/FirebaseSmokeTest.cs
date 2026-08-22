using System;
using System.Threading.Tasks;
using UnityEngine;
#if !UNITY_WEBGL || UNITY_EDITOR
using Firebase;
using Firebase.Extensions;
using Firebase.Firestore;
#endif

public class FirebaseSmokeTest : MonoBehaviour
{
#if !UNITY_WEBGL || UNITY_EDITOR
    private readonly string docId = "jyOw9zXFQhAk50na6KMT";

    private void Start()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                Debug.LogWarning("[FirebaseSmokeTest] Firebase dependency check skipped: " + GetTaskIssue(task));
                return;
            }

            if (task.Result != DependencyStatus.Available)
            {
                Debug.LogWarning("[FirebaseSmokeTest] Firebase dependencies are not available: " + task.Result);
                return;
            }

            Debug.Log("Firebase initialized");

            FirebaseFirestore db = FirebaseFirestore.DefaultInstance;

            db.Collection("QRCodes").Document(docId)
                .GetSnapshotAsync()
                .ContinueWithOnMainThread(t =>
                {
                    if (t.IsFaulted || t.IsCanceled)
                    {
                        Debug.LogWarning("[FirebaseSmokeTest] Firestore read skipped: " + GetTaskIssue(t));
                        return;
                    }

                    DocumentSnapshot snap = t.Result;

                    if (snap.Exists)
                    {
                        Debug.Log("Firestore connection OK");
                    }
                    else
                    {
                        Debug.Log("Document not found");
                    }
                });
        });
    }

    private static string GetTaskIssue(Task task)
    {
        if (task == null)
        {
            return "unknown";
        }

        if (task.IsCanceled)
        {
            return "task canceled";
        }

        if (task.Exception == null)
        {
            return "unknown";
        }

        Exception exception = task.Exception.Flatten().InnerException ?? task.Exception;
        return exception.GetType().Name + ": " + exception.Message;
    }
#else
    private void Start()
    {
        Debug.Log("[FirebaseSmokeTest] Skipped in WebGL player builds.");
    }
#endif
}
