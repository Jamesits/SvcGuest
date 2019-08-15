using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

// ReSharper disable UnusedMember.Local
// ReSharper disable MemberHidesStaticFromOuterClass
// ReSharper disable CommentTypo

// ReSharper disable InconsistentNaming
// ReSharper disable IdentifierTypo
// ReSharper disable UnusedMember.Global
// ReSharper disable BuiltInTypeReferenceStyle
// ReSharper disable StringLiteralTypo

namespace SudoLib.Win32
{
    public class DeepDarkWin32Fantasy
    {
        #region Structs

        [StructLayout(LayoutKind.Sequential)]
        internal struct ServiceStatus
        {
            public int dwServiceType;
            public DeepDarkWin32Fantasy.ServiceState dwCurrentState;
            public int dwControlsAccepted;
            public int dwWin32ExitCode;
            public int dwServiceSpecificExitCode;
            public int dwCheckPoint;
            public int dwWaitHint;
        };
        public struct LUID
        {
            public int LowPart;
            public int HighPart;
        }
        public struct LUID_AND_ATTRIBUTES
        {
            public LUID Luid;
            public int Attributes;
        }

        public struct TOKEN_PRIVILEGES
        {
            public int PrivilegeCount;
            public LUID_AND_ATTRIBUTES Privileges;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct IMAGE_DOS_HEADER
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public char[] e_magic;
            public ushort e_cblp;
            public ushort e_cp;
            public ushort e_crlc;
            public ushort e_cparhdr;
            public ushort e_minalloc;
            public ushort e_maxalloc;
            public ushort e_ss;
            public ushort e_sp;
            public ushort e_csum;
            public ushort e_ip;
            public ushort e_cs;
            public ushort e_lfarlc;
            public ushort e_ovno;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public ushort[] e_res1;
            public ushort e_oemid;
            public ushort e_oeminfo;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
            public ushort[] e_res2;
            public uint e_lfanew;

            private string _e_magic => new string(e_magic);

            public bool isValid => _e_magic == "MZ";
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct STARTUPINFO
        {
            public int cb;
            public string lpReserved;
            public string lpDesktop;
            public string lpTitle;
            public int dwX;
            public int dwY;
            public int dwXSize;
            public int dwYSize;
            public int dwXCountChars;
            public int dwYCountChars;
            public int dwFillAttribute;
            public int dwFlags;
            public Int16 wShowWindow;
            public Int16 cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        public struct STARTUPINFOEX
        {
#pragma warning disable CS0649
            public STARTUPINFO StartupInfo;
            public IntPtr lpAttributeList;
#pragma warning restore CS0649
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public int dwProcessId;
            public int dwThreadId;
        }

        /// <summary>
        /// The structure represents a security identifier (SID) and its 
        /// attributes. SIDs are used to uniquely identify users or groups.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct SID_AND_ATTRIBUTES
        {
            public IntPtr Sid;
            public UInt32 Attributes;
        }

        /// <summary>
        /// The structure indicates whether a token has elevated privileges.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct TOKEN_ELEVATION
        {
            public Int32 TokenIsElevated;
        }

        /// <summary>
        /// The structure specifies the mandatory integrity level for a token.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct TOKEN_MANDATORY_LABEL
        {
            public SID_AND_ATTRIBUTES Label;
        }

        #endregion Structs

        #region enums

        internal enum ServiceState
        {
            // ReSharper disable InconsistentNaming
            // ReSharper disable UnusedMember.Global
            SERVICE_STOPPED = 0x00000001,
            SERVICE_START_PENDING = 0x00000002,
            SERVICE_STOP_PENDING = 0x00000003,
            SERVICE_RUNNING = 0x00000004,
            SERVICE_CONTINUE_PENDING = 0x00000005,
            SERVICE_PAUSE_PENDING = 0x00000006,
            SERVICE_PAUSED = 0x00000007,
            // ReSharper restore InconsistentNaming
            // ReSharper restore UnusedMember.Global
        }

        /// <summary>
        /// The TOKEN_INFORMATION_CLASS enumeration type contains values that 
        /// specify the type of information being assigned to or retrieved from 
        /// an access token.
        /// </summary>
        internal enum TOKEN_INFORMATION_CLASS
        {
            TokenUser = 1,
            TokenGroups,
            TokenPrivileges,
            TokenOwner,
            TokenPrimaryGroup,
            TokenDefaultDacl,
            TokenSource,
            TokenType,
            TokenImpersonationLevel,
            TokenStatistics,
            TokenRestrictedSids,
            TokenSessionId,
            TokenGroupsAndPrivileges,
            TokenSessionReference,
            TokenSandBoxInert,
            TokenAuditPolicy,
            TokenOrigin,
            TokenElevationType,
            TokenLinkedToken,
            TokenElevation,
            TokenHasRestrictions,
            TokenAccessInformation,
            TokenVirtualizationAllowed,
            TokenVirtualizationEnabled,
            TokenIntegrityLevel,
            TokenUIAccess,
            TokenMandatoryPolicy,
            TokenLogonSid,
            MaxTokenInfoClass
        }

        // Enumerated type for the control messages sent to the handler routine
        public enum CtrlTypes : uint
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT,
            CTRL_CLOSE_EVENT,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT
        }

        [Flags]
        public enum DuplicateOptions : uint
        {
            DUPLICATE_CLOSE_SOURCE = (0x00000001),// Closes the source handle. This occurs regardless of any error status returned.
            DUPLICATE_SAME_ACCESS = (0x00000002), //Ignores the dwDesiredAccess parameter. The duplicate handle has the same access as the source handle.
        }

        public enum ShowWindowCommands : uint
        {
            /// <summary>
            ///        Hides the window and activates another window.
            /// </summary>
            SW_HIDE = 0,

            /// <summary>
            ///        Activates and displays a window. If the window is minimized or maximized, the system restores it to its original size and position. An application should specify this flag when displaying the window for the first time.
            /// </summary>
            SW_SHOWNORMAL = 1,

            /// <summary>
            ///        Activates and displays a window. If the window is minimized or maximized, the system restores it to its original size and position. An application should specify this flag when displaying the window for the first time.
            /// </summary>
            SW_NORMAL = 1,

            /// <summary>
            ///        Activates the window and displays it as a minimized window.
            /// </summary>
            SW_SHOWMINIMIZED = 2,

            /// <summary>
            ///        Activates the window and displays it as a maximized window.
            /// </summary>
            SW_SHOWMAXIMIZED = 3,

            /// <summary>
            ///        Maximizes the specified window.
            /// </summary>
            SW_MAXIMIZE = 3,

            /// <summary>
            ///        Displays a window in its most recent size and position. This value is similar to <see cref="ShowWindowCommands.SW_SHOWNORMAL"/>, except the window is not activated.
            /// </summary>
            SW_SHOWNOACTIVATE = 4,

            /// <summary>
            ///        Activates the window and displays it in its current size and position.
            /// </summary>
            SW_SHOW = 5,

            /// <summary>
            ///        Minimizes the specified window and activates the next top-level window in the z-order.
            /// </summary>
            SW_MINIMIZE = 6,

            /// <summary>
            ///        Displays the window as a minimized window. This value is similar to <see cref="ShowWindowCommands.SW_SHOWMINIMIZED"/>, except the window is not activated.
            /// </summary>
            SW_SHOWMINNOACTIVE = 7,

            /// <summary>
            ///        Displays the window in its current size and position. This value is similar to <see cref="ShowWindowCommands.SW_SHOW"/>, except the window is not activated.
            /// </summary>
            SW_SHOWNA = 8,

            /// <summary>
            ///        Activates and displays the window. If the window is minimized or maximized, the system restores it to its original size and position. An application should specify this flag when restoring a minimized window.
            /// </summary>
            SW_RESTORE = 9
        }

        [Flags]
        public enum STARTF : uint
        {
            STARTF_USESHOWWINDOW = 0x00000001,
            STARTF_USESIZE = 0x00000002,
            STARTF_USEPOSITION = 0x00000004,
            STARTF_USECOUNTCHARS = 0x00000008,
            STARTF_USEFILLATTRIBUTE = 0x00000010,
            STARTF_RUNFULLSCREEN = 0x00000020,  // ignored for non-x86 platforms
            STARTF_FORCEONFEEDBACK = 0x00000040,
            STARTF_FORCEOFFFEEDBACK = 0x00000080,
            STARTF_USESTDHANDLES = 0x00000100,
        }

        #endregion

        #region constants

        // Token Specific Access Rights

        public const UInt32 STANDARD_RIGHTS_REQUIRED = 0x000F0000;
        public const UInt32 STANDARD_RIGHTS_READ = 0x00020000;
        public const UInt32 TOKEN_ASSIGN_PRIMARY = 0x0001;
        public const UInt32 TOKEN_DUPLICATE = 0x0002;
        public const UInt32 TOKEN_IMPERSONATE = 0x0004;
        public const UInt32 TOKEN_QUERY = 0x0008;
        public const UInt32 TOKEN_QUERY_SOURCE = 0x0010;
        public const UInt32 TOKEN_ADJUST_PRIVILEGES = 0x0020;
        public const UInt32 TOKEN_ADJUST_GROUPS = 0x0040;
        public const UInt32 TOKEN_ADJUST_DEFAULT = 0x0080;
        public const UInt32 TOKEN_ADJUST_SESSIONID = 0x0100;
        public const UInt32 TOKEN_READ = (STANDARD_RIGHTS_READ | TOKEN_QUERY);
        public const UInt32 TOKEN_ALL_ACCESS = (STANDARD_RIGHTS_REQUIRED |
                                                TOKEN_ASSIGN_PRIMARY | TOKEN_DUPLICATE | TOKEN_IMPERSONATE |
                                                TOKEN_QUERY | TOKEN_QUERY_SOURCE | TOKEN_ADJUST_PRIVILEGES |
                                                TOKEN_ADJUST_GROUPS | TOKEN_ADJUST_DEFAULT | TOKEN_ADJUST_SESSIONID);


        public const Int32 ERROR_INSUFFICIENT_BUFFER = 122;


        // Integrity Levels

        public const Int32 SECURITY_MANDATORY_UNTRUSTED_RID = 0x00000000;
        public const Int32 SECURITY_MANDATORY_LOW_RID = 0x00001000;
        public const Int32 SECURITY_MANDATORY_MEDIUM_RID = 0x00002000;
        public const Int32 SECURITY_MANDATORY_HIGH_RID = 0x00003000;
        public const Int32 SECURITY_MANDATORY_SYSTEM_RID = 0x00004000;

        // WaitForSingleObject
        public const UInt32 INFINITE = 0xFFFFFFFF;
        public const UInt32 WAIT_ABANDONED = 0x00000080;
        public const UInt32 WAIT_OBJECT_0 = 0x00000000;
        public const UInt32 WAIT_TIMEOUT = 0x00000102;

        // CreateProcess dwCreationFlag
        public const uint DEBUG_PROCESS = 0x00000001;
        public const uint DEBUG_ONLY_THIS_PROCESS = 0x00000002;
        public const uint CREATE_SUSPENDED = 0x00000004;
        public const uint DETACHED_PROCESS = 0x00000008;
        public const uint CREATE_NEW_CONSOLE = 0x00000010;
        public const uint NORMAL_PRIORITY_CLASS = 0x00000020;
        public const uint IDLE_PRIORITY_CLASS = 0x00000040;
        public const uint HIGH_PRIORITY_CLASS = 0x00000080;
        public const uint REALTIME_PRIORITY_CLASS = 0x00000100;
        public const uint CREATE_NEW_PROCESS_GROUP = 0x00000200;
        public const uint CREATE_UNICODE_ENVIRONMENT = 0x00000400;
        public const uint CREATE_SEPARATE_WOW_VDM = 0x00000800;
        public const uint CREATE_SHARED_WOW_VDM = 0x00001000;
        public const uint CREATE_FORCEDOS = 0x00002000;
        public const uint BELOW_NORMAL_PRIORITY_CLASS = 0x00004000;
        public const uint ABOVE_NORMAL_PRIORITY_CLASS = 0x00008000;
        public const uint INHERIT_PARENT_AFFINITY = 0x00010000;
        public const uint INHERIT_CALLER_PRIORITY = 0x00020000;
        public const uint CREATE_PROTECTED_PROCESS = 0x00040000;
        public const uint EXTENDED_STARTUPINFO_PRESENT = 0x00080000;
        public const uint PROCESS_MODE_BACKGROUND_BEGIN = 0x00100000;
        public const uint PROCESS_MODE_BACKGROUND_END = 0x00200000;
        public const uint CREATE_BREAKAWAY_FROM_JOB = 0x01000000;
        public const uint CREATE_PRESERVE_CODE_AUTHZ_LEVEL = 0x02000000;
        public const uint CREATE_DEFAULT_ERROR_MODE = 0x04000000;
        public const uint CREATE_NO_WINDOW = 0x08000000;
        public const uint PROFILE_USER = 0x10000000;
        public const uint PROFILE_KERNEL = 0x20000000;
        public const uint PROFILE_SERVER = 0x40000000;
        public const uint CREATE_IGNORE_SYSTEM_DEFAULT = 0x80000000;
        #endregion

        internal class SafeTokenHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            private SafeTokenHandle() : base(true)
            {
            }

            internal SafeTokenHandle(IntPtr handle) : base(true)
            {
                SetHandle(handle);
            }

            [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            internal static extern bool CloseHandle(IntPtr handle);

            protected override bool ReleaseHandle()
            {
                return CloseHandle(handle);
            }
        }


        // Delegate type to be used as the Handler Routine for SCCH
        public delegate Boolean ConsoleCtrlDelegate(CtrlTypes CtrlType);
        
    }
    internal class Kernel32
    {
        
        #region Functions
        [DllImport("kernel32.dll")]
        public static extern IntPtr CreateFileA(string lpFleName, uint dwDesiredAccess, int dwShareMode, IntPtr lpSecurityAttributes, int dwCreationDisposition, int dwFlagsAndAttributes, IntPtr hTemplateFile);
        [DllImport("kernel32.dll")]
        public static extern bool ReadFile(IntPtr hFile, IntPtr Buffer, int nNumberOfBytesToRead, ref int lpNumberOfBytesRead, IntPtr lpOverlapped);
        [DllImport("kernel32.dll")]
        public static extern bool CloseHandle(IntPtr hObject);
        [DllImport("kernel32.dll")]
        public static extern int GetFileSize(IntPtr hFile, IntPtr lpFileSizeHigh);
        [DllImport("kernel32.dll")]
        public static extern IntPtr VirtualAlloc(IntPtr lpAddress, int dwSize, int flAllocationType, int flProtect);
        [DllImport("kernel32.dll")]
        public static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, int dwSize, int flAllocationType, int flProtect);
        [DllImport("kernel32.dll")]
        public static extern bool VirtualFree(IntPtr lpAddress, int dwSize, int dwFreeType);
        [DllImport("kernel32.dll")]
        public static extern bool VirtualFreeEx(IntPtr hProcess, IntPtr lpAddress, int dwSize, int dwFreeType);
        [DllImport("kernel32.dll")]
        public static extern int WaitForSingleObject(IntPtr hObject, uint dwMilliseconds);
        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);
        [DllImport("kernel32.dll")]
        public static extern bool TerminateProcess(IntPtr hProcess, int ExitStatus);
        [DllImport("kernel32.dll")]
        public static extern bool TerminateThread(IntPtr hThread, int ExitStatus);
        [DllImport("kernel32.dll")]
        public static extern bool OpenProcessToken(IntPtr hProcess, uint DesiredAccess, ref IntPtr TokenHandle);
        [DllImport("KERNELBASE.dll")]
        public static extern bool AdjustTokenPrivileges(IntPtr TokenHandle, bool DisableAllPrivileges, ref DeepDarkWin32Fantasy.TOKEN_PRIVILEGES NewState, int BufferLength, IntPtr PreviousState, ref int ReturnLength);
        [DllImport("kernel32.dll")]
        public static extern bool QueryFullProcessImageNameA(IntPtr hProcess, int dwFlags, byte[] lpExeName, ref int lpdwSize);
        [DllImport("kernel32.dll")]
        public static extern IntPtr GetCurrentProcess();
        [DllImport("kernel32.dll")]
        public static extern int GetCurrentProcessId();
        [DllImport("kernel32.dll")]
        public static extern int GetProcessId(IntPtr hProcess);
        [DllImport("kernel32.dll")]
        public static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, IntPtr lpBuffer, int nSize, IntPtr lpNumberOfBytesWritten);
        [DllImport("kernel32.dll")]
        public static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);
        [DllImport("kernel32.dll")]
        public static extern IntPtr GetModuleHandleA(string lpModuleName);
        [DllImport("kernel32.dll")]
        public static extern Boolean CreateProcessAsUserA(IntPtr hToken, string lpApplicationName, string lpCommandLine, IntPtr lpProcessAttributes, IntPtr lpThreadAttributes, bool bInheritHandles, int dwCreationFlags, IntPtr lpEnvironment, string lpCurrentDirectory, ref DeepDarkWin32Fantasy.STARTUPINFOEX si, ref DeepDarkWin32Fantasy.PROCESS_INFORMATION pi);
        [DllImport("kernel32.dll")]
        public static extern void DeleteProcThreadAttributeList(IntPtr lpAttributeList);
        [DllImport("kernel32.dll")]
        public static extern bool InitializeProcThreadAttributeList(IntPtr lpAttributeList, int dwAttributeCount, int dwFlags, ref IntPtr lpSize);
        [DllImport("kernel32.dll")]
        public static extern bool UpdateProcThreadAttribute(IntPtr lpAttributeList, uint dwFlags, IntPtr Attribute, ref IntPtr lpValue, IntPtr cbSize, IntPtr lpPreviousValue, IntPtr lpReturnSize);
        [DllImport("kernel32.dll")]
        public static extern bool GetExitCodeThread(IntPtr hThread, out uint lpExitCode);
        [DllImport("kernel32.dll")]
        public static extern bool DuplicateHandle(IntPtr hSourceProcessHandle, IntPtr hSourceHandle, IntPtr TargetProcessHandle, ref IntPtr lpTargetHandle, int dwDesiredAccess, bool bInherithandle, int dwOptions);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool AttachConsole(uint dwProcessId);

        [DllImport("kernel32.dll")]
        public static extern bool SetConsoleCtrlHandler(DeepDarkWin32Fantasy.ConsoleCtrlDelegate HandlerRoutine, bool Add);

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GenerateConsoleCtrlEvent(DeepDarkWin32Fantasy.CtrlTypes dwCtrlEvent, uint dwProcessGroupId);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        public static extern bool FreeConsole();

        [DllImport("kernel32.dll")]
        public static extern Boolean CreatePipe(
            out IntPtr hReadPipe,
            out IntPtr hWritePipe,
            ref Advapi32.SECURITY_ATTRIBUTES lpPipeAttributes,
            uint nSize);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern Boolean DuplicateHandle(
            IntPtr hSourceProcessHandle,
            IntPtr hSourceHandle,
            IntPtr hTargetProcessHandle,
            out IntPtr lpTargetHandle,
            UInt32 dwDesiredAccess,
            [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle,
            UInt32 dwOptions);

        [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Auto)]
        public static extern IntPtr CreateEvent(IntPtr lpEventAttributes,
            [In, MarshalAs(UnmanagedType.Bool)] Boolean bManualReset,
            [In, MarshalAs(UnmanagedType.Bool)] Boolean bIntialState,
            [In, MarshalAs(UnmanagedType.BStr)] string lpName);

        [DllImport("kernel32.dll")]
        public static extern void SetLastError(Int32 dwErrCode);

        [DllImport("kernel32.dll")]
        public static extern bool SetEvent(IntPtr hEvent);

        [DllImport("kernel32.dll")]
        public static extern Boolean WriteFile(
            IntPtr hFile,
            Byte[] lpBuffer,
            UInt32 nNumberOfBytesToWrite,
            out UInt32 lpNumberOfBytesWritten,
            IntPtr lpOverlapped);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool ReadFile(
            IntPtr hFile,
            [Out] Byte[] lpBuffer,
            UInt32 nNumberOfBytesToRead,
            out UInt32 lpNumberOfBytesRead,
            IntPtr lpOverlapped);

        [DllImport("kernel32.dll")]
        public static extern bool CreateProcess(
            string lpApplicationName,
            string lpCommandLine,
            IntPtr lpProcessAttributes,
            IntPtr lpThreadAttributes,
            Boolean bInheritHandles,
            UInt32 dwCreationFlags,
            IntPtr lpEnvironment,
            string lpCurrentDirectory,
            [In] ref DeepDarkWin32Fantasy.STARTUPINFO lpStartupInfo,
            out DeepDarkWin32Fantasy.PROCESS_INFORMATION lpProcessInformation);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool TerminateProcess(IntPtr hProcess, UInt32 uExitCode);

        [DllImport("kernel32.dll", EntryPoint = "PeekNamedPipe", SetLastError = true)]
        public static extern bool PeekNamedPipe(
            IntPtr handle,
            Byte[] buffer,
            UInt32 nBufferSize,
            ref UInt32 bytesRead,
            ref UInt32 bytesAvail,
            ref UInt32 BytesLeftThisMessage);

        [DllImport("kernel32.dll", EntryPoint = "WaitForMultipleObjects", SetLastError = true)]
        public static extern int WaitForMultipleObjects(
            UInt32 nCount,
            IntPtr[] lpHandles,
            Boolean fWaitAll,
            UInt32 dwMilliseconds);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern Boolean GetExitCodeProcess(IntPtr hProcess, out Int32 lpExitCode);

        #endregion Function
    }

    internal class Advapi32
    {
        #region Structs
        [StructLayout(LayoutKind.Sequential)]
        public struct SECURITY_ATTRIBUTES
        {
            public int nLength;
            public byte lpSecurityDescriptor;
            public int bInheritHandle;
        }

        public enum SECURITY_IMPERSONATION_LEVEL
        {
            SecurityAnonymous,
            SecurityIdentification,
            SecurityImpersonation,
            SecurityDelegation
        }

        public enum TOKEN_TYPE
        {
            TokenPrimary = 1,
            TokenImpersonation
        }

        public const UInt32 STANDARD_RIGHTS_REQUIRED = 0x000F0000;
        public const UInt32 STANDARD_RIGHTS_READ = 0x00020000;
        public const UInt32 TOKEN_ASSIGN_PRIMARY = 0x0001;
        public const UInt32 TOKEN_DUPLICATE = 0x0002;
        public const UInt32 TOKEN_IMPERSONATE = 0x0004;
        public const UInt32 TOKEN_QUERY = 0x0008;
        public const UInt32 TOKEN_QUERY_SOURCE = 0x0010;
        public const UInt32 TOKEN_ADJUST_PRIVILEGES = 0x0020;
        public const UInt32 TOKEN_ADJUST_GROUPS = 0x0040;
        public const UInt32 TOKEN_ADJUST_DEFAULT = 0x0080;
        public const UInt32 TOKEN_ADJUST_SESSIONID = 0x0100;
        public const UInt32 TOKEN_READ = (STANDARD_RIGHTS_READ | TOKEN_QUERY);
        public const UInt32 TOKEN_ALL_ACCESS = (STANDARD_RIGHTS_REQUIRED | TOKEN_ASSIGN_PRIMARY |
                                                TOKEN_DUPLICATE | TOKEN_IMPERSONATE | TOKEN_QUERY | TOKEN_QUERY_SOURCE |
                                                TOKEN_ADJUST_PRIVILEGES | TOKEN_ADJUST_GROUPS | TOKEN_ADJUST_DEFAULT |
                                                TOKEN_ADJUST_SESSIONID);

        public enum TOKEN_ELEVATION_TYPE
        {
            TokenElevationTypeDefault = 1,
            TokenElevationTypeFull,
            TokenElevationTypeLimited
        }
        #endregion Structs
        #region Functions
        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool SetServiceStatus(IntPtr handle, ref DeepDarkWin32Fantasy.ServiceStatus serviceStatus);

        [DllImport("Advapi32.dll")]
        public static extern bool LookupPrivilegeValueA(string lpSystemName, string lpName, ref DeepDarkWin32Fantasy.LUID lpLuid);
        [DllImport("advapi32.dll")]
        public static extern bool DuplicateTokenEx(IntPtr hExistingToken, uint dwDesiredAccess, out SECURITY_ATTRIBUTES lpTokenAttributes, SECURITY_IMPERSONATION_LEVEL ImpersonationLevel, TOKEN_TYPE TokenType, out IntPtr phNewToken);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern bool CreateProcessAsUser(
            IntPtr hToken,
            string lpApplicationName,
            string lpCommandLine,
            ref SECURITY_ATTRIBUTES lpProcessAttributes,
            ref SECURITY_ATTRIBUTES lpThreadAttributes,
            bool bInheritHandles,
            uint dwCreationFlags,
            IntPtr lpEnvironment,
            string lpCurrentDirectory,
            ref DeepDarkWin32Fantasy.STARTUPINFO lpStartupInfo,
            out DeepDarkWin32Fantasy.PROCESS_INFORMATION lpProcessInformation);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool CreateProcessAsUserW(
            IntPtr hToken,
            string lpApplicationName,
            string lpCommandLine,
            ref SECURITY_ATTRIBUTES lpProcessAttributes,
            ref SECURITY_ATTRIBUTES lpThreadAttributes,
            bool bInheritHandles,
            uint dwCreationFlags,
            IntPtr lpEnvironment,
            string lpCurrentDirectory,
            ref DeepDarkWin32Fantasy.STARTUPINFO lpStartupInfo,
            out DeepDarkWin32Fantasy.PROCESS_INFORMATION lpProcessInformation);

        /// <summary>
        /// The function opens the access token associated with a process.
        /// </summary>
        /// <param name="hProcess">
        /// A handle to the process whose access token is opened.
        /// </param>
        /// <param name="desiredAccess">
        /// Specifies an access mask that specifies the requested types of 
        /// access to the access token. 
        /// </param>
        /// <param name="hToken">
        /// Outputs a handle that identifies the newly opened access token 
        /// when the function returns.
        /// </param>
        /// <returns></returns>
        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool OpenProcessToken(IntPtr hProcess,
            UInt32 desiredAccess, out DeepDarkWin32Fantasy.SafeTokenHandle hToken);


        /// <summary>
        /// The function creates a new access token that duplicates one 
        /// already in existence.
        /// </summary>
        /// <param name="ExistingTokenHandle">
        /// A handle to an access token opened with TOKEN_DUPLICATE access.
        /// </param>
        /// <param name="ImpersonationLevel">
        /// Specifies a SECURITY_IMPERSONATION_LEVEL enumerated type that 
        /// supplies the impersonation level of the new token.
        /// </param>
        /// <param name="DuplicateTokenHandle">
        /// Outputs a handle to the duplicate token. 
        /// </param>
        /// <returns></returns>
        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool DuplicateToken(
            DeepDarkWin32Fantasy.SafeTokenHandle ExistingTokenHandle,
            SECURITY_IMPERSONATION_LEVEL ImpersonationLevel,
            out DeepDarkWin32Fantasy.SafeTokenHandle DuplicateTokenHandle);


        /// <summary>
        /// The function retrieves a specified type of information about an 
        /// access token. The calling process must have appropriate access 
        /// rights to obtain the information.
        /// </summary>
        /// <param name="hToken">
        /// A handle to an access token from which information is retrieved.
        /// </param>
        /// <param name="tokenInfoClass">
        /// Specifies a value from the TOKEN_INFORMATION_CLASS enumerated 
        /// type to identify the type of information the function retrieves.
        /// </param>
        /// <param name="pTokenInfo">
        /// A pointer to a buffer the function fills with the requested 
        /// information.
        /// </param>
        /// <param name="tokenInfoLength">
        /// Specifies the size, in bytes, of the buffer pointed to by the 
        /// TokenInformation parameter. 
        /// </param>
        /// <param name="returnLength">
        /// A pointer to a variable that receives the number of bytes needed 
        /// for the buffer pointed to by the TokenInformation parameter. 
        /// </param>
        /// <returns></returns>
        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetTokenInformation(
            DeepDarkWin32Fantasy.SafeTokenHandle hToken,
            DeepDarkWin32Fantasy.TOKEN_INFORMATION_CLASS tokenInfoClass,
            IntPtr pTokenInfo,
            Int32 tokenInfoLength,
            out Int32 returnLength);

        /// <summary>
        /// The function returns a pointer to a specified subauthority in a 
        /// security identifier (SID). The subauthority value is a relative 
        /// identifier (RID).
        /// </summary>
        /// <param name="pSid">
        /// A pointer to the SID structure from which a pointer to a 
        /// subauthority is to be returned.
        /// </param>
        /// <param name="nSubAuthority">
        /// Specifies an index value identifying the subauthority array 
        /// element whose address the function will return.
        /// </param>
        /// <returns>
        /// If the function succeeds, the return value is a pointer to the 
        /// specified SID subauthority. To get extended error information, 
        /// call GetLastError. If the function fails, the return value is 
        /// undefined. The function fails if the specified SID structure is 
        /// not valid or if the index value specified by the nSubAuthority 
        /// parameter is out of bounds.
        /// </returns>
        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr GetSidSubAuthority(IntPtr pSid, UInt32 nSubAuthority);
        #endregion Functions
    }

    internal class NtDll
    {
        #region Structs
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct SYSTEM_HANDLE
        {
            public int ProcessID;
            public byte ObjectTypeNumber;
            public char Flags;
            public ushort Handle;
            public IntPtr Object_Pointer;
            public IntPtr GrantedAccess;
        }
        #endregion
        #region Functions
        [DllImport("ntdll.dll")]
        public static extern int NtSuspendProcess(IntPtr hProcess);
        [DllImport("ntdll.dll")]
        public static extern int NtResumeProcess(IntPtr hProcess);
        [DllImport("ntdll.dll")]
        public static extern int NtQueryInformationProcess(IntPtr ProcessHandle, int ProcessInformationClass, IntPtr[] ProcessInformation, int ProcessInformationLength, ref int ReturnLength);
        [DllImport("ntdll.dll")]
        public static extern int NtQuerySystemInformation(int SystemInformationClass, IntPtr SystemInformation, int SystemInformationLength, ref int ReturnLength);
        [DllImport("ntdll.dll")]
        public static extern int RtlCreateUserThread(IntPtr Process, IntPtr ThreadSecurityDescriptor, Boolean CreateSuspended, IntPtr ZeroBits, IntPtr MaximumStackSize, IntPtr CommittedStackSize, IntPtr StartAddress, IntPtr Parameter, ref IntPtr Thread, IntPtr ClientId);
        #endregion Functions
    }
}
