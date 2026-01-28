using UnityEngine;

public class ObstacleSpawner : MonoBehaviour
{
    [SerializeField] private GameObject obstaclePrefab;
    [SerializeField] private float spawnInterval = 1.75f;
    [SerializeField] private float spawnX = 9f;
    [SerializeField] private float minY = -1.5f;
    [SerializeField] private float maxY = 2.5f;

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
        Instantiate(obstaclePrefab, spawnPosition, Quaternion.identity);
    }
}
