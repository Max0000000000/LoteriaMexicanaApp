using System;
using System.Drawing;
using System.Windows.Forms;
using LoteriaMexicanaApp.Data;

namespace LoteriaMexicanaApp.UI
{
    public class StatsForm : Form
    {
        private readonly DataRepository _repository;
        private readonly string _username;
        private DataGridView _dgvHistory = null!;

        public StatsForm(DataRepository repository, string username)
        {
            _repository = repository;
            _username = username;
            InitializeComponent();
            LoadStatsData();
        }

        private void InitializeComponent()
        {
            this.Text = "Estadísticas e Historial de Partidas";
            this.Size = new Size(720, 520);
            this.MinimumSize = new Size(700, 480);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(28, 28, 28);
            this.ForeColor = Color.FromArgb(230, 230, 230);
            this.Font = new Font("Segoe UI", 9.5f);

            // Title
            Label lblTitle = new Label
            {
                Text = $"📊 Historial de {_username}",
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = Color.FromArgb(76, 175, 80),
                Location = new Point(20, 15),
                Size = new Size(400, 30),
                TextAlign = ContentAlignment.MiddleLeft
            };

            // Summary Stats Panel
            Panel summaryPanel = new Panel
            {
                Location = new Point(20, 55),
                Size = new Size(660, 80),
                BackColor = Color.FromArgb(38, 38, 38),
                Padding = new Padding(10)
            };

            // Stats labels
            Label lblGamesPlayed = new Label
            {
                Text = "Partidas Jugadas:\n-",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Location = new Point(15, 15),
                Size = new Size(180, 50),
                TextAlign = ContentAlignment.MiddleCenter,
                Name = "lblGamesPlayed"
            };

            Label lblSoloWins = new Label
            {
                Text = "Victorias Solitario:\n-",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(33, 150, 243),
                Location = new Point(240, 15),
                Size = new Size(180, 50),
                TextAlign = ContentAlignment.MiddleCenter,
                Name = "lblSoloWins"
            };

            Label lblLanWins = new Label
            {
                Text = "Victorias LAN:\n-",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 152, 0),
                Location = new Point(460, 15),
                Size = new Size(180, 50),
                TextAlign = ContentAlignment.MiddleCenter,
                Name = "lblLanWins"
            };

            summaryPanel.Controls.AddRange(new Control[] { lblGamesPlayed, lblSoloWins, lblLanWins });

            // History Label
            Label lblHistoryTitle = new Label
            {
                Text = "Historial Detallado",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                Location = new Point(20, 155),
                Size = new Size(200, 25)
            };

            // DataGridView for match history list
            _dgvHistory = new DataGridView
            {
                Location = new Point(20, 190),
                Size = new Size(660, 240),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                BackgroundColor = Color.FromArgb(38, 38, 38),
                BorderStyle = BorderStyle.None,
                ForeColor = Color.White,
                GridColor = Color.FromArgb(60, 60, 60),
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                EnableHeadersVisualStyles = false
            };

            // Header Style
            _dgvHistory.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(76, 175, 80);
            _dgvHistory.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            _dgvHistory.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            _dgvHistory.ColumnHeadersHeight = 30;

            // Row Style
            _dgvHistory.DefaultCellStyle.BackColor = Color.FromArgb(45, 45, 45);
            _dgvHistory.DefaultCellStyle.ForeColor = Color.White;
            _dgvHistory.DefaultCellStyle.SelectionBackColor = Color.FromArgb(60, 120, 60);
            _dgvHistory.DefaultCellStyle.SelectionForeColor = Color.White;

            // Define columns
            _dgvHistory.Columns.Add("Date", "Fecha");
            _dgvHistory.Columns.Add("Mode", "Modo");
            _dgvHistory.Columns.Add("Won", "Resultado");
            _dgvHistory.Columns.Add("Winner", "Ganador");
            _dgvHistory.Columns.Add("Line", "Línea");
            _dgvHistory.Columns.Add("Opponents", "Oponentes");

            // Format columns
            _dgvHistory.Columns[0].Width = 110;
            _dgvHistory.Columns[1].Width = 70;
            _dgvHistory.Columns[2].Width = 90;
            _dgvHistory.Columns[3].Width = 100;
            _dgvHistory.Columns[4].Width = 110;

            // OK Button
            Button btnOk = new Button
            {
                Text = "Cerrar",
                DialogResult = DialogResult.OK,
                Location = new Point(580, 440),
                Size = new Size(100, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(76, 175, 80),
                ForeColor = Color.White,
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right
            };
            btnOk.FlatAppearance.BorderSize = 0;

            this.Controls.AddRange(new Control[] { lblTitle, summaryPanel, lblHistoryTitle, _dgvHistory, btnOk });
        }

        private void LoadStatsData()
        {
            var stats = _repository.LoadStats(_username);
            
            // Set summary values
            var lblGamesPlayed = this.Controls.Find("lblGamesPlayed", true)[0] as Label;
            var lblSoloWins = this.Controls.Find("lblSoloWins", true)[0] as Label;
            var lblLanWins = this.Controls.Find("lblLanWins", true)[0] as Label;

            if (lblGamesPlayed != null) lblGamesPlayed.Text = $"Partidas Jugadas:\n{stats.GamesPlayed}";
            if (lblSoloWins != null) lblSoloWins.Text = $"Victorias Solitario:\n{stats.WinsLocal}";
            if (lblLanWins != null) lblLanWins.Text = $"Victorias LAN:\n{stats.WinsLan}";

            // Populate history table
            _dgvHistory.Rows.Clear();
            foreach (var record in stats.MatchHistory)
            {
                string wonText = record.LocalPlayerWon ? "🏆 Ganaste" : "❌ Perdiste";
                string opponentsList = string.Join(", ", record.Opponents);
                if (string.IsNullOrWhiteSpace(opponentsList)) opponentsList = "-";

                int rowIndex = _dgvHistory.Rows.Add(
                    record.Date.ToString("yyyy-MM-dd HH:mm"),
                    record.GameMode,
                    wonText,
                    record.WinnerName,
                    string.IsNullOrWhiteSpace(record.WinningLine) ? "-" : record.WinningLine,
                    opponentsList
                );

                // Set color for Result cell
                var cell = _dgvHistory.Rows[rowIndex].Cells[2];
                if (record.LocalPlayerWon)
                {
                    cell.Style.ForeColor = Color.LightGreen;
                    cell.Style.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                }
                else
                {
                    cell.Style.ForeColor = Color.LightPink;
                }
            }
        }
    }
}
