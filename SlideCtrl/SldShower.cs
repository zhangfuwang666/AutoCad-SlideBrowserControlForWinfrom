using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace SlideCtrl
{
    public partial class SldShower: UserControl
    {
        private const string _FileType = @"AutoCAD Slide";
        private string _file = "";
        private long _filelen = 0L;
        private byte[] _sld = null;
        private SizeF _size = SizeF.Empty;
        private bool _leftlow = false;
        private bool isSuccess = false;
        private Dictionary<string, object> _info = new Dictionary<string, object>();
        private Image _img = null;
        private string _lasterror = "";
		public Size IMGSize
        {
            get
            {
				if (_img != null)
					return _img.Size;
				else
					return new Size(300, 200);
            }
        }
		public event Action<string, Image> OnChangedImage;
		public long ObjCount
        {
			get;set;
        }
		public string LastErrorString
        {
            get
            {
				return _lasterror;
            }
        }
		public bool State
        {
            get
            {
				return isSuccess;
            }
        }
		public List<KeyValuePair<string,object>> Information
        {
            get
            {
				List<KeyValuePair<string, object>> ls = _info.ToList();
				return ls;
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
				_info.Clear();
                if(value!=null && value!=_file && value.ToLower().EndsWith(".sld"))
                {
                    //设置了新的图形了
                    _file = value;
                    if (LoadFile(_file))
                    {
                        DrawImage();
                        this.BackgroundImage = _img;
                    }
                }
            }
        
        }

        private void DrawImage()
        {
            Bitmap bit = new Bitmap((int)_size.Width, (int)_size.Height);
            Graphics g = Graphics.FromImage(bit);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
			DrawAll(g);
			bit.RotateFlip(RotateFlipType.Rotate180FlipX);
			g.Dispose();
            _img = bit;
			OnChangedImage?.Invoke(_file, _img);
        }
        private byte HighByte(ushort val)
        {
            return ((byte)(val >> 8));
        }
		public float LineSize
		{
			get; set;
		} = 2f;
        private byte LowByte(ushort val)
        {
            return ((byte)(val & 0xff));
        }
		private ushort ReadPoint(byte[] sld,long index, bool b)
		{
			ushort value = Read2Bytes(sld,index, b);
			if (value > 32767) //----- For a bug in MSLIDE which returns the negative value
				value = (ushort)(65535 - value + 1);
			return (value);
		}
		private void DrawAll(Graphics g, bool bw = false)
		{
			if (_sld == null || _sld.Length == 0)
				return;
			ObjCount = 0;
			Brush brush = null;
			Point pt1 = new Point(0, 0), pt2 = new Point(0, 0);
			long j, i = int.Parse(_info["Version"].ToString()) == 1 ? 34 : 31;
			for (; i < _filelen;)
			{
				//----- Read Field Start
				ushort val = Read2Bytes(_sld, i, _leftlow);
				switch (HighByte(val))
				{
					case 0xff: //----- Color Change
						if (!bw)
						{
							if ((j = LowByte(val)) > acadColor.Count()
								|| (~(acadColor[0].R) == acadColor[j].R
									&& ~(acadColor[0].G) == acadColor[j].G
									&& ~(acadColor[0].B) == acadColor[j].B)
							)
								j = 0;
							brush = new SolidBrush(acadColor[j]);
						}
						else
						{
							SolidBrush bck = new SolidBrush(Color.Transparent);
							brush = new SolidBrush(Color.FromArgb((byte)(bck.Color.R ^ 0xff), (byte)(bck.Color.G ^ 0xff), (byte)(bck.Color.B ^ 0xff)));
						}
						i += 2;
						break;
					case 0xfe: //----- Common Endpoint Vector
						pt2 = pt1;
						pt1.X += (sbyte)LowByte(val);
						pt1.Y += (sbyte)_sld[i + 2];
						i += 3;
						//----- Draw
						g.DrawLine(new Pen(brush, LineSize), pt1, pt2);
						ObjCount += 1;
						break;
					case 0xfd: //----- Solid Fill
						j = Read2Bytes(_sld, i + 2, _leftlow);
						i += 6;
						if (j == 0)
							break;
						List<PointF> pts = new List<PointF>();
						for (long k = 0; k < j; k++, i += 6)
							pts.Add(new Point(ReadPoint(_sld, i + 2, _leftlow), ReadPoint(_sld, i + 4, _leftlow)));
						//----- Draw
						g.FillPolygon(brush, pts.ToArray());
						ObjCount += 1;
						break;
					case 0xfc: //----- End of File
						i += 2;
						return;
					case 0xfb: //----- Offset Vector
						pt2 = pt1;
						pt1.X += (sbyte)LowByte(val);
						pt1.Y += (sbyte)(_sld[i + 2]);
						pt2.X += (sbyte)(_sld[i + 3]);
						pt2.Y += (sbyte)(_sld[i + 4]);
						i += 5;
						//----- Draw
						g.DrawLine(new Pen(brush, LineSize), pt1, pt2);
						ObjCount += 1;
						break;
					default:
						if (HighByte(val) > 0x7f) //----- Undefined
							return;
						//----- Vector
						pt1.X = val;
						pt1.Y = ReadPoint(_sld, i + 2, _leftlow);
						pt2.X = ReadPoint(_sld, i + 4, _leftlow);
						pt2.Y = ReadPoint(_sld, i + 6, _leftlow);
						i += 8;
						//----- Draw
						g.DrawLine(new Pen(brush, LineSize), pt1, pt2);
						ObjCount += 1;
						break;
				}
			}
			_info.Add("ObjectCount", ObjCount);
		}
		public byte[] Data
        {
            get
            {
				return _sld;
            }
            set
            {
				if (value != null && value.Length > 0)
					if(LoadData(value))
                    {
						DrawImage();
						this.BackgroundImage = _img;
					}

            }
        }
		public string SLDName
        {
            get
            {
				return FileName;
            }
            set
            {
				_file = value;
            }
        }

        private bool LoadData(byte[] value)
        {
			isSuccess = false;
			_lasterror = "";
			_file = "";
			_sld = value;
			_filelen = value.Length;
			_info.Clear();
			_info.Add("FileName", "DATA");
			_info.Add("FileLength", _filelen);
			_info.Add("FullPath", "DATA");
			if (!IsSLD())
			{
				_lasterror = "不是SLD文件";
				return isSuccess;
			}
			else
				isSuccess = true;
			return isSuccess;
		}

        private bool LoadFile(string file)
        {
            isSuccess = false;
            _lasterror = "";
            try
            {
                _sld = System.IO.File.ReadAllBytes(file);
                _filelen = _sld.LongLength;
            }
            catch(Exception ex)
            {
                _lasterror = ex.Message;
                return isSuccess;
            }
            _info.Add("FileName",System.IO.Path.GetFileName(_file));
            _info.Add("FileLength", _filelen);
            _info.Add("FullPath", _file);
            if (!IsSLD())
            {
                _lasterror = "不是SLD文件";
                return isSuccess;
            }
			isSuccess = true;
            return isSuccess;
        }
        /// <summary>
        /// 读取两个字节
        /// </summary>
        /// <param name="data">数据数组</param>
        /// <param name="index">开始</param>
        /// <param name="b">是否低位在前</param>
        /// <returns></returns>
        private ushort Read2Bytes(byte[] data,long index, bool b)
        {
            if (b)
            {
                ushort hi = (ushort)(data[index + 1] << 8);
                ushort lo = (ushort)(data[index]);
                return ((ushort)(lo + hi));
            }
            ushort hi2 = (ushort)(data[index] << 8);
            ushort lo2 = (ushort)(data[index + 1]);
            return ((ushort)(lo2 + hi2));
        }
        private bool IsSLD()
        {
            string ft = System.Text.Encoding.Default.GetString(_sld, 0, _FileType.Length);
            if (ft != _FileType)
                return false;
            _info.Add("FileType", _FileType);
            ulong i = 0L;
            //----- Get version number, and byte order
            switch (_sld[18])
            {
                case 0x01: //----- Old Format
                           //----- We have to test the low order byte
                    i = Read2Bytes(_sld,_filelen- 2, true);
                    if (i == 0xfc00)
                    {
                        _leftlow = true;
                    }
                    else if (i == 0x00fc)
                    {
                        _leftlow = false;
                    }
                    else
                    {
                        return (false);
                    }
                    _info.Add("FirstLow", _leftlow);
                    _info.Add("Version", 1);
                    break;
                case 0x02: //----- Should be equal to 2, since r9
                    _leftlow =Read2Bytes(_sld,29, true) == 0x1234;
                    _info.Add("FirstLow", _leftlow);
                    _info.Add("Version", 2);
                    break;
                default:
                    return false;
            }
            _size = GetSize(_sld);// new SizeF(Read2Bytes(_sld,19, _leftlow), Read2Bytes(_sld,21, _leftlow));
			_info.Add("Size", _size.ToString());
            return true;
        }
        /// <summary>
        /// 取得尺寸
        /// </summary>
        /// <param name="sld">数据数组</param>
        /// <returns></returns>
        private SizeF GetSize(byte[] sld)
        {
           return  new SizeF(Read2Bytes(sld, 19, _leftlow), Read2Bytes(sld, 21, _leftlow));
        }

        public SldShower()
        {
            InitializeComponent();
        }
		public readonly Color[] acadColor = new Color[] {
			Color.FromArgb (255, 255, 255),	//----- 0 - ByBlock - White
			Color.FromArgb (255, 0, 0),		//----- 1 - Red 
			Color.FromArgb (255, 255, 0),	//----- 2 - Yellow
			Color.FromArgb (0, 255, 0),		//----- 3 - Green
			Color.FromArgb (0, 255, 255),	//----- 4 - Cyan
			Color.FromArgb (0, 0, 255),		//----- 5 - Blue
			Color.FromArgb (255, 0, 255),	//----- 6 - Magenta
			Color.FromArgb (255, 255, 255),	//----- 7 - White

			Color.FromArgb (128, 128, 128),	//----- 8 - Dark Gray
			Color.FromArgb (192, 192, 192),	//----- 9 - Light Gray
		
			Color.FromArgb (255, 0, 0),		//----- 10
			Color.FromArgb (255, 127, 127),	//----- 11
			Color.FromArgb (165, 0, 0),		//----- 12
			Color.FromArgb (165, 82, 82),	//----- 13
			Color.FromArgb (127, 0, 0),		//----- 14
			Color.FromArgb (127, 63, 63),	//----- 15
			Color.FromArgb (76, 0, 0),		//----- 16
			Color.FromArgb (76, 38, 38),		//----- 17
			Color.FromArgb (38, 0, 0),		//----- 18
			Color.FromArgb (38, 19, 19),		//----- 19
			Color.FromArgb (255, 63, 0),		//----- 20
			Color.FromArgb (255, 159, 127),	//----- 21
			Color.FromArgb (165, 41, 0),		//----- 22
			Color.FromArgb (165, 103, 82),	//----- 23
			Color.FromArgb (127, 31, 0),		//----- 24
			Color.FromArgb (127, 79, 63),	//----- 25
			Color.FromArgb (76, 19, 0),		//----- 26
			Color.FromArgb (76, 47, 38),		//----- 27
			Color.FromArgb (38, 9, 0),		//----- 28
			Color.FromArgb (38, 23, 19),		//----- 29
			Color.FromArgb (255, 127, 0),	//----- 30
			Color.FromArgb (255, 191, 127),	//----- 31
			Color.FromArgb (165, 82, 0),		//----- 32
			Color.FromArgb (165, 124, 82),	//----- 33
			Color.FromArgb (127, 63, 0),		//----- 34
			Color.FromArgb (127, 95, 63),	//----- 35
			Color.FromArgb (76, 38, 0),		//----- 36
			Color.FromArgb (76, 57, 38),		//----- 37
			Color.FromArgb (38, 19, 0),		//----- 38
			Color.FromArgb (38, 28, 19),		//----- 39
			Color.FromArgb (255, 191, 0),	//----- 40
			Color.FromArgb (255, 223, 127),	//----- 41
			Color.FromArgb (165, 124, 0),	//----- 42
			Color.FromArgb (165, 145, 82),	//----- 43
			Color.FromArgb (127, 95, 0),		//----- 44
			Color.FromArgb (127, 111, 63),	//----- 45
			Color.FromArgb (76, 57, 0),		//----- 46
			Color.FromArgb (76, 66, 38),		//----- 47
			Color.FromArgb (38, 28, 0),		//----- 48
			Color.FromArgb (38, 33, 19),		//----- 49
			Color.FromArgb (255, 255, 0),	//----- 50
			Color.FromArgb (255, 255, 127),	//----- 51
			Color.FromArgb (165, 165, 0),	//----- 52
			Color.FromArgb (165, 165, 82),	//----- 53
			Color.FromArgb (127, 127, 0),	//----- 54
			Color.FromArgb (127, 127, 63),	//----- 55
			Color.FromArgb (76, 76, 0),		//----- 56
			Color.FromArgb (76, 76, 38),		//----- 57
			Color.FromArgb (38, 38, 0),		//----- 58
			Color.FromArgb (38, 38, 19),		//----- 59
			Color.FromArgb (191, 255, 0),	//----- 60
			Color.FromArgb (223, 255, 127),	//----- 61
			Color.FromArgb (124, 165, 0),	//----- 62
			Color.FromArgb (145, 165, 82),	//----- 63
			Color.FromArgb (95, 127, 0),		//----- 64
			Color.FromArgb (111, 127, 63),	//----- 65
			Color.FromArgb (57, 76, 0),		//----- 66
			Color.FromArgb (66, 76, 38),		//----- 67
			Color.FromArgb (28, 38, 0),		//----- 68
			Color.FromArgb (33, 38, 19),		//----- 69
			Color.FromArgb (127, 255, 0),	//----- 70
			Color.FromArgb (191, 255, 127),	//----- 71
			Color.FromArgb (82, 165, 0),		//----- 72
			Color.FromArgb (124, 165, 82),	//----- 73
			Color.FromArgb (63, 127, 0),		//----- 74
			Color.FromArgb (95, 127, 63),	//----- 75
			Color.FromArgb (38, 76, 0),		//----- 76
			Color.FromArgb (57, 76, 38),		//----- 77
			Color.FromArgb (19, 38, 0),		//----- 78
			Color.FromArgb (28, 38, 19),		//----- 79
			Color.FromArgb (63, 255, 0),		//----- 80
			Color.FromArgb (159, 255, 127),	//----- 81
			Color.FromArgb (41, 165, 0),		//----- 82
			Color.FromArgb (103, 165, 82),	//----- 83
			Color.FromArgb (31, 127, 0),		//----- 84
			Color.FromArgb (79, 127, 63),	//----- 85
			Color.FromArgb (19, 76, 0),		//----- 86
			Color.FromArgb (47, 76, 38),		//----- 87
			Color.FromArgb (9, 38, 0),		//----- 88
			Color.FromArgb (23, 38, 19),		//----- 89
			Color.FromArgb (0, 255, 0),		//----- 90
			Color.FromArgb (127, 255, 127),	//----- 91
			Color.FromArgb (0, 165, 0),		//----- 92
			Color.FromArgb (82, 165, 82),	//----- 93
			Color.FromArgb (0, 127, 0),		//----- 94
			Color.FromArgb (63, 127, 63),	//----- 95
			Color.FromArgb (0, 76, 0),		//----- 96
			Color.FromArgb (38, 76, 38),		//----- 97
			Color.FromArgb (0, 38, 0),		//----- 98
			Color.FromArgb (19, 38, 19),		//----- 99
			Color.FromArgb (0, 255, 63),		//----- 100
			Color.FromArgb (127, 255, 159),	//----- 101
			Color.FromArgb (0, 165, 41),		//----- 102
			Color.FromArgb (82, 165, 103),	//----- 103
			Color.FromArgb (0, 127, 31),		//----- 104
			Color.FromArgb (63, 127, 79),	//----- 105
			Color.FromArgb (0, 76, 19),		//----- 106
			Color.FromArgb (38, 76, 47),		//----- 107
			Color.FromArgb (0, 38, 9),		//----- 108
			Color.FromArgb (19, 38, 23),		//----- 109
			Color.FromArgb (0, 255, 127),	//----- 110
			Color.FromArgb (127, 255, 191),	//----- 111
			Color.FromArgb (0, 165, 82),		//----- 112
			Color.FromArgb (82, 165, 124),	//----- 113
			Color.FromArgb (0, 127, 63),		//----- 114
			Color.FromArgb (63, 127, 95),	//----- 115
			Color.FromArgb (0, 76, 38),		//----- 116
			Color.FromArgb (38, 76, 57),		//----- 117
			Color.FromArgb (0, 38, 19),		//----- 118
			Color.FromArgb (19, 38, 28),		//----- 119
			Color.FromArgb (0, 255, 191),	//----- 120
			Color.FromArgb (127, 255, 223),	//----- 121
			Color.FromArgb (0, 165, 124),	//----- 122
			Color.FromArgb (82, 165, 145),	//----- 123
			Color.FromArgb (0, 127, 95),		//----- 124
			Color.FromArgb (63, 127, 111),	//----- 125
			Color.FromArgb (0, 76, 57),		//----- 126
			Color.FromArgb (38, 76, 66),		//----- 127
			Color.FromArgb (0, 38, 28),		//----- 128
			Color.FromArgb (19, 38, 33),		//----- 129
			Color.FromArgb (0, 255, 255),	//----- 130
			Color.FromArgb (127, 255, 255),	//----- 131
			Color.FromArgb (0, 165, 165),	//----- 132
			Color.FromArgb (82, 165, 165),	//----- 133
			Color.FromArgb (0, 127, 127),	//----- 134
			Color.FromArgb (63, 127, 127),	//----- 135
			Color.FromArgb (0, 76, 76),		//----- 136
			Color.FromArgb (38, 76, 76),		//----- 137
			Color.FromArgb (0, 38, 38),		//----- 138
			Color.FromArgb (19, 38, 38),		//----- 139
			Color.FromArgb (0, 191, 255),	//----- 140
			Color.FromArgb (127, 223, 255),	//----- 141
			Color.FromArgb (0, 124, 165),	//----- 142
			Color.FromArgb (82, 145, 165),	//----- 143
			Color.FromArgb (0, 95, 127),		//----- 144
			Color.FromArgb (63, 111, 127),	//----- 145
			Color.FromArgb (0, 57, 76),		//----- 146
			Color.FromArgb (38, 66, 76),		//----- 147
			Color.FromArgb (0, 28, 38),		//----- 148
			Color.FromArgb (19, 33, 38),		//----- 149
			Color.FromArgb (0, 127, 255),	//----- 150
			Color.FromArgb (127, 191, 255),	//----- 151
			Color.FromArgb (0, 82, 165),		//----- 152
			Color.FromArgb (82, 124, 165),	//----- 153
			Color.FromArgb (0, 63, 127),		//----- 154
			Color.FromArgb (63, 95, 127),	//----- 155
			Color.FromArgb (0, 38, 76),		//----- 156
			Color.FromArgb (38, 57, 76),		//----- 157
			Color.FromArgb (0, 19, 38),		//----- 158
			Color.FromArgb (19, 28, 38),		//----- 159
			Color.FromArgb (0, 63, 255),		//----- 160
			Color.FromArgb (127, 159, 255),	//----- 161
			Color.FromArgb (0, 41, 165),		//----- 162
			Color.FromArgb (82, 103, 165),	//----- 163
			Color.FromArgb (0, 31, 127),		//----- 164
			Color.FromArgb (63, 79, 127),	//----- 165
			Color.FromArgb (0, 19, 76),		//----- 166
			Color.FromArgb (38, 47, 76),		//----- 167
			Color.FromArgb (0, 9, 38),		//----- 168
			Color.FromArgb (19, 23, 38),		//----- 169
			Color.FromArgb (0, 0, 255),		//----- 170
			Color.FromArgb (127, 127, 255),	//----- 171
			Color.FromArgb (0, 0, 165),		//----- 172
			Color.FromArgb (82, 82, 165),	//----- 173
			Color.FromArgb (0, 0, 127),		//----- 174
			Color.FromArgb (63, 63, 127),	//----- 175
			Color.FromArgb (0, 0, 76),		//----- 176
			Color.FromArgb (38, 38, 76),		//----- 177
			Color.FromArgb (0, 0, 38),		//----- 178
			Color.FromArgb (19, 19, 38),		//----- 179
			Color.FromArgb (63, 0, 255),		//----- 180
			Color.FromArgb (159, 127, 255),	//----- 181
			Color.FromArgb (41, 0, 165),		//----- 182
			Color.FromArgb (103, 82, 165),	//----- 183
			Color.FromArgb (31, 0, 127),		//----- 184
			Color.FromArgb (79, 63, 127),	//----- 185
			Color.FromArgb (19, 0, 76),		//----- 186
			Color.FromArgb (47, 38, 76),		//----- 187
			Color.FromArgb (9, 0, 38),		//----- 188
			Color.FromArgb (23, 19, 38),		//----- 189
			Color.FromArgb (127, 0, 255),	//----- 190
			Color.FromArgb (191, 127, 255),	//----- 191
			Color.FromArgb (82, 0, 165),		//----- 192
			Color.FromArgb (124, 82, 165),	//----- 193
			Color.FromArgb (63, 0, 127),		//----- 194
			Color.FromArgb (95, 63, 127),	//----- 195
			Color.FromArgb (38, 0, 76),		//----- 196
			Color.FromArgb (57, 38, 76),		//----- 197
			Color.FromArgb (19, 0, 38),		//----- 198
			Color.FromArgb (28, 19, 38),		//----- 199
			Color.FromArgb (191, 0, 255),	//----- 200
			Color.FromArgb (223, 127, 255),	//----- 201
			Color.FromArgb (124, 0, 165),	//----- 202
			Color.FromArgb (145, 82, 165),	//----- 203
			Color.FromArgb (95, 0, 127),		//----- 204
			Color.FromArgb (111, 63, 127),	//----- 205
			Color.FromArgb (57, 0, 76),		//----- 206
			Color.FromArgb (66, 38, 76),		//----- 207
			Color.FromArgb (28, 0, 38),		//----- 208
			Color.FromArgb (33, 19, 38),		//----- 209
			Color.FromArgb (255, 0, 255),	//----- 210
			Color.FromArgb (255, 127, 255),	//----- 211
			Color.FromArgb (165, 0, 165),	//----- 212
			Color.FromArgb (165, 82, 165),	//----- 213
			Color.FromArgb (127, 0, 127),	//----- 214
			Color.FromArgb (127, 63, 127),	//----- 215
			Color.FromArgb (76, 0, 76),		//----- 216
			Color.FromArgb (76, 38, 76),		//----- 217
			Color.FromArgb (38, 0, 38),		//----- 218
			Color.FromArgb (38, 19, 38),		//----- 219
			Color.FromArgb (255, 0, 191),	//----- 220
			Color.FromArgb (255, 127, 223),	//----- 221
			Color.FromArgb (165, 0, 124),	//----- 222
			Color.FromArgb (165, 82, 145),	//----- 223
			Color.FromArgb (127, 0, 95),		//----- 224
			Color.FromArgb (127, 63, 111),	//----- 225
			Color.FromArgb (76, 0, 57),		//----- 226
			Color.FromArgb (76, 38, 66),		//----- 227
			Color.FromArgb (38, 0, 28),		//----- 228
			Color.FromArgb (38, 19, 33),		//----- 229
			Color.FromArgb (255, 0, 127),	//----- 230
			Color.FromArgb (255, 127, 191),	//----- 231
			Color.FromArgb (165, 0, 82),		//----- 232
			Color.FromArgb (165, 82, 124),	//----- 233
			Color.FromArgb (127, 0, 63),		//----- 234
			Color.FromArgb (127, 63, 95),	//----- 235
			Color.FromArgb (76, 0, 38),		//----- 236
			Color.FromArgb (76, 38, 57),		//----- 237
			Color.FromArgb (38, 0, 19),		//----- 238
			Color.FromArgb (38, 19, 28),		//----- 239
			Color.FromArgb (255, 0, 63),		//----- 240
			Color.FromArgb (255, 127, 159),	//----- 241
			Color.FromArgb (165, 0, 41),		//----- 242
			Color.FromArgb (165, 82, 103),	//----- 243
			Color.FromArgb (127, 0, 31),		//----- 244
			Color.FromArgb (127, 63, 79),	//----- 245
			Color.FromArgb (76, 0, 19),		//----- 246
			Color.FromArgb (76, 38, 47),		//----- 247
			Color.FromArgb (38, 0, 9),		//----- 248
			Color.FromArgb (38, 19, 23),		//----- 249

			Color.FromArgb (84, 84, 84),		//----- 250 - Gray Shades
			Color.FromArgb (118, 118, 118),	//----- 251
			Color.FromArgb (152, 152, 152),	//----- 252
			Color.FromArgb (186, 186, 186),	//----- 253
			Color.FromArgb (220, 220, 220),	//----- 254
			Color.FromArgb (255, 255, 255),	//----- 255
		
			Color.FromArgb (255, 255, 255)	//----- ByLayer - White
		};
	}
}
