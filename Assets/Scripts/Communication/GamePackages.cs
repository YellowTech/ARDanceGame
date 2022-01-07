using System;
using LiteNetLib.Utils;

namespace Communiction.Util {
    public enum PacketType : byte {
        ServerState,
        Serialized,
    }

    //Auto serializable packets

    // Communicate loading a different track
    public class LoadTrackPacket {
        public int Id { get; set; }
    }

    // Request to evaluate pose with index
    public class EvaluatePoseRequestPacket {
        public int trackNr { get; set; }
        public int PoseIndex { get; set; }
        public int RequestId { get; set; }
    }

    // Reponse to an evaluated pose request
    public class EvaluatePoseResponsePacket {
        public int RequestId { get; set; }
        public float Score { get; set; }
        // public int faultyJoint eg for feedback on where the error was
    }

    // Manual serializable packets
    //public struct ServerState : INetSerializable {
    //    public uint Tick;

    //    public int PlayerStatesCount;
    //    public int StartState; //server only

    //    //tick
    //    public const int HeaderSize = sizeof(uint) * 2;

    //    public void Serialize(NetDataWriter writer) {
    //        writer.Put(Tick);
    //        writer.Put(PlayerStatesCount);
    //    }

    //    public void Deserialize(NetDataReader reader) {
    //        Tick = reader.GetUInt();
    //        PlayerStatesCount = reader.GetInt();
    //    }
    //}

    // Nested packet types for being included in super packets
    public struct VectorPacket : INetSerializable {
        public float x;
        public float y;
        public float z;

        public VectorPacket(float x, float y, float z) {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public void Deserialize(NetDataReader reader) {
            x = reader.GetFloat();
            y = reader.GetFloat();
            z = reader.GetFloat();
        }

        public void Serialize(NetDataWriter writer) {
            writer.Put(x);
            writer.Put(y);
            writer.Put(z);
        }
    }


}