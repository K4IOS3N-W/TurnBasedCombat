using UnityEngine;
using UnityEngine.AI;
using BattleSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 3.5f;
    public float rotationSpeed = 10f;
    
    [Header("Visual")]
    public Animator animator;
    public Transform visualModel;
    
    [Header("Team Management")]
    public string teamId;
    
    private NavMeshAgent navAgent;
    private Camera playerCamera;
    private WaypointBehaviour currentWaypoint;
    private bool isInBattle = false;
    private TurnManager turnManager;
    
    // Animation parameter hashes for performance
    private int moveSpeedHash;
    private int isMovingHash;
    private int inBattleHash;
    
    void Start()
    {
        InitializeComponents();
        SetupAnimationHashes();
        FindCurrentWaypoint();
    }
    
    void Update()
    {
        if (!isInBattle && CanMove())
        {
            HandleMovementInput();
        }
        
        UpdateAnimations();
    }
    
    private void InitializeComponents()
    {
        navAgent = GetComponent<NavMeshAgent>();
        if (navAgent == null)
        {
            navAgent = gameObject.AddComponent<NavMeshAgent>();
        }
        
        navAgent.speed = moveSpeed;
        navAgent.angularSpeed = rotationSpeed * 50f; // Convert to degrees/second
        
        playerCamera = Camera.main;
        turnManager = FindObjectOfType<TurnManager>();
        
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
    }
    
    private void SetupAnimationHashes()
    {
        if (animator != null)
        {
            moveSpeedHash = Animator.StringToHash("MoveSpeed");
            isMovingHash = Animator.StringToHash("IsMoving");
            inBattleHash = Animator.StringToHash("InBattle");
        }
    }
    
    private void HandleMovementInput()
    {
        // Raycast for waypoint selection
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
            
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.collider.CompareTag("Waypoints"))
                {
                    TryMoveToWaypoint(hit.collider.GetComponent<WaypointBehaviour>());
                }
            }
        }
        
        // Alternative: Keyboard movement for direct control (optional)
        HandleKeyboardMovement();
    }
    
    private void HandleKeyboardMovement()
    {
        Vector3 inputDirection = Vector3.zero;
        
        if (Input.GetKey(KeyCode.W)) inputDirection += Vector3.forward;
        if (Input.GetKey(KeyCode.S)) inputDirection += Vector3.back;
        if (Input.GetKey(KeyCode.A)) inputDirection += Vector3.left;
        if (Input.GetKey(KeyCode.D)) inputDirection += Vector3.right;
        
        if (inputDirection != Vector3.zero)
        {
            // Convert to world space based on camera
            Vector3 worldDirection = playerCamera.transform.TransformDirection(inputDirection);
            worldDirection.y = 0; // Keep movement on ground plane
            worldDirection.Normalize();
            
            // Move with NavMeshAgent
            Vector3 targetPosition = transform.position + worldDirection * moveSpeed * Time.deltaTime;
            navAgent.SetDestination(targetPosition);
        }
    }
    
    private bool CanMove()
    {
        // Check if it's this team's turn
        if (turnManager != null && !turnManager.IsCurrentTeam(teamId))
        {
            return false;
        }
        
        // Check if not in battle
        return !isInBattle;
    }
    
    private void TryMoveToWaypoint(WaypointBehaviour targetWaypoint)
    {
        if (targetWaypoint == null || targetWaypoint.inBattle)
        {
            Debug.Log("Cannot move to waypoint - battle in progress or invalid waypoint");
            return;
        }
        
        // Move to waypoint
        navAgent.SetDestination(targetWaypoint.transform.position);
        
        // Update current waypoint when we reach it
        StartCoroutine(CheckWaypointReached(targetWaypoint));
    }
    
    private System.Collections.IEnumerator CheckWaypointReached(WaypointBehaviour targetWaypoint)
    {
        while (Vector3.Distance(transform.position, targetWaypoint.transform.position) > 1f)
        {
            yield return null;
        }
        
        // Trigger waypoint selection
        targetWaypoint.OnSelected();
        currentWaypoint = targetWaypoint;
        
        // Check if battle started
        if (targetWaypoint.inBattle)
        {
            EnterBattle();
        }
    }
    
    private void FindCurrentWaypoint()
    {
        var allWaypoints = FindObjectsOfType<WaypointBehaviour>();
        
        foreach (var waypoint in allWaypoints)
        {
            if (waypoint.teamsInWaypoint.Contains(teamId))
            {
                currentWaypoint = waypoint;
                transform.position = waypoint.transform.position;
                break;
            }
        }
    }
    
    private void EnterBattle()
    {
        isInBattle = true;
        navAgent.enabled = false; // Disable movement during battle
        
        // Optional: Change to battle stance animation
        if (animator != null)
        {
            animator.SetBool(inBattleHash, true);
        }
        
        Debug.Log($"Player {teamId} entered battle!");
    }
    
    public void ExitBattle()
    {
        isInBattle = false;
        navAgent.enabled = true; // Re-enable movement
        
        if (animator != null)
        {
            animator.SetBool(inBattleHash, false);
        }
        
        Debug.Log($"Player {teamId} exited battle!");
    }
    
    private void UpdateAnimations()
    {
        if (animator == null) return;
        
        // Update movement animations
        float currentSpeed = navAgent.velocity.magnitude;
        bool isMoving = currentSpeed > 0.1f;
        
        animator.SetFloat(moveSpeedHash, currentSpeed);
        animator.SetBool(isMovingHash, isMoving);
        
        // Rotate model to face movement direction
        if (isMoving && visualModel != null)
        {
            Vector3 lookDirection = navAgent.velocity.normalized;
            if (lookDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                visualModel.rotation = Quaternion.Slerp(visualModel.rotation, targetRotation, 
                    rotationSpeed * Time.deltaTime);
            }
        }
    }
    
    // Public methods for external control
    public void SetTeamId(string newTeamId)
    {
        teamId = newTeamId;
    }
    
    public void MoveTo(Vector3 position)
    {
        if (navAgent.enabled)
        {
            navAgent.SetDestination(position);
        }
    }
    
    public void StopMovement()
    {
        if (navAgent.enabled)
        {
            navAgent.ResetPath();
        }
    }
    
    void OnDrawGizmosSelected()
    {
        // Visualize current waypoint connection
        if (currentWaypoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, currentWaypoint.transform.position);
            Gizmos.DrawWireSphere(currentWaypoint.transform.position, 1f);
        }
        
        // Show NavMesh path
        if (navAgent != null && navAgent.hasPath)
        {
            Gizmos.color = Color.yellow;
            Vector3[] pathCorners = navAgent.path.corners;
            for (int i = 0; i < pathCorners.Length - 1; i++)
            {
                Gizmos.DrawLine(pathCorners[i], pathCorners[i + 1]);
            }
        }
    }
}