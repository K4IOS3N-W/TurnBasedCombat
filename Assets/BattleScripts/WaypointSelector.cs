using UnityEngine;

public class WaypointSelector : MonoBehaviour
{
    [Header("If true, clicks in the Scene View → set waypoint. Otherwise, use playerCamera")]
    public bool useSceneCamera = true;

    [Tooltip("Game/Player camera (used when useSceneCamera == false)")]
    public Camera playerCamera;

    [Tooltip("The script that holds 'public Transform waypoint;'")]
    public playerMovement mover;

    void Update()
    {
        // If useSceneCamera == false (Game View raycast):
        if (Application.isPlaying && !useSceneCamera)
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (playerCamera == null || mover == null) return;

                Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    if (hit.collider.CompareTag("Waypoints"))
                    {
                        // 1) Tell the clicked waypoint to disable itself & enable neighbors
                        WaypointBehaviour wp = hit.collider.GetComponent<WaypointBehaviour>();
                        if (wp != null)
                            wp.OnSelected();

                        // 2) Update the NavMeshAgent's target
                        mover.waypoint = hit.collider.transform;
                    }
                }
            }
        }
    }

#if UNITY_EDITOR
    // If useSceneCamera == true, this intercepts Scene View clicks:
    void OnEnable()
    {
        UnityEditor.SceneView.duringSceneGui += OnSceneGUI;
    }

    void OnDisable()
    {
        UnityEditor.SceneView.duringSceneGui -= OnSceneGUI;
    }

    private void OnSceneGUI(UnityEditor.SceneView sceneView)
    {
        if (!Application.isPlaying || !useSceneCamera) return;

        Event e = Event.current;
        if (e.type == EventType.MouseDown && e.button == 0)
        {
            Vector2 mousePos = e.mousePosition;
            Camera cam = sceneView.camera;
            Vector3 worldPos = cam.ScreenToWorldPoint(new Vector3(mousePos.x, cam.pixelHeight - mousePos.y, cam.nearClipPlane));

            Ray ray = cam.ScreenPointToRay(new Vector3(mousePos.x, cam.pixelHeight - mousePos.y, 0));
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.collider.CompareTag("Waypoints") && mover != null)
                {
                    WaypointBehaviour wp = hit.collider.GetComponent<WaypointBehaviour>();
                    if (wp != null)
                        wp.OnSelected();

                    mover.waypoint = hit.collider.transform;
                    e.Use(); // Consume the event
                }
            }
        }
    }
#endif
}
