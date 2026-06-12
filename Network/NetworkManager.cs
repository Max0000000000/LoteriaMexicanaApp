using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LoteriaMexicanaApp.Core;
using LoteriaMexicanaApp.UI;

namespace LoteriaMexicanaApp.Network
{
    public class NetworkManager
    {
        private TcpListener? _listener;
        private TcpClient? _client;
        private NetworkStream? _clientStream;
        private CancellationTokenSource? _cts;
        private readonly List<ClientHandler> _clients = new List<ClientHandler>();
        
        public bool IsHost { get; private set; }
        public bool IsConnected { get; private set; }
        public string LocalClientId { get; private set; } = Guid.NewGuid().ToString();
        public string LocalPlayerName { get; private set; } = "Jugador";
        
        public List<LobbyPlayer> ConnectedPlayers { get; } = new List<LobbyPlayer>();
        
        public List<int> HostCalledCardIds { get; } = new List<int>();

        // Host events
        public event Action<LobbyPlayer>? PlayerJoined;
        public event Action<string>? PlayerProgressUpdated; // notifies when player marks cell

        // Client events
        public event Action<Card>? CardReceived;
        public event Action<GameState>? GameStateReceived;
        public event Action<string, string>? LoteriaResultReceived; // winner name, details
        public event Action<string, string>? ChatReceived;
        public event Action? ConnectedToHost;
        public event Action? Disconnected;
        public event Action<string>? ErrorOccurred;

        // Shared event
        public event Action? PlayerListUpdated;

        /// <summary>
        /// Starts hosting a TCP LAN server.
        /// </summary>
        public void StartHost(int port, string hostPlayerName, Board hostBoard)
        {
            StartHost(port, hostPlayerName, new List<Board> { hostBoard });
        }

        public void StartHost(int port, string hostPlayerName, List<Board> hostBoards)
        {
            Stop();
            IsHost = true;
            IsConnected = true;
            LocalPlayerName = hostPlayerName;
            _cts = new CancellationTokenSource();

            lock (HostCalledCardIds)
            {
                HostCalledCardIds.Clear();
            }

            // Setup local host player
            ConnectedPlayers.Clear();
            var hostPlayer = new LobbyPlayer
            {
                Id = LocalClientId,
                Name = hostPlayerName,
                IsHost = true,
                BoardJson = JsonSerializer.Serialize(hostBoards),
                IsConnected = true
            };
            
            hostPlayer.MarkedCellsList.Clear();
            foreach (var b in hostBoards)
            {
                hostPlayer.MarkedCellsList.Add(new bool[25]);
            }
            
            ConnectedPlayers.Add(hostPlayer);

            try
            {
                _listener = new TcpListener(IPAddress.Any, port);
                _listener.Start();
                Task.Run(() => AcceptClientsAsync(_cts.Token));
            }
            catch (Exception ex)
            {
                Stop();
                ErrorOccurred?.Invoke($"Error al iniciar Host: {ex.Message}");
            }
        }

        private async Task AcceptClientsAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested && _listener != null)
            {
                try
                {
                    TcpClient client = await _listener.AcceptTcpClientAsync(token);
                    var handler = new ClientHandler(client, this);
                    lock (_clients)
                    {
                        _clients.Add(handler);
                    }
                    _ = Task.Run(() => handler.ReadLoopAsync(token));
                }
                catch
                {
                    // Listener stopped or token cancelled
                    break;
                }
            }
        }

        /// <summary>
        /// Connects to a Host LAN server as a client.
        /// </summary>
        public void ConnectToHost(string ip, int port, string playerName, Board board)
        {
            ConnectToHost(ip, port, playerName, new List<Board> { board });
        }

        public void ConnectToHost(string ip, int port, string playerName, List<Board> boards)
        {
            Stop();
            IsHost = false;
            LocalPlayerName = playerName;
            _cts = new CancellationTokenSource();

            try
            {
                _client = new TcpClient();
                // Async connect
                var connectTask = _client.ConnectAsync(ip, port);
                connectTask.Wait(5000); // 5 seconds timeout
                
                if (!_client.Connected)
                {
                    throw new TimeoutException("No se pudo conectar al servidor (Tiempo de espera agotado)");
                }

                _clientStream = _client.GetStream();
                IsConnected = true;
                ConnectedToHost?.Invoke();

                // Start listening for host packets
                Task.Run(() => ReadFromServerAsync(_cts.Token));

                // Send JOIN request
                var joinPacket = new NetworkPacket
                {
                    Type = "JOIN",
                    SenderId = LocalClientId,
                    SenderName = playerName,
                    PlayerName = playerName,
                    BoardJson = JsonSerializer.Serialize(boards)
                };
                SendPacketToServer(joinPacket);
            }
            catch (Exception ex)
            {
                Stop();
                ErrorOccurred?.Invoke($"Error de conexión: {ex.Message}");
            }
        }

        private async Task ReadFromServerAsync(CancellationToken token)
        {
            var reader = new StreamReader(_clientStream!, Encoding.UTF8);
            while (!token.IsCancellationRequested && _client != null && _client.Connected)
            {
                try
                {
                    string? line = await reader.ReadLineAsync(token);
                    if (line == null)
                    {
                        // Server disconnected
                        break;
                    }

                    var packet = JsonSerializer.Deserialize<NetworkPacket>(line);
                    if (packet != null)
                    {
                        HandlePacketAsClient(packet);
                    }
                }
                catch
                {
                    break;
                }
            }

            IsConnected = false;
            Disconnected?.Invoke();
        }

        /// <summary>
        /// Handles incoming packets on the client side.
        /// </summary>
        private void HandlePacketAsClient(NetworkPacket packet)
        {
            switch (packet.Type)
            {
                case "PLAYER_LIST":
                    lock (ConnectedPlayers)
                    {
                        ConnectedPlayers.Clear();
                        foreach (var pName in packet.Players)
                        {
                            // Parse name: Check if it's host or client
                            ConnectedPlayers.Add(new LobbyPlayer { Name = pName });
                        }
                    }
                    PlayerListUpdated?.Invoke();
                    break;

                case "CARD_DRAWN":
                    var card = Deck.BaseCards.FirstOrDefault(c => c.Id == packet.CardId);
                    if (card != null)
                    {
                        CardReceived?.Invoke(card);
                    }
                    break;

                case "GAME_STATE":
                    if (Enum.TryParse<GameState>(packet.GameState, out var state))
                    {
                        GameStateReceived?.Invoke(state);
                    }
                    break;

                case "LOTERIA_RESULT":
                    LoteriaResultReceived?.Invoke(packet.PlayerName, packet.WinningLineDescription);
                    break;

                case "CHAT":
                    ChatReceived?.Invoke(packet.SenderName, packet.MessageText);
                    break;
            }
        }

        /// <summary>
        /// Broadcasts game state to all players. Only callable by Host.
        /// </summary>
        public void HostBroadcastGameState(GameState state)
        {
            if (!IsHost) return;

            var packet = new NetworkPacket
            {
                Type = "GAME_STATE",
                GameState = state.ToString(),
                SenderId = LocalClientId,
                SenderName = LocalPlayerName
            };
            BroadcastToAllClients(packet);
        }

        /// <summary>
        /// Broadcasts the drawn card to all players. Only callable by Host.
        /// </summary>
        public void HostBroadcastCardDrawn(Card card)
        {
            if (!IsHost) return;

            lock (HostCalledCardIds)
            {
                if (!HostCalledCardIds.Contains(card.Id))
                {
                    HostCalledCardIds.Add(card.Id);
                }
            }

            var packet = new NetworkPacket
            {
                Type = "CARD_DRAWN",
                CardId = card.Id,
                SenderId = LocalClientId,
                SenderName = LocalPlayerName
            };
            BroadcastToAllClients(packet);
        }

        /// <summary>
        /// Broadcasts a chat message.
        /// </summary>
        public void SendChatMessage(string message)
        {
            var packet = new NetworkPacket
            {
                Type = "CHAT",
                MessageText = message,
                SenderId = LocalClientId,
                SenderName = LocalPlayerName
            };

            if (IsHost)
            {
                BroadcastToAllClients(packet);
                ChatReceived?.Invoke(LocalPlayerName, message);
            }
            else
            {
                SendPacketToServer(packet);
            }
        }

        /// <summary>
        /// Declares "Lotería!" to the host.
        /// </summary>
        public void ClientDeclareLoteria()
        {
            if (IsHost)
            {
                // Local host claim
                var hostPlayer = ConnectedPlayers.FirstOrDefault(p => p.Id == LocalClientId);
                if (hostPlayer != null)
                {
                    VerifyAndDeclareWinner(hostPlayer);
                }
            }
            else
            {
                var packet = new NetworkPacket
                {
                    Type = "LOTERIA_CLAIM",
                    SenderId = LocalClientId,
                    SenderName = LocalPlayerName
                };
                SendPacketToServer(packet);
            }
        }

        /// <summary>
        /// Sends marking changes to the Host.
        /// </summary>
        public void ClientNotifyCellMarked(int index, bool isMarked)
        {
            ClientNotifyCellMarked(0, index, isMarked);
        }

        public void ClientNotifyCellMarked(int boardIndex, int cellIndex, bool isMarked)
        {
            if (IsHost)
            {
                var hostPlayer = ConnectedPlayers.FirstOrDefault(p => p.Id == LocalClientId);
                if (hostPlayer != null && boardIndex >= 0 && boardIndex < hostPlayer.MarkedCellsList.Count)
                {
                    hostPlayer.MarkedCellsList[boardIndex][cellIndex] = isMarked;
                    PlayerProgressUpdated?.Invoke(hostPlayer.Name);
                    UpdatePlayerListAndBroadcast();
                }
            }
            else
            {
                var packet = new NetworkPacket
                {
                    Type = "MARK_CELL",
                    BoardIndex = boardIndex,
                    CellIndex = cellIndex,
                    IsMarked = isMarked,
                    SenderId = LocalClientId,
                    SenderName = LocalPlayerName
                };
                SendPacketToServer(packet);
            }
        }

        /// <summary>
        /// Verifies a player's win and broadcasts result.
        /// </summary>
        public void VerifyAndDeclareWinner(LobbyPlayer player)
        {
            if (!IsHost) return;

            try
            {
                List<Board> boards = new List<Board>();
                try
                {
                    boards = JsonSerializer.Deserialize<List<Board>>(player.BoardJson) ?? new List<Board>();
                }
                catch
                {
                    // Fallback to single board
                    var singleBoard = JsonSerializer.Deserialize<Board>(player.BoardJson);
                    if (singleBoard != null) boards.Add(singleBoard);
                }

                List<int> calledIds;
                lock (HostCalledCardIds)
                {
                    calledIds = HostCalledCardIds.ToList();
                }

                bool hasWon = false;
                string winningLineDesc = string.Empty;
                bool hasMarkedButNotCalled = false;
                string markedButNotCalledDesc = string.Empty;
                int falseWinBoardIndex = -1;

                for (int i = 0; i < boards.Count; i++)
                {
                    while (player.MarkedCellsList.Count <= i)
                    {
                        player.MarkedCellsList.Add(new bool[25]);
                    }

                    var winResult = boards[i].CheckWinWithCalled(player.MarkedCellsList[i], calledIds);
                    if (winResult.HasWon)
                    {
                        hasWon = true;
                        string boardWord = TranslationManager.CurrentLanguage == "EN" ? "Board" : "Tabla";
                        winningLineDesc = boards.Count > 1 
                            ? $"{boardWord} {i + 1} - {winResult.Description}" 
                            : winResult.Description;
                        break;
                    }
                    else if (winResult.HasMarkedButNotCalled && !hasMarkedButNotCalled)
                    {
                        hasMarkedButNotCalled = true;
                        markedButNotCalledDesc = winResult.MarkedButNotCalledDescription;
                        falseWinBoardIndex = i;
                    }
                }

                if (hasWon)
                {
                    // Valid win! Broadcast winner
                    var resultPacket = new NetworkPacket
                    {
                        Type = "LOTERIA_RESULT",
                        PlayerName = player.Name,
                        WinningLineDescription = winningLineDesc,
                        IsWinner = true
                    };
                    BroadcastToAllClients(resultPacket);
                    LoteriaResultReceived?.Invoke(player.Name, winningLineDesc);
                }
                else if (hasMarkedButNotCalled)
                {
                    // Invalid claim (marked uncalled cards), broadcast rejection
                    string boardWord = TranslationManager.CurrentLanguage == "EN" ? "Board" : "Tabla";
                    string boardDetailText = boards.Count > 1 ? $" ({boardWord} {falseWinBoardIndex + 1})" : string.Empty;

                    var chatPacket = new NetworkPacket
                    {
                        Type = "CHAT",
                        SenderName = "Sistema",
                        MessageText = $"¡Lotería falsa! {player.Name} tiene cartas marcadas en su línea de victoria ({markedButNotCalledDesc}){boardDetailText} que aún no han sido cantadas."
                    };
                    BroadcastToAllClients(chatPacket);
                    ChatReceived?.Invoke("Sistema", chatPacket.MessageText);
                }
                else
                {
                    // Invalid claim, broadcast rejection or notify sender
                    var chatPacket = new NetworkPacket
                    {
                        Type = "CHAT",
                        SenderName = "Sistema",
                        MessageText = $"¡Declaración inválida de Lotería por {player.Name}! Sigue el juego."
                    };
                    BroadcastToAllClients(chatPacket);
                    ChatReceived?.Invoke("Sistema", chatPacket.MessageText);
                }
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke($"Error al verificar ganador: {ex.Message}");
            }
        }

        private void BroadcastToAllClients(NetworkPacket packet)
        {
            string json = JsonSerializer.Serialize(packet) + "\n";
            byte[] bytes = Encoding.UTF8.GetBytes(json);

            lock (_clients)
            {
                foreach (var client in _clients)
                {
                    try
                    {
                        if (client.Client.Connected)
                        {
                            client.Stream.Write(bytes, 0, bytes.Length);
                        }
                    }
                    catch
                    {
                        // Ignore failed sends, they'll clean up on read fail
                    }
                }
            }
        }

        private void SendPacketToServer(NetworkPacket packet)
        {
            if (_client == null || !_client.Connected || _clientStream == null) return;
            try
            {
                string json = JsonSerializer.Serialize(packet) + "\n";
                byte[] bytes = Encoding.UTF8.GetBytes(json);
                _clientStream.Write(bytes, 0, bytes.Length);
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke($"Error al enviar datos: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates the client list on host and broadcasts to all clients.
        /// </summary>
        public void UpdatePlayerListAndBroadcast()
        {
            if (!IsHost) return;

            var packet = new NetworkPacket
            {
                Type = "PLAYER_LIST",
                Players = ConnectedPlayers.Select(p => $"{p.Name} ({(p.IsHost ? "Anfitrión" : p.MarkedCount + "/25")})").ToList()
            };
            BroadcastToAllClients(packet);
            PlayerListUpdated?.Invoke();
        }

        /// <summary>
        /// Stops the socket connections and listeners.
        /// </summary>
        public void Stop()
        {
            _cts?.Cancel();
            _listener?.Stop();
            _listener = null;

            _clientStream?.Close();
            _clientStream = null;

            _client?.Close();
            _client = null;

            lock (_clients)
            {
                foreach (var client in _clients)
                {
                    client.Close();
                }
                _clients.Clear();
            }

            IsConnected = false;
            IsHost = false;
        }

        // Nested client handler for Host mode
        private class ClientHandler
        {
            public TcpClient Client { get; }
            public NetworkStream Stream { get; }
            private readonly NetworkManager _manager;
            private LobbyPlayer? _player;

            public ClientHandler(TcpClient client, NetworkManager manager)
            {
                Client = client;
                Stream = client.GetStream();
                _manager = manager;
            }

            public async Task ReadLoopAsync(CancellationToken token)
            {
                var reader = new StreamReader(Stream, Encoding.UTF8);
                while (!token.IsCancellationRequested && Client.Connected)
                {
                    try
                    {
                        string? line = await reader.ReadLineAsync(token);
                        if (line == null) break;

                        var packet = JsonSerializer.Deserialize<NetworkPacket>(line);
                        if (packet != null)
                        {
                            HandlePacket(packet);
                        }
                    }
                    catch
                    {
                        break;
                    }
                }

                // Disconnected
                Close();
                if (_player != null)
                {
                    lock (_manager.ConnectedPlayers)
                    {
                        _manager.ConnectedPlayers.Remove(_player);
                    }
                    _manager.UpdatePlayerListAndBroadcast();
                }
                lock (_manager._clients)
                {
                    _manager._clients.Remove(this);
                }
            }

            private void HandlePacket(NetworkPacket packet)
            {
                switch (packet.Type)
                {
                    case "JOIN":
                        _player = new LobbyPlayer
                        {
                            Id = packet.SenderId,
                            Name = packet.PlayerName,
                            BoardJson = packet.BoardJson,
                            IsHost = false
                        };
                        
                        try
                        {
                            var boards = JsonSerializer.Deserialize<List<Board>>(packet.BoardJson);
                            if (boards != null)
                            {
                                foreach (var b in boards)
                                {
                                    _player.MarkedCellsList.Add(new bool[25]);
                                }
                            }
                        }
                        catch
                        {
                            try
                            {
                                var board = JsonSerializer.Deserialize<Board>(packet.BoardJson);
                                if (board != null)
                                {
                                    _player.MarkedCellsList.Add(new bool[25]);
                                }
                            }
                            catch { }
                        }

                        if (_player.MarkedCellsList.Count == 0)
                        {
                            _player.MarkedCellsList.Add(new bool[25]);
                        }

                        lock (_manager.ConnectedPlayers)
                        {
                            _manager.ConnectedPlayers.Add(_player);
                        }
                        _manager.UpdatePlayerListAndBroadcast();
                        _manager.PlayerJoined?.Invoke(_player);
                        break;

                    case "MARK_CELL":
                        if (_player != null)
                        {
                            while (_player.MarkedCellsList.Count <= packet.BoardIndex)
                            {
                                _player.MarkedCellsList.Add(new bool[25]);
                            }
                            _player.MarkedCellsList[packet.BoardIndex][packet.CellIndex] = packet.IsMarked;
                            _manager.PlayerProgressUpdated?.Invoke(_player.Name);
                            _manager.UpdatePlayerListAndBroadcast();
                        }
                        break;

                    case "LOTERIA_CLAIM":
                        if (_player != null)
                        {
                            _manager.VerifyAndDeclareWinner(_player);
                        }
                        break;

                    case "CHAT":
                        // Echo chat to all other clients
                        _manager.BroadcastToAllClients(packet);
                        _manager.ChatReceived?.Invoke(packet.SenderName, packet.MessageText);
                        break;
                }
            }

            public void Close()
            {
                Stream.Close();
                Client.Close();
            }
        }
    }
}
