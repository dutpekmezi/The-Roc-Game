using Game.Anim;
using Game.UI;
using GameLift.Audio;
using UnityEngine;
using UnityEngine.InputSystem;
using Utils.Pools;
using Utils.Popup;
using Utils.Signal;

namespace Game.Systems
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class Flipper : PlayerController
    {
        private const string FlipSoundName = "Flip";
        private const string PunchSoundName = "Punch";
        private static readonly int FlapTriggerHash = Animator.StringToHash("Flap");
        private static readonly int IsDeadHash = Animator.StringToHash("IsDead");
        private static readonly int BirdIdleStateHash = Animator.StringToHash("BirdIdle");

        [Header("Refs")]
        [SerializeField] private Animator animator;
        [SerializeField] private Rigidbody2D rb;
        [SerializeField] private PlayerSettings playerData;
        [SerializeField] private DoAnim startIdleAnim;
        [SerializeField] private Collider2D[] colliders;
        [SerializeField] private SpriteRenderer[] spriteRenderers;

        [Header("Death")]
        [SerializeField] private float deathDownwardImpulse = 8f;
        [SerializeField] private float deathAngularVelocity = 240f;

        private bool isAlive = true;
        private bool gameplayStarted;
        private bool deathFallActive;

        private bool hasCachedRigidbodyConstraints;
        private RigidbodyConstraints2D defaultRigidbodyConstraints;
        private IAudioService audioService;

        public void Initialize(IAudioService audioService)
        {
            this.audioService = audioService;
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResetRuntimeState();
        }

        private void LateUpdate()
        {
            if (deathFallActive)
            {
                SetSpriteRenderersEnabled(true);
            }
        }

        private void Update()
        {
            HandleScreenClickInput();
        }

        public void OnSpawn()
        {
            ResetRuntimeState();
        }

        public void OnDespawn()
        {
            deathFallActive = false;
            SetCollidersEnabled(true);
            SetSpriteRenderersEnabled(true);
        }

        private void ResetRuntimeState()
        {
            isAlive = true;
            gameplayStarted = false;
            deathFallActive = false;

            ResolveReferences();
            SetCollidersEnabled(true);
            SetSpriteRenderersEnabled(true);
            StopStartIdleAnimation();

            if (rb != null)
            {
                RestoreDefaultRigidbodyConstraints();
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }

            ResetAnimatorForAliveState();
        }

        public void PrepareForStart()
        {
            ResolveReferences();
            SetCollidersEnabled(true);
            SetSpriteRenderersEnabled(true);

            isAlive = true;
            gameplayStarted = false;
            deathFallActive = false;

            if (rb != null)
            {
                RestoreDefaultRigidbodyConstraints();
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }

            StopStartIdleAnimation();

            ResetAnimatorForAliveState();
        }

        public void BeginGameplay(bool flapOnStart = false)
        {
            StartGameplay(flapOnStart);
        }

        public void RestartGameplay(bool flapOnStart = false)
        {
            StartGameplay(flapOnStart);
        }

        public void StopGameplay()
        {
            gameplayStarted = false;
        }

        private void StartGameplay(bool flapOnStart)
        {
            ResolveReferences();
            Pools.Instance?.CancelDelayedDespawn(gameObject);
            SetCollidersEnabled(true);

            isAlive = true;
            deathFallActive = false;
            gameplayStarted = true;

            StopStartIdleAnimation();

            ResetAnimatorForAliveState();

            if (rb != null)
            {
                RestoreDefaultRigidbodyConstraints();
                rb.bodyType = RigidbodyType2D.Dynamic;
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }

            if (flapOnStart)
            {
                OnClick();
            }
        }

        private void HandleScreenClickInput()
        {
            if (!isAlive || !gameplayStarted)
            {
                return;
            }

            if (WasScreenClickedThisFrame())
            {
                OnClick();
            }
        }

        private bool WasScreenClickedThisFrame()
        {
            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                return true;
            }

            var touchscreen = Touchscreen.current;
            if (touchscreen != null && touchscreen.primaryTouch.press.wasPressedThisFrame)
            {
                return true;
            }

            var pen = Pen.current;
            if (pen != null && pen.tip.wasPressedThisFrame)
            {
                return true;
            }

            return false;
        }

        public void Tick()
        {
        }

        public override void OnClick()
        {
            if (!isAlive || !gameplayStarted)
            {
                return;
            }

            Flap();
        }

        private void Flap()
        {
            if (rb == null || playerData == null)
            {
                return;
            }

            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            rb.AddForce(Vector2.up * playerData.flapForce, ForceMode2D.Impulse);

            if (animator != null)
            {
                animator.SetTrigger(FlapTriggerHash);
            }

            audioService?.Play(FlipSoundName);
        }

        private void Die()
        {
            if (!isAlive) return;

            isAlive = false;
            deathFallActive = true;
            Pools.Instance?.CancelDelayedDespawn(gameObject);
            SetSpriteRenderersEnabled(true);

            if (animator != null)
            {
                animator.SetBool(IsDeadHash, true);
            }

            gameplayStarted = false;

            audioService?.Play(PunchSoundName);
            SignalBus.Get<GameplayStoppedSignal>().Invoke();
            SetCollidersEnabled(false);

            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Dynamic;
                rb.constraints &= ~RigidbodyConstraints2D.FreezeRotation;
                rb.linearVelocity = Vector2.zero;
                rb.AddForce(Vector2.down * Mathf.Abs(deathDownwardImpulse), ForceMode2D.Impulse);
                rb.angularVelocity = GetRandomDeathAngularVelocity();
            }

            var popupService = PopupService.Instance;
            if (popupService != null && popupService.Get(GameOverPopup.PopupKey) == null)
            {
                popupService.Create(GameOverPopup.PopupKey);
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (!isAlive || !gameplayStarted) return;

            Die();
        }

        private void ResolveReferences()
        {
            if (rb == null)
            {
                rb = GetComponent<Rigidbody2D>();
            }

            CacheDefaultRigidbodyConstraints();

            ResolveColliders();
            ResolveSpriteRenderers();
        }

        private void StopStartIdleAnimation()
        {
            if (startIdleAnim == null)
            {
                return;
            }

            startIdleAnim.Stop();
            startIdleAnim.enabled = false;
        }

        private void CacheDefaultRigidbodyConstraints()
        {
            if (rb == null || hasCachedRigidbodyConstraints)
            {
                return;
            }

            defaultRigidbodyConstraints = rb.constraints;
            hasCachedRigidbodyConstraints = true;
        }

        private void RestoreDefaultRigidbodyConstraints()
        {
            if (rb != null && hasCachedRigidbodyConstraints)
            {
                rb.constraints = defaultRigidbodyConstraints;
            }
        }

        private float GetRandomDeathAngularVelocity()
        {
            float spinSpeed = Mathf.Abs(deathAngularVelocity);
            if (spinSpeed <= 0f)
            {
                return 0f;
            }

            float direction = UnityEngine.Random.value < 0.5f ? -1f : 1f;
            return spinSpeed * direction;
        }

        private void ResetAnimatorForAliveState()
        {
            if (animator == null)
            {
                return;
            }

            animator.ResetTrigger(FlapTriggerHash);
            animator.SetBool(IsDeadHash, false);

            if (animator.HasState(0, BirdIdleStateHash))
            {
                animator.Play(BirdIdleStateHash, 0, 0f);
                animator.Update(0f);
            }
        }

        private void ResolveColliders()
        {
            if (colliders == null || colliders.Length == 0)
            {
                colliders = GetComponentsInChildren<Collider2D>(true);
            }
        }

        private void SetCollidersEnabled(bool enabled)
        {
            ResolveColliders();

            if (colliders == null)
            {
                return;
            }

            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null)
                {
                    colliders[i].enabled = enabled;
                }
            }
        }

        private void ResolveSpriteRenderers()
        {
            if (spriteRenderers == null || spriteRenderers.Length == 0)
            {
                spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
            }
        }

        private void SetSpriteRenderersEnabled(bool enabled)
        {
            ResolveSpriteRenderers();

            if (spriteRenderers == null)
            {
                return;
            }

            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                if (spriteRenderers[i] != null)
                {
                    spriteRenderers[i].enabled = enabled;
                }
            }
        }
    }
}
