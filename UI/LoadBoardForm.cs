using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using LoteriaMexicanaApp.Core;
using LoteriaMexicanaApp.Data;

namespace LoteriaMexicanaApp.UI
{
    public class LoadBoardForm : Form
    {
        private readonly DataRepository _repository;
        private ListBox _lstBoards = null!;
        private Button _btnLoad = null!;
        private Button _btnDelete = null!;
        private Button _btnCancel = null!;
        private List<string> _boardFiles = new List<string>();

        public Board? SelectedBoard { get; private set; }

        public LoadBoardForm(DataRepository repository)
        {
            _repository = repository;
            InitializeComponent();
            RefreshList();
        }

        private void InitializeComponent()
        {
            this.Text = TranslationManager.CurrentLanguage == "EN" ? "Load Saved Board" : "Cargar Tabla Guardada";
            this.Size = new Size(350, 400);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(30, 30, 30);
            this.ForeColor = Color.FromArgb(230, 230, 230);
            this.Font = new Font("Segoe UI", 9.5f);

            Label lblTitle = new Label
            {
                Text = TranslationManager.CurrentLanguage == "EN" ? "Select a Board to Load" : "Selecciona una Tabla para Cargar",
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                Location = new Point(15, 15),
                Size = new Size(320, 20),
                ForeColor = Color.FromArgb(76, 175, 80)
            };

            _lstBoards = new ListBox
            {
                Location = new Point(15, 45),
                Size = new Size(300, 240),
                BackColor = Color.FromArgb(40, 40, 40),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            _lstBoards.SelectedIndexChanged += (s, e) => UpdateButtons();
            _lstBoards.DoubleClick += (s, e) => LoadSelected();

            _btnLoad = new Button
            {
                Text = TranslationManager.CurrentLanguage == "EN" ? "Load" : "Cargar",
                Location = new Point(215, 305),
                Size = new Size(100, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(76, 175, 80),
                ForeColor = Color.White,
                Cursor = Cursors.Hand,
                Enabled = false
            };
            _btnLoad.FlatAppearance.BorderSize = 0;
            _btnLoad.Click += (s, e) => LoadSelected();

            _btnDelete = new Button
            {
                Text = TranslationManager.CurrentLanguage == "EN" ? "Delete" : "Eliminar",
                Location = new Point(15, 305),
                Size = new Size(90, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(244, 67, 54),
                ForeColor = Color.White,
                Cursor = Cursors.Hand,
                Enabled = false
            };
            _btnDelete.FlatAppearance.BorderSize = 0;
            _btnDelete.Click += BtnDelete_Click;

            _btnCancel = new Button
            {
                Text = TranslationManager.CurrentLanguage == "EN" ? "Cancel" : "Cancelar",
                Location = new Point(115, 305),
                Size = new Size(90, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(55, 55, 55),
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            _btnCancel.FlatAppearance.BorderSize = 0;
            _btnCancel.DialogResult = DialogResult.Cancel;

            this.Controls.AddRange(new Control[] { lblTitle, _lstBoards, _btnLoad, _btnDelete, _btnCancel });
        }

        private void RefreshList()
        {
            _lstBoards.Items.Clear();
            _boardFiles = _repository.GetSavedBoardFiles();
            foreach (var file in _boardFiles)
            {
                _lstBoards.Items.Add(Path.GetFileNameWithoutExtension(file));
            }
            UpdateButtons();
        }

        private void UpdateButtons()
        {
            bool hasSelection = _lstBoards.SelectedIndex >= 0;
            _btnLoad.Enabled = hasSelection;
            _btnDelete.Enabled = hasSelection;
        }

        private void LoadSelected()
        {
            if (_lstBoards.SelectedIndex < 0) return;
            string selectedFile = _boardFiles[_lstBoards.SelectedIndex];
            var board = _repository.LoadBoard(selectedFile);
            if (board != null)
            {
                SelectedBoard = board;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                MessageBox.Show(
                    TranslationManager.CurrentLanguage == "EN" ? "Error loading board file." : "Error al cargar el archivo de la tabla.",
                    TranslationManager.CurrentLanguage == "EN" ? "Error" : "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error
                );
            }
        }

        private void BtnDelete_Click(object? sender, EventArgs e)
        {
            if (_lstBoards.SelectedIndex < 0) return;
            string selectedFile = _boardFiles[_lstBoards.SelectedIndex];
            string boardName = Path.GetFileNameWithoutExtension(selectedFile);

            var confirm = MessageBox.Show(
                TranslationManager.CurrentLanguage == "EN" ? $"Are you sure you want to delete '{boardName}'?" : $"¿Estás seguro de que deseas eliminar '{boardName}'?",
                TranslationManager.CurrentLanguage == "EN" ? "Confirm Delete" : "Confirmar Eliminación",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning
            );

            if (confirm == DialogResult.Yes)
            {
                try
                {
                    File.Delete(selectedFile);
                    RefreshList();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}
