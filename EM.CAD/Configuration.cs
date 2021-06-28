using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Teigha.DatabaseServices;
using Teigha.Runtime;

namespace EM.CAD
{
    /// <summary>
    /// 配置类
    /// </summary>
    public static class Configuration
    {
        static int _count;
        private static object _syncLocker = new object();
        static bool IsBusy
        {
            get => _count > 0;
            set
            {
                if (value)
                {
                    _count++;
                }
                else
                {
                    _count--;
                }
                if (_count < 0)
                {
                    _count = 0;
                }
            }
        }
        static Services _services;
        /// <summary>
        /// 配置Teigha环境
        /// </summary>
        public static void Configure()
        {
            try
            {
                if (_services == null)
                {
                    lock (_syncLocker)
                    {
                        if (_services == null)
                        {
                            Services.odActivate(ActivationData.userInfo, ActivationData.userSignature);
                            _services = new Services();
                        }
                    }
                }
            }
            catch (System.Exception)
            {
                throw;
            }

            //if (_services == null)
            //{
            //    _services = new Services();
            //    SystemObjects.DynamicLinker.LoadApp("GripPoints", false, false);
            //    SystemObjects.DynamicLinker.LoadApp("PlotSettingsValidator", false, false);
            //    HostAppServ hostAppServ = new HostAppServ(_services);
            //    HostApplicationServices.Current = hostAppServ;
            //    Environment.SetEnvironmentVariable("DDPLOTSTYLEPATHS", hostAppServ.FindConfigPath(string.Format("PrinterStyleSheetDir")));
            //}
            //IsBusy = true;
        }
        /// <summary>
        /// 关闭服务并释放资源（调用之前必须释放Teigha的所有资源）
        /// </summary>
        public static void Close()
        {
            if (_services != null)
            {
                _services.Dispose();
                _services = null;
            }
            //IsBusy = false;
            //if (!IsBusy && _services != null)
            //{
            //    HostApplicationServices.Current.Dispose();
            //    _services.Dispose();
            //    _services = null;
            //}
        }
    }
}
