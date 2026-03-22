using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Attach to each floor call button in the UI.
/// Sends a request to ElevatorManager when clicked.
/// Lights up when a request is active, turns off when serviced.
/// </summary>
public class FloorCallButton : MonoBehaviour
{
    [Header("Configuration")]
    public int floor;

    [Header("References")]
    public Button button;
    public Image buttonImage;
    public TextMeshProUGUI buttonLabel;

    [Header("Colors")]
    public Color normalColor = new Color(0.2f, 0.25f, 0.3f);
    public Color activeColor = new Color(1f, 0.75f, 0.1f);
    public Color hoverColor = new Color(0.3f, 0.35f, 0.4f);

    private bool isActive = false;
    private readonly string[] floorNames = { "G", "1", "2", "3", "4", "5", "6", "7", "8", "9" };

    void Start()
    {
        if (button == null)
            button = GetComponent<Button>();
        if (buttonImage == null)
            buttonImage = GetComponent<Image>();

        button.onClick.AddListener(OnButtonClicked);

        // Set label
        if (buttonLabel != null)
        {
            buttonLabel.text = floor < floorNames.Length ? floorNames[floor] : floor.ToString();
        }

        UpdateVisual();

        // Subscribe to manager events
        if (ElevatorManager.Instance != null)
        {
            ElevatorManager.Instance.OnFloorRequestChanged += OnRequestChanged;
        }
    }

    void OnEnable()
    {
        // Re-subscribe when enabled (in case manager was created after this)
        if (ElevatorManager.Instance != null)
        {
            ElevatorManager.Instance.OnFloorRequestChanged -= OnRequestChanged;
            ElevatorManager.Instance.OnFloorRequestChanged += OnRequestChanged;
        }
    }

    void OnDestroy()
    {
        if (ElevatorManager.Instance != null)
        {
            ElevatorManager.Instance.OnFloorRequestChanged -= OnRequestChanged;
        }
    }

    private void OnButtonClicked()
    {
        if (isActive) return; // Already requested

        ElevatorManager.Instance?.RequestElevator(floor);
    }

    private void OnRequestChanged(int changedFloor, bool active)
    {
        if (changedFloor == floor)
        {
            isActive = active;
            UpdateVisual();
        }
    }

    private void UpdateVisual()
    {
        if (buttonImage != null)
        {
            buttonImage.color = isActive ? activeColor : normalColor;
        }
    }
}
