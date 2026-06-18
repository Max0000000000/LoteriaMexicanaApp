using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;

namespace LoteriaMexicanaApp.Core
{
    public class GameEngine
    {
        private System.Timers.Timer? _timer;
        private int _secondsElapsed;
        private Deck _deck = new Deck();

        public List<Board> LocalBoards { get; } = new List<Board>();
        public List<bool[]> LocalPlayersMarked { get; } = new List<bool[]>();

        // Backward compatibility properties for single board usage
        public Board LocalBoard => LocalBoards.Count > 0 ? LocalBoards[0] : new Board();
        public bool[] LocalPlayerMarked => LocalPlayersMarked.Count > 0 ? LocalPlayersMarked[0] : new bool[25];

        public List<Card> CalledCards { get; private set; } = new List<Card>();
        public Card? CurrentCard { get; private set; }
        public GameState State { get; private set; } = GameState.Lobby;
        public int IntervalSeconds { get; set; } = 5;
        public GamePattern ActiveWinPattern { get; set; } = GamePattern.FromEnum(WinPattern.Linea5);

        // Events
        public event Action<Card, int>? CardDrawn;
        public event Action<GameState>? StateChanged;
        public event Action? DeckExhausted;
        public event Action<int>? TimerTick; // Seconds elapsed in current card (0 to IntervalSeconds)

        public GameEngine()
        {
            SetupTimer();
        }

        private void SetupTimer()
        {
            _timer = new System.Timers.Timer(1000); // Trigger every second
            _timer.Elapsed += OnTimerElapsed;
            _timer.AutoReset = true;
        }

        private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
        {
            if (State != GameState.Playing) return;

            _secondsElapsed++;
            TimerTick?.Invoke(_secondsElapsed);

            if (_secondsElapsed >= IntervalSeconds)
            {
                _secondsElapsed = 0;
                DrawNextCard();
            }
        }

        /// <summary>
        /// Starts a new game with a single board (compatibility wrapper).
        /// </summary>
        public void StartGame(Board board, int duplicateCount = 0)
        {
            StartGame(new List<Board> { board }, duplicateCount);
        }

        /// <summary>
        /// Starts a new game with multiple boards.
        /// </summary>
        public void StartGame(List<Board> boards, int duplicateCount = 0)
        {
            LocalBoards.Clear();
            LocalBoards.AddRange(boards);

            LocalPlayersMarked.Clear();
            foreach (var b in boards)
            {
                LocalPlayersMarked.Add(new bool[25]);
            }

            CalledCards.Clear();
            CurrentCard = null;

            // Re-generate and shuffle deck
            _deck = new Deck();
            if (duplicateCount > 0)
            {
                _deck.GenerateCustomDeck(duplicateCount);
            }
            _deck.Shuffle();

            State = GameState.Playing;
            _secondsElapsed = 0;
            StateChanged?.Invoke(State);

            _timer?.Start();

            // Draw first card immediately
            DrawNextCard();
        }

        /// <summary>
        /// Inicializa el motor para el modo cliente LAN sin activar el temporizador de sorteo de cartas.
        /// </summary>
        public void InitializeClientGame(List<Board> boards)
        {
            _timer?.Stop();

            LocalBoards.Clear();
            LocalBoards.AddRange(boards);

            LocalPlayersMarked.Clear();
            foreach (var b in boards)
            {
                LocalPlayersMarked.Add(new bool[25]);
            }

            CalledCards.Clear();
            CurrentCard = null;

            State = GameState.Playing;
            _secondsElapsed = 0;
            StateChanged?.Invoke(State);
        }

        /// <summary>
        /// Manually draws the next card from the deck.
        /// </summary>
        public void DrawNextCard()
        {
            if (State != GameState.Playing && State != GameState.Paused) return;

            _timer?.Stop();
            _secondsElapsed = 0;

            var card = _deck.Draw();
            if (card == null)
            {
                State = GameState.Finished;
                StateChanged?.Invoke(State);
                DeckExhausted?.Invoke();
            }
            else
            {
                CurrentCard = card;
                CalledCards.Add(card);
                CardDrawn?.Invoke(card, _deck.Count);

                if (State == GameState.Playing)
                {
                    _timer?.Start();
                }
            }
        }

        /// <summary>
        /// Pauses the game.
        /// </summary>
        public void PauseGame()
        {
            if (State != GameState.Playing) return;
            State = GameState.Paused;
            _timer?.Stop();
            StateChanged?.Invoke(State);
        }

        /// <summary>
        /// Resumes a paused game.
        /// </summary>
        public void ResumeGame()
        {
            if (State != GameState.Paused) return;
            State = GameState.Playing;
            _timer?.Start();
            StateChanged?.Invoke(State);
        }

        /// <summary>
        /// Stops the game completely.
        /// </summary>
        public void StopGame()
        {
            State = GameState.Finished;
            _timer?.Stop();
            StateChanged?.Invoke(State);
        }

        /// <summary>
        /// Toggles a cell's marked state on the first board.
        /// </summary>
        public bool ToggleMarkCell(int index)
        {
            return ToggleMarkCell(0, index);
        }

        /// <summary>
        /// Toggles a cell's marked state on a specific board.
        /// </summary>
        public bool ToggleMarkCell(int boardIndex, int cellIndex)
        {
            if (boardIndex < 0 || boardIndex >= LocalPlayersMarked.Count) return false;
            if (cellIndex < 0 || cellIndex >= 25) return false;
            LocalPlayersMarked[boardIndex][cellIndex] = !LocalPlayersMarked[boardIndex][cellIndex];
            return true;
        }

        /// <summary>
        /// Explicitly marks a cell (for network synchronization).
        /// </summary>
        public void SetCellMarked(int index, bool marked)
        {
            SetCellMarked(0, index, marked);
        }

        /// <summary>
        /// Explicitly marks a cell on a specific board.
        /// </summary>
        public void SetCellMarked(int boardIndex, int cellIndex, bool marked)
        {
            if (boardIndex >= 0 && boardIndex < LocalPlayersMarked.Count)
            {
                if (cellIndex >= 0 && cellIndex < 25)
                {
                    LocalPlayersMarked[boardIndex][cellIndex] = marked;
                }
            }
        }

        /// <summary>
        /// Checks if the first local player has won (compatibility method).
        /// </summary>
        public (bool HasWon, List<int> WinningIndices, string Description) CheckLocalWin()
        {
            return LocalBoard.CheckWin(LocalPlayerMarked);
        }

        /// <summary>
        /// Checks if the first board has won considering only cards that have been called.
        /// </summary>
        public (bool HasWon, List<int> WinningIndices, string Description, bool HasMarkedButNotCalled, List<int> MarkedButNotCalledIndices, string MarkedButNotCalledDescription) CheckLocalWinWithCalled()
        {
            var calledIds = CalledCards.Select(c => c.Id).ToList();
            return LocalBoard.CheckWinWithCalled(LocalPlayerMarked, calledIds, ActiveWinPattern);
        }

        /// <summary>
        /// Checks if any of the active local boards has won considering only called cards.
        /// Returns detailed winning information, prioritising real win, then false win.
        /// </summary>
        public (bool HasWon, int BoardIndex, List<int> WinningIndices, string Description, bool HasMarkedButNotCalled, int MarkedButNotCalledBoardIndex, List<int> MarkedButNotCalledIndices, string MarkedButNotCalledDescription) CheckLocalWinWithCalledMulti()
        {
            var calledIds = CalledCards.Select(c => c.Id).ToList();

            // 1. Check if there's any actual win on any board
            for (int i = 0; i < LocalBoards.Count; i++)
            {
                var check = LocalBoards[i].CheckWinWithCalled(LocalPlayersMarked[i], calledIds, ActiveWinPattern);
                if (check.HasWon)
                {
                    return (true, i, check.WinningIndices, check.Description, false, -1, new List<int>(), string.Empty);
                }
            }

            // 2. If no actual win, check if there's any false win (marked but not called) on any board
            for (int i = 0; i < LocalBoards.Count; i++)
            {
                var check = LocalBoards[i].CheckWinWithCalled(LocalPlayersMarked[i], calledIds, ActiveWinPattern);
                if (check.HasMarkedButNotCalled)
                {
                    return (false, -1, new List<int>(), string.Empty, true, i, check.MarkedButNotCalledIndices, check.MarkedButNotCalledDescription);
                }
            }

            return (false, -1, new List<int>(), string.Empty, false, -1, new List<int>(), string.Empty);
        }
    }
}
