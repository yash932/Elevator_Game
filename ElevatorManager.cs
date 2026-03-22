using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Central manager that dispatches floor requests to the best available elevator.
/// Implements the Nearest Car algorithm with direction awareness.
/// Attach to an empty GameObject in the scene.
/// </summary>
public class ElevatorManager : MonoBehaviour
{
    public static ElevatorManager Instance { get; private set; }

    [Header("Elevators")]
    public Elevator[] elevators;

    [Header("Configuration")]
    public int totalFloors = 4;

    // Track which floors have active (unserviced) requests
    private HashSet<int> activeRequests = new HashSet<int>();

    // Event for UI to listen to
    public System.Action<int, bool> OnFloorRequestChanged; // (floor, isActive)

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// Called by floor buttons. Dispatches the request to the best elevator.
    /// </summary>
    public void RequestElevator(int floor)
    {
        if (floor < 0 || floor >= totalFloors)
        {
            Debug.LogWarning($"Invalid floor request: {floor}");
            return;
        }

        // Check if any elevator is already at this floor and idle
        foreach (var elevator in elevators)
        {
            if (elevator.CurrentFloor == floor && elevator.State == ElevatorState.Idle)
            {
                elevator.AddRequest(floor); // Will just open doors
                return;
            }
        }

        // Check if this floor is already being serviced
        foreach (var elevator in elevators)
        {
            if (elevator.HasRequestForFloor(floor))
            {
                Debug.Log($"Floor {floor} already in queue for Elevator {elevator.elevatorId}");
                return;
            }
        }

        // Find the best elevator using cost function
        Elevator bestElevator = FindBestElevator(floor);

        if (bestElevator != null)
        {
            bestElevator.AddRequest(floor);
            activeRequests.Add(floor);
            OnFloorRequestChanged?.Invoke(floor, true);

            Debug.Log($"Floor {floor} assigned to Elevator {bestElevator.elevatorId} " +
                      $"(cost: {bestElevator.GetCostToFloor(floor):F1})");
        }
        else
        {
            Debug.LogWarning($"No elevator available for floor {floor}");
        }
    }

    /// <summary>
    /// Called by Elevator when it arrives at a floor and opens doors.
    /// </summary>
    public void OnFloorServiced(int floor)
    {
        activeRequests.Remove(floor);
        OnFloorRequestChanged?.Invoke(floor, false);
    }

    /// <summary>
    /// Check if a floor currently has an active request.
    /// </summary>
    public bool IsFloorRequested(int floor)
    {
        return activeRequests.Contains(floor);
    }

    /// <summary>
    /// Find the best elevator to handle a floor request.
    /// Uses a cost function that considers:
    /// - Distance to the requested floor
    /// - Current direction of travel
    /// - Number of pending requests
    /// </summary>
    private Elevator FindBestElevator(int requestedFloor)
    {
        Elevator best = null;
        float bestCost = float.MaxValue;

        foreach (var elevator in elevators)
        {
            float cost = elevator.GetCostToFloor(requestedFloor);

            // Add small penalty for elevators with many pending requests (load balancing)
            cost += elevator.PendingRequestCount() * 0.5f;

            // Prefer idle elevators slightly
            if (elevator.State == ElevatorState.Idle)
            {
                cost -= 0.1f;
            }

            if (cost < bestCost)
            {
                bestCost = cost;
                best = elevator;
            }
        }

        return best;
    }

    // Debug: show all active requests
    void OnGUI()
    {
        if (activeRequests.Count > 0)
        {
            string requests = "Active Requests: ";
            foreach (int floor in activeRequests)
            {
                requests += floor + " ";
            }
            GUI.Label(new Rect(10, 10, 300, 25), requests);
        }
    }
}
