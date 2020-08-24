using System;
using System.Diagnostics;
using System.Linq;

namespace Snoop.CLI
{
    class Program
    {
        static void Main(string[] args)
        {
            var p = Process.GetProcessesByName("MaterialDesignDemo").Single();

            Interactive.Interactive.Start(p.Id);
        }
    }
}
