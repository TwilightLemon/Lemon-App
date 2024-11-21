using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LemonApp.Common.Configs;

public class ProcessInfo
{
    public int MainWindowHandle { get; set; } = 0;
    public string InstancePid { get; set; } = string.Empty;
}
