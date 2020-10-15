using System;
using Impostor.Server.Games;
using Impostor.Server.Net;

namespace Impostor.Server.Events.Players
{
    public sealed class PlayerJoinGameEvent : IPlayerEvent, IEventCancelable
    {

        public PlayerJoinGameEvent(IGame iGame, IClientPlayer player, bool rejoining)
        {
            Player = player;
            Game = iGame;
            IsCancelled = false;

            Random random = new Random();
            IsRejoining = rejoining;
            if (random.Next(2) == 1)
            {
                IsCancelled = true;
            }
        }

        public bool IsRejoining { get; }

        public IGame Game { get; }

        public IClientPlayer Player { get; }
        
        public bool IsCancelled { get; set; }
    }
}