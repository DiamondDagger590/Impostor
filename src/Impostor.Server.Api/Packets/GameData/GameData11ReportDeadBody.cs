using Impostor.Server.Net.Messages;

namespace Impostor.Server.Net.GameData
{
    public class GameData11ReportDeadBody
    {

        public GameData11ReportDeadBody(IMessageReader iMessageReader,
            int packetLength,
            byte netID)
        {
            ReportedPlayer = iMessageReader.ReadByte();
        }

        public byte ReportedPlayer { get; }
    }
}