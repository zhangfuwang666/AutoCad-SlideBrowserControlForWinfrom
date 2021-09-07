using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace SLDBrowser
{
    public partial class frmItem : Form
    {
        private Image img = null;
        public static Color back = Color.Black;
        public frmItem(string name,Image img)
        {
            InitializeComponent();
            this.BackgroundImage = img;
            this.img = img;
            this.Text = name;
        }

        private void frmItem_Load(object sender, EventArgs e)
        {
            this.BackColor = back;
            this.ClientSize = img != null ? img.Size : new Size(500, 300);
            this.Location = new Point(Screen.PrimaryScreen.Bounds.Width / 2 - this.Width / 2, Screen.PrimaryScreen.Bounds.Height / 2 - this.Height / 2);
        }

        private void frmItem_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            ColorDialog op = new ColorDialog() { Color = this.BackColor };
            if (op.ShowDialog() == DialogResult.OK)
            { this.BackColor = op.Color;
                back = op.Color;
            }
            
        }
    }
}
