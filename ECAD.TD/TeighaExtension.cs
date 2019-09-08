using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Teigha.DatabaseServices;
using Teigha.Geometry;
using Teigha.GraphicsSystem;

namespace ECAD.TD
{
    public static class TeighaExtension
    {
        public static ObjectId GetActiveViewportId(this Database database)
        {
            if (database.TileMode)
            {
                return database.CurrentViewportTableRecordId;
            }
            else
            {
                using (BlockTableRecord paperBTR = (BlockTableRecord)database.CurrentSpaceId.GetObject(OpenMode.ForRead))
                {
                    Layout l = (Layout)paperBTR.LayoutId.GetObject(OpenMode.ForRead);
                    return l.CurrentViewportId;
                }
            }
        }
        public static void Transform(this Teigha.GraphicsSystem.View pView, Point2d startPoint, Point2d endPoint)
        {
            double dx = endPoint.X - startPoint.X;
            double dy = endPoint.Y - startPoint.Y;
            Transform(pView, dx, dy);
        }
        // helper function transforming parameters from screen to world coordinates
        public static void Transform(this Teigha.GraphicsSystem.View pView, double x, double y)
        {
            Vector3d vec = new Vector3d(-x, -y, 0.0);
            vec = vec.TransformBy((pView.ScreenMatrix * pView.ProjectionMatrix).Inverse());
            pView.Dolly(vec);
        }
        public static void ZoomMap(Teigha.GraphicsSystem.View pView, Point2d centerPoint, double zoomFactor)
        {
            // camera position in world coordinates
            Point3d pos = pView.Position;
            // TransformBy() returns a transformed copy
            pos = pos.TransformBy(pView.WorldToDeviceMatrix);
            double vx = (int)pos.X;
            double vy = (int)pos.Y;
            vx = centerPoint.X - vx;
            vy = centerPoint.Y - vy;
            // we move point of view to the mouse location, to create an illusion of scrolling in/out there
            Transform(pView, -vx, -vy);
            // note that we essentially ignore delta value (sign is enough for illustrative purposes)
            pView.Zoom(zoomFactor);
            Transform(pView, vx, vy);
        }
        public static bool GetLayoutExtents(Database db, Teigha.GraphicsSystem.View pView, ref BoundBlock3d bbox)
        {
            Extents3d ext = new Extents3d();
            using (BlockTable bt = (BlockTable)db.BlockTableId.GetObject(OpenMode.ForRead))
            {
                using (BlockTableRecord pSpace = (BlockTableRecord)bt[BlockTableRecord.PaperSpace].GetObject(OpenMode.ForRead))
                {
                    using (Layout pLayout = (Layout)pSpace.LayoutId.GetObject(OpenMode.ForRead))
                    {
                        if (pLayout.GetViewports().Count > 0)
                        {
                            bool bOverall = true;
                            foreach (ObjectId id in pLayout.GetViewports())
                            {
                                if (bOverall)
                                {
                                    bOverall = false;
                                    continue;
                                }
                                //Viewport pVp = (Viewport)id.GetObject(OpenMode.ForRead);
                            }
                            ext.TransformBy(pView.ViewingMatrix);
                            bbox.Set(ext.MinPoint, ext.MaxPoint);
                        }
                        else
                        {
                            ext = pLayout.Extents;
                        }
                        bbox.Set(ext.MinPoint, ext.MaxPoint);
                    }
                }
            }

            return ext.MinPoint != ext.MaxPoint;
        }
        public static BoundBlock3d GetLayoutExtents(this LayoutHelperDevice helperDevice)
        {
            BoundBlock3d boundBlock3D = new BoundBlock3d();
            using (View pView = helperDevice.ActiveView)
            {
                // camera position in world coordinates
                Point3d pos = pView.Position;
                double halfWidth = pView.FieldWidth / 2;
                double halfHeight = pView.FieldHeight / 2;
                double xMin = pos.X - halfWidth;
                double xMax = pos.X + halfWidth;
                double yMin = pos.Y - halfHeight;
                double yMax = pos.Y + halfHeight;
                boundBlock3D.Set(new Point3d(xMin, yMin, 0), new Point3d(xMax, yMax, 0));
            }
            return boundBlock3D;
        }

        public static void ZoomToExtents(this Database database)
        {
            using (DBObject dbObj = GetActiveViewportId(database).GetObject(OpenMode.ForWrite))
            {
                // using protocol extensions we handle PS and MS viewports in the same manner
                using (AbstractViewportData viewportData = new AbstractViewportData(dbObj))
                {
                    using (Teigha.GraphicsSystem.View view = viewportData.GsView)
                    {
                        // do actual zooming - change GS view
                        using (AbstractViewPE viewPE = new AbstractViewPE(view))
                        {
                            BoundBlock3d boundBlock = new BoundBlock3d();
                            bool bBboxValid = viewPE.GetViewExtents(boundBlock);
                            // paper space overall view
                            if (dbObj is Viewport && ((Viewport)dbObj).Number == 1)
                            {
                                if (!bBboxValid || !(boundBlock.GetMinimumPoint().X < boundBlock.GetMaximumPoint().X && boundBlock.GetMinimumPoint().Y < boundBlock.GetMaximumPoint().Y))
                                {
                                    bBboxValid = GetLayoutExtents(database, view, ref boundBlock);
                                }
                            }
                            else if (!bBboxValid) // model space viewport
                            {
                                bBboxValid = GetLayoutExtents(database, view, ref boundBlock);
                            }
                            if (!bBboxValid)
                            {
                                // set to somewhat reasonable (e.g. paper size)
                                if (database.Measurement == MeasurementValue.Metric)
                                {
                                    boundBlock.Set(Point3d.Origin, new Point3d(297.0, 210.0, 0.0)); // set to papersize ISO A4 (portrait)
                                }
                                else
                                {
                                    boundBlock.Set(Point3d.Origin, new Point3d(11.0, 8.5, 0.0)); // ANSI A (8.50 x 11.00) (landscape)
                                }
                                boundBlock.TransformBy(view.ViewingMatrix);
                            }
                            viewPE.ZoomExtents(boundBlock);
                            boundBlock.Dispose();
                        }
                        // save changes to database
                        viewportData.SetView(view);
                    }
                }
            }
        }

        public static void Zoom(this Database database, BoundBlock3d box)
        {
            using (var vtr = (ViewportTableRecord)database.CurrentViewportTableRecordId.GetObject(OpenMode.ForWrite))
            {
                // using protocol extensions we handle PS and MS viewports in the same manner
                using (var vpd = new AbstractViewportData(vtr))
                {
                    var view = vpd.GsView;
                    // do actual zooming - change GS view
                    // here protocol extension is used again, that provides some helpful functions
                    using (var vpe = new AbstractViewPE(view))
                    {
                        using (BoundBlock3d boundBlock3D = (BoundBlock3d)box.Clone())
                        {
                            boundBlock3D.TransformBy(view.ViewingMatrix);
                            vpe.ZoomExtents(boundBlock3D);
                        }
                    }
                    vpd.SetView(view);
                }
            }
            //ReSize();
        }

        public static void Zoom(this Database database, Extents3d ext)
        {
            BoundBlock3d box = new BoundBlock3d();
            box.Set(ext.MinPoint, ext.MaxPoint);
            Zoom(database, box);
        }

        public static void Zoom(this Database database, Point3d minPoint, Point3d maxPoint)
        {
            BoundBlock3d box = new BoundBlock3d();
            box.Set(minPoint, maxPoint);
            Zoom(database, box);
        }
        /// <summary>
        /// 长度
        /// </summary>
        /// <param name="boundBlock3D"></param>
        /// <returns></returns>
        public static double Width(this BoundBlock3d boundBlock3D)
        {
            double value = 0;
            if (boundBlock3D != null)
            {
                value = boundBlock3D.GetMaximumPoint().X - boundBlock3D.GetMinimumPoint().X;
            }
            return value;
        }
        /// <summary>
        /// 宽度
        /// </summary>
        /// <param name="boundBlock3D"></param>
        /// <returns></returns>
        public static double Height(this BoundBlock3d boundBlock3D)
        {
            double value = 0;
            if (boundBlock3D != null)
            {
                value = boundBlock3D.GetMaximumPoint().Y - boundBlock3D.GetMinimumPoint().Y;
            }
            return value;
        }
        /// <summary>
        /// 高度
        /// </summary>
        /// <param name="boundBlock3D"></param>
        /// <returns></returns>
        public static double Depth(this BoundBlock3d boundBlock3D)
        {
            double value = 0;
            if (boundBlock3D != null)
            {
                value = boundBlock3D.GetMaximumPoint().Z - boundBlock3D.GetMinimumPoint().Z;
            }
            return value;
        }
        public static Point3d PixelToWorld(this Database database, Point point)
        {
            if (database == null)
            {
                throw new Exception("参数错误");
            }
            Point3d point3d = new Point3d(point.X, point.Y, 0);
            using (var vtr = (ViewportTableRecord)database.CurrentViewportTableRecordId.GetObject(OpenMode.ForRead))
            {
                // using protocol extensions we handle PS and MS viewports in the same manner
                using (var vpd = new AbstractViewportData(vtr))
                {
                    point3d = point3d.TransformBy(vpd.GsView.ObjectToDeviceMatrix.Inverse());
                }
            }
            return point3d;
        }
        public static Point3d[] PixelToWorld(this Database database, IEnumerable<Point> points)
        {
            if (database == null)
            {
                throw new Exception("参数错误");
            }
            List<Point3d> point3Ds = new List<Point3d>();
            if (points != null)
            {
                using (var vtr = (ViewportTableRecord)database.CurrentViewportTableRecordId.GetObject(OpenMode.ForRead))
                {
                    // using protocol extensions we handle PS and MS viewports in the same manner
                    using (var vpd = new AbstractViewportData(vtr))
                    {
                        Matrix3d matrix3D = vpd.GsView.ObjectToDeviceMatrix.Inverse();
                        foreach (var point in points)
                        {
                            Point3d point3d = new Point3d(point.X, point.Y, 0);
                            point3d = point3d.TransformBy(matrix3D);
                            point3Ds.Add(point3d);
                        }
                    }
                }
            }
            return point3Ds.ToArray();
        }
        public static BoundBlock3d PixelToWorld(this Database database, Rectangle srcRectangle)
        {
            Point bl = new Point(srcRectangle.Left, srcRectangle.Bottom);
            Point tr = new Point(srcRectangle.Right, srcRectangle.Top);
            var bottomLeft = PixelToWorld(database, bl);
            var topRight = PixelToWorld(database, tr);
            BoundBlock3d destBoundBlock3D = new BoundBlock3d();
            destBoundBlock3D.Set(bottomLeft, topRight);
            return destBoundBlock3D;
        }
        public static Point WorldToPixel(this Database database, Point3d point3D)
        {
            Point3d destPoint3d = new Point3d();
            using (var vtr = (ViewportTableRecord)database.CurrentViewportTableRecordId.GetObject(OpenMode.ForRead))
            {
                // using protocol extensions we handle PS and MS viewports in the same manner
                using (var vpd = new AbstractViewportData(vtr))
                {
                    destPoint3d = point3D.TransformBy(vpd.GsView.ObjectToDeviceMatrix);//测试
                }
            }
            Point point = new Point((int)destPoint3d.X, (int)destPoint3d.Y);
            return point;
        }

        public static Rectangle WorldToPixel(this Database database, BoundBlock3d srcBoundBlock3d)
        {
            Point3d minPoint3d = srcBoundBlock3d.GetMinimumPoint();
            Point3d maxPoint3d = srcBoundBlock3d.GetMaximumPoint();
            Point3d tl3d = new Point3d(minPoint3d.X, maxPoint3d.Y, 0);
            Point3d br3d = new Point3d(maxPoint3d.X, minPoint3d.Y, 0);
            Point tl = WorldToPixel(database, tl3d);
            Point br = WorldToPixel(database, br3d);
            Rectangle destRectangle = new Rectangle(tl.X, tl.Y, br.X - tl.X + 1, br.Y - tl.Y + 1);
            return destRectangle;
        }
    }
}
