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

    //Player Steam Image Variables
    public RawImage playerImage = null;
    private bool steamImageReceived = false;
    protected Callback<AvatarImageLoaded_t> ImageLoaded;

    public string playerTeam;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ImageLoaded = Callback<AvatarImageLoaded_t>.Create(OnImageLoaded);
    }

    //Gets called externally to set player info
    public void SetPlayerValues()
    {
        playerPersonaText.text = playerUsername;
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

    public void SetPositionOnCanvas()
    {
        
        switch (connectionId)
        {
            case 0:
                this.gameObject.transform.localPosition = new Vector3((float)187.5, (float)(-212.5), 0);
                break;
            case 1:
                this.gameObject.transform.localPosition = new Vector3((float)-195, (float)(-212.5), 0);
                break;
            case 2:
                this.gameObject.transform.localPosition = new Vector3(570, (float)(-212.5), 0);
                break;
            case 3:
                this.gameObject.transform.localPosition = new Vector3((float)-577.5, (float)(-212.5), 0);
                break;
            case 4:
                this.gameObject.transform.localPosition = new Vector3((float)(952.5), (float)(-212.5), 0);
                break;
        }

    }

    

}
