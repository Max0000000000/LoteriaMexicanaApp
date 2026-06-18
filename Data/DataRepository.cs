using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using LoteriaMexicanaApp.Core;

namespace LoteriaMexicanaApp.Data
{
    public class MatchRecord
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public DateTime Date { get; set; } = DateTime.Now;
        public string GameMode { get; set; } = "Solitario"; // Solitario or LAN
        public string LocalPlayerName { get; set; } = "Jugador";
        public bool LocalPlayerWon { get; set; }
        public string WinnerName { get; set; } = string.Empty;
        public string WinningLine { get; set; } = string.Empty;
        public List<string> Opponents { get; set; } = new List<string>();
    }

    public class PlayerStats
    {
        public string Username { get; set; } = "Jugador";
        public int GamesPlayed { get; set; }
        public int WinsLocal { get; set; }
        public int WinsLan { get; set; }
        public List<MatchRecord> MatchHistory { get; set; } = new List<MatchRecord>();
    }

    public class DataRepository
    {
        private readonly string _basePath;
        private readonly string _boardsPath;
        private readonly string _statsFilePath;
        private readonly string _patternsPath;

        public DataRepository()
        {
            // Set base path to the application directory
            _basePath = AppDomain.CurrentDomain.BaseDirectory;
            _boardsPath = Path.Combine(_basePath, "SavedBoards");
            _statsFilePath = Path.Combine(_basePath, "stats.json");
            _patternsPath = Path.Combine(_basePath, "SavedPatterns");

            // Ensure directories exist
            Directory.CreateDirectory(_boardsPath);
            Directory.CreateDirectory(_patternsPath);
        }

        // --- BOARD PERSISTENCE ---

        /// <summary>
        /// Saves a board to a JSON file.
        /// </summary>
        public void SaveBoard(Board board)
        {
            if (string.IsNullOrEmpty(board.Name))
            {
                board.Name = $"Tabla_{DateTime.Now:yyyyMMdd_HHmmss}";
            }

            // Clean invalid filename chars
            string safeName = string.Join("_", board.Name.Split(Path.GetInvalidFileNameChars()));
            string filePath = Path.Combine(_boardsPath, $"{safeName}.json");

            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(board, options);
            File.WriteAllText(filePath, json);
        }

        /// <summary>
        /// Loads a board from a JSON file.
        /// </summary>
        public Board? LoadBoard(string filePath)
        {
            if (!File.Exists(filePath)) return null;

            try
            {
                string json = File.ReadAllText(filePath);
                return JsonSerializer.Deserialize<Board>(json);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Lists all saved boards.
        /// </summary>
        public List<string> GetSavedBoardFiles()
        {
            var files = Directory.GetFiles(_boardsPath, "*.json");
            return new List<string>(files);
        }

        /// <summary>
        /// Loads a default board if none exist, or generates and saves one.
        /// </summary>
        public Board GetDefaultBoard()
        {
            var files = GetSavedBoardFiles();
            if (files.Count > 0)
            {
                var board = LoadBoard(files[0]);
                if (board != null && board.IsValid()) return board;
            }

            // Generate and save a default one
            var defaultBoard = Board.GenerateRandom("Tabla Predeterminada");
            SaveBoard(defaultBoard);
            return defaultBoard;
        }

        // --- STATISTICS PERSISTENCE ---

        /// <summary>
        /// Loads user statistics. Creates default if file doesn't exist.
        /// </summary>
        public PlayerStats LoadStats(string username = "Jugador")
        {
            if (!File.Exists(_statsFilePath))
            {
                return new PlayerStats { Username = username };
            }

            try
            {
                string json = File.ReadAllText(_statsFilePath);
                var stats = JsonSerializer.Deserialize<PlayerStats>(json);
                if (stats != null)
                {
                    // Update username if it is empty
                    if (string.IsNullOrWhiteSpace(stats.Username))
                    {
                        stats.Username = username;
                    }
                    return stats;
                }
            }
            catch
            {
                // Fallback
            }

            return new PlayerStats { Username = username };
        }

        /// <summary>
        /// Saves player statistics to stats.json.
        /// </summary>
        public void SaveStats(PlayerStats stats)
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(stats, options);
                File.WriteAllText(_statsFilePath, json);
            }
            catch
            {
                // Handle file locked errors silently or log
            }
        }

        /// <summary>
        /// Records a completed match and updates the player stats.
        /// </summary>
        public void RecordMatch(string localUsername, string gameMode, bool localPlayerWon, string winnerName, string winningLine, List<string> opponents)
        {
            var stats = LoadStats(localUsername);
            stats.Username = localUsername;
            stats.GamesPlayed++;

            if (localPlayerWon)
            {
                if (gameMode.Equals("LAN", StringComparison.OrdinalIgnoreCase))
                {
                    stats.WinsLan++;
                }
                else
                {
                    stats.WinsLocal++;
                }
            }

            var record = new MatchRecord
            {
                GameMode = gameMode,
                LocalPlayerName = localUsername,
                LocalPlayerWon = localPlayerWon,
                WinnerName = winnerName,
                WinningLine = winningLine,
                Opponents = opponents
            };

            stats.MatchHistory.Insert(0, record); // Most recent first
            SaveStats(stats);
        }

        // --- CUSTOM PATTERN PERSISTENCE ---

        public void SaveCustomPattern(GamePattern pattern)
        {
            if (string.IsNullOrEmpty(pattern.Name))
            {
                pattern.Name = $"Patron_{DateTime.Now:yyyyMMdd_HHmmss}";
            }

            // Clean invalid filename chars
            string safeName = string.Join("_", pattern.Name.Split(Path.GetInvalidFileNameChars()));
            string filePath = Path.Combine(_patternsPath, $"{safeName}.json");

            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(pattern, options);
            File.WriteAllText(filePath, json);
        }

        public List<GamePattern> LoadCustomPatterns()
        {
            var patterns = new List<GamePattern>();
            if (!Directory.Exists(_patternsPath)) return patterns;

            string[] files = Directory.GetFiles(_patternsPath, "*.json");
            foreach (var file in files)
            {
                try
                {
                    string json = File.ReadAllText(file);
                    var pattern = JsonSerializer.Deserialize<GamePattern>(json);
                    if (pattern != null)
                    {
                        patterns.Add(pattern);
                    }
                }
                catch { }
            }
            return patterns;
        }

        public void DeleteCustomPattern(string name)
        {
            string safeName = string.Join("_", name.Split(Path.GetInvalidFileNameChars()));
            string filePath = Path.Combine(_patternsPath, $"{safeName}.json");
            if (File.Exists(filePath))
            {
                try
                {
                    File.Delete(filePath);
                }
                catch { }
            }
        }
    }
}
