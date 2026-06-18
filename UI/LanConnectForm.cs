using System;
using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;

namespace LoteriaMexicanaApp.UI
{
    public class LanConnectForm : Form
    {
        private TextBox _txtName;
        private TextBox _txtIp;
        private TextBox _txtPort;
        private RadioButton _rbHost;
        private RadioButton _rbClient;
        private Button _btnConnect;
        private Button _btnCancel;
        private Label _lblIpInfo;

        public string PlayerName => _txtName.Text.Trim();
        public string IpAddress => _txtIp.Text.Trim();
        public int Port => int.TryParse(_txtPort.Text, out int port) ? port : 4040;
        public bool IsHost => _rbHost.Checked;

        public LanConnectForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Conexión de Red Local (LAN)";
            this.Size = new Size(380, 340);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(30, 30, 30);
            this.ForeColor = Color.FromArgb(230, 230, 230);
            this.Font = new Font("Segoe UI", 9.5f);

            // Title Header
            Label lblHeader = new Label
            {
                Text = "📡 Lotería LAN Multiplayer",
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = Color.FromArgb(76, 175, 80), // Accent green
                Location = new Point(20, 15),
                Size = new Size(340, 35),
                TextAlign = ContentAlignment.MiddleLeft
            };

            // Player Name Label & TextBox
            Label lblName = new Label { Text = "Nombre del Jugador:", Location = new Point(20, 60), Size = new Size(130, 20) };
            _txtName = new TextBox
            {
                Location = new Point(160, 58),
                Size = new Size(180, 25),
                BackColor = Color.FromArgb(45, 45, 45),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Text = "Jugador_" + new Random().Next(100, 999)
            };

            // Mode Label & RadioButtons
            Label lblMode = new Label { Text = "Modo de Red:", Location = new Point(20, 100), Size = new Size(130, 20) };

            _rbHost = new RadioButton
            {
                Text = "Crear Partida (Host)",
                Location = new Point(160, 98),
                Size = new Size(180, 22),
                Checked = true
            };
            _rbHost.CheckedChanged += NetworkMode_Changed;

            _rbClient = new RadioButton
            {
                Text = "Unirse a Partida (Cliente)",
                Location = new Point(160, 122),
                Size = new Size(180, 22)
            };

            // IP Address Label & TextBox
            Label lblIp = new Label { Text = "Dirección IP:", Location = new Point(20, 160), Size = new Size(130, 20) };
            _txtIp = new TextBox
            {
                Location = new Point(160, 158),
                Size = new Size(180, 25),
                BackColor = Color.FromArgb(45, 45, 45),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Text = GetLocalIPAddress(),
                ReadOnly = false
            };

            _lblIpInfo = new Label
            {
                Text = "Tus IPs: " + GetLocalIPAddressesString(),
                Font = new Font("Segoe UI", 8f, FontStyle.Italic),
                ForeColor = Color.Gray,
                Location = new Point(20, 185),
                Size = new Size(320, 20)
            };

            // Port Label & TextBox
            Label lblPort = new Label { Text = "Puerto:", Location = new Point(20, 210), Size = new Size(130, 20) };
            _txtPort = new TextBox
            {
                Location = new Point(160, 208),
                Size = new Size(180, 25),
                BackColor = Color.FromArgb(45, 45, 45),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Text = "4040"
            };

            // Buttons
            _btnConnect = new Button
            {
                Text = "Iniciar Partida",
                DialogResult = DialogResult.OK,
                Location = new Point(220, 250),
                Size = new Size(120, 35),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(76, 175, 80),
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            _btnConnect.FlatAppearance.BorderSize = 0;
            _btnConnect.Click += BtnConnect_Click;

            _btnCancel = new Button
            {
                Text = "Cancelar",
                DialogResult = DialogResult.Cancel,
                Location = new Point(110, 250),
                Size = new Size(100, 35),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(50, 50, 50),
                ForeColor = Color.FromArgb(200, 200, 200),
                Cursor = Cursors.Hand
            };
            _btnCancel.FlatAppearance.BorderSize = 0;

            // Add to form
            this.Controls.AddRange(new Control[] {
                lblHeader, lblName, _txtName, lblMode, _rbHost, _rbClient,
                lblIp, _txtIp, _lblIpInfo, lblPort, _txtPort, _btnConnect, _btnCancel
            });
        }

        private void NetworkMode_Changed(object? sender, EventArgs e)
        {
            if (_rbHost.Checked)
            {
                _txtIp.Text = GetLocalIPAddress();
                _txtIp.ReadOnly = false;
                _lblIpInfo.Text = "Tus IPs: " + GetLocalIPAddressesString();
                _btnConnect.Text = "Iniciar Servidor";
                _btnConnect.BackColor = Color.FromArgb(76, 175, 80);
            }
            else
            {
                _txtIp.ReadOnly = false;
                _txtIp.Text = "127.0.0.1";
                _lblIpInfo.Text = "Ingresa la IP del Host.";
                _btnConnect.Text = "Conectar";
                _btnConnect.BackColor = Color.FromArgb(33, 150, 243); // Accent blue
            }
        }

        private void BtnConnect_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(PlayerName))
            {
                MessageBox.Show("Por favor ingresa tu nombre de jugador.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.DialogResult = DialogResult.None; // Prevent close
                return;
            }

            if (string.IsNullOrWhiteSpace(IpAddress))
            {
                MessageBox.Show("Por favor ingresa una dirección IP válida.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.DialogResult = DialogResult.None;
                return;
            }

            if (!int.TryParse(_txtPort.Text, out int port) || port < 1024 || port > 65535)
            {
                MessageBox.Show("Por favor ingresa un puerto válido (1024 - 65535).", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.DialogResult = DialogResult.None;
                return;
            }
        }

        public static string GetLocalIPAddress()
        {
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                var ipList = new System.Collections.Generic.List<string>();
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        string ipStr = ip.ToString();
                        if (!ipStr.StartsWith("127."))
                        {
                            ipList.Add(ipStr);
                        }
                    }
                }

                // Prioritize standard local networks (192.168.1.x, 192.168.0.x, etc.) over virtual host-only ranges (like 192.168.56.x)
                var preferred = ipList.FirstOrDefault(ip => ip.StartsWith("192.168.1.") || ip.StartsWith("192.168.0."));
                if (preferred != null) return preferred;

                preferred = ipList.FirstOrDefault(ip => ip.StartsWith("192.168."));
                if (preferred != null && !preferred.StartsWith("192.168.56.")) return preferred;

                preferred = ipList.FirstOrDefault(ip => ip.StartsWith("10."));
                if (preferred != null) return preferred;

                // Fallback to first non-loopback IP
                if (ipList.Count > 0) return ipList[0];
            }
            catch
            {
                // Ignore
            }
            return "127.0.0.1";
        }

        public static string GetLocalIPAddressesString()
        {
            var ips = new System.Collections.Generic.List<string>();
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        if (!ip.ToString().StartsWith("127."))
                        {
                            ips.Add(ip.ToString());
                        }
                    }
                }
            }
            catch
            {
                // Ignore
            }
            return ips.Count > 0 ? string.Join(" / ", ips) : "127.0.0.1";
        }
    }
}
