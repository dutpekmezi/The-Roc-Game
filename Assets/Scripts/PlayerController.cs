using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private float flapForce = 5.5f;
    [SerializeField] private float maxUpAngle = 25f;
    [SerializeField] private float maxDownAngle = -75f;
    [SerializeField] private float rotationSpeed = 6f;

    private Rigidbody2D rb;
    private bool isAlive = true;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

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

        float targetAngle = Mathf.Lerp(maxDownAngle, maxUpAngle, Mathf.InverseLerp(-6f, 6f, rb.velocity.y));
        float angle = Mathf.LerpAngle(transform.eulerAngles.z, targetAngle, rotationSpeed * Time.fixedDeltaTime);
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void Flap()
    {
        rb.velocity = new Vector2(rb.velocity.x, 0f);
        rb.AddForce(Vector2.up * flapForce, ForceMode2D.Impulse);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        isAlive = false;
    }
}
