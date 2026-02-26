using System;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Utils.Save;

public class SaveResetToolsWindow : EditorWindow
{
    private static readonly string[] SaveKeys =
    {
        "currencies",
        "store_purchased_products",
        "current_level"
    };

    private string _status = "Hazır";
    private bool _isBusy;

    [MenuItem("Window/Tools/Save Reset")]
    public static void Open()
    {
        GetWindow<SaveResetToolsWindow>("Save Reset");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Kayıt Temizleme", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Bu pencere local SaveService kayıtlarını ve Firebase kullanıcı kayıtlarını temizler.", MessageType.Info);

        using (new EditorGUI.DisabledScope(_isBusy))
        {
            if (GUILayout.Button("Local kayıtları sil"))
            {
                ClearLocalData();
            }

            if (GUILayout.Button("Firebase kayıtlarını sil"))
            {
                _ = ClearFirebaseDataAsync();
            }

            if (GUILayout.Button("Hem local hem Firebase sil"))
            {
                _ = ClearAllDataAsync();
            }
        }

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Firebase silme işlemi için Play Mode açık olmalı.", MessageType.Warning);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Durum:", _status);
    }

    private void ClearLocalData()
    {
        try
        {
            EncryptedSaveHandler saveHandler = new EncryptedSaveHandler();

            foreach (var key in SaveKeys)
            {
                saveHandler.DeleteData(key);
            }

            _status = "Local kayıtlar silindi.";
            Debug.Log("[SaveResetToolsWindow] Local kayıtlar silindi.");
        }
        catch (Exception e)
        {
            _status = "Local kayıtlar silinemedi.";
            Debug.LogError("[SaveResetToolsWindow] Local silme hatası: " + e);
        }

        Repaint();
    }

    private async Task ClearFirebaseDataAsync()
    {
        _isBusy = true;
        _status = "Firebase kayıtları siliniyor...";
        Repaint();

        try
        {
            FirestoreGameSecurityService firebaseService = FirestoreGameSecurityService.Instance;

            if (firebaseService == null || !firebaseService.IsReady)
            {
                _status = "Firebase hazır değil. Play Mode'da tekrar deneyin.";
                return;
            }

            await firebaseService.ClearCurrentUserDataAsync();
            _status = "Firebase kayıtları silindi.";
            Debug.Log("[SaveResetToolsWindow] Firebase kayıtları silindi.");
        }
        catch (Exception e)
        {
            _status = "Firebase kayıtları silinemedi.";
            Debug.LogError("[SaveResetToolsWindow] Firebase silme hatası: " + e);
        }
        finally
        {
            _isBusy = false;
            Repaint();
        }
    }

    private async Task ClearAllDataAsync()
    {
        _isBusy = true;
        _status = "Tüm kayıtlar siliniyor...";
        Repaint();

        try
        {
            ClearLocalData();
            await ClearFirebaseDataAsync();
            _status = "Local ve Firebase kayıtları silindi.";
        }
        finally
        {
            _isBusy = false;
            Repaint();
        }
    }
}
