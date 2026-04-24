using UnityEngine;
using Mirror;

public class Weapon : NetworkBehaviour
{
    public GameObject playerObjectRef;
    public GameObject shotImpact_prefab;
    public AudioClip shoot_audio;

    public void Shoot(Vector3 shootFromPoint, Vector3 shootDir)
    {
        int origLayer = playerObjectRef.layer;

        //Ignore self shooting on the player
        playerObjectRef.layer = LayerMask.NameToLayer("Ignore Raycast");

        RaycastHit hit;
        if (Physics.Raycast(shootFromPoint, shootDir, out hit, 10f))
        {
            //If hit object is another player, will be applying damage...
            if(hit.transform.GetComponent<playerGameController>() != null)
            {
                //runs on server only, and the "health" amount will sync thanks to SyncVar
                hit.transform.GetComponent<playerGameController>().Srv_UpdateHealthDamage(-1);
            }

            Rpc_SpawnAtShotHit(hit.point);
        }

        playerObjectRef.layer = origLayer;
    }

    [ClientRpc] //Replicate this method call on ALL clients (incl. host)
    void Rpc_SpawnAtShotHit(Vector3 location)
    {
        GetComponent<AudioSource>().PlayOneShot(shoot_audio, 0.5f);
        GameObject impactObj = Instantiate(shotImpact_prefab, location, Quaternion.identity);

        Destroy(impactObj, 5f);
    }
}
