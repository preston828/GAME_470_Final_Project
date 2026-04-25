using UnityEngine;
using Mirror;
using Steamworks;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
//using UnityEditor.SearchService;
//using UnityEditor;

public class PlayerObjectController : NetworkBehaviour //!!! Essential for all network stuff controlled by mirror
{
    //[SyncVar] a mirror variable synced across all connections through the host, update on a client updates for host and all other clients in lobby

    //Player (lobby) data
    [SyncVar] public int connectionId;
    [SyncVar] public int playerIdNumber;
    [SyncVar] public ulong playerSteamId;
    [SyncVar] public string playerReadyStatus = "Unready";
    [SyncVar(hook = nameof(PlayerTeamUpdate))] public string playerTeam;
    [SyncVar(hook = nameof(PlayerNameUpdate))] public string playerUsername; //when "Playerusername" will sync update hook function will be called on all clients

    [SyncVar(hook = nameof(PlayerReadyUpdate))] public bool isPlayerReady = false;

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


    void Start()
    {
        DontDestroyOnLoad(this.gameObject);
    }

    //Called on the player game instance on which this client should take authority
    public override void OnStartAuthority()
    {
        //base.OnStartAuthority();
        Debug.Log("Authority: Called on requesting client. " + SteamFriends.GetPersonaName().ToString());

        Cmd_SetPlayerName(SteamFriends.GetPersonaName().ToString());
        
        gameObject.name = "LocalGamePlayer";
        LobbyController.Instance.FindLocalPlayer();
        LobbyController.Instance.UpdateLobbyName();
    }


    //Called on ALL clients where this object gets sync spawned
    public override void OnStartClient()
    {
        Manager.gamePlayers.Add(this);
        Debug.Log("Client Start: Called on requesting client. MAnager: " + Manager.gamePlayers.Count);
    }

    public override void OnStopClient()
    {
        Manager.gamePlayers.Remove(this);
        Debug.Log("On Stop Client: Called on requesting client");
        
        LobbyController.Instance.UpdatePlayerList();

        if (isLocalPlayer) //If I am the client leaving the lobby
        {
            MainMenuSteamworksLobby.Instance.Init();
        }
    }


    //Update Player Name
    [Command] //Will send a command to the host to execute the method remotely (and potentially sync)
    private void Cmd_SetPlayerName(string playerName)
    {
        this.playerUsername = playerName;
    }
    private void PlayerNameUpdate(string oldValue, string newValue)
    {
        //This hook function will be called on ALL clients (incl. host)
        LobbyController.Instance.UpdatePlayerList();
    }


    //Player Team
    [Command]
    private void Cmd_SetPlayerTeam(string teamName)
    {
        this.playerTeam = teamName;
    }
    private void PlayerTeamUpdate(string oldValue, string newValue)
    {
        LobbyController.Instance.UpdatePlayerList();
    }


    //Player Ready Status
    [Command]
    private void Cmd_SetPlayerReady()
    {
        this.isPlayerReady = !this.isPlayerReady;
    }
    private void PlayerReadyUpdate(bool oldValue, bool newValue)
    {
        LobbyController.Instance.UpdatePlayerList();
    }
    public void ChangeReadyStatus_tglAction()
    {
        if (isOwned) //If this game instance has authority on the gameObject calling the method
        {
            Cmd_SetPlayerReady();
        }
    }


    //Start Game
    [Command]
    private void Cmd_CanStartGame(string sceneName)
    {
        Manager.StartGame(sceneName);
    }
    public void CanStartGame(string sceneName)
    {
        if (isOwned)
        {
            Cmd_CanStartGame(sceneName);
        }
    }

}
