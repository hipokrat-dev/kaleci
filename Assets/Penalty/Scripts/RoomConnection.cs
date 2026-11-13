using System;
using System.Threading.Tasks;
using System.Text;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Multiplayer;
using UnityEngine;
namespace PenaltyArena
{
    // Commands carry intent only. The host validates role, shot id and phase.
    [Serializable] public class PlayerCommand { public string action; public string name; public int shotId; public float x, y, power; }
    [Serializable] public class MatchSnapshot
    {
        public int sequence, shotId, shooter, phase, score0, score1, winner = -1;
        public int diveCode; public float diveProgress, diveDuration;
        public bool locked0,locked1;
        public double shotStartedAt, diveStartedAt, serverSentAt;
        public Vector3 ballVelocity;
        public double netHitAt;
        public Vector3 netHitPoint;
        public float netHitSpeed;
        public string name0 = "Oyuncu 1", name1 = "Oyuncu 2", history0 = "", history1 = "";
        public bool ready0, ready1, easy; public int count;
        public Vector3 ball, keeper; public Quaternion ballRotation; public string message;
    }
    public sealed class RoomConnection : MonoBehaviour
    {
        public string Code { get; private set; } = "";
        public string Status { get; private set; } = "Özel oda oluştur veya kodla katıl.";
        public bool Busy { get; private set; }
        public bool Connected => manager != null && manager.IsListening;
        public bool Host => manager != null && manager.IsHost;
        public int LocalSlot => Host ? 0 : 1;
        public int Count => Host ? manager.ConnectedClients.Count : (Connected ? 2 : 0);
        public double NetworkTime => Connected ? manager.ServerTime.Time : Time.timeAsDouble;
        public event Action<int, PlayerCommand> Command;
        public event Action<MatchSnapshot> Snapshot;
        public event Action Lost;
        NetworkManager manager; ISession session; bool registered, leaving;
        ulong peer = ulong.MaxValue;
        void Awake()
        {
            manager = gameObject.AddComponent<NetworkManager>();
            var transport = gameObject.AddComponent<UnityTransport>();
            manager.NetworkConfig = new NetworkConfig { ProtocolVersion = 3, NetworkTransport = transport, EnableSceneManagement = false, ConnectionApproval = true };
            manager.ConnectionApprovalCallback = (request, response) => {
                response.Approved = manager.ConnectedClients.Count < 2;
                response.CreatePlayerObject = false; response.Pending = false;
                if (!response.Approved) response.Reason = "Oda dolu.";
            };
            manager.OnClientConnectedCallback += OnConnected;
            manager.OnClientDisconnectCallback += OnDisconnected;
        }
        void Register()
        {
            if (registered || manager.CustomMessagingManager == null) return;
            manager.CustomMessagingManager.RegisterNamedMessageHandler("pa.cmd", (sender, reader) => {
                if (!Host || (sender != peer && sender != manager.LocalClientId)) return;
                try { string json=ReadJson(reader,1024);
                    Command?.Invoke(sender == manager.LocalClientId ? 0 : 1, JsonUtility.FromJson<PlayerCommand>(json));
                } catch (Exception e) { Debug.LogWarning(e.Message); }
            });
            manager.CustomMessagingManager.RegisterNamedMessageHandler("pa.state", (sender, reader) => {
                if (Host || sender != NetworkManager.ServerClientId) return;
                try { Snapshot?.Invoke(JsonUtility.FromJson<MatchSnapshot>(ReadJson(reader,8192))); }
                catch (Exception e) { Debug.LogWarning(e.Message); }
            });
            registered = true;
        }
        void OnConnected(ulong id)
        {
            Register(); if (Host && id != manager.LocalClientId) peer = id;
            Status = "Bağlandı. İki oyuncunun hazır olması bekleniyor.";
        }
        void OnDisconnected(ulong id)
        {
            if (leaving) return;
            peer = ulong.MaxValue; Status = "Bağlantı kesildi. Maç sonucu belirlenmedi."; Lost?.Invoke();
        }
        async Task Initialize()
        {
            if (UnityServices.State != ServicesInitializationState.Initialized) {
                var options=new InitializationOptions();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                // Separate local test processes must not reuse the same anonymous player.
                var args=Environment.GetCommandLineArgs();
                if(Array.IndexOf(args,"-arena-relay-host")>=0)options.SetProfile("arena-smoke-host");
                else if(Array.IndexOf(args,"-arena-relay-client")>=0)options.SetProfile("arena-smoke-client");
#endif
                await UnityServices.InitializeAsync(options);
            }
            if (!AuthenticationService.Instance.IsSignedIn) await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
        public async void Open(bool host, string code)
        {
            if (Busy || Connected) return;
            Busy = true; Status = "Bağlanıyor…"; leaving = false;
            try {
                await Initialize();
                session = host
                    ? await MultiplayerService.Instance.CreateSessionAsync(new SessionOptions { MaxPlayers = 2, IsPrivate = true }.WithRelayNetwork())
                    : await MultiplayerService.Instance.JoinSessionByCodeAsync(code.Trim().ToUpperInvariant());
                Code = session.Code; Register(); Status = "Oda hazır. Rakip bekleniyor.";
            } catch (Exception e) { Status = "Oda açılamadı. İnternet ve Unity Services proje bağlantısını kontrol et."; Debug.LogException(e); if (manager.IsListening) manager.Shutdown(); registered = false; }
            finally { Busy = false; }
        }
        // Development-only local networking; never starts a single-player game.
        public void Local(bool host, string address = "127.0.0.1")
        {
            if (Connected || Busy) return;
            leaving = false; manager.GetComponent<UnityTransport>().SetConnectionData(address, 7777, "0.0.0.0");
            if (host) manager.StartHost(); else manager.StartClient();
            Register(); Code = "YEREL TEST";
        }
        public void Send(PlayerCommand command)
        {
            if (!Connected) return;
            if (Host) Command?.Invoke(0, command);
            else SendJson("pa.cmd", NetworkManager.ServerClientId, JsonUtility.ToJson(command), NetworkDelivery.ReliableSequenced);
        }
        public void Broadcast(MatchSnapshot state)
        {
            Snapshot?.Invoke(state);
            if (peer != ulong.MaxValue && manager.ConnectedClients.ContainsKey(peer)) SendJson("pa.state", peer, JsonUtility.ToJson(state), NetworkDelivery.UnreliableSequenced);
        }
        void SendJson(string channel, ulong recipient, string json, NetworkDelivery delivery)
        {
            // NGO's default string encoding doubles ASCII JSON. UTF-8 keeps motion snapshots below MTU
            // and preserves Turkish names. Very long sudden-death histories can use fragmentation.
            byte[] bytes=Encoding.UTF8.GetBytes(json);
            using var writer = new FastBufferWriter(bytes.Length+4, Allocator.Temp);
            writer.WriteValueSafe(bytes.Length);writer.WriteBytesSafe(bytes);
            if(writer.Length>1100)delivery=NetworkDelivery.ReliableFragmentedSequenced;
            manager.CustomMessagingManager.SendNamedMessage(channel, recipient, writer, delivery);
        }
        static string ReadJson(FastBufferReader reader,int maximum)
        {
            reader.ReadValueSafe(out int length);
            if(length<0 || length>maximum || length>reader.Length-reader.Position)throw new ArgumentException("Invalid message length");
            var bytes=new byte[length];reader.ReadBytesSafe(ref bytes,length);return Encoding.UTF8.GetString(bytes);
        }
        public async void Leave() => await LeaveAsync();
        public async Task LeaveAsync()
        {
            if (Busy) return;
            leaving = true; Busy = true;
            try { if (session != null) await session.LeaveAsync(); }
            catch (Exception e) { Debug.LogWarning(e.Message); }
            finally { session = null; manager.Shutdown(); registered = false; peer = ulong.MaxValue; Code = ""; Busy = false; Status = "Odadan ayrıldın."; Lost?.Invoke(); }
        }
    }
}
