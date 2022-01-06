using System;
using System.Net;
using System.Net.Sockets;
using Communiction.Server;
using Communiction.Util;
using Communiction;
//using Communiction.Util;
using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;
using UnityEngine.UI;
using Random = System.Random;
using PoseTeacher;

namespace Communiction.Client {
    public class Client : MonoBehaviour, INetEventListener {

        private Action<DisconnectInfo> _onDisconnected;

        private NetManager netManager;
        private NetDataWriter writer;
        private NetPacketProcessor packetProcessor;

        private string userName;
        //private ServerState cachedServerState;
        private uint _lastServerTick;
        private NetPeer server;
        private int ping;
        private float discoveryCooldown = 0f;

        public bool joined = false;

        // use to test local only
        public bool fake = false;

        // return if we have a connected server
        public static bool Connected => Instance == null ? false : Instance.fake || Instance.netManager.FirstPeer != null && Instance.netManager.FirstPeer.ConnectionState == ConnectionState.Connected;
        public static String ServerName => Instance == null ? "None" : Instance.fake ? "Fake Server" : Instance.netManager.FirstPeer != null && Instance.netManager.FirstPeer.ConnectionState == ConnectionState.Connected ? Instance.netManager.FirstPeer.EndPoint.ToString() : "None";
        public static Client Instance;

        public uint ClientTick = 0;

        // Start Client
        private void Awake() {
            // Implement as singleton, destroy if there is another client running
            if (Instance != null) {
                Destroy(this);
            } else {
                // Start the client
                DontDestroyOnLoad(gameObject);
                Instance = this;
                Random r = new Random();
                userName = Environment.MachineName + " " + r.Next(100000);
                writer = new NetDataWriter();

                // Register all packet types and nested types
                packetProcessor = new NetPacketProcessor();
                packetProcessor.RegisterNestedType<VectorPacket>();
                packetProcessor.SubscribeReusable<EvaluatePoseResponsePacket>(OnEvaluationResponse);
                //_packetProcessor.SubscribeReusable<JoinAcceptPacket>(OnJoinAccept);
                
                // create and start the client manager
                netManager = new NetManager(this) {
                    AutoRecycle = true,
                    IPv6Mode = IPv6Mode.Disabled,
                    UnconnectedMessagesEnabled = true,
                    EnableStatistics = true
                };
                netManager.Start();
            }
        }

        private void Update() {
            // Handle all received packets
            netManager.PollEvents();

            // check if connected to a server
            if (Connected) {
                // Connected
                //Debug.Log( "Bytes sent: " + Util.Util.ConvertNumber(_netManager.Statistics.BytesSent));
                //Debug.Log("Bytes received: " + Util.Util.ConvertNumber(_netManager.Statistics.BytesReceived));
            } else {
                // Not connected and not fake
                if (!fake && discoveryCooldown + 1f < Time.time) { // Cooldown for discovery broadcast
                    Debug.Log("[CLIENT] Sending new Broadcast message");
                    netManager.SendBroadcast(new byte[] { 1 }, 5000);
                    
                    discoveryCooldown = Time.time;
                }
            }
        }

        private void FixedUpdate() {
            ClientTick++;
        }

        private void OnDestroy() {
            netManager.Stop();
        }

        //private void OnServerState() {
        //    ////skip duplicate or old because we received that packet unreliably
        //    //if (cachedServerState.Tick < _lastServerTick)
        //    //    return;
        //    //_lastServerTick = cachedServerState.Tick;
        //    //playerManager.ApplyServerState(ref cachedServerState);
        //}

        // Server confirmed the join
        //private void OnJoinAccept(JoinAcceptPacket packet) {
        //    Debug.Log("[C] Join accept. Received player id: " + packet.Id);
        //    _lastServerTick = packet.ServerTick;
        //}

        // send packet of packettype
        public void SendPacketSerializable<T>(PacketType type, T packet, DeliveryMethod deliveryMethod) where T : INetSerializable {
            if (server == null)
                return;
            writer.Reset();
            writer.Put((byte)type);
            packet.Serialize(writer);
            server.Send(writer, deliveryMethod);
        }

        // send auto serializable
        public void SendPacket<T>(T packet, DeliveryMethod deliveryMethod) where T : class, new() {
            if (server == null)
                return;
            writer.Reset();
            writer.Put((byte)PacketType.Serialized);
            packetProcessor.Write(writer, packet);
            server.Send(writer, deliveryMethod);
        }

        void INetEventListener.OnPeerConnected(NetPeer peer) {
            Debug.Log("[C] Connected to server: " + peer.EndPoint);
            server = peer;
        }

        void INetEventListener.OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo) {
            server = null;
            Debug.Log("[C] Disconnected from server: " + disconnectInfo.Reason);
            if (_onDisconnected != null) {
                _onDisconnected(disconnectInfo);
                _onDisconnected = null;
            }
        }

        void INetEventListener.OnNetworkError(IPEndPoint endPoint, SocketError socketError) {
            Debug.Log("[C] NetworkError: " + socketError);
        }

        void INetEventListener.OnNetworkReceive(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod) {
            byte packetType = reader.GetByte();
            if (packetType >= Server.Server.PacketTypesCount)
                return;
            PacketType pt = (PacketType)packetType;
            switch (pt) {
                case PacketType.ServerState:
                    //cachedServerState.Deserialize(reader);
                    //OnServerState();
                    break;
                case PacketType.Serialized:
                    packetProcessor.ReadAllPackets(reader);
                    break;
                default:
                    Debug.Log("[C] Unhandled packet: " + pt);
                    break;
            }
        }
        

        // eg. broadcast answers
        void INetEventListener.OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType) {
            if (messageType == UnconnectedMessageType.BasicMessage && netManager.ConnectedPeersCount == 0 && reader.GetInt() == 2) {
                Debug.Log("[CLIENT] Received discovery response. Connecting to: " + remoteEndPoint);
                netManager.Connect(remoteEndPoint, "itsdancetime");
            }
        }

        void INetEventListener.OnNetworkLatencyUpdate(NetPeer peer, int latency) {
            ping = latency;
            //Debug.Log("Ping = " + _ping);
            transform.name = "Clientlogic Ping = " + ping;
        }

        void INetEventListener.OnConnectionRequest(ConnectionRequest request) {
            request.Reject();
        }

        private void OnEvaluationResponse(EvaluatePoseResponsePacket pkt) {
            ClientManager.Instance.ScoreResponse(pkt.RequestId, pkt.Score);
        }
    }
}

namespace Communiction.Util {
    public static class Util {
        public static string ConvertNumber(long number) {
            if (number > 1000000000)
                return Mathf.Floor(number / 1000000).ToString() + "G";
            else if (number > 1000000)
                return Mathf.Floor(number / 1000000).ToString() + "M";
            else if (number > 1000)
                return Mathf.Floor(number / 1000).ToString() + "K";
            else
                return number.ToString();
        }
    }
}