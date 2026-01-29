using UnityEngine;

public class ObstacleMover : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float destroyX = -12f;

    [SerializeField] private GameObject top;
    [SerializeField] private GameObject body;
    [SerializeField] private GameObject bottom;

    private Vector3 initialTopLocalPosition;
    private Vector3 initialBodyLocalScale;
    private Vector3 initialBottomLocalPosition;

    private void Awake()
    {
        if (top != null)
        {
            initialTopLocalPosition = top.transform.localPosition;
        }

        if (body != null)
        {
            initialBodyLocalScale = body.transform.localScale;
        }

        if (bottom != null)
        {
            initialBottomLocalPosition = bottom.transform.localPosition;
        }
    }

    private void Update()
    {
        transform.position += Vector3.left * moveSpeed * Time.deltaTime;

        if (transform.position.x <= destroyX)
        {
            Destroy(gameObject);
        }
    }
}
