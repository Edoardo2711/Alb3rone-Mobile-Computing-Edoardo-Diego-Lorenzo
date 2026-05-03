using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MobSpawner : MonoBehaviour
{
    [Header("Prefab")]
    public List<GameObject> mobPrefabs;

    [Header("Riferimenti Area")]
    public PolygonCollider2D spawnAreaCollider;

    [Header("Limite Mob")]
    public int maxMobsInScene = 10;

    [Header("Impostazioni Spawn")]
    public float spawnInterval = 3f;
    public int maxAttempts = 10;
    public float overlapCheckRadius = 0.2f;

    [Header("Rilevamento Player")]
    public bool autoDetectPlayer = true;
    public float playerDetectionRange = 30f;

    private bool playerInZone = false;
    private LayerMask obstacleLayer;
    private Transform playerTransform;
    private readonly List<GameObject> spawnedMobs = new List<GameObject>();

    void Start()
    {
        int idx = LayerMask.NameToLayer("Ostacoli");
        if (idx < 0) idx = LayerMask.NameToLayer("ostacoli");
        obstacleLayer = (idx >= 0) ? (1 << idx) : 0;

        FindPlayerIfNeeded();

        if (mobPrefabs == null || mobPrefabs.Count == 0)
            Debug.LogWarning("[Spawner] mobPrefabs vuoto: nessun mob verrà spawnato.");
        if (spawnAreaCollider == null)
            Debug.LogWarning("[Spawner] spawnAreaCollider non assegnato!");

        StartCoroutine(SpawnLoop());
    }

    void FindPlayerIfNeeded()
    {
        if (playerTransform != null) return;
        try
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) { playerTransform = p.transform; return; }
        }
        catch (UnityException) { }
        GameObject byName = GameObject.Find("Player");
        if (byName != null) playerTransform = byName.transform;
    }

    Vector2 GetRandomPointInPolygon()
    {
        if (spawnAreaCollider == null) return Vector2.positiveInfinity;
        Bounds bounds = spawnAreaCollider.bounds;
        for (int i = 0; i < maxAttempts; i++)
        {
            Vector2 candidate = new Vector2(
                Random.Range(bounds.min.x, bounds.max.x),
                Random.Range(bounds.min.y, bounds.max.y));
            if (spawnAreaCollider.OverlapPoint(candidate)) return candidate;
        }
        return Vector2.positiveInfinity;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (IsPlayer(other.gameObject))
        {
            playerInZone = true;
            playerTransform = other.transform;
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (IsPlayer(other.gameObject)) playerInZone = false;
    }

    bool IsPlayer(GameObject go)
    {
        try { if (go.CompareTag("Player")) return true; }
        catch (UnityException) { }
        return go.name == "Player" || go.GetComponent<MovementPlayer>() != null;
    }

    bool IsPositionFree(Vector2 point)
    {
        if (obstacleLayer.value == 0) return true;
        return Physics2D.OverlapCircle(point, overlapCheckRadius, obstacleLayer) == null;
    }

    void CleanupSpawnedList()
    {
        for (int i = spawnedMobs.Count - 1; i >= 0; i--)
            if (spawnedMobs[i] == null) spawnedMobs.RemoveAt(i);
    }

    bool PlayerIsActuallyInZone()
    {
        if (playerInZone) return true;
        if (!autoDetectPlayer) return false;
        FindPlayerIfNeeded();
        if (playerTransform == null) return false;
        if (spawnAreaCollider != null && spawnAreaCollider.OverlapPoint(playerTransform.position)) return true;
        return Vector2.Distance(transform.position, playerTransform.position) <= playerDetectionRange;
    }

    IEnumerator SpawnLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);
            if (mobPrefabs == null || mobPrefabs.Count == 0) continue;
            if (!PlayerIsActuallyInZone()) continue;

            CleanupSpawnedList();
            if (spawnedMobs.Count >= maxMobsInScene) continue;

            Vector2 spawnPoint = GetRandomPointInPolygon();
            if (float.IsPositiveInfinity(spawnPoint.x)) continue;
            if (!IsPositionFree(spawnPoint)) continue;

            GameObject prefab = mobPrefabs[Random.Range(0, mobPrefabs.Count)];
            if (prefab == null) continue;

            GameObject mob = Instantiate(prefab, spawnPoint, Quaternion.identity);
            try { mob.tag = "Mob"; } catch (UnityException) { }
            spawnedMobs.Add(mob);
            Debug.Log($"[Spawner] '{mob.name}' spawnato in {spawnPoint} ({spawnedMobs.Count}/{maxMobsInScene})");
        }
    }
}