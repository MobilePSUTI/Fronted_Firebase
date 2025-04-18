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
        // �������� ������� ������� ������
        float spawnWidth = spawnArea.rect.width;
        float spawnHeight = spawnArea.rect.height;

        // ��� �������: ���� (25% ������), �����, ����� (75% ������)
        float[] spawnPositionsX = {
            -spawnWidth * 0.25f,
            0f,
            spawnWidth * 0.25f
        };

        // ��������� ����� �������
        float randomX = spawnPositionsX[Random.Range(0, spawnPositionsX.Length)];

        Vector2 spawnPos = new Vector2(
            randomX,
            spawnHeight * 0.5f // ����� � ������� �������� �������
        );

        GameObject enemy = Instantiate(enemyPrefab, spawnArea.transform);
        RectTransform rt = enemy.GetComponent<RectTransform>();
        rt.anchoredPosition = spawnPos;
        rt.localScale = Vector3.one;
    }
}