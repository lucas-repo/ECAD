using System;
using System.Drawing;
using System.Windows.Forms;

namespace EM.CAD
{
    public abstract class CadFunction : ICadFunction,IDisposable
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

        #region IDisposable Support
        private bool disposedValue = false; // 要检测冗余调用

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: 释放托管状态(托管对象)。
                    ButtonImage?.Dispose();
                    ButtonImage = null;
                    CursorBitmap?.Dispose();
                    CursorBitmap = null;
                }

                // TODO: 释放未托管的资源(未托管的对象)并在以下内容中替代终结器。
                // TODO: 将大型字段设置为 null。

                disposedValue = true;
            }
        }

        // TODO: 仅当以上 Dispose(bool disposing) 拥有用于释放未托管资源的代码时才替代终结器。
        // ~CadFunction()
        // {
        //   // 请勿更改此代码。将清理代码放入以上 Dispose(bool disposing) 中。
        //   Dispose(false);
        // }

        // 添加此代码以正确实现可处置模式。
        public void Dispose()
        {
            // 请勿更改此代码。将清理代码放入以上 Dispose(bool disposing) 中。
            Dispose(true);
            // TODO: 如果在以上内容中替代了终结器，则取消注释以下行。
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}