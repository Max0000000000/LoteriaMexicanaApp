using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Forms;
using LoteriaMexicanaApp.Core;
using LoteriaMexicanaApp.Data;
using LoteriaMexicanaApp.Network;

namespace LoteriaMexicanaApp.UI
{
    public class MainForm : Form
    {
        private readonly GameEngine _engine;
        private readonly NetworkManager _network;
        private readonly SoundManager _sounds;
        private readonly DataRepository _repository;

        // Active board tracking
        private Board _currentBoard = null!;
        private List<Board> _activeBoards = new List<Board>();

        // UI Components
        private CardControl _currentCardPreview = null!;
        private Label _lblRiddle = null!;
        private Label _lblRemainingCards = null!;
        private ProgressBar _pbTimer = null!;
        
        private TableLayoutPanel _boardsContainer = null!;
        private List<List<CardControl>> _boardsCardControls = new List<List<CardControl>>();
        private List<Label> _boardTitleLabels = new List<Label>();
        
        private FlowLayoutPanel _historyPanel = null!;
        private ListBox _lstPlayers = null!;
        private ListBox _lstChat = null!;
        private TextBox _txtChatMessage = null!;
        private Button _btnSendChat = null!;
        private TextBox _txtUsername = null!;

        private NumericUpDown _numInterval = null!;
        private ComboBox _cmbChipType = null!;
        private NumericUpDown _numBoards = null!;
        private ComboBox _cmbLanguage = null!;

        // Chip Style configuration accessible from CardControl
        public static string SelectedChipStyle { get; private set; } = "Frijolito";

        // Localized Labels for references in translation updates
        private Label _lblProfileHeader = null!;
        private Label _lblConfigHeader = null!;
        private Label _lblSpeedHeader = null!;
        private Label _lblBoardsHeader = null!;
        private Label _lblChipsHeader = null!;
        private Label _lblLanguageHeader = null!;
        private Label _lblPlayersHeader = null!;
        private Label _lblChatHeader = null!;
        private Label _lblPreviewHeader = null!;
        private Label _lblHistoryHeader = null!;

        // Control Buttons
        private Button _btnStart = null!;
        private Button _btnPause = null!;
        private Button _btnStop = null!;
        private Button _btnConnectLan = null!;
        private Button _btnDesignBoard = null!;
        private Button _btnStats = null!;
        private Button _btnClaimLoteria = null!;
        
        private Label _lblGameMode = null!;
        private Label _lblLanStatus = null!;

        private string _currentGameMode = "Solitario"; // Solitario or LAN
        private string _username = "Jugador";

        public MainForm()
        {
            _repository = new DataRepository();
            _engine = new GameEngine();
            _network = new NetworkManager();
            _sounds = new SoundManager();

            InitializeComponent();
            SetupEventHandlers();
            
            // Set default board and load it
            _currentBoard = _repository.GetDefaultBoard();
            RecreateBoards(1);
            
            UpdateLocalization();
            UpdateControlStates();

            // Trigger background download of card images
            ImageCache.StartDownloadBackground();
        }

        private void InitializeComponent()
        {
            this.Size = new Size(1150, 750);
            this.MinimumSize = new Size(1150, 720);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(18, 18, 18);
            this.ForeColor = Color.FromArgb(230, 230, 230);
            this.Font = new Font("Segoe UI", 9.5f);

            // 1. LEFT SIDEBAR (Controls & LAN)
            Panel leftPanel = new Panel
            {
                Dock = DockStyle.Left,
                Width = 260,
                BackColor = Color.FromArgb(30, 30, 30),
                Padding = new Padding(15)
            };

            _lblProfileHeader = new Label { Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = Color.Gray, Location = new Point(15, 15), Size = new Size(230, 15) };
            _txtUsername = new TextBox
            {
                Text = "Jugador_" + new Random().Next(10, 99),
                Location = new Point(15, 35),
                Size = new Size(230, 25),
                BackColor = Color.FromArgb(45, 45, 45),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            _txtUsername.TextChanged += (s, e) => _username = _txtUsername.Text.Trim();
            _username = _txtUsername.Text;

            _lblConfigHeader = new Label { Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = Color.Gray, Location = new Point(15, 75), Size = new Size(230, 15) };
            
            _btnDesignBoard = new Button
            {
                Location = new Point(15, 95),
                Size = new Size(110, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(55, 55, 55),
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            _btnDesignBoard.FlatAppearance.BorderSize = 0;
            _btnDesignBoard.Click += BtnDesignBoard_Click;

            _btnStats = new Button
            {
                Location = new Point(135, 95),
                Size = new Size(110, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(55, 55, 55),
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            _btnStats.FlatAppearance.BorderSize = 0;
            _btnStats.Click += BtnStats_Click;

            _lblSpeedHeader = new Label { Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), ForeColor = Color.Gray, Location = new Point(15, 135), Size = new Size(230, 15) };
            _numInterval = new NumericUpDown
            {
                Location = new Point(15, 150),
                Size = new Size(230, 25),
                BackColor = Color.FromArgb(45, 45, 45),
                ForeColor = Color.White,
                Minimum = 3,
                Maximum = 15,
                Value = 7,
                BorderStyle = BorderStyle.FixedSingle
            };
            _numInterval.ValueChanged += (s, e) => {
                _engine.IntervalSeconds = (int)_numInterval.Value;
                _pbTimer.Maximum = _engine.IntervalSeconds;
            };
            _engine.IntervalSeconds = (int)_numInterval.Value;

            _lblBoardsHeader = new Label { Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), ForeColor = Color.Gray, Location = new Point(15, 185), Size = new Size(230, 15) };
            _numBoards = new NumericUpDown
            {
                Location = new Point(15, 200),
                Size = new Size(230, 25),
                BackColor = Color.FromArgb(45, 45, 45),
                ForeColor = Color.White,
                Minimum = 1,
                Maximum = 4,
                Value = 1,
                BorderStyle = BorderStyle.FixedSingle
            };
            _numBoards.ValueChanged += (s, e) => {
                if (_engine.State == GameState.Lobby || _engine.State == GameState.Finished)
                {
                    RecreateBoards((int)_numBoards.Value);
                    UpdateLocalization();
                }
            };

            _lblChipsHeader = new Label { Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), ForeColor = Color.Gray, Location = new Point(15, 235), Size = new Size(230, 15) };
            _cmbChipType = new ComboBox
            {
                Location = new Point(15, 250),
                Size = new Size(230, 25),
                BackColor = Color.FromArgb(45, 45, 45),
                ForeColor = Color.White,
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat
            };
            _cmbChipType.Items.AddRange(new object[] { "Frijolito", "Ficha Roja", "Ficha Azul", "Ficha Verde", "Ficha Amarilla" });
            _cmbChipType.SelectedIndex = 0;
            _cmbChipType.SelectedIndexChanged += (s, e) => {
                SelectedChipStyle = _cmbChipType.SelectedItem?.ToString() ?? "Frijolito";
                foreach (var ctrlList in _boardsCardControls)
                {
                    foreach (var ctrl in ctrlList)
                    {
                        ctrl.Invalidate();
                    }
                }
            };
            SelectedChipStyle = _cmbChipType.SelectedItem?.ToString() ?? "Frijolito";

            _lblLanguageHeader = new Label { Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), ForeColor = Color.Gray, Location = new Point(15, 285), Size = new Size(230, 15) };
            _cmbLanguage = new ComboBox
            {
                Location = new Point(15, 300),
                Size = new Size(230, 25),
                BackColor = Color.FromArgb(45, 45, 45),
                ForeColor = Color.White,
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat
            };
            _cmbLanguage.Items.AddRange(new object[] { "Español", "English" });
            _cmbLanguage.SelectedIndex = 0;
            _cmbLanguage.SelectedIndexChanged += (s, e) => {
                TranslationManager.CurrentLanguage = _cmbLanguage.SelectedIndex == 1 ? "EN" : "ES";
                UpdateLocalization();
                _sounds.UpdateVoice();
            };

            _btnConnectLan = new Button
            {
                Location = new Point(15, 335),
                Size = new Size(230, 35),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(33, 150, 243),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnConnectLan.FlatAppearance.BorderSize = 0;
            _btnConnectLan.Click += BtnConnectLan_Click;

            _lblGameMode = new Label
            {
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(76, 175, 80),
                Location = new Point(15, 380),
                Size = new Size(230, 20)
            };

            _lblLanStatus = new Label
            {
                Font = new Font("Segoe UI", 8.5f, FontStyle.Italic),
                ForeColor = Color.Gray,
                Location = new Point(15, 400),
                Size = new Size(230, 18)
            };

            // Lobby Player List
            _lblPlayersHeader = new Label { Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = Color.Gray, Location = new Point(15, 425), Size = new Size(230, 15) };
            _lstPlayers = new ListBox
            {
                Location = new Point(15, 445),
                Size = new Size(230, 65),
                BackColor = Color.FromArgb(40, 40, 40),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.None
            };

            // Lobby Chat Room
            _lblChatHeader = new Label { Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = Color.Gray, Location = new Point(15, 515), Size = new Size(230, 15) };
            _lstChat = new ListBox
            {
                Location = new Point(15, 535),
                Size = new Size(230, 100),
                BackColor = Color.FromArgb(40, 40, 40),
                ForeColor = Color.FromArgb(200, 200, 200),
                BorderStyle = BorderStyle.None,
                HorizontalScrollbar = true
            };

            _txtChatMessage = new TextBox
            {
                Location = new Point(15, 645),
                Size = new Size(160, 25),
                BackColor = Color.FromArgb(45, 45, 45),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            _txtChatMessage.KeyDown += TxtChatMessage_KeyDown;

            _btnSendChat = new Button
            {
                Location = new Point(180, 645),
                Size = new Size(65, 25),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(70, 70, 70),
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            _btnSendChat.FlatAppearance.BorderSize = 0;
            _btnSendChat.Click += BtnSendChat_Click;

            leftPanel.Controls.AddRange(new Control[] {
                _lblProfileHeader, _txtUsername, _lblConfigHeader, _btnDesignBoard, _btnStats,
                _lblSpeedHeader, _numInterval, _lblBoardsHeader, _numBoards, _lblChipsHeader, _cmbChipType,
                _lblLanguageHeader, _cmbLanguage, _btnConnectLan, _lblGameMode, _lblLanStatus, _lblPlayersHeader, _lstPlayers,
                _lblChatHeader, _lstChat, _txtChatMessage, _btnSendChat
            });

            // 2. RIGHT SIDEBAR (Active Card Preview & Called History)
            Panel rightPanel = new Panel
            {
                Dock = DockStyle.Right,
                Width = 280,
                BackColor = Color.FromArgb(30, 30, 30),
                Padding = new Padding(15)
            };

            _lblPreviewHeader = new Label { Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = Color.Gray, Location = new Point(15, 15), Size = new Size(250, 15) };
            
            _currentCardPreview = new CardControl
            {
                Location = new Point(65, 35),
                Size = new Size(150, 210),
                CardData = new Card() // Initial empty card
            };

            _lblRiddle = new Label
            {
                Text = "\"Presiona Iniciar para comenzar el juego\"",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Italic),
                ForeColor = Color.FromArgb(200, 200, 200),
                Location = new Point(15, 255),
                Size = new Size(250, 50),
                TextAlign = ContentAlignment.TopCenter
            };

            _lblRemainingCards = new Label
            {
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                ForeColor = Color.Gray,
                Location = new Point(15, 310),
                Size = new Size(250, 18),
                TextAlign = ContentAlignment.MiddleCenter
            };

            _pbTimer = new ProgressBar
            {
                Location = new Point(15, 335),
                Size = new Size(250, 8),
                Maximum = _engine.IntervalSeconds,
                Value = 0,
                Style = ProgressBarStyle.Continuous
            };

            _lblHistoryHeader = new Label { Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = Color.Gray, Location = new Point(15, 360), Size = new Size(250, 15) };
            _historyPanel = new FlowLayoutPanel
            {
                Location = new Point(15, 380),
                Size = new Size(250, 310),
                BackColor = Color.FromArgb(40, 40, 40),
                AutoScroll = true,
                Padding = new Padding(5)
            };

            rightPanel.Controls.AddRange(new Control[] {
                _lblPreviewHeader, _currentCardPreview, _lblRiddle, _lblRemainingCards, _pbTimer, _lblHistoryHeader, _historyPanel
            });

            // 3. CENTER PANEL (Board Grid & Controls)
            Panel centerPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20)
            };

            // Top Game Toolbar (Start, Pause, Stop)
            FlowLayoutPanel toolbar = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 50,
                Padding = new Padding(0, 5, 0, 5)
            };

            _btnStart = new Button
            {
                Size = new Size(90, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(76, 175, 80),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnStart.FlatAppearance.BorderSize = 0;
            _btnStart.Click += BtnStart_Click;

            _btnPause = new Button
            {
                Size = new Size(90, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(255, 152, 0),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnPause.FlatAppearance.BorderSize = 0;
            _btnPause.Click += BtnPause_Click;

            _btnStop = new Button
            {
                Size = new Size(100, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(244, 67, 54),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnStop.FlatAppearance.BorderSize = 0;
            _btnStop.Click += BtnStop_Click;

            toolbar.Controls.AddRange(new Control[] { _btnStart, _btnPause, _btnStop });

            // Container for Multiple Boards
            _boardsContainer = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(25, 25, 25),
                Padding = new Padding(5)
            };

            // Bottom Win Button
            _btnClaimLoteria = new Button
            {
                Dock = DockStyle.Bottom,
                Height = 50,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(76, 175, 80),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 16f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnClaimLoteria.FlatAppearance.BorderSize = 0;
            _btnClaimLoteria.Click += BtnClaimLoteria_Click;

            centerPanel.Controls.Add(_boardsContainer);
            centerPanel.Controls.Add(toolbar);
            centerPanel.Controls.Add(_btnClaimLoteria);
            _boardsContainer.SendToBack();

            // Add all sidebars to main Form
            this.Controls.Add(centerPanel);
            this.Controls.Add(leftPanel);
            this.Controls.Add(rightPanel);
        }

        private void SetupEventHandlers()
        {
            // Core engine events
            _engine.CardDrawn += (card, remaining) => SafeInvoke(() => OnEngineCardDrawn(card, remaining));
            _engine.TimerTick += (seconds) => SafeInvoke(() => OnEngineTimerTick(seconds));
            _engine.StateChanged += (state) => SafeInvoke(() => OnEngineStateChanged(state));
            _engine.DeckExhausted += () => SafeInvoke(() => OnEngineDeckExhausted());

            // Network events
            _network.ConnectedToHost += () => SafeInvoke(() => OnNetworkConnected());
            _network.Disconnected += () => SafeInvoke(() => OnNetworkDisconnected());
            _network.PlayerJoined += (player) => SafeInvoke(() => OnNetworkPlayerJoined(player));
            _network.PlayerListUpdated += () => SafeInvoke(() => OnNetworkPlayerListUpdated());
            _network.CardReceived += (card) => SafeInvoke(() => OnNetworkCardReceived(card));
            _network.GameStateReceived += (state) => SafeInvoke(() => OnNetworkGameStateReceived(state));
            _network.LoteriaResultReceived += (winner, details) => SafeInvoke(() => OnNetworkLoteriaResult(winner, details));
            _network.ChatReceived += (sender, text) => SafeInvoke(() => OnNetworkChatReceived(sender, text));
            _network.ErrorOccurred += (msg) => SafeInvoke(() => OnNetworkError(msg));
        }

        // Thread safe invoker helper
        private void SafeInvoke(Action action)
        {
            if (this.IsDisposed) return;
            if (this.InvokeRequired)
            {
                this.BeginInvoke(action);
            }
            else
            {
                action();
            }
        }

        private void UpdateLocalization()
        {
            this.Text = TranslationManager.Get("Text_Title");
            _lblProfileHeader.Text = TranslationManager.Get("Profile_Player");
            _lblConfigHeader.Text = TranslationManager.Get("Config");
            _btnDesignBoard.Text = TranslationManager.Get("Btn_Design");
            _btnStats.Text = TranslationManager.Get("Btn_Stats");
            _lblSpeedHeader.Text = TranslationManager.Get("Speed_Interval");
            _lblBoardsHeader.Text = TranslationManager.Get("Boards_Count");
            _lblChipsHeader.Text = TranslationManager.Get("Chips_Type");
            _lblLanguageHeader.Text = TranslationManager.Get("Lang_Select");
            _btnConnectLan.Text = _network.IsConnected ? TranslationManager.Get("Btn_DisconnectLan") : TranslationManager.Get("Btn_ConnectLan");
            _lblPlayersHeader.Text = TranslationManager.Get("Players_Connected");
            _lblChatHeader.Text = TranslationManager.Get("Chat_Network");
            _lblPreviewHeader.Text = TranslationManager.Get("Current_Card");
            _lblHistoryHeader.Text = TranslationManager.Get("History_Cards");
            _btnSendChat.Text = TranslationManager.Get("Chat_Send");

            // Game Mode Labels
            if (_currentGameMode == "LAN")
            {
                _lblGameMode.Text = _network.IsHost ? TranslationManager.Get("Mode_LanHost") : TranslationManager.Get("Mode_LanClient");
            }
            else
            {
                _lblGameMode.Text = TranslationManager.Get("Mode_Solo");
            }

            if (!_network.IsConnected)
            {
                _lblLanStatus.Text = TranslationManager.Get("Status_Disconnected");
            }

            _btnStart.Text = TranslationManager.Get("Btn_Start");
            _btnPause.Text = _engine.State == GameState.Paused ? TranslationManager.Get("Btn_Resume") : TranslationManager.Get("Btn_Pause");
            _btnStop.Text = TranslationManager.Get("Btn_Stop");
            _btnClaimLoteria.Text = TranslationManager.Get("Btn_Loteria");

            // Update remaining cards
            if (_engine.CurrentCard != null)
            {
                _lblRemainingCards.Text = string.Format(TranslationManager.Get("Remaining_Cards"), _engine.CalledCards.Count);
            }
            else
            {
                _lblRemainingCards.Text = string.Format(TranslationManager.Get("Remaining_Cards"), 54);
            }

            // Update username in connected players list if LAN
            if (!_network.IsConnected)
            {
                _lstPlayers.Items.Clear();
                _lstPlayers.Items.Add((TranslationManager.CurrentLanguage == "EN" ? "Local: " : "Local: ") + _username);
            }

            // Update board title labels
            for (int i = 0; i < _boardTitleLabels.Count; i++)
            {
                if (i < _activeBoards.Count)
                {
                    string boardWord = TranslationManager.CurrentLanguage == "EN" ? "Board" : "Tabla";
                    _boardTitleLabels[i].Text = $"{boardWord} {i + 1} ({_activeBoards[i].Name})";
                }
            }

            // Invalidate board card controls to redraw names in the selected language
            foreach (var ctrlList in _boardsCardControls)
            {
                foreach (var ctrl in ctrlList)
                {
                    ctrl.Invalidate();
                }
            }
            _currentCardPreview.Invalidate();
        }

        // --- LOCAL BOARDS LOADING ---

        private void RecreateBoards(int count)
        {
            _activeBoards.Clear();
            _activeBoards.Add(_currentBoard);

            for (int i = 1; i < count; i++)
            {
                _activeBoards.Add(Board.GenerateRandom($"Tabla Auxiliar {i + 1}"));
            }

            LoadBoards(_activeBoards);
        }

        private void LoadBoard(Board board)
        {
            _currentBoard = board;
            RecreateBoards((int)_numBoards.Value);
        }

        private void LoadBoards(List<Board> boards)
        {
            _boardsContainer.Controls.Clear();
            _boardsContainer.RowStyles.Clear();
            _boardsContainer.ColumnStyles.Clear();
            _boardsCardControls.Clear();
            _boardTitleLabels.Clear();

            int count = boards.Count;
            if (count <= 1)
            {
                _boardsContainer.ColumnCount = 1;
                _boardsContainer.RowCount = 1;
                _boardsContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
                _boardsContainer.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            }
            else if (count == 2)
            {
                _boardsContainer.ColumnCount = 2;
                _boardsContainer.RowCount = 1;
                _boardsContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
                _boardsContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
                _boardsContainer.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            }
            else // 3 or 4 boards
            {
                _boardsContainer.ColumnCount = 2;
                _boardsContainer.RowCount = 2;
                _boardsContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
                _boardsContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
                _boardsContainer.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
                _boardsContainer.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
            }

            for (int b = 0; b < count; b++)
            {
                int boardIndex = b; // Closure
                Board board = boards[b];

                // Create a container panel for this board to hold the Title and the 5x5 grid
                Panel boardPanel = new Panel
                {
                    Dock = DockStyle.Fill,
                    Padding = new Padding(5),
                    BackColor = Color.FromArgb(20, 20, 20)
                };

                string boardTitleText = $"{(TranslationManager.CurrentLanguage == "EN" ? "Board" : "Tabla")} {boardIndex + 1} ({board.Name})";
                Label lblBoardTitle = new Label
                {
                    Text = boardTitleText,
                    Dock = DockStyle.Top,
                    Height = 25,
                    Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(76, 175, 80),
                    TextAlign = ContentAlignment.MiddleCenter
                };
                _boardTitleLabels.Add(lblBoardTitle);

                TableLayoutPanel grid = new TableLayoutPanel
                {
                    ColumnCount = 5,
                    RowCount = 5,
                    Dock = DockStyle.Fill,
                    BackColor = Color.FromArgb(25, 25, 25),
                    Padding = new Padding(3)
                };

                for (int i = 0; i < 5; i++)
                {
                    grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20f));
                    grid.RowStyles.Add(new RowStyle(SizeType.Percent, 20f));
                }

                var cardControls = new List<CardControl>();
                for (int i = 0; i < 25; i++)
                {
                    int index = i; // Closure
                    var cardCtrl = new CardControl
                    {
                        CardData = board.Cards[index],
                        Dock = DockStyle.Fill,
                        Cursor = Cursors.Hand
                    };

                    cardCtrl.Click += (s, e) => ClickBoardCell(boardIndex, index);
                    cardControls.Add(cardCtrl);

                    int row = index / 5;
                    int col = index % 5;
                    grid.Controls.Add(cardCtrl, col, row);
                }

                _boardsCardControls.Add(cardControls);
                boardPanel.Controls.Add(grid);
                boardPanel.Controls.Add(lblBoardTitle); // Add title at the top, grid fills the rest
                
                // Dock order: Title first (Dock.Top), then Grid (Dock.Fill)
                lblBoardTitle.BringToFront();
                grid.SendToBack();

                // Add to container grid
                int gridRow = boardIndex / 2;
                int gridCol = boardIndex % 2;
                if (count <= 2)
                {
                    gridRow = 0;
                    gridCol = boardIndex;
                }
                _boardsContainer.Controls.Add(boardPanel, gridCol, gridRow);
            }
        }

        private void ClickBoardCell(int boardIndex, int index)
        {
            // Only allow marking if game is playing
            if (_engine.State != GameState.Playing && !_network.IsConnected)
            {
                MessageBox.Show(TranslationManager.Get("Msg_ActiveGameMark"), TranslationManager.Get("Editor_ConfirmTitle"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Toggle cell marker locally and update controls
            var cardControl = _boardsCardControls[boardIndex][index];
            cardControl.IsMarked = !cardControl.IsMarked;
            _engine.SetCellMarked(boardIndex, index, cardControl.IsMarked);

            if (_currentGameMode == "LAN")
            {
                _network.ClientNotifyCellMarked(boardIndex, index, cardControl.IsMarked);
            }

            // Play a brief confirmation tick sound
            System.Media.SystemSounds.Asterisk.Play();
        }

        // --- BUTTON ACTIONS ---

        private void BtnStart_Click(object? sender, EventArgs e)
        {
            if (_currentGameMode == "LAN")
            {
                if (_network.IsHost)
                {
                    _engine.StartGame(_activeBoards, 0);
                    _network.HostBroadcastGameState(GameState.Playing);
                    _network.SendChatMessage(TranslationManager.Get("Host_StartChat"));
                }
            }
            else
            {
                _engine.StartGame(_activeBoards, 0);
            }
        }

        private void BtnPause_Click(object? sender, EventArgs e)
        {
            if (_engine.State == GameState.Playing)
            {
                _engine.PauseGame();
                if (_currentGameMode == "LAN" && _network.IsHost)
                {
                    _network.HostBroadcastGameState(GameState.Paused);
                    _network.SendChatMessage(TranslationManager.Get("Host_PauseChat"));
                }
            }
            else if (_engine.State == GameState.Paused)
            {
                _engine.ResumeGame();
                if (_currentGameMode == "LAN" && _network.IsHost)
                {
                    _network.HostBroadcastGameState(GameState.Playing);
                    _network.SendChatMessage(TranslationManager.Get("Host_ResumeChat"));
                }
            }
        }

        private void BtnStop_Click(object? sender, EventArgs e)
        {
            _engine.StopGame();
            if (_currentGameMode == "LAN" && _network.IsHost)
            {
                _network.HostBroadcastGameState(GameState.Finished);
                _network.SendChatMessage(TranslationManager.Get("Host_StopChat"));
            }
            ResetCurrentPreview();
        }

        private void ResetCurrentPreview()
        {
            _currentCardPreview.CardData = new Card();
            _lblRiddle.Text = TranslationManager.CurrentLanguage == "EN" ? "\"The game has ended\"" : "\"El juego ha terminado\"";
            _pbTimer.Value = 0;
            _historyPanel.Controls.Clear();
            foreach (var ctrlList in _boardsCardControls)
            {
                foreach (var ctrl in ctrlList)
                {
                    ctrl.IsMarked = false;
                    ctrl.IsHighlighted = false;
                }
            }
        }

        private void BtnDesignBoard_Click(object? sender, EventArgs e)
        {
            if (_engine.State == GameState.Playing || _engine.State == GameState.Paused)
            {
                MessageBox.Show(TranslationManager.Get("Msg_DesignActiveGame"), TranslationManager.Get("Editor_ConfirmTitle"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using var editor = new BoardEditorForm(_repository, _currentBoard);
            if (editor.ShowDialog() == DialogResult.OK)
            {
                LoadBoard(editor.EditedBoard);
                _engine.StartGame(_activeBoards, 0); // Load boards to engine
                _engine.StopGame(); // Keep in lobby/ready state
            }
        }

        private void BtnStats_Click(object? sender, EventArgs e)
        {
            using var statsFrm = new StatsForm(_repository, _username);
            statsFrm.ShowDialog();
        }

        private void BtnConnectLan_Click(object? sender, EventArgs e)
        {
            if (_network.IsConnected)
            {
                // Disconnect
                var res = MessageBox.Show(
                    TranslationManager.CurrentLanguage == "EN" ? "Do you want to disconnect from the LAN network?" : "¿Deseas desconectarte de la red LAN?", 
                    TranslationManager.CurrentLanguage == "EN" ? "Disconnect" : "Desconectar", 
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question
                );
                if (res == DialogResult.Yes)
                {
                    _network.Stop();
                    _engine.StopGame();
                    
                    _currentGameMode = "Solitario";
                    _lblGameMode.ForeColor = Color.FromArgb(76, 175, 80);
                    _lstPlayers.Items.Clear();
                    _lstPlayers.Items.Add((TranslationManager.CurrentLanguage == "EN" ? "Local: " : "Local: ") + _username);
                    UpdateLocalization();
                    UpdateControlStates();
                }
                return;
            }

            using var conn = new LanConnectForm();
            if (conn.ShowDialog() == DialogResult.OK)
            {
                _username = conn.PlayerName;
                _txtUsername.Text = _username;

                if (conn.IsHost)
                {
                    _currentGameMode = "LAN";
                    _lblGameMode.ForeColor = Color.FromArgb(33, 150, 243);
                    _lblLanStatus.Text = $"Host IP: {conn.IpAddress}:{conn.Port}";
                    
                    _network.StartHost(conn.Port, _username, _activeBoards);
                }
                else
                {
                    _currentGameMode = "LAN";
                    _lblGameMode.ForeColor = Color.FromArgb(255, 152, 0);
                    _lblLanStatus.Text = $"Conectando a {conn.IpAddress}:{conn.Port}...";
                    
                    _network.ConnectToHost(conn.IpAddress, conn.Port, _username, _activeBoards);
                }
                UpdateLocalization();
                UpdateControlStates();
            }
        }

        private void BtnClaimLoteria_Click(object? sender, EventArgs e)
        {
            // Perform local validation using CheckLocalWinWithCalledMulti
            var check = _engine.CheckLocalWinWithCalledMulti();

            if (check.HasWon)
            {
                if (_currentGameMode == "LAN")
                {
                    _network.ClientDeclareLoteria();
                }
                else
                {
                    _engine.StopGame();
                    HighlightWinningLine(check.BoardIndex, check.WinningIndices);
                    _sounds.CallCard(TranslationManager.CurrentLanguage == "EN" ? "Winner!" : "¡Ganaste!", TranslationManager.CurrentLanguage == "EN" ? "You completed the board." : "Has completado la tabla.");
                    
                    string boardText = _numBoards.Value > 1 
                        ? string.Format(TranslationManager.Get("Win_Message_Multi"), $"{(TranslationManager.CurrentLanguage == "EN" ? "Board" : "Tabla")} {check.BoardIndex + 1}", check.Description) 
                        : string.Format(TranslationManager.Get("Win_Message"), check.Description);
                    
                    MessageBox.Show(boardText, TranslationManager.CurrentLanguage == "EN" ? "Victory" : "Victoria", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    
                    // Save history
                    _repository.RecordMatch(_username, "Solitario", true, _username, check.Description, new List<string>());
                }
            }
            else if (check.HasMarkedButNotCalled)
            {
                // Find which specific cards in the marked-but-not-called line have not been called
                var uncalledCards = check.MarkedButNotCalledIndices
                    .Select(idx => _engine.LocalBoards[check.MarkedButNotCalledBoardIndex].Cards[idx])
                    .Where(card => !_engine.CalledCards.Any(c => c.Id == card.Id))
                    .Select(card => TranslationManager.GetCardName(card))
                    .Distinct()
                    .ToList();

                string uncalledCardsText = string.Join(", ", uncalledCards);
                string boardNameText = $"{(TranslationManager.CurrentLanguage == "EN" ? "Board" : "Tabla")} {check.MarkedButNotCalledBoardIndex + 1}";
                
                string errorMsg = string.Format(TranslationManager.Get("Msg_UncalledClaimFalse"), check.MarkedButNotCalledDescription, uncalledCardsText, boardNameText);
                MessageBox.Show(errorMsg, TranslationManager.CurrentLanguage == "EN" ? "Verification" : "Verificación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else
            {
                // Doesn't even have a marked line of 5
                MessageBox.Show(TranslationManager.Get("Msg_AccidentalClaimFalse"), TranslationManager.Get("Editor_ConfirmTitle"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void HighlightWinningLine(int boardIndex, List<int> indices)
        {
            if (boardIndex >= 0 && boardIndex < _boardsCardControls.Count)
            {
                foreach (var idx in indices)
                {
                    if (idx >= 0 && idx < _boardsCardControls[boardIndex].Count)
                    {
                        _boardsCardControls[boardIndex][idx].IsHighlighted = true;
                    }
                }
            }
        }

        private void ClearBoardHighlights()
        {
            foreach (var ctrlList in _boardsCardControls)
            {
                foreach (var ctrl in ctrlList)
                {
                    ctrl.IsHighlighted = false;
                }
            }
        }

        // --- GAME ENGINE CALLBACKS ---

        private void OnEngineCardDrawn(Card card, int remaining)
        {
            _currentCardPreview.CardData = card;
            _lblRiddle.Text = string.Empty;
            _lblRemainingCards.Text = string.Format(TranslationManager.Get("Remaining_Cards"), remaining);
            _pbTimer.Value = 0;

            // Add to history sidebar list
            var miniCard = new CardControl
            {
                CardData = card,
                Size = new Size(40, 60),
                Margin = new Padding(3),
                Enabled = false // visual only
            };
            _historyPanel.Controls.Add(miniCard);
            _historyPanel.ScrollControlIntoView(miniCard);

            // Announce voice/bell (only speak card name, omit riddle)
            _sounds.CallCard(TranslationManager.GetCardName(card));

            // Host sync draw
            if (_currentGameMode == "LAN" && _network.IsHost)
            {
                _network.HostBroadcastCardDrawn(card);
            }
        }

        private void OnEngineTimerTick(int seconds)
        {
            if (seconds <= _pbTimer.Maximum)
            {
                _pbTimer.Value = seconds;
            }
        }

        private void OnEngineStateChanged(GameState state)
        {
            UpdateControlStates();
            UpdateLocalization();
        }

        private void OnEngineDeckExhausted()
        {
            _pbTimer.Value = 0;
            MessageBox.Show(TranslationManager.Get("Msg_DeckExhausted"), TranslationManager.Get("Editor_ConfirmTitle"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            
            if (_currentGameMode == "LAN" && _network.IsHost)
            {
                _network.SendChatMessage(TranslationManager.Get("Host_ExhaustedChat"));
            }
        }

        // --- NETWORK CALLBACKS ---

        private void OnNetworkConnected()
        {
            _lblLanStatus.Text = TranslationManager.Get("Status_Connected");
            _lblLanStatus.ForeColor = Color.LightGreen;
            
            string joinMsg = TranslationManager.CurrentLanguage == "EN" 
                ? $"👋 {_username} has joined the game." 
                : $"👋 {_username} se ha unido al juego.";
            _network.SendChatMessage(joinMsg);
            
            UpdateLocalization();
            UpdateControlStates();
        }

        private void OnNetworkDisconnected()
        {
            _lblLanStatus.Text = TranslationManager.Get("Status_Disconnected");
            _lblLanStatus.ForeColor = Color.LightPink;
            
            string discMsg = TranslationManager.CurrentLanguage == "EN" 
                ? "Connection lost with the LAN server." 
                : "Se ha perdido la conexión con el servidor LAN.";
            MessageBox.Show(discMsg, TranslationManager.CurrentLanguage == "EN" ? "Connection Lost" : "Conexión Perdida", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            
            _currentGameMode = "Solitario";
            _engine.StopGame();
            
            UpdateLocalization();
            UpdateControlStates();
        }

        private void OnNetworkPlayerJoined(LobbyPlayer player)
        {
            string joinAlert = TranslationManager.CurrentLanguage == "EN" 
                ? $"* {player.Name} joined the room." 
                : $"* {player.Name} entró a la sala.";
            _lstChat.Items.Add(joinAlert);
            _sounds.PlayBell();
        }

        private void OnNetworkPlayerListUpdated()
        {
            _lstPlayers.Items.Clear();
            foreach (var p in _network.ConnectedPlayers)
            {
                _lstPlayers.Items.Add(p.Name);
            }
        }

        private void OnNetworkCardReceived(Card card)
        {
            // Sync client draw
            _currentCardPreview.CardData = card;
            _lblRiddle.Text = string.Empty;
            _lblRemainingCards.Text = string.Format(TranslationManager.Get("Remaining_Cards"), _engine.CalledCards.Count + 1);
            _pbTimer.Value = 0;

            // Add to client local called list
            _engine.CalledCards.Add(card);

            var miniCard = new CardControl
            {
                CardData = card,
                Size = new Size(40, 60),
                Margin = new Padding(3),
                Enabled = false
            };
            _historyPanel.Controls.Add(miniCard);
            _historyPanel.ScrollControlIntoView(miniCard);

            // Announce voice/bell (only speak card name, omit riddle)
            _sounds.CallCard(TranslationManager.GetCardName(card));
        }

        private void OnNetworkGameStateReceived(GameState state)
        {
            if (state == GameState.Playing)
            {
                if (_engine.State == GameState.Paused)
                {
                    _engine.ResumeGame();
                }
                else
                {
                    // Initialize client-side engine with active boards
                    _engine.InitializeClientGame(_activeBoards);
                    
                    // Reset client markings/highlights
                    ClearBoardHighlights();
                    foreach (var ctrlList in _boardsCardControls)
                    {
                        foreach (var ctrl in ctrlList)
                        {
                            ctrl.IsMarked = false;
                        }
                    }
                    _engine.CalledCards.Clear();
                    _historyPanel.Controls.Clear();
                }
            }
            else if (state == GameState.Paused)
            {
                _engine.PauseGame();
            }
            else if (state == GameState.Finished)
            {
                _engine.StopGame();
            }

            UpdateControlStates();
            UpdateLocalization();
        }

        private void OnNetworkLoteriaResult(string winner, string details)
        {
            _engine.StopGame();
            
            string speakText = TranslationManager.CurrentLanguage == "EN" 
                ? $"Congratulations to {winner}" 
                : $"Felicidades a {winner}";
            _sounds.CallCard(TranslationManager.CurrentLanguage == "EN" ? "Lottery!" : "¡Lotería!", speakText);
            
            string alertMsg = string.Format(TranslationManager.Get("Win_Message_Announce"), winner, details);
            MessageBox.Show(alertMsg, TranslationManager.CurrentLanguage == "EN" ? "Game Over" : "Fin de la Partida", MessageBoxButtons.OK, MessageBoxIcon.Information);
            
            // Build list of opponents
            List<string> opponents = _network.ConnectedPlayers
                .Select(p => p.Name)
                .Where(n => n != _username)
                .ToList();

            bool localWon = winner.Equals(_username, StringComparison.OrdinalIgnoreCase);
            
            // Save history
            _repository.RecordMatch(_username, "LAN", localWon, winner, details, opponents);
            UpdateControlStates();
            UpdateLocalization();
        }

        private void OnNetworkChatReceived(string sender, string text)
        {
            _lstChat.Items.Add($"{sender}: {text}");
            _lstChat.SelectedIndex = _lstChat.Items.Count - 1; // Scroll to bottom
        }

        private void OnNetworkError(string message)
        {
            MessageBox.Show(message, TranslationManager.CurrentLanguage == "EN" ? "Network Error" : "Error de Red", MessageBoxButtons.OK, MessageBoxIcon.Error);
            _lblLanStatus.Text = "Error";
            _lblLanStatus.ForeColor = Color.Red;
            UpdateControlStates();
        }

        private void TxtChatMessage_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                SendChatFromInput();
                e.SuppressKeyPress = true; // Stop beep
            }
        }

        private void BtnSendChat_Click(object? sender, EventArgs e)
        {
            SendChatFromInput();
        }

        private void SendChatFromInput()
        {
            string msg = _txtChatMessage.Text.Trim();
            if (!string.IsNullOrEmpty(msg))
            {
                _network.SendChatMessage(msg);
                _txtChatMessage.Clear();
            }
        }

        // --- UI STATE MANAGEMENT ---

        private void UpdateControlStates()
        {
            bool isPlaying = _engine.State == GameState.Playing;
            bool isPaused = _engine.State == GameState.Paused;
            bool isLAN = _currentGameMode == "LAN";
            bool isHost = _network.IsHost;

            // Textbox username only editable before game/connection
            _txtUsername.ReadOnly = isPlaying || isPaused || _network.IsConnected;

            // Speed, boards count, and language selectors only editable when not actively playing
            _numInterval.Enabled = !isPlaying && !isPaused && (!isLAN || isHost);
            _numBoards.Enabled = !isPlaying && !isPaused && !_network.IsConnected;
            _cmbLanguage.Enabled = !isPlaying && !isPaused;
            _cmbChipType.Enabled = !isPlaying && !isPaused;

            // LAN Connection only togglable when not in active game
            _btnConnectLan.Enabled = !isPlaying && !isPaused;

            // Dashboard buttons
            if (isLAN)
            {
                _btnStart.Enabled = isHost && !isPlaying && !isPaused;
                _btnPause.Enabled = isHost && (isPlaying || isPaused);
                _btnStop.Enabled = isHost && (isPlaying || isPaused);
            }
            else
            {
                _btnStart.Enabled = !isPlaying && !isPaused;
                _btnPause.Enabled = isPlaying || isPaused;
                _btnStop.Enabled = isPlaying || isPaused;
            }

            // Board editor only open when idle and not connected to LAN
            _btnDesignBoard.Enabled = !isPlaying && !isPaused && !_network.IsConnected;
            
            // Claim Loteria button active when game is running
            if (isLAN)
            {
                _btnClaimLoteria.Enabled = _network.IsConnected;
            }
            else
            {
                _btnClaimLoteria.Enabled = isPlaying;
            }
        }
    }
}
