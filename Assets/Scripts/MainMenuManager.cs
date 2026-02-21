using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class MainMenuManager : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject howToPlayPanel;
    public TextMeshProUGUI difficultyButtonText;

    [Header("Loading Screen")]
    public GameObject loadingPanel;         // A simple panel that says "Loading..."
    public Slider loadingBar;               // Optional progress bar on the loading panel

    // ─── IMPORTANT: Set this in Inspector to your GameScene's exact name ───
    [Header("Scene Name")]
    public string gameSceneName = "GameScene";  // Must match exactly in Build Settings

    private int currentDifficulty = 0;
    private readonly string[] difficultyNames = { "EASY", "MEDIUM", "HARD" };

    void Start()
    {
        currentDifficulty = PlayerPrefs.GetInt("Difficulty", 0);
        UpdateDifficultyText();

        // Make sure game isn't paused from a previous session
        Time.timeScale = 1f;

        if (howToPlayPanel != null) howToPlayPanel.SetActive(false);
        if (loadingPanel != null)  loadingPanel.SetActive(false);
    }

    public void PlayGame()
    {
        PlayerPrefs.SetInt("Difficulty", currentDifficulty);
        PlayerPrefs.Save();
        StartCoroutine(LoadGameScene());
    }

    IEnumerator LoadGameScene()
    {
        // Show loading screen
        if (loadingPanel != null) loadingPanel.SetActive(true);

        // Small delay so the loading panel is visible
        yield return new WaitForSeconds(0.1f);

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(gameSceneName);

        // FIX: Don't allow the scene to activate until fully loaded
        asyncLoad.allowSceneActivation = false;

        while (asyncLoad.progress < 0.9f)
        {
            if (loadingBar != null)
                loadingBar.value = asyncLoad.progress;
            yield return null;
        }

        // Fill bar to 100% then activate
        if (loadingBar != null) loadingBar.value = 1f;
        yield return new WaitForSeconds(0.2f);

        asyncLoad.allowSceneActivation = true;
    }

    public void CycleDifficulty()
    {
        currentDifficulty = (currentDifficulty + 1) % 3;
        UpdateDifficultyText();
    }

    void UpdateDifficultyText()
    {
        if (difficultyButtonText != null)
            difficultyButtonText.text = "DIFFICULTY: " + difficultyNames[currentDifficulty];
    }

    public void ShowHowToPlay()
    {
        if (howToPlayPanel != null) howToPlayPanel.SetActive(true);
    }

    public void HideHowToPlay()
    {
        if (howToPlayPanel != null) howToPlayPanel.SetActive(false);
    }

    public void ExitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}