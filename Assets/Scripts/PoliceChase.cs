using UnityEngine;
using UnityEngine.AI;

public class PoliceChase : MonoBehaviour
{
    [Header("Chase Settings")]
    public Transform player;
    public float catchDistance = 0.8f;
    public float sightRange = 50f;
    public float baseSpeed = 6f;

    [Header("Siren Lights")]
    public Light sirenLight;
    public Color sirenColorA = Color.red;
    public Color sirenColorB = Color.blue;
    public float sirenSpeed = 4f;

    [Header("Effects")]
    public TrailRenderer tireTrail;

    private NavMeshAgent agent;
    private bool playerCaught = false;

    // ── NEW: Animator reference ──
    private Animator anim;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        if (agent == null)
        {
            Debug.LogError("No NavMeshAgent on " + gameObject.name);
            return;
        }

        agent.speed = baseSpeed;

        // ── NEW: Find Animator on police character child ──
        anim = GetComponentInChildren<Animator>();
        if (anim == null)
            Debug.LogWarning("No Animator found on " + gameObject.name + " children!");

        if (player == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) player = p.transform;
        }

        // Auto-create siren light if none assigned
        if (sirenLight == null)
        {
            GameObject lightObj = new GameObject("SirenLight");
            lightObj.transform.SetParent(transform);
            lightObj.transform.localPosition = new Vector3(0, 1.2f, 0);
            sirenLight = lightObj.AddComponent<Light>();
            sirenLight.type = LightType.Point;
            sirenLight.range = 8f;
            sirenLight.intensity = 3f;
            sirenLight.color = sirenColorA;
        }
    }

    // Called by CopSpawner after instantiation
    public void SetSpeedFromWantedLevel(int wantedLevel)
    {
        float speed = baseSpeed + (wantedLevel - 1) * 0.5f;
        if (agent != null) agent.speed = speed;
    }

    void Update()
    {
        if (playerCaught || player == null || agent == null) return;
        if (!agent.isOnNavMesh) return;

        // Animate siren light
        if (sirenLight != null)
        {
            float t = Mathf.PingPong(Time.time * sirenSpeed, 1f);
            sirenLight.color = Color.Lerp(sirenColorA, sirenColorB, t);
        }

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer < sightRange)
            agent.SetDestination(player.position);

        // ── NEW: Drive run animation from agent velocity ──
        if (anim != null)
            anim.SetFloat("Speed", agent.velocity.magnitude);

        if (distanceToPlayer < catchDistance)
            CatchPlayer();
    }

    void CatchPlayer()
    {
        playerCaught = true;
        agent.ResetPath();
        agent.isStopped = true;

        // ── NEW: Trigger caught animation ──
        if (anim != null)
            anim.SetTrigger("Caught");

        if (GameManager.instance != null)
            GameManager.instance.TriggerGameOver("BUSTED!");
        else
            Time.timeScale = 0f;
    }

    void OnDestroy()
    {
        CopSpawner spawner = FindObjectOfType<CopSpawner>();
        if (spawner != null) spawner.CopDestroyed();
    }
}