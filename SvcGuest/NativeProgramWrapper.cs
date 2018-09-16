﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Management;
using System.Timers;

namespace SvcGuest
{
    class NativeProgramWrapper : ProgramWrapper
    {

        private Kernel32.PROCESS_INFORMATION _pi;
        private Kernel32.STARTUPINFO _si;
        private Advapi32.SECURITY_ATTRIBUTES _saProcessAttributes;
        private Advapi32.SECURITY_ATTRIBUTES _saThreadAttributes;

        private readonly string _launchType;
        private readonly int _launchIndex;
        private readonly IntPtr _identityToken;

        public int SelfProcessId => Process.GetCurrentProcess().Id;
        public int ChildProcessId => _pi.dwProcessId;

        private readonly Timer _checkChildProcessTimer = new Timer()
        {
            AutoReset = true,
            Enabled = true,
            Interval = 1000,
        };

        public NativeProgramWrapper(string launchType, int launchIndex, IntPtr identityToken)
        {
            _launchType = launchType;
            _launchIndex = launchIndex;
            _identityToken = identityToken;

            _si.cb = Marshal.SizeOf(_si);
        }

        public override void Start()
        {
            var execArgs = $"\"{Globals.ExecutablePath}\" --config {Globals.ConfigPath} --impersonated --LaunchType {_launchType} --LaunchIndex {_launchIndex}";

            if (!Advapi32.CreateProcessAsUser(
                _identityToken, 
                Globals.ExecutablePath, 
                execArgs,
                ref _saProcessAttributes, 
                ref _saThreadAttributes, 
                false, 
                0, 
                IntPtr.Zero, 
                Globals.ExecutableDirectory, 
                ref _si, 
                out _pi
                ))
                throw new Win32Exception();
            Debug.WriteLine($"Helper process at {ChildProcessId}");
            _checkChildProcessTimer.Elapsed += OnCheckChildProcessTimer;
        }

        public override void Stop()
        {
            if (!IfChildProcessAlive()) return;
            var cp = Process.GetProcessById(ChildProcessId);
            QuitProcess(cp);
        }

        public IEnumerable<int> GetChildProcessIds()
        {
            List<int> children = new List<int>();
            ManagementObjectSearcher mos = new ManagementObjectSearcher(
                $"Select * From Win32_Process Where ParentProcessID={SelfProcessId}");

            foreach (var o in mos.Get())
            {
                var mo = (ManagementObject) o;
                children.Add(Convert.ToInt32(mo["ProcessID"]));
            }

            return children;
        }

        public bool IfChildProcessAlive()
        {
            return GetChildProcessIds().Contains(ChildProcessId);
        }

        private void OnCheckChildProcessTimer(object sender, EventArgs e)
        {
            if (!IfChildProcessAlive())
            {
                OnProgramExited(this, null);
                _checkChildProcessTimer.Enabled = false;
            }
        }
    }
}