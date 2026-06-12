using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using LoteriaMexicanaApp.Core;
using LoteriaMexicanaApp.Data;

namespace LoteriaMexicanaApp.UI
{
    public class BoardEditorForm : Form
    {
        private readonly DataRepository _repository;
        private Board _editingBoard;
        private int _selectedCellIndex = -1;

        private TextBox _txtBoardName = null!;
        private TableLayoutPanel _gridPanel = null!;
        private FlowLayoutPanel _drawerPanel = null!;
        private Button _btnSave = null!;
        private Button _btnAutofill = null!;
        private Button _btnClear = null!;
        private Button _btnCancel = null!;
        private Label _lblInstruction = null!;

        private readonly List<CardControl> _gridControls = new List<CardControl>();

        public Board EditedBoard => _editingBoard;

        public BoardEditorForm(DataRepository repository, Board? boardToEdit = null)
        {
            _repository = repository;
            _editingBoard = boardToEdit != null 
                ? new Board(boardToEdit.Name, boardToEdit.Cards) // Edit clone
                : Board.GenerateRandom("Mi Tabla"); // Generate random default

            InitializeComponent();
            LoadBoardToGrid();
            LoadDrawer();
            SelectCell(0); // Select first cell by default
        }

        private void InitializeComponent()
        {
            this.Text = TranslationManager.Get("Editor_Title");
            this.Size = new Size(880, 640);
            this.MinimumSize = new Size(880, 640);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(24, 24, 24);
            this.ForeColor = Color.FromArgb(230, 230, 230);
            this.Font = new Font("Segoe UI", 9.5f);

            // Left Layout: Board name + Instructions + 5x5 Grid
            Panel leftPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20)
            };

            Label lblName = new Label { Text = TranslationManager.Get("Editor_Label_Name"), Location = new Point(20, 20), Size = new Size(130, 20), Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
            _txtBoardName = new TextBox
            {
                Location = new Point(160, 18),
                Size = new Size(200, 25),
                BackColor = Color.FromArgb(40, 40, 40),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Text = _editingBoard.Name
            };

            _lblInstruction = new Label
            {
                Text = TranslationManager.Get("Editor_Instruction"),
                ForeColor = Color.FromArgb(180, 180, 180),
                Font = new Font("Segoe UI", 9f, FontStyle.Italic),
                Location = new Point(20, 50),
                Size = new Size(400, 35)
            };

            // 5x5 Grid
            _gridPanel = new TableLayoutPanel
            {
                ColumnCount = 5,
                RowCount = 5,
                Location = new Point(20, 95),
                Size = new Size(425, 450),
                BackColor = Color.FromArgb(30, 30, 30),
                Padding = new Padding(5)
            };

            // Setup column and row ratios
            for (int i = 0; i < 5; i++)
            {
                _gridPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20f));
                _gridPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 20f));
            }

            // Right Layout: Sidebar Scrollable drawer for all 54 cards
            Panel rightPanel = new Panel
            {
                Dock = DockStyle.Right,
                Width = 380,
                BackColor = Color.FromArgb(35, 35, 35),
                Padding = new Padding(10)
            };

            Label lblCatalog = new Label
            {
                Text = TranslationManager.Get("Title_Catalog"),
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(76, 175, 80),
                Dock = DockStyle.Top,
                Height = 30,
                TextAlign = ContentAlignment.MiddleLeft
            };

            _drawerPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.FromArgb(40, 40, 40),
                Padding = new Padding(5)
            };

            rightPanel.Controls.Add(_drawerPanel);
            rightPanel.Controls.Add(lblCatalog);

            // Bottom Actions (inside Left Panel)
            _btnSave = new Button
            {
                Text = TranslationManager.Get("Btn_Save_Editor"),
                DialogResult = DialogResult.OK,
                Location = new Point(20, 555),
                Size = new Size(130, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(76, 175, 80),
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            _btnSave.FlatAppearance.BorderSize = 0;
            _btnSave.Click += BtnSave_Click;

            _btnAutofill = new Button
            {
                Text = TranslationManager.Get("Btn_Autofill_Editor"),
                Location = new Point(160, 555),
                Size = new Size(95, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(33, 150, 243),
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            _btnAutofill.FlatAppearance.BorderSize = 0;
            _btnAutofill.Click += BtnAutofill_Click;

            _btnClear = new Button
            {
                Text = TranslationManager.Get("Btn_Clear_Editor"),
                Location = new Point(265, 555),
                Size = new Size(80, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(120, 120, 120),
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            _btnClear.FlatAppearance.BorderSize = 0;
            _btnClear.Click += BtnClear_Click;

            _btnCancel = new Button
            {
                Text = TranslationManager.Get("Btn_Cancel_Editor"),
                DialogResult = DialogResult.Cancel,
                Location = new Point(365, 555),
                Size = new Size(80, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(50, 50, 50),
                ForeColor = Color.FromArgb(200, 200, 200),
                Cursor = Cursors.Hand
            };
            _btnCancel.FlatAppearance.BorderSize = 0;

            leftPanel.Controls.AddRange(new Control[] {
                lblName, _txtBoardName, _lblInstruction, _gridPanel,
                _btnSave, _btnAutofill, _btnClear, _btnCancel
            });

            this.Controls.Add(leftPanel);
            this.Controls.Add(rightPanel);
        }

        private void LoadBoardToGrid()
        {
            _gridPanel.Controls.Clear();
            _gridControls.Clear();

            for (int i = 0; i < 25; i++)
            {
                int index = i; // Closure
                var cardCtrl = new CardControl
                {
                    CardData = _editingBoard.Cards[index],
                    Dock = DockStyle.Fill,
                    Cursor = Cursors.Hand
                };
                
                cardCtrl.Click += (s, e) => SelectCell(index);
                _gridControls.Add(cardCtrl);
                
                int row = index / 5;
                int col = index % 5;
                _gridPanel.Controls.Add(cardCtrl, col, row);
            }
        }

        private void LoadDrawer()
        {
            _drawerPanel.Controls.Clear();
            foreach (var card in Deck.BaseCards)
            {
                var thumb = new CardControl
                {
                    CardData = card,
                    Size = new Size(65, 95),
                    Margin = new Padding(4),
                    Cursor = Cursors.Hand
                };

                thumb.Click += (s, e) => AssignCardToSelectedCell(card);
                _drawerPanel.Controls.Add(thumb);
            }
        }

        private void SelectCell(int index)
        {
            _selectedCellIndex = index;
            for (int i = 0; i < 25; i++)
            {
                _gridControls[i].IsHighlighted = (i == _selectedCellIndex);
            }
            _lblInstruction.Text = string.Format(TranslationManager.Get("Editor_SelectCell"), index / 5 + 1, index % 5 + 1);
        }

        private void AssignCardToSelectedCell(Card card)
        {
            if (_selectedCellIndex < 0 || _selectedCellIndex >= 25) return;

            // Check if card already exists on the board to avoid simple user errors
            bool cardExists = _editingBoard.Cards.Any(c => c.Id == card.Id);
            if (cardExists)
            {
                string msg = string.Format(TranslationManager.Get("Editor_ConfirmRepeat"), TranslationManager.GetCardName(card));
                string title = TranslationManager.Get("Editor_ConfirmTitle");
                var result = MessageBox.Show(msg, title, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.No)
                {
                    return;
                }
            }

            _editingBoard.Cards[_selectedCellIndex] = card;
            _gridControls[_selectedCellIndex].CardData = card;

            // Move selection to next cell automatically
            if (_selectedCellIndex < 24)
            {
                SelectCell(_selectedCellIndex + 1);
            }
        }

        private void BtnAutofill_Click(object? sender, EventArgs e)
        {
            var random = new Random();
            var shuffledBase = Deck.BaseCards.OrderBy(x => random.Next()).ToList();
            
            for (int i = 0; i < 25; i++)
            {
                _editingBoard.Cards[i] = shuffledBase[i];
                _gridControls[i].CardData = shuffledBase[i];
            }

            SelectCell(0);
        }

        private void BtnClear_Click(object? sender, EventArgs e)
        {
            for (int i = 0; i < 25; i++)
            {
                _editingBoard.Cards[i] = new Card(); // Empty card
                _gridControls[i].CardData = _editingBoard.Cards[i];
            }
            SelectCell(0);
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            string name = _txtBoardName.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show(TranslationManager.Get("Msg_EnterBoardName"), TranslationManager.Get("Editor_ConfirmTitle"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.DialogResult = DialogResult.None;
                return;
            }

            // Validate duplicate cards: max 2 duplicate cards (cartas dobles) on the board
            var cardCounts = _editingBoard.Cards
                .Where(c => c.Id > 0)
                .GroupBy(c => c.Id)
                .Select(g => new { CardId = g.Key, Count = g.Count() })
                .ToList();

            if (cardCounts.Any(c => c.Count > 2))
            {
                MessageBox.Show(TranslationManager.Get("Msg_NoTripleLimit"), TranslationManager.Get("Editor_ConfirmTitle"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.DialogResult = DialogResult.None;
                return;
            }

            int doubleCardsCount = cardCounts.Count(c => c.Count == 2);
            if (doubleCardsCount > 2)
            {
                string msg = string.Format(TranslationManager.Get("Msg_NoDuplicateLimit"), doubleCardsCount);
                MessageBox.Show(msg, TranslationManager.Get("Editor_ConfirmTitle"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.DialogResult = DialogResult.None;
                return;
            }

            // Validate if all cells are filled
            if (_editingBoard.Cards.Any(c => c.Id <= 0))
            {
                string msg = TranslationManager.Get("Editor_ConfirmSaveEmpty");
                string title = TranslationManager.Get("Editor_ConfirmSaveEmptyTitle");
                var result = MessageBox.Show(msg, title, MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (result == DialogResult.No)
                {
                    this.DialogResult = DialogResult.None;
                    return;
                }
            }

            _editingBoard.Name = name;
            _repository.SaveBoard(_editingBoard);
            MessageBox.Show(string.Format(TranslationManager.Get("Msg_SavedSuccess"), name), TranslationManager.Get("Editor_ConfirmTitle"), MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
