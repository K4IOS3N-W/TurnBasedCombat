using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaypointController : MonoBehaviour
{
    [SerializeField] private string waypointId;
    [SerializeField] private Material highlightMaterial;
    [SerializeField] private Material normalMaterial;
    
    private Renderer waypointRenderer;
    private BattleSystem.Client.GameClient gameClient;
    
    public string WaypointId => waypointId;
    
    private void Start()
    {
        waypointRenderer = GetComponent<Renderer>();
        gameClient = FindObjectOfType<BattleSystem.Client.GameClient>();
        waypointRenderer.material = normalMaterial;
    }
    
    private void OnMouseEnter()
    {
        if (CanInteract())
        {
            waypointRenderer.material = highlightMaterial;
        }
    }
    
    private void OnMouseExit()
    {
        waypointRenderer.material = normalMaterial;
    }
    
    private void OnMouseDown()
    {
        if (CanInteract())
        {
            gameClient?.MoveToWaypoint(waypointId);
        }
    }
    
    private bool CanInteract()
    {
        // Check if it's the player's turn and they can move
        return gameClient != null && gameClient.IsConnected;
    }
}