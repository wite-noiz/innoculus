using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace innoculus
{
    public class ProcessMonitor
    {
        private readonly string _processName;
        private Process _process = null;
        private Task _watchTask = null;

        public bool HasProcess { get { return _process != null; } }

        public ProcessMonitor(string name)
        {
            _processName = name;
            attachToProcess();
        }

        private void attachToProcess()
        {
            _process = Process.GetProcessesByName(_processName).FirstOrDefault();
            if (_process != null)
            {
                waitForExit();
            }
        }

        private void waitForExit()
        {
            if (_watchTask != null)
            {
                // TODO: interrupt existing
            }
            _watchTask = Task.Run(() =>
            {
                var procId = _process.Id;
                _process.WaitForExit();
                if (_watchTask != null && _process != null && _process.Id == procId)
                {
                    _process = null;
                    _watchTask = null;
                    OnProcessExited(this, null);
                }
            });
        }

        public event EventHandler ProcessExited;
        private void OnProcessExited(object sender, EventArgs e)
        {
            try
            {
                ProcessExited.Invoke(sender, e);
            }
            catch { }
        }

        public void StartProcess(string path)
        {
            var newProc = Process.Start(path);
            System.Threading.Thread.Sleep(5000);

            attachToProcess();
            if (!HasProcess)
            {
                Console.WriteLine("Started process does not appear to match or did not start correctly");
                try
                {
                    newProc.Kill();
                }
                catch { }
            }
        }

        public void StopMonitor()
        {
            // TODO
            _watchTask = null;
        }

        public void StopProcess(bool dontWatch = false)
        {
            if (dontWatch)
            {
                StopMonitor();
            }

            if (_process != null)
            {
                _process.Close();
                _process.WaitForExit(5000);
                _process.Kill();
                _process = null;
            }
        }
    }
}
