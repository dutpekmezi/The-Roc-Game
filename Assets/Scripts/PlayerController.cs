using UnityEngine;

namespace Game.Systems
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private Animator animator;

        [SerializeField] private Rigidbody2D rb;

        [SerializeField] private PlayerData playerData;

        private bool isAlive = true;

        private void Update()
        {
            if (!isAlive)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
            {
                Flap();
            }
        }

        private void FixedUpdate()
        {
            if (!isAlive)
            {
                return;
            }

            float targetAngle = Mathf.Lerp(playerData.maxDownAngle, playerData.maxUpAngle, Mathf.InverseLerp(-6f, 6f, rb.linearVelocity.y));
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