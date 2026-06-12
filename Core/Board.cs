using System;
using System.Collections.Generic;
using System.Linq;

namespace LoteriaMexicanaApp.Core
{
    public class Board
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = "Nueva Tabla";
        
        // 25 cards in a 1D list for easy JSON serialization
        public List<Card> Cards { get; set; } = new List<Card>(25);

        public Board()
        {
            // Initialize with empty cards
            for (int i = 0; i < 25; i++)
            {
                Cards.Add(new Card());
            }
        }

        public Board(string name, List<Card> cards)
        {
            Id = Guid.NewGuid().ToString();
            Name = name;
            Cards = cards.ToList();
            
            // Pad if less than 25
            while (Cards.Count < 25)
            {
                Cards.Add(new Card());
            }
        }

        /// <summary>
        /// Gets a card at specific 0-indexed row and column.
        /// </summary>
        public Card GetCard(int row, int col)
        {
            int index = row * 5 + col;
            if (index >= 0 && index < Cards.Count)
            {
                return Cards[index];
            }
            return new Card();
        }

        /// <summary>
        /// Sets a card at specific 0-indexed row and column.
        /// </summary>
        public void SetCard(int row, int col, Card card)
        {
            int index = row * 5 + col;
            if (index >= 0 && index < Cards.Count)
            {
                Cards[index] = card;
            }
        }

        /// <summary>
        /// Generates a randomized board using unique cards from the base deck.
        /// </summary>
        public static Board GenerateRandom(string name)
        {
            var random = new Random();
            var randomCards = Deck.BaseCards
                .OrderBy(x => random.Next())
                .Take(25)
                .ToList();

            return new Board(name, randomCards);
        }

        /// <summary>
        /// Validates if the board has 25 valid cards.
        /// </summary>
        public bool IsValid()
        {
            if (Cards.Count != 25) return false;
            // Ensure no invalid card IDs (all should be between 1 and 54)
            return Cards.All(c => c.Id >= 1 && c.Id <= 54);
        }

        /// <summary>
        /// Checks if the board has a winning line given a set of marked positions.
        /// Returns (bool hasWon, List<int> winningIndices, string description).
        /// </summary>
        public (bool HasWon, List<int> WinningIndices, string Description) CheckWin(bool[] marked)
        {
            if (marked == null || marked.Length != 25)
            {
                return (false, new List<int>(), string.Empty);
            }

            var winningIndices = new List<int>();

            // Check Rows (5 rows)
            for (int r = 0; r < 5; r++)
            {
                bool rowWin = true;
                var rowIndices = new List<int>();
                for (int c = 0; c < 5; c++)
                {
                    int idx = r * 5 + c;
                    rowIndices.Add(idx);
                    if (!marked[idx])
                    {
                        rowWin = false;
                    }
                }
                if (rowWin)
                {
                    return (true, rowIndices, $"Fila {r + 1}");
                }
            }

            // Check Columns (5 columns)
            for (int c = 0; c < 5; c++)
            {
                bool colWin = true;
                var colIndices = new List<int>();
                for (int r = 0; r < 5; r++)
                {
                    int idx = r * 5 + c;
                    colIndices.Add(idx);
                    if (!marked[idx])
                    {
                        colWin = false;
                    }
                }
                if (colWin)
                {
                    return (true, colIndices, $"Columna {c + 1}");
                }
            }

            // Check Primary Diagonal (top-left to bottom-right)
            bool pDiagWin = true;
            var pDiagIndices = new List<int>();
            for (int i = 0; i < 5; i++)
            {
                int idx = i * 5 + i;
                pDiagIndices.Add(idx);
                if (!marked[idx])
                {
                    pDiagWin = false;
                }
            }
            if (pDiagWin)
            {
                return (true, pDiagIndices, "Diagonal Principal");
            }

            // Check Secondary Diagonal (top-right to bottom-left)
            bool sDiagWin = true;
            var sDiagIndices = new List<int>();
            for (int i = 0; i < 5; i++)
            {
                int idx = i * 5 + (4 - i);
                sDiagIndices.Add(idx);
                if (!marked[idx])
                {
                    sDiagWin = false;
                }
            }
            if (sDiagWin)
            {
                return (true, sDiagIndices, "Diagonal Secundaria");
            }

            return (false, new List<int>(), string.Empty);
        }

        /// <summary>
        /// Checks if the board has a winning line considering only cards that have been called.
        /// Also returns information about any marked line (even if not fully called) to give better feedback.
        /// </summary>
        public (bool HasWon, List<int> WinningIndices, string Description, bool HasMarkedButNotCalled, List<int> MarkedButNotCalledIndices, string MarkedButNotCalledDescription) CheckWinWithCalled(bool[] marked, List<int> calledCardIds)
        {
            if (marked == null || marked.Length != 25)
            {
                return (false, new List<int>(), string.Empty, false, new List<int>(), string.Empty);
            }

            var allLines = new List<(List<int> Indices, string Description)>();

            // Rows
            for (int r = 0; r < 5; r++)
            {
                var rowIndices = new List<int>();
                for (int c = 0; c < 5; c++) rowIndices.Add(r * 5 + c);
                allLines.Add((rowIndices, $"Fila {r + 1}"));
            }

            // Columns
            for (int c = 0; c < 5; c++)
            {
                var colIndices = new List<int>();
                for (int r = 0; r < 5; r++) colIndices.Add(r * 5 + c);
                allLines.Add((colIndices, $"Columna {c + 1}"));
            }

            // Diagonals
            var pDiag = new List<int>();
            for (int i = 0; i < 5; i++) pDiag.Add(i * 5 + i);
            allLines.Add((pDiag, "Diagonal Principal"));

            var sDiag = new List<int>();
            for (int i = 0; i < 5; i++) sDiag.Add(i * 5 + (4 - i));
            allLines.Add((sDiag, "Diagonal Secundaria"));

            // Find valid win lines (fully marked AND all cards called)
            foreach (var line in allLines)
            {
                bool isFullyMarked = line.Indices.All(idx => marked[idx]);
                if (isFullyMarked)
                {
                    bool allCalled = line.Indices.All(idx => calledCardIds.Contains(Cards[idx].Id));
                    if (allCalled)
                    {
                        return (true, line.Indices, line.Description, false, new List<int>(), string.Empty);
                    }
                }
            }

            // If no valid win, check if there are any fully marked lines (which means they have uncalled cards)
            foreach (var line in allLines)
            {
                bool isFullyMarked = line.Indices.All(idx => marked[idx]);
                if (isFullyMarked)
                {
                    return (false, new List<int>(), string.Empty, true, line.Indices, line.Description);
                }
            }

            return (false, new List<int>(), string.Empty, false, new List<int>(), string.Empty);
        }
    }
}
