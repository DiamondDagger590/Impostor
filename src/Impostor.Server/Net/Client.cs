using System;
using System.Threading.Tasks;
using Impostor.Server.Data;
using Impostor.Server.Events.Players;
using Impostor.Server.Games;
using Impostor.Server.Games.Managers;
using Impostor.Server.Net.Data;
using Impostor.Server.Net.GameData;
using Impostor.Server.Net.Manager;
using Impostor.Server.Net.Messages;
using Impostor.Shared.Innersloth;
using Impostor.Shared.Innersloth.Data;
using Serilog;
using ILogger = Serilog.ILogger;

namespace Impostor.Server.Net
{
    internal class Client : ClientBase
    {
        private static readonly ILogger Logger = Log.ForContext<Client>();

        private readonly IClientManager _clientManager;
        private readonly IGameManager _gameManager;

        public Client(IClientManager clientManager, IGameManager gameManager, string name, IConnection connection)
            : base(name, connection)
        {
            _clientManager = clientManager;
            _gameManager = gameManager;
        }

        public override async ValueTask HandleMessageAsync(IMessage message)
        {
            var reader = message.CreateReader();

            var flag = reader.Tag;

            Logger.Verbose("[{0}] Server got {1}.", Id, flag);

            switch (flag)
            {
                case MessageFlags.HostGame:
                {
                    // Read game settings.
                    var gameInfo = Message00HostGame.Deserialize(reader);

                    // Create game.
                    var game = await _gameManager.CreateAsync(gameInfo);

                    // Code in the packet below will be used in JoinGame.
                    using var writer = Connection.CreateMessage(MessageType.Reliable);
                    Message00HostGame.Serialize(writer, game.Code);

                    await writer.SendAsync();

                    break;
                }

                case MessageFlags.JoinGame:
                {
                    Message01JoinGame.Deserialize(
                        reader,
                        out var gameCode,
                        out _);

                    var game = _gameManager.Find(gameCode);
                    if (game == null)
                    {
                        await SendDisconnectReason(DisconnectReason.GameMissing);
                        return;
                    }

                    var result = await game.AddClientAsync(this);

                    switch (result.Error)
                    {
                        case GameJoinError.None:
                            break;
                        case GameJoinError.InvalidClient:
                            await SendDisconnectReason(DisconnectReason.Custom, "Client is in an invalid state.");
                            break;
                        case GameJoinError.Banned:
                            await SendDisconnectReason(DisconnectReason.Banned);
                            break;
                        case GameJoinError.GameFull:
                            await SendDisconnectReason(DisconnectReason.GameFull);
                            break;
                        case GameJoinError.InvalidLimbo:
                            await SendDisconnectReason(DisconnectReason.Custom, "Invalid limbo state while joining.");
                            break;
                        case GameJoinError.GameStarted:
                            await SendDisconnectReason(DisconnectReason.GameStarted);
                            break;
                        case GameJoinError.GameDestroyed:
                            await SendDisconnectReason(DisconnectReason.Custom, DisconnectMessages.Destroyed);
                            break;
                        case GameJoinError.Custom:
                            await SendDisconnectReason(DisconnectReason.Custom, result.Message);
                            break;
                        default:
                            await SendDisconnectReason(DisconnectReason.Custom, "Unknown error.");
                            break;
                    }

                    break;
                }

                case MessageFlags.StartGame:
                {
                    if (!IsPacketAllowed(reader, true))
                    {
                        return;
                    }

                    await Player.Game.HandleStartGame(reader);
                    break;
                }

                // No idea how this flag is triggered.
                case MessageFlags.RemoveGame:
                    break;

                case MessageFlags.RemovePlayer:
                {
                    if (!IsPacketAllowed(reader, true))
                    {
                        return;
                    }

                    Message04RemovePlayer.Deserialize(
                        reader,
                        out var playerId,
                        out var reason);

                    await Player.Game.HandleRemovePlayer(playerId, (DisconnectReason)reason);
                    break;
                }

                case MessageFlags.GameData:
                case MessageFlags.GameDataTo:
                {
                    Logger.Information("Game Data: " + (flag == MessageFlags.GameData) + " " +
                                       (flag == MessageFlags.GameDataTo));

                    if (!IsPacketAllowed(reader, false))
                    {
                        return;
                    }

                    // Create a new reader. I'm not sure how exactly this works as I know 0 C# but yolo
                    var reader2 = message.CreateReader();
                    // Observed breaking with any length lower, so use this for now
                    if (reader2.Length >= 5)
                    {
                        // We want to store the gamecode in an arry for usage later. We don't *really* care about it currently, but it's here if we end up needing it
                        byte[] gameCode = new byte[4];

                        for (int i = 0; i < 4; i++)
                        {
                            gameCode[i] = reader2.ReadByte();
                        }

                        // while (reader2.Position < reader2.Length) //This is here because sometimes the packet can have multiple sub packets? I've yet to observe this and it's yet to break so keeping it commented until it breaks
                        {
                            var length = reader2.ReadByte() + (reader2.ReadByte() << 8);
                            Logger.Information("Length of packet: " + length);

                            var type = reader2.ReadByte();
                            Logger.Information("Type: " + type);

                            switch (type)
                            {
                                case 1: // Data

                                    var dataType = reader2.ReadByte();

                                    switch (dataType)
                                    {
                                        case DataFlags.Movement:
                                            reader2.ReadByte();
                                            reader2.ReadByte();

                                            var data06Movement = new Data06Movement(reader2);

                                            var playerMoveEvent = new PlayerMoveEvent(Player.Game, Player, data06Movement);
                                            await (this._gameManager as GameManager).GetManager().CallAsync(playerMoveEvent);

                                            break;
                                        default:
                                            // Handle things like movement packets here in the future
                                            for (int i = 0; i < length; i++)
                                            {
                                                Logger.Information("" + reader2.ReadByte());
                                            }

                                            break;
                                    }

                                    break;
                                case 2: // RPC

                                    // Grab the net ID. We don't need it for anything at this stage but store it in case
                                    var netID = reader2.ReadByte();
                                    Logger.Information("Net ID: " + netID);

                                    var rpcType = reader2.ReadByte();

                                    Logger.Information("RPC Type: " + rpcType);

                                    switch (rpcType)
                                    {
                                        case GameDataFlags.SyncSettings:
                                            GameData02SyncSettings gameData02ChangeSettings =
                                                new GameData02SyncSettings(reader2, length, netID);

                                            Logger.Information("Player Speed: " + gameData02ChangeSettings.PlayerSpeed +
                                                               ", Emergency Meetings: " +
                                                               gameData02ChangeSettings.EmergencyMeetings +
                                                               ", Emergency Cooldown: " +
                                                               gameData02ChangeSettings.EmergencyCooldown);
                                            break;
                                        case GameDataFlags.ReportDeadBody:
                                            GameData11ReportDeadBody gameData11ReportDeadBody =
                                                new GameData11ReportDeadBody(reader2, length, netID);

                                            Logger.Information("Reported Body: " + gameData11ReportDeadBody.ReportedPlayer);
                                            break;
                                        default:
                                            // Clean off reader
                                            for (int i = 0; i < length - 1; i++)
                                            {
                                                reader2.ReadByte();
                                            }

                                            break;
                                    }

                                    break;
                            }
                        }
                    }

                    // Broadcast packet to all other players.
                    using var writer = Player.Game.CreateMessage(message.Type);

                    if (flag == MessageFlags.GameDataTo)
                    {
                        var target = reader.ReadPackedInt32();

                        if (Player.Game.TryGetPlayer(target, out var playerTarget))
                        {
                            Logger.Information("Caller: " + Player.Client.Name + " Target: " +
                                               playerTarget.Client.Name);
                        }

                        Logger.Information("Game Data To Message: " + message.CreateReader().ReadPackedInt32());

                        reader.CopyTo(writer);
                        await writer.SendToAsync(target);
                    }
                    else
                    {
                        reader.CopyTo(writer);
                        await writer.SendToAllExceptAsync(Id);
                    }

                    break;
                }

                case MessageFlags.EndGame:
                {
                    if (!IsPacketAllowed(reader, true))
                    {
                        return;
                    }

                    await Player.Game.HandleEndGame(reader);
                    break;
                }

                case MessageFlags.AlterGame:
                {
                    if (!IsPacketAllowed(reader, true))
                    {
                        return;
                    }

                    Message10AlterGame.Deserialize(
                        reader,
                        out var gameTag,
                        out var value);

                    if (gameTag != AlterGameTags.ChangePrivacy)
                    {
                        return;
                    }

                    await Player.Game.HandleAlterGame(reader, Player, value);
                    break;
                }

                case MessageFlags.KickPlayer:
                {
                    if (!IsPacketAllowed(reader, true))
                    {
                        return;
                    }

                    Message11KickPlayer.Deserialize(
                        reader,
                        out var playerId,
                        out var isBan);

                    await Player.Game.HandleKickPlayer(playerId, isBan);
                    break;
                }

                case MessageFlags.GetGameListV2:
                {
                    Message16GetGameListV2.Deserialize(reader, out var options);
                    await OnRequestGameList(options);
                    break;
                }

                default:
                    Logger.Warning("Server received unknown flag {0}.", flag);
                    break;
            }

#if DEBUG
            if (flag != MessageFlags.GameData &&
                flag != MessageFlags.GameDataTo &&
                flag != MessageFlags.EndGame &&
                reader.Position < reader.Length)
            {
                Logger.Warning(
                    "Server did not consume all bytes from {0} ({1} < {2}).",
                    flag,
                    reader.Position,
                    reader.Length);
            }
#endif
        }

        public override async ValueTask HandleDisconnectAsync()
        {
            try
            {
                if (Player != null)
                {
                    await Player.Game.HandleRemovePlayer(Id, DisconnectReason.ExitGame);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Exception caught in client disconnection.");
            }

            _clientManager.Remove(this);
        }

        private bool IsPacketAllowed(IMessageReader message, bool hostOnly)
        {
            if (Player == null)
            {
                return false;
            }

            var game = Player.Game;

            // GameCode must match code of the current game assigned to the player.
            if (message.ReadInt32() != game.Code)
            {
                return false;
            }

            // Some packets should only be sent by the host of the game.
            if (hostOnly)
            {
                if (game.HostId == Id)
                {
                    return true;
                }

                Logger.Warning("[{0}] Client sent packet only allowed by the host ({1}).", Id, game.HostId);
                return false;
            }

            return true;
        }

        /// <summary>
        ///     Triggered when the connected client requests the game listing.
        /// </summary>
        /// <param name="options">
        ///     All options given.
        ///     At this moment, the client can only specify the map, impostor count and chat language.
        /// </param>
        private async ValueTask OnRequestGameList(GameOptionsData options)
        {
            using var message = Connection.CreateMessage(MessageType.Reliable);
            var games = _gameManager.FindListings((MapFlags)options.MapId, options.NumImpostors, options.Keywords);

            var skeldGameCount = _gameManager.GetGameCount(MapFlags.Skeld);
            var miraHqGameCount = _gameManager.GetGameCount(MapFlags.MiraHQ);
            var polusGameCount = _gameManager.GetGameCount(MapFlags.Polus);

            Message16GetGameListV2.Serialize(message, skeldGameCount, miraHqGameCount, polusGameCount, games);

            await message.SendAsync();
        }

        private async ValueTask SendDisconnectReason(DisconnectReason reason, string message = null)
        {
            if (Connection == null)
            {
                return;
            }

            using var packet = Connection.CreateMessage(MessageType.Reliable);
            Message01JoinGame.SerializeError(packet, false, reason, message);
            await packet.SendAsync();
        }
    }
}