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
        
        // executable
        public string ExecutablePath { get; set; } // optional
        public string CommandLine { get; set; } = "%ComSpec% /K";

        // runtime information
        private Advapi32.SECURITY_ATTRIBUTES _saProcessAttributes;
        private Advapi32.SECURITY_ATTRIBUTES _saThreadAttributes;
        private IntPtr _lpEnvironmentBlock = IntPtr.Zero;
        private GCHandle? _environmentBlockGcHandle = null;
        private DeepDarkWin32Fantasy.STARTUPINFO _startupInfo;
        private DeepDarkWin32Fantasy.PROCESS_INFORMATION _processInfo;
        public uint ExitCode => _exitCode;
        private uint _exitCode;

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

            // default window show/hide/maximize
            // set _startupInfo.wShowWindow and startup_info.dwFlags != STARTF_USESHOWWINDOW

            // create CreationFlags
            // https://docs.microsoft.com/en-us/windows/win32/procthread/process-creation-flags
            if (CreationFlags == 0)
            {
                CreationFlags = DeepDarkWin32Fantasy.CREATE_UNICODE_ENVIRONMENT |
                                DeepDarkWin32Fantasy.CREATE_DEFAULT_ERROR_MODE;

                if (NewConsole)
                {
                    CreationFlags |= DeepDarkWin32Fantasy.CREATE_NEW_CONSOLE;
                }
                else
                {
                    CreationFlags |= DeepDarkWin32Fantasy.CREATE_NO_WINDOW;
                }
            }

            // basic token
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
            
            // create the process
            try
            {
                
                if (!Advapi32.CreateProcessAsUser(
                        token.DangerousGetHandle(),
                        ExecutablePath,
                        CommandLine,
                        ref _saProcessAttributes,
                        ref _saThreadAttributes,
                        false,
                        CreationFlags,
                        CreateLpEnvironment(),
                        WorkingDirectory,
                        ref _startupInfo,
                        out _processInfo
                    ))
                {
                    throw new Win32Exception();
                }
            }
            finally
            {
                FreeLpEnvironment();
                Kernel32.FreeConsole();
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
            Advapi32.OpenProcessToken(processHandle, DeepDarkWin32Fantasy.TOKEN_QUERY, out var ret);
            return ret;
        }

        private DeepDarkWin32Fantasy.SafeTokenHandle CreateToken()
        {
            return GetProcessToken(Process.GetCurrentProcess().Handle);
        }

        private IntPtr CreateLpEnvironment()
        {
            if (_environmentBlockGcHandle != null)
            {
                FreeLpEnvironment();
            }

            // concat string
            // https://stackoverflow.com/a/25400104
            var sb = new StringBuilder();
            foreach (var key in Environment.Keys)
            {
                if (!(key is string keyString)) continue;
                sb.Append(keyString);
                sb.Append("=");
                sb.Append(Environment[keyString]);
                sb.Append(0);
            }
            sb.Append(0);

            // https://stackoverflow.com/a/8855252
            _lpEnvironmentBlock = Marshal.StringToCoTaskMemUni(sb.ToString());
            _environmentBlockGcHandle = GCHandle.Alloc(_lpEnvironmentBlock, GCHandleType.Pinned);
            return _lpEnvironmentBlock;
        }

        private void FreeLpEnvironment()
        {
            _environmentBlockGcHandle?.Free();
            _environmentBlockGcHandle = null;

            if (_lpEnvironmentBlock != IntPtr.Zero)
            {
                Marshal.FreeCoTaskMem(_lpEnvironmentBlock);
            }
        }
    }
}
