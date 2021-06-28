using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Teigha.DatabaseServices;
using Teigha.GraphicsInterface;
using Teigha.Runtime;

namespace EM.CAD
{
    public class Aux
    {
        /// <summary>
        /// 活动的视口ID
        /// </summary>
        /// <param name="database"></param>
        /// <returns></returns>
        public static ObjectId Active_viewport_id(Database database)
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
        /// <summary>
        /// 预览图类型
        /// </summary>
        /// <param name="database"></param>
        /// <param name="ctx"></param>
        public static void preparePlotstyles(Database database, ContextForDbDatabase ctx)
        {
            using (var trans = database.TransactionManager.StartTransaction())
            {
                using (BlockTableRecord paperBTR = (BlockTableRecord)database.CurrentSpaceId.GetObject(OpenMode.ForRead))
                {
                    //通过块表记录得到布局
                    using (Layout pLayout = (Layout)paperBTR.LayoutId.GetObject(OpenMode.ForRead))
                    {
                        if (ctx.IsPlotGeneration ? pLayout.PlotPlotStyles : pLayout.ShowPlotStyles)
                        {
                            string pssFile = pLayout.CurrentStyleSheet;
                            if (pssFile.Length > 0)
                            {
                                string testpath = ((HostAppServ)HostApplicationServices.Current).FindFile(pssFile, database, FindFileHint.Default);
                                if (testpath.Length > 0)
                                {
                                    using (FileStreamBuf pFileBuf = new FileStreamBuf(testpath))
                                    {
                                        ctx.LoadPlotStyleTable(pFileBuf);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
