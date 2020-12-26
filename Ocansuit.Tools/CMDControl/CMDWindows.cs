using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Ocansuit.Tools.CMDControl
{
    public delegate bool ConsoleCtrlDelegate(int ctrlType);
    public class CMDWindows
    {
        #region 设置控制台标题 禁用关闭按钮
        [DllImport("user32.dll", EntryPoint = "FindWindow")]
        extern static IntPtr FindWindow(string lpClassName, string lpWindowName);
        [DllImport("user32.dll", EntryPoint = "GetSystemMenu")]
        extern static IntPtr GetSystemMenu(IntPtr hWnd, IntPtr bRevert);
        [DllImport("user32.dll", EntryPoint = "RemoveMenu")]
        extern static IntPtr RemoveMenu(IntPtr hMenu, uint uPosition, uint uFlags);

        public static void DisbleClosebtn(string appname)
        {
            IntPtr windowHandle = FindWindow(null, appname);
            IntPtr closeMenu = GetSystemMenu(windowHandle, IntPtr.Zero);
            uint SC_CLOSE = 0xF060;
            RemoveMenu(closeMenu, SC_CLOSE, 0x0);
        }
        protected static void CloseConsole(object sender, ConsoleCancelEventArgs e)
        {
            Environment.Exit(0);
        }
        #endregion
        #region 关闭控制台 快速编辑模式、插入模式
        const int STD_INPUT_HANDLE = -10;
        const uint ENABLE_QUICK_EDIT_MODE = 0x0040;
        const uint ENABLE_INSERT_MODE = 0x0020;
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern IntPtr GetStdHandle(int hConsoleHandle);
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint mode);
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint mode);

        public static void DisbleQuickEditMode()
        {
            IntPtr hStdin = GetStdHandle(STD_INPUT_HANDLE);
            uint mode;
            GetConsoleMode(hStdin, out mode);
            mode &= ~ENABLE_QUICK_EDIT_MODE;//移除快速编辑模式
            mode &= ~ENABLE_INSERT_MODE;      //移除插入模式
            SetConsoleMode(hStdin, mode);
        }
        #endregion

        #region 捕捉退出信号
        //导入SetCtrlHandlerHandler API
        [DllImport("kernel32.dll")]
        public static extern bool SetConsoleCtrlHandler(ConsoleCtrlDelegate HandlerRoutine, bool Add);
        //当用户关闭Console时，系统会发送次消息
        private const int CTRL_CLOSE_EVENT = 2;
        //Ctrl+C，系统会发送次消息
        private const int CTRL_C_EVENT = 0;
        //Ctrl+break，系统会发送次消息
        private const int CTRL_BREAK_EVENT = 1;
        //用户退出（注销），系统会发送次消息
        private const int CTRL_LOGOFF_EVENT = 5;
        //系统关闭，系统会发送次消息
        private const int CTRL_SHUTDOWN_EVENT = 6;

        public static List<int> CLOSE_LIST = new List<int>();


        /// <summary>
        /// 处理程序例程，在这里编写对指定事件的处理程序代码
        /// 注意：在VS中调试执行时，在这里设置断点，但不会中断；会提示：无可用源；
        /// </summary>
        /// <param name="CtrlType"></param>
        /// <returns></returns>
        public static bool HandlerRoutine(int ctrlType)
        {
            if (CLOSE_LIST.Contains(ctrlType))
            {
                Console.WriteLine(string.Format("已禁用手动退出信号<{0}>！", ctrlType));
                return true;
            }
            return false;//忽略处理，让系统进行默认操作
        }
        #endregion

    }
}
