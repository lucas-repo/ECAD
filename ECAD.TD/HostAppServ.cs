using Microsoft.Win32;
using System;
using Teigha.DatabaseServices;

namespace ECAD.TD
{
    class HostAppServ : HostApplicationServices
    {
        Teigha.Runtime.Services dd;
        public HostAppServ(Teigha.Runtime.Services serv)
        {
            dd = serv;
        }

        public string FindConfigPath(string configType)
        {
            string subkey = GetRegistryAcadProfilesKey();
            if (subkey.Length > 0)
            {
                subkey += string.Format("\\General");
                string searchPath;
                if (GetRegistryString(Registry.CurrentUser, subkey, configType, out searchPath))
                    return searchPath;
            }
            return string.Format("");
        }

        private string FindConfigFile(string configType, string file)
        {
            string searchPath = FindConfigPath(configType);
            if (searchPath.Length > 0)
            {
                searchPath = string.Format("{0}\\{1}", searchPath, file);
                if (dd.AccessFileRead(searchPath))
                    return searchPath;
            }
            return string.Format("");
        }

        public override string FindFile(string file, Database db, FindFileHint hint)
        {
            string sFile = this.FindFileEx(file, db, hint);
            if (sFile.Length > 0)
                return sFile;

            string strFileName = file;
            string ext;
            if (strFileName.Length > 3)
                ext = strFileName.Substring(strFileName.Length - 4, 4).ToUpper();
            else
                ext = file.ToUpper();
            if (ext == string.Format(".PC3"))
                return FindConfigFile(string.Format("PrinterConfigDir"), file);
            if (ext == string.Format(".STB") || ext == string.Format(".CTB"))
                return FindConfigFile(string.Format("PrinterStyleSheetDir"), file);
            if (ext == string.Format(".PMP"))
                return FindConfigFile(string.Format("PrinterDescDir"), file);

            switch (hint)
            {
                case FindFileHint.FontFile:
                case FindFileHint.CompiledShapeFile:
                case FindFileHint.TrueTypeFontFile:
                case FindFileHint.PatternFile:
                case FindFileHint.FontMapFile:
                case FindFileHint.TextureMapFile:
                    break;
                default:
                    return sFile;
            }

            if (hint != FindFileHint.TextureMapFile && ext != string.Format(".SHX") && ext != string.Format(".PAT") && ext != string.Format(".TTF") && ext != string.Format(".TTC"))
            {
                strFileName += string.Format(".shx");
            }
            else if (hint == FindFileHint.TextureMapFile)
            {
                strFileName.Replace(string.Format("/"), string.Format("\\"));
                int last = strFileName.LastIndexOf("\\");
                strFileName = strFileName.Substring(0, last);
            }


            sFile = (hint != FindFileHint.TextureMapFile) ? GetRegistryACADFromProfile() : GetRegistryAVEMAPSFromProfile();
            while (sFile.Length > 0)
            {
                int nFindStr = sFile.IndexOf(";");
                string sPath;
                if (-1 == nFindStr)
                {
                    sPath = sFile;
                    sFile = string.Format("");
                }
                else
                {
                    sPath = string.Format("{0}\\{1}", sFile.Substring(0, nFindStr), strFileName);
                    if (dd.AccessFileRead(sPath))
                    {
                        return sPath;
                    }
                    sFile = sFile.Substring(nFindStr + 1, sFile.Length - nFindStr - 1);
                }
            }

            if (hint == FindFileHint.TextureMapFile)
            {
                return sFile;
            }

            if (sFile.Length <= 0)
            {
                string sAcadLocation = GetRegistryAcadLocation();
                if (sAcadLocation.Length > 0)
                {
                    sFile = string.Format("{0}\\Fonts\\{1}", sAcadLocation, strFileName);
                    if (dd.AccessFileRead(sFile))
                    {
                        sFile = string.Format("{0}\\Support\\{1}", sAcadLocation, strFileName);
                        if (dd.AccessFileRead(sFile))
                        {
                            sFile = string.Format("");
                        }
                    }
                }
            }
            return sFile;
        }

        public override string FontMapFileName
        {
            get
            {
                string subkey = GetRegistryAcadProfilesKey();
                if (subkey.Length > 0)
                {
                    subkey += string.Format("\\Editor Configuration");
                    string fontMapFile;
                    if (GetRegistryString(Registry.CurrentUser, subkey, string.Format("FontMappingFile"), out fontMapFile))
                        return fontMapFile;
                }
                return string.Format("");
            }
        }

        bool GetRegistryString(RegistryKey rKey, string subkey, string name, out string value)
        {
            bool rv = false;
            object objData = null;

            RegistryKey regKey;
            regKey = rKey.OpenSubKey(subkey);
            if (regKey != null)
            {
                objData = regKey.GetValue(name);
                if (objData != null)
                {
                    rv = true;
                }
                regKey.Close();
            }
            if (rv)
                value = objData.ToString();
            else
                value = string.Format("");

            rKey.Close();
            return rv;
        }

        string GetRegistryAVEMAPSFromProfile()
        {
            string subkey = GetRegistryAcadProfilesKey();
            if (subkey.Length > 0)
            {
                subkey += string.Format("\\General");
                // get the value for the ACAD entry in the registry
                string tmp;
                if (GetRegistryString(Registry.CurrentUser, subkey, string.Format("AVEMAPS"), out tmp))
                    return tmp;
            }
            return string.Format("");
        }

        string GetRegistryAcadProfilesKey()
        {
            string subkey = string.Format("SOFTWARE\\Autodesk\\AutoCAD");
            string tmp;

            if (!GetRegistryString(Registry.CurrentUser, subkey, string.Format("CurVer"), out tmp))
                return string.Format("");
            subkey += string.Format("\\{0}", tmp);

            if (!GetRegistryString(Registry.CurrentUser, subkey, string.Format("CurVer"), out tmp))
                return string.Format("");
            subkey += string.Format("\\{0}\\Profiles", tmp);

            if (!GetRegistryString(Registry.CurrentUser, subkey, string.Format(""), out tmp))
                return string.Format("");
            subkey += string.Format("\\{0}", tmp);
            return subkey;
        }

        string GetRegistryAcadLocation()
        {
            string subkey = string.Format("SOFTWARE\\Autodesk\\AutoCAD");
            string tmp;

            if (!GetRegistryString(Registry.CurrentUser, subkey, string.Format("CurVer"), out tmp))
                return string.Format("");
            subkey += string.Format("\\{0}", tmp);

            if (!GetRegistryString(Registry.CurrentUser, subkey, string.Format("CurVer"), out tmp))
                return string.Format("");
            subkey += string.Format("\\{0}", tmp);

            if (!GetRegistryString(Registry.CurrentUser, subkey, string.Format(""), out tmp))
                return string.Format("");
            return tmp;
        }

        string GetRegistryACADFromProfile()
        {
            string subkey = GetRegistryAcadProfilesKey();
            if (subkey.Length > 0)
            {
                subkey += string.Format("\\General");
                // get the value for the ACAD entry in the registry
                string tmp;
                if (GetRegistryString(Registry.CurrentUser, subkey, string.Format("ACAD"), out tmp))
                    return tmp;
            }
            return string.Format("");
        }
    };
}
