using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
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

        private Label _lblWinPatternHeader = null!;
        private ComboBox _cmbWinPattern = null!;
        private Button _btnDeletePattern = null!;
        private CheckBox _chkDoublesMode = null!;
        private List<Button> _boardEditButtons = new List<Button>();
        private List<Button> _boardLoadButtons = new List<Button>();

        private List<GamePattern> _customPatterns = new List<GamePattern>();
        private List<GamePattern> _comboBoxPatterns = new List<GamePattern>();

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
            if (!_chkDoublesMode.Checked)
            {
                _currentBoard.RemoveDuplicates();
            }
            RecreateBoards(1);

            UpdateLocalization();
            UpdateControlStates();

            // Trigger background download of card images
            ImageCache.StartDownloadBackground();
        }

        private void InitializeComponent()
        {
            this.Size = new Size(1280, 880);
            this.MinimumSize = new Size(1280, 880);
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
            _numInterval.ValueChanged += (s, e) =>
            {
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
                Maximum = 100,
                Value = 1,
                BorderStyle = BorderStyle.FixedSingle
            };
            _numBoards.ValueChanged += (s, e) =>
            {
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
            _cmbChipType.SelectedIndexChanged += (s, e) =>
            {
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
            _cmbLanguage.SelectedIndexChanged += (s, e) =>
            {
                TranslationManager.CurrentLanguage = _cmbLanguage.SelectedIndex == 1 ? "EN" : "ES";
                UpdateLocalization();
                _sounds.UpdateVoice();
            };

            _lblWinPatternHeader = new Label { Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), ForeColor = Color.Gray, Location = new Point(15, 335), Size = new Size(230, 15) };
            _cmbWinPattern = new ComboBox
            {
                Location = new Point(15, 350),
                Size = new Size(180, 25),
                BackColor = Color.FromArgb(45, 45, 45),
                ForeColor = Color.White,
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat
            };
            _btnDeletePattern = new Button
            {
                Location = new Point(200, 350),
                Size = new Size(45, 25),
                BackColor = Color.FromArgb(180, 40, 40),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Text = "❌",
                Font = new Font("Segoe UI", 9f),
                Cursor = Cursors.Hand,
                Visible = false
            };
            _btnDeletePattern.FlatAppearance.BorderSize = 0;
            _btnDeletePattern.Click += (s, e) =>
            {
                int idx = _cmbWinPattern.SelectedIndex;
                if (idx >= 0 && idx < _comboBoxPatterns.Count)
                {
                    var pattern = _comboBoxPatterns[idx];
                    if (pattern.IsCustom)
                    {
                        string confirmTitle = TranslationManager.CurrentLanguage == "EN" ? "Delete Pattern" : "Eliminar Patrón";
                        string confirmMsg = TranslationManager.CurrentLanguage == "EN" 
                            ? $"Are you sure you want to delete the pattern '{pattern.Name}'?" 
                            : $"¿Estás seguro de que deseas eliminar el patrón '{pattern.Name}'?";
                        
                        if (MessageBox.Show(confirmMsg, confirmTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                        {
                            _repository.DeleteCustomPattern(pattern.Name);
                            var defaultPat = GamePattern.FromEnum(WinPattern.Linea5);
                            _engine.ActiveWinPattern = defaultPat;
                            _network.ActiveWinPattern = defaultPat;
                            UpdateWinPatternComboboxItems();
                        }
                    }
                }
            };

            _cmbWinPattern.SelectedIndexChanged += (s, e) =>
            {
                int idx = _cmbWinPattern.SelectedIndex;
                if (idx < 0) return;

                if (idx == _cmbWinPattern.Items.Count - 1)
                {
                    using (var editor = new PatternEditorForm(_repository))
                    {
                        if (editor.ShowDialog() == DialogResult.OK && editor.CreatedPattern != null)
                        {
                            _engine.ActiveWinPattern = editor.CreatedPattern;
                            _network.ActiveWinPattern = editor.CreatedPattern;
                            UpdateWinPatternComboboxItems();
                        }
                        else
                        {
                            UpdateWinPatternComboboxItems();
                        }
                    }
                }
                else if (idx >= 0 && idx < _comboBoxPatterns.Count)
                {
                    var selectedPattern = _comboBoxPatterns[idx];
                    _engine.ActiveWinPattern = selectedPattern;
                    _network.ActiveWinPattern = selectedPattern;
                    _btnDeletePattern.Visible = selectedPattern.IsCustom && (!_network.IsConnected || _network.IsHost);
                }
            };

            _chkDoublesMode = new CheckBox
            {
                Location = new Point(15, 385),
                Size = new Size(230, 20),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };
            _chkDoublesMode.CheckedChanged += (s, e) =>
            {
                _network.IsDoublesMode = _chkDoublesMode.Checked;
                if (_chkDoublesMode.Checked)
                {
                    bool modifiedAny = false;
                    foreach (var board in _activeBoards)
                    {
                        if (!board.HasDuplicates())
                        {
                            board.IntroduceDuplicate();
                            modifiedAny = true;
                        }
                    }

                    if (modifiedAny)
                    {
                        LoadBoards(_activeBoards);
                        UpdateLocalization();
                        _engine.StartGame(_activeBoards, 0);
                        _engine.StopGame();
                    }
                }
                else
                {
                    // Clean duplicates from all active boards when Doubles Mode is disabled
                    bool modifiedAny = false;
                    foreach (var board in _activeBoards)
                    {
                        if (board.HasDuplicates())
                        {
                            board.RemoveDuplicates();
                            modifiedAny = true;
                        }
                    }

                    if (modifiedAny)
                    {
                        LoadBoards(_activeBoards);
                        UpdateLocalization();
                        _engine.StartGame(_activeBoards, 0);
                        _engine.StopGame();
                    }
                }
            };

            _btnConnectLan = new Button
            {
                Location = new Point(15, 415),
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
                Location = new Point(15, 460),
                Size = new Size(230, 20)
            };

            _lblLanStatus = new Label
            {
                Font = new Font("Segoe UI", 8.5f, FontStyle.Italic),
                ForeColor = Color.Gray,
                Location = new Point(15, 480),
                Size = new Size(230, 18)
            };

            // Lobby Player List
            _lblPlayersHeader = new Label { Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = Color.Gray, Location = new Point(15, 505), Size = new Size(230, 15) };
            _lstPlayers = new ListBox
            {
                Location = new Point(15, 525),
                Size = new Size(230, 65),
                BackColor = Color.FromArgb(40, 40, 40),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.None
            };

            // Lobby Chat Room
            _lblChatHeader = new Label { Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = Color.Gray, Location = new Point(15, 600), Size = new Size(230, 15) };
            _lstChat = new ListBox
            {
                Location = new Point(15, 620),
                Size = new Size(230, 100),
                BackColor = Color.FromArgb(40, 40, 40),
                ForeColor = Color.FromArgb(200, 200, 200),
                BorderStyle = BorderStyle.None,
                HorizontalScrollbar = true
            };

            _txtChatMessage = new TextBox
            {
                Location = new Point(15, 730),
                Size = new Size(160, 25),
                BackColor = Color.FromArgb(45, 45, 45),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            _txtChatMessage.KeyDown += TxtChatMessage_KeyDown;

            _btnSendChat = new Button
            {
                Location = new Point(180, 730),
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
                _lblLanguageHeader, _cmbLanguage, _lblWinPatternHeader, _cmbWinPattern, _btnDeletePattern, _chkDoublesMode, _btnConnectLan,
                _lblGameMode, _lblLanStatus, _lblPlayersHeader, _lstPlayers,
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
                Location = new Point(80, 35),
                Size = new Size(120, 210),
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
                Size = new Size(250, 370),
                BackColor = Color.FromArgb(40, 40, 40),
                AutoScroll = true,
                Padding = new Padding(5)
            };

            rightPanel.Controls.AddRange(new Control[] {
                _lblPreviewHeader, _currentCardPreview, _lblRiddle, _lblRemainingCards, _pbTimer, _lblHistoryHeader, _historyPanel
            });

            // 3. CENTER PANEL (Board Grid & Controls)
            TableLayoutPanel centerPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(15),
                RowCount = 3,
                ColumnCount = 1
            };
            centerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            centerPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 50f)); // Row 0: toolbar
            centerPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));  // Row 1: board container
            centerPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 50f)); // Row 2: claim button

            // Top Game Toolbar (Start, Pause, Stop)
            FlowLayoutPanel toolbar = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
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
                BackColor = Color.FromArgb(25, 25, 25), // Pausar/Reanudar matches theme
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
                Dock = DockStyle.None,
                Anchor = AnchorStyles.None,
                BackColor = Color.FromArgb(25, 25, 25),
                Padding = new Padding(5)
            };

            // Bottom Win Button
            _btnClaimLoteria = new Button
            {
                Dock = DockStyle.Fill,
                Height = 50,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(76, 175, 80),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 16f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnClaimLoteria.FlatAppearance.BorderSize = 0;
            _btnClaimLoteria.Click += BtnClaimLoteria_Click;

            // Add controls to specific cells of the TableLayoutPanel to prevent any overlap
            centerPanel.Controls.Add(toolbar, 0, 0);
            centerPanel.Controls.Add(_boardsContainer, 0, 1);
            centerPanel.Controls.Add(_btnClaimLoteria, 0, 2);

            centerPanel.SizeChanged += (s, e) => ResizeActiveBoards();

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

            _lblWinPatternHeader.Text = TranslationManager.Get("Config_WinPattern");
            _chkDoublesMode.Text = TranslationManager.Get("Config_DoublesMode");
            UpdateWinPatternComboboxItems();

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

            _boardEditButtons.Clear();
            _boardLoadButtons.Clear();

            int count = boards.Count;
            Control? parent = _boardsContainer.Parent;
            int maxW = parent != null ? parent.Width - 30 : 1280 - 540;
            int maxH = parent != null ? parent.Height - 130 : 880 - 130;

            GetOptimalGridDimensions(count, maxW, maxH, out int cols, out int rows);

            _boardsContainer.ColumnCount = cols;
            _boardsContainer.RowCount = rows;

            float colPercent = 100f / cols;
            for (int i = 0; i < cols; i++)
            {
                _boardsContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, colPercent));
            }

            float rowPercent = 100f / rows;
            for (int i = 0; i < rows; i++)
            {
                _boardsContainer.RowStyles.Add(new RowStyle(SizeType.Percent, rowPercent));
            }

            for (int b = 0; b < count; b++)
            {
                int boardIndex = b; // Closure
                Board board = boards[b];

                // Create a container TableLayoutPanel for this board to hold the Title and the 5x5 grid without overlap
                TableLayoutPanel boardPanel = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    Padding = new Padding(2),
                    BackColor = Color.FromArgb(20, 20, 20),
                    ColumnCount = 1,
                    RowCount = 2
                };
                boardPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
                boardPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 32f)); // Row 0: Header
                boardPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));  // Row 1: Grid

                Panel headerPanel = new Panel
                {
                    Dock = DockStyle.Fill,
                    Height = 32,
                    BackColor = Color.FromArgb(28, 28, 28)
                };

                string boardTitleText = $"{(TranslationManager.CurrentLanguage == "EN" ? "Board" : "Tabla")} {boardIndex + 1} ({board.Name})";
                Label lblBoardTitle = new Label
                {
                    Text = boardTitleText,
                    Dock = DockStyle.Fill,
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(76, 175, 80),
                    TextAlign = ContentAlignment.MiddleLeft,
                    Padding = new Padding(5, 0, 0, 0)
                };
                _boardTitleLabels.Add(lblBoardTitle);

                Button btnEdit = new Button
                {
                    Text = TranslationManager.CurrentLanguage == "EN" ? "Edit" : "Editar",
                    Dock = DockStyle.Right,
                    Width = 60,
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(60, 60, 60),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 8f),
                    Cursor = Cursors.Hand
                };
                btnEdit.FlatAppearance.BorderSize = 0;
                btnEdit.Click += (s, e) => EditSpecificBoard(boardIndex);
                _boardEditButtons.Add(btnEdit);

                Button btnLoad = new Button
                {
                    Text = TranslationManager.CurrentLanguage == "EN" ? "Load" : "Cargar",
                    Dock = DockStyle.Right,
                    Width = 60,
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(76, 175, 80),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 8f),
                    Cursor = Cursors.Hand
                };
                btnLoad.FlatAppearance.BorderSize = 0;
                btnLoad.Click += (s, e) => LoadSpecificBoard(boardIndex);
                _boardLoadButtons.Add(btnLoad);

                headerPanel.Controls.Add(lblBoardTitle);
                headerPanel.Controls.Add(btnLoad);
                headerPanel.Controls.Add(btnEdit);

                TableLayoutPanel grid = new TableLayoutPanel
                {
                    ColumnCount = 5,
                    RowCount = 5,
                    BackColor = Color.FromArgb(25, 25, 25),
                    Padding = new Padding(2) // Reduced padding to bring cards closer
                };

                for (int i = 0; i < 5; i++)
                {
                    grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20f));
                    grid.RowStyles.Add(new RowStyle(SizeType.Percent, 20f));
                }

                // Let the grid fill the entire boardPanel to make full use of the screen area and resize dynamically
                grid.Dock = DockStyle.Fill;

                var cardControls = new List<CardControl>();
                for (int i = 0; i < 25; i++)
                {
                    int index = i; // Closure
                    var cardCtrl = new CardControl
                    {
                        CardData = board.Cards[index],
                        Dock = DockStyle.Fill,
                        Margin = new Padding(1), // Explicitly set tiny margins to bring cards closer together and prevent clipping
                        Cursor = Cursors.Hand
                    };

                    cardCtrl.Click += (s, e) => ClickBoardCell(boardIndex, index);
                    cardControls.Add(cardCtrl);

                    int row = index / 5;
                    int col = index % 5;
                    grid.Controls.Add(cardCtrl, col, row);
                }

                _boardsCardControls.Add(cardControls);
                boardPanel.Controls.Add(headerPanel, 0, 0);
                boardPanel.Controls.Add(grid, 0, 1);

                // Add to container grid
                int gridRow = boardIndex / cols;
                int gridCol = boardIndex % cols;
                _boardsContainer.Controls.Add(boardPanel, gridCol, gridRow);
            }

            ResizeActiveBoards();
        }

        private void ResizeActiveBoards()
        {
            if (_boardsContainer == null || _boardsContainer.Controls.Count == 0) return;

            int count = _boardsContainer.Controls.Count;
            Control? parent = _boardsContainer.Parent;
            if (parent == null) return;

            // Available space in centerPanel is parent size minus its padding (15 on each side = 30 horizontally)
            // and row heights (50 toolbar + 50 claim button + 30 padding = 130 vertically)
            int maxW = parent.Width - 30;
            int maxH = parent.Height - 130;

            if (maxW <= 20 || maxH <= 20) return;

            GetOptimalGridDimensions(count, maxW, maxH, out int cols, out int rows);

            // Update container row/column counts dynamically if size changes
            _boardsContainer.ColumnCount = cols;
            _boardsContainer.RowCount = rows;

            // Update column styles
            _boardsContainer.ColumnStyles.Clear();
            float colPercent = 100f / cols;
            for (int i = 0; i < cols; i++)
            {
                _boardsContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, colPercent));
            }

            // Update row styles
            _boardsContainer.RowStyles.Clear();
            float rowPercent = 100f / rows;
            for (int i = 0; i < rows; i++)
            {
                _boardsContainer.RowStyles.Add(new RowStyle(SizeType.Percent, rowPercent));
            }

            int usableW = maxW - 10; // subtract _boardsContainer padding (5 on each side)
            int usableH = maxH - 10;

            // Enforce aspect ratio of 0.58 for cards grid inside each boardPanel
            float targetRatio = 0.58f;
            int headerH = 32;

            // Solve:
            // 1. boardH <= usableH / rows
            // 2. (boardH - 32) * 0.58 + 4 <= usableW / cols
            int h_max1 = usableH / rows;
            int h_max2 = (int)((usableW / cols - 4) / targetRatio) + headerH;

            int boardH = Math.Min(h_max1, h_max2);
            int boardW = (int)((boardH - headerH) * targetRatio) + 4;

            // Ensure sensible minimums
            boardH = Math.Max(120, boardH);
            boardW = Math.Max(80, boardW);

            int totalW = cols * boardW + 10;
            int totalH = rows * boardH + 10;

            _boardsContainer.Size = new Size(totalW, totalH);
        }

        private void GetOptimalGridDimensions(int count, int maxW, int maxH, out int bestCols, out int bestRows)
        {
            if (count <= 4)
            {
                bestCols = count;
                bestRows = 1;
                return;
            }

            // Default fallback
            bestCols = (int)Math.Ceiling(Math.Sqrt(count));
            bestRows = (int)Math.Ceiling((double)count / bestCols);

            int usableW = maxW - 10;
            int usableH = maxH - 10;
            if (usableW <= 20 || usableH <= 20) return;

            float targetRatio = 0.58f;
            int headerH = 32;
            int maxBoardH = 0;

            // Try all possible column counts from 1 to count
            for (int c = 1; c <= count; c++)
            {
                int r = (int)Math.Ceiling((double)count / c);

                int h_max1 = usableH / r;
                int h_max2 = (int)((usableW / c - 4) / targetRatio) + headerH;

                int boardH = Math.Min(h_max1, h_max2);

                // We want to maximize the board height (and thus card size).
                // If heights are equal, we prefer the one with MORE columns to make the panel wider and span the screen width.
                if (boardH > maxBoardH)
                {
                    maxBoardH = boardH;
                    bestCols = c;
                    bestRows = r;
                }
                else if (boardH == maxBoardH)
                {
                    if (c > bestCols)
                    {
                        bestCols = c;
                        bestRows = r;
                    }
                }
            }
        }

        private void UpdateWinPatternComboboxItems()
        {
            _cmbWinPattern.Items.Clear();
            _comboBoxPatterns.Clear();

            _comboBoxPatterns.Add(GamePattern.FromEnum(WinPattern.Linea5));
            _comboBoxPatterns.Add(GamePattern.FromEnum(WinPattern.Full));
            _comboBoxPatterns.Add(GamePattern.FromEnum(WinPattern.Cruz));
            _comboBoxPatterns.Add(GamePattern.FromEnum(WinPattern.LetraL));
            _comboBoxPatterns.Add(GamePattern.FromEnum(WinPattern.Esquinas));

            _cmbWinPattern.Items.Add(TranslationManager.Get("Pattern_Linea5"));
            _cmbWinPattern.Items.Add(TranslationManager.Get("Pattern_Full"));
            _cmbWinPattern.Items.Add(TranslationManager.Get("Pattern_Cruz"));
            _cmbWinPattern.Items.Add(TranslationManager.Get("Pattern_LetraL"));
            _cmbWinPattern.Items.Add(TranslationManager.Get("Pattern_Esquinas"));

            _customPatterns = _repository.LoadCustomPatterns();
            foreach (var pat in _customPatterns)
            {
                _comboBoxPatterns.Add(pat);
                _cmbWinPattern.Items.Add(pat.Name);
            }

            string createText = TranslationManager.CurrentLanguage == "EN" ? "+ Create Custom Pattern..." : "+ Crear Patrón Personalizado...";
            _cmbWinPattern.Items.Add(createText);

            int selectedIdx = 0;
            if (_engine.ActiveWinPattern != null)
            {
                for (int i = 0; i < _comboBoxPatterns.Count; i++)
                {
                    if (_comboBoxPatterns[i].Id == _engine.ActiveWinPattern.Id)
                    {
                        selectedIdx = i;
                        break;
                    }
                }
            }
            _cmbWinPattern.SelectedIndex = selectedIdx;

            if (selectedIdx >= 0 && selectedIdx < _comboBoxPatterns.Count)
            {
                _btnDeletePattern.Visible = _comboBoxPatterns[selectedIdx].IsCustom && (!_network.IsConnected || _network.IsHost);
            }
            else
            {
                _btnDeletePattern.Visible = false;
            }
        }

        private void EditSpecificBoard(int boardIndex)
        {
            if (_engine.State == GameState.Playing || _engine.State == GameState.Paused)
            {
                MessageBox.Show(TranslationManager.Get("Msg_DesignActiveGame"), TranslationManager.Get("Editor_ConfirmTitle"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (boardIndex < 0 || boardIndex >= _activeBoards.Count) return;

            using var editor = new BoardEditorForm(_repository, _activeBoards[boardIndex]);
            if (editor.ShowDialog() == DialogResult.OK)
            {
                if (!_chkDoublesMode.Checked)
                {
                    editor.EditedBoard.RemoveDuplicates();
                }
                _activeBoards[boardIndex] = editor.EditedBoard;

                if (boardIndex == 0)
                {
                    _currentBoard = editor.EditedBoard;
                }

                LoadBoards(_activeBoards);
                UpdateLocalization();

                _engine.StartGame(_activeBoards, 0);
                _engine.StopGame();
            }
        }

        private void LoadSpecificBoard(int boardIndex)
        {
            if (_engine.State == GameState.Playing || _engine.State == GameState.Paused)
            {
                return;
            }

            if (boardIndex < 0 || boardIndex >= _activeBoards.Count) return;

            using var loader = new LoadBoardForm(_repository);
            if (loader.ShowDialog() == DialogResult.OK && loader.SelectedBoard != null)
            {
                // Check if this board is already loaded in another slot
                for (int i = 0; i < _activeBoards.Count; i++)
                {
                    if (i != boardIndex && _activeBoards[i].Id == loader.SelectedBoard.Id)
                    {
                        MessageBox.Show(
                            TranslationManager.CurrentLanguage == "EN"
                                ? "This board is already loaded in another slot. You cannot load the same board twice."
                                : "Esta tabla ya está cargada en otra ranura. No puedes cargar la misma tabla dos veces.",
                            TranslationManager.CurrentLanguage == "EN" ? "Duplicate Board" : "Tabla Duplicada",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning
                        );
                        return;
                    }
                }

                if (!_chkDoublesMode.Checked)
                {
                    loader.SelectedBoard.RemoveDuplicates();
                }

                _activeBoards[boardIndex] = loader.SelectedBoard;

                if (boardIndex == 0)
                {
                    _currentBoard = loader.SelectedBoard;
                }

                LoadBoards(_activeBoards);
                UpdateLocalization();

                _engine.StartGame(_activeBoards, 0);
                _engine.StopGame();
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
            int dupCount = _chkDoublesMode.Checked ? 2 : 0;
            if (!_chkDoublesMode.Checked)
            {
                bool modifiedAny = false;
                foreach (var board in _activeBoards)
                {
                    if (board.HasDuplicates())
                    {
                        board.RemoveDuplicates();
                        modifiedAny = true;
                    }
                }
                if (modifiedAny)
                {
                    LoadBoards(_activeBoards);
                }
            }

            if (_currentGameMode == "LAN")
            {
                if (_network.IsHost)
                {
                    _engine.StartGame(_activeBoards, dupCount);
                    _network.HostBroadcastGameState(GameState.Playing, _chkDoublesMode.Checked, _network.ActiveWinPattern.IsCustom ? "CUSTOM" : _network.ActiveWinPattern.Name);
                    _network.SendChatMessage(TranslationManager.Get("Host_StartChat"));
                }
            }
            else
            {
                _engine.StartGame(_activeBoards, dupCount);
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
                if (!_chkDoublesMode.Checked)
                {
                    editor.EditedBoard.RemoveDuplicates();
                }
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
                Size = new Size(35, 60),
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
                Size = new Size(35, 60),
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
                    // Sync settings to engine
                    _engine.ActiveWinPattern = _network.ActiveWinPattern;

                    // Initialize client-side engine with active boards
                    _engine.InitializeClientGame(_activeBoards);

                    // Clean duplicates client-side if Host has disabled Doubles Mode
                    if (!_network.IsDoublesMode)
                    {
                        bool modifiedAny = false;
                        foreach (var board in _activeBoards)
                        {
                            if (board.HasDuplicates())
                            {
                                board.RemoveDuplicates();
                                modifiedAny = true;
                            }
                        }
                        if (modifiedAny)
                        {
                            MessageBox.Show(
                                TranslationManager.CurrentLanguage == "EN"
                                    ? "Your boards contained duplicate cards, but since the Host has disabled Doubles Mode, they have been automatically removed to ensure fair play."
                                    : "Tus tablas contenían cartas dobles, pero como el Anfitrión ha desactivado el Modo Dobles, se han eliminado automáticamente para garantizar un juego justo.",
                                TranslationManager.Get("Editor_ConfirmTitle"),
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information
                            );
                            LoadBoards(_activeBoards);
                        }
                    }

                    // Sync settings to client UI controls so client can see them
                    _chkDoublesMode.Checked = _network.IsDoublesMode;
                    int selectedIdx = -1;
                    for (int i = 0; i < _comboBoxPatterns.Count; i++)
                    {
                        if (_comboBoxPatterns[i].Id == _network.ActiveWinPattern.Id)
                        {
                            selectedIdx = i;
                            break;
                        }
                    }
                    if (selectedIdx == -1)
                    {
                        int insertPos = _comboBoxPatterns.Count;
                        _comboBoxPatterns.Insert(insertPos, _network.ActiveWinPattern);
                        _cmbWinPattern.Items.Insert(insertPos, _network.ActiveWinPattern.Name);
                        selectedIdx = insertPos;
                    }
                    _cmbWinPattern.SelectedIndex = selectedIdx;

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
            string firewallAdvice = TranslationManager.CurrentLanguage == "EN"
                ? "\n\nTip: Ensure both players are on the same Wi-Fi network and check Windows Firewall settings (allow the app on Private networks)."
                : "\n\nConsejo: Asegúrate de que ambos jugadores estén en la misma red Wi-Fi y revisa la configuración del Firewall de Windows (permite el acceso de la aplicación en redes Privadas).";

            MessageBox.Show(message + firewallAdvice, TranslationManager.CurrentLanguage == "EN" ? "Network Error" : "Error de Red", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            _cmbWinPattern.Enabled = !isPlaying && !isPaused && (!isLAN || isHost);
            bool isCustomSelected = false;
            int patIdx = _cmbWinPattern.SelectedIndex;
            if (patIdx >= 0 && patIdx < _comboBoxPatterns.Count)
            {
                isCustomSelected = _comboBoxPatterns[patIdx].IsCustom;
            }
            _btnDeletePattern.Visible = isCustomSelected && (!isLAN || isHost);
            _btnDeletePattern.Enabled = !isPlaying && !isPaused && (!isLAN || isHost);
            _chkDoublesMode.Enabled = !isPlaying && !isPaused && (!isLAN || isHost);

            // Per-board manage buttons only enabled when idle and offline
            bool canManageBoards = !isPlaying && !isPaused && !_network.IsConnected;
            foreach (var btn in _boardEditButtons) btn.Enabled = canManageBoards;
            foreach (var btn in _boardLoadButtons) btn.Enabled = canManageBoards;

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
