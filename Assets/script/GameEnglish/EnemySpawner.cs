using UnityEngine;
using UnityEngine.UI;

public class EnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab;
    private float spawnTimer;
    public float spawnInterval = 3f;
    public RectTransform spawnArea;

    private void Update()
    {

        spawnTimer += Time.deltaTime;
        if (spawnTimer >= spawnInterval)
        {
            SpawnEnemy();
            spawnTimer = 0f;
            spawnInterval = Mathf.Max(1f, spawnInterval * 0.98f);
        }
    }

    private void SpawnEnemy()
    {
        // Координаты для спавна в верхней части экрана
        Vector2 spawnPos = new Vector2(
            Random.Range(-300f, 300f),
            400f // Отступ сверху
        );
    
        GameObject enemy = Instantiate(enemyPrefab, spawnArea.transform);
        RectTransform rt = enemy.GetComponent<RectTransform>();
        rt.anchoredPosition = spawnPos;
        rt.localScale = Vector3.one; // Важно!
    }
}