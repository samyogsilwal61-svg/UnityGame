using UnityEngine;
using UnityEngine.AI;

public class CopSpawner : MonoBehaviour
{
    [Header("Spawning Settings")]
    public GameObject copPrefab;
    public Transform player;
    public float spawnDistance = 25f;
    public int maxCops = 20;

    private float startSpawnTime;
    private float minSpawnTime;
    private float currentSpawnTime;
    private float timer = 0f;
    private int copCount = 0;

    void Start()
    {
        int difficulty = PlayerPrefs.GetInt("Difficulty", 0);
        SetDifficulty(difficulty);
        currentSpawnTime = startSpawnTime;

        if (player == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) player = p.transform;
        }
    }

    void SetDifficulty(int difficulty)
    {
        switch (difficulty)
        {
            case 0: startSpawnTime = 20f; minSpawnTime = 8f; maxCops = 10; break; // EASY
            case 1: startSpawnTime = 15f; minSpawnTime = 5f; maxCops = 15; break; // MEDIUM
            case 2: startSpawnTime = 10f; minSpawnTime = 3f; maxCops = 25; break; // HARD
        }
    }

    // Called by GameManager when wanted level increases
    public void OnWantedLevelUp(int wantedLevel)
    {
        // Each star cuts the minimum spawn interval by 0.5s and raises max cops
        minSpawnTime = Mathf.Max(1.5f, minSpawnTime - 0.5f);
        maxCops += 2;
        currentSpawnTime = Mathf.Max(minSpawnTime, currentSpawnTime - 2f);
        Debug.Log($"Wanted level {wantedLevel}! Spawn interval: {currentSpawnTime}s, MaxCops: {maxCops}");
    }

    void Update()
    {
        if (GameManager.instance == null || player == null) return;

        timer += Time.deltaTime;

        if (timer >= currentSpawnTime && copCount < maxCops)
        {
            SpawnCop();
            timer = 0f;
            currentSpawnTime = Mathf.Max(minSpawnTime, currentSpawnTime - 0.5f);
        }
    }

    void SpawnCop()
    {
        // Spawn behind or to the sides, never in front of the player
        Vector3 randomDirection = Random.insideUnitSphere * spawnDistance;
        randomDirection.y = 0f;
        Vector3 spawnPos = player.position + randomDirection;
        spawnPos.y = 0f;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(spawnPos, out hit, 15f, NavMesh.AllAreas))
        {
            GameObject cop = Instantiate(copPrefab, hit.position, Quaternion.identity);

            // Set difficulty-based speed on the cop's PoliceChase script
            PoliceChase chase = cop.GetComponent<PoliceChase>();
            if (chase != null)
            {
                int wantedLevel = GameManager.instance != null ? GameManager.instance.GetWantedLevel() : 1;
                chase.SetSpeedFromWantedLevel(wantedLevel);
            }

            NavMeshAgent agent = cop.GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                agent.enabled = true;
                agent.Warp(hit.position);
            }

            cop.transform.SetParent(null); // Don't parent under spawner
            copCount++;
        }
    }

    public void CopDestroyed()
    {
        copCount = Mathf.Max(0, copCount - 1);
    }
}