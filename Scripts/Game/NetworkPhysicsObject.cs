using UnityEngine;
using Mirror;

public class NetworkPhysicsObject : NetworkBehaviour
{
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        if (!isServer)
        {
            //Disable physics locally on all clients
            //Rely on server replication only
            rb.isKinematic = true;
        }
    }

}
