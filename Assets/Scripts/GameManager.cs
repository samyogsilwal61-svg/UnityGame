using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class GameManager : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI coinPopup;        // Shows "+$1 COIN!" etc.
    public TextMeshProUGUI speedBoostText;   // Shows "SPEED BOOST!" flash
    public TextMeshProUGUI wantedText;       // Shows wanted star level
    public GameObject gameOverPanel;
    public TextMeshProUGUI gameOverTitleText;
    public TextMeshProUGUI finalScoreText;

    [Header("Screen Flash")]
    public UnityEngine.UI.Image flashImage;  // Full-screen image on canvas (color changes on events)

    [Header("Camera Reference")]
    public CameraFollow cameraFollow;

    private float timeAlive = 0f;
    private int score = 0;
    private bool gameOver = false;

    // Wanted level escalation
    private int wantedLevel = 1;
    private float wantedTimer = 0f;
    private float wantedInterval = 30f; // seconds per wanted level increase

    public static GameManager instance;

    void Awake()
    {
        instance = this;
        Time.timeScale = 1f;

        if (cameraFollow == null)
            cameraFollow = FindObjectOfType<CameraFollow>();

        UpdateScoreUI();
        UpdateWantedUI();

        // Hide flash image at start
        if (flashImage != null)
            flashImage.color = new Color(1, 1, 1, 0);
    }

    void Update()
    {
        if (gameOver) return;

        timeAlive += Time.unscaledDeltaTime;

        int minutes = Mathf.FloorToInt(timeAlive / 60f);
        int seconds = Mathf.FloorToInt(timeAlive % 60f);

        if (timerText != null)
            timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);

        // Escalate wanted level every wantedInterval seconds
        wantedTimer += Time.unscaledDeltaTime;
        if (wantedTimer >= wantedInterval && wantedLevel < 5)
        {
            wantedLevel++;
            wantedTimer = 0f;
            UpdateWantedUI();
            NotifyCopSpawnerWantedLevel();
        }
    }

    void NotifyCopSpawnerWantedLevel()
    {
        CopSpawner spawner = FindObjectOfType<CopSpawner>();
        if (spawner != null)
            spawner.OnWantedLevelUp(wantedLevel);
    }

    public int GetWantedLevel() => wantedLevel;

    void UpdateWantedUI()
    {
        if (wantedText == null) return;
        string stars = "";
        for (int i = 0; i < 5; i++)
            stars += (i < wantedLevel) ? "★" : "☆";
        wantedText.text = stars;
    }

    public void AddScore(int amount)
    {
        score += amount;
        UpdateScoreUI();
        StartCoroutine(ShowCoinPopup(amount));
    }

    void UpdateScoreUI()
    {
        if (scoreText != null)
            scoreText.text = "$" + score;
    }

    IEnumerator ShowCoinPopup(int amount)
    {
        if (coinPopup == null) yield break;

        // FIX: Actually show the amount earned
        coinPopup.text = "+$" + amount + " COIN!";

        CanvasGroup cg = coinPopup.GetComponent<CanvasGroup>();
        if (cg == null) yield break;

        // Move popup upward while fading
        RectTransform rt = coinPopup.GetComponent<RectTransform>();
        Vector2 startPos = rt != null ? rt.anchoredPosition : Vector2.zero;

        cg.alpha = 0f;
        float elapsed = 0f;
        while (elapsed < 0.3f)
        {
            cg.alpha += Time.unscaledDeltaTime / 0.3f;
            if (rt != null) rt.anchoredPosition = startPos + Vector2.up * (elapsed / 0.3f * 20f);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        cg.alpha = 1f;

        yield return new WaitForSecondsRealtime(0.8f);

        elapsed = 0f;
        while (elapsed < 0.4f)
        {
            cg.alpha -= Time.unscaledDeltaTime / 0.4f;
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        cg.alpha = 0f;
        if (rt != null) rt.anchoredPosition = startPos;
    }

    // Called by SpeedPickup when boost starts
    public void OnSpeedBoostStart()
    {
        StartCoroutine(FlashScreen(new Color(0f, 0.8f, 1f, 0.25f), 0.3f)); // cyan flash
        StartCoroutine(ShowBoostText());
    }

    IEnumerator ShowBoostText()
    {
        if (speedBoostText == null) yield break;
        speedBoostText.text = "⚡ SPEED BOOST! ⚡";
        speedBoostText.gameObject.SetActive(true);

        CanvasGroup cg = speedBoostText.GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.alpha = 1f;
            yield return new WaitForSecondsRealtime(1.5f);
            float e = 0f;
            while (e < 0.5f) { cg.alpha = 1f - (e / 0.5f); e += Time.unscaledDeltaTime; yield return null; }
            cg.alpha = 0f;
        }
        speedBoostText.gameObject.SetActive(false);
    }

    IEnumerator FlashScreen(Color flashColor, float duration)
    {
        if (flashImage == null) yield break;
        flashImage.color = flashColor;
        float e = 0f;
        while (e < duration)
        {
            flashImage.color = new Color(flashColor.r, flashColor.g, flashColor.b, flashColor.a * (1f - e / duration));
            e += Time.unscaledDeltaTime;
            yield return null;
        }
        flashImage.color = new Color(flashColor.r, flashColor.g, flashColor.b, 0f);
    }

    public void TriggerGameOver(string reason = "BUSTED!")
    {
        if (gameOver) return;
        gameOver = true;

        // Camera shake on game over
        if (cameraFollow != null)
            cameraFollow.TriggerShake(0.5f, 0.8f);

        // Flash red on crash/bust
        bool isBust = reason == "BUSTED!";
        StartCoroutine(FlashScreen(isBust ? new Color(1, 0, 0, 0.4f) : new Color(1, 0.5f, 0, 0.4f), 0.5f));

        // Freeze frame then show panel
        StartCoroutine(GameOverSequence(reason));
    }

    IEnumerator GameOverSequence(string reason)
    {
        // Brief freeze frame
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(0.15f);
        Time.timeScale = 1f;

        yield return new WaitForSecondsRealtime(0.3f);

        int minutes = Mathf.FloorToInt(timeAlive / 60f);
        int seconds = Mathf.FloorToInt(timeAlive % 60f);
        string timeString = string.Format("{0:00}:{1:00}", minutes, seconds);

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);

            if (gameOverTitleText != null)
                gameOverTitleText.text = reason;

            if (finalScoreText != null)
                finalScoreText.text = "Time: " + timeString + "\nMoney: $" + score;
        }

        // Freeze after showing panel
        yield return new WaitForSecondsRealtime(0.1f);
        Time.timeScale = 0f;
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(0); // MainMenu is always index 0
    }
}