using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Ocansuit.Tools.WinAPI
{
    public class SendToWindow
    {
        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll", EntryPoint = "keybd_event", SetLastError = true)]
        public static extern void keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);
        [DllImport("user32.dll")]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr ProcessId);
        [DllImport("user32.dll")]
        static extern bool GetGUIThreadInfo(uint idThread, ref GUITHREADINFO lpgui);
        [StructLayout(LayoutKind.Sequential)]
        public struct GUITHREADINFO
        {
            public int cbSize;
            public int flags;
            public IntPtr hwndActive;
            public IntPtr hwndFocus;
            public IntPtr hwndCapture;
            public IntPtr hwndMenuOwner;
            public IntPtr hwndMoveSize;
            public IntPtr hwndCaret;
            public RECT rectCaret;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            int left;
            int top;
            int right;
            int bottom;
        }
        public GUITHREADINFO? GetGuiThreadInfo(IntPtr hwnd)
        {
            if (hwnd != IntPtr.Zero)
            {
                uint threadId = GetWindowThreadProcessId(hwnd, IntPtr.Zero);
                GUITHREADINFO guiThreadInfo = new GUITHREADINFO();
                guiThreadInfo.cbSize = Marshal.SizeOf(guiThreadInfo);
                if (GetGUIThreadInfo(threadId, ref guiThreadInfo) == false)
                    return null;
                return guiThreadInfo;
            }
            return null;
        }
        /// <summary>
        /// 向获得焦点的最前方窗口，发送字符串string
        /// </summary>
        /// <param name="text"></param>
        public void SendText(string text)
        {
            IntPtr hwnd = GetForegroundWindow();
            if (String.IsNullOrEmpty(text))
                return;
            GUITHREADINFO? guiInfo = GetGuiThreadInfo(hwnd);
            if (guiInfo != null)
            {
                for (int i = 0; i < text.Length; i++)
                {
                    SendMessage(guiInfo.Value.hwndFocus, 0x0102, (IntPtr)(int)text[i], IntPtr.Zero);

                }
                SendKeys.SendWait("{Enter}");
                SendKeys.Flush();
            }
        }
    }
}
