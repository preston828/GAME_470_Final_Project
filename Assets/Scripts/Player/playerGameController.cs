using UnityEngine;
using Mirror;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class playerGameController : NetworkBehaviour
{
    public GameObject playerModel;
    public Animator animator;
    public Camera playerCamera;
    public Weapon playerWeapon;
    public Text playerHealthText;
    public Text playerTeamText;

    [SyncVar (hook = nameof(OnCameraRotated))] private Quaternion cameraRotation;

    [SyncVar (hook = nameof(OnAnimSpeedChange))] private float animSpeed = 0f;

    [SyncVar(hook = nameof(OnHealthUpdate))] private int health = 10;

    [SyncVar(hook = nameof(OnRespawnUpdate))] private bool isRespawning = false;

    private int maxHealth = 10;
    private float speed = 3f;
    private float xRotation = 0f, yRotation = 0f;

    private bool spawnSetup = false;

    void Start()
    {
        playerModel.SetActive(false);
        playerCamera.gameObject.SetActive(false);
        playerHealthText.gameObject.SetActive(false);
        GetComponent<Rigidbody>().useGravity = false;
        Cursor.lockState = CursorLockMode.None;
    }

    // Update is called once per frame
    void Update()
    {
        //This script will only control the game in the Game Scene
        if (SceneManager.GetActiveScene().name.Equals("Game"))
        {
            if (!spawnSetup)
            {
                spawnSetup = true;
                Cursor.lockState = CursorLockMode.Locked;
                StartCoroutine(InitDelayed());
            }

            //Only control the "owned" player
            if (isLocalPlayer && isOwned && !isRespawning)
            {
                //Motion, actions, etc.
                CameraRotation();
                Movement();
                Actions();
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        //Collect Object (on the host)
        Cmd_OnCollect(other.gameObject);
    }

    [Command]
    private void Cmd_OnCollect(GameObject other)
    {
        if(other.GetComponent<HealthCollectable>() != null)
        {
            Srv_UpdateHealthDamage(other.GetComponent<HealthCollectable>().healthRecoveryAmount);
            NetworkServer.Destroy(other); //Don't use Unity's destroy if networked collectable

            GameObject.Find("gameMechanics").GetComponent<GameMechanics>().Rpc_OnHealthCollected();
        }
    }

    private IEnumerator InitDelayed()
    {
        playerCamera.gameObject.SetActive(isOwned);

        //Short Init delay to ensure network readiness
        yield return new WaitForSeconds(0.5f);

        GetComponent<Rigidbody>().useGravity = true;
        playerModel.SetActive(true);

        //Improve this random spawn logic (considering collision, game scene geometries...)
        transform.position = new Vector3(Random.Range(-10f, 10f), 2f, Random.Range(-10f, 10f));

        OnHealthUpdate(maxHealth, maxHealth);
        playerHealthText.gameObject.SetActive(isOwned);
    }

    private void Movement()
    {

        float movSpeed = speed;
        if (Input.GetKey(KeyCode.LeftShift))
        {
            movSpeed += 5; //Sprint
        }
        Vector3 moveDir = (playerCamera.transform.right * Input.GetAxis("Horizontal"))
            + (Vector3.Cross(playerCamera.transform.right, Vector3.up) * Input.GetAxis("Vertical")); //forward

        transform.position += moveDir.normalized * movSpeed * Time.deltaTime;

        float newSpeed = moveDir.normalized.magnitude;
        animator.SetFloat("Speed", newSpeed); //Local animator set
        Cmd_UpdateAnimSpeed(newSpeed); //On all clients
    }

    private void CameraRotation()
    {
        xRotation -= Input.GetAxis("Mouse Y") * 500f * Time.deltaTime;
        xRotation = Mathf.Clamp(xRotation, -70f, 70f);

        yRotation += Input.GetAxis("Mouse X") * 500f * Time.deltaTime;

        playerCamera.transform.localRotation = Quaternion.Euler(xRotation, yRotation, 0f);

        //weapon (locally)
        playerWeapon.transform.rotation = playerCamera.transform.rotation;

        //player model (locally)
        animator.transform.localEulerAngles = new Vector3(animator.transform.localEulerAngles.x,
            playerCamera.transform.localEulerAngles.y,
            animator.transform.localEulerAngles.z
            ); //Sync yaw only

        //sends command to host to request update to all clients
        Cmd_UpdateCameraRotation(playerCamera.transform.localRotation);
    }

    private IEnumerator RespawnCor()
    {
        Cmd_Respawning(true);
        Cursor.lockState = CursorLockMode.None;
        GetComponent<Rigidbody>().useGravity = false;
        playerHealthText.text = "GAME OVER! Respawning...";


        yield return new WaitForSeconds(4.5f);

        Cmd_Respawning(false);
        Cursor.lockState = CursorLockMode.Locked;
        GetComponent<Rigidbody>().useGravity = true;

        transform.position = new Vector3(Random.Range(-10f, 10f), 2f, Random.Range(-10f, 10f));

        yield return new WaitForSeconds(0.5f); //Delay allows for full sync across the network

        Cmd_SetHealth(maxHealth);
    }

    [Command]
    private void Cmd_UpdateCameraRotation(Quaternion rot)
    {
        cameraRotation = rot;
    }
    //SyncVar hook function for rotation
    private void OnCameraRotated(Quaternion oldRot, Quaternion newRot)
    {
        if (!isLocalPlayer)
        {
            //weapons...
            playerWeapon.transform.rotation = newRot;

            //player models
            animator.transform.localEulerAngles = new Vector3(animator.transform.localEulerAngles.x,
                newRot.eulerAngles.y,
                animator.transform.localEulerAngles.z
                ); //Sync yaw only
        }
    }

    [Command]
    private void Cmd_Shoot(Vector3 shootFromPoint, Vector3 shootDir)
    {
        playerWeapon.Shoot(shootFromPoint, shootDir);
    }

    private void Actions()
    {
        if (Input.GetMouseButtonDown(0))
        {
            //Send command to host to actually shoot from there then replicate on all clients
            Cmd_Shoot(playerWeapon.transform.position, playerWeapon.transform.forward);
        }
        else if (Input.GetKeyDown(KeyCode.L))
        {
            (CustomNetworkManager.singleton as CustomNetworkManager).QuitMatch();
        }else if (Input.GetKeyUp(KeyCode.E))
        {
            Debug.Log("Pressed E");
            TryInteract(playerCamera.transform.position, playerCamera.transform.forward);
        }
    }

    [Command]
    private void TryInteract(Vector3 playerPosSource, Vector3 playerLookDir)
    {
        Debug.Log("Start Try Interact");
        int origLayer = this.transform.gameObject.layer;

        //Ignore self shooting on the player
        this.transform.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");

        RaycastHit hit;
        if (Physics.Raycast(playerPosSource, playerLookDir, out hit, 5f))
        {
            //If hit object is another player, will be applying damage...
            if (hit.transform.GetComponent<CapsuleCollider>() != null)
            {
                
                if(hit.transform.GetComponent<DoorMechanics>() != null)
                {
                    Debug.Log("Hit Door");
                    hit.transform.GetComponent<DoorMechanics>().InteractWithDoor();
                }
                
            }
            else
            {
                Debug.Log("Hit something else. " + hit.transform.gameObject.name);
            }
        }

        this.transform.gameObject.layer = origLayer;
        Debug.Log("End Try Interact");
    }

    [Command]
    private void Cmd_UpdateAnimSpeed(float speed)
    {
        animSpeed = speed;
    }

    private void OnAnimSpeedChange(float oldSpeed, float newSpeed)
    {
        if (!isLocalPlayer)
        {
            animator.SetFloat("Speed", newSpeed);
        }
    }


    //Health & Respawning
    #region Health
    [Server]
    public void Srv_UpdateHealthDamage(int amount)
    {
        if (!isRespawning)
        {
            health += amount;
            health = Mathf.Max(health, 0); //prevents below zero
        }
    }

    [Server] //Only runs on the host
    private void Srv_SetHealth(int newHealth)
    {
        health = newHealth;
    }

    [Command]
    private void Cmd_SetHealth(int newHealth)
    {
        Srv_SetHealth(newHealth);
    }

    private void OnHealthUpdate(int oldHealth, int newHealth)
    {
        if (isLocalPlayer && !isRespawning)
        {
            //Update local UI (for local player only)
            playerHealthText.text = GetComponent<PlayerObjectController>().playerUsername + "'s health: " + health;

            //if died
            if(health <= 0)
            {
                StartCoroutine(RespawnCor());
            }
        }
    }

    //Respwaning
    [Command]
    private void Cmd_Respawning(bool value)
    {
        isRespawning = value;
    }

    private void OnRespawnUpdate(bool oldValue,  bool newValue)
    {
        if (!isLocalPlayer)
        {
            //Don't disable player model, bc maybe you want to play a "die" animation
            GetComponent<Rigidbody>().useGravity = !isRespawning;
            playerWeapon.gameObject.SetActive(!isRespawning); //Visualize the respawning event
        }
    }
    #endregion Health
}
