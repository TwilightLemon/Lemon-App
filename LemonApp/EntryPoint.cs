using LemonApp.Common.Configs;
using LemonApp.Common.Funcs;
using System;
using System.Reflection;
using System.Threading;

namespace LemonApp;

internal class EntryPoint
{
    static Mutex? _appMutex = null;
    static bool IsAppRunning()
    {
        _appMutex = new Mutex(false, Assembly.GetExecutingAssembly().GetName().Name+"_DEBUG", out bool firstInstant);
        return !firstInstant;
    }


    [STAThread]
    static void Main(string[] args)
    {
        if (IsAppRunning())
        {
            return;
        }
        App app = new();
        app.Run();
    }
}
