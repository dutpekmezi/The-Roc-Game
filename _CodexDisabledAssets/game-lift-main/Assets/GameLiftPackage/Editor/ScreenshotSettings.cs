using UnityEngine;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Input;
using System.IO;
using Cysharp.Threading.Tasks;

namespace GameLift.Editor
{
    [System.Serializable]
    public struct ScreenshotResolution
    {
        public string title;
        public int width;
        public int height;
    }

    [CreateAssetMenu(fileName = "ScreenshotSettings", menuName = "Game Lift/Screenshot Settings")]
    public class ScreenshotSettings : ScriptableObject
    {
        [Tooltip("Format: {title}_{width}x{height}")]
        public string saveFolder = "Screenshots";
        
        [Tooltip("Increments automatically after each successful screenshot session.")]
        public int take = 1;
        
        public ScreenshotResolution[] resolutions = new ScreenshotResolution[]
        {
            new ScreenshotResolution { title = "Play Store", width = 1080, height = 1920 },
            new ScreenshotResolution { title = "iPhone", width = 1320, height = 2868 },
            new ScreenshotResolution { title = "iPad", width = 2064, height = 2752 }
        };
    }

    [CustomEditor(typeof(ScreenshotSettings))]
    public class ScreenshotSettingsEditor : UnityEditor.Editor
    {
        private class ScreenshotGroup
        {
            public string title;
            public System.Collections.Generic.List<Texture2D> textures = new System.Collections.Generic.List<Texture2D>();
            public Vector2 scrollPosition;
        }

        private System.Collections.Generic.List<ScreenshotGroup> galleryGroups = new System.Collections.Generic.List<ScreenshotGroup>();

        private void OnEnable()
        {
            RefreshGallery((ScreenshotSettings)target);
        }

        private void OnDisable()
        {
            ClearTextures();
        }

        private void ClearTextures()
        {
            foreach (var group in galleryGroups)
            {
                foreach (var tex in group.textures)
                {
                    if (tex != null) DestroyImmediate(tex);
                }
            }
            galleryGroups.Clear();
        }

        private void RefreshGallery(ScreenshotSettings settings)
        {
            ClearTextures();

            string directory = string.IsNullOrEmpty(settings.saveFolder) ? "Screenshots" : settings.saveFolder;
            string fullPath = Path.GetFullPath(directory);

            if (!Directory.Exists(fullPath)) return;

            foreach (var res in settings.resolutions)
            {
                string resDir = Path.Combine(fullPath, res.title);
                if (Directory.Exists(resDir))
                {
                    string[] files = Directory.GetFiles(resDir, "*.png");
                    if (files.Length > 0)
                    {
                        System.Array.Sort(files);

                        var group = new ScreenshotGroup { title = $"{res.title} ({res.width}x{res.height})" };

                        foreach (var file in files)
                        {
                            byte[] fileData = System.IO.File.ReadAllBytes(file);
                            Texture2D tex = new Texture2D(2, 2);
                            tex.LoadImage(fileData);
                            group.textures.Add(tex);
                        }
                        
                        galleryGroups.Add(group);
                    }
                }
            }
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            ScreenshotSettings settings = (ScreenshotSettings)target;

            GUILayout.Space(20);
            if (GUILayout.Button("Take Screenshots", GUILayout.Height(40)))
            {
                TakeScreenshots(settings).Forget();
            }

            GUILayout.Space(5);
            if (GUILayout.Button("Open Screenshots Folder", GUILayout.Height(30)))
            {
                OpenScreenshotsFolder(settings);
            }

            GUILayout.Space(5);
            if (GUILayout.Button("Refresh Gallery", GUILayout.Height(30)))
            {
                RefreshGallery(settings);
            }

            // Draw Gallery
            if (galleryGroups.Count > 0)
            {
                GUILayout.Space(20);
                GUILayout.Label("All Screenshots", EditorStyles.boldLabel);
                
                foreach (var group in galleryGroups)
                {
                    GUILayout.Space(15);
                    GUILayout.Label(group.title, EditorStyles.boldLabel);

                    if (group.textures.Count > 0)
                    {
                        group.scrollPosition = EditorGUILayout.BeginScrollView(group.scrollPosition, GUILayout.Height(300));
                        EditorGUILayout.BeginHorizontal();
                        
                        foreach (var tex in group.textures)
                        {
                            if (tex == null) continue;
                            
                            float aspect = (float)tex.width / tex.height;
                            float height = 260f; // Fixed height for a neat gallery
                            float width = height * aspect;
                            
                            Rect rect = GUILayoutUtility.GetRect(width, height);
                            rect.width = width;
                            GUI.DrawTexture(rect, tex, ScaleMode.ScaleToFit);
                        }
                        
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.EndScrollView();
                    }
                }
            }
        }

        private void OpenScreenshotsFolder(ScreenshotSettings settings)
        {
            string directory = string.IsNullOrEmpty(settings.saveFolder) ? "Screenshots" : settings.saveFolder;
            string fullPath = Path.GetFullPath(directory);

            if (Directory.Exists(fullPath))
            {
                EditorUtility.RevealInFinder(fullPath);
            }
            else
            {
                Debug.LogWarning($"[ScreenshotSettings] Directory does not exist yet: {fullPath}");
            }
        }

        private async UniTask TakeScreenshots(ScreenshotSettings settings)
        {
            if (settings.resolutions == null || settings.resolutions.Length == 0)
            {
                Debug.LogWarning("ScreenshotSettings: No resolutions defined!");
                return;
            }

            if (!Application.isPlaying)
            {
                Debug.LogWarning("ScreenshotSettings: It's recommended to enter Play Mode for accurate GameView rendering before taking screenshots!");
            }

            Debug.Log($"[ScreenshotSettings] Start Recording {settings.resolutions.Length} resolutions to '{settings.saveFolder}' directory...");

            foreach (var res in settings.resolutions)
            {
                var controllerSettings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
                controllerSettings.SetRecordModeToSingleFrame(0);

                var imageRecorder = ScriptableObject.CreateInstance<ImageRecorderSettings>();
                imageRecorder.name = res.title;
                imageRecorder.Enabled = true;

                imageRecorder.OutputFormat = ImageRecorderSettings.ImageRecorderOutputFormat.PNG;
                imageRecorder.CaptureAlpha = false;

                // Make sure the path creates a valid relative or absolute string
                string directory = string.IsNullOrEmpty(settings.saveFolder) ? "Screenshots" : settings.saveFolder;
                directory += $"/{res.title}";
                
                // Use Take formatting directly
                imageRecorder.Take = settings.take;
                imageRecorder.OutputFile = Path.Combine(directory, $"{res.title}_{res.width}x{res.height}_Take{settings.take:000}");
                
                var inputSettings = new GameViewInputSettings();
                inputSettings.OutputWidth = res.width;
                inputSettings.OutputHeight = res.height;

                imageRecorder.imageInputSettings = inputSettings;
                
                controllerSettings.AddRecorderSettings(imageRecorder);

                var recorderController = new RecorderController(controllerSettings);
                recorderController.PrepareRecording();
                recorderController.StartRecording();
                
                // Wait for the recording to finish before capturing the next resolution
                while (recorderController.IsRecording())
                {
                    await UniTask.Yield();
                }
                
                // Optional delay to ensure the GameView resets smoothly between layout shifts
                await UniTask.Delay(100);
            }
            
            Debug.Log("[ScreenshotSettings] Finished capturing all screenshots.");
            
            // Increment the take automatically
            settings.take++;
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            
            if (this != null)
            {
                RefreshGallery(settings);
                Repaint();
            }
        }
    }
}
