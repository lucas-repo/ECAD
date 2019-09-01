using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Teigha.DatabaseServices;
using Teigha.Geometry;
using Teigha.GraphicsInterface;
using Teigha.GraphicsSystem;
using Teigha.Runtime;

namespace ECAD.TD
{
    public partial class CadControl : UserControl, ICadControl
    {
         Graphics _graphics;
        private Graphics Graphics
        {
            get => _graphics;
            set{
                if (_graphics != value)
                {
                    if (_graphics != null)
                    {
                        _graphics.Dispose();
                    }
                    _graphics = value;
                }
            }

        }
        private LayoutManager _layoutManager;
        public LayoutManager LayoutManager
        {
            get => _layoutManager;
            set
            {
                if (_layoutManager != value)
                {
                    if (_layoutManager != null)
                    {
                        _layoutManager.LayoutSwitched -= LayoutManager_LayoutSwitched;
                        _layoutManager.Dispose();
                    }
                    _layoutManager = value;
                    if (_layoutManager != null)
                    {
                        _layoutManager.LayoutSwitched += LayoutManager_LayoutSwitched;
                    }
                }
            }
        }
        public string FileName { get; private set; }

        private LayoutHelperDevice _helperDevice;
        public LayoutHelperDevice HelperDevice
        {
            get => _helperDevice;
            private set
            {
                if (_helperDevice != value)
                {
                    if (_helperDevice != null)
                    {
                        _helperDevice.Dispose();
                    }
                    _helperDevice = value;
                }
            }
        }
        private Database _database;
        public Database Database
        {
            get => _database;
            private set
            {
                if (_database != value)
                {
                    if (_database != null)
                    {
                        _database.Dispose();
                    }
                    _database = value;
                }
            }
        }
        public List<ICadFunction> CadFunctions { get; }

        public Rectangle View => new Rectangle(0, 0, Width, Height);
        private BoundBlock3d _viewExtent;
        public BoundBlock3d ViewExtent
        {
            get => _viewExtent;
            set
            {
                if (_viewExtent != value)
                {
                    if (_viewExtent != null)
                    {
                        _viewExtent.Dispose();
                    }
                    _viewExtent = value;
                    if (_viewExtent != null)
                    {
                        Database.Zoom(_viewExtent);
                        Invalidate(new Rectangle(0, 0, 1, 1));
                        //Invalidate();
                    }
                    else
                    {
                        throw new System.Exception("值不能为空");
                    }
                }
            }
        }
        public CadControl()
        {
            InitializeComponent();
            CadFunctions = new List<ICadFunction>();
            ZoomFunction zoomFunction = new ZoomFunction(this);
            CadFunctions.Add(zoomFunction);
            ActivateCadFunction(zoomFunction);
        }
        ~CadControl()
        {
            Close();
        }

        void InitializeGraphics()
        {
            Graphics = Graphics.FromHwnd(Handle);
            // load some predefined rendering module (may be also "WinDirectX" or "WinOpenGL")
            using (GsModule gsModule = (GsModule)SystemObjects.DynamicLinker.LoadModule("WinBitmap.txv", false, true))
            {
                // create graphics device
                using (Device graphichsDevice = gsModule.CreateDevice())
                {
                    // setup device properties
                    using (Dictionary props = graphichsDevice.Properties)
                    {
                        if (props.Contains("WindowHWND")) // Check if property is supported
                            props.AtPut("WindowHWND", new RxVariant((int)Handle)); // hWnd necessary for DirectX device
                        if (props.Contains("WindowHDC")) // Check if property is supported
                            props.AtPut("WindowHDC", new RxVariant((int)Graphics.GetHdc())); // hWindowDC necessary for Bitmap device
                        if (props.Contains("DoubleBufferEnabled")) // Check if property is supported
                            props.AtPut("DoubleBufferEnabled", new RxVariant(true));
                        if (props.Contains("EnableSoftwareHLR")) // Check if property is supported
                            props.AtPut("EnableSoftwareHLR", new RxVariant(true));
                        if (props.Contains("DiscardBackFaces")) // Check if property is supported
                            props.AtPut("DiscardBackFaces", new RxVariant(true));
                    }
                    // setup paperspace viewports or tiles
                    ContextForDbDatabase ctx = new ContextForDbDatabase(Database)
                    {
                        UseGsModel = true
                    };
                    //ctx.SetPlotGeneration(true);
                    //ctx.DisableFastMoveDrag();
                    HelperDevice = LayoutHelperDevice.SetupActiveLayoutViews(graphichsDevice, ctx);

                    //using (BlockTableRecord paperBTR = (BlockTableRecord)Database.CurrentSpaceId.GetObject(OpenMode.ForRead))
                    //{
                    //    using (Layout layout = (Layout)paperBTR.LayoutId.GetObject(OpenMode.ForRead))
                    //    {
                    //        HelperDevice = LayoutHelperDevice.SetupLayoutViews(layout.Id, graphichsDevice, ctx);
                    //    }
                    //}

                    HelperDevice.BackgroundColor = Color.FromArgb(33, 40, 48);
                    Aux.preparePlotstyles(Database, ctx);
                }
            }
            // set palette
            HelperDevice.SetLogicalPalette(Device.DarkPalette);
            // set output extents
            ResizeControl();
            //helperDevice.Model.Invalidate(InvalidationHint.kInvalidateAll);

        }
        void ResizeControl()
        {
            if (HelperDevice != null)
            {
                Rectangle r = Bounds;
                r.Offset(-Location.X, -Location.Y);
                // HDC assigned to the device corresponds to the whole client area of the panel
                HelperDevice.OnSize(r);
                Invalidate(new Rectangle(0, 0, 1, 1));
            }
        }
        public void Close()
        {
            if (Database != null)
            {
                LayoutManager = null;
                Database = null;
                HostApplicationServices.WorkingDatabase = null;
                HelperDevice = null;
                //InitializeGraphics();//todo 测试
                FileName = null;
                Graphics = null;
                Invalidate(new Rectangle(0, 0, 1, 1));
            }
        }
        public void Open(string fileName)
        {
            if (FileName != fileName)
            {
                if (Database != null)
                {
                    Close();
                }
                bool bLoaded = true;

                Database database = new Database(false, false);
                try
                {
                    database.ReadDwgFile(fileName, FileOpenMode.OpenForReadAndAllShare, false, "");
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    bLoaded = false;
                }
                if (bLoaded)
                {
                    Database = database;
                    FileName = fileName;
                }

                if (Database != null)
                {
                    HostApplicationServices.WorkingDatabase = Database;
                    LayoutManager = LayoutManager.Current;
                    InitializeGraphics();
                    _viewExtent = HelperDevice.GetLayoutExtents();
                }
            }
        }

        private void LayoutManager_LayoutSwitched(object sender, Teigha.DatabaseServices.LayoutEventArgs e)
        {
            InitializeGraphics();
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            ResizeControl();
            base.OnSizeChanged(e);
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            if (HelperDevice != null)
            {
                try
                {
                    HelperDevice.Update();
                }
                catch (Teigha.Runtime.Exception errror)
                {
                    Debug.WriteLine(string.Format("刷新图纸失败，原因：{0}", errror.Message));
                }
            }
        }
        protected override void OnMouseWheel(MouseEventArgs e)
        {
            foreach (var tool in CadFunctions.Where(_ => _.Enabled))
            {
                tool.DoMouseWheel(e);
            }
            base.OnMouseWheel(e);
        }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            foreach (var tool in CadFunctions.Where(_ => _.Enabled))
            {
                tool.DoMouseUp(e);
            }
            base.OnMouseUp(e);
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            foreach (var tool in CadFunctions.Where(_ => _.Enabled))
            {
                tool.DoMouseMove(e);
            }
            base.OnMouseMove(e);
        }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            foreach (var tool in CadFunctions.Where(_ => _.Enabled))
            {
                tool.DoMouseDown(e);
            }
            base.OnMouseDown(e);
        }

        public Point3d PixelToWorld(Point point)
        {
            if (ViewExtent == null)
            {
                return new Point3d(0, 0, 0);
            }
            Point3d minPoint3d = ViewExtent.GetMinimumPoint();
            Point3d maxPoint3d = ViewExtent.GetMaximumPoint();
            double width = maxPoint3d.X - minPoint3d.X;
            double height = maxPoint3d.Y - minPoint3d.Y;
            double x = Convert.ToDouble(point.X);
            double y = Convert.ToDouble(point.Y);
            x = (x * width / Width) + minPoint3d.X;
            y = maxPoint3d.Y - (y * height / Height);
            return new Point3d(x, y, 0.0);
        }

        public Point WorldToPixel(Point3d point3D)
        {
            Point point = new Point();
            if (ViewExtent == null)
            {
                return point;
            }
            Point3d minPoint3d = ViewExtent.GetMinimumPoint();
            Point3d maxPoint3d = ViewExtent.GetMaximumPoint();
            double width = maxPoint3d.X - minPoint3d.X;
            double height = maxPoint3d.Y - minPoint3d.Y;
            if (Width == 0 || Height == 0) return new Point(0, 0);
            int x = Convert.ToInt32((point3D.X - minPoint3d.X) * (Width / width));
            int y = Convert.ToInt32((maxPoint3d.Y - point3D.Y) * (Height / height));
            return new Point(x, y);
        }

        public BoundBlock3d PixelToWorld(Rectangle rectangle)
        {
            Point bl = new Point(rectangle.X, rectangle.Bottom);
            Point tr = new Point(rectangle.Right, rectangle.Top);

            var bottomLeft = PixelToWorld(bl);
            var topRight = PixelToWorld(tr);
            BoundBlock3d boundBlock3D = new BoundBlock3d();
            boundBlock3D.Set(bottomLeft, topRight);
            return boundBlock3D;
        }

        public Rectangle WorldToPixel(BoundBlock3d boundBlock3D)
        {
            Point3d minPoint3d = boundBlock3D.GetMinimumPoint();
            Point3d maxPoint3d = boundBlock3D.GetMaximumPoint();
            Point3d tl = new Point3d(minPoint3d.X, maxPoint3d.Y, 0);
            Point3d br = new Point3d(maxPoint3d.X, minPoint3d.Y, 0);
            Point topLeft = WorldToPixel(tl);
            Point bottomRight = WorldToPixel(br);
            return new Rectangle(topLeft.X, topLeft.Y, bottomRight.X - topLeft.X, bottomRight.Y - topLeft.Y);
        }

        public void ActivateCadFunction(ICadFunction function)
        {
            if (function != null)
            {
                if (!CadFunctions.Contains(function))
                {
                    CadFunctions.Add(function);
                }
                foreach (var f in CadFunctions)
                {
                    if ((f.YieldStyle & YieldStyles.AlwaysOn) == YieldStyles.AlwaysOn) continue; // ignore "Always On" functions
                    int test = (int)(f.YieldStyle & function.YieldStyle);
                    if (test > 0) f.Deactivate(); // any overlap of behavior leads to deactivation
                }
                function.Activate();
            }
        }
    }
}
