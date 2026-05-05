using Mirror;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class GameMechanics : NetworkBehaviour
{
    public static GameMechanics Instance;

    [SyncVar] public int redScore = 0;
    [SyncVar] public int blueScore = 0;

    private void Awake()
    {
        Instance = this;
    }

    public Camera scene_camera;
    public Text numberOfHealthCollectedText;
    public Transform collectables;
    public HealthCollectable healthCollectable_prefab;
    public GameObject ballPrefab;
    public Transform ballSpawnPoint;

    public int numberOfHealthCollected = 0;

    private int health_spawned = 0;

    void Start()
    {
        LocalInit();

        //Server only procedures
        if (isServer)
        {
            StartCoroutine(Srv_SpawnHealthCoroutine());
        }
        
    }

    [Server]
    public void RespawnBall()
    {
        GameObject newBall = Instantiate(ballPrefab, ballSpawnPoint.position, Quaternion.identity);
        NetworkServer.Spawn(newBall);
    }
    [Server]
    public void AddGoal(PlayerObjectController.Team scoringTeam)
    {
        if (scoringTeam == PlayerObjectController.Team.Red)
        {
            redScore++;
        }
        else if (scoringTeam == PlayerObjectController.Team.Blue)
        {
            blueScore++;
        }

        RpcUpdateScoreUI(redScore, blueScore);
    }

    [ClientRpc]
    void RpcUpdateScoreUI(int red, int blue)
    {
        ScoreUI.Instance.UpdateScore(red, blue);
    }

    [Server]
    private IEnumerator Srv_SpawnHealthCoroutine()
    {
        yield return new WaitForSeconds(0.1f); //wait for network readiness

        while (true)
        {
            yield return new WaitForSeconds(1f); //Check once per second

            //Check how many health prefabs there are currently
            health_spawned = collectables.childCount;

            while(health_spawned < 3)
            {
                Vector3 ranLocation = new Vector3(Random.Range(-20, 20), 0.6f, Random.Range(-20, 20));

                //Only works locally (on the server)
                GameObject healthToAdd = Instantiate(healthCollectable_prefab, ranLocation, Quaternion.identity, collectables).gameObject;
                NetworkServer.Spawn(healthToAdd); //Spawn the object on the network (all clients)
                StartCoroutine(DelayedParentSet(healthToAdd));
                health_spawned++;
            }
        }
    }

    private IEnumerator DelayedParentSet(GameObject spawned)
    {
        yield return new WaitForSeconds(0.3f); //make sure clients spawned the object already

        var hc = spawned.GetComponent<HealthCollectable>();
        if(hc != null)
        {
            hc.Rpc_SetParent();
        }
    }

    public void LocalInit()
    {
        scene_camera.gameObject.SetActive(false);
        Debug.Log("LocalInit");
        numberOfHealthCollectedText.text = "HEALTH COLLECTED: 0";
    }

    [ClientRpc]
    public void Rpc_OnHealthCollected()
    {
        numberOfHealthCollected++;
        
        numberOfHealthCollectedText.text = "HEALTH COLLECTED: " + numberOfHealthCollected;
    }
}
