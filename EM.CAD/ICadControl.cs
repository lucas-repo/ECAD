﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Teigha.DatabaseServices;
using Teigha.Geometry;
using Teigha.GraphicsSystem;

namespace EM.CAD
{
    public interface ICadControl : IDisposable
    {
        Rectangle View { get; }
        BoundBlock3d ViewExtent { get; set; }
        string FileName { get; }
        LayoutHelperDevice HelperDevice { get; }
        Database Database { get; }
        List<ICadFunction> CadFunctions { get; }
        void Open(string fileName, FileOpenMode fileOpenMode);
        void Close();
        void Invalidate();
        void Invalidate(Rectangle clipRectangle);
        Point3d PixelToWorld(Point point);
        BoundBlock3d PixelToWorld(Rectangle rectangle);
        Point WorldToPixel(Point3d point3D);
        Rectangle WorldToPixel(BoundBlock3d boundBlock3D);
        void ActivateCadFunction(ICadFunction function);
        ObjectIdCollection GetSelection(Point location, Teigha.GraphicsSystem.SelectionMode selectionMode);
        /// <summary>
        /// 根据控件大小重置范围比例
        /// </summary>
        /// <param name="boundBlock">范围</param>
        void ResetAspectRatio(BoundBlock3d boundBlock);
    }
}
