using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SudoLib.Win32;


namespace SudoLib
{
    /// <summary>
    /// Run a program as any user
    /// </summary>
    public class Sudo : IDisposable
    {
        private DeepDarkWin32Fantasy.PROCESS_INFORMATION _pi;
        private DeepDarkWin32Fantasy.STARTUPINFO _si;
        private Advapi32.SECURITY_ATTRIBUTES _saProcessAttributes;
        private Advapi32.SECURITY_ATTRIBUTES _saThreadAttributes;

        public int ChildProcessId => _pi.dwProcessId;

        private System.Threading.Thread m_hThread; // thread to receive the output of the child process
        private IntPtr m_hEvtStop; // event to notify the redir thread to exit
        private Int32 m_dwThreadId; // id of the redir thread
        private Int32 m_resultCode; // returned result code of the process

        protected IntPtr m_hStdinWrite; // write end of child's stdin pipe
        protected IntPtr m_hStdoutRead; // read end of child's stdout pipe
        protected IntPtr m_hStderrRead; // read end of child's stderr pipe
        protected IntPtr m_hChildProcess;

        private readonly SudoConfig _config;

        public Sudo(SudoConfig config)
        {
            _config = config;

            // fix config for values left out by the caller

            if (_config.ExtendProgramPath)
            {
                var fi = new FileInfo(_config.Program);
                _config.Program = fi.FullName;
            }

            if (!_config.ArgumentsCameWithProgramName)
            {
                // _config.Arguments = $"\"{Path.GetFileName(_config.Program)}\" {config.Arguments}";
                _config.Arguments = $"{_config.Program} {config.Arguments}";
            }

            // initialize internal objects
            _saProcessAttributes.nLength = Marshal.SizeOf(_saProcessAttributes);
            _saThreadAttributes.nLength = Marshal.SizeOf(_saThreadAttributes);
            _si.cb = Marshal.SizeOf(_si);
            m_hStdinWrite = IntPtr.Zero;
            m_hStdoutRead = IntPtr.Zero;
            m_hStderrRead = IntPtr.Zero;
            m_hChildProcess = IntPtr.Zero;
            m_hThread = null;
            m_hEvtStop = IntPtr.Zero;
            m_dwThreadId = 0;
            m_resultCode = -1;
        }

        public Sudo(string program, string args): this(new SudoConfig
        {
            Program = program,
            Arguments = args
        }) {}

        public Sudo(string program): this(program, "") { }

        public Sudo(Sudo sudo): this(sudo._config) { }

        ~Sudo()
        {
            // Finalizer calls Dispose(false)
            Dispose(false);
        }

        #region Dispose
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // The bulk of the clean-up code is implemented in Dispose(bool)
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // free managed resources
            }

            // free native resources if there are any.
            Close(true);
        }
        #endregion

        public Int32 ResultCode => m_resultCode;

        #region protected DestroyHandle
        protected void DestroyHandle(ref IntPtr rhObject)
        {
            if (rhObject == IntPtr.Zero)
                return;

            Kernel32.CloseHandle(rhObject);
            rhObject = IntPtr.Zero;
        }
        #endregion

        #region private InternalClose
        private void InternalClose(bool getExitCode)
        {
            if (getExitCode && m_hChildProcess != IntPtr.Zero)
            {
                // Snag the exit code before it's gone
                if (!Kernel32.GetExitCodeProcess(m_hChildProcess, out m_resultCode))
                {
                    m_resultCode = -1;
                }
            }

            DestroyHandle(ref m_hEvtStop);
            DestroyHandle(ref m_hChildProcess);
            DestroyHandle(ref m_hStdinWrite);
            DestroyHandle(ref m_hStdoutRead);
            DestroyHandle(ref m_hStderrRead);
            m_dwThreadId = 0;
        }
        #endregion

        #region Stdout/Stderr redirection processing
        delegate void RedirectDelegate(string msg);
        int InternalRedirect(IntPtr hPipeRead, RedirectDelegate del)
        {
            const int ERROR_BROKEN_PIPE = 109;
            const int ERROR_NO_DATA = 232;

            for (; ; )
            {
                uint bytesRead = 0;
                uint dwAvail = 0;
                uint bytesLeft = 0;
                if (!Kernel32.PeekNamedPipe(hPipeRead, null, 0, ref bytesRead, ref dwAvail, ref bytesLeft))
                    break; // error

                if (dwAvail == 0)
                {
                    // no data available
                    return 1;
                }

                byte[] szOutput = new byte[dwAvail];
                if (!Kernel32.ReadFile(hPipeRead, szOutput, dwAvail, out bytesRead, IntPtr.Zero) || bytesRead == 0)
                    break; // error, the child might have ended

                del(ASCIIEncoding.ASCII.GetString(szOutput, 0, (int)bytesRead));
            }

            int dwError = Marshal.GetLastWin32Error();
            if (dwError == ERROR_BROKEN_PIPE ||	// pipe has been ended
                dwError == ERROR_NO_DATA)		// pipe closing in progress
            {
                return 0;	// child process ended
            }

            WriteStdError("Read stdout pipe error\r\n");
            return -1;		// os error
        }

        // redirect the child process's stdout:
        // return: 1: no more data, 0: child terminated, -1: os error
        protected int RedirectStdout()
        {
            return InternalRedirect(m_hStdoutRead, new RedirectDelegate(delegate (string msg) { WriteStdOut(msg); }));
        }

        // redirect the child process's stderr:
        // return: 1: no more data, 0: child terminated, -1: os error
        protected int RedirectStderr()
        {
            return InternalRedirect(m_hStderrRead, new RedirectDelegate(delegate (string msg) { WriteStdError(msg); }));
        }

        protected void OutputThread()
        {
            const int WAIT_OBJECT_0 = 0;

            IntPtr[] aHandles = new IntPtr[2];
            aHandles[0] = m_hChildProcess;
            aHandles[1] = m_hEvtStop;

            bool exitNormally = false;
            for (; ; )
            {
                // redirect stdout till there's no more data.
                int nRet;

                nRet = RedirectStdout();
                if (nRet < 0)
                    break;

                nRet = RedirectStderr();
                if (nRet < 0)
                    break;

                // check if the child process has terminated.
                int dwRc = Kernel32.WaitForMultipleObjects(2, aHandles, false, _config.EventWaitTimeMs);
                if (WAIT_OBJECT_0 == dwRc)		// the child process ended
                {
                    RedirectStdout();
                    RedirectStderr();
                    exitNormally = true;
                    break;
                }

                if (WAIT_OBJECT_0 + 1 == dwRc)	// m_hEvtStop was signaled
                {
                    Kernel32.TerminateProcess(m_hChildProcess, 0xFFFFFFFF);
                    break;
                }
            }

            // close handles
            InternalClose(exitNormally);
        }
        #endregion

        #region virtual WriteStdOut
        /// <summary>
        /// Override to handle processing data written to stdout by the process
        /// </summary>
        /// <param name="outputStr"></param>
        protected virtual void WriteStdOut(string outputStr)
        {
            Console.Out.Write(outputStr);
        }
        #endregion

        #region virtual WriteStdError
        /// <summary>
        /// Override to handle processing data written to stderr by the process
        /// </summary>
        /// <param name="errorStr"></param>
        protected virtual void WriteStdError(string errorStr)
        {
            Console.Error.Write(errorStr);
        }
        #endregion

        #region Close
        /// <summary>
        /// Close a process
        /// </summary>
        /// <param name="abort">If true, abort the processing (waiting up to
        /// 5 seconds before terminating), otherwise just wait until it exits</param>
        public virtual void Close(bool abort)
        {
            if (m_hThread != null)
            {
                if (abort)
                {
                    // tell the thread to bail
                    Kernel32.SetEvent(m_hEvtStop);
                    if (!m_hThread.Join(_config.QuitWaitTimeMs))
                    {
                        try
                        {
                            m_hThread.Abort();
                        }
                        catch
                        {
                        }
                    }
                }
                else
                {
                    // wait until the thread exits
                    m_hThread.Join();
                }

                m_hThread = null;
            }

            InternalClose(false);
        }
        #endregion

        #region SendToStdIn
        /// <summary>
        /// Send the given string of data to the stdin of the spawned process
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public bool SendToStdIn(string str)
        {
            if (m_hStdinWrite == IntPtr.Zero)
                return false;

            byte[] strData = ASCIIEncoding.ASCII.GetBytes(str);

            uint dwWritten;
            return Kernel32.WriteFile(m_hStdinWrite, strData, (uint)strData.Length, out dwWritten, IntPtr.Zero);
        }
        #endregion

        public void Start()
        {
            Close(true);

            IntPtr hStdoutReadTmp = IntPtr.Zero; // parent stdout read handle
            IntPtr hStderrReadTmp = IntPtr.Zero; // parent stderr read handle
            IntPtr hStdoutWrite = IntPtr.Zero; // child stdout write handle
            IntPtr hStderrWrite = IntPtr.Zero;
            IntPtr hStdinWriteTmp = IntPtr.Zero; // parent stdin write handle
            IntPtr hStdinRead = IntPtr.Zero; // child stdin read handle

            m_resultCode = -1;

            var sa = new Advapi32.SECURITY_ATTRIBUTES();
            sa.nLength = Marshal.SizeOf(sa);
            sa.lpSecurityDescriptor = 0;
            sa.bInheritHandle = 1;

            bool setupOk = false;
            try
            {
                // Create a child stdout pipe.
                if (!Kernel32.CreatePipe(out hStdoutReadTmp, out hStdoutWrite, ref sa, 0))
                    throw new Win32Exception();

                // Create a child stderr pipe.
                if (!Kernel32.CreatePipe(out hStderrReadTmp, out hStderrWrite, ref sa, 0))
                    throw new Win32Exception();

                // Create a child stdin pipe.
                if (!Kernel32.CreatePipe(out hStdinRead, out hStdinWriteTmp, ref sa, 0))
                    throw new Win32Exception();

                // Create new stdout read handle, stderr read handle and the stdin write handle.
                // Set the inheritance properties to FALSE. Otherwise, the child
                // inherits the these handles; resulting in non-closeable
                // handles to the pipes being created.
                if (!Kernel32.DuplicateHandle(Kernel32.GetCurrentProcess(), hStdoutReadTmp, Kernel32.GetCurrentProcess(), out m_hStdoutRead, 0, false, (uint)DeepDarkWin32Fantasy.DuplicateOptions.DUPLICATE_SAME_ACCESS))
                    throw new Win32Exception();

                if (!Kernel32.DuplicateHandle(Kernel32.GetCurrentProcess(), hStderrReadTmp, Kernel32.GetCurrentProcess(), out m_hStderrRead, 0, false, (uint)DeepDarkWin32Fantasy.DuplicateOptions.DUPLICATE_SAME_ACCESS))
                    throw new Win32Exception();

                if (!Kernel32.DuplicateHandle(Kernel32.GetCurrentProcess(), hStdinWriteTmp, Kernel32.GetCurrentProcess(), out m_hStdinWrite, 0, false, (uint)DeepDarkWin32Fantasy.DuplicateOptions.DUPLICATE_SAME_ACCESS))
                    throw new Win32Exception();

                // Close inheritable copies of the handles we do not want to be inherited.
                DestroyHandle(ref hStdoutReadTmp);
                DestroyHandle(ref hStderrReadTmp);
                DestroyHandle(ref hStdinWriteTmp);

                // Set up the start up info struct.
                _si.hStdOutput = hStdoutWrite;
                _si.hStdInput = hStdinRead;
                _si.hStdError = hStderrWrite;
                _si.wShowWindow = (short)_config.WindowMode;
                _si.dwFlags = (int)(DeepDarkWin32Fantasy.STARTF.STARTF_USESTDHANDLES | DeepDarkWin32Fantasy.STARTF.STARTF_USESHOWWINDOW);

                // Note that dwFlags must include STARTF_USESHOWWINDOW if we
                // use the wShowWindow flags. This also assumes that the
                // CreateProcess() call will use CREATE_NEW_CONSOLE.

                if (false)
                {
                    // we are not impersonating anyone
                    if (!Kernel32.CreateProcess(
                        _config.Program,
                        _config.Arguments,
                        IntPtr.Zero,
                        IntPtr.Zero,
                        true,
                        DeepDarkWin32Fantasy.CREATE_NEW_CONSOLE,
                        IntPtr.Zero,
                        _config.WorkingDirectory,
                        ref _si,
                        out _pi
                    ))
                    {
                        throw new Win32Exception();
                    }
                }
                else
                {
                    //Console.WriteLine(WindowsIdentity.GetCurrent().Name);
                    //using (Impersonation imp = new Impersonation(BuiltinUser.NetworkService))
                    //{
                    //    Console.WriteLine(WindowsIdentity.GetCurrent().Name);

                    //    if (!Advapi32.CreateProcessAsUserW(
                    //        _config.WindowsIdentity.Token,
                    //        _config.Program,
                    //        _config.Arguments,
                    //        ref _saProcessAttributes,
                    //        ref _saThreadAttributes,
                    //        true,
                    //        DeepDarkWin32Fantasy.CREATE_NEW_CONSOLE,
                    //        IntPtr.Zero,
                    //        string.Empty,
                    //        ref _si,
                    //        out _pi
                    //    ))
                    //    {
                    //        throw new Win32Exception();
                    //    }
                    //}

                    Debug.WriteLine(Environment.UserName);

                    var currentProcess = new CProcess();
                    if (!currentProcess.SetPrivilege("SeTcbPrivilege", true))
                    {
                        throw new InvalidOperationException("Required privilege SeTcbPrivilege failed");
                    }
                    if (!currentProcess.SetPrivilege("SeDelegateSessionUserImpersonatePrivilege", true))
                    {
                        throw new InvalidOperationException("Required privilege SeDelegateSessionUserImpersonatePrivilege failed");
                    }

                    var newWindowsIdentity = new WindowsIdentity(_config.UserName);
                    if (newWindowsIdentity.ImpersonationLevel != TokenImpersonationLevel.Impersonation)
                    {
                        throw new InvalidOperationException("Insufficient permission");
                    }

                    WindowsIdentity.RunImpersonated(newWindowsIdentity.AccessToken, () =>
                    {
                        if (!Advapi32.CreateProcessAsUser(
                            newWindowsIdentity.Token,
                            _config.Program,
                            _config.Arguments,
                            ref _saProcessAttributes,
                            ref _saThreadAttributes,
                            true,
                            DeepDarkWin32Fantasy.CREATE_NEW_CONSOLE,
                            IntPtr.Zero,
                            string.Empty,
                            ref _si,
                            out _pi
                        ))
                        {
                            throw new Win32Exception();
                        }
                    });
                }

                m_hChildProcess = _pi.hProcess;

                // Close any non-useful handles
                Kernel32.CloseHandle(_pi.hThread);

                // Child is launched. Close the parents copy of those pipe
                // handles that only the child should have open.
                // Make sure that no handles to the write end of the stdout pipe
                // are maintained in this process or else the pipe will not
                // close when the child process exits and ReadFile will hang.
                DestroyHandle(ref hStdoutWrite);
                DestroyHandle(ref hStdinRead);
                DestroyHandle(ref hStderrWrite);

                // Launch a thread to receive output from the child process.
                m_hEvtStop = Kernel32.CreateEvent(IntPtr.Zero, true, false, null);

                try
                {
                    m_hThread = new System.Threading.Thread(new System.Threading.ThreadStart(OutputThread))
                    {
                        Name = "StdOutErr Processor " + _config.Program
                    };
                }
                catch
                {
                    throw new Win32Exception();
                }
                m_hThread.Start();
                m_dwThreadId = m_hThread.ManagedThreadId;
                setupOk = true;
            }
            finally
            {
                if (!setupOk)
                {
                    Int32 dwOsErr = Marshal.GetLastWin32Error();
                    if (dwOsErr != 0)
                    {
                        WriteStdError("Redirect console error: " + dwOsErr.ToString("x8") + "\r\n");
                    }

                    DestroyHandle(ref hStdoutReadTmp);
                    DestroyHandle(ref hStderrReadTmp);
                    DestroyHandle(ref hStdoutWrite);
                    DestroyHandle(ref hStderrWrite);
                    DestroyHandle(ref hStdinWriteTmp);
                    DestroyHandle(ref hStdinRead);
                    Close(true);

                    Kernel32.SetLastError(dwOsErr);
                }
            }

        }

    }
}
