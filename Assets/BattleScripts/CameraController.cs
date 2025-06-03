using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Target")]
    public Transform target; // The player or object to follow
    
    [Header("Camera Settings")]
    public float distance = 5f;
    public float height = 2f;
    public float rotationSpeed = 2f;
    public float followSpeed = 5f;
    
    [Header("Input")]
    public KeyCode orbitKey = KeyCode.Mouse1; // Right mouse button to orbit
    
    [Header("Limits")]
    public float minVerticalAngle = -30f;
    public float maxVerticalAngle = 60f;
    
    private float currentX = 0f;
    private float currentY = 0f;
    private Vector3 offset;
    
    void Start()
    {
        // Initialize camera position
        if (target != null)
        {
            // Set initial rotation based on current camera position
            Vector3 angles = transform.eulerAngles;
            currentX = angles.y;
            currentY = angles.x;
            
            // Calculate initial offset
            UpdateCameraPosition();
        }
    }
    
    void LateUpdate()
    {
        if (target == null) return;
        
        HandleInput();
        UpdateCameraPosition();
    }
    
    void HandleInput()
    {
        // Only rotate camera when holding the orbit key
        if (Input.GetKey(orbitKey))
        {
            currentX += Input.GetAxis("Mouse X") * rotationSpeed;
            currentY -= Input.GetAxis("Mouse Y") * rotationSpeed;
            
            // Clamp vertical rotation
            currentY = Mathf.Clamp(currentY, minVerticalAngle, maxVerticalAngle);
            
            // Hide cursor while orbiting
            Cursor.lockState = CursorLockMode.Locked;
        }
        else
        {
            // Show cursor when not orbiting
            Cursor.lockState = CursorLockMode.None;
        }
    }
    
    void UpdateCameraPosition()
    {
        // Calculate desired position based on angles
        Quaternion rotation = Quaternion.Euler(currentY, currentX, 0);
        Vector3 direction = new Vector3(0, height, -distance);
        Vector3 desiredPosition = target.position + rotation * direction;
        
        // Smoothly move to desired position
        transform.position = Vector3.Lerp(transform.position, desiredPosition, followSpeed * Time.deltaTime);
        
        // Look at target
        transform.LookAt(target.position + Vector3.up * height * 0.5f);
    }
    
    // Public method to set a new target
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
    
    // Public method to instantly snap to target (useful for scene transitions)
    public void SnapToTarget()
    {
        if (target != null)
        {
            UpdateCameraPosition();
            transform.position = target.position + offset;
        }
    }
}