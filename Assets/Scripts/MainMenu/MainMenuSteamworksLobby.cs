using UnityEngine;
using Mirror;
using Steamworks;
using UnityEngine.UI;

public class MainMenuSteamworksLobby : MonoBehaviour
{
    public static MainMenuSteamworksLobby Instance;

    //Variables
    public ulong currentLobbyID;
    private const string hostAddressKey = "HostAddress";
    public const string nameKey = "name";
    public Button hostGame_btn;

    //Generic
    public Button quitGame_btn;
    public Canvas mainMenuCanvas;

    //Player Settings
    public Slider gameVolume_slider;
    public int gameVolume = 1;

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

    // Start is called once before the first execution of Update after the MonoBehaviour is created
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
    }


    #region Buttons
    public void HostLobby()
    {
        SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypeFriendsOnly, Manager.maxConnections);
        hostGame_btn.gameObject.SetActive(false);
        mainMenuCanvas.gameObject.SetActive(false);
    }

    //Assignment 1
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

}
