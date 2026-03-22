using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public enum ElevatorState
{
    Idle,
    MovingUp,
    MovingDown,
    DoorsOpening,
    DoorsOpen,
    DoorsClosing
}

public class Elevator : MonoBehaviour
{
    [Header("Configuration")]
    public int elevatorId;
    public float speed = 2.0f;
    public float doorOpenDuration = 2.0f;
    public float floorHeight = 3.0f;
    public int totalFloors = 4;

    [Header("Visual References")]
    public SpriteRenderer cabinRenderer;
    public SpriteRenderer doorLeftRenderer;
    public SpriteRenderer doorRightRenderer;
    public TextMeshPro floorLabel;
    public TextMeshPro stateLabel;

    [Header("UI Panel References")]
    public TextMeshProUGUI uiFloorText;
    public TextMeshProUGUI uiStateText;
    public Image uiDirectionArrow;

    // Runtime state
    public int CurrentFloor { get; private set; } = 0;
    public ElevatorState State { get; private set; } = ElevatorState.Idle;
    public ElevatorState Direction { get; private set; } = ElevatorState.Idle;

    private SortedSet<int> upQueue = new SortedSet<int>();
    private SortedSet<int> downQueue = new SortedSet<int>(Comparer<int>.Create((a, b) => b.CompareTo(a)));
    private float targetY;
    private int targetFloor;
    private float doorTimer;
    private float doorOpenAmount = 0f;
    private float doorMaxOffset = 0.35f;
    private Vector3 doorLeftClosed, doorRightClosed;
    private bool isMoving = false;

    private readonly string[] floorNames = { "G", "1", "2", "3", "4", "5", "6", "7", "8", "9" };

    void Start()
    {
        CurrentFloor = 0;
        targetFloor = 0;
        transform.position = new Vector3(transform.position.x, 0, 0);
        targetY = 0;

        if (doorLeftRenderer != null && doorRightRenderer != null)
        {
            doorLeftClosed = doorLeftRenderer.transform.localPosition;
            doorRightClosed = doorRightRenderer.transform.localPosition;
        }

        UpdateVisuals();
    }

    void Update()
    {
        switch (State)
        {
            case ElevatorState.Idle:
                ProcessNextRequest();
                break;

            case ElevatorState.MovingUp:
            case ElevatorState.MovingDown:
                MoveToTarget();
                break;

            case ElevatorState.DoorsOpening:
                AnimateDoors(true);
                break;

            case ElevatorState.DoorsOpen:
                doorTimer -= Time.deltaTime;
                if (doorTimer <= 0)
                {
                    State = ElevatorState.DoorsClosing;
                }
                break;

            case ElevatorState.DoorsClosing:
                AnimateDoors(false);
                break;
        }

        UpdateVisuals();
    }

    /// <summary>
    /// Add a floor request to this elevator's queue.
    /// </summary>
    public void AddRequest(int floor)
    {
        if (floor == CurrentFloor && State == ElevatorState.Idle)
        {
            StartDoorSequence();
            return;
        }

        if (floor > CurrentFloor || (State == ElevatorState.Idle && floor > CurrentFloor))
        {
            upQueue.Add(floor);
        }
        else if (floor < CurrentFloor || (State == ElevatorState.Idle && floor < CurrentFloor))
        {
            downQueue.Add(floor);
        }
        else
        {
            // Same floor but not idle — add to appropriate queue based on direction
            if (Direction == ElevatorState.MovingUp)
                upQueue.Add(floor);
            else
                downQueue.Add(floor);
        }

        if (State == ElevatorState.Idle)
        {
            ProcessNextRequest();
        }
    }

    /// <summary>
    /// Check if this elevator already has a request for the given floor.
    /// </summary>
    public bool HasRequestForFloor(int floor)
    {
        return upQueue.Contains(floor) || downQueue.Contains(floor) ||
               (isMoving && targetFloor == floor);
    }

    /// <summary>
    /// Calculate the cost (distance) for this elevator to service a floor request.
    /// Lower cost = better candidate. Returns -1 if elevator is full/busy.
    /// </summary>
    public float GetCostToFloor(int floor)
    {
        if (State == ElevatorState.Idle)
        {
            return Mathf.Abs(floor - CurrentFloor);
        }

        // If elevator is moving toward the requested floor in the same direction, low cost
        if (State == ElevatorState.MovingUp && floor >= CurrentFloor)
        {
            return floor - CurrentFloor;
        }
        if (State == ElevatorState.MovingDown && floor <= CurrentFloor)
        {
            return CurrentFloor - floor;
        }

        // Elevator is moving away — higher cost (will need to reverse)
        int maxFloor = totalFloors - 1;
        if (State == ElevatorState.MovingUp)
        {
            // Must go up to highest request, then come back down
            int highest = upQueue.Count > 0 ? upQueue.Max : CurrentFloor;
            return (highest - CurrentFloor) + (highest - floor) + 5; // +5 penalty
        }
        else
        {
            int lowest = downQueue.Count > 0 ? downQueue.Min : CurrentFloor;
            return (CurrentFloor - lowest) + (floor - lowest) + 5;
        }
    }

    /// <summary>
    /// Returns the total number of pending requests.
    /// </summary>
    public int PendingRequestCount()
    {
        return upQueue.Count + downQueue.Count;
    }

    private void ProcessNextRequest()
    {
        int nextFloor = -1;

        // Continue in current direction if possible
        if (Direction == ElevatorState.MovingUp && upQueue.Count > 0)
        {
            nextFloor = upQueue.Min;
            upQueue.Remove(nextFloor);
        }
        else if (Direction == ElevatorState.MovingDown && downQueue.Count > 0)
        {
            nextFloor = downQueue.Max; // Max of reverse-sorted = smallest value... 
            // Actually downQueue is reverse sorted, so "Max" in SortedSet terms is first element
            // Let me get the first element
            foreach (int f in downQueue) { nextFloor = f; break; }
            downQueue.Remove(nextFloor);
        }
        else if (upQueue.Count > 0)
        {
            // Switch direction to up
            nextFloor = upQueue.Min;
            upQueue.Remove(nextFloor);
        }
        else if (downQueue.Count > 0)
        {
            foreach (int f in downQueue) { nextFloor = f; break; }
            downQueue.Remove(nextFloor);
        }

        if (nextFloor >= 0 && nextFloor != CurrentFloor)
        {
            targetFloor = nextFloor;
            targetY = targetFloor * floorHeight;
            isMoving = true;

            if (targetFloor > CurrentFloor)
            {
                State = ElevatorState.MovingUp;
                Direction = ElevatorState.MovingUp;
            }
            else
            {
                State = ElevatorState.MovingDown;
                Direction = ElevatorState.MovingDown;
            }
        }
        else
        {
            State = ElevatorState.Idle;
            Direction = ElevatorState.Idle;
            isMoving = false;
        }
    }

    private void MoveToTarget()
    {
        Vector3 pos = transform.position;
        float step = speed * Time.deltaTime;

        if (State == ElevatorState.MovingUp)
        {
            pos.y = Mathf.MoveTowards(pos.y, targetY, step);
        }
        else
        {
            pos.y = Mathf.MoveTowards(pos.y, targetY, step);
        }

        transform.position = pos;

        // Update current floor based on position (snap when close)
        int nearestFloor = Mathf.RoundToInt(pos.y / floorHeight);
        nearestFloor = Mathf.Clamp(nearestFloor, 0, totalFloors - 1);

        if (Mathf.Abs(pos.y - targetY) < 0.01f)
        {
            // Arrived at target floor
            pos.y = targetY;
            transform.position = pos;
            CurrentFloor = targetFloor;
            isMoving = false;

            // Check if there are intermediate stops in current direction
            StartDoorSequence();
        }
        else
        {
            // Check for intermediate stops
            float floorY = nearestFloor * floorHeight;
            if (Mathf.Abs(pos.y - floorY) < 0.05f)
            {
                if (State == ElevatorState.MovingUp && upQueue.Contains(nearestFloor))
                {
                    // Stop at this intermediate floor
                    upQueue.Remove(nearestFloor);
                    pos.y = floorY;
                    transform.position = pos;
                    CurrentFloor = nearestFloor;
                    StartDoorSequence();
                }
                else if (State == ElevatorState.MovingDown && downQueue.Contains(nearestFloor))
                {
                    downQueue.Remove(nearestFloor);
                    pos.y = floorY;
                    transform.position = pos;
                    CurrentFloor = nearestFloor;
                    StartDoorSequence();
                }
            }
        }
    }

    private void StartDoorSequence()
    {
        State = ElevatorState.DoorsOpening;
        doorOpenAmount = 0f;
        doorTimer = doorOpenDuration;

        // Notify the manager that this floor has been serviced
        ElevatorManager.Instance?.OnFloorServiced(CurrentFloor);
    }

    private void AnimateDoors(bool opening)
    {
        float doorSpeed = 2.0f * Time.deltaTime;

        if (opening)
        {
            doorOpenAmount = Mathf.MoveTowards(doorOpenAmount, 1f, doorSpeed);
            if (doorOpenAmount >= 1f)
            {
                State = ElevatorState.DoorsOpen;
                doorTimer = doorOpenDuration;
            }
        }
        else
        {
            doorOpenAmount = Mathf.MoveTowards(doorOpenAmount, 0f, doorSpeed);
            if (doorOpenAmount <= 0f)
            {
                doorOpenAmount = 0f;
                State = ElevatorState.Idle;
                ProcessNextRequest();
            }
        }

        // Animate door positions
        if (doorLeftRenderer != null && doorRightRenderer != null)
        {
            float offset = doorOpenAmount * doorMaxOffset;
            doorLeftRenderer.transform.localPosition = doorLeftClosed + Vector3.left * offset;
            doorRightRenderer.transform.localPosition = doorRightClosed + Vector3.right * offset;
        }
    }

    private void UpdateVisuals()
    {
        string floorName = CurrentFloor < floorNames.Length ? floorNames[CurrentFloor] : CurrentFloor.ToString();

        // Update cabin color based on state
        if (cabinRenderer != null)
        {
            switch (State)
            {
                case ElevatorState.Idle:
                    cabinRenderer.color = new Color(0.7f, 0.75f, 0.8f);
                    break;
                case ElevatorState.MovingUp:
                case ElevatorState.MovingDown:
                    cabinRenderer.color = new Color(0.6f, 0.8f, 0.6f);
                    break;
                case ElevatorState.DoorsOpening:
                case ElevatorState.DoorsOpen:
                case ElevatorState.DoorsClosing:
                    cabinRenderer.color = new Color(0.8f, 0.85f, 0.6f);
                    break;
            }
        }

        // Update 3D labels on the cabin
        if (floorLabel != null)
        {
            floorLabel.text = floorName;
        }

        if (stateLabel != null)
        {
            stateLabel.text = GetStateShortText();
        }

        // Update UI panel
        if (uiFloorText != null)
        {
            uiFloorText.text = $"Floor: {floorName}";
        }

        if (uiStateText != null)
        {
            uiStateText.text = GetStateDisplayText();
        }

        if (uiDirectionArrow != null)
        {
            switch (State)
            {
                case ElevatorState.MovingUp:
                    uiDirectionArrow.color = Color.green;
                    uiDirectionArrow.transform.rotation = Quaternion.Euler(0, 0, 0);
                    uiDirectionArrow.enabled = true;
                    break;
                case ElevatorState.MovingDown:
                    uiDirectionArrow.color = Color.red;
                    uiDirectionArrow.transform.rotation = Quaternion.Euler(0, 0, 180);
                    uiDirectionArrow.enabled = true;
                    break;
                default:
                    uiDirectionArrow.enabled = false;
                    break;
            }
        }
    }

    private string GetStateShortText()
    {
        return State switch
        {
            ElevatorState.Idle => "IDLE",
            ElevatorState.MovingUp => "▲",
            ElevatorState.MovingDown => "▼",
            ElevatorState.DoorsOpening => "◄►",
            ElevatorState.DoorsOpen => "[ ]",
            ElevatorState.DoorsClosing => "►◄",
            _ => ""
        };
    }

    private string GetStateDisplayText()
    {
        return State switch
        {
            ElevatorState.Idle => "Idle",
            ElevatorState.MovingUp => "Going Up",
            ElevatorState.MovingDown => "Going Down",
            ElevatorState.DoorsOpening => "Doors Opening",
            ElevatorState.DoorsOpen => "Doors Open",
            ElevatorState.DoorsClosing => "Doors Closing",
            _ => ""
        };
    }

    // Debug visualization in Scene view
    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        for (int i = 0; i < totalFloors; i++)
        {
            float y = i * floorHeight;
            Gizmos.DrawLine(
                new Vector3(transform.position.x - 0.5f, y, 0),
                new Vector3(transform.position.x + 0.5f, y, 0)
            );
        }
    }
}
