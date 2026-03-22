using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Attach this to an empty GameObject named "SceneBuilder" in a blank scene.
/// On Awake, it programmatically creates the entire elevator simulation:
///   - 3 elevator shafts with cabins and doors
///   - Floor indicators (horizontal lines + labels)
///   - Canvas with floor call buttons and elevator status panels
///   - Camera setup
///
/// This avoids manual scene assembly — just add this one script and press Play.
/// </summary>
public class ElevatorSceneBuilder : MonoBehaviour
{
    [Header("Simulation Settings")]
    public int totalFloors = 4;
    public int totalElevators = 3;
    public float floorHeight = 3.0f;
    public float elevatorSpacing = 2.5f;
    public float elevatorSpeed = 2.5f;

    private Elevator[] elevators;
    private readonly string[] floorNames = { "Ground", "Floor 1", "Floor 2", "Floor 3",
                                              "Floor 4", "Floor 5", "Floor 6", "Floor 7" };

    void Awake()
    {
        SetupCamera();
        CreateBuilding();
        elevators = CreateElevators();
        CreateManager(elevators);
        CreateUI(elevators);

        // Add keyboard input handler
        gameObject.AddComponent<ElevatorKeyboardInput>();
    }

    // ─── Camera ────────────────────────────────────────────────────────

    void SetupCamera()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            GameObject camObj = new GameObject("Main Camera");
            cam = camObj.AddComponent<Camera>();
            camObj.tag = "MainCamera";
        }

        // Center camera on the building
        float centerX = (totalElevators - 1) * elevatorSpacing / 2f;
        float centerY = (totalFloors - 1) * floorHeight / 2f;
        cam.transform.position = new Vector3(centerX, centerY, -10f);
        cam.orthographic = true;
        cam.orthographicSize = Mathf.Max(totalFloors * floorHeight / 2f + 2f, 8f);
        cam.backgroundColor = new Color(0.12f, 0.13f, 0.16f);
    }

    // ─── Building Structure ────────────────────────────────────────────

    void CreateBuilding()
    {
        GameObject building = new GameObject("Building");

        float buildingLeft = -2f;
        float buildingRight = (totalElevators - 1) * elevatorSpacing + 2f;

        for (int i = 0; i < totalFloors; i++)
        {
            float y = i * floorHeight;

            // Floor line
            GameObject floorLine = new GameObject($"FloorLine_{i}");
            floorLine.transform.parent = building.transform;
            var lr = floorLine.AddComponent<LineRenderer>();
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = lr.endColor = new Color(0.3f, 0.35f, 0.4f);
            lr.startWidth = lr.endWidth = 0.05f;
            lr.positionCount = 2;
            lr.SetPosition(0, new Vector3(buildingLeft, y, 0));
            lr.SetPosition(1, new Vector3(buildingRight, y, 0));
            lr.sortingOrder = -1;

            // Floor label (left side)
            GameObject label = new GameObject($"FloorLabel_{i}");
            label.transform.parent = building.transform;
            label.transform.position = new Vector3(buildingLeft - 0.3f, y + 0.3f, 0);
            var tmp = label.AddComponent<TextMeshPro>();
            tmp.text = i < floorNames.Length ? floorNames[i] : $"Floor {i}";
            tmp.fontSize = 2.5f;
            tmp.alignment = TextAlignmentOptions.Right;
            tmp.color = new Color(0.6f, 0.65f, 0.7f);
            tmp.rectTransform.sizeDelta = new Vector2(4f, 1f);
        }

        // Shaft walls for each elevator
        for (int e = 0; e < totalElevators; e++)
        {
            float x = e * elevatorSpacing;
            float shaftLeft = x - 0.6f;
            float shaftRight = x + 0.6f;
            float topY = (totalFloors - 1) * floorHeight + floorHeight;

            CreateShaftWall(building.transform, $"ShaftWallL_{e}",
                new Vector3(shaftLeft, topY / 2f, 0.1f),
                new Vector2(0.04f, topY), new Color(0.25f, 0.28f, 0.32f));

            CreateShaftWall(building.transform, $"ShaftWallR_{e}",
                new Vector3(shaftRight, topY / 2f, 0.1f),
                new Vector2(0.04f, topY), new Color(0.25f, 0.28f, 0.32f));
        }
    }

    void CreateShaftWall(Transform parent, string name, Vector3 pos, Vector2 size, Color color)
    {
        GameObject wall = new GameObject(name);
        wall.transform.parent = parent;
        wall.transform.position = pos;
        var sr = wall.AddComponent<SpriteRenderer>();
        sr.sprite = CreateSquareSprite();
        sr.color = color;
        wall.transform.localScale = new Vector3(size.x, size.y, 1);
        sr.sortingOrder = -2;
    }

    // ─── Elevators ─────────────────────────────────────────────────────

    Elevator[] CreateElevators()
    {
        Elevator[] result = new Elevator[totalElevators];
        Color[] cabinColors = {
            new Color(0.45f, 0.55f, 0.7f),
            new Color(0.55f, 0.65f, 0.5f),
            new Color(0.65f, 0.5f, 0.55f)
        };

        for (int i = 0; i < totalElevators; i++)
        {
            float x = i * elevatorSpacing;

            // Cabin root
            GameObject cabin = new GameObject($"Elevator_{i}");
            cabin.transform.position = new Vector3(x, 0, 0);

            // Cabin body
            GameObject body = new GameObject("CabinBody");
            body.transform.parent = cabin.transform;
            body.transform.localPosition = Vector3.zero;
            var bodyRenderer = body.AddComponent<SpriteRenderer>();
            bodyRenderer.sprite = CreateSquareSprite();
            bodyRenderer.color = cabinColors[i % cabinColors.Length];
            body.transform.localScale = new Vector3(1f, 1.2f, 1);
            bodyRenderer.sortingOrder = 1;

            // Left door
            GameObject doorL = new GameObject("DoorLeft");
            doorL.transform.parent = cabin.transform;
            doorL.transform.localPosition = new Vector3(-0.15f, 0, -0.01f);
            var doorLR = doorL.AddComponent<SpriteRenderer>();
            doorLR.sprite = CreateSquareSprite();
            doorLR.color = new Color(0.35f, 0.38f, 0.42f);
            doorL.transform.localScale = new Vector3(0.3f, 1.0f, 1);
            doorLR.sortingOrder = 2;

            // Right door
            GameObject doorR = new GameObject("DoorRight");
            doorR.transform.parent = cabin.transform;
            doorR.transform.localPosition = new Vector3(0.15f, 0, -0.01f);
            var doorRR = doorR.AddComponent<SpriteRenderer>();
            doorRR.sprite = CreateSquareSprite();
            doorRR.color = new Color(0.35f, 0.38f, 0.42f);
            doorR.transform.localScale = new Vector3(0.3f, 1.0f, 1);
            doorRR.sortingOrder = 2;

            // Floor number label on cabin
            GameObject floorLabelObj = new GameObject("FloorLabel");
            floorLabelObj.transform.parent = cabin.transform;
            floorLabelObj.transform.localPosition = new Vector3(0, 0.25f, -0.1f);
            var floorTMP = floorLabelObj.AddComponent<TextMeshPro>();
            floorTMP.text = "G";
            floorTMP.fontSize = 3f;
            floorTMP.alignment = TextAlignmentOptions.Center;
            floorTMP.color = Color.white;
            floorTMP.sortingOrder = 3;
            floorTMP.rectTransform.sizeDelta = new Vector2(1f, 0.5f);

            // State label on cabin
            GameObject stateLabelObj = new GameObject("StateLabel");
            stateLabelObj.transform.parent = cabin.transform;
            stateLabelObj.transform.localPosition = new Vector3(0, -0.2f, -0.1f);
            var stateTMP = stateLabelObj.AddComponent<TextMeshPro>();
            stateTMP.text = "IDLE";
            stateTMP.fontSize = 2f;
            stateTMP.alignment = TextAlignmentOptions.Center;
            stateTMP.color = new Color(0.9f, 0.9f, 0.9f, 0.8f);
            stateTMP.sortingOrder = 3;
            stateTMP.rectTransform.sizeDelta = new Vector2(1f, 0.4f);

            // Elevator ID label (above cabin)
            GameObject idLabel = new GameObject("ElevatorIDLabel");
            idLabel.transform.parent = cabin.transform;
            idLabel.transform.localPosition = new Vector3(0, 0.85f, -0.1f);
            var idTMP = idLabel.AddComponent<TextMeshPro>();
            idTMP.text = $"E{i + 1}";
            idTMP.fontSize = 2.5f;
            idTMP.alignment = TextAlignmentOptions.Center;
            idTMP.color = cabinColors[i % cabinColors.Length];
            idTMP.sortingOrder = 3;
            idTMP.rectTransform.sizeDelta = new Vector2(1f, 0.5f);

            // Add Elevator component
            var elevator = cabin.AddComponent<Elevator>();
            elevator.elevatorId = i;
            elevator.speed = elevatorSpeed;
            elevator.floorHeight = floorHeight;
            elevator.totalFloors = totalFloors;
            elevator.cabinRenderer = bodyRenderer;
            elevator.doorLeftRenderer = doorLR;
            elevator.doorRightRenderer = doorRR;
            elevator.floorLabel = floorTMP;
            elevator.stateLabel = stateTMP;

            result[i] = elevator;
        }

        return result;
    }

    // ─── Manager ───────────────────────────────────────────────────────

    void CreateManager(Elevator[] elevatorArray)
    {
        GameObject managerObj = new GameObject("ElevatorManager");
        var manager = managerObj.AddComponent<ElevatorManager>();
        manager.elevators = elevatorArray;
        manager.totalFloors = totalFloors;
    }

    // ─── UI ────────────────────────────────────────────────────────────

    void CreateUI(Elevator[] elevatorArray)
    {
        // Create Canvas
        GameObject canvasObj = new GameObject("UICanvas");
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObj.AddComponent<GraphicRaycaster>();

        // ─── Left Panel: Floor Call Buttons ─────────────────────────────
        GameObject leftPanel = CreatePanel(canvasObj.transform, "FloorButtonPanel",
            new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f),
            new Vector2(20, -20), new Vector2(200, 0),
            new Color(0.15f, 0.16f, 0.2f, 0.9f));

        // Title
        CreateTextElement(leftPanel.transform, "Title", "CALL ELEVATOR",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0, -15), new Vector2(180, 30),
            16, FontStyles.Bold, new Color(0.8f, 0.82f, 0.85f));

        // Add VerticalLayoutGroup for buttons
        GameObject buttonContainer = new GameObject("ButtonContainer");
        buttonContainer.transform.SetParent(leftPanel.transform, false);
        var rt = buttonContainer.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0, -50);
        rt.sizeDelta = new Vector2(160, totalFloors * 55);

        var vlg = buttonContainer.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 8;
        vlg.padding = new RectOffset(10, 10, 5, 5);
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // Resize left panel to fit
        var leftPanelRT = leftPanel.GetComponent<RectTransform>();
        leftPanelRT.sizeDelta = new Vector2(200, totalFloors * 55 + 70);

        // Create buttons from top floor down
        for (int i = totalFloors - 1; i >= 0; i--)
        {
            CreateFloorButton(buttonContainer.transform, i);
        }

        // ─── Bottom Panel: Elevator Status ──────────────────────────────
        GameObject bottomPanel = CreatePanel(canvasObj.transform, "StatusPanel",
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0, 20), new Vector2(totalElevators * 220 + 40, 110),
            new Color(0.15f, 0.16f, 0.2f, 0.9f));

        var hlg = bottomPanel.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 15;
        hlg.padding = new RectOffset(15, 15, 10, 10);
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = true;

        Color[] panelColors = {
            new Color(0.2f, 0.25f, 0.35f, 0.8f),
            new Color(0.22f, 0.3f, 0.22f, 0.8f),
            new Color(0.3f, 0.22f, 0.25f, 0.8f)
        };

        for (int i = 0; i < totalElevators; i++)
        {
            GameObject elevPanel = new GameObject($"ElevatorStatus_{i}");
            elevPanel.transform.SetParent(bottomPanel.transform, false);
            var eImg = elevPanel.AddComponent<Image>();
            eImg.color = panelColors[i % panelColors.Length];

            var le = elevPanel.AddComponent<LayoutElement>();
            le.minWidth = 180;
            le.preferredWidth = 200;
            le.minHeight = 80;

            // Elevator name
            CreateTextElement(elevPanel.transform, "Name", $"Elevator {i + 1}",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0, -8), new Vector2(180, 25),
                14, FontStyles.Bold, new Color(0.9f, 0.9f, 0.9f));

            // Floor text
            var floorText = CreateTextElement(elevPanel.transform, "FloorText", "Floor: G",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, 5), new Vector2(180, 25),
                18, FontStyles.Bold, Color.white);

            // State text
            var stateText = CreateTextElement(elevPanel.transform, "StateText", "Idle",
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0, 12), new Vector2(180, 22),
                12, FontStyles.Normal, new Color(0.7f, 0.75f, 0.8f));

            // Link to elevator component
            elevatorArray[i].uiFloorText = floorText;
            elevatorArray[i].uiStateText = stateText;
        }

        // ─── Instruction text ───────────────────────────────────────────
        CreateTextElement(canvasObj.transform, "Instructions",
            "Click floor buttons to call an elevator.\nKeyboard: 0 = Ground, 1-3 = Floors",
            new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f),
            new Vector2(-20, 20), new Vector2(350, 50),
            12, FontStyles.Italic, new Color(0.5f, 0.55f, 0.6f));
    }

    // ─── UI Helpers ────────────────────────────────────────────────────

    void CreateFloorButton(Transform parent, int floor)
    {
        string label = floor == 0 ? "G - Ground" : $"{floor} - Floor {floor}";

        GameObject btnObj = new GameObject($"FloorBtn_{floor}");
        btnObj.transform.SetParent(parent, false);

        var img = btnObj.AddComponent<Image>();
        img.color = new Color(0.22f, 0.25f, 0.3f);

        var btn = btnObj.AddComponent<Button>();
        var colors = btn.colors;
        colors.normalColor = new Color(0.22f, 0.25f, 0.3f);
        colors.highlightedColor = new Color(0.3f, 0.35f, 0.4f);
        colors.pressedColor = new Color(0.4f, 0.45f, 0.5f);
        colors.selectedColor = new Color(0.25f, 0.28f, 0.33f);
        btn.colors = colors;

        var le = btnObj.AddComponent<LayoutElement>();
        le.minHeight = 40;
        le.preferredHeight = 44;

        // Button label
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        var tmpro = textObj.AddComponent<TextMeshProUGUI>();
        tmpro.text = label;
        tmpro.fontSize = 14;
        tmpro.fontStyle = FontStyles.Bold;
        tmpro.color = Color.white;
        tmpro.alignment = TextAlignmentOptions.Center;
        var textRT = textObj.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;

        // Add FloorCallButton component
        var callButton = btnObj.AddComponent<FloorCallButton>();
        callButton.floor = floor;
        callButton.button = btn;
        callButton.buttonImage = img;
        callButton.buttonLabel = tmpro;
        callButton.normalColor = new Color(0.22f, 0.25f, 0.3f);
        callButton.activeColor = new Color(0.95f, 0.7f, 0.15f);
        callButton.hoverColor = new Color(0.3f, 0.35f, 0.4f);
    }

    GameObject CreatePanel(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
        Vector2 anchoredPos, Vector2 size, Color color)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);
        var rt = panel.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        var img = panel.AddComponent<Image>();
        img.color = color;

        return panel;
    }

    TextMeshProUGUI CreateTextElement(Transform parent, string name, string text,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
        Vector2 anchoredPos, Vector2 size,
        float fontSize, FontStyles style, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        var rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        var tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;

        return tmp;
    }

    // ─── Sprite Creation ───────────────────────────────────────────────

    static Sprite _squareSprite;
    public static Sprite CreateSquareSprite()
    {
        if (_squareSprite != null) return _squareSprite;

        Texture2D tex = new Texture2D(4, 4);
        Color[] colors = new Color[16];
        for (int i = 0; i < 16; i++) colors[i] = Color.white;
        tex.SetPixels(colors);
        tex.Apply();
        tex.filterMode = FilterMode.Point;

        _squareSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4);
        return _squareSprite;
    }
}
