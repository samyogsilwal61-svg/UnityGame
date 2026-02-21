using UnityEngine;
using System.Collections;

public class SpeedPickup : MonoBehaviour
{
    [Header("Boost Settings")]
    public float boostSpeed = 22f;
    public float boostDuration = 3f;

    [Header("Spawn Settings")]
    public float respawnTime = 8f;
    public float spawnAreaSize = 40f;

    [Header("Coin Value")]
    public int coinValue = 1;

    private bool collected = false;
    private MeshRenderer meshRenderer;
    private Vector3 basePosition;

    void Start()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        basePosition = transform.position;

        SphereCollider col = GetComponent<SphereCollider>();
        if (col == null) col = gameObject.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = 1.5f;
    }

    void Update()
    {
        // Spin like a coin on Y axis (looks better than Z flip)
        transform.Rotate(0, 180f * Time.deltaTime, 0);

        // Bob up and down
        float bobY = basePosition.y + Mathf.Sin(Time.time * 3f) * 0.2f;
        transform.position = new Vector3(basePosition.x, bobY, basePosition.z);
    }

    void OnTriggerEnter(Collider other)
    {
        if (collected) return;

        // Accept hit from player or player's children
        bool isPlayer = other.CompareTag("Player") ||
                        (other.transform.parent != null && other.transform.parent.CompareTag("Player"));
        if (!isPlayer) return;

        Collect(other.gameObject);
    }

    void Collect(GameObject playerObj)
    {
        collected = true;
        meshRenderer.enabled = false;

        // Add score
        if (GameManager.instance != null)
            GameManager.instance.AddScore(coinValue);

        // Find skater and apply boost via the clean API
        SkaterController skater = playerObj.GetComponent<SkaterController>()
                               ?? playerObj.GetComponentInParent<SkaterController>()
                               ?? playerObj.GetComponentInChildren<SkaterController>();

        if (skater != null)
            skater.ApplyBoost(boostSpeed, boostDuration);

        Invoke(nameof(RespawnRandom), respawnTime);
    }

    void RespawnRandom()
    {
        float rx = Random.Range(-spawnAreaSize, spawnAreaSize);
        float rz = Random.Range(-spawnAreaSize, spawnAreaSize);
        basePosition = new Vector3(rx, 0.5f, rz);
        transform.position = basePosition;
        collected = false;
        meshRenderer.enabled = true;
    }
}