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

namespace Communiction.Client {
    public class Client : MonoBehaviour, INetEventListener {

        private Action<DisconnectInfo> _onDisconnected;

        private NetManager _netManager;
        private NetDataWriter _writer;
        private NetPacketProcessor _packetProcessor;

        private string userName;
        //private ServerState cachedServerState;
        private uint _lastServerTick;
        private NetPeer _server;
        private int _ping;
        private float discoveryCooldown = 0f;

        public bool joined = false;

        // return if we have a connected server
        public static bool Connected => instance == null ? false : instance._netManager.FirstPeer != null && instance._netManager.FirstPeer.ConnectionState == ConnectionState.Connected;
        public static Client instance;

        public uint ClientTick = 0;

        // Start Client
        private void Awake() {
            // Implement as singleton, destroy if there is another client running
            if (instance != null) {
                Destroy(this);
            } else {
                // Start the client
                DontDestroyOnLoad(gameObject);
                instance = this;
                Random r = new Random();
                userName = Environment.MachineName + " " + r.Next(100000);
                _writer = new NetDataWriter();

                // Register all packet types and nested types
                _packetProcessor = new NetPacketProcessor();
                _packetProcessor.RegisterNestedType<VectorPacket>();
                //_packetProcessor.SubscribeReusable<JoinAcceptPacket>(OnJoinAccept);
                
                // create and start the client manager
                _netManager = new NetManager(this) {
                    AutoRecycle = true,
                    IPv6Mode = IPv6Mode.Disabled,
                    UnconnectedMessagesEnabled = true,
                    EnableStatistics = true
                };
                _netManager.Start();
            }
        }

        private void Update() {
            // Handle all received packets
            _netManager.PollEvents();

            // check if connected to a server
            if (Connected) {
                // Connected
                //Debug.Log( "Bytes sent: " + Util.Util.ConvertNumber(_netManager.Statistics.BytesSent));
                //Debug.Log("Bytes received: " + Util.Util.ConvertNumber(_netManager.Statistics.BytesReceived));
            } else {
                // Not connected
                if (discoveryCooldown + 0.3f < Time.time) { // Cooldown for discovery broadcast
                    Debug.Log("[CLIENT] Sending new Broadcast message");
                    _netManager.SendBroadcast(new byte[] { 1 }, 5000);
                    discoveryCooldown = Time.time;
                }
            }
        }

        private void FixedUpdate() {
            ClientTick++;
        }

        private void OnDestroy() {
            _netManager.Stop();
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
            if (_server == null)
                return;
            _writer.Reset();
            _writer.Put((byte)type);
            packet.Serialize(_writer);
            _server.Send(_writer, deliveryMethod);
        }

        // send auto serializable
        public void SendPacket<T>(T packet, DeliveryMethod deliveryMethod) where T : class, new() {
            if (_server == null)
                return;
            _writer.Reset();
            _writer.Put((byte)PacketType.Serialized);
            _packetProcessor.Write(_writer, packet);
            _server.Send(_writer, deliveryMethod);
        }

        void INetEventListener.OnPeerConnected(NetPeer peer) {
            Debug.Log("[C] Connected to server: " + peer.EndPoint);
            _server = peer;
        }

        void INetEventListener.OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo) {
            _server = null;
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
                    _packetProcessor.ReadAllPackets(reader);
                    break;
                default:
                    Debug.Log("[C] Unhandled packet: " + pt);
                    break;
            }
        }
        

        // eg. broadcast answers
        void INetEventListener.OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType) {
            if (messageType == UnconnectedMessageType.BasicMessage && _netManager.ConnectedPeersCount == 0 && reader.GetInt() == 2) {
                Debug.Log("[CLIENT] Received discovery response. Connecting to: " + remoteEndPoint);
                _netManager.Connect(remoteEndPoint, "itsdancetime");
            }
        }

        void INetEventListener.OnNetworkLatencyUpdate(NetPeer peer, int latency) {
            _ping = latency;
            //Debug.Log("Ping = " + _ping);
            transform.name = "Clientlogic Ping = " + _ping;
        }

        void INetEventListener.OnConnectionRequest(ConnectionRequest request) {
            request.Reject();
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