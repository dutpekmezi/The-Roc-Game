using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Systems
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private Animator animator;

        [SerializeField] private Rigidbody2D rb;

        [SerializeField] private PlayerData playerData;

        private bool isAlive = true;

        public void Tick()
        {
            if (!isAlive)
            {
                return;
            }

            if (IsFlapPressed())
            {
                Flap();
            }
        }

        private static bool IsFlapPressed()
        {
            if (Keyboard.current?.spaceKey.wasPressedThisFrame == true)
            {
                return true;
            }

            if (Mouse.current?.leftButton.wasPressedThisFrame == true)
            {
                return true;
            }

            return Touchscreen.current?.primaryTouch.press.wasPressedThisFrame == true;
        }

        private void Flap()
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            rb.AddForce(Vector2.up * playerData.flapForce, ForceMode2D.Impulse);

            animator.SetTrigger("Flap");
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            //isAlive = false;
        }
    }
}
