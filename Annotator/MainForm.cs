﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

namespace Annotator
{
    public enum RectAction
    {
        NoAction,
        Selecting,
        Editing
    };

    public partial class MainForm : Form
    {
        #region Private Variables

        private string _imageFolder = null;
        private int _currenIndex = -1;

        private List<string> _extentions;
        private List<string> _files;

        private AnnotationList _annList;
        private List<BRectangle> _currentRectangles;

        private Image _drawnImage;
        private Image _loadedImage;

        private float _ratio;
        private float _xoffset;
        private float _yoffset;

        private int _mouseX;
        private int _mouseY;
        private RectAction _currentAction;

        #endregion Private Variables

        #region Constructor

        public MainForm()
        {
            _extentions = GetImageFileExtensions();
            InitializeComponent();
            if (Directory.Exists(Properties.Settings.Default.ImageFolder))
            {
                ImageFolder = Properties.Settings.Default.ImageFolder;
            }
            else
            {
                ImageFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            }
            CurrentAction = RectAction.NoAction;
        }

        #endregion Constructor

        #region Form properties

        public string ImageFolder
        {
            get { return _imageFolder; }
            set
            {
                _imageFolder = value;
                if (!String.IsNullOrWhiteSpace(_imageFolder) && Directory.Exists(_imageFolder))
                {
                    LoadFileList();
                    LoadAnnotations();
                    CurrenIndex = 0;
                }
                Properties.Settings.Default.ImageFolder = _imageFolder;
                txtImageFolder.Text = _imageFolder;
            }
        }

        public int CurrenIndex
        {
            get
            {
                return _currenIndex;
            }

            set
            {
                if (value >= 0 && value != _currenIndex)
                {
                    SaveImageRectangles(_currenIndex);
                    if (_currentRectangles != null)
                        _currentRectangles.Clear();
                    _currenIndex = value;
                    LoadFile(_currenIndex);
                    LoadImageRectangles(_currenIndex);
                }
                picBox.Refresh();
            }
        }

        public int MouseX
        {
            get
            {
                return _mouseX;
            }

            set
            {
                _mouseX = value;
                lblMouseX.Text = _mouseX.ToString();
            }
        }

        public int MouseY
        {
            get
            {
                return _mouseY;
            }

            set
            {
                _mouseY = value;
                lblMouseY.Text = _mouseY.ToString();
            }
        }

        public RectAction CurrentAction
        {
            get
            {
                return _currentAction;
            }

            set
            {
                _currentAction = value;
            }
        }

        #endregion Form properties

        #region Save and Load

        private void LoadFileList()
        {
            if (_files == null)
                _files = new List<string>();

            _files.Clear();

            if (!string.IsNullOrWhiteSpace(ImageFolder) && System.IO.Directory.Exists(ImageFolder))
            {
                string[] f = System.IO.Directory.GetFiles(ImageFolder);

                foreach (string file in f)
                {
                    string ext = Path.GetExtension(file).ToLowerInvariant();

                    if (_extentions.Contains(ext))
                    {
                        _files.Add(Path.GetFileName(file));
                    }
                }
                _files.Sort();
            }
        }

        private void LoadAnnotations()
        {
            if (Directory.Exists(ImageFolder))
            {
                string annXml = Path.Combine(ImageFolder, "Annotations.xml");
                if (File.Exists(annXml))
                {
                    _annList = AnnotationList.FromFile(annXml);
                }
                else
                {
                    _annList = new AnnotationList();
                }
                if (_files != null)
                {
                    foreach (string file in _files)
                    {
                        if (!_annList.ContainsKey(file))
                        {
                            _annList.Add(file);
                        }
                    }
                }
            }
        }

        private void SaveAnnotations(string fileName)
        {
            SaveImageRectangles(CurrenIndex);
            if (Directory.Exists(ImageFolder))
            {
                if (_annList == null)
                    _annList = new AnnotationList();
                _annList.Save(fileName);
            }
        }

        private void SaveImageRectangles(int index)
        {
            if (_files != null && index >= 0 && index < _files.Count)
            {
                string file = _files[index];
                if (_annList != null)
                    _annList.CheckInRectangles(file, _currentRectangles, _xoffset, _yoffset, _ratio);
            }
        }

        private void LoadImageRectangles(int index)
        {
            if (_files != null && index >= 0 && index < _files.Count)
            {
                string file = _files[index];
                if (_annList != null)
                    _currentRectangles = _annList.CheckoutRectangles(file, _xoffset, _yoffset, _ratio);
            }
        }

        private void LoadFile(int index)
        {
            if (_files != null && _files.Count > 0)
            {
                string fullPath = System.IO.Path.Combine(ImageFolder, _files[index]);
                if (System.IO.File.Exists(fullPath))
                {
                    _loadedImage = Image.FromFile(fullPath);
                    ShowLoadedImage();
                    picBox.Refresh();
                }
            }
        }

        private static List<string> GetImageFileExtensions()
        {
            ImageCodecInfo[] encoders = ImageCodecInfo.GetImageEncoders();

            List<string> extensions = new List<string>();

            foreach (var enc in encoders)
            {
                extensions.AddRange(enc.FilenameExtension.ToLowerInvariant().Replace("*", "").Split(';'));
            }
            return extensions;
        }

        private void ShowLoadedImage()
        {
            int maxWidth = picBox.Width;
            int maxHeight = picBox.Height;

            float ratioX = ((float)maxWidth) / ((float)_loadedImage.Width);
            float ratioY = ((float)maxHeight) / ((float)_loadedImage.Height);
            _ratio = Math.Min(ratioX, ratioY);

            int newWidth = Convert.ToInt32(_loadedImage.Width * _ratio);
            int newHeight = Convert.ToInt32(_loadedImage.Height * _ratio);

            _xoffset = ((float)(maxWidth - newWidth)) / 2.0f;
            _yoffset = ((float)(maxHeight - newHeight)) / 2.0f;

            if (_drawnImage != null)
            {
                if (_drawnImage.Width != newWidth || _drawnImage.Height != newHeight)
                {
                    _drawnImage.Dispose();
                    _drawnImage = new Bitmap(newWidth, newHeight);
                }
            }
            else
            {
                _drawnImage = new Bitmap(newWidth, newHeight);
            }

            using (var graphics = Graphics.FromImage(_drawnImage))
            {
                graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                graphics.DrawImage(_loadedImage, 0, 0, newWidth, newHeight);
            }
        }

        private void ExportAnnotations(string fileName)
        {
            TextWriter tw = new StreamWriter(fileName);
            string format = Properties.Settings.Default.RectangleLineFormat;
            foreach (var ann in _annList)
            {
                tw.WriteLine(ann.Key);
                tw.WriteLine(ann.Value.Count);

                foreach (var rec in ann.Value)
                    tw.WriteLine(string.Format(format, rec.X, rec.Y, rec.Width, rec.Height));
            }
            tw.Close();
        }

        private void DeleteActive()
        {
            if (_currentRectangles != null && _aRect != null)
            {
                _currentRectangles.Remove(_aRect);
                SaveImageRectangles(CurrenIndex);
            }
            picBox.Refresh();
        }

        #endregion Save and Load

        #region Form Events

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveAnnotations(Path.Combine(ImageFolder, "Annotations.xml"));
            Properties.Settings.Default.Save();
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            if (FBD.ShowDialog() == DialogResult.OK)
            {
                ImageFolder = FBD.SelectedPath;
            }
        }

        private void btnNext_Click(object sender, EventArgs e)
        {
            if (CurrenIndex < _files.Count - 1)
                CurrenIndex++;
        }

        private void btnPrevious_Click(object sender, EventArgs e)
        {
            if (CurrenIndex > 0)
                CurrenIndex--;
        }

        private void picBox_Paint(object sender, PaintEventArgs e)
        {
            if (_drawnImage != null)
            {
                e.Graphics.DrawImage(_drawnImage, new PointF(_xoffset, _yoffset));
            }
            if (_currentRectangles != null)
            {
                Color drawColor = Properties.Settings.Default.RectangleColor;

                foreach (BRectangle r in _currentRectangles)
                {
                    if (r == _aRect)
                    {
                        r.Draw(e.Graphics, drawColor, true);
                    }
                    else
                    {
                        r.Draw(e.Graphics, drawColor, false);
                    }
                }
            }
        }

        private void picBox_Resize(object sender, EventArgs e)
        {
            SaveImageRectangles(CurrenIndex);
            ShowLoadedImage();
            LoadImageRectangles(CurrenIndex);
            picBox.Refresh();
        }

        private void tsExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void tsSave_Click(object sender, EventArgs e)
        {
            SaveAnnotations(Path.Combine(ImageFolder, "Annotations.xml"));
        }

        private void tsSaveAs_Click(object sender, EventArgs e)
        {
            SFD.Filter = "XML files (*.xml)|*.xml|All files (*.*)|*.*";
            SFD.FileName = Path.Combine(ImageFolder, "Annotations.xml");
            if (SFD.ShowDialog() == DialogResult.OK)
            {
                SaveAnnotations(SFD.FileName);
            }
        }

        private void tsExport_Click(object sender, EventArgs e)
        {
            SFD.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
            SFD.InitialDirectory = ImageFolder;
            if (SFD.ShowDialog() == DialogResult.OK)
            {
                ExportAnnotations(SFD.FileName);
            }
        }

        private void tsOptions_Click(object sender, EventArgs e)
        {
            OptionsForm ofrm = new OptionsForm();
            ofrm.Show(this);
        }

        #endregion Form Events

        #region Mouse Editing

        private int _oldx;
        private int _oldy;
        private BRectangle _aRect = null;
        private AnchorType _curAn = AnchorType.None;

        private void GetActiveRectangle(int x, int y)
        {
            if (_currentRectangles == null)
            {
                _aRect = null;
            }
            else
            {
                BRectangle act = null;
                foreach (BRectangle r in _currentRectangles)
                {
                    if (r.Contains(x, y))
                    {
                        act = r;
                        break;
                    }
                }
                _aRect = act;
            }
        }

        private void ProcessEdit()
        {
            int dx = MouseX - _oldx;
            int dy = MouseY - _oldy;

            if (_aRect != null && CurrentAction == RectAction.Editing)
            {
                switch (_curAn)
                {
                    case AnchorType.None:
                        _aRect.X += dx;
                        _aRect.Y += dy;
                        break;

                    case AnchorType.BottomCenter:
                        _aRect.Height += dy;
                        break;

                    case AnchorType.BottomLeft:
                        _aRect.Height += dy;
                        _aRect.X += dx;
                        _aRect.Width += -dx;
                        break;

                    case AnchorType.BottomRight:
                        _aRect.Width += dx;
                        _aRect.Height += dy;
                        break;

                    case AnchorType.MiddleLeft:
                        _aRect.X += dx;
                        _aRect.Width += -dx;
                        break;

                    case AnchorType.MiddleRight:
                        _aRect.Width += dx;
                        break;

                    case AnchorType.TopCenter:
                        _aRect.Y += dy;
                        _aRect.Height -= dy;
                        break;

                    case AnchorType.TopLeft:
                        _aRect.X += dx;
                        _aRect.Width += -dx;
                        _aRect.Y += dy;
                        _aRect.Height += -dy;
                        break;

                    case AnchorType.TopRight:
                        _aRect.Width += dx;
                        _aRect.Y += dy;
                        _aRect.Height += -dy;
                        break;

                    default:
                        break;
                }
            }
        }

        private void CreateNewRect()
        {
            BRectangle rec = new BRectangle(MouseX - 20, MouseY - 20, 23, 23);
            _currentRectangles.Add(rec);
            _aRect = rec;
            _curAn = AnchorType.BottomRight;
            picBox.Refresh();
        }

        private void picBox_MouseMove(object sender, MouseEventArgs e)
        {
            MouseX = e.X;
            MouseY = e.Y;
            if (_aRect == null)
            {
                picBox.Cursor = Cursors.Default;
            }
            else
            {
                AnchorType a = _aRect.GetHitAnchor(MouseX, MouseY);
                picBox.Cursor = BRectangle.GetCursor(a);
            }
            switch (CurrentAction)
            {
                case RectAction.NoAction:
                    GetActiveRectangle(e.X, e.Y);
                    if (_aRect != null)
                        CurrentAction = RectAction.Selecting;
                    break;

                case RectAction.Selecting:
                    GetActiveRectangle(e.X, e.Y);
                    if (_aRect == null)
                        CurrentAction = RectAction.NoAction;
                    break;

                case RectAction.Editing:
                    if (_aRect != null)
                        ProcessEdit();
                    break;

                default:
                    break;
            }
            _oldx = e.X;
            _oldy = e.Y;
            picBox.Refresh();
        }

        private void picBox_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                switch (CurrentAction)
                {
                    case RectAction.NoAction:
                    case RectAction.Selecting:
                        CurrentAction = RectAction.Editing;
                        if (_aRect == null)
                        {
                            CreateNewRect();
                            _curAn = AnchorType.BottomRight;
                        }
                        else
                        {
                            _curAn = _aRect.GetHitAnchor(e.X, e.Y);
                        }
                        break;

                    case RectAction.Editing:
                        break;

                    default:
                        break;
                }
            }
        }

        private void picBox_MouseUp(object sender, MouseEventArgs e)
        {
            switch (CurrentAction)
            {
                case RectAction.NoAction:
                    break;

                case RectAction.Selecting:
                    break;

                case RectAction.Editing:
                    CurrentAction = RectAction.Selecting;
                    break;

                default:
                    break;
            }
        }

        #endregion Mouse Editing

        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (_aRect != null && e.KeyCode == Keys.Delete)
            {
                DeleteActive();
                e.Handled = true;
            }
        }
    }
}