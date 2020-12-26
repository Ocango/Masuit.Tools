using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;

namespace Ocansuit.Tools.INIFile
{
    public class INIFileUtil
    {
        readonly static public string sysinifile = System.IO.Directory.GetCurrentDirectory() + @"\sys.ini";
        private String filePath = "";

        public INIFileUtil(string filename)
        {
            filePath = filename;
        }
        /// <summary>
        /// 为INI文件中指定的节点取得字符串
        /// </summary>
        /// <param name="lpAppName">欲在其中查找关键字的节点名称</param>
        /// <param name="lpKeyName">欲获取的项名</param>
        /// <param name="lpDefault">指定的项没有找到时返回的默认值</param>
        /// <param name="lpReturnedString">指定一个字串缓冲区，长度至少为nSize</param>
        /// <param name="nSize">指定装载到lpReturnedString缓冲区的最大字符数量</param>
        /// <param name="lpFileName">INI文件完整路径</param>
        /// <returns>复制到lpReturnedString缓冲区的字节数量，其中不包括那些NULL中止字符</returns>
        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string lpAppName, string lpKeyName, string lpDefault, StringBuilder lpReturnedString, int nSize, string lpFileName);
        [DllImport("kernel32", EntryPoint = "GetPrivateProfileString")]
        private static extern uint GetPrivateProfileStringA(string section, string key,string def, Byte[] retVal, int size, string filePath);
        /// <summary>
        /// 修改INI文件中内容
        /// </summary>
        /// <param name="lpApplicationName">欲在其中写入的节点名称</param>
        /// <param name="lpKeyName">欲设置的项名</param>
        /// <param name="lpString">要写入的新字符串</param>
        /// <param name="lpFileName">INI文件完整路径</param>
        /// <returns>非零表示成功，零表示失败</returns>
        [DllImport("kernel32")]
        private static extern int WritePrivateProfileString(string lpApplicationName, string lpKeyName, string lpString, string lpFileName);
        /// <summary>
        /// 读取INI文件值
        /// </summary>
        /// <param name="section">节点名</param>
        /// <param name="key">键</param>
        /// <param name="CNname">对象本地化名称</param>
        /// <param name="haserror">是否要抛出错误</param>
        /// <returns>读取的值</returns>
        public string Read(string section, string key, string CNname = "", bool haserror = false)
        {
            return Read(section, key, filePath, CNname, haserror);
        }
        /// <summary>
        /// 读取INI文件值
        /// </summary>
        /// <param name="section">节点名</param>
        /// <param name="key">键</param>
        /// <param name="filePath">INI文件完整路径</param>
        /// <param name="CNname">对象本地化名称</param>
        /// <param name="haserror">是否要抛出错误</param>
        /// <returns>读取的值</returns>
        public static string Read(string section, string key, string filePath, string CNname = "", bool haserror = false)
        {
            StringBuilder sb = new StringBuilder(1024);
            GetPrivateProfileString(section, key, "", sb, 1024, filePath);
            if (string.IsNullOrWhiteSpace(sb.ToString()) && haserror)
                throw new Exception(key + '(' + CNname + ")未找到对象或对象为空。");
            return sb.ToString();
        }
        /// <summary>
        /// 写INI文件值
        /// </summary>
        /// <param name="section">欲在其中写入的节点名称</param>
        /// <param name="key">欲设置的项名</param>
        /// <param name="value">要写入的新字符串</param>
        /// <returns>非零表示成功，零表示失败</returns>
        public int Write(string section, string key, string value)
        {
            return Write(section, key, value, filePath);
        }
        /// <summary>
        /// 写INI文件值
        /// </summary>
        /// <param name="section">欲在其中写入的节点名称</param>
        /// <param name="key">欲设置的项名</param>
        /// <param name="value">要写入的新字符串</param>
        /// <param name="filePath">INI文件完整路径</param>
        /// <returns>非零表示成功，零表示失败</returns>
        public static int Write(string section, string key, string value, string filePath )
        {
            int a = WritePrivateProfileString(section, key, value, filePath);
            if (a == 0)
                throw new Exception("写入ini异常：" + a.ToString());
            return a;
        }
        /// <summary>
        /// 删除节
        /// </summary>
        /// <param name="section">节点名</param>
        /// <returns>非零表示成功，零表示失败</returns>
        public int DeleteSection(string section)
        {
            return DeleteSection(section, filePath);
        }
        /// <summary>
        /// 删除节
        /// </summary>
        /// <param name="section">节点名</param>
        /// <param name="filePath">INI文件完整路径</param>
        /// <returns>非零表示成功，零表示失败</returns>
        public static int DeleteSection(string section, string filePath)
        {
            return Write(section, null, null, filePath);
        }
        /// <summary>
        /// 删除键的值
        /// </summary>
        /// <param name="section">节点名</param>
        /// <param name="key">键名</param>
        /// <returns>非零表示成功，零表示失败</returns>
        public int DeleteKey(string section, string key)
        {
            return DeleteKey(section, key, filePath);
        }
        /// <summary>
        /// 删除键的值
        /// </summary>
        /// <param name="section">节点名</param>
        /// <param name="key">键名</param>
        /// <param name="filePath">INI文件完整路径</param>
        /// <returns>非零表示成功，零表示失败</returns>
        public static int DeleteKey(string section, string key, string filePath)
        {
            return Write(section, key, null,filePath);
        }
        /// <summary>
        /// 读取所有的Sections
        /// </summary>
        /// <returns></returns>
        public List<string> ReadSections()
        {
            return ReadSections(filePath);
        }
        /// <summary>
        /// 读取所有的Sections
        /// </summary>
        /// <param name="iniFilename">文件名</param>
        /// <returns></returns>
        public static List<string> ReadSections(string iniFilename)
        {
            List<string> result = new List<string>();
            Byte[] buf = new Byte[65536];
            uint len = GetPrivateProfileStringA(null, null, null, buf, buf.Length, iniFilename);
            int j = 0;
            for (int i = 0; i < len; i++)
                if (buf[i] == 0)
                {
                    result.Add(Encoding.Default.GetString(buf, j, i - j));
                    j = i + 1;
                }
            return result;
        }
        /// <summary>
        /// 读取section下所有keys
        /// </summary>
        /// <param name="SectionName">Section值</param>
        /// <returns></returns>
        public List<string> ReadKeys(String SectionName)
        {
            return ReadKeys(SectionName, filePath);
        }
        /// <summary>
        /// 读取section下所有keys
        /// </summary>
        /// <param name="SectionName">Section值</param>
        /// <param name="iniFilename">文件名</param>
        /// <returns></returns>
        public static List<string> ReadKeys(string SectionName, string iniFilename)
        {
            List<string> result = new List<string>();
            Byte[] buf = new Byte[65536];
            uint len = GetPrivateProfileStringA(SectionName, null, null, buf, buf.Length, iniFilename);
            int j = 0;
            for (int i = 0; i < len; i++)
                if (buf[i] == 0)
                {
                    result.Add(Encoding.Default.GetString(buf, j, i - j));
                    j = i + 1;
                }
            return result;
        }

    }
}
