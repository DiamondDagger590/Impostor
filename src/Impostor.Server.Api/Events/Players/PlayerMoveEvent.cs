using System;
using Impostor.Server.Games;
using Impostor.Server.Net;
using Impostor.Server.Net.Data;

namespace Impostor.Server.Events.Players
{
    public sealed class PlayerMoveEvent : IPlayerEvent
    {

        public PlayerMoveEvent(IGame iGame, IClientPlayer player, Data06Movement data06Movement)
        {
            Player = player;
            Game = iGame;
            MovementPacket = data06Movement;
        }

        public IGame Game { get; }

        public IClientPlayer Player { get; }

        public Data06Movement MovementPacket { get; }
    }
}