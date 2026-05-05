using UnityEngine;
using Mirror;

public class MakeGoal : NetworkBehaviour
{
    public PlayerObjectController.Team goalForTeam;
    public Transform ballSpawnPoint;

    private void OnTriggerEnter(Collider other)
    {
        if (!isServer) return;

        if(other.CompareTag("Ball"))
        {
            GameMechanics.Instance.AddGoal(goalForTeam);

            NetworkServer.Destroy(other.gameObject);
            GameMechanics.Instance.RespawnBall();
        }
    }
}
