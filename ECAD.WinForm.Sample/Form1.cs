using ECAD.TD;
using System;
using System.ComponentModel;
using System.Drawing;
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
                _cadControl.Open(dg.FileName, Teigha.DatabaseServices.FileOpenMode.OpenForReadAndAllShare);
            }
        }
        protected override void OnClosed(EventArgs e)
        {
            if (_cadControl != null)
            {
                _cadControl.Dispose();
                _cadControl = null;
            }
            Configuration.Close();
            base.OnClosed(e);
        }

        private void 动态演示ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ChangeColor(_cadControl.Database);
        }
        private void SetColor(Teigha.DatabaseServices.Entity entity, Color color)
        {
            var tempColor = entity.Color;
            tempColor.Dispose();
            entity.Color = Teigha.Colors.Color.FromColor(color);
        }
        bool _tmp;
        private void ChangeColor(Teigha.DatabaseServices.Database database)
        {
            Color color = _tmp ? Color.FromArgb(192, 0, 192) : Color.Blue;
            _tmp = !_tmp;
            using (var pTable = (Teigha.DatabaseServices.BlockTable)database.BlockTableId.GetObject(Teigha.DatabaseServices.OpenMode.ForRead))
            {
                foreach (var blockTableRecordId in pTable)
                {
                    using (var blockTableRecord = (Teigha.DatabaseServices.BlockTableRecord)blockTableRecordId.GetObject(Teigha.DatabaseServices.OpenMode.ForRead))
                    {
                        foreach (var entid in blockTableRecord)
                        {
                            using (var entity = (Teigha.DatabaseServices.Entity)entid.GetObject(Teigha.DatabaseServices.OpenMode.ForWrite))
                            {
                                var blockName = entity.BlockName;
                                var layerName = entity.Layer;
                                if (entity is Teigha.DatabaseServices.BlockReference blockReference)//todo 块引用
                                {
                                    foreach (Teigha.DatabaseServices.ObjectId attributeId in blockReference.AttributeCollection)
                                    {
                                        using (var attribute = (Teigha.DatabaseServices.AttributeReference)attributeId.GetObject(Teigha.DatabaseServices.OpenMode.ForRead))
                                        {
                                            string fieldName = attribute.Tag;
                                            string value = attribute.TextString;
                                            if (fieldName == "唯一ID" && value == "22222")
                                            {
                                                //InsertBlockTableRecord(blockTableRecordId, "0", "属性块2", blockReference.Position, blockReference.ScaleFactors, blockReference.Rotation);
                                                //entity.Erase();//删除实体
                                                entity.Highlight();
                                                break;
                                            }
                                        }
                                    }
                                }
                                else if (entity is Teigha.DatabaseServices.Line line)
                                {
                                    switch (blockName)
                                    {
                                        case "4030010":
                                            SetColor(entity, color);
                                            break;
                                    }
                                }
                                else if (entity is Teigha.DatabaseServices.Hatch hatch)
                                {
                                }
                                else if (entity is Teigha.DatabaseServices.Circle circle)
                                {
                                }
                                else if (entity is Teigha.DatabaseServices.AttributeDefinition attributeDefinition)
                                {
                                }
                                else if (entity is Teigha.DatabaseServices.MText text)
                                {
                                    text.Contents = "123";
                                    var value = text.Text;
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
