using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class ObstacleSpawner : MonoBehaviour
{
    [SerializeField] private List<ObstacleMover> obstaclePrefabs;
    [SerializeField] private float spawnInterval = 1.75f;
    [SerializeField] private float spawnX = 9f;
    [SerializeField] private float minY = -1.5f;
    [SerializeField] private float maxY = 2.5f;
    [SerializeField] private float minGap = 2.5f;
    [SerializeField] private float maxGap = 4f;

    private float timer;

    private void OnEnable()
    {
        timer = spawnInterval;
    }

    private void Update()
    {
        if (obstaclePrefabs == null)
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

        var targetSpawnPosYBottom = spawnPosition.y;
        var targetSpawnPosYTop = ((Vector2)spawnPosition + Vector2.up * (gap)).y;

        if (targetSpawnPosYTop > maxY)
        {
            var diff = targetSpawnPosYTop - maxY;
            targetSpawnPosYTop -= diff;
            targetSpawnPosYBottom -= diff;
        }
        else if (targetSpawnPosYBottom < minY)
        {
            var diff = targetSpawnPosYBottom - minY;
            targetSpawnPosYBottom -= diff;

            targetSpawnPosYTop -= diff;
        }

        CreateObstacle(new Vector2(spawnX, targetSpawnPosYBottom), false);
        CreateObstacle(new Vector2(spawnX, targetSpawnPosYTop), true);
    }

    private void CreateObstacle(Vector3 position, bool flipVertically)
    {
        var randomIndex = Random.Range(0, obstaclePrefabs.Count);

        ObstacleMover randomObstaclePrefab = obstaclePrefabs[randomIndex];

        var obstacleInstance = Instantiate(randomObstaclePrefab, position, Quaternion.identity);

        if (flipVertically)
        {
            obstacleInstance.transform.rotation = Quaternion.Euler(0f, 0f, 180f);
        }
        else
        {
            obstacleInstance.transform.rotation = Quaternion.identity;
        }
    }
}
