using Mirror;
using UnityEngine;
using System.Collections;

public class DoorMechanics : NetworkBehaviour
{
   [SyncVar (hook = nameof(OnDoorInteraction))] private bool isDoorOpen = false;
    private float closedRotation = 0f;
    private float openRotation = -120f;
    public GameObject doorHinge;
    public GameObject doorObj;
    private float doorRotationSpeed = 120f;
    private float doorOpenTime = 1f;
    [SyncVar (hook = nameof(OnCanOpenChanged))] public bool canOpen = true;
    [SyncVar(hook = nameof(OnDoorHingeRotating))] public float doorHingeRotation = 0f;
    
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void InteractWithDoor()
    {
        if (canOpen && isServer)
        {
            Rpc_InteractWithDoor();
        }
    }

    //Local coroutine triggered from Rpc for smooth door motion
    private IEnumerator InteractWithDoorCor()
    {
        float time = 0f;
        
        canOpen = false;
        if (isDoorOpen)
        {
            Debug.Log("Door is open... closing");
            while (time <= doorOpenTime)
            {
                var rotateTo = Quaternion.Euler(new Vector3(0f, closedRotation, 0f));
                doorHinge.transform.rotation = Quaternion.RotateTowards(doorHinge.transform.rotation, rotateTo, doorRotationSpeed * Time.deltaTime);
                time += Time.deltaTime;
                yield return null;
            }
            doorHinge.transform.rotation = Quaternion.Euler(new Vector3(0f, closedRotation, 0f));
            isDoorOpen = false;
        }
        else
        {
            Debug.Log("Door is closed... opening");
            while (time <= doorOpenTime)
            {
                var rotateTo = Quaternion.Euler(new Vector3(0f, openRotation, 0f));
                doorHinge.transform.rotation = Quaternion.RotateTowards(doorHinge.transform.rotation, rotateTo, doorRotationSpeed * Time.deltaTime);
                /* yRotation += doorRotationSpeed * Time.deltaTime;
                yRotation = Mathf.Clamp(yRotation, openRotation, closedRotation);
                doorHinge.transform.rotation = Quaternion.Euler(0f, yRotation, 0f);*/
                time += Time.deltaTime;
                yield return null;
            }
            doorHinge.transform.rotation = Quaternion.Euler(new Vector3(0f, openRotation, 0f));
            isDoorOpen = true;
        }
        canOpen = true;
        Debug.Log("Interact Door done");
        yield break;
    }

    [ClientRpc] //Replicate this method call on ALL clients (incl. host)
    void Rpc_InteractWithDoor()
    {
        Debug.Log("Start Rpc_Interact With Door");
        
        StartCoroutine(InteractWithDoorCor());
        

        /*float yRotation = 0f;
        if (canOpen)
        {
            canOpen = false;
            if (isDoorOpen)
            {
                Debug.Log("Door is open... closing");
                yRotation = 0f;
                while(yRotation > openRotation)
                {
                    yRotation -= doorRotationSpeed * Time.deltaTime;
                    yRotation = Mathf.Clamp(yRotation, openRotation, closedRotation);
                    doorHinge.transform.rotation = Quaternion.Euler(0f, yRotation, 0f);
                    //doorHingeRotation = yRotation;
                    //doorHinge.transform.rotation = Quaternion.RotateTowards(doorHinge.transform.rotation, Quaternion.Euler(0f, openRotation, 0f), yRotation);
                }
            }
            else if (!isDoorOpen)
            {
                Debug.Log("Door is closed... opening");
                yRotation = -120f;
                while (yRotation < closedRotation)
                {
                    yRotation += doorRotationSpeed * Time.deltaTime;
                    yRotation = Mathf.Clamp(yRotation, openRotation, closedRotation);
                    doorHinge.transform.rotation = Quaternion.Euler(0f, yRotation, 0f);
                }
            }

            isDoorOpen = !isDoorOpen;
            canOpen = true;
        }*/
    }

    [Command]
    private void Cmd_DoorInteracting(bool value)
    {
        isDoorOpen = value;
    }

    private void OnDoorInteraction(bool oldValue, bool newValue)
    {
        if (isOwned)
        {
            Cmd_DoorInteracting(newValue);
        }
    }

    [Command]
    private void Cmd_DoorHingeRotating(float value)
    {
        doorHingeRotation = value;
        
    }

    private void OnDoorHingeRotating(float oldValue,  float newValue)
    {
        if (isOwned)
        {
            Cmd_DoorHingeRotating(newValue);
        }
    }

    [Command]
    private void Cmd_OnCanOpenChange(bool value)
    {
        canOpen = value;
    }

    private void OnCanOpenChanged(bool oldValue, bool newValue)
    {
        if (isOwned)
        {
            Cmd_OnCanOpenChange(newValue);
        }
    }
}
