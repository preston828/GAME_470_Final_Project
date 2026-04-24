using UnityEngine;
using Mirror;

public class HealthCollectable : NetworkBehaviour
{
    public int healthRecoveryAmount = 1;

    [ClientRpc]
    public void Rpc_SetParent() //Cannot pass the parent transform bia parameter because it is not a network identity
    {
        var collectableParent = GameObject.Find("collectables_container")?.transform;
        if(collectableParent != null )
        {
            transform.SetParent(collectableParent);
        }
    }
}
