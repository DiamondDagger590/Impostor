using Impostor.Server.Net;

namespace Impostor.Server.Events.Players
{
    public interface IPlayerEvent : IEvent
    {
        IClientPlayer Player { get;  }
    }
}