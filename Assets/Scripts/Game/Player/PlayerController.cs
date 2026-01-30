using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Systems
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private Animator animator;
        [SerializeField] private Rigidbody2D rb;
        [SerializeField] private PlayerSettings playerData;

        [Header("Input")]
        [SerializeField] private InputActionReference flapAction;

        private bool isAlive = true;

        private bool flapBuffered;

        private void OnEnable()
        {
            if (flapAction != null)
            {
                flapAction.action.Enable();
                flapAction.action.performed += OnFlapPerformed;
            }
        }

        private void OnDisable()
        {
            if (flapAction != null)
            {
                flapAction.action.performed -= OnFlapPerformed;
                flapAction.action.Disable();
            }
        }

        private void OnFlapPerformed(InputAction.CallbackContext ctx)
        {
            if (!isAlive) return;

            flapBuffered = true;
        }

        public void Tick()
        {
            if (!isAlive) return;

            if (flapBuffered)
            {
                flapBuffered = false;
                Flap();
            }
        }

        private void Flap()
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            rb.AddForce(Vector2.up * playerData.flapForce, ForceMode2D.Impulse);

            animator.SetTrigger("Flap");
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
        }
    }
}
