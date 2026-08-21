using UnityEngine;

public class BatSpawner : MonoBehaviour
{
    [Header("Bat")]
    [SerializeField] private GameObject batPrefab;

    [Header("Spawn Settings")]
    [SerializeField] private float spawnInterval = 3f;

    [SerializeField] private float spawnOffsetX = 2f;

    [Header("Height")]
    [SerializeField] private float minHeight = -2f;
    [SerializeField] private float maxHeight = 3f;

    private Camera mainCamera;
    private float timer;

    void Start()
    {
        mainCamera = Camera.main;

        timer = spawnInterval;
    }

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= spawnInterval)
        {
            SpawnBat();

            timer = 0f;
        }
    }

    void SpawnBat()
    {
        if (batPrefab == null)
        {
            Debug.LogError("Bat Prefab chưa được gán!");
            return;
        }

        if (mainCamera == null)
            return;

        // Lấy vị trí bên phải màn hình
        Vector3 rightEdge =
            mainCamera.ViewportToWorldPoint(
                new Vector3(1f, 0f, 0f)
            );

        // Random độ cao
        float randomY =
            Random.Range(minHeight, maxHeight);

        Vector3 spawnPosition = new Vector3(
            rightEdge.x + spawnOffsetX,
            randomY,
            0f
        );

        Instantiate(
            batPrefab,
            spawnPosition,
            Quaternion.identity
        );
    }
}