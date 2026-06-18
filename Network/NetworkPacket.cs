using System;
using System.Collections.Generic;
using System.Linq;

namespace LoteriaMexicanaApp.Network
{
    public class NetworkPacket
    {
        public string Type { get; set; } = string.Empty; // JOIN, JOIN_ACK, PLAYER_LIST, GAME_STATE, CARD_DRAWN, MARK_CELL, LOTERIA_CLAIM, LOTERIA_RESULT, CHAT
        public string SenderId { get; set; } = string.Empty;
        public string SenderName { get; set; } = string.Empty;

        // Dynamic payload properties
        public string PlayerName { get; set; } = string.Empty;
        public string GameState { get; set; } = string.Empty;
        public int CardId { get; set; }
        public int BoardIndex { get; set; } // Identifies which board is being marked (0-indexed)
        public int CellIndex { get; set; }
        public bool IsMarked { get; set; }
        public bool IsWinner { get; set; }
        public string MessageText { get; set; } = string.Empty;
        public List<string> Players { get; set; } = new List<string>();
        public string BoardJson { get; set; } = string.Empty; // Serialized Board or List<Board> object
        public string WinningLineDescription { get; set; } = string.Empty;
        public bool IsDoublesMode { get; set; }
        public string WinPattern { get; set; } = "Linea5";
        public string CustomPatternData { get; set; } = string.Empty;
    }

    public class LobbyPlayer
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool IsHost { get; set; }
        public bool IsConnected { get; set; } = true;
        public string BoardJson { get; set; } = string.Empty;
        public List<bool[]> MarkedCellsList { get; set; } = new List<bool[]>();

        // Backward compatibility property for single-board references
        public bool[] MarkedCells
        {
            get => MarkedCellsList.Count > 0 ? MarkedCellsList[0] : new bool[25];
            set
            {
                if (MarkedCellsList.Count == 0)
                {
                    MarkedCellsList.Add(value);
                }
                else
                {
                    MarkedCellsList[0] = value;
                }
            }
        }

        public int MarkedCount => MarkedCellsList.Sum(arr => arr.Count(c => c));
    }
}
