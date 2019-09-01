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
    }
}
