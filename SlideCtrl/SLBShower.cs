using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace SlideCtrl
{
    public partial class SLBShower : UserControl
    {
        public event Action<SldShower, bool> OnDoubleClick;
        protected const int _hlen = 68;
        /// <summary>幻灯片库文件头标识符字符串</summary>
        protected const string _hstr = @"AutoCAD Slide Library 1.0";
        /// <summary>幻灯片库文件头标识符字符串的最大长度。</summary>
        protected const int _hsinglen = 32;
        /// <summary>幻灯片库条目头的长度(字符串+索引)。</summary>
        protected const int _hsslen = 36;

        private string _file;
        public Size ItemSize { get; set; } = new Size(100, 100);
        public Size SpanSize { get; set; } = new Size(5, 5);

        private Dictionary<string, byte[]> Data = new Dictionary<string, byte[]>();
        private string _lasterror = "";
        public string LastErrorString
        {
            get
            {
                return _lasterror;
            }
        }
        public string FileName
        {
            get
            {
                return _file;
            }
            set
            {
                if (value != null)
                {
                    _file = value;
                    foreach (SldShower sld in this.Controls)
                        sld.Dispose();
                    Data.Clear();
                    if (LoadData())
                    {

                        this.Controls.Clear();
                        DrawImage();
                    }
                }
            }
        }

        private void DrawImage()
        {
            int x = SpanSize.Width;
            int y = SpanSize.Height;
            foreach (string name in Data.Keys)
            {
                SldShower sl = new SldShower() { Parent = this, Size = ItemSize, Location = new Point(x, y),LineSize=this.LineSize, Data = Data[name], SLDName = name};
                x += ItemSize.Width + SpanSize.Width;
                if (x > this.Width-ItemSize.Width-SpanSize.Width)
                {
                    x = SpanSize.Width;
                    y += SpanSize.Height + ItemSize.Height;
                }
                sl.MouseEnter += new EventHandler((object sender, EventArgs e) => { sl.BorderStyle = BorderStyle.FixedSingle; });
                sl.MouseLeave+= new EventHandler((object sender, EventArgs e) => { sl.BorderStyle = BorderStyle.None; });
                sl.MouseDoubleClick+= new  MouseEventHandler((object sender,  MouseEventArgs e) => { OnDoubleClick?.Invoke(sl, true); });
            }

        }
        public float LineSize
        {
            get; set;
        } = 2f;
        private bool LoadData()
        {
            if (!_file.EndsWith(".slb"))
            {
                _lasterror = "Not A SLB File";
                return false; 
            }
            FileInfo f = new FileInfo(_file);
            if (f.Length < _hlen)
                return (false);
            byte[] content = File.ReadAllBytes(_file);
            // Read Header
            // Check if it is really an AutoCAD slide file
            string st = System.Text.Encoding.Default.GetString(content, 0, _hstr.Length);
            if (st != _hstr)
                return (false);
            // Load Slides
            List<int> indexes = new List<int>();
            for (int start = _hsinglen + _hsslen - 4; ; start += _hsslen)
            {
                int pos = BitConverter.ToInt32(content, start);
                if (pos == 0)
                    break;
                indexes.Add(pos);
            }
            indexes.Add((int)f.Length);
            indexes.RemoveAt(0);

            for (int start = _hsinglen; indexes.Count > 0; start += _hsslen, indexes.RemoveAt(0))
            {
                st = System.Text.Encoding.Default.GetString(content, start, _hsslen - 4);
                st = st.Replace("\0", "");
                int pos = BitConverter.ToInt32(content, start + _hsslen - 4);
                int sldLength = indexes[0] - pos, i = 1;
                while (sldLength == 0) // In case a library has 2 entries for the same slide
                    sldLength = indexes[i++] - pos;
                byte[] sldContent = new byte[sldLength];
                Buffer.BlockCopy(content, pos, sldContent, 0, sldLength);
                Data.Add(st, sldContent);
            }

            return (true);
        }

        public SLBShower()
        {
            InitializeComponent();
        }

        private void SLBShower_Resize(object sender, EventArgs e)
        {
            if (this.Width < 100)
                return;
            RefalshSLD();
        }

        private void RefalshSLD()
        {
            int x = SpanSize.Width;
            int y = SpanSize.Height;
            foreach (SldShower sl in this.Controls)
            {
                sl.Location = new Point(x, y) ;
                x += ItemSize.Width + SpanSize.Width;
                if (x > this.Width-ItemSize.Width-SpanSize.Width)
                {
                    x = SpanSize.Width;
                    y += SpanSize.Height + ItemSize.Height;
                }
            }
        }
    }
}
