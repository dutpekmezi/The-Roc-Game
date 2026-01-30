using System;
using DG.Tweening;
using UnityEngine;

namespace Game.Anim
{
    [DisallowMultipleComponent]
    public class DoAnim : MonoBehaviour
    {
        [Header("Sequence")]
        [SerializeField] private Transform target;
        [SerializeField] private bool playOnEnable = true;
        [SerializeField] private bool rebuildOnEnable = true;
        [SerializeField] private int loops = -1;
        [SerializeField] private LoopType loopType = LoopType.Yoyo;
        [SerializeField] private UpdateType updateType = UpdateType.Normal;
        [SerializeField] private bool useUnscaledTime;

        [Header("Movement")]
        [SerializeField] private MoveSettings move = new MoveSettings { enabled = false };
        [SerializeField] private RotateSettings rotate = new RotateSettings();
        [SerializeField] private ScaleSettings scale = new ScaleSettings();
        [SerializeField] private AnchorSettings anchor = new AnchorSettings();

        [Header("Punch")]
        [SerializeField] private PunchSettings punchPosition = new PunchSettings { punchType = PunchSettings.PunchType.Position };
        [SerializeField] private PunchSettings punchRotation = new PunchSettings { punchType = PunchSettings.PunchType.Rotation };
        [SerializeField] private PunchSettings punchScale = new PunchSettings { punchType = PunchSettings.PunchType.Scale };

        [Header("Shake")]
        [SerializeField] private ShakeSettings shakePosition = new ShakeSettings { shakeType = ShakeSettings.ShakeType.Position };
        [SerializeField] private ShakeSettings shakeRotation = new ShakeSettings { shakeType = ShakeSettings.ShakeType.Rotation };
        [SerializeField] private ShakeSettings shakeScale = new ShakeSettings { shakeType = ShakeSettings.ShakeType.Scale };
        [SerializeField] private AnchorShakeSettings shakeAnchor = new AnchorShakeSettings();

        [Header("UI")]
        [SerializeField] private FadeSettings fade = new FadeSettings();

        private Sequence _sequence;

        private void Start()
        {
            if (playOnEnable)
            {
                Play();
            }
            else if (rebuildOnEnable)
            {
                BuildSequence();
            }
        }

        private void OnDisable()
        {
            KillSequence();
        }

        [ContextMenu("Play")]
        public void Play()
        {
            if (_sequence == null || rebuildOnEnable)
            {
                BuildSequence();
            }

            _sequence?.Restart();
        }

        [ContextMenu("Pause")]
        public void Pause()
        {
            _sequence?.Pause();
        }

        [ContextMenu("Stop")]
        public void Stop()
        {
            if (_sequence == null) return;

            _sequence.Rewind();
            _sequence.Pause();
        }

        [ContextMenu("Rebuild Sequence")]
        public void BuildSequence()
        {
            KillSequence();

            Transform resolvedTarget = target != null ? target : transform;
            _sequence = DOTween.Sequence();
            _sequence.SetAutoKill(false);
            _sequence.SetUpdate(updateType, useUnscaledTime);
            _sequence.SetLoops(NormalizeLoopCount(loops), loopType);

            bool hasTweens = false;

            hasTweens |= move.TryAdd(_sequence, resolvedTarget);
            hasTweens |= rotate.TryAdd(_sequence, resolvedTarget);
            hasTweens |= scale.TryAdd(_sequence, resolvedTarget);
            hasTweens |= anchor.TryAdd(_sequence, resolvedTarget);
            hasTweens |= punchPosition.TryAdd(_sequence, resolvedTarget);
            hasTweens |= punchRotation.TryAdd(_sequence, resolvedTarget);
            hasTweens |= punchScale.TryAdd(_sequence, resolvedTarget);
            hasTweens |= shakePosition.TryAdd(_sequence, resolvedTarget);
            hasTweens |= shakeRotation.TryAdd(_sequence, resolvedTarget);
            hasTweens |= shakeScale.TryAdd(_sequence, resolvedTarget);
            hasTweens |= shakeAnchor.TryAdd(_sequence, resolvedTarget);
            hasTweens |= fade.TryAdd(_sequence, resolvedTarget);

            if (!hasTweens)
            {
                KillSequence();
                return;
            }

            _sequence.Pause();
        }

        private static int NormalizeLoopCount(int loopCount)
        {
            if (loopCount < -1) return -1;
            if (loopCount == 0) return 1;
            return loopCount;
        }

        private void KillSequence()
        {
            if (_sequence == null) return;

            _sequence.Kill();
            _sequence = null;
        }

        [Serializable]
        public abstract class TweenSettingsBase
        {
            public bool enabled;
            public float duration = 0.35f;
            public float delay;
            public Ease ease = Ease.OutQuad;
            public SequencePlacement placement = SequencePlacement.Join;

            protected Tween ConfigureTween(Tween tween)
            {
                if (tween == null) return null;

                tween.SetEase(ease);

                return tween;
            }

            protected void AddToSequence(Sequence sequence, Tween tween)
            {
                if (sequence == null || tween == null) return;

                Sequence tweenSequence = DOTween.Sequence();
                if (delay > 0f) tweenSequence.AppendInterval(delay);
                tweenSequence.Append(tween);

                switch (placement)
                {
                    case SequencePlacement.Append:
                        sequence.Append(tweenSequence);
                        break;
                    case SequencePlacement.InsertAtStart:
                        sequence.Insert(0f, tweenSequence);
                        break;
                    default:
                        sequence.Join(tweenSequence);
                        break;
                }
            }
        }

        public enum SequencePlacement
        {
            Join,
            Append,
            InsertAtStart
        }

        [Serializable]
        public class MoveSettings : TweenSettingsBase
        {
            public Vector3 to = new Vector3(0f, 1f, 0f);
            public bool useLocal = true;
            public bool relative = true;
            public bool snapping;

            public bool TryAdd(Sequence sequence, Transform target)
            {
                if (!enabled || sequence == null || target == null) return false;

                Tween tween = useLocal
                    ? target.DOLocalMove(to, duration, snapping)
                    : target.DOMove(to, duration, snapping);

                if (relative) tween.SetRelative();

                ConfigureTween(tween);
                AddToSequence(sequence, tween);
                return true;
            }
        }

        [Serializable]
        public class RotateSettings : TweenSettingsBase
        {
            public Vector3 euler = new Vector3(0f, 0f, 30f);
            public bool useLocal = true;
            public bool relative = true;
            public RotateMode rotateMode = RotateMode.FastBeyond360;

            public bool TryAdd(Sequence sequence, Transform target)
            {
                if (!enabled || sequence == null || target == null) return false;

                Tween tween = useLocal
                    ? target.DOLocalRotate(euler, duration, rotateMode)
                    : target.DORotate(euler, duration, rotateMode);

                if (relative) tween.SetRelative();

                ConfigureTween(tween);
                AddToSequence(sequence, tween);
                return true;
            }
        }

        [Serializable]
        public class ScaleSettings : TweenSettingsBase
        {
            public Vector3 targetScale = Vector3.one * 1.1f;
            public bool relative = false;

            public bool TryAdd(Sequence sequence, Transform target)
            {
                if (!enabled || sequence == null || target == null) return false;

                Tween tween = target.DOScale(targetScale, duration);
                if (relative) tween.SetRelative();

                ConfigureTween(tween);
                AddToSequence(sequence, tween);
                return true;
            }
        }

        [Serializable]
        public class AnchorSettings : TweenSettingsBase
        {
            public RectTransform rectTransform;
            public Vector2 anchor = new Vector2(0f, 75f);
            public bool relative = true;

            public bool TryAdd(Sequence sequence, Transform defaultTarget)
            {
                if (!enabled || sequence == null) return false;

                RectTransform resolved = rectTransform != null ? rectTransform : defaultTarget as RectTransform;
                if (resolved == null) return false;

                Tween tween = resolved.DOAnchorPos(anchor, duration);
                if (relative) tween.SetRelative();

                ConfigureTween(tween);
                AddToSequence(sequence, tween);
                return true;
            }
        }

        [Serializable]
        public class PunchSettings : TweenSettingsBase
        {
            public enum PunchType
            {
                Position,
                Rotation,
                Scale
            }

            public PunchType punchType = PunchType.Position;
            public Vector3 punch = new Vector3(0f, 30f, 0f);
            public int vibrato = 10;
            public float elasticity = 1f;
            public bool snapping;

            public bool TryAdd(Sequence sequence, Transform target)
            {
                if (!enabled || sequence == null || target == null) return false;

                Tween tween = punchType switch
                {
                    PunchType.Rotation => target.DOPunchRotation(punch, duration, vibrato, elasticity),
                    PunchType.Scale => target.DOPunchScale(punch, duration, vibrato, elasticity),
                    _ => target.DOPunchPosition(punch, duration, vibrato, elasticity, snapping)
                };

                ConfigureTween(tween);
                AddToSequence(sequence, tween);
                return true;
            }
        }

        [Serializable]
        public class ShakeSettings : TweenSettingsBase
        {
            public enum ShakeType
            {
                Position,
                Rotation,
                Scale
            }

            public ShakeType shakeType = ShakeType.Position;
            public Vector3 strength = new Vector3(15f, 0f, 0f);
            public int vibrato = 10;
            public float randomness = 90f;
            public bool fadeOut = true;
            public bool snapping;

            public bool TryAdd(Sequence sequence, Transform target)
            {
                if (!enabled || sequence == null || target == null) return false;

                Tween tween = shakeType switch
                {
                    ShakeType.Rotation => target.DOShakeRotation(duration, strength, vibrato, randomness, fadeOut),
                    ShakeType.Scale => target.DOShakeScale(duration, strength, vibrato, randomness, fadeOut),
                    _ => target.DOShakePosition(duration, strength, vibrato, randomness, fadeOut, snapping)
                };

                ConfigureTween(tween);
                AddToSequence(sequence, tween);
                return true;
            }
        }

        [Serializable]
        public class AnchorShakeSettings : TweenSettingsBase
        {
            public RectTransform rectTransform;
            public Vector2 strength = new Vector2(10f, 10f);
            public int vibrato = 10;
            public float randomness = 90f;
            public bool snapping;
            public bool fadeOut = true;

            public bool TryAdd(Sequence sequence, Transform defaultTarget)
            {
                if (!enabled || sequence == null) return false;

                RectTransform resolved = rectTransform != null ? rectTransform : defaultTarget as RectTransform;
                if (resolved == null) return false;

                Tween tween = resolved.DOShakeAnchorPos(duration, strength, vibrato, randomness, snapping, fadeOut);
                ConfigureTween(tween);
                AddToSequence(sequence, tween);
                return true;
            }
        }

        [Serializable]
        public class FadeSettings : TweenSettingsBase
        {
            public CanvasGroup canvasGroup;
            [Range(0f, 1f)] public float alpha = 1f;

            public bool TryAdd(Sequence sequence, Transform defaultTarget)
            {
                if (!enabled || sequence == null) return false;

                CanvasGroup resolved = canvasGroup != null ? canvasGroup : defaultTarget.GetComponent<CanvasGroup>();
                if (resolved == null) return false;

                Tween tween = resolved.DOFade(alpha, duration);
                ConfigureTween(tween);
                AddToSequence(sequence, tween);
                return true;
            }
        }
    }
}
