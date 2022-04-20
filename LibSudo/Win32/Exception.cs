using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace LibSudo.Win32
{
    public static class ExceptionHelper
    {
        private static void _checkAndThrow()
        {
            //var error = Marshal.GetLastWin32Error();
            //var errorHR = Marshal.GetHRForLastWin32Error();
            //throw new Win32Exception(error, $"Win32 exception: {errorHR}");

            // actually, an empty Win32Exception will get the correct HRESULT.
            // Lookup HRESULT: https://errorcodelookup.com/
            throw new Win32Exception();
        }

        public static int Check(int ret)
        {
            if (ret != 0) _checkAndThrow();
            return ret;
        }

        public static bool Check(bool ret)
        {
            if (ret == false) _checkAndThrow();
            return ret;
        }
    }
}
