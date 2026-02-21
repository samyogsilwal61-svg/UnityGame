using UnityEngine;
using System.Collections;

public class CameraFollow : MonoBehaviour
{
    [Header("Follow Settings")]
    public Transform target;
    public float distance = 8f;
    public float height = 4f;
    public float smoothSpeed = 5f;

    [Header("Shake Settings")]
    private Vector3 shakeOffset;
    private bool isShaking = false;

    // Call this from GameManager on crash/bust
    public void TriggerShake(float duration = 0.4f, float magnitude = 0.6f)
    {
        if (!isShaking)
            StartCoroutine(Shake(duration, magnitude));
    }

    IEnumerator Shake(float duration, float magnitude)
    {
        isShaking = true;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float z = Random.Range(-1f, 1f) * magnitude;
            shakeOffset = new Vector3(x, 0f, z);
            elapsed += Time.unscaledDeltaTime;
            magnitude *= 0.9f; // dampen shake over time
            yield return null;
        }

        shakeOffset = Vector3.zero;
        isShaking = false;
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPos = target.position
                           - target.forward * distance
                           + Vector3.up * height
                           + shakeOffset;

        transform.position = Vector3.Lerp(
            transform.position,
            desiredPos,
            smoothSpeed * Time.deltaTime
        );

        transform.LookAt(target.position + Vector3.up * 1f);
    }
}