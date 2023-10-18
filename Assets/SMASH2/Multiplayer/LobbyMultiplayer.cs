using System.Collections;
using System.Collections.Generic;
//using System.Net.Security;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.PlayerLoop;
using Utilities;



// namespace Lobby {     // TODO?
[System.Serializable]
public enum EncryptionType
{
    DTLS,   // Datagram Transport Layer Security, for most builds
    WSS,    // Web Socket Secure, for webGL builds
}
// Note: Also Udp and Ws are possible choices

public class LobbyMultiplayer : MonoBehaviour
{
    //public string lobbyName = "TheLobby";
    [SerializeField] string lobbyName = "TheLobby";
    [SerializeField] int maxPlayers = 8;
    [SerializeField] EncryptionType encryption = EncryptionType.DTLS;


    public static LobbyMultiplayer instance;

    public string PlayerId;// { get; private set; }
    public string PlayerName;// { get; private set; }


    Lobby currentLobby;



    const float HEARTBEAT_INTERVAL = 20f;
    const float LOBBY_POLL_FOR_UPDATES_INTERVAL = 65f;

    /// <summary>
    /// Keeps the lobby alive by sending a heartbeat every X seconds
    /// </summary>
    CountdownTimer heartBeatTimer = new CountdownTimer(HEARTBEAT_INTERVAL);
    CountdownTimer lobbyPollForUpdatesTimer = new CountdownTimer(LOBBY_POLL_FOR_UPDATES_INTERVAL);

    public const string KEYJOINCODE = "RelayJoinCode";
    public const string HOSTNAME = "HostName";
    const string DTLS_Encryption = "dtls";
    const string WSS_Encryption = "wss";
    string connectionType => encryption == EncryptionType.DTLS ? DTLS_Encryption : WSS_Encryption;


    // New stuff
    public QueryResponse lobbies;
    CountdownTimer refreshLobbiesTimer = new CountdownTimer(3f);    // I think the rate limit is 2.5 seconds
    const float REFRESH_LOBBIES_INTERVAL = 3f;


    // Start is called before the first frame update
    async void Start()
    {
        if (instance != null)
            Debug.LogError("More than one LobbyMultiplayer in scene");
        instance = this;

        DontDestroyOnLoad(this);

        await Authenticate();   // async method


        // NOTE: NONE OF THESE TIMERS SEEM TO BE RUNNING!
        //heartBeatTimer.OnTimerStop += () =>
        //{
        //    HandleHeartbeatAsync();
        //    heartBeatTimer.Start();     // restart the timer    (TODO would this go on after the program ends..? Probably not..?)
        //};
        //lobbyPollForUpdatesTimer.OnTimerStop += () =>
        //{
        //    print("HandlelobbyPollForUpdatesTimerAsync");
        //    HandlelobbyPollForUpdatesTimerAsync();
        //    lobbyPollForUpdatesTimer.Start();   // restart the timer
        //};


        //// new
        //refreshLobbiesTimer.OnTimerStop += () =>    
        //{
        //    print("refreshing lobbies1");   // TODO do these not work?? Never get called??
        //    updateLobbiesListAsync();
        //    refreshLobbiesTimer.Start();
        //};
    }


    float updateLobbiesCountdown = 0f;
    float updateHeartbeatCountdown = 0f;
    float lobbyPollForUpdatesCountdown = 0f;
    void Update()
    {
        updateLobbiesCountdown -= Time.deltaTime;
        if (updateLobbiesCountdown <= 0)
        {
            //print("refreshing lobbies, from update()");
            updateLobbiesCountdown = REFRESH_LOBBIES_INTERVAL;
            updateLobbiesListAsync();
        }

        //if (IsServer)   // TODO, somehow. Stop heartbeats from clients
        updateHeartbeatCountdown -= Time.deltaTime;
        if (updateHeartbeatCountdown <= 0)
        {
            updateHeartbeatCountdown = HEARTBEAT_INTERVAL;
            HandleHeartbeatAsync();
        }

        lobbyPollForUpdatesCountdown -= Time.deltaTime;
        if (lobbyPollForUpdatesCountdown <= 0)
        {
            lobbyPollForUpdatesCountdown = LOBBY_POLL_FOR_UPDATES_INTERVAL;
            HandlelobbyPollForUpdatesTimerAsync();
        }

    }

    // authenticate players

    /// <summary>
    /// An overload to assign a random unique player name. (Unique names are required)
    /// </summary>
    /// <returns></returns>
    async Task Authenticate()
    {
        await Authenticate("Player" + Random.Range(0, 1000));
    }

    async Task Authenticate(string playerName)
    {
        // https://youtu.be/zimljd4Rxr0?list=PLnJJ5frTPwRN79MQt13JVCjZ9WVPKUjO3&t=515 not sure what this does
        if (UnityServices.State == ServicesInitializationState.Uninitialized)
        {
            InitializationOptions options = new InitializationOptions();
            options.SetProfile(playerName);

            await UnityServices.InitializeAsync(options);
        }

        AuthenticationService.Instance.SignedIn += () =>    // hook into the SignedIn delegate and report it
        {
            Debug.Log("Signed in as " + AuthenticationService.Instance.PlayerId + " PlayerName: " + AuthenticationService.Instance.Profile);
        };

        // if not signed in, do it anonymously
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            PlayerId = AuthenticationService.Instance.PlayerId;
            PlayerName = playerName;
            await AuthenticationService.Instance.UpdatePlayerNameAsync(playerName); // new

        }
        PlayerName = playerName;
    }



    // relay stuff:
    // host methods:
    // public methods:
    public async Task CreateLobby() { 
        try
        {
            Allocation allocation = await AllocateRelay();
            string relayJoinCode = await GetRelayJoinCode(allocation);

            CreateLobbyOptions options = new CreateLobbyOptions()
            {
                
                IsPrivate = false,

            };

            currentLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers);

            // keeps the lobby alive
            heartBeatTimer.Start();
            lobbyPollForUpdatesTimer.Start();


            // add relay code to lobby so other joiners can grab it from the lobby and join relay
            await LobbyService.Instance.UpdateLobbyAsync(currentLobby.Id, new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
                {
                    { KEYJOINCODE, new DataObject(DataObject.VisibilityOptions.Member, relayJoinCode) },
                    { HOSTNAME, new DataObject(DataObject.VisibilityOptions.Public, PlayerName)}    // new
                }
            });

            // set relay server data
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(new RelayServerData(
                allocation, connectionType));    // connectionType is encryption actually

            // finally
            NetworkManager.Singleton.StartHost();

            Debug.Log("created lobby: Name: " + currentLobby.Name + ", ID: " + currentLobby.Id + ", JoinCode: " + relayJoinCode);

        } catch (LobbyServiceException e)
        {
            Debug.LogError("Failed to create lobby: " + e.Message);
        }
    }

    public async Task QuickJoinLobby()
    {
        try
        {
            //currentLobby = await LobbyService.Instance.QuickJoinLobbyAsync();
            currentLobby = await LobbyService.Instance.QuickJoinLobbyAsync();
            lobbyPollForUpdatesTimer.Start();

            string relayJoinCode = currentLobby.Data[KEYJOINCODE].Value;
            JoinAllocation joinAllocation = await JoinRelay(relayJoinCode);

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(new RelayServerData(
                               joinAllocation, connectionType));

            NetworkManager.Singleton.StartClient();
    }
        catch (LobbyServiceException e)
        {
            Debug.LogError("Failed to join lobby: " + e.Message);
        }
    }

    //public async Task JoinLobby(string lobbyId)
    //{
    //    try
    //    {
    //        currentLobby = await LobbyService.Instance.JoinLobbyAsync(lobbyId);
    //    } catch (LobbyServiceException e)
    //    {
    //        Debug.LogError("Failed to join lobby: " + e.Message);
    //    }
    //}


    // private methods:
    async Task<Allocation> AllocateRelay()
    {
        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers - 1);   // exclude the host
            return allocation;
        } catch (RelayServiceException e)
        {
            Debug.LogError("Failed to allocate relay: " + e.Message);
            return default;
        }
    }

    // after allocation
    async Task<string> GetRelayJoinCode(Allocation allocation)
    {
        try
        {
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            return joinCode;
        } catch (RelayServiceException e)
        {
            Debug.LogError("Failed to get join code: " + e.Message);
            return default;
        }
    }


    // client methods:
    async Task<JoinAllocation> JoinRelay(string relayJoinCode)
    {
        try
        {
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(relayJoinCode);
            return joinAllocation;
        } catch (RelayServiceException e)
        {
            Debug.LogError("Failed to join relay: " + e.Message);
            return default;
        }
    }



    // lobby heartbeats
    async Task HandleHeartbeatAsync()
    {
        try
        {
            await LobbyService.Instance.SendHeartbeatPingAsync(currentLobby.Id);
            Debug.Log("Sent heartbeat ping to lobby: " + currentLobby.Name);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError("Failed to send heartbeat to lobby: " + e.Message);
        }
    }

    async Task HandlelobbyPollForUpdatesTimerAsync()
    {
        try
        {
            Lobby lobby = await LobbyService.Instance.GetLobbyAsync(currentLobby.Id);
            Debug.Log("Polled for updates on lobby: " + lobby.Name);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError("Failed to poll for updates on lobby: " + e.Message);
        }
    }



    // my new stuff

    public async Task updateLobbiesListAsync()
    {
        // https://docs.unity.com/ugs/en-us/manual/lobby/manual/query-for-lobbies
        try
        {
            QueryLobbiesOptions options = new QueryLobbiesOptions();
            options.Count = 25;

            // Filter for open lobbies only
            options.Filters = new List<QueryFilter>()
    {
        new QueryFilter(
            field: QueryFilter.FieldOptions.AvailableSlots,
            op: QueryFilter.OpOptions.GT,
            value: "0")
    };

            // Order by newest lobbies first
            options.Order = new List<QueryOrder>()
    {
        new QueryOrder(
            asc: false,
            field: QueryOrder.FieldOptions.Created)
    };

            lobbies = await Lobbies.Instance.QueryLobbiesAsync(options);

            //...
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }

    }


    ///// <summary>
    ///// 
    ///// </summary>
    ///// <param name="name"> No spaces?</param>
    ///// <returns></returns>
    //public async Task setName(string name)
    //{
    //    // https://stackoverflow.com/questions/76546659/unity-gaming-services-leaderboard-i-want-to-replace-the-player-id-with-a-player
    //    await AuthenticationService.Instance.UpdatePlayerNameAsync("name");
    //}


    //// async get playername
    //public async Task<string> GetPlayerName(string playerId)
    //{
    //    try
    //    {
    //        var player = await AuthenticationService.Instance.player(playerId);
    //        return player.DisplayName;
    //    }
    //    catch (AuthenticationException e)
    //    {
    //        Debug.Log(e);
    //        return null;
    //    }
    //}


}
