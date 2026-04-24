using UnityEngine;
using Steamworks;
using UnityEngine.UI;
using System.Linq;
using System.Collections.Generic;
//using UnityEditor.SearchService;

public class LobbyController : MonoBehaviour
{
    public static LobbyController Instance;

    //UI
    public Text lobbyNameText;
    public ulong currentLobbyId;

    
    public GameObject lobbyCanvasObject;
    public GameObject playerListItemPrefab;

    //Player
    public GameObject localPlayerObject;
    public PlayerObjectController localPlayerController;

    //Lobby Data
    public bool playerItemCreated = false;
    private List<SteamPlayerItem> playerListItemList = new List<SteamPlayerItem>();

    public Button startGameButton;
    public Button leaveLobbyButton;

    //Manager
    private CustomNetworkManager manager;
    private CustomNetworkManager Manager
    {
        get
        {
            if (manager != null) { return manager; }
            return manager = CustomNetworkManager.singleton as CustomNetworkManager;
        }
    }

    void Awake()
    {
        if(Instance == null) { Instance = this; }
    }

    public void UpdateLobbyName()
    {
        currentLobbyId = MainMenuSteamworksLobby.Instance.currentLobbyID;
        lobbyNameText.text = SteamMatchmaking.GetLobbyData(new CSteamID(currentLobbyId), MainMenuSteamworksLobby.nameKey);
        if (Input.GetKeyDown(KeyCode.L))
        {
            LeaveLobby();
        }
    }

    private void Update()
    {
        //ToDo -> add code input to leave lobby
    }

    public void UpdatePlayerList()
    {
        if (!playerItemCreated) //Host item (first element in the list)
        {
            CreateHostPlayerItem();
        }
        if(playerListItemList.Count < Manager.gamePlayers.Count) //Client just joined and is waiting to appear visually in the lobby
        {
            CreateClientPlayerItem();
        }
        if (playerListItemList.Count > Manager.gamePlayers.Count) //Client just left lobby, need to remove them visually
        {
            RemovePlayerItem();
        }
        if (playerListItemList.Count == Manager.gamePlayers.Count) //There was a client update which needs to be reflected in the list
        {
            UpdatePlayerItem();
        }
    }


    //Create Player Items
    public void CreateHostPlayerItem()
    {
        foreach(PlayerObjectController player in Manager.gamePlayers)
        {
            GameObject newPlayerListItem = Instantiate(playerListItemPrefab);
            SteamPlayerItem newSteamPlayerItem = newPlayerListItem.GetComponent<SteamPlayerItem>();

            newSteamPlayerItem.playerUsername = player.playerUsername;
            newSteamPlayerItem.connectionId = player.connectionId;
            newSteamPlayerItem.playerSteamId = player.playerSteamId;
            newSteamPlayerItem.SetPlayerValues();

            //Transform Prefab
            newPlayerListItem.transform.SetParent(lobbyCanvasObject.transform);
            newSteamPlayerItem.SetPositionOnCanvas();

            //Ready Status
            newSteamPlayerItem.isPlayerReady = player.isPlayerReady;
            newSteamPlayerItem.playerObjectController = player.GetComponent<PlayerObjectController>();

            Debug.Log("New Player List Item as HOST " + newSteamPlayerItem.playerUsername);
            playerListItemList.Add(newSteamPlayerItem);
        }

        playerItemCreated = true;
    }

    public void CreateClientPlayerItem() //Create non-host player items
    {
        foreach (PlayerObjectController player in Manager.gamePlayers)
        {
            //If there is no-one in the current list elements, adds them, prevents adding same player multiple times
            if(!playerListItemList.Any(b => b.connectionId == player.connectionId))
            {
                GameObject newPlayerListItem = Instantiate(playerListItemPrefab);
                SteamPlayerItem newSteamPlayerItem = newPlayerListItem.GetComponent<SteamPlayerItem>();

                newSteamPlayerItem.playerUsername = player.playerUsername;
                newSteamPlayerItem.connectionId = player.connectionId;
                newSteamPlayerItem.playerSteamId = player.playerSteamId;
                newSteamPlayerItem.SetPlayerValues();

                //Transform Prefab
                newPlayerListItem.transform.SetParent(lobbyCanvasObject.transform);
                newSteamPlayerItem.SetPositionOnCanvas();

                //Ready Status
                newSteamPlayerItem.isPlayerReady = player.isPlayerReady;
                newSteamPlayerItem.playerObjectController = player.GetComponent<PlayerObjectController>();

                Debug.Log("New Player List Item as CLIENT " + newSteamPlayerItem.playerUsername);
                playerListItemList.Add(newSteamPlayerItem);
            }
        }
    }

    public void UpdatePlayerItem()
    {
        foreach(PlayerObjectController player in Manager.gamePlayers)
        {
            foreach(SteamPlayerItem playerItemScript in playerListItemList)
            {
                if(player.connectionId == playerItemScript.connectionId)
                {
                    playerItemScript.playerUsername = player.playerUsername;
                    playerItemScript.isPlayerReady = player.isPlayerReady;
                    playerItemScript.SetPlayerValues();
                }
            }
        }

        CheckAllReady();
    }

    public void RemovePlayerItem() //When Player leaves lobby
    {
        List<SteamPlayerItem> playerItemsToRemove = new List<SteamPlayerItem>();

        foreach(SteamPlayerItem playerListItem in playerListItemList)
        {
            //If no player has the same connectionId as the current list item, remove the list item (old)
            if(Manager.gamePlayers.Any(b => b.connectionId == playerListItem.connectionId))
            {
                playerItemsToRemove.Add(playerListItem);
            }
        }

        //Actually remove list items
        if(playerItemsToRemove.Count > 0)
        {
            foreach(SteamPlayerItem playerItemOld in playerItemsToRemove)
            {
                playerListItemList.Remove(playerItemOld);
                if(playerItemOld != null && playerItemOld.gameObject != null)
                {
                    Destroy(playerItemOld.gameObject);
                }
            }
        }

        CheckAllReady();
    }

    public void FindLocalPlayer()
    {
        localPlayerObject = GameObject.Find("LocalGamePlayer"); //WARNING This must match the name you give the 
        localPlayerController = localPlayerObject.GetComponent<PlayerObjectController>();
    }



    public void StartGame(string sceneName) //button click callback (via delegate)
    {
        if(localPlayerController != null)
        {
            localPlayerController.CanStartGame(sceneName);
        }
    }

    public void CheckAllReady()
    {
        if(localPlayerController == null)
        {
            return;
        }

        bool allReady = true;

        foreach(PlayerObjectController p in Manager.gamePlayers)
        {
            if (!p.isPlayerReady)
            {
                allReady = false;
                break;
            }
        }

        //Only enabled if all clients are ready and I am the host
        startGameButton.interactable = allReady && localPlayerController.playerIdNumber == 1;
    }

    public void LeaveLobby()
    {
        Manager.QuitMatch();
    }
    
}

