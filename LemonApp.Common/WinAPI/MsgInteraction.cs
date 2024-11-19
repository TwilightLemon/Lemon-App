using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace LemonApp.Common.WinAPI;

public class MsgInteraction
{
    public const int WM_COPYDATA = 0x004A;
    public const string SEND_SHOW = "SEND_SHOW";

    [StructLayout(LayoutKind.Sequential)]
    public struct COPYDATASTRUCT
    {
        public IntPtr dwData;
        public int cbData;
        public IntPtr lpData;
    }

    [DllImport("User32.dll")]
    public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, ref COPYDATASTRUCT lParam);

    public static bool SendMsg(IntPtr hWnd, string msg)
    {
        var cds = new COPYDATASTRUCT
        {
            dwData = IntPtr.Zero,
            cbData = Encoding.Default.GetBytes(msg).Length + 1,
            lpData = Marshal.StringToHGlobalAnsi(msg)
        };
        var result = SendMessage(hWnd, WM_COPYDATA, 0, ref cds);
        Marshal.FreeHGlobal(cds.lpData);
        return result != 0;
    }

    public static string HandleMsg(IntPtr lParam)
    {
        var cds = Marshal.PtrToStructure<COPYDATASTRUCT>(lParam);
        var msg = Marshal.PtrToStringAnsi(cds.lpData, cds.cbData);
        return msg;
    }
}
