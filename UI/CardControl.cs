using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using LoteriaMexicanaApp.Core;

namespace LoteriaMexicanaApp.UI
{
    public class CardControl : Control
    {
        private Card _card = new Card();
        private bool _isMarked;
        private bool _isHighlighted;

        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public Card CardData
        {
            get => _card;
            set
            {
                _card = value;
                Invalidate();
            }
        }

        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public bool IsMarked
        {
            get => _isMarked;
            set
            {
                _isMarked = value;
                Invalidate();
            }
        }

        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public bool IsHighlighted
        {
            get => _isHighlighted;
            set
            {
                _isHighlighted = value;
                Invalidate();
            }
        }

        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public bool FillBounds { get; set; } = false;

        public CardControl()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);

            Size = new Size(75, 130);

            // Subscribe to image downloaded event to refresh when card images arrive
            ImageCache.ImageDownloaded += HandleImageDownloaded;
        }

        private void HandleImageDownloaded()
        {
            if (this.IsDisposed) return;
            if (this.InvokeRequired)
            {
                try { this.BeginInvoke(new Action(Invalidate)); } catch { }
            }
            else
            {
                Invalidate();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ImageCache.ImageDownloaded -= HandleImageDownloaded;
            }
            base.Dispose(disposing);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.CompositingQuality = CompositingQuality.HighQuality;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // Clear background
            g.Clear(Parent?.BackColor ?? Color.White);

            int width = Width;
            int height = Height;

            Rectangle cardRect;
            int cornerRadius;

            // Enforce aspect ratio (0.58) and apply spacing factor (0.96) to prevent stretching and crowding
            float targetRatio = 0.58f;
            float spacingFactor = 0.96f;

            int availW = (int)((width - 8) * spacingFactor);
            int availH = (int)((height - 8) * spacingFactor);

            if (availW < 10 || availH < 10)
            {
                // Fallback for extremely small controls (like when window is minimized or collapsed)
                cardRect = new Rectangle(2, 2, Math.Max(2, width - 4), Math.Max(2, height - 4));
                cornerRadius = Math.Max(2, cardRect.Width / 10);
            }
            else
            {
                int cardW = availW;
                int cardH = (int)(availW / targetRatio);

                if (cardH > availH)
                {
                    cardH = availH;
                    cardW = (int)(availH * targetRatio);
                }

                int cardX = (width - cardW) / 2;
                int cardY = (height - cardH) / 2;
                cardRect = new Rectangle(cardX, cardY, cardW, cardH);
                cornerRadius = Math.Max(4, cardRect.Width / 10);
            }

            // Define card path
            using GraphicsPath path = GetRoundedRectPath(cardRect, cornerRadius);

            // Fill card background (fallback gradient)
            Color baseColor = ColorTranslator.FromHtml(string.IsNullOrWhiteSpace(_card.BackgroundColorCode) ? "#E3F2FD" : _card.BackgroundColorCode);
            using (LinearGradientBrush bgBrush = new LinearGradientBrush(
                cardRect,
                Color.FromArgb(255, 253, 253, 253),
                Color.FromArgb(255, Color.FromArgb(
                    (int)(baseColor.R * 0.9 + 25),
                    (int)(baseColor.G * 0.9 + 25),
                    (int)(baseColor.B * 0.9 + 25)
                )),
                45f))
            {
                g.FillPath(bgBrush, path);
            }

            // Draw shadow/border
            if (_isHighlighted)
            {
                // Gold highlight for winning lines
                using Pen goldPen = new Pen(Color.FromArgb(255, 215, 0), 4);
                goldPen.Alignment = PenAlignment.Center;
                g.DrawPath(goldPen, path);
            }
            else
            {
                using Pen borderPen = new Pen(Color.FromArgb(50, 0, 0, 0), 1.5f);
                borderPen.Alignment = PenAlignment.Inset;
                g.DrawPath(borderPen, path);
            }

            if (_card.Id > 0)
            {
                // Try to get download image
                Image? img = ImageCache.GetCardImage(_card.Id);
                if (img != null)
                {
                    // Draw official image inside card boundary with rounded corners clipping
                    GraphicsState state = g.Save();
                    g.SetClip(path);

                    // Draw image stretched to cover card
                    g.DrawImage(img, cardRect);
                    g.Restore(state);
                }
                else
                {
                    // Fallback to emoji drawing in color
                    // 1. Draw Index Number (scaled based on cardRect height)
                    using (Font indexFont = new Font("Segoe UI", cardRect.Height * 0.08f, FontStyle.Bold))
                    {
                        g.DrawString(_card.Id.ToString(), indexFont, Brushes.DimGray, cardRect.X + Math.Max(2, cardRect.Width * 0.06f), cardRect.Y + Math.Max(2, cardRect.Height * 0.04f));
                    }

                    // 2. Draw Color Emoji Symbol (Centered using TextRenderer)
                    string emoji = string.IsNullOrWhiteSpace(_card.Emoji) ? "🃏" : _card.Emoji;
                    using (Font emojiFont = new Font("Segoe UI Emoji", cardRect.Height * 0.32f))
                    {
                        Size emojiSize = TextRenderer.MeasureText(emoji, emojiFont);
                        int emojiX = cardRect.X + (cardRect.Width - emojiSize.Width) / 2;
                        int emojiY = cardRect.Y + (cardRect.Height - emojiSize.Height) / 2 - (int)(cardRect.Height * 0.04f);

                        // TextRenderer.DrawText triggers DirectWrite color fallback
                        TextRenderer.DrawText(g, emoji, emojiFont, new Point(emojiX, emojiY), Color.Black, TextFormatFlags.Default);
                    }

                    // 3. Draw Card Name (Bottom, scaled based on cardRect height)
                    using (Font nameFont = new Font("Segoe UI", cardRect.Height * 0.085f, FontStyle.Bold))
                    {
                        string name = TranslationManager.GetCardName(_card);
                        SizeF nameSize = g.MeasureString(name, nameFont);

                        RectangleF textBar = new RectangleF(
                            cardRect.X + 2,
                            cardRect.Bottom - nameSize.Height - 6,
                            cardRect.Width - 4,
                            nameSize.Height + 4
                        );

                        using (SolidBrush barBrush = new SolidBrush(Color.FromArgb(30, 0, 0, 0)))
                        {
                            g.FillRectangle(barBrush, textBar);
                        }

                        float nameX = cardRect.X + (cardRect.Width - nameSize.Width) / 2f;
                        float nameY = textBar.Y + (textBar.Height - nameSize.Height) / 2f;

                        g.DrawString(name, nameFont, Brushes.Black, nameX, nameY);
                    }
                }
            }
            else
            {
                // Empty card placeholder
                using (Font emptyFont = new Font("Segoe UI", cardRect.Height * 0.08f, FontStyle.Italic))
                {
                    string text = TranslationManager.CurrentLanguage == "EN" ? "Empty" : "Vacío";
                    SizeF size = g.MeasureString(text, emptyFont);
                    g.DrawString(text, emptyFont, Brushes.LightGray, cardRect.X + (cardRect.Width - size.Width) / 2, cardRect.Y + (cardRect.Height - size.Height) / 2);
                }
            }

            // 4. Draw Marker Chip if marked
            if (_isMarked)
            {
                DrawMarker(g, cardRect);
            }
        }

        private void DrawMarker(Graphics g, Rectangle rect)
        {
            string style = MainForm.SelectedChipStyle;
            if (style == "Frijolito")
            {
                DrawFrijolito(g, rect);
            }
            else
            {
                Color chipColor = Color.Red;
                if (style == "Ficha Roja") chipColor = Color.FromArgb(190, 220, 20, 20);
                else if (style == "Ficha Azul") chipColor = Color.FromArgb(190, 20, 100, 220);
                else if (style == "Ficha Verde") chipColor = Color.FromArgb(190, 20, 180, 70);
                else if (style == "Ficha Amarilla") chipColor = Color.FromArgb(190, 240, 200, 20);

                DrawPlasticChip(g, rect, chipColor);
            }
        }

        private void DrawFrijolito(Graphics g, Rectangle rect)
        {
            float beanWidth = rect.Width * 0.38f;
            float beanHeight = rect.Width * 0.26f;
            float beanX = rect.X + (rect.Width - beanWidth) / 2f;
            float beanY = rect.Y + (rect.Height - beanHeight) / 2f + 10f;

            GraphicsState state = g.Save();
            g.TranslateTransform(beanX + beanWidth / 2f, beanY + beanHeight / 2f);
            g.RotateTransform(20f);

            RectangleF beanRect = new RectangleF(-beanWidth / 2f, -beanHeight / 2f, beanWidth, beanHeight);

            // Shadow
            using (SolidBrush shadowBrush = new SolidBrush(Color.FromArgb(80, 0, 0, 0)))
            {
                g.FillEllipse(shadowBrush, beanRect.X + 3, beanRect.Y + 3, beanRect.Width, beanRect.Height);
            }

            // Bean body
            Color darkBrown = Color.FromArgb(93, 64, 37);
            Color lightBrown = Color.FromArgb(141, 110, 99);
            using (LinearGradientBrush beanBrush = new LinearGradientBrush(beanRect, lightBrown, darkBrown, 60f))
            {
                g.FillEllipse(beanBrush, beanRect);
            }

            using (Pen beanBorder = new Pen(Color.FromArgb(70, 30, 20), 1.5f))
            {
                g.DrawEllipse(beanBorder, beanRect);
            }

            // Glossy highlight
            using (SolidBrush glareBrush = new SolidBrush(Color.FromArgb(180, 255, 255, 255)))
            {
                g.FillEllipse(glareBrush, beanRect.X + beanRect.Width * 0.18f, beanRect.Y + beanRect.Height * 0.18f, beanRect.Width * 0.25f, beanRect.Height * 0.25f);
            }

            g.Restore(state);
        }

        private void DrawPlasticChip(Graphics g, Rectangle rect, Color baseColor)
        {
            float chipDiameter = rect.Width * 0.5f;
            float chipX = rect.X + (rect.Width - chipDiameter) / 2f;
            float chipY = rect.Y + (rect.Height - chipDiameter) / 2f + 5f;

            RectangleF chipRect = new RectangleF(chipX, chipY, chipDiameter, chipDiameter);

            // 1. Shadow
            using (SolidBrush shadowBrush = new SolidBrush(Color.FromArgb(70, 0, 0, 0)))
            {
                g.FillEllipse(shadowBrush, chipRect.X + 2, chipRect.Y + 3, chipRect.Width, chipRect.Height);
            }

            // 2. Translucent body
            using (SolidBrush bodyBrush = new SolidBrush(baseColor))
            {
                g.FillEllipse(bodyBrush, chipRect);
            }

            // 3. Beveled border (3D edge)
            using (Pen ridgePen = new Pen(Color.FromArgb(130, Color.White), 2f))
            {
                g.DrawEllipse(ridgePen, chipRect);
            }
            using (Pen darkRidgePen = new Pen(Color.FromArgb(70, Color.Black), 1f))
            {
                g.DrawEllipse(darkRidgePen, chipRect.X + 1, chipRect.Y + 1, chipRect.Width - 2, chipRect.Height - 2);
            }

            // 4. Glossy specular highlight
            float glareWidth = chipRect.Width * 0.35f;
            float glareHeight = chipRect.Height * 0.2f;
            float glareX = chipRect.X + chipRect.Width * 0.2f;
            float glareY = chipRect.Y + chipRect.Height * 0.15f;
            RectangleF glareRect = new RectangleF(glareX, glareY, glareWidth, glareHeight);

            GraphicsState state = g.Save();
            g.TranslateTransform(glareRect.X + glareRect.Width / 2f, glareRect.Y + glareRect.Height / 2f);
            g.RotateTransform(-15f);
            RectangleF localGlare = new RectangleF(-glareRect.Width / 2f, -glareRect.Height / 2f, glareRect.Width, glareHeight);

            using (LinearGradientBrush glareBrush = new LinearGradientBrush(
                localGlare,
                Color.FromArgb(170, 255, 255, 255),
                Color.FromArgb(0, 255, 255, 255),
                90f))
            {
                g.FillEllipse(glareBrush, localGlare);
            }
            g.Restore(state);

            // 5. Poker chip dash ring
            using (Pen innerPen = new Pen(Color.FromArgb(50, Color.White), 1f))
            {
                innerPen.DashStyle = DashStyle.Dash;
                float innerSize = chipDiameter * 0.6f;
                g.DrawEllipse(innerPen, chipX + (chipDiameter - innerSize) / 2f, chipY + (chipDiameter - innerSize) / 2f, innerSize, innerSize);
            }
        }

        private GraphicsPath GetRoundedRectPath(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int diameter = radius * 2;

            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);

            path.CloseFigure();
            return path;
        }
    }
}
