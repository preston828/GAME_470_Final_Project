using UnityEngine;
using Mirror;
using Steamworks;
using UnityEngine.SceneManagement;
using System.Collections.Generic;


public class CustomNetworkManager : NetworkManager
{
    [SerializeField]
    private PlayerObjectController gamePlayerPrefab; // This will override the default "player" slot in the network manager

    //This will be a local (on all clients, incl. host) list of current players
    public List<PlayerObjectController> gamePlayers { get; } = new List<PlayerObjectController>();

    public override void Start()
    {
        base.Start();
    }

    //This method gets called on the HOST, every time a client (& Host) requests to add a player (joins lobby)
    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        //base.OnServerAddPlayer(conn); //Calls parent method (we are overriding)

        if (SceneManager.GetActiveScene().name.Equals("Lobby")) //checks the server validity (while waiting for clients to join)
        {
            PlayerObjectController gamePlayerInstance = Instantiate(gamePlayerPrefab);
            //All of this data is "SyncVar" -> gets auto synced to all clients
            gamePlayerInstance.connectionId = conn.connectionId;
            gamePlayerInstance.playerIdNumber = gamePlayers.Count + 1;
            gamePlayerInstance.playerSteamId = (ulong)SteamMatchmaking.GetLobbyMemberByIndex(
                (CSteamID)MainMenuSteamworksLobby.Instance.currentLobbyID,
                gamePlayers.Count
                );

            if(gamePlayerInstance.playerIdNumber % 2 == 1) //Host and other odd numbered clients (1,3,5,7,9,11,13,15,17,19,21)
            {
                gamePlayerInstance.playerTeam = "Blue";
            }
            else //1st client to join and other even numbered clients (2,4,6,8,10,12,14,16,18,20)
            {
                gamePlayerInstance.playerTeam = "Red";
            }

                Debug.Log("CONN ID: " + gamePlayerInstance.connectionId + " | STEAM ID: " + gamePlayerInstance.playerSteamId);

            //Adds the player on the server, via the network (mirror syncs to all clients)
            NetworkServer.AddPlayerForConnection(conn, gamePlayerInstance.gameObject);
        }
    }

    public void StartGame(string sceneName)
    {
        ServerChangeScene(sceneName);
    }

    public void QuitMatch()
    {
        if(NetworkServer.active && NetworkClient.isConnected)
        {
            StopHost();
        }
        else if (NetworkClient.isConnected)
        {
            StopClient();
        }
        //We DO NOT need to manually load the menu screen bc Mirror will auto-load the "offline" scene
    }

}
