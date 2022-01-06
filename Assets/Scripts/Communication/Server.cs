using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Communiction.Util;
using System.Net;
using System.Net.Sockets;
using LiteNetLib;
using LiteNetLib.Utils;
using System;

namespace Communiction.Server {
    public class Server : MonoBehaviour, INetEventListener {
        private NetManager netManager;
        private NetPacketProcessor packetProcessor;

        public const int MaxPlayers = 1;
        private readonly NetDataWriter cachedWriter = new NetDataWriter();
        //private PlayerMovementPacket _cachedMovementPacket = new PlayerMovementPacket();
        private uint serverTick;

        public static readonly int PacketTypesCount = Enum.GetValues(typeof(PacketType)).Length;

        public void StartServer() {
            if (netManager.IsRunning)
                return;
            netManager.Start(5000);

            Debug.Log("Server started at " + NetUtils.GetLocalIp(LocalAddrType.IPv4) + " at Port " + netManager.LocalPort);
        }

        private void Awake() {
            DontDestroyOnLoad(gameObject);
            packetProcessor = new NetPacketProcessor();
            //serverPlayerManager = new ServerPlayerManager(this);
            //serverEntityManager = new ServerEntityManager(this);

            //register auto serializable Vector
            //_packetProcessor.RegisterNestedType((w, v) => w.Put(v), r => r.GetVectorPacket());
            packetProcessor.RegisterNestedType<VectorPacket>();

            //register auto serializable PlayerStatePacket
            //packetProcessor.RegisterNestedType<PlayerStatePacket>();
            //packetProcessor.SubscribeNetSerializable<EntityStatePacket, NetPeer>(OnEntityStateReceived);
            //packetProcessor.SubscribeReusable<JoinPacket, NetPeer>(OnJoinReceived);
            netManager = new NetManager(this) {
                AutoRecycle = true,
                BroadcastReceiveEnabled = true,
                IPv6Mode = IPv6Mode.Disabled,
            };
        }

        private void Start() {
            StartServer();
        }

        private void OnDestroy() {
            netManager.Stop();
            Debug.Log("Server stopped");
        }

        private void Update() {
            netManager.PollEvents();
        }

        private void FixedUpdate() {
            serverTick++;
        }

        private NetDataWriter WriteSerializable<T>(PacketType type, T packet) where T : struct, INetSerializable {
            cachedWriter.Reset();
            cachedWriter.Put((byte)type);
            packet.Serialize(cachedWriter);
            return cachedWriter;
        }

        private NetDataWriter WritePacket<T>(T packet) where T : class, new() {
            cachedWriter.Reset();
            cachedWriter.Put((byte)PacketType.Serialized);
            packetProcessor.Write(cachedWriter, packet);
            return cachedWriter;
        }

        void INetEventListener.OnPeerConnected(NetPeer peer) {
            Debug.Log("[S] Player connected: " + peer.EndPoint);
        }

        void INetEventListener.OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo) {
            Debug.Log("[S] Player disconnected: " + disconnectInfo.Reason);

            //Debug.Log("[S] Restarting Server");
            //netManager.Stop();
            //netManager = new NetManager(this) {
            //    AutoRecycle = true,
            //    BroadcastReceiveEnabled = true,
            //    IPv6Mode = IPv6Mode.Disabled,
            //};
            //netManager.Start(5000);
        }

        void INetEventListener.OnNetworkError(IPEndPoint endPoint, SocketError socketError) {
            Debug.Log("[S] NetworkError: " + socketError);
        }

        void INetEventListener.OnNetworkReceive(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod) {
            Debug.Log("[S] Received Packet from " + peer);
            byte packetType = reader.GetByte();
            if (packetType >= PacketTypesCount)
                return;
            PacketType pt = (PacketType)packetType;
            switch (pt) {
                case PacketType.Serialized:
                    packetProcessor.ReadAllPackets(reader, peer);
                    break;
                //case PacketType.EntityState:
                //    cachedEntityStatePacket.Deserialize(reader);
                //    //Debug.Log("[S] EntityStatedata received: UID" + cachedEntityStatePacket.UID + " at " + cachedEntityStatePacket.Tick + " Tick");
                //    serverEntityManager.handleEntityStatePacket(cachedEntityStatePacket);
                //    break;
                //case PacketType.Entity:
                //    cachedEntityPacket.Deserialize(reader);
                //    Debug.Log("[S] Entitydata received UID:" + cachedEntityPacket.UID + ", type:" + cachedEntityPacket.details.GetType() + " from " + cachedEntityPacket.OwnerId);
                //    serverEntityManager.handleEntityPacket(cachedEntityPacket);
                //    break;
                default:
                    Debug.Log("[S] Unhandled packet: " + pt);
                    break;
            }
        }

        void INetEventListener.OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType) {
            Debug.Log("[S] Received Unconnected Packet from " + remoteEndPoint + " as " + messageType);
            if (messageType == UnconnectedMessageType.Broadcast) {
                Debug.Log("[S] Received discovery request. Send discovery response");
                NetDataWriter resp = new NetDataWriter();
                resp.Put(2);
                netManager.SendUnconnectedMessage(resp, remoteEndPoint);
            }
        }

        void INetEventListener.OnNetworkLatencyUpdate(NetPeer peer, int latency) {
            if (peer.Tag != null) {
                Debug.Log("[S] Peer " + peer + " has ping " + latency);
            }
        }

        void INetEventListener.OnConnectionRequest(ConnectionRequest request) {
            request.AcceptIfKey("itsdancetime");
        }
    }
}