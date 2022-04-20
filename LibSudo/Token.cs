using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using LibSudo.Win32;

namespace LibSudo
{
    public static class Token
    {
        public static DeepDarkWin32Fantasy.SafeTokenHandle GetProcessToken(IntPtr processHandle)
        {
            // http://lumineerlabs.com/user-impersonation-in-c
            ExceptionHelper.Check(Advapi32.OpenProcessToken(
                processHandle,
                (uint)(TokenAccessLevels.AllAccess),
                out var ret
            ));
            return ret;
        }

        public static DeepDarkWin32Fantasy.SafeTokenHandle GetSelfProcessToken()
        {
            return GetProcessToken(Process.GetCurrentProcess().Handle);
        }

        public static DeepDarkWin32Fantasy.SafeTokenHandle Copy(DeepDarkWin32Fantasy.SafeTokenHandle baseToken)
        {
            var securityAttributes = new Advapi32.SECURITY_ATTRIBUTES
            {
                bInheritHandle = 1,
                nLength = Marshal.SizeOf(typeof(Advapi32.SECURITY_ATTRIBUTES)),
                lpSecurityDescriptor = 0,
            };
            // https://stackoverflow.com/a/33765296
            ExceptionHelper.Check(Advapi32.DuplicateTokenEx(
                baseToken,
                (uint)(TokenAccessLevels.AllAccess),
                ref securityAttributes,
                Advapi32.SECURITY_IMPERSONATION_LEVEL.SecurityImpersonation,
                Advapi32.TOKEN_TYPE.TokenPrimary,
                out var token
            ));
            return token;
        }

        public static DeepDarkWin32Fantasy.SafeTokenHandle Create()
        {
            return Copy(GetSelfProcessToken());
           
        }

        public static void Destroy(DeepDarkWin32Fantasy.SafeTokenHandle token)
        {

        }
    }
}
