using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using LoteriaMexicanaApp.Core;
using LoteriaMexicanaApp.Data;

namespace LoteriaMexicanaApp.UI
{
    public class PatternEditorForm : Form
    {
        private readonly DataRepository _repository;
        private readonly List<int> _selectedIndices = new List<int>();

        private TextBox _txtPatternName = null!;
        private TableLayoutPanel _gridPanel = null!;
        private Button _btnSave = null!;
        private Button _btnCancel = null!;

        public GamePattern CreatedPattern { get; private set; } = null!;

        public PatternEditorForm(DataRepository repository)
        {
            _repository = repository;
            InitializeComponent();
            InitializeGrid();
        }

        private void InitializeComponent()
        {
            this.Text = TranslationManager.CurrentLanguage == "EN" ? "Win Pattern Designer" : "Diseñador de Patrón de Victoria";
            this.Size = new Size(400, 500);
            this.MinimumSize = new Size(400, 500);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(24, 24, 24);
            this.ForeColor = Color.FromArgb(230, 230, 230);
            this.Font = new Font("Segoe UI", 9.5f);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // Name Label
            Label lblName = new Label
            {
                Text = TranslationManager.CurrentLanguage == "EN" ? "Pattern Name:" : "Nombre del Patrón:",
                Location = new Point(20, 20),
                Size = new Size(360, 20),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };

            // Name Input TextBox
            _txtPatternName = new TextBox
            {
                Location = new Point(20, 45),
                Size = new Size(345, 25),
                BackColor = Color.FromArgb(45, 45, 45),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10f),
                Text = TranslationManager.CurrentLanguage == "EN" ? "My Custom Pattern" : "Mi Patrón Personalizado"
            };

            // Instructions Label
            Label lblInstruction = new Label
            {
                Text = TranslationManager.CurrentLanguage == "EN" ? "Click cells to toggle them in the winning combination:" : "Haz clic en las celdas para incluirlas en la combinación ganadora:",
                Location = new Point(20, 85),
                Size = new Size(360, 35),
                ForeColor = Color.FromArgb(180, 180, 180),
                Font = new Font("Segoe UI", 9f, FontStyle.Italic)
            };

            // 5x5 Grid TableLayoutPanel
            _gridPanel = new TableLayoutPanel
            {
                ColumnCount = 5,
                RowCount = 5,
                Location = new Point(50, 130),
                Size = new Size(280, 280),
                BackColor = Color.FromArgb(30, 30, 30),
                Padding = new Padding(5)
            };

            for (int i = 0; i < 5; i++)
            {
                _gridPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20f));
                _gridPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 20f));
            }

            // Save Button
            _btnSave = new Button
            {
                Text = TranslationManager.CurrentLanguage == "EN" ? "Save" : "Guardar",
                Location = new Point(80, 420),
                Size = new Size(110, 32),
                BackColor = Color.FromArgb(76, 175, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnSave.FlatAppearance.BorderSize = 0;
            _btnSave.Click += BtnSave_Click;

            // Cancel Button
            _btnCancel = new Button
            {
                Text = TranslationManager.CurrentLanguage == "EN" ? "Cancel" : "Cancelar",
                Location = new Point(210, 420),
                Size = new Size(110, 32),
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnCancel.FlatAppearance.BorderSize = 0;
            _btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

            this.Controls.AddRange(new Control[] { lblName, _txtPatternName, lblInstruction, _gridPanel, _btnSave, _btnCancel });
        }

        private void InitializeGrid()
        {
            _gridPanel.Controls.Clear();
            for (int i = 0; i < 25; i++)
            {
                int index = i; // Closure
                Button btn = new Button
                {
                    Dock = DockStyle.Fill,
                    Margin = new Padding(2),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(45, 45, 45),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                    Text = (index + 1).ToString(),
                    Cursor = Cursors.Hand
                };
                btn.FlatAppearance.BorderSize = 0;

                btn.Click += (s, e) =>
                {
                    if (_selectedIndices.Contains(index))
                    {
                        _selectedIndices.Remove(index);
                        btn.BackColor = Color.FromArgb(45, 45, 45);
                    }
                    else
                    {
                        _selectedIndices.Add(index);
                        btn.BackColor = Color.FromArgb(76, 175, 80);
                    }
                };

                int row = index / 5;
                int col = index % 5;
                _gridPanel.Controls.Add(btn, col, row);
            }
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            string name = _txtPatternName.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                string msg = TranslationManager.CurrentLanguage == "EN" ? "Please enter a pattern name." : "Por favor ingresa un nombre para el patrón.";
                MessageBox.Show(msg, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (_selectedIndices.Count == 0)
            {
                string msg = TranslationManager.CurrentLanguage == "EN" ? "Please select at least one cell for the pattern." : "Por favor selecciona al menos una celda para el patrón.";
                MessageBox.Show(msg, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Create pattern
            var pattern = new GamePattern
            {
                Name = name,
                IsCustom = true
            };
            pattern.Combinations.Add(new List<int>(_selectedIndices));

            try
            {
                _repository.SaveCustomPattern(pattern);
                CreatedPattern = pattern;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                string msg = TranslationManager.CurrentLanguage == "EN" ? "Failed to save pattern: " : "Error al guardar el patrón: ";
                MessageBox.Show(msg + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
