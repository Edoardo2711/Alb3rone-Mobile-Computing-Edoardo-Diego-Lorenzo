using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MobSpawner : MonoBehaviour
{
    [Header("Prefab")]
    public List<GameObject> mobPrefabs;

    [Header("Riferimenti Area")]
    // Il PolygonCollider2D interno che delimita dove nascono i mob
    public PolygonCollider2D spawnAreaCollider;
    [Header("Limite Mob")]
    public int maxMobsInScene = 10;
    [Header("Impostazioni Spawn")]
    public float spawnInterval = 3f;
    public int maxAttempts = 10;
    public float overlapCheckRadius = 0.2f;

    private bool playerInZone = false;
    private LayerMask obstacleLayer;

    void Start()
    {
        obstacleLayer = LayerMask.GetMask("Ostacoli");
        StartCoroutine(SpawnLoop());
    }

    Vector2 GetRandomPointInPolygon()
    {
        Bounds bounds = spawnAreaCollider.bounds;

        for (int i = 0; i < maxAttempts; i++)
        {
            Vector2 candidate = new Vector2(
                Random.Range(bounds.min.x, bounds.max.x),
                Random.Range(bounds.min.y, bounds.max.y)
            );

            if (spawnAreaCollider.OverlapPoint(candidate))
                return candidate;
        }

        return Vector2.positiveInfinity;
    }

    void OnTriggerEnter2D(Collider2D other) 
    {
        Debug.Log($"[Spawner] Trigger colpito da: {other.gameObject.name} | Tag: {other.tag}");
        if (other.CompareTag("Player")) playerInZone = true;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player")) playerInZone = false;
    }

    bool IsPositionFree(Vector2 point)
    {
        return Physics2D.OverlapCircle(point, overlapCheckRadius, obstacleLayer) == null;
    }

    IEnumerator SpawnLoop()
{
    while (true)
    {
        yield return new WaitForSeconds(spawnInterval);

        if (!playerInZone || mobPrefabs.Count == 0)
        {
            Debug.Log($"[Spawner] Skip — playerInZone: {playerInZone}, prefab count: {mobPrefabs.Count}");
            continue;
        }

        // Conta i mob attualmente in scena tramite tag
        int currentMobs = GameObject.FindGameObjectsWithTag("Mob").Length;
        if (currentMobs >= maxMobsInScene)
        {
            Debug.Log($"[Spawner] Limite mob raggiunto ({currentMobs}/{maxMobsInScene})");
            continue;
        }

        Vector2 spawnPoint = GetRandomPointInPolygon();
        if (spawnPoint == Vector2.positiveInfinity) continue;

        if (IsPositionFree(spawnPoint))
        {
            GameObject prefab = mobPrefabs[Random.Range(0, mobPrefabs.Count)];
            GameObject mob = Instantiate(prefab, spawnPoint, Quaternion.identity);
            mob.tag = "Mob"; // assicura che il tag sia impostato
            Debug.Log($"[Spawner] Mob spawnato in {spawnPoint} ({currentMobs + 1}/{maxMobsInScene})");
        }
    }
}
}