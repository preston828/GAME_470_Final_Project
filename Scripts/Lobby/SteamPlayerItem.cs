using Mirror;
using Steamworks;
using UnityEngine;
using UnityEngine.UI;

public class SteamPlayerItem : MonoBehaviour
{
    //Player Info
    public int connectionId;
    public ulong playerSteamId;

    //Player Steam Username Variables
    public string playerUsername;
    public Text playerPersonaText;

    //Player Ready Status Variables
    public bool isPlayerReady = false;
    public string playerReadyStatus = "Unready";
    public Text ReadyStatusText;
    public Toggle ReadyStatusToggle;
    public PlayerObjectController playerObjectController;

    //Player Team Variables
    public string playerTeam;
    public Text playerTeamText;

    //Player Steam Image Variables
    public RawImage playerImage = null;
    private bool steamImageReceived = false;
    protected Callback<AvatarImageLoaded_t> ImageLoaded;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ImageLoaded = Callback<AvatarImageLoaded_t>.Create(OnImageLoaded);
    }

    //Gets called externally to set player info
    public void SetPlayerValues()
    {
        playerPersonaText.text = playerUsername;
        playerTeamText.text = playerTeam;

        ChangeReadyStatus();
        if (!steamImageReceived)
        {
            GetPlayerIcon();
        }
    }

    private void GetPlayerIcon()
    {
        int imageID = SteamFriends.GetLargeFriendAvatar((CSteamID)playerSteamId);
        if(imageID == -1)
        {
            Debug.Log("Steam image loading error");
            return;
        }
        playerImage.texture = GetSteamImageAsTexture(imageID);
    }

    private void OnImageLoaded(AvatarImageLoaded_t callback)
    {
        if(callback.m_steamID.m_SteamID == playerSteamId)
        {
            playerImage.texture = GetSteamImageAsTexture(callback.m_iImage);
        }
    }


    private Texture2D GetSteamImageAsTexture(int iImage)
    {
        Texture2D texture = null;
        bool isValid = SteamUtils.GetImageSize(iImage, out uint width, out uint height);

        if (isValid)
        {
            byte[] image = new byte[width * height * 4];
            isValid = SteamUtils.GetImageRGBA(iImage, image, (int)(width * height * 4));

            if (isValid)
            {
                texture = new Texture2D((int)width, (int)height, TextureFormat.RGBA32, false, true);
                texture.LoadRawTextureData(image);
                texture.Apply();
            }

        }
        steamImageReceived = true;
        return texture;
    }

    public void ChangeReadyStatus()
    {
        if (isPlayerReady)
        {
            playerReadyStatus = "Ready";
            ReadyStatusToggle.isOn = true;
        }
        else
        {
            playerReadyStatus = "Unready";
            ReadyStatusToggle.isOn = false;
        }

        ReadyStatusText.text = playerReadyStatus;
    }

    public void OnTryValueChanged_tgl() //Attempts to update the players ready status
    {
        playerObjectController.ChangeReadyStatus_tglAction();
    }

    public void ChangeTeam()
    {
        if(playerTeam == "Blue")
        {
            playerTeam = "Red";
        }
        else
        {
            playerTeam = "Blue";
        }

        playerTeamText.text = playerTeam;
    }

    public void OnTryChangeTeamPress() //Attempts to update the players team value
    {
        if (playerTeam == "Blue")
        {
            playerObjectController.ChangeTeamValue_onPressAction("Red"); //Pass in oposite of current team
        }
        else
        {
            playerObjectController.ChangeTeamValue_onPressAction("Blue"); //Pass in oposite of current team
        }
    }

    public GameObject GetGameObjectScriptAttachedTo()
    {
        return gameObject;
    }

}
