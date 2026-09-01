using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class CityDirectoryController : MonoBehaviour
{
    public GameObject cityDirectoryPanel;
    public Button cityDirectoryButton;
    public Button closeButton;
    public Button languageButton;
    public Button returnDirectoryButton;
    public Button[] cityButtons = new Button[5];

    private static readonly string[] CityScenes = { "1", "2", "3", "4", "5" };

    private void Awake()
    {
        LanguageManager.EnsureExists();

        if (cityDirectoryButton != null)
        {
            cityDirectoryButton.onClick.AddListener(OpenDirectory);
        }
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseDirectory);
        }
        if (languageButton != null)
        {
            languageButton.onClick.AddListener(ToggleLanguage);
        }
        if (returnDirectoryButton != null)
        {
            returnDirectoryButton.onClick.AddListener(ReturnToDirectory);
        }

        for (int i = 0; i < cityButtons.Length && i < CityScenes.Length; i++)
        {
            if (cityButtons[i] == null)
            {
                continue;
            }
            string sceneName = CityScenes[i];
            cityButtons[i].onClick.AddListener(() => LoadCity(sceneName));
        }

        if (cityDirectoryPanel != null)
        {
            cityDirectoryPanel.SetActive(LanguageManager.ConsumeDirectoryRequest());
        }
    }

    public void OpenDirectory()
    {
        if (cityDirectoryPanel != null)
        {
            cityDirectoryPanel.SetActive(true);
        }
    }

    public void CloseDirectory()
    {
        if (cityDirectoryPanel != null)
        {
            cityDirectoryPanel.SetActive(false);
        }
    }

    public void ToggleLanguage()
    {
        LanguageManager.EnsureExists().ToggleLanguage();
    }

    public void LoadCity(string sceneName)
    {
        if (cityDirectoryPanel != null)
        {
            cityDirectoryPanel.SetActive(false);
        }
        SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }

    public void ReturnToDirectory()
    {
        LanguageManager.RequestDirectoryOnStart();
        SceneManager.LoadScene("Start", LoadSceneMode.Single);
    }
}
