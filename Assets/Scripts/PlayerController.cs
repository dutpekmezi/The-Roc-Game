using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private float flapForce = 5.5f;
    [SerializeField] private float maxUpAngle = 25f;
    [SerializeField] private float maxDownAngle = -75f;
    [SerializeField] private float rotationSpeed = 6f;

    [SerializeField] private Rigidbody2D rb;
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

        float targetAngle = Mathf.Lerp(maxDownAngle, maxUpAngle, Mathf.InverseLerp(-6f, 6f, rb.linearVelocity.y));
    }

    private void Flap()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        rb.AddForce(Vector2.up * flapForce, ForceMode2D.Impulse);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        isAlive = false;
    }
}
