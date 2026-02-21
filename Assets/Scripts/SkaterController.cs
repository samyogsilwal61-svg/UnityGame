using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class SkaterController : MonoBehaviour
{
    [Header("Skating Settings")]
    public float topSpeed = 12f;
    public float acceleration = 8f;
    public float turnSpeed = 100f;
    public float friction = 5f;

    [Header("Effects")]
    public GameObject explosionEffect;
    public TrailRenderer skateTrail;
    public ParticleSystem dustParticles;

    private CharacterController cc;
    private float currentSpeed = 0f;
    private bool isDead = false;

    private float _baseTopSpeed;
    private Coroutine _boostCoroutine;

    // ── NEW: Animator reference ──
    private Animator anim;

    void Start()
    {
        cc = GetComponent<CharacterController>();
        _baseTopSpeed = topSpeed;

        // ── NEW: Find Animator on james child object ──
        anim = GetComponentInChildren<Animator>();

        if (anim == null)
            Debug.LogWarning("No Animator found on SkaterBoy children! Make sure james has an Animator component.");

        if (skateTrail != null)
        {
            skateTrail.emitting = false;
            skateTrail.time = 0.3f;
        }
    }

    void Update()
    {
        if (isDead) return;

        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        // Turning
        if (keyboard.aKey.isPressed) transform.Rotate(0, -turnSpeed * Time.deltaTime, 0);
        if (keyboard.dKey.isPressed) transform.Rotate(0, turnSpeed * Time.deltaTime, 0);

        // Acceleration
        if (keyboard.wKey.isPressed)
            currentSpeed = Mathf.MoveTowards(currentSpeed, topSpeed, acceleration * Time.deltaTime);
        else if (keyboard.sKey.isPressed)
            currentSpeed = Mathf.MoveTowards(currentSpeed, -topSpeed * 0.5f, acceleration * Time.deltaTime);
        else
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0, friction * Time.deltaTime);

        // Move
        Vector3 move = transform.forward * currentSpeed;
        move.y = -20f;
        cc.Move(move * Time.deltaTime);

        // ── NEW: Drive animation speed ──
        if (anim != null)
            anim.SetFloat("Speed", Mathf.Abs(currentSpeed));

        // Trail: emit only when going fast
        if (skateTrail != null)
            skateTrail.emitting = Mathf.Abs(currentSpeed) > topSpeed * 0.5f;

        // Dust particles: only when very fast
        if (dustParticles != null)
        {
            bool fastEnough = Mathf.Abs(currentSpeed) > topSpeed * 0.75f;
            if (fastEnough && !dustParticles.isPlaying) dustParticles.Play();
            if (!fastEnough && dustParticles.isPlaying) dustParticles.Stop();
        }
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (isDead) return;
        if (currentSpeed < 4f) return;
        if (hit.normal.y > 0.5f) return;
        if (hit.gameObject.CompareTag("Police")) return;

        Blast();
    }

    public void Blast()
    {
        if (isDead) return;
        isDead = true;
        currentSpeed = 0f;

        // ── NEW: Trigger crash animation ──
        if (anim != null)
            anim.SetTrigger("Crash");

        if (skateTrail != null) skateTrail.emitting = false;
        if (dustParticles != null) dustParticles.Stop();

        if (explosionEffect != null)
            Instantiate(explosionEffect, transform.position, Quaternion.identity);

        if (GameManager.instance != null)
            GameManager.instance.TriggerGameOver("CRASHED!");
        else
            Time.timeScale = 0f;
    }

    // ── Boost API ────────────────────────────────────────────────────────
    public void ApplyBoost(float boostSpeed, float duration)
    {
        if (_boostCoroutine != null)
            StopCoroutine(_boostCoroutine);
        _boostCoroutine = StartCoroutine(BoostRoutine(boostSpeed, duration));
    }

    IEnumerator BoostRoutine(float boostSpeed, float duration)
    {
        topSpeed = boostSpeed;

        if (GameManager.instance != null)
            GameManager.instance.OnSpeedBoostStart();

        if (skateTrail != null)
        {
            skateTrail.time = 0.6f;
            skateTrail.startColor = Color.cyan;
        }

        yield return new WaitForSeconds(duration);

        topSpeed = _baseTopSpeed;
        _boostCoroutine = null;

        if (skateTrail != null)
        {
            skateTrail.time = 0.3f;
            skateTrail.startColor = Color.white;
        }
    }
}