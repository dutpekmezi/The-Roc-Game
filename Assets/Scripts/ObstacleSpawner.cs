using UnityEngine;

public class ObstacleSpawner : MonoBehaviour
{
    [SerializeField] private GameObject obstaclePrefab;
    [SerializeField] private float spawnInterval = 1.75f;
    [SerializeField] private float spawnX = 9f;
    [SerializeField] private float minY = -1.5f;
    [SerializeField] private float maxY = 2.5f;
    [SerializeField] private float minGap = 2.5f;
    [SerializeField] private float maxGap = 4f;
    [SerializeField] private float minBodyScaleY = 0.6f;
    [SerializeField] private float maxBodyScaleY = 2.5f;

    private float timer;

    private void OnEnable()
    {
        timer = spawnInterval;
    }

    private void Update()
    {
        if (obstaclePrefab == null)
        {
            return;
        }

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            SpawnObstacle();
            timer = spawnInterval;
        }
    }

    private void SpawnObstacle()
    {
        float spawnY = Random.Range(minY, maxY);
        Vector3 spawnPosition = new Vector3(spawnX, spawnY, 0f);
        float gap = Random.Range(minGap, maxGap);

        CreateObstacle(spawnPosition + Vector3.up * (gap * 0.5f), true);
        CreateObstacle(spawnPosition + Vector3.down * (gap * 0.5f), false);
    }

    private void CreateObstacle(Vector3 position, bool flipVertically)
    {
        GameObject obstacleInstance = Instantiate(obstaclePrefab, position, Quaternion.identity);
        if (obstacleInstance.TryGetComponent(out ObstacleMover obstacleMover))
        {
            float bodyScaleY = Random.Range(minBodyScaleY, maxBodyScaleY);
            obstacleMover.Configure(bodyScaleY, flipVertically);
        }
    }
}
