using UnityEngine;
using UnityEngine.UI;

public class EnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab;
    public float spawnInterval = 3f;
    public RectTransform spawnArea;

    private void Update()
    {
        if (Time.timeSinceLevelLoad % spawnInterval < Time.deltaTime)
        {
            SpawnEnemy();
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