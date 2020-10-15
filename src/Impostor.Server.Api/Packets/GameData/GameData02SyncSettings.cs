using System;
using Impostor.Server.Net.Messages;

namespace Impostor.Server.Net.GameData
{
    public class GameData02SyncSettings
    {
        public GameData02SyncSettings(IMessageReader iMessageReader, int packetLength, //Should always be 47 for this packet
            byte netID) //NetID always seems to be 44 from my testing
        {
            PacketLength = packetLength;
            NetID = netID;
            GameOptionDataSize = iMessageReader.ReadByte();
            Version = iMessageReader.ReadByte();
            MaxPlayers = iMessageReader.ReadByte();
            Language = iMessageReader.ReadUInt32();
            MapType = iMessageReader.ReadByte();
            PlayerSpeed = iMessageReader.ReadSingle();
            CrewLightModifier = iMessageReader.ReadSingle();
            ImpostorLightModifier = iMessageReader.ReadSingle();
            KillCooldown = iMessageReader.ReadSingle();
            CommonTasks = iMessageReader.ReadByte();
            LongTasks = iMessageReader.ReadByte();
            ShortTasks = iMessageReader.ReadByte();
            EmergencyMeetings = iMessageReader.ReadInt32();
            ImpostorCount = iMessageReader.ReadByte();
            KillDistance = iMessageReader.ReadByte();
            DiscussionTime = iMessageReader.ReadInt32();
            VotingTime = iMessageReader.ReadInt32();
            IsDefault = iMessageReader.ReadBoolean();
            EmergencyCooldown = iMessageReader.ReadByte();
            ConfirmEjects = iMessageReader.ReadBoolean();
            VisualTasks = iMessageReader.ReadBoolean();
        }

        public GameData02SyncSettings(int packetLength, //Should always be 47 for this packet
            byte netID, //NetID always seems to be 44 from my testing
            byte gameOptionDataSize, byte version, byte maxPlayers, uint language, byte mapType, float playerSpeed, float crewLightModifier,
            float impostorLightModifier, float killCooldown, byte commonTasks, byte longTasks, byte shortTasks, int emergencyMeetings,
            byte impostorCount, byte killDistance, int discussionTime, int votingTime, bool isDefault, byte emergencyCooldown, bool confirmEjects,
            bool visualTasks) 
        {
            PacketLength = packetLength;
            NetID = netID;
            GameOptionDataSize = gameOptionDataSize;
            Version = version;
            MaxPlayers = maxPlayers;
            Language = language;
            MapType = mapType;
            PlayerSpeed = playerSpeed;
            CrewLightModifier = crewLightModifier;
            ImpostorLightModifier = impostorLightModifier;
            KillCooldown = killCooldown;
            CommonTasks = commonTasks;
            LongTasks = longTasks;
            ShortTasks = shortTasks;
            EmergencyMeetings = emergencyMeetings;
            ImpostorCount = impostorCount;

            byte min = 0;
            byte max = 3;
            KillDistance = Math.Max(min, Math.Min(max, killDistance));

            DiscussionTime = discussionTime;
            VotingTime = votingTime;
            IsDefault = isDefault;
            EmergencyCooldown = emergencyCooldown;
            ConfirmEjects = confirmEjects;
            VisualTasks = visualTasks;
        }

        public byte NetID { get; }
        
        public byte GameOptionDataSize { get; }

        public int PacketLength { get; }

        public byte Version { get; }

        public byte MaxPlayers { get; set; }

        public uint Language { get; }

        public byte MapType { get; }

        public float PlayerSpeed { get; set; }

        public float CrewLightModifier { get; set; }

        public float ImpostorLightModifier { get; set; }

        public float KillCooldown { get; set; }

        public byte CommonTasks { get; set; }

        public byte LongTasks { get; set; }

        public byte ShortTasks { get; set; }

        public byte ImpostorCount { get; set; }

        /**
         * This should return a number 0-2, representing Short, Medium, and Long respectively.
         */
        public byte KillDistance { get; set; }

        public int DiscussionTime { get; set; }

        public int VotingTime { get; set; }

        public bool IsDefault { get; }

        public int EmergencyMeetings { get; set; }

        public int EmergencyCooldown { get; }
        
        public bool ConfirmEjects { get; }
        
        public bool VisualTasks { get; }
    }
}