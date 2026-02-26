using UnityEngine;
using Firebase;
using Firebase.Extensions;
using Firebase.Firestore;

public class FirebaseSmokeTest : MonoBehaviour
{
    string docId = "jyOw9zXFQhAk50na6KMT";

    void Start()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Result != DependencyStatus.Available)
            {
                Debug.LogError("Firebase init failed: " + task.Result);
                return;
            }

            Debug.Log("✅ Firebase initialized");

            var db = FirebaseFirestore.DefaultInstance;

            db.Collection("QRCodes").Document(docId)
                .GetSnapshotAsync()
                .ContinueWithOnMainThread(t =>
                {
                    if (t.IsFaulted)
                    {
                        Debug.LogError("Firestore read error");
                        return;
                    }

                    var snap = t.Result;

                    if (snap.Exists)
                        Debug.Log("✅ Firestore bağlantısı OK");
                    else
                        Debug.Log("❌ Document bulunamadı");
                });
        });
    }
}