using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Collections;
using System.ComponentModel;
using System.Runtime;
using System.Runtime.InteropServices;
using LibSudo.Win32;
using System.Collections.Specialized;
using System.Security.Permissions;
using System.Security.Principal;
using System.Threading;

namespace LibSudo
{
    public class Sudo : IDisposable
    {
        // invocation parameters
        public string DACL { get; set; } = "G:BA";
        public string Desktop { get; set; } = null;
        public uint PrivilegeAvailable { get; set; } = 0xFFFFFFFE;
        public uint PrivilegeEnabled { get; set; } = 0xFFFFFFFE;
        public string IntegrityLevel { get; set; } = "UT";
        public int IntegrityMandatoryPolicy { get; set; } = 0;
        public string UserSID { get; set; }
        public StringDictionary GroupSIDs { get; set; }
        public int SessionId { get; set; } = -1;
        public bool NewConsole { get; set; } = false;
        public uint CreationFlags { get; private set; } = 0;
        public string WorkingDirectory { get; set; } = Directory.GetCurrentDirectory();
        public StringDictionary Environment { get; set; } = Process.GetCurrentProcess().StartInfo.EnvironmentVariables;
        public bool UnicodeEnvironment { get; set; } = true;
        
        // executable
        public string ExecutablePath { get; set; } // optional
        public string CommandLine { get; set; } = "%ComSpec% /K";

        // runtime information
        private Advapi32.SECURITY_ATTRIBUTES _saProcessAttributes;
        private Advapi32.SECURITY_ATTRIBUTES _saThreadAttributes;
        private DeepDarkWin32Fantasy.STARTUPINFO _startupInfo;
        private DeepDarkWin32Fantasy.PROCESS_INFORMATION _processInfo;
        public uint ExitCode => _exitCode;
        private uint _exitCode;

        public static void ElevateSelf()
        {
            var currentProcess = new CProcess();
            if (!currentProcess.SetPrivilege("SeTcbPrivilege", true))
            {
                throw new InvalidOperationException("Required privilege SeTcbPrivilege failed");
            }
            if (!currentProcess.SetPrivilege("SeDelegateSessionUserImpersonatePrivilege", true))
            {
                throw new InvalidOperationException("Required privilege SeDelegateSessionUserImpersonatePrivilege failed");
            }

            // Windows 2000 compat
            // See:
            // http://lumineerlabs.com/user-impersonation-in-c
            // https://docs.microsoft.com/en-us/windows/security/threat-protection/security-policy-settings/adjust-memory-quotas-for-a-process
            //if (!currentProcess.SetPrivilege("SeIncreaseQuotaPrivilege", true))
            //{
            //    throw new InvalidOperationException("Required privilege SeIncreaseQuotaPrivilege failed");
            //}
        }

        public void Start()
        {
            // create StartupInfo
            _startupInfo = new DeepDarkWin32Fantasy.STARTUPINFO
            {
                cb = Marshal.SizeOf(typeof(DeepDarkWin32Fantasy.STARTUPINFO)),
                cbReserved2 = 0,
                lpDesktop = Desktop,
                lpTitle = null,
                lpReserved = null,
                lpReserved2 = IntPtr.Zero,
                dwFlags = 0,
            };

            // create _saProcessAttributes
            _saProcessAttributes = new Advapi32.SECURITY_ATTRIBUTES
            {
                nLength = Marshal.SizeOf(typeof(Advapi32.SECURITY_ATTRIBUTES)),
            };

            // create _saThreadAttributes
            _saThreadAttributes = new Advapi32.SECURITY_ATTRIBUTES
            {
                nLength = Marshal.SizeOf(typeof(Advapi32.SECURITY_ATTRIBUTES)),
            };

            // default window show/hide/maximize
            // set _startupInfo.wShowWindow and startup_info.dwFlags != STARTF_USESHOWWINDOW

            // create CreationFlags
            // https://docs.microsoft.com/en-us/windows/win32/procthread/process-creation-flags
            if (CreationFlags == 0)
            {
                CreationFlags = DeepDarkWin32Fantasy.CREATE_DEFAULT_ERROR_MODE;

                if (UnicodeEnvironment)
                {
                    CreationFlags |= DeepDarkWin32Fantasy.CREATE_UNICODE_ENVIRONMENT;
                }

                if (NewConsole)
                {
                    CreationFlags |= DeepDarkWin32Fantasy.CREATE_NEW_CONSOLE;
                }
                else
                {
                    CreationFlags |= DeepDarkWin32Fantasy.CREATE_NO_WINDOW;
                }
            }

            // token
            var token = CreateToken();

            // mandatory policy

            // additional groups

            // users

            // session ID
            if (SessionId == -1)
            {
                SessionId = Process.GetCurrentProcess().SessionId;
            }

            if (SessionId == -1)
            {
                SessionId = 0;
            }

            // environment
            var environmentBlock = EnvironmentBlock.NewFromEmpty(UnicodeEnvironment);

            // create the process
            try
            {
                ExceptionHelper.Check(Advapi32.CreateProcessAsUser(
                    token,
                    ExecutablePath,
                    CommandLine,
                    ref _saProcessAttributes,
                    ref _saThreadAttributes,
                    false,
                    CreationFlags,
                    environmentBlock,
                    WorkingDirectory,
                    ref _startupInfo,
                    out _processInfo
                ));
            }
            finally
            {
                DestroyToken(token);
                EnvironmentBlock.Free(environmentBlock);
                //Kernel32.FreeConsole();
            }
        }

        public uint Wait()
        {
            Kernel32.WaitForSingleObjectEx(_processInfo.hProcess, DeepDarkWin32Fantasy.INFINITE, false);
            Kernel32.GetExitCodeProcess(_processInfo.hProcess, out _exitCode);
            return _exitCode;
        }

        public void Dispose()
        {

        }

        private DeepDarkWin32Fantasy.SafeTokenHandle GetProcessToken(IntPtr processHandle)
        {
            // http://lumineerlabs.com/user-impersonation-in-c
            ExceptionHelper.Check(Advapi32.OpenProcessToken(
                processHandle,
                (uint)(TokenAccessLevels.AllAccess),
                out var ret
                ));
            return ret;
        }

        private DeepDarkWin32Fantasy.SafeTokenHandle CreateToken()
        {
            var baseToken = GetProcessToken(Process.GetCurrentProcess().Handle);
            var securityAttributes = new Advapi32.SECURITY_ATTRIBUTES()
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

        private void DestroyToken(DeepDarkWin32Fantasy.SafeTokenHandle token)
        {

        }
    }
}
