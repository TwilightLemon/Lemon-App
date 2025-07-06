using LemonApp.Common.Configs;
using LemonApp.Common.Funcs;
using LemonApp.Common.WinAPI;
using System;
using System.Reflection;
using System.Threading;

namespace LemonApp;

internal class EntryPoint
{
    private static Mutex? _appMutex = null;
    public readonly static SettingsMgr<ProcessInfo> _procMgr =
                        new(typeof(ProcessInfo).Name,typeof(EntryPoint).Namespace!);
    static bool IsAppRunning()
    {
        _appMutex = new Mutex(false, Assembly.GetExecutingAssembly().GetName().Name+"_DEBUG", out bool firstInstant);
        return !firstInstant;
    }
    static void CallExistingInstance()
    {
        _procMgr.Load();
        var hwnd = _procMgr.Data.MainWindowHandle;
        if (hwnd !=0)
        {
            MsgInteraction.SendMsg(hwnd, MsgInteraction.SEND_SHOW);
        }
    }


    [STAThread]
    static void Main(string[] args)
    {
        try
        {
            if (IsAppRunning())
            {
                CallExistingInstance();
                return;
            }
            App app = new();
             app.Run();
        }
        catch { }
    }
}
