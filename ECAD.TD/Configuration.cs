using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Teigha.DatabaseServices;
using Teigha.Runtime;

namespace ECAD.TD
{
    /// <summary>
    /// 配置类
    /// </summary>
    public static class Configuration
    {
        static Services _services;
        /// <summary>
        /// 配置Teigha环境
        /// </summary>
        public static void Configure()
        {
            if (_services == null)
            {
                _services = new Services();
                //SystemObjects.DynamicLinker.LoadApp("GripPoints", false, false);
                //SystemObjects.DynamicLinker.LoadApp("PlotSettingsValidator", false, false);
                //HostAppServ hostAppServ= new HostAppServ(_services);
                //HostApplicationServices.Current = hostAppServ;
                //Environment.SetEnvironmentVariable("DDPLOTSTYLEPATHS", hostAppServ.FindConfigPath(string.Format("PrinterStyleSheetDir")));
            }
        }
        /// <summary>
        /// 释放资源
        /// </summary>
        public static void Close()
        {
            if (_services != null)
            {
                _services.Dispose();
                _services = null;
            }
        }
    } 
}
