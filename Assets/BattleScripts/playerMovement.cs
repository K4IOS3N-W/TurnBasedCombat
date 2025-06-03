using UnityEngine;
using UnityEngine.AI;

public class playerMovement : MonoBehaviour
{
    public Transform waypoint; // Put Waypoint here 
    private NavMeshAgent agent;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        if (waypoint != null)
        {
            agent.SetDestination(waypoint.position);
        }
    }

    void Update()
    {
        if (waypoint != null && Vector3.Distance(agent.destination, waypoint.position) > 0.1f)
        {
            agent.SetDestination(waypoint.position);
        }
    }
}
