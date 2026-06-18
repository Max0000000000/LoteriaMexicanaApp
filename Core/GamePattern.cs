using System;
using System.Collections.Generic;

namespace LoteriaMexicanaApp.Core
{
    public class GamePattern
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty; // Holds either standard Enum name (e.g. "Linea5") or Custom Name (e.g. "Esquinas y Centro")
        public bool IsCustom { get; set; }
        
        // List of winning combinations. Each combination is a list of cell indices (0 to 24) on the 5x5 board.
        // The player wins if they complete all cells in any of these combinations.
        public List<List<int>> Combinations { get; set; } = new List<List<int>>();

        public GamePattern()
        {
        }

        public static GamePattern FromEnum(WinPattern pattern)
        {
            var gp = new GamePattern
            {
                Id = pattern.ToString(),
                Name = pattern.ToString(),
                IsCustom = false
            };

            var combinations = Board.GetPatternCombinations(pattern);
            foreach (var comb in combinations)
            {
                gp.Combinations.Add(comb.Indices);
            }

            return gp;
        }
    }
}
