using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ReferencePresenter {
    public partial class Form1 : Form {

        private Image CurrentImage;
        private Bitmap CurrentImageGrayscale;
        private Task GrayScaleLoader;

        private const int cGrip = 20;      // Grip size
        private const int cCaption = 15;   // Caption bar height;

        private string currentFilePath;
        private bool mouseDown;
        private Point lastLocation;
        private bool borderVisible = true;
        private bool grayscale = false;
        private bool alwaysOnTop = true;
        private bool autoResize = true;
        MenuItem menuItemGrayscale = new MenuItem("[G] grayscale");

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
            this.KeyUp += Form_KeyDown;
        }

        public bool LoadImage(string file, bool showMessage = true) {
            if (label1.Visible) {
                label1.Visible = false;
                label2.Visible = false;
            }
            try {
                CurrentImage = Image.FromFile(file);
            } catch (Exception e) {
                if (showMessage)
                    MessageBox.Show("Could not load image!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            currentFilePath = file;
            pictureBox1.Image = CurrentImage;
            //menuItemGrayscale.Checked = false;
            CurrentImageGrayscale = new Bitmap((Image)CurrentImage.Clone());
            if (autoResize)
                ResizeFormWithSamePerimeter();
            GrayScaleLoader = new Task(() => {
                CurrentImageGrayscale = ConvertToGrayScale(CurrentImageGrayscale);
                if (grayscale)
                    pictureBox1.Invoke(new Action(() => { pictureBox1.Image = CurrentImageGrayscale; }));
            });
            GrayScaleLoader.Start();
            return true;
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
            cm.MenuItems.Add("-");
            AddNextImageOption(cm);
            AddPreviousImageOption(cm);
            AddRandomImageOption(cm);
            cm.MenuItems.Add("-");

            AddGrayScaleOption(cm);
            AddFlippOption(cm);
            cm.MenuItems.Add("-");

            //AddResizeOption(cm);
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
                SwitchToGrayscaleImage();
            };
            cm.MenuItems.Add(menuItemGrayscale);
        }

        private void SwitchToGrayscaleImage() {
            if (CurrentImage == null)
                return;
            GrayScaleLoader.Wait();
            if (grayscale)
                pictureBox1.Image = CurrentImage;
            else
                pictureBox1.Image = CurrentImageGrayscale;
            grayscale = !grayscale;
            menuItemGrayscale.Checked = grayscale;
        }

        private void AddFlippOption(ContextMenu cm) {
            var flipp = new MenuItem("[F] flip horizontal");
            flipp.Click += (a, b) => {
                FlippImage();
            };
            cm.MenuItems.Add(flipp);
        }

        private void FlippImage() {
            if (CurrentImage == null)
                return;
            GrayScaleLoader.Wait();
            CurrentImageGrayscale?.RotateFlip(RotateFlipType.RotateNoneFlipX);
            CurrentImage?.RotateFlip(RotateFlipType.RotateNoneFlipX);
            pictureBox1.Image = pictureBox1.Image;
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

        private void AddRandomImageOption(ContextMenu cm) {
            var itemChangeImage = new MenuItem("[R] random image");
            itemChangeImage.Click += (a, b) => {
                OpenRandomImage();
            };
            cm.MenuItems.Add(itemChangeImage);
        }

        private void OpenRandomImage() {
            if (!File.Exists(currentFilePath))
                return;
            var files = Directory.GetFiles(Path.GetDirectoryName(currentFilePath));
            int index = new Random().Next(0, files.Count());
            // Iterate files until one can be opened
            for (int x = 0; x < files.Count() && !LoadImage(files[(index + x) % files.Count()], false); x++) { }
        }

        private void AddPreviousImageOption(ContextMenu cm) {
            var itemChangeImage = new MenuItem("[<] previous file");
            itemChangeImage.Click += (a, b) => {
                OpenPreviousFile();
            };
            cm.MenuItems.Add(itemChangeImage);
        }

        private void OpenPreviousFile() {
            if (!File.Exists(currentFilePath))
                return;
            var files = Directory.GetFiles(Path.GetDirectoryName(currentFilePath));
            for (int i = 0; i < files.Count(); i++) {
                if (Path.GetFullPath(files[i]) == Path.GetFullPath(currentFilePath)) {
                    // Iterate files until one can be opened
                    for (int x = 1; x <= files.Count() && !LoadImage(files[(i - x + files.Count()) % files.Count()], false); x++) { }
                    return;
                }
            }
        }

        private void AddNextImageOption(ContextMenu cm) {
            var itemChangeImage = new MenuItem("[>] next file");
            itemChangeImage.Click += (a, b) => {
                OpenNextFile();
            };
            cm.MenuItems.Add(itemChangeImage);
        }

        private void OpenNextFile() {
            if (!File.Exists(currentFilePath))
                return;
            var files = Directory.GetFiles(Path.GetDirectoryName(currentFilePath));
            for (int i = 0; i < files.Count(); i++) {
                if (Path.GetFullPath(files[i]) == Path.GetFullPath(currentFilePath)) {
                    // Iterate files until one can be opened
                    for (int x = 1; x <= files.Count() && !LoadImage(files[(i + x) % files.Count()], false); x++) { }
                    return;
                }
            }
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

        private void ResizeFormWithSamePerimeter() {
            if (CurrentImage == null)
                return;
            var centerPoint = new Point(this.Location.X + this.Width / 2, this.Location.Y + this.Height / 2);
            if (pictureBox1.Image.Width / (double)this.Width > pictureBox1.Image.Height / (double)this.Height) {
                double scale = pictureBox1.Image.Width / (double)this.Width;
                double diff = pictureBox1.Image.Height / scale - this.Height;
                double factor = (this.Height + this.Width) / (this.Height + diff + this.Width);
                this.Height = (int)Math.Round((this.Height + diff) * factor);
                this.Width = (int)Math.Round((this.Width) * factor);
            } else {
                double scale = pictureBox1.Image.Height / (double)this.Height;
                double diff = pictureBox1.Image.Width / scale - this.Width;
                double factor = (this.Height + this.Width) / (this.Height + diff + this.Width);
                this.Height = (int)Math.Round((this.Height) * factor);
                this.Width = (int)Math.Round((this.Width + diff) * factor);
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

        public Bitmap ConvertToGrayScale(Bitmap original) {
            // https://web.archive.org/web/20130111215043/http://www.switchonthecode.com/tutorials/csharp-tutorial-convert-a-color-image-to-grayscale
            //create a blank bitmap the same size as original
            Bitmap newBitmap = new Bitmap(original.Width, original.Height);

            //get a graphics object from the new image
            Graphics g = Graphics.FromImage(newBitmap);

            //create the grayscale ColorMatrix
            ColorMatrix colorMatrix = new ColorMatrix(
               new float[][]
               {
                 new float[] {.3f, .3f, .3f, 0, 0},
                 new float[] {.59f, .59f, .59f, 0, 0},
                 new float[] {.11f, .11f, .11f, 0, 0},
                 new float[] {0, 0, 0, 1, 0},
                 new float[] {0, 0, 0, 0, 1}
               });

            //create some image attributes
            ImageAttributes attributes = new ImageAttributes();

            //set the color matrix attribute
            attributes.SetColorMatrix(colorMatrix);

            //draw the original image on the new image
            //using the grayscale color matrix
            g.DrawImage(original, new Rectangle(0, 0, original.Width, original.Height),
               0, 0, original.Width, original.Height, GraphicsUnit.Pixel, attributes);

            //dispose the Graphics object
            g.Dispose();
            return newBitmap;
        }

        private void Form_KeyDown(object sender, KeyEventArgs e) {
            if (e.KeyCode == Keys.Left)
                OpenPreviousFile();
            if (e.KeyCode == Keys.Right)
                OpenNextFile();
            if (e.KeyCode == Keys.F)
                FlippImage();
            if (e.KeyCode == Keys.G)
                SwitchToGrayscaleImage();
            if (e.KeyCode == Keys.R)
                OpenRandomImage();
        }

    }
}
