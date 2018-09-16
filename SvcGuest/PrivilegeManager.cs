using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global
// ReSharper disable IdentifierTypo
// ReSharper disable StringLiteralTypo

namespace SvcGuest
{
    public class CProcess
    {
        Process[] m_ProcessList;
        private readonly string m_ProcessName;
        private IntPtr m_hProcess;

        public CProcess()
        {
            this.m_ProcessName = "";
            this.m_hProcess = Kernel32.GetCurrentProcess();
        }

        ~CProcess()
        {
        }

        /// <summary>
        /// This functions waits for our target process to start.
        /// </summary>
        public bool Wait(int Interval)
        {
            if (this.m_ProcessName.Length == 0) return false;
            this.m_hProcess = IntPtr.Zero;

            while (true)
            {
                this.m_ProcessList = Process.GetProcessesByName(m_ProcessName);
                System.Threading.Thread.Sleep(Interval);
                if (this.m_ProcessList.Length > 0)
                {
                    this.m_hProcess = Kernel32.OpenProcess(0x1fffff, false, this.m_ProcessList[0].Id);
                    break;
                }
            }

            return true;
        }

        /// <summary>
        /// This functions sets the privilege of our target process
        /// </summary>
        public bool SetPrivilege(string lpszPrivilege, bool bEnablePrivilege)
        {
            bool Status = true;
            DeepDarkWin32Fantasy.TOKEN_PRIVILEGES priv = new DeepDarkWin32Fantasy.TOKEN_PRIVILEGES();
            IntPtr hToken = IntPtr.Zero;
            DeepDarkWin32Fantasy.LUID luid = new DeepDarkWin32Fantasy.LUID();
            int RetLength = 0;

            if (!Kernel32.OpenProcessToken(this.m_hProcess, 0x0020, ref hToken))
            {
                Status = false;
                goto EXIT;
            }

            if (!Advapi32.LookupPrivilegeValueA(null, lpszPrivilege, ref luid))
            {
                Status = false;
                goto EXIT;
            }

            priv.PrivilegeCount = 1;
            priv.Privileges = new DeepDarkWin32Fantasy.LUID_AND_ATTRIBUTES();
            priv.Privileges.Luid = luid;
            priv.Privileges.Attributes = (int)((bEnablePrivilege == true) ? 0x00000002L : 0x00000004L);

            if (!Kernel32.AdjustTokenPrivileges(hToken, false, ref priv, 0, IntPtr.Zero, ref RetLength))
            {
                Status = false;
                goto EXIT;
            }

        EXIT:
            if (hToken != IntPtr.Zero) Kernel32.CloseHandle(hToken);
            return Status;
        }

        /// <summary>
        /// This functions suspends our process
        /// </summary>
        public bool Suspend()
        {
            return (NtDll.NtSuspendProcess(this.m_hProcess) == 0);
        }

        /// <summary>
        /// This function resumes our process
        /// </summary>
        public bool Resume()
        {
            return (NtDll.NtResumeProcess(this.m_hProcess) == 0);
        }

        /// <summary>
        /// This functions kills our process
        /// </summary>
        public bool Kill()
        {
            return Kernel32.TerminateProcess(this.m_hProcess, 0);
        }

        /// <summary>
        /// This functions opens our target process
        /// </summary>
        public bool Open(UInt32 DesiredAccess = 0x1fffff)
        {
            if (this.m_ProcessName.Length == 0)
                return false;
            this.m_hProcess = IntPtr.Zero;
            this.m_ProcessList = System.Diagnostics.Process.GetProcessesByName(m_ProcessName);
            if (this.m_ProcessList.Length > 0)
                this.m_hProcess = Kernel32.OpenProcess(DesiredAccess, false, this.m_ProcessList[0].Id);
            return IsValidProcess();
        }

        /// <summary>
        /// This functions closes our target process
        /// </summary>
        public bool Close()
        {
            return Kernel32.CloseHandle(this.m_hProcess);
        }

        /// <summary>
        /// This functions returns our target process as handle
        /// </summary>
        public IntPtr GetHandle()
        {
            return this.m_hProcess;
        }

        /// <summary>
        /// This functions returns our target process as process id
        /// </summary>
        public int GetPid()
        {
            return Kernel32.GetProcessId(this.m_hProcess);
        }

        /// <summary>
        /// This functions returns the parent id of our target process
        /// </summary>
        public int GetParentPid()
        {
            IntPtr[] pbi = new IntPtr[6];
            int ulSize = 0;
            if (NtDll.NtQueryInformationProcess(this.m_hProcess, 0, pbi, Marshal.SizeOf(pbi), ref ulSize) >= 0) return (int)pbi[5];
            return 0;
        }

        /// <summary>
        /// This functions checks if the target process is x64
        /// </summary>
        public int Is64(ref bool Is64)
        {
            int Status = 1;
            IntPtr hFile = (IntPtr)(-1);
            IntPtr lpFile = (IntPtr)0;
            int dwFileSize = 0, dwReaded = 0, dwSize = 255;
            DeepDarkWin32Fantasy.IMAGE_DOS_HEADER DosHeader;
            byte[] Path = new byte[255];
            byte[] FileCopy = null;
            string lpFileName = "";
            int machineUint = 0;

            if (!Kernel32.QueryFullProcessImageNameA(this.m_hProcess, 0, Path, ref dwSize))
            {
                Status = 2;
                goto EXIT;
            }

            lpFileName = System.Text.Encoding.Default.GetString(Path);
            hFile = Kernel32.CreateFileA(lpFileName, (0x80000000), 0, IntPtr.Zero, 3, 0, IntPtr.Zero);
            if (hFile == (IntPtr)(-1))
            {
                Status = 3;
                goto EXIT;
            }

            dwFileSize = Kernel32.GetFileSize(hFile, IntPtr.Zero);
            lpFile = Kernel32.VirtualAlloc(IntPtr.Zero, dwFileSize, 0x1000, 0x40);
            if (lpFile == IntPtr.Zero)
            {
                Status = 4;
                goto EXIT;
            }

            if (!Kernel32.ReadFile(hFile, lpFile, dwFileSize, ref dwReaded, IntPtr.Zero))
            {
                Status = 5;
                goto EXIT;
            }

            DosHeader = new DeepDarkWin32Fantasy.IMAGE_DOS_HEADER();
            DosHeader = (DeepDarkWin32Fantasy.IMAGE_DOS_HEADER)Marshal.PtrToStructure(lpFile, typeof(DeepDarkWin32Fantasy.IMAGE_DOS_HEADER));
            if (!DosHeader.isValid)
            {
                Status = 6;
                goto EXIT;
            }

            FileCopy = new byte[dwFileSize];
            Marshal.Copy(lpFile, FileCopy, 0, dwFileSize);
            machineUint = BitConverter.ToUInt16(FileCopy, BitConverter.ToInt32(FileCopy, 60) + 4);
            if (machineUint == 0x8664 || machineUint == 0x0200)
            {
                Is64 = true;
                goto EXIT;
            }
            if (machineUint == 0x014c)
            {
                Is64 = false;
                goto EXIT;
            }

        EXIT:
            if (hFile != IntPtr.Zero) Kernel32.CloseHandle(hFile);
            if (lpFile != IntPtr.Zero) Kernel32.VirtualFree(lpFile, dwFileSize, 0x4000);
            return Status;
        }

        /// <summary>
        /// This functions checks if our target process is valid
        /// </summary>
        public bool IsValidProcess()
        {
            if (m_hProcess == (IntPtr)(-1))
                return false;
            return (Kernel32.WaitForSingleObject(this.m_hProcess, 0) == 258L);
        }
    }
}
