using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Draw_HJ
{
    public partial class MainDraw : Form
    {
        private DrawModel dm;
        private Graphics setPaint;
        private Stack<Image> undoStack = new Stack<Image>();
        private Stack<Image> redoStack = new Stack<Image>();
        private BackgroundWorker bw = new BackgroundWorker();
        Dictionary<string, IPlugin> _Plugins;
        private string ActivePlugin { get; set; }
        private readonly object undoRedoLocker = new object();
        private int mouseStartX = 0;
        private int mouseStartY = 0;
        private int mouseCurrentX = 0;
        private int mouseCurrentY = 0;
        private int recSartPointX = 0;
        private int recSartPointY = 0;
        private int recSizeY = 0;
        private int recSizeX = 0;
        private bool mouseDown = false;
        private Bitmap bm;
        private int curFormWidth;
        private int curFormHeight;

        public MainDraw()
        {
            InitializeComponent();

            curFormHeight = this.Height;
            curFormWidth = this.Width;

            bw.WorkerReportsProgress = true;
            bw.DoWork += new DoWorkEventHandler(bw_DoWork);
            bw.ProgressChanged += new ProgressChangedEventHandler(bw_ProgressChanged);
            bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bw_RunWorkerCompleted);

            setPaint = pictureBox.CreateGraphics();
            bm = new Bitmap(pictureBox.Width, pictureBox.Height);
            pictureBox.Image = bm;
            toolStripWidth.Text = pictureBox.Image.Width.ToString();
            toolStripHeigth.Text = pictureBox.Image.Height.ToString();

            dm = new DrawModel()
            {
                Color = Color.Black,
                ColorBackground = Color.White,
                Tool = Tools.TPencil,
                G = pictureBox.CreateGraphics(),
                Fill = Fills.FFill,
                Pixel = 1
            };

            ChangeCulture(Thread.CurrentThread.CurrentUICulture);
        }
        private void MainDraw_SizeChanged(object sender, EventArgs e)
        {
            flowLayoutPanel1.Width += this.Width - curFormWidth;
            flowLayoutPanel1.Height += this.Height - curFormHeight;

            curFormHeight = this.Height;
            curFormWidth = this.Width;
        }

        #region Mechanizm Undo/Redo 
        private void Undo()
        {
            lock (undoRedoLocker)
            {
                if (undoStack.Count > 0)
                {
                    Image image = undoStack.Pop();
                    redoStack.Push(image);
                    pictureBox.Image = image;
                    pictureBox.Invalidate();
                    pictureBox.Refresh();
                }
            }
        }

        private void Redo()
        {
            lock (undoRedoLocker)
            {
                if (redoStack.Count > 0)
                {
                    undoStack.Push(redoStack.Pop());
                    pictureBox.Image = undoStack.Peek();
                    pictureBox.Invalidate();
                    pictureBox.Refresh();
                }
            }
        }

        private void UpdateImageData(Action updateImage)
        {
            lock (undoRedoLocker)
            {
                undoStack.Push(pictureBox.Image);

                try
                {
                    updateImage();
                }
                catch
                {
                    undoStack.Pop();
                    throw;
                }
            }
        }
        #endregion

        #region Operacje pictureBox
        private void pictureBox_Paint(object sender, PaintEventArgs e)
        {
            Graphics setPaint = e.Graphics;
            if (dm.Tool != Tools.TPencil || dm.Tool != Tools.TBrush || dm.Tool != Tools.TElastic)
            {
                if (mouseDown == true)
                {
                    Pen size = new Pen(dm.Color, float.Parse(dm.Pixel.ToString()));

                    if (dm.Tool == Tools.TLine)
                    {
                        setPaint.DrawLine(size, new Point(mouseStartX, mouseStartY), new Point(mouseCurrentX + mouseStartX, mouseCurrentY + mouseStartY));
                    }
                    else if (dm.Tool == Tools.TCircle)
                    {
                        setPaint.DrawEllipse(size, mouseStartX, mouseStartY, mouseCurrentX, mouseCurrentY);
                    }
                    else if (dm.Tool == Tools.TRectangle)
                    {
                        setPaint.DrawRectangle(size, recSartPointX, recSartPointY, recSizeX, recSizeY);
                    }
                }
            }
            //setPaint.DrawImage(bm, new Point(0, 0));
        }

        private void pictureBox_MouseMove(object sender, MouseEventArgs e)
        {
            xStatusLabel.Text = "X:" + e.X.ToString();
            yStatusLabel.Text = "Y:" + e.Y.ToString();

            if (mouseDown == true)
            {
                mouseCurrentX = e.X - mouseStartX;
                mouseCurrentY = e.Y - mouseStartY;

                if (dm.Tool == Tools.TPencil || dm.Tool == Tools.TBrush || dm.Tool == Tools.TElastic)
                {
                    Graphics setPaint = Graphics.FromImage(bm);
                        if (dm.Tool == Tools.TPencil)
                        {
                            setPaint.FillEllipse(new SolidBrush(dm.Color), e.X, e.Y, 2, 2);
                        }
                        else if (dm.Tool == Tools.TBrush)
                        {
                            setPaint.FillEllipse(new SolidBrush(dm.Color), e.X, e.Y, 2 + dm.Pixel, 2 + dm.Pixel);
                        }
                        else if (dm.Tool == Tools.TElastic)
                        {
                            setPaint.FillEllipse(new SolidBrush(Color.White), e.X, e.Y, 8, 8);
                        }
                }

                pictureBox.Invalidate();
            }
            else
            {
                pictureBox.Invalidate();
            }

            recSartPointX = Math.Min(mouseStartX, e.X);
            recSartPointY = Math.Min(mouseStartY, e.Y);
            recSizeX = Math.Max(mouseStartX, e.X) - Math.Min(mouseStartX, e.X);
            recSizeY = Math.Max(mouseStartY, e.Y) - Math.Min(mouseStartY, e.Y);
        }

        private void pictureBox_MouseClick(object sender, MouseEventArgs e)
        {
            UpdateImageData(() =>
            {
                pictureBox.Invalidate();

                Image pic = pictureBox.Image;
                bm = new Bitmap(pic);
                setPaint  = Graphics.FromImage(bm);
                pictureBox.Image = bm;

                pictureBox.Refresh();
            });
        }

        private void pictureBox_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                mouseDown = true;
                mouseStartX = e.X;
                mouseStartY = e.Y;
            }
        }

        private void pictureBox_MouseLeave(object sender, EventArgs e)
        {

        }

        private void pictureBox_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                Pen size = new Pen(dm.Color, float.Parse(dm.Pixel.ToString()));

                mouseDown = false;
                setPaint = Graphics.FromImage(bm);

                if (dm.Tool == Tools.TLine)
                {
                    setPaint.DrawLine(size, new Point(mouseStartX, mouseStartY), new Point(mouseCurrentX + mouseStartX, mouseCurrentY + mouseStartY));
                }
                else if (dm.Tool == Tools.TCircle)
                {
                    if(dm.Fill == Fills.FFill)
                    {
                        setPaint.DrawEllipse(size, mouseStartX, mouseStartY, mouseCurrentX, mouseCurrentY);
                        setPaint.FillEllipse(new SolidBrush(dm.ColorBackground), mouseStartX + dm.Pixel, mouseStartY + dm.Pixel, mouseCurrentX - dm.Pixel, mouseCurrentY - dm.Pixel);
                    }
                    else
                    {
                        setPaint.DrawEllipse(size, mouseStartX, mouseStartY, mouseCurrentX, mouseCurrentY);
                    }
                }
                else if (dm.Tool == Tools.TRectangle)
                {
                    if (dm.Fill == Fills.FFill)
                    {
                        setPaint.DrawRectangle(size, recSartPointX, recSartPointY, recSizeX, recSizeY);
                        setPaint.FillRectangle(new SolidBrush(dm.ColorBackground), recSartPointX + dm.Pixel, recSartPointY + dm.Pixel, recSizeX - dm.Pixel, recSizeY - dm.Pixel);
                    }
                    else
                    {
                        setPaint.DrawRectangle(size, recSartPointX, recSartPointY, recSizeX, recSizeY);
                    }
                }
                setPaint.DrawImage(bm, 0, 0);
                pictureBox.Image = bm;
                pictureBox.Invalidate();
            }
        }
        #endregion

        #region Definiowanie kolorów

        private void Default_Click(object sender, EventArgs e)
        {
            colorDialog.AllowFullOpen = true;
            colorDialog.ShowHelp = true;
            colorDialog.Color = Default.BackColor;

            if (colorDialog.ShowDialog() == DialogResult.OK)
            {
                dm.Color = colorDialog.Color;
                Default.BackColor = dm.Color;
            }
        }
        private void Back_Click(object sender, EventArgs e)
        {
            colorDialog.AllowFullOpen = true;
            colorDialog.ShowHelp = true;
            colorDialog.Color = Default.BackColor;

            if (colorDialog.ShowDialog() == DialogResult.OK)
            {
                dm.ColorBackground = colorDialog.Color;
                Back.BackColor = dm.ColorBackground;
            }
        }
        void BlackClick(object sender, EventArgs e)
        {
            dm.Color = Color.Black;
            Default.BackColor = dm.Color;
        }
        void DarkGrayClick(object sender, EventArgs e)
        {
            dm.Color = Color.DarkGray;
            Default.BackColor = dm.Color;
        }
        void BrownClick(object sender, EventArgs e)
        {
            dm.Color = Color.Brown;
            Default.BackColor = dm.Color;

        }
        void GrayClick(object sender, EventArgs e)
        {
            dm.Color = Color.Gray;
            Default.BackColor = dm.Color;
        }
        void MaroonClick(object sender, EventArgs e)
        {
            dm.Color = Color.Maroon;
            Default.BackColor = dm.Color;
        }
        void RedClick(object sender, EventArgs e)
        {
            dm.Color = Color.Red;
            Default.BackColor = dm.Color;
        }
        void WhiteClick(object sender, EventArgs e)
        {
            dm.Color = Color.White;
            Default.BackColor = dm.Color;
        }
        void PinkClick(object sender, EventArgs e)
        {
            dm.Color = Color.Pink;
            Default.BackColor = dm.Color;
        }
        void YellowClick(object sender, EventArgs e)
        {
            dm.Color = Color.OrangeRed;
            Default.BackColor = dm.Color;
        }
        void OrangeRedClick(object sender, EventArgs e)
        {
            dm.Color = Color.Yellow;
            Default.BackColor = dm.Color;
        }
        void GoldClick(object sender, EventArgs e)
        {
            dm.Color = Color.Gold;
            Default.BackColor = dm.Color;
        }
        void LightSalmonClick(object sender, EventArgs e)
        {
            dm.Color = Color.LightSalmon;
            Default.BackColor = dm.Color;
        }
        void GreenClick(object sender, EventArgs e)
        {
            dm.Color = Color.Green;
            Default.BackColor = dm.Color;
        }
        void YellowGreenClick(object sender, EventArgs e)
        {
            dm.Color = Color.YellowGreen;
            Default.BackColor = dm.Color;
        }
        void SteelBlueClick(object sender, EventArgs e)
        {
            dm.Color = Color.SteelBlue;
            Default.BackColor = dm.Color;
        }

        void AquaClick(object sender, EventArgs e)
        {
            dm.Color = Color.Aqua;
            Default.BackColor = dm.Color;
        }

        void MediumSlateBlueClick(object sender, EventArgs e)
        {
            dm.Color = Color.MediumSlateBlue;
            Default.BackColor = dm.Color;
        }
        void RoyalBlueClick(object sender, EventArgs e)
        {
            dm.Color = Color.RoyalBlue;
            Default.BackColor = dm.Color;
        }
        void PurpleClick(object sender, EventArgs e)
        {
            dm.Color = Color.Purple;
            Default.BackColor = dm.Color;
        }

        void BisqueClick(object sender, EventArgs e)
        {
            dm.Color = Color.Bisque;
            Default.BackColor = dm.Color;
        }

        private void tUndo_Click(object sender, EventArgs e)
        {
            Undo();
        }

        private void tRedo_Click(object sender, EventArgs e)
        {
            Redo();
        }

        #endregion

        #region Operacje menu
        private void newToolStripMenu_Click(object sender, EventArgs e)
        {
            if (pictureBox.Image != null)
            {
                pictureBox.Image.Dispose();
                pictureBox.Image = new Bitmap(pictureBox.Width, pictureBox.Height);
                pictureBox.Refresh();
            }

            toolStripWidth.Text = pictureBox.Image.Width.ToString();
            toolStripHeigth.Text = pictureBox.Image.Height.ToString();
        }
        private void openToolStripMenu_Click(object sender, EventArgs e)
        {
            OpenFileDialog o = new OpenFileDialog();
            o.Filter = "PNG|*.png|JPEG|*jpg|BITMAP|*.bmp";
            if (o.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                pictureBox.Image = (Image)Image.FromFile(o.FileName).Clone();
            }

            toolStripWidth.Text = pictureBox.Image.Width.ToString();
            toolStripHeigth.Text = pictureBox.Image.Height.ToString();
        }

        private void saveToolStripMenu_Click(object sender, EventArgs e)
        {
            Bitmap bmp = new Bitmap(pictureBox.Width, pictureBox.Height);
            Graphics g = Graphics.FromImage(bmp);
            Rectangle rect = pictureBox.RectangleToScreen(pictureBox.ClientRectangle);
            g.CopyFromScreen(rect.Location, Point.Empty, pictureBox.Size);
            g.Dispose();
            SaveFileDialog s = new SaveFileDialog();
            s.Filter = "PNG|*.png|JPEG|*jpg|BITMAP|*.bmp";
            if (s.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                if (File.Exists(s.FileName))
                {
                    File.Delete(s.FileName);
                }
                if (s.FileName.Contains(".jpg"))
                {
                    bmp.Save(s.FileName, ImageFormat.Jpeg);
                }
                else if (s.FileName.Contains(".png"))
                {
                    bmp.Save(s.FileName, ImageFormat.Png);
                }
                else if (s.FileName.Contains(".bmp"))
                {
                    bmp.Save(s.FileName, ImageFormat.Bmp);
                }
            }
        }

        private void closeToolStripMenu_Click(object sender, EventArgs e)
        {
            if (pictureBox.Image != null)
            {

            }
            System.Windows.Forms.Application.Exit();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new About().ShowDialog();
        }

        private void colorPickerToolStripMenu_Click(object sender, EventArgs e)
        {
            colorDialog.AllowFullOpen = true;
            colorDialog.ShowHelp = true;
            colorDialog.Color = Default.BackColor;

            if (colorDialog.ShowDialog() == DialogResult.OK)
                Default.BackColor = colorDialog.Color;
        }
        #endregion

        #region Operacje tool
        private void toolStript_TextChanged(object sender, EventArgs e)
        {

        }

        private void Pencil_Click(object sender, EventArgs e)
        {
            dm.Tool = Tools.TPencil;
        }

        private void Brush_Click(object sender, EventArgs e)
        {
            dm.Tool = Tools.TBrush;
        }

        private void Elastic_Click(object sender, EventArgs e)
        {
            dm.Tool = Tools.TElastic;
        }

        private void Line_Click(object sender, EventArgs e)
        {
            dm.Tool = Tools.TLine;
        }

        private void Rectangle_Click(object sender, EventArgs e)
        {
            dm.Tool = Tools.TRectangle;
        }

        private void Circle_Click(object sender, EventArgs e)
        {
            dm.Tool = Tools.TCircle;
        }

        private void toolStrip_Leave(object sender, EventArgs e)
        {
            Image pic = pictureBox.Image;
            Size size = new Size(Convert.ToInt32(toolStripWidth.Text), Convert.ToInt32(toolStripHeigth.Text));

            pictureBox.Image = dm.Resize(pic, size);

            pictureBox.Refresh();
        }

        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            dm.Pixel = 1;
            toolStripMenuItem2.Checked = true;
            toolStripMenuItem3.Checked = false;
            toolStripMenuItem4.Checked = false;
            toolStripMenuItem5.Checked = false;
        }

        private void toolStripMenuItem3_Click(object sender, EventArgs e)
        {
            dm.Pixel = 3;
            toolStripMenuItem2.Checked = false;
            toolStripMenuItem3.Checked = true;
            toolStripMenuItem4.Checked = false;
            toolStripMenuItem5.Checked = false;
        }

        private void toolStripMenuItem4_Click(object sender, EventArgs e)
        {
            dm.Pixel = 6;
            toolStripMenuItem2.Checked = false;
            toolStripMenuItem3.Checked = false;
            toolStripMenuItem4.Checked = true;
            toolStripMenuItem5.Checked = false;
        }

        private void toolStripMenuItem5_Click(object sender, EventArgs e)
        {
            dm.Pixel = 8;
            toolStripMenuItem2.Checked = false;
            toolStripMenuItem3.Checked = false;
            toolStripMenuItem4.Checked = false;
            toolStripMenuItem5.Checked = true;
        }
        private void fillToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (sender == yesToolStripMenuItem)
            {
                dm.Fill = Fills.FFill;
                yesToolStripMenuItem.Checked = true;
                noToolStripMenuItem.Checked = false;
            }
            else
            {
                dm.Fill = Fills.FDraw;
                yesToolStripMenuItem.Checked = false;
                noToolStripMenuItem.Checked = true;
            }
        }
        #endregion

        #region Lokalizacja
        private void ChangeCulture(CultureInfo culture)
        {
            Thread.CurrentThread.CurrentUICulture = culture;
            ComponentResourceManager resources = new ComponentResourceManager(typeof(MainDraw));

            resources.ApplyResources(this, "$this", culture);
            toolTip.SetToolTip(this, resources.GetString("$this.ToolTip"));
            UpdateControlsCulture(this, resources, culture);

            if (culture.Name == "pl-PL")
            {
                polishToolStripMenuItem.Checked = true;
                englishToolStripMenuItem.Checked = false;
            }
            else
            {
                englishToolStripMenuItem.Checked = true;
                polishToolStripMenuItem.Checked = false;
            }
        }

        private void UpdateControlsCulture(Control control, ComponentResourceManager resourceProvider, CultureInfo culture)
        {
            control.SuspendLayout();
            resourceProvider.ApplyResources(control, control.Name, culture);

            foreach (Control ctrl in control.Controls)
            {
                UpdateControlsCulture(ctrl, resourceProvider, culture);
            }

            PropertyInfo property = control.GetType().GetProperty("Items");
            if (property != null)
            {
                foreach (ToolStripItem item in (IList)property.GetValue(control, null))
                {
                    UpdateToolStripItemsCulture(item, resourceProvider, culture);
                }
            }

            control.ResumeLayout(false);
        }

        private void UpdateToolStripItemsCulture(ToolStripItem item, ComponentResourceManager resourceProvider,
            CultureInfo culture)
        {
            resourceProvider.ApplyResources(item, item.Name, culture);

            if (item is ToolStripMenuItem)
            {
                foreach (ToolStripItem it in ((ToolStripMenuItem)item).DropDownItems)
                {
                    UpdateToolStripItemsCulture(it, resourceProvider, culture);
                }
            }
        }

        private void changeLanguageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (sender == polishToolStripMenuItem)
            {
                ChangeCulture(new CultureInfo("pl-PL"));
            }
            else
            {
                ChangeCulture(new CultureInfo("en"));
            }
        }

        #endregion

        #region Wtyczki
        private void loadPluginsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _Plugins = new Dictionary<string, IPlugin>();
            ICollection<IPlugin> plugins = PluginModel.LoadPlugins("Plugins");
            foreach (var item in plugins)
            {
                _Plugins.Add(item.Name, item);

                ToolStripMenuItem m = sender as ToolStripMenuItem;
                m.Text = item.Name;
                m.Click += m_Click;
                pulginsToolStripMenuItem.DropDownItems.Add(m);
            }
        }

        private void m_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem m = sender as ToolStripMenuItem;
            if (m != null)
            {
                ActivePlugin = m.Text.ToString();

                if (_Plugins.ContainsKey(ActivePlugin))
                {
                    IPlugin plugin = _Plugins[ActivePlugin];
                    Image pic = pictureBox.Image;
                    Bitmap bmp = new Bitmap(pic);
                    pictureBox.Image = plugin.Work(bmp);
                    pictureBox.Refresh();
                }

                //if (bw.IsBusy != true)
                //{
                //    bw.RunWorkerAsync();
                //}
            }
        }
        #endregion

        #region Backgroundworker
        private void bw_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;

            //if (_Plugins.ContainsKey(ActivePlugin))
            //{
            //    IPlugin plugin = _Plugins[ActivePlugin];
            //    Image pic = pictureBox.Image;
            //    Bitmap bmp = new Bitmap(pic);
            //    pictureBox.Image = plugin.Work(bmp);
            //    pictureBox.Refresh();
            //}

            worker.ReportProgress((100));
        }
        private void bw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            CultureInfo culture = Thread.CurrentThread.CurrentUICulture;
            if ((e.Cancelled == true))
            {
                if (culture.Name == "pl-PL")
                {
                    this.tProgressBar.Text = "Anulowane!";
                }
                else
                {
                    this.tProgressBar.Text = "Canceled!";
                }
            }

            else if (!(e.Error == null))
            {
                if (culture.Name == "pl-PL")
                {
                    this.tProgressBar.Text = ("Błąd: " + e.Error.Message);
                }
                else
                {
                    this.tProgressBar.Text = ("Error: " + e.Error.Message);
                }
            }

            else
            {
                if (culture.Name == "pl-PL")
                {
                    this.tProgressBar.Text = "Zakończone!";
                }
                else
                {
                    this.tProgressBar.Text = "Done!";
                }
            }
        }
        private void bw_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            this.tProgressBar.Text = (e.ProgressPercentage.ToString() + "%");
            this.tProgressBar.Value = e.ProgressPercentage;
        }
        #endregion
    }
}
