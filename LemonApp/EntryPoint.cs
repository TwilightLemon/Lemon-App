using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LemonApp;

internal class EntryPoint
{

    static Mutex? _appMutex = null;
    static bool IsAppRunning()
    {
        _appMutex = new Mutex(false, Assembly.GetExecutingAssembly().GetName().Name, out bool firstInstant);
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
