using UnityEngine;
using BattleSystem.Client; // Make sure this is included
using BattleSystem.Map;

public class TeamCameraController : MonoBehaviour
{
    [Header("Camera Settings")]
    [SerializeField] private float followSpeed = 5f;
    [SerializeField] private float zoomSpeed = 2f;
    [SerializeField] private float minZoom = 3f;
    [SerializeField] private float maxZoom = 15f;
    [SerializeField] private Vector3 cameraOffset = new Vector3(0, 10, -8);
    
    [Header("Movement Settings")]
    [SerializeField] private float panSpeed = 3f;
    [SerializeField] private bool allowPanning = true;
    [SerializeField] private Vector2 panBounds = new Vector2(20f, 20f);
    
    [Header("Team Focus")]
    [SerializeField] private bool autoFollowTeam = true;
    [SerializeField] private float focusTransitionSpeed = 2f;
    
    private Camera cam;
    private BattleSystem.Client.GameClient gameClient; // Use the correct GameClient type
    private MapManager mapManager;
    private Vector3 targetPosition;
    private float targetZoom;
    private bool isFollowingTeam = true;
    private Vector3 initialPosition;
    private string currentTeamPosition = "start";
    
    // Input handling
    private Vector3 lastMousePosition;
    private bool isDragging = false;
    
    private void Start()
    {
        cam = GetComponent<Camera>();
        gameClient = FindObjectOfType<BattleSystem.Client.GameClient>(); // Use the correct type
        mapManager = FindObjectOfType<MapManager>();
        
        initialPosition = transform.position;
        targetPosition = transform.position;
        targetZoom = cam.orthographicSize;
        
        // Subscribe to game client events
        if (gameClient != null)
        {
            gameClient.OnTurnChanged += OnTurnChanged;
            gameClient.OnGameStarted += OnGameStarted;
        }
        
        // Focus on team's starting position
        FocusOnTeamPosition();
    }
    
    private void Update()
    {
        HandleInput();
        UpdateCameraPosition();
        UpdateCameraZoom();
        HandleWaypointClicks();
    }
    
    private void HandleInput()
    {
        // Mouse wheel zoom
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            targetZoom -= scroll * zoomSpeed;
            targetZoom = Mathf.Clamp(targetZoom, minZoom, maxZoom);
            isFollowingTeam = false;
        }
        
        // Pan with right mouse button or when not following team
        if (allowPanning)
        {
            if (Input.GetMouseButtonDown(1) || (Input.GetMouseButtonDown(0) && !IsOverWaypoint()))
            {
                lastMousePosition = Input.mousePosition;
                isDragging = true;
                isFollowingTeam = false;
            }
            
            if (Input.GetMouseButtonUp(1) || Input.GetMouseButtonUp(0))
            {
                isDragging = false;
            }
            
            if (isDragging)
            {
                Vector3 deltaMousePosition = Input.mousePosition - lastMousePosition;
                Vector3 worldDelta = cam.ScreenToWorldPoint(new Vector3(deltaMousePosition.x, deltaMousePosition.y, cam.nearClipPlane));
                targetPosition -= new Vector3(worldDelta.x, 0, worldDelta.z) * panSpeed;
                
                // Clamp within bounds
                targetPosition.x = Mathf.Clamp(targetPosition.x, -panBounds.x, panBounds.x);
                targetPosition.z = Mathf.Clamp(targetPosition.z, -panBounds.y, panBounds.y);
                
                lastMousePosition = Input.mousePosition;
            }
        }
        
        // Keyboard controls
        Vector3 keyboardInput = Vector3.zero;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            keyboardInput.z += 1;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            keyboardInput.z -= 1;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            keyboardInput.x -= 1;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            keyboardInput.x += 1;
            
        if (keyboardInput != Vector3.zero)
        {
            targetPosition += keyboardInput.normalized * panSpeed * Time.deltaTime;
            targetPosition.x = Mathf.Clamp(targetPosition.x, -panBounds.x, panBounds.x);
            targetPosition.z = Mathf.Clamp(targetPosition.z, -panBounds.y, panBounds.y);
            isFollowingTeam = false;
        }
        
        // Return to team focus
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.F))
        {
            FocusOnTeamPosition();
        }
    }
    
    private void HandleWaypointClicks()
    {
        if (Input.GetMouseButtonDown(0) && !isDragging)
        {
            // Only allow waypoint clicks if it's the player's turn and they're a team leader
            if (gameClient != null && gameClient.IsTeamLeader && CanPlayerMove())
            {
                Ray ray = cam.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;
                
                if (Physics.Raycast(ray, out hit))
                {
                    // Check for WaypointController (your existing system)
                    WaypointController waypointController = hit.collider.GetComponent<WaypointController>();
                    if (waypointController != null)
                    {
                        Debug.Log($"Clicked waypoint: {waypointController.WaypointId}");
                        gameClient.MoveToWaypoint(waypointController.WaypointId);
                        return;
                    }
                    
                    // Check for Waypoint (Map system)
                    var waypoint = hit.collider.GetComponent<Waypoint>();
                    if (waypoint != null && waypoint.IsAvailable)
                    {
                        Debug.Log($"Clicked map waypoint: {waypoint.WaypointId}");
                        gameClient.MoveToWaypoint(waypoint.WaypointId);
                        return;
                    }
                    
                    // Check for WaypointBehaviour
                    WaypointBehaviour waypointBehaviour = hit.collider.GetComponent<WaypointBehaviour>();
                    if (waypointBehaviour != null)
                    {
                        Debug.Log($"Clicked waypoint behaviour: {waypointBehaviour.waypointId}");
                        gameClient.MoveToWaypoint(waypointBehaviour.waypointId);
                        return;
                    }
                }
            }
        }
    }
    
    private bool IsOverWaypoint()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit))
        {
            return hit.collider.GetComponent<WaypointController>() != null ||
                   hit.collider.GetComponent<Waypoint>() != null ||
                   hit.collider.GetComponent<WaypointBehaviour>() != null;
        }
        return false;
    }
    
    private bool CanPlayerMove()
    {
        // Check if it's the player's turn (you'll need to implement this based on your game state)
        return gameClient != null && gameClient.IsConnected;
    }
    
    private void UpdateCameraPosition()
    {
        if (autoFollowTeam && isFollowingTeam)
        {
            Vector3 teamWorldPosition = GetTeamWorldPosition();
            targetPosition = teamWorldPosition + cameraOffset;
        }
        
        transform.position = Vector3.Lerp(transform.position, targetPosition, followSpeed * Time.deltaTime);
    }
    
    private void UpdateCameraZoom()
    {
        cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetZoom, zoomSpeed * Time.deltaTime);
    }
    
    private Vector3 GetTeamWorldPosition()
    {
        if (gameClient == null) return initialPosition;
        
        // Try to get position from MapManager first
        if (mapManager != null)
        {
            Vector3 position = mapManager.GetWaypointPosition(currentTeamPosition);
            if (position != Vector3.zero)
                return position;
        }
        
        // Fallback: find waypoint by ID
        WaypointController[] waypoints = FindObjectsOfType<WaypointController>();
        foreach (var waypoint in waypoints)
        {
            if (waypoint.WaypointId == currentTeamPosition)
            {
                return waypoint.transform.position;
            }
        }
        
        // Fallback: find waypoint behaviour
        WaypointBehaviour[] waypointBehaviours = FindObjectsOfType<WaypointBehaviour>();
        foreach (var waypoint in waypointBehaviours)
        {
            if (waypoint.waypointId == currentTeamPosition)
            {
                return waypoint.transform.position;
            }
        }
        
        return initialPosition;
    }
    
    public void FocusOnTeamPosition()
    {
        isFollowingTeam = true;
        targetZoom = (minZoom + maxZoom) * 0.4f; // Nice default zoom level
    }
    
    public void FocusOnWaypoint(string waypointId)
    {
        Vector3 waypointPosition = Vector3.zero;
        
        // Try MapManager first
        if (mapManager != null)
        {
            waypointPosition = mapManager.GetWaypointPosition(waypointId);
        }
        
        // Fallback methods
        if (waypointPosition == Vector3.zero)
        {
            WaypointController waypoint = FindWaypointController(waypointId);
            if (waypoint != null)
                waypointPosition = waypoint.transform.position;
        }
        
        if (waypointPosition != Vector3.zero)
        {
            targetPosition = waypointPosition + cameraOffset;
            isFollowingTeam = false;
        }
    }
    
    private WaypointController FindWaypointController(string waypointId)
    {
        WaypointController[] waypoints = FindObjectsOfType<WaypointController>();
        foreach (var waypoint in waypoints)
        {
            if (waypoint.WaypointId == waypointId)
                return waypoint;
        }
        return null;
    }
    
    public void SetPanBounds(Vector2 bounds)
    {
        panBounds = bounds;
    }
    
    public void EnableAutoFollow(bool enable)
    {
        autoFollowTeam = enable;
        isFollowingTeam = enable;
    }
    
    // Event handlers
    private void OnTurnChanged(string currentTeam, bool isMyTurn)
    {
        if (isMyTurn && autoFollowTeam)
        {
            FocusOnTeamPosition();
        }
    }
    
    private void OnGameStarted()
    {
        currentTeamPosition = "start";
        FocusOnTeamPosition();
    }
    
    // Public methods for UI
    public void ZoomIn()
    {
        targetZoom = Mathf.Max(minZoom, targetZoom - 2f);
    }
    
    public void ZoomOut()
    {
        targetZoom = Mathf.Min(maxZoom, targetZoom + 2f);
    }
    
    public void ResetCamera()
    {
        targetPosition = initialPosition;
        targetZoom = (minZoom + maxZoom) * 0.5f;
        isFollowingTeam = false;
    }
    
    private void OnDestroy()
    {
        if (gameClient != null)
        {
            gameClient.OnTurnChanged -= OnTurnChanged;
            gameClient.OnGameStarted -= OnGameStarted;
        }
    }
}