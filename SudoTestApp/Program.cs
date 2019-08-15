using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SudoLib;

namespace SudoTestApp
{
    class Program
    {
        static void Main(string[] args)
        {
            Sudo s;

            // test with current user
            s = new Sudo(new SudoConfig()
            {
                Program = "FakeTarget.exe",
                Arguments = "-help",
                UserName = "Administrator",
            });
            s.Start();
        }
    }
}
