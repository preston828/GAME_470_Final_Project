using Steamworks;
using UnityEngine;
using UnityEngine.UI;

public class SteamGameLobbyItem : MonoBehaviour
{
    public CSteamID hostSteamId;
    public Text NameOfLobby;
    public CSteamID idOfLobby;
    public Button joinLobby_btn;

    //Host Steam Image Variables
    public RawImage hostImage = null;
    protected Callback<AvatarImageLoaded_t> ImageLoaded;

    void Start()
    {
        ImageLoaded = Callback<AvatarImageLoaded_t>.Create(OnImageLoaded);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void InitializeSteamGameLobbyItem(CSteamID lobbyID, CSteamID friendSteamID, string friendName)
    {
        idOfLobby = lobbyID;
        hostSteamId = friendSteamID;
        NameOfLobby.text = friendName + "'s Lobby";
        GetPlayerIcon();
    }

    public void OnJoinLobbyPress()
    {
        SteamMatchmaking.JoinLobby(idOfLobby);
    }

    private void GetPlayerIcon()
    {
        int imageID = SteamFriends.GetLargeFriendAvatar(hostSteamId);
        if (imageID == -1)
        {
            Debug.Log("Steam image loading error");
            return;
        }
        hostImage.texture = GetSteamImageAsTexture(imageID);
    }

    private void OnImageLoaded(AvatarImageLoaded_t callback)
    {
        if (new CSteamID(callback.m_steamID.m_SteamID) == hostSteamId)
        {
            hostImage.texture = GetSteamImageAsTexture(callback.m_iImage);
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
        return texture;
    }
}
