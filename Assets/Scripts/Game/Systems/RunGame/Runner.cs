using Game.UI;
using GameLift.Audio;
using UnityEngine;
using UnityEngine.InputSystem;
using Utils.Popup;
using Utils.Signal;

namespace Game.Systems
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class Runner : PlayerController
    {
        private const string JumpSoundName = "Flip";
        private const string HitSoundName = "Punch";
        private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
        private static readonly int IsDeadHash = Animator.StringToHash("IsDead");

        [Header("Refs")]
        [SerializeField] private Animator animator;
        [SerializeField] private Rigidbody2D rb;
        [SerializeField] private Collider2D[] colliders;
        [SerializeField] private SpriteRenderer[] spriteRenderers;

        [Header("Jump")]
        [SerializeField, Min(0f)] private float jumpForce = 9f;

        [Header("Death")]
        [SerializeField, Min(0f)] private float deathUpwardImpulse = 2.5f;

        private bool isAlive = true;
        private bool gameplayStarted;
        private bool isGrounded;
        private IAudioService audioService;
        private PopupService popupService;

        public void Initialize(
            IAudioService audioService,
            PopupService popupService)
        {
            this.audioService = audioService;
            this.popupService = popupService;
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();
        }

        private void Update()
        {
            HandleScreenClickInput();
        }

        public void PrepareForStart(Vector3 localStartPosition)
        {
            ResolveReferences();

            transform.localPosition = localStartPosition;
            isAlive = true;
            gameplayStarted = false;
            isGrounded = false;
            UpdateAnimatorState();

            SetCollidersEnabled(true);
            SetSpriteRenderersEnabled(true);

            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
                rb.rotation = 0f;
            }
        }

        public void BeginGameplay(bool jumpOnStart = false)
        {
            ResolveReferences();

            isAlive = true;
            gameplayStarted = true;
            isGrounded = false;
            UpdateAnimatorState();

            SetCollidersEnabled(true);
            SetSpriteRenderersEnabled(true);

            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Dynamic;
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
                rb.rotation = 0f;
            }

            if (jumpOnStart)
            {
                SetGrounded(true);
                OnClick();
            }
        }

        public void RestartGameplay(bool jumpOnStart = false)
        {
            BeginGameplay(jumpOnStart);
        }

        public void StopGameplay()
        {
            gameplayStarted = false;
        }

        public void Tick()
        {
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

        public override void OnClick()
        {
            if (!isAlive || !gameplayStarted)
            {
                return;
            }

            TryJump();
        }

        private void TryJump()
        {
            if (rb == null || !isGrounded)
            {
                return;
            }

            SetGrounded(false);
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            audioService?.Play(JumpSoundName);
        }

        private void Die()
        {
            if (!isAlive)
            {
                return;
            }

            isAlive = false;
            gameplayStarted = false;
            isGrounded = false;
            UpdateAnimatorState();

            SignalBus.Get<GameplayStoppedSignal>().Invoke();
            SetCollidersEnabled(false);

            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Dynamic;
                rb.linearVelocity = Vector2.zero;

                if (deathUpwardImpulse > 0f)
                {
                    rb.AddForce(Vector2.up * deathUpwardImpulse, ForceMode2D.Impulse);
                }
            }

            audioService?.Play(HitSoundName);
            ShowGameOverPopup();
        }

        private void ShowGameOverPopup()
        {
            if (popupService != null && popupService.Get(GameOverPopup.PopupKey) == null)
            {
                popupService.Create(GameOverPopup.PopupKey);
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            HandleCollision(collision);
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            HandleCollision(collision);
        }

        private void OnCollisionExit2D(Collision2D collision)
        {
            if (!isAlive || collision == null || IsObstacleCollision(collision))
            {
                return;
            }

            SetGrounded(false);
        }

        private void HandleCollision(Collision2D collision)
        {
            if (!isAlive || collision == null)
            {
                return;
            }

            if (IsObstacleCollision(collision))
            {
                if (gameplayStarted)
                {
                    Die();
                }

                return;
            }

            SetGrounded(true);
        }

        private bool IsObstacleCollision(Collision2D collision)
        {
            return IsObstacleCollider(collision?.collider) ||
                   IsObstacleCollider(collision?.otherCollider);
        }

        private static bool IsObstacleCollider(Collider2D contactCollider)
        {
            return contactCollider != null && contactCollider.CompareTag("Obstacle");
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

        private void ResolveReferences()
        {
            if (rb == null)
            {
                rb = GetComponent<Rigidbody2D>();
            }

            if (rb != null)
            {
                rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            }

            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>(true);
            }

            if (colliders == null || colliders.Length == 0)
            {
                colliders = GetComponentsInChildren<Collider2D>(true);
            }

            EnsureSolidColliders();

            if (spriteRenderers == null || spriteRenderers.Length == 0)
            {
                spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
            }
        }

        private void EnsureSolidColliders()
        {
            if (colliders == null)
            {
                return;
            }

            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null)
                {
                    colliders[i].isTrigger = false;
                }
            }
        }

        private void SetCollidersEnabled(bool enabled)
        {
            if (colliders == null)
            {
                return;
            }

            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null)
                {
                    colliders[i].isTrigger = false;
                    colliders[i].enabled = enabled;
                }
            }
        }

        private void SetSpriteRenderersEnabled(bool enabled)
        {
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

        private void SetGrounded(bool grounded)
        {
            if (isGrounded == grounded)
            {
                return;
            }

            isGrounded = grounded;
            UpdateAnimatorState();
        }

        private void UpdateAnimatorState()
        {
            if (animator == null)
            {
                return;
            }

            animator.SetBool(IsGroundedHash, isAlive && isGrounded);
            animator.SetBool(IsDeadHash, !isAlive);
        }
    }
}
