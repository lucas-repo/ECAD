using System;
using System.Drawing;
using System.Windows.Forms;

namespace ECAD.TD
{
    public abstract class CadFunction : ICadFunction
    {
        public Image ButtonImage { get; set; }

        public Bitmap CursorBitmap { get; set; }

        public bool Enabled { get; protected set; }

        public ICadControl CadControl { get; set; }
        public string Name { get; set; }

        public YieldStyles YieldStyle { get; set; }

        public event EventHandler FunctionActivated;
        public event EventHandler FunctionDeactivated;
        public event EventHandler<KeyEventArgs> KeyUp;
        public event EventHandler<MouseEventArgs> MouseDoubleClick;
        public event EventHandler<MouseEventArgs> MouseDown;
        public event EventHandler<MouseEventArgs> MouseMove;
        public event EventHandler<MouseEventArgs> MouseUp;
        public event EventHandler<MouseEventArgs> MouseWheel;
    
        public CadFunction(ICadControl cadControl)
        {
            CadControl = cadControl;
        }
        public virtual void Activate()
        {
            Enabled = true;
            FunctionActivated?.Invoke(this, EventArgs.Empty);
        }

        public virtual void Deactivate()
        {
            Enabled = false;
            FunctionDeactivated?.Invoke(this, EventArgs.Empty);
        }

        public virtual void DoKeyDown(KeyEventArgs e)
        {
        }

        public virtual void DoKeyUp(KeyEventArgs e)
        {
            KeyUp?.Invoke(this, e);
        }

        public virtual void DoMouseDoubleClick(MouseEventArgs e)
        {
            MouseDoubleClick?.Invoke(this, e);
        }

        public virtual void DoMouseDown(MouseEventArgs e)
        {
            MouseDown?.Invoke(this, e);
        }

        public virtual void DoMouseMove(MouseEventArgs e)
        {
            MouseMove?.Invoke(this, e);
        }

        public virtual void DoMouseUp(MouseEventArgs e)
        {
            MouseUp?.Invoke(this, e);
        }

        public virtual void DoMouseWheel(MouseEventArgs e)
        {
            MouseWheel?.Invoke(this, e);
        }

        public virtual void Unload()
        {
        }
    }
}