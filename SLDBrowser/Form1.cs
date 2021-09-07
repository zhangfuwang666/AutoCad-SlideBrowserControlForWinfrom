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
    public partial class frmMain : Form
    {
        public frmMain()
        {
            InitializeComponent();
            sldShower1.MouseWheel += SldShower1_MouseWheel;
            slbShower1.OnDoubleClick += SlbShower1_OnDoubleClick;
        }

        private void SlbShower1_OnDoubleClick(SlideCtrl.SldShower sld, bool e)
        {
            frmItem f = new frmItem(sld.Name,sld.BackgroundImage);
            f.Show();
        }

        private void SldShower1_MouseWheel(object sender, MouseEventArgs e)
        {
            if (checkBox1.Checked)
                return;
           Size si = new Size(sldShower1.Size.Width + e.Delta, sldShower1.Height + e.Delta );
            if (si.Width < 100) si.Width = 100;
            if (si.Height < 100) si.Height = 100;
            sldShower1.ClientSize = si;

            sldShower1.Location = new Point((splitContainer2.Panel1.ClientSize.Width - sldShower1.Width) / 2,
                   (splitContainer2.Panel1.Height - sldShower1.Height) / 2);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog f = new OpenFileDialog() { Filter = "*.sld|*.sld;*.slb" };
            if (f.ShowDialog() == DialogResult.OK)
            {
                if (f.FileName.ToLower().EndsWith(".sld"))
                {
                    sldShower1.FileName = f.FileName;
                    dataGridView1.DataSource = sldShower1.Information;
                    sldShower1.Visible = true;
                    slbShower1.Visible = false;
                }
                else
                {
                    slbShower1.FileName = f.FileName;
                    splitContainer2.Panel2Collapsed = true;
                    sldShower1.Visible = false;
                    slbShower1.Visible = true;
                }
                this.Text = f.FileName;
            }
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            treeView1.Nodes.Clear();
            FolderBrowserDialog of = new FolderBrowserDialog();
            if (of.ShowDialog() == DialogResult.OK)
            {
                string[] files = System.IO.Directory.GetFiles(of.SelectedPath, "*.sld", System.IO.SearchOption.AllDirectories);
                foreach(string fi in files)
                {
                    TreeNode nod = new TreeNode(System.IO.Path.GetFileName(fi)) { Tag = fi };
                    treeView1.Nodes.Add(nod);
                }
                this.Text = of.SelectedPath;
            }
        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            string path = e.Node.Tag.ToString();
            sldShower1.FileName = path;
            sldShower1.Visible = true;
            slbShower1.Visible = false;
            splitContainer2.Panel2Collapsed = false;
            
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            SetSize(checkBox1.Checked);
            
        }

        private void sldShower1_OnChangedImage(string file, Image arg2)
        {
            dataGridView1.DataSource = sldShower1.Information;
            this.Text = file;
            SetSize(checkBox1.Checked);
        }

        private void SetSize(bool isAuto)
        {
            if (isAuto)
            {
                sldShower1.Dock = DockStyle.Fill;
                sldShower1.BackgroundImageLayout = ImageLayout.Zoom;
            }
            else
            {
                sldShower1.Dock = DockStyle.None;
                sldShower1.ClientSize = sldShower1.IMGSize;
                sldShower1.Location = new Point((splitContainer2.Panel1.ClientSize.Width - sldShower1.Width) / 2,
                    (splitContainer2.Panel1.Height - sldShower1.Height) / 2);
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void button3_Click(object sender, EventArgs e)
        {
            slbShower1.Visible = true;
            sldShower1.Visible = false;
            splitContainer2.Panel2Collapsed = true;
        }
    }
}
