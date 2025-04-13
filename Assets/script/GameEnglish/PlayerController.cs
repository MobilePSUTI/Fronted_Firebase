// PlayerController.cs
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance;
    private EnemyController currentTarget;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        FindNearestEnemy();
    }

    private void FindNearestEnemy()
    {
        EnemyController[] enemies = FindObjectsByType<EnemyController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        float minDistance = float.MaxValue;
        currentTarget = null;

        foreach (var enemy in enemies)
        {
            float dist = Vector3.Distance(transform.position, enemy.transform.position);
            if (dist < minDistance)
            {
                minDistance = dist;
                currentTarget = enemy;
            }
        }
    }

    public void OnKeyboardKeyPressed(char key)
    {
        if (currentTarget != null)
        {
            currentTarget.ProcessKeyPress(key);
        }
    }
}