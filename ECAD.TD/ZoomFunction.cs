using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;

namespace ECAD.TD
{
    public class ZoomFunction : CadFunction
    {
        #region Fields

        private Rectangle _client;
        private int _direction;
        private Point _dragStart;
        private bool _isDragging;
        private bool _preventDrag;
        private double _sensitivity;
        private Rectangle _source;
        private Rectangle _destView;
        private int _timerInterval;
        private System.Timers.Timer _zoomTimer;

        private double _offsetX = 0;
        private double _offsetY = 0;
        private double _zoomFactor;

        #endregion
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
        public double Sensitivity
        {
            get
            {
                return 1.0 / _sensitivity;
            }

            set
            {
                if (value > 0.5)
                    value = 0.5;
                else if (value < 0.01)
                    value = 0.01;
                _sensitivity = 1.0 / value;
            }
        }

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
            if (e.Button == MouseButtons.Middle && !_preventDrag)
            {
                _dragStart = e.Location;
                _source = CadControl.View;
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
            if (_dragStart != Point.Empty && !_preventDrag)
            {
                if (!BusySet)
                {
                    BusySet = true;
                }

                _isDragging = true;
                Point diff = new Point
                {
                    X = _dragStart.X - e.X,
                    Y = _dragStart.Y - e.Y
                };
                _destView = new Rectangle(_source.X + diff.X, _source.Y + diff.Y, _source.Width, _source.Height);
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
                _isDragging = false;
                _preventDrag = true;
                CadControl.ViewExtent = CadControl.PixelToWorld(_destView);
                _preventDrag = false;
                BusySet = false;
            }
            _dragStart = Point.Empty;
            base.DoMouseUp(e);
        }

        /// <summary>
        /// Mouse Wheel
        /// </summary>
        /// <param name="e">The event args.</param>
        public override void DoMouseWheel(MouseEventArgs e)
        {
            // Fix this
            _zoomTimer.Stop(); // if the timer was already started, stop it.

            Rectangle r = CadControl.View;

            // For multiple zoom steps before redrawing, we actually
            // want the x coordinate relative to the screen, not
            // the x coordinate relative to the previously modified view.
            if (_client == Rectangle.Empty) _client = r;
            int cw = _client.Width;
            int ch = _client.Height;

            double w = r.Width;
            double h = r.Height;

            if (_direction * e.Delta > 0)
            {
                double inFactor = 2.0 * _sensitivity;
                r.Inflate(Convert.ToInt32(-w / inFactor), Convert.ToInt32(-h / inFactor));

                // try to keep the mouse cursor in the same geographic position
                r.X += Convert.ToInt32((e.X * w / (_sensitivity * cw)) - (w / inFactor));
                r.Y += Convert.ToInt32((e.Y * h / (_sensitivity * ch)) - (h / inFactor));
            }
            else
            {
                double outFactor = 0.5 * _sensitivity;
                r.Inflate(Convert.ToInt32(w / _sensitivity), Convert.ToInt32(h / _sensitivity));
                r.X += Convert.ToInt32((w / _sensitivity) - (e.X * w / (outFactor * cw)));
                r.Y += Convert.ToInt32((h / _sensitivity) - (e.Y * h / (outFactor * ch)));
            }

            _destView = r;
            _zoomTimer.Start();
            if (!BusySet)
            {
                BusySet = true;
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
            _client = Rectangle.Empty;
            Sensitivity = .50;
            ForwardZoomsIn = true;
            Name = "ScrollZoom";
        }

        private void ZoomTimerTick(object sender, EventArgs e)
        {
            _zoomTimer.Stop();
            if (CadControl == null) return;
            _client = Rectangle.Empty;
            CadControl.ViewExtent = CadControl.PixelToWorld(_destView);
            _destView = CadControl.View;
            BusySet = false;
        }

        #endregion
    }
}
