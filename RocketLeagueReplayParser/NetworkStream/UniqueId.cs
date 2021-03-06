﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RocketLeagueReplayParser.NetworkStream
{
    public class UniqueId
    {
        public enum UniqueIdType { Unknown = 0, Steam = 1, PS4 = 2, PS3 = 3, Xbox = 4 }

        public UniqueIdType Type { get; protected set; }
        public byte[] Id { get; private set; }
        public byte PlayerNumber { get; private set; } // Split screen player (0 when not split screen)

        public static UniqueId Deserialize(BitReader br)
        {
            List<object> data = new List<object>();
            var type = (UniqueIdType)br.ReadByte();

            UniqueId uid = new UniqueId();
            if (type == UniqueIdType.Steam)
            {
                uid = new SteamId();
            }
            uid.Type = type;

            DeserializeId(br, uid);
            return uid;
        }

        protected static void DeserializeId(BitReader br, UniqueId uid)
        {
            if (uid.Type == UniqueIdType.Steam)
            {
                uid.Id = br.ReadBytes(8);
            }
            else if (uid.Type == UniqueIdType.PS4)
            {
                uid.Id = br.ReadBytes(32);
            }
            else if (uid.Type == UniqueIdType.Unknown)
            {
                uid.Id = br.ReadBytes(3); // Will be 0
                if (uid.Id.Sum(x => x) != 0)
                {
                    throw new Exception("Unknown id isn't 0, might be lost");
                }
            }
            else if (uid.Type == UniqueIdType.Xbox)
            {
                uid.Id = br.ReadBytes(8);
            }
            else
            {
                throw new ArgumentException(string.Format("Invalid type: {0}. Next bits are {1}", ((int)uid.Type).ToString(), br.GetBits(br.Position, Math.Min(4096, br.Length - br.Position)).ToBinaryString()));
            }
            uid.PlayerNumber = br.ReadByte();
        }

        public void Serialize(BitWriter bw)
        {
            bw.Write((byte)Type);
            SerializeId(bw);
        }

        protected void SerializeId(BitWriter bw)
        {
            bw.Write(Id);
            bw.Write(PlayerNumber);
        }

        public override string ToString()
        {
            return string.Format("{0} {1} {2}", Type, BitConverter.ToString(Id ?? new byte[0]).Replace("-", ""), PlayerNumber);
        }
    }

    // Do I really need a separate type for this? 
    public class SteamId : UniqueId
    {
        public Int64 SteamID64
        {
            get
            {
                if (Type != UniqueIdType.Steam)
                {
                    throw new InvalidDataException(string.Format("Invalid type {0}, cant extract steam id", Type));
                }

                return BitConverter.ToInt64(Id, 0);
            }
        }

        public string SteamProfileUrl
        {
            get
            {
                return string.Format("http://steamcommunity.com/profiles/{0}", SteamID64);
            }
        }

        public override string ToString()
        {
            return string.Format("{0} ({1}), PlayerNumber {2}", SteamID64, SteamProfileUrl, PlayerNumber);
        }
    }
}
