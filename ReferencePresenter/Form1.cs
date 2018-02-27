using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ReferencePresenter {
    public partial class Form1 : Form {

        private Image CurrentImage;
        private Bitmap CurrentImageGrayscale;

        private const int cGrip = 20;      // Grip size
        private const int cCaption = 15;   // Caption bar height;

        private bool mouseDown;
        private Point lastLocation;
        private bool borderVisible = true;
        private bool grayscale = false;
        private bool alwaysOnTop = true;
        private bool autoResize = true;
        MenuItem menuItemGrayscale = new MenuItem("grayscale");

        private const int MINMAX = 100;
        private const double ZOOMFACTOR = 1.2;

        public Form1(string picture) {
            InitializeComponent();
            InitEventListener();
            InitContextMenu();
            InitFormWindow();
            InitPictureBox(picture);
        }

        private void InitPictureBox(string picture) {
            if (picture != null) {
                label1.Visible = false;
                label2.Visible = false;
                LoadImage(picture);
            }
        }

        private void InitFormWindow() {
            Toggleborder();
            this.FormBorderStyle = FormBorderStyle.None;
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.ResizeRedraw, true);
            this.TopMost = alwaysOnTop;
        }

        public void LoadImage(string file) {
            if (label1.Visible) {
                label1.Visible = false;
                label2.Visible = false;
            }
            try {
                CurrentImage = Image.FromFile(file);
            } catch (Exception e) {
                MessageBox.Show("Could not load image!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            pictureBox1.Image = CurrentImage;
            menuItemGrayscale.Checked = false;
            CurrentImageGrayscale = new Bitmap((Image)CurrentImage.Clone());
            if (autoResize)
                ResizeForm();
            new Thread(() => {
                CurrentImageGrayscale = ConvertToGrayScale(CurrentImageGrayscale);
                if (grayscale)
                    pictureBox1.Invoke(new Action(() => { pictureBox1.Image = CurrentImageGrayscale; }));
            }).Start();
        }

        private void InitEventListener() {
            label1.MouseDown += PictureBox1_MouseDown;
            label1.MouseUp += PictureBox1_MouseUp;
            label1.MouseMove += PictureBox1_MouseMove;
            label2.MouseDown += PictureBox1_MouseDown;
            label2.MouseUp += PictureBox1_MouseUp;
            label2.MouseMove += PictureBox1_MouseMove;
            pictureBox1.MouseDown += PictureBox1_MouseDown;
            pictureBox1.MouseUp += PictureBox1_MouseUp;
            pictureBox1.MouseMove += PictureBox1_MouseMove;
            this.AllowDrop = true;
            this.DragEnter += new DragEventHandler(Form1_DragEnter);
            this.DragDrop += new DragEventHandler(Form1_DragDrop);
        }

        private void InitContextMenu() {
            ContextMenu cm = new ContextMenu();

            AddChangeImageOption(cm);
            AddResizeOption(cm);
            cm.MenuItems.Add("-");

            AddGrayScaleOption(cm);
            AddFlippOption(cm);
            cm.MenuItems.Add("-");

            AddToggleWindowBorderOption(cm);
            AddAutoResizeOption(cm);
            AddToggleAlwaysOnTopOption(cm);
            AddChangeBackgroundColor(cm);
            cm.MenuItems.Add("-");

            cm.MenuItems.Add("close", (a, b) => Environment.Exit(0));

            pictureBox1.ContextMenu = cm;
        }

        #region ContextMenue Options
        private void AddChangeBackgroundColor(ContextMenu cm) {
            var itemColor = new MenuItem("change color");
            itemColor.Click += (a, b) => {
                var dialog = new ColorDialog();
                var result = dialog.ShowDialog();
                if (result == DialogResult.OK) {
                    label1.BackColor = dialog.Color;
                    label2.BackColor = dialog.Color;
                    pictureBox1.BackColor = dialog.Color;
                }
            };
            cm.MenuItems.Add(itemColor);
        }

        private void AddToggleAlwaysOnTopOption(ContextMenu cm) {
            var itemAlwaysTop = new MenuItem("always on top") { Checked = alwaysOnTop };
            itemAlwaysTop.Click += (a, b) => {
                alwaysOnTop = !alwaysOnTop;
                itemAlwaysTop.Checked = alwaysOnTop;
                this.TopMost = alwaysOnTop;
            };
            cm.MenuItems.Add(itemAlwaysTop);
        }

        private void AddAutoResizeOption(ContextMenu cm) {
            var itemAutoResize = new MenuItem("auto resize") { Checked = autoResize };
            itemAutoResize.Click += (a, b) => {
                autoResize = !autoResize;
                itemAutoResize.Checked = autoResize;
                ResizeForm();
            };
            cm.MenuItems.Add(itemAutoResize);
        }

        private void AddGrayScaleOption(ContextMenu cm) {
            menuItemGrayscale.Click += (a, b) => {
                if (grayscale)
                    pictureBox1.Image = CurrentImage;
                else
                    pictureBox1.Image = CurrentImageGrayscale;
                grayscale = !grayscale;
                menuItemGrayscale.Checked = grayscale;
            };
            cm.MenuItems.Add(menuItemGrayscale);
        }
        private void AddFlippOption(ContextMenu cm) {
            var flipp = new MenuItem("flip horizontal");
            flipp.Click += (a, b) => {
                CurrentImageGrayscale?.RotateFlip(RotateFlipType.RotateNoneFlipX);
                CurrentImage?.RotateFlip(RotateFlipType.RotateNoneFlipX);
                pictureBox1.Image = pictureBox1.Image;
            };
            cm.MenuItems.Add(flipp);
        }

        private void AddToggleWindowBorderOption(ContextMenu cm) {
            var itemBorder = new MenuItem("window border");
            itemBorder.Click += (a, b) => {
                Toggleborder();
                itemBorder.Checked = borderVisible;
            };
            cm.MenuItems.Add(itemBorder);
        }

        private void AddResizeOption(ContextMenu cm) {
            var itemResize = new MenuItem("resize");
            itemResize.Click += (a, b) => {
                ResizeForm();
            };
            cm.MenuItems.Add(itemResize);
        }

        private void AddChangeImageOption(ContextMenu cm) {
            var itemChangeImage = new MenuItem("change image");
            itemChangeImage.Click += (a, b) => {
                var dialog = new OpenFileDialog() {
                    Title = "Select a image",
                };
                if (dialog.ShowDialog() == DialogResult.OK) {
                    LoadImage(dialog.FileName);
                }
            };
            cm.MenuItems.Add(itemChangeImage);
        }
        #endregion


        private void ResizeForm() {
            if (CurrentImage == null)
                return;
            var centerPoint = new Point(this.Location.X + this.Width / 2, this.Location.Y + this.Height / 2);
            if (pictureBox1.Image.Width / (double)this.Width > pictureBox1.Image.Height / (double)this.Height) {
                double scale = pictureBox1.Image.Width / (double)this.Width;
                this.Height = (int)Math.Round(pictureBox1.Image.Height / scale);
            } else {
                double scale = pictureBox1.Image.Height / (double)this.Height;
                this.Width = (int)Math.Round(pictureBox1.Image.Width / scale);
            }
            this.SetDesktopLocation((int)Math.Round(centerPoint.X - this.Width / 2.0), (int)Math.Round(centerPoint.Y + 0.0 - this.Height / 2.0));
        }

        private void Toggleborder() {
            if (borderVisible) {
                this.TransparencyKey = Color.Turquoise;
                this.BackColor = Color.Turquoise;
            } else {
                this.TransparencyKey = Color.Turquoise;
                this.BackColor = Color.Green;
            }
            borderVisible = !borderVisible;
        }

        private void PictureBox1_MouseDown(object sender, MouseEventArgs e) {
            mouseDown = true;
            lastLocation = e.Location;
        }

        private void PictureBox1_MouseMove(object sender, MouseEventArgs e) {
            if (mouseDown) {
                this.Location = new Point(
                    (this.Location.X - lastLocation.X) + e.X, (this.Location.Y - lastLocation.Y) + e.Y);

                this.Update();
            }
        }

        private void PictureBox1_MouseUp(object sender, MouseEventArgs e) {
            mouseDown = false;
        }
        protected override void OnPaint(PaintEventArgs e) {
            Rectangle rc = new Rectangle(this.ClientSize.Width - cGrip, this.ClientSize.Height - cGrip, cGrip, cGrip);
            ControlPaint.DrawSizeGrip(e.Graphics, this.BackColor, rc);
        }

        protected override void WndProc(ref Message m) {
            if (m.Msg == 0x84) {  // Trap WM_NCHITTEST
                Point pos = new Point(m.LParam.ToInt32() & 0xffff, m.LParam.ToInt32() >> 16);
                pos = this.PointToClient(pos);
                if (pos.Y < cCaption) {
                    m.Result = (IntPtr)2;  // HTCAPTION
                    if (autoResize)
                        ResizeForm();
                    return;
                }
                if (pos.X >= this.ClientSize.Width - cGrip && pos.Y >= this.ClientSize.Height - cGrip) {
                    m.Result = (IntPtr)17; // HTBOTTOMRIGHT
                    if (autoResize)
                        ResizeForm();
                    return;
                }
            }
            base.WndProc(ref m);
        }

        void Form1_DragEnter(object sender, DragEventArgs e) {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
        }

        void Form1_DragDrop(object sender, DragEventArgs e) {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files.Count() > 1) {
                foreach (string file in files)
                    Console.WriteLine(file);
            }
            LoadImage(files.First());
        }

        public Bitmap ConvertToGrayScale(Bitmap Bmp) {
            int rgb;
            Color c;

            for (int y = 0; y < Bmp.Height; y++)
                for (int x = 0; x < Bmp.Width; x++) {
                    c = Bmp.GetPixel(x, y);
                    rgb = (int)((c.R + c.G + c.B) / 3);
                    Bmp.SetPixel(x, y, Color.FromArgb(rgb, rgb, rgb));
                }
            return Bmp;
        }
    }
}
