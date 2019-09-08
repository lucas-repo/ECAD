using ECAD.TD;
using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace ECAD.WinForm.Sample
{
    public partial class Form1 : Form
    {
        CadControl _cadControl;
        public Form1()
        {
            Configuration.Configure();
            InitializeComponent();
            _cadControl = new CadControl()
            {
                Dock = DockStyle.Fill
            };
            panel1.Controls.Add(_cadControl);
        }

        private void 打开ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog dg = new OpenFileDialog()
            {
                Filter = "DWG files|*.dwg|DXF files|*.dxf"
            };
            if (dg.ShowDialog() == DialogResult.OK)
            {
                _cadControl.Open(dg.FileName);
            }
        }
        protected override void OnClosing(CancelEventArgs e)
        {
            _cadControl.Close();
            Configuration.Close();
            base.OnClosing(e);
        }
    }
}
