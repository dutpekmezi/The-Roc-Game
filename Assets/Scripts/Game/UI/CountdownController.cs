using System.Threading;
using System.Threading.Tasks;
using GameLift.Audio;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    public class CountdownController : MonoBehaviour
    {
        [SerializeField] private TMP_Text countdownText;
        [SerializeField, Min(0)] private int startValue = 3;
        [SerializeField] private string countdownSoundName = "Countdown_Sound";
        [SerializeField] private string countdownOverSoundName = "Countdown_Over_Sound";

        private int playVersion;
        private int lastCountdownSoundValue = int.MinValue;
        private GameObject runtimeCanvasRoot;
        private IAudioService audioService;

        private void Awake()
        {
            EnsureText();
            Hide();
        }

        public void SetAudioService(IAudioService audioService)
        {
            this.audioService = audioService;
        }

        public async Task PlayAsync(CancellationToken cancellationToken = default)
        {
            int version = ++playVersion;
            EnsureText();
            SetVisible(true);
            lastCountdownSoundValue = int.MinValue;

            int value = Mathf.Max(0, startValue);
            for (int current = value; current >= 0; current--)
            {
                if (cancellationToken.IsCancellationRequested || version != playVersion)
                {
                    return;
                }

                SetText(current);
                PlayCountdownSound(current);

                if (current == 0)
                {
                    return;
                }

                float nextTickTime = Time.realtimeSinceStartup + 1f;
                while (Time.realtimeSinceStartup < nextTickTime)
                {
                    if (cancellationToken.IsCancellationRequested || version != playVersion)
                    {
                        return;
                    }

                    await Task.Yield();
                }
            }
        }

        public void Hide()
        {
            playVersion++;
            SetVisible(false);
        }

        public void PlayCountdownOverSound()
        {
            if (!string.IsNullOrEmpty(countdownOverSoundName))
            {
                audioService?.Play(countdownOverSoundName);
            }
        }

        private void PlayCountdownSound(int current)
        {
            if (current > 0 && current != lastCountdownSoundValue && !string.IsNullOrEmpty(countdownSoundName))
            {
                lastCountdownSoundValue = current;
                audioService?.Play(countdownSoundName);
            }
        }

        private void SetText(int value)
        {
            if (countdownText != null)
            {
                countdownText.text = value.ToString();
            }
        }

        private void SetVisible(bool visible)
        {
            if (runtimeCanvasRoot != null)
            {
                runtimeCanvasRoot.SetActive(visible);
                return;
            }

            if (countdownText != null)
            {
                countdownText.gameObject.SetActive(visible);
            }
        }

        private void EnsureText()
        {
            if (countdownText != null)
            {
                return;
            }

            countdownText = GetComponentInChildren<TMP_Text>(true);
            if (countdownText != null)
            {
                return;
            }

            var canvasObject = new GameObject("CountdownCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            canvasObject.transform.SetParent(transform, false);
            runtimeCanvasRoot = canvasObject;

            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;

            var textObject = new GameObject("CountdownText", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(canvasObject.transform, false);

            var rectTransform = textObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = new Vector2(320f, 220f);

            var text = textObject.GetComponent<TextMeshProUGUI>();
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.fontSize = 150f;
            text.raycastTarget = false;
            countdownText = text;
        }
    }
}
