using Mirror;
using Steamworks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuSteamworksLobby : MonoBehaviour
{
    public static MainMenuSteamworksLobby Instance;

    //Variables
    public ulong currentLobbyID;
    private const string hostAddressKey = "HostAddress";
    public const string nameKey = "name";
    public Canvas mainMenuCanvas;

    //Main Menu Elements
    public Button hostGame_btn;
    public Button findGame_btn;
    public Button quitGame_btn;

    //Player Settings
    public Slider gameVolume_slider;
    public int gameVolume = 1;

    //Containers
    public GameObject mainMenu_Container;
    public GameObject findGame_Container;

    //Find Game Elements
    public GameObject GameLobbyItemPrefab;
    private List<SteamGameLobbyItem> findGameLobbyList = new List<SteamGameLobbyItem>();

    //Callbacks
    protected Callback<LobbyCreated_t> LobbyCreated;
    protected Callback<GameLobbyJoinRequested_t> JoinRequest;
    protected Callback<LobbyEnter_t> LobbyEntered;
    protected Callback<LobbyKicked_t> LobbyKicked;

    //Manager
    private CustomNetworkManager manager;
    private CustomNetworkManager Manager
    {
        get{
            if(manager != null) { return manager; }
            return manager = CustomNetworkManager.singleton as CustomNetworkManager;
        }
    }

    private void Awake()
    {
        //Singleton
        if(Instance == null)
        {
            Instance = this;
        }
    }

    
    void Start()
    {
        if (!SteamManager.Initialized) { return; } //Means steam client must be open and running when we press play

        //Callbacks registration
        LobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        JoinRequest = Callback<GameLobbyJoinRequested_t>.Create(OnJoinRequest);
        LobbyEntered = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
        LobbyKicked = Callback<LobbyKicked_t>.Create(OnLobbyKicked);

    }

    public void Init()
    {
        Cursor.lockState = CursorLockMode.None;
        hostGame_btn.gameObject.SetActive(true);
        mainMenuCanvas.gameObject.SetActive(true);
        mainMenu_Container.gameObject.SetActive(true);
        findGame_Container.gameObject.SetActive(false);
    }

    #region Buttons
    public void HostLobby()
    {
        SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypeFriendsOnly, Manager.maxConnections);
        hostGame_btn.gameObject.SetActive(false);
        mainMenuCanvas.gameObject.SetActive(false);
        mainMenu_Container.gameObject.SetActive(false);
        findGame_Container.gameObject.SetActive(false);
    }

    public void FindGame()
    {
        mainMenu_Container.gameObject.SetActive(false);
        findGame_Container.gameObject.SetActive(true);
        GetSteamFriendLobbies();
    }

    public void OnBackToMainMenuPress()
    {
        mainMenu_Container.gameObject.SetActive(true);
        findGame_Container.gameObject.SetActive(false);
        ClearFindGameLobbyList();
    }

    
    public void OnChangeVolumeSetting()
    {
        gameVolume = (int)gameVolume_slider.value / 100;
        Debug.Log("Game Volume set to " + gameVolume * 100 + " or " + gameVolume);
    }


    public void OnQuitGamePress()
    {
        Debug.Log("Quiting Game Application");
        Application.Quit(); //Only works in built versions of the game.
    }

    #endregion Buttons

    //Lobby Management
    /* Methods
    - Create
    - Join Request
    - Enter
    - Kick
    */
    #region Lobby Management
    //Callbacks Implementation
    private void OnLobbyCreated(LobbyCreated_t callback)
    {
        if(callback.m_eResult != EResult.k_EResultOK) {  return; } //Failed to create steam lobby

        //Success
        Debug.Log("Lobby created successfully");

        //This "starts the host" on this machine
        //The ONLINE scene will loac -> request player spawining...etc.
        Manager.StartHost();
        Debug.Log("Host Started");

        SteamMatchmaking.SetLobbyData(new CSteamID(callback.m_ulSteamIDLobby),
            hostAddressKey, 
            SteamUser.GetSteamID().ToString()
            );

        SteamMatchmaking.SetLobbyData(new CSteamID(callback.m_ulSteamIDLobby),
            nameKey,
            SteamFriends.GetPersonaName().ToString() + "'s Lobby"
            );
    }

    private void OnJoinRequest(GameLobbyJoinRequested_t callback)
    {
        Debug.Log("Request to join lobby!");
        
        SteamMatchmaking.JoinLobby(callback.m_steamIDLobby);
    }

    private void OnLobbyEntered(LobbyEnter_t callback)
    {
        currentLobbyID = callback.m_ulSteamIDLobby;

        //NOTE: Steam Matchmaking might be slow... and return 0 for both methods below (especially while playing in the editor)
        
        int members = SteamMatchmaking.GetNumLobbyMembers(new CSteamID(currentLobbyID));
        int maxMembers = SteamMatchmaking.GetLobbyMemberLimit(new CSteamID(currentLobbyID));
        Debug.Log("Current members: " + members + " and max allowed: " + maxMembers);
        if(members <= maxMembers)
        {
            //Client-only (skip if server)
            if (NetworkServer.active) //If I am the Host (server)
            {
                return;
            }

            //If client (sets the host network address on the client)
            Manager.networkAddress = SteamMatchmaking.GetLobbyData(new CSteamID(currentLobbyID), hostAddressKey);
            Manager.StartClient(); //Same "flow" as "Manager.StartHost();"

            Debug.Log("Manager.StartClient on Main Menu");

            hostGame_btn.gameObject.SetActive(false);
            mainMenuCanvas.gameObject.SetActive(false);
        }
        else
        {
            Debug.Log("Lobby is full! Cannot join.");
        }
    }

    private void OnLobbyKicked(LobbyKicked_t callback)
    {
        Debug.Log("Lobby left (kicked)");

        SteamMatchmaking.LeaveLobby(new CSteamID(callback.m_ulSteamIDLobby));
    }

    #endregion Lobby Management


    private void GetSteamFriendLobbies()
    {
        ClearFindGameLobbyList();

        int friendsCount = SteamFriends.GetFriendCount(EFriendFlags.k_EFriendFlagImmediate);
        int currentPlayingGame = 0;

        if(friendsCount == -1)
        {
            Debug.LogError("Friend count returned at -1, Steam is not open, or user is not logged in");
            friendsCount = 0;
        }

        //Check online friends
        for(int i = 0; i < friendsCount; i++)
        {
            CSteamID friendSteamID = SteamFriends.GetFriendByIndex(i, EFriendFlags.k_EFriendFlagImmediate);
            string friendUsername = SteamFriends.GetFriendPersonaName(friendSteamID);
            FriendGameInfo_t friendGameInfo;

            //Check if friend is playing a game
            if(SteamFriends.GetFriendGamePlayed(friendSteamID, out friendGameInfo))
            {
                if(friendGameInfo.m_gameID == new CGameID(480))
                {
                    SteamGameLobbyItem steamGameLobbyItem = Instantiate(GameLobbyItemPrefab).GetComponent<SteamGameLobbyItem>();
                    steamGameLobbyItem.InitializeSteamGameLobbyItem(friendGameInfo.m_steamIDLobby, friendSteamID, friendUsername);
                    currentPlayingGame++;
                }
            }
        }
    }

    private void ClearFindGameLobbyList()
    {
        //Clear out all lobbys listed already
        if (findGameLobbyList.Count > 0)
        {
            foreach (SteamGameLobbyItem lobby in findGameLobbyList)
            {
                Destroy(lobby.gameObject);
            }
            findGameLobbyList.Clear();
        }
    }

}
