using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;
using Teigha.DatabaseServices;
using Teigha.Geometry;

namespace EM.CAD
{
    public class ZoomFunction : CadFunction
    {
        #region Fields

        private BoundBlock3d _client;
        BoundBlock3d Client
        {
            get => _client;
            set
            {
                if (_client != value)
                {
                    if (_client != null)
                    {
                        _client.Dispose();
                    }
                    _client = value;
                }
            }
        }
        private int _direction;
        private Point _dragStart;
        private bool _isDragging;
        private bool _preventDrag;

        private int _timerInterval;
        private System.Timers.Timer _zoomTimer;

        #endregion
        private Point2d GetResolution(BoundBlock3d boundBlock3D, Rectangle rectangle)
        {
            Point3d minPoint = boundBlock3D.GetMinimumPoint();
            Point3d maxPoint = boundBlock3D.GetMaximumPoint();
            double xRes = (maxPoint.X - minPoint.X) / rectangle.Width;
            double yRes = (maxPoint.Y - minPoint.Y) / rectangle.Height;
            Point2d point2D = new Point2d(xRes, yRes);
            return point2D;
        }
        public ZoomFunction(ICadControl cadControl) : base(cadControl)
        {
            Configure();
            BusySet = false;
        }
        #region Properties

        /// <summary>
        /// Gets or sets a value indicating whether the map function is currently interacting with the map.
        /// </summary>
        public bool BusySet { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether forward zooms in. This controls the sense (direction) of zoom (in or out) as you roll the mouse wheel.
        /// </summary>
        public bool ForwardZoomsIn
        {
            get
            {
                return _direction > 0;
            }

            set
            {
                _direction = value ? 1 : -1;
            }
        }

        /// <summary>
        /// Gets or sets the wheel zoom sensitivity. Increasing makes it more sensitive. Maximum is 0.5, Minimum is 0.01
        /// </summary>
        public double Sensitivity { get; set; }

        /// <summary>
        /// Gets or sets the full refresh timeout value in milliseconds
        /// </summary>
        public int TimerInterval
        {
            get
            {
                return _timerInterval;
            }

            set
            {
                _timerInterval = value;
                _zoomTimer.Interval = _timerInterval;
            }
        }

        #endregion

        #region Methods
        /// <summary>
        /// Handles the actions that the tool controls during the OnMouseDown event
        /// </summary>
        /// <param name="e">The event args.</param>
        public override void DoMouseDown(MouseEventArgs e)
        {
            _dragStart = new Point(e.X, e.Y);
            if (e.Button == MouseButtons.Middle && !_preventDrag && CadControl.ViewExtent != null)
            {
                _isDragging = true;
                Client = (BoundBlock3d)CadControl.ViewExtent.Clone();
            }

            base.DoMouseDown(e);
        }

        /// <summary>
        /// Handles the mouse move event, changing the viewing extents to match the movements
        /// of the mouse if the left mouse button is down.
        /// </summary>
        /// <param name="e">The event args.</param>
        public override void DoMouseMove(MouseEventArgs e)
        {
            if (_isDragging && CadControl.Database != null)
            {
                if (!BusySet)
                {
                    BusySet = true;
                }
                Point[] points = new Point[] { _dragStart, e.Location };
                Point3d[] point3Ds = CadControl.Database.PixelToWorld(points);
                Point3d startPoint3D = point3Ds[0];
                Point3d currentPoint3D = point3Ds[1];
                double xOff = startPoint3D.X - currentPoint3D.X;
                double yOff = startPoint3D.Y - currentPoint3D.Y;
                BoundBlock3d boundBlock3D = (BoundBlock3d)_client.Clone();
                boundBlock3D.TranslateBy(new Vector3d(xOff, yOff, 0));
                SetCadExtent(boundBlock3D);
            }

            base.DoMouseMove(e);
        }
        /// <summary>
        /// Mouse Up
        /// </summary>
        /// <param name="e">The event args.</param>
        public override void DoMouseUp(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Middle && _isDragging)
            {
                BusySet = false;
                _client = null;
                _isDragging = false;
            }
            _dragStart = Point.Empty;
            base.DoMouseUp(e);
        }
        public override void DoMouseDoubleClick(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Middle && CadControl?.Database!=null)
            {
                BoundBlock3d boundBlock3D = new BoundBlock3d();
                var database = CadControl.Database;
                boundBlock3D.Set(database.Extmin, database.Extmax);
                SetCadExtent(boundBlock3D);
            }
            base.DoMouseDoubleClick(e);
        }
        /// <summary>
        /// Mouse Wheel
        /// </summary>
        /// <param name="e">The event args.</param>
        public override void DoMouseWheel(MouseEventArgs e)
        {
            if (!_isDragging && CadControl.Database != null)
            {
                // Fix this
                _zoomTimer.Stop(); // if the timer was already started, stop it.
                _preventDrag = true;
                if (_client == null)
                {
                    _client = (BoundBlock3d)CadControl.ViewExtent.Clone();
                }
                Point3d point3D = CadControl.Database.PixelToWorld(e.Location);
                double ratio;
                if (_direction * e.Delta > 0)
                {
                    ratio = 1 - Sensitivity;
                }
                else
                {
                    ratio = 1 / (1 - Sensitivity);
                }
                _client.ScaleBy(ratio, new Point3d(point3D.X, point3D.Y, 0));

                _zoomTimer.Start();
                if (!BusySet)
                {
                    BusySet = true;
                }
            }
            base.DoMouseWheel(e);
        }

        private void Configure()
        {
            YieldStyle = YieldStyles.Scroll;
            _timerInterval = 100;
            _zoomTimer = new System.Timers.Timer
            {
                Interval = _timerInterval
            };
            _zoomTimer.Elapsed += ZoomTimerTick;
            Sensitivity = 0.2;
            ForwardZoomsIn = true;
            Name = "ScrollZoom";
        }

        private void ZoomTimerTick(object sender, EventArgs e)
        {
            _zoomTimer.Stop();
            SetCadExtent(_client);
            _client = null;
            BusySet = false;
            _preventDrag = false;
        }
        private void SetCadExtent(BoundBlock3d boundBlock3D)
        {
            if (CadControl == null || _client == null) return;
            CadControl.ViewExtent = boundBlock3D;
        }
        #endregion
    }
}
