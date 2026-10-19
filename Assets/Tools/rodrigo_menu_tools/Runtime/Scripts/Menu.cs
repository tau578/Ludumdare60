using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem; // Import the new Input System namespace
using UnityEngine.InputSystem.Controls; // Add this using directive

namespace MenuTemplates
{
}
public class Menu : MonoBehaviour
{
    [SerializeField] private GameObject pressStartPanel;
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private GameObject creditsPanel;
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private GameObject MainMenuFirstButton;
    [SerializeField] private GameObject MainMenuSecondButton;
    [SerializeField] private GameObject MainMenuThirdButton;
    [SerializeField] private GameObject OptionsFirstButton;
    [SerializeField] private GameObject OptionsClosedButton;
    [SerializeField] private GameObject CreditsFirstButton;
    [SerializeField] private GameObject CreditsClosedButton;

    private bool hasStarted = false;

    private void Awake()
    {
        // Mouse-driven buttons highlight only while hovered, never because of selection.
        foreach (var button in GetComponentsInChildren<Button>(true))
            button.navigation = new Navigation { mode = Navigation.Mode.None };
        ClearSelection();
    }

    private static void ClearSelection()
    {
        if (EventSystem.current == null) return;
        EventSystem.current.firstSelectedGameObject = null;
        EventSystem.current.SetSelectedGameObject(null);
    }

    private void Start()
    {
        hasStarted = pressStartPanel == null;
        if (pressStartPanel != null) pressStartPanel.SetActive(true);
        mainMenuPanel.SetActive(hasStarted);
        optionsPanel.SetActive(false);
        creditsPanel.SetActive(false);
        ClearSelection();

        // Initialize toggle and slider values
        if (fullscreenToggle != null)
            fullscreenToggle.isOn = Screen.fullScreen;

        if (volumeSlider != null)
            volumeSlider.value = AudioListener.volume;
    }

    private void Update()
    {
        if (!hasStarted && IsAnyInput())
        {
            pressStartPanel.SetActive(false);
            ClearSelection();
            mainMenuPanel.SetActive(true);
            hasStarted = true;
        }
    }

    // Use the new Input System to check for any input
    private bool IsAnyInput()
    {
        // Keyboard
        if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
            return true;

        // Mouse
        if (Mouse.current != null && (Mouse.current.leftButton.wasPressedThisFrame ||
                                      Mouse.current.rightButton.wasPressedThisFrame ||
                                      Mouse.current.middleButton.wasPressedThisFrame))
            return true;

        // Touch: a tap closes the press-start panel on phones (D28)
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            return true;

        // Gamepad
        if (Gamepad.current != null && Gamepad.current.allControls.Any(c => c is ButtonControl btn && btn.wasPressedThisFrame))
            return true;

        return false;
    }

    public void onPlayButtonClick()
    {
        int currentSceneBuildIndex = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(currentSceneBuildIndex + 1);
    }

    public void onOptionsButtonClick()
    {
        ClearSelection();
        mainMenuPanel.SetActive(false);
        optionsPanel.SetActive(true);
    }

    public void onCreditsButtonClick()
    {
        ClearSelection();
        mainMenuPanel.SetActive(false);
        creditsPanel.SetActive(true);
    }

    public void onCloseOptionsButtonClick()
    {
        ClearSelection();
        optionsPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }

    public void onCloseCreditsButtonClick()
    {
        ClearSelection();
        creditsPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }

    public void onQuitButtonClick()
    {
        Application.Quit();
    }

    // Call this from the Toggle's OnValueChanged event
    public void OnFullscreenToggleChanged(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
    }

    // Call this from the Slider's OnValueChanged event
    public void OnVolumeSliderChanged(float value)
    {
        AudioListener.volume = value;
    }
}
