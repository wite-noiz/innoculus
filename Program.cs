using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Permissions;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace innoculus
{
    /// <summary>
    /// When started:
    /// * Look for Oculus Home
    ///     * If exists, attach and wait
    ///     * Else exit, unless /start when we should start service then Home
    /// * If Oculus Home ends, stop service
    /// </summary>
    public static class Program
    {
        private const string OCULUS_PROCESS_NAME = "OculusClient";
        private const string OCULUS_CLIENT_PATH = @"%ProgramFiles(x86)%\Oculus\Support\oculus-client\OculusClient.exe";
        private const string OCULUS_SERVICE_NAME = "Oculus VR Runtime Service";
        private const string OCULUS_SERVER_NAME = "OVRServer_x64";
        private const string OCULUS_SERVER_PATH = @"%ProgramFiles(x86)%\Oculus\Support\oculus-runtime\OVRServer_x64.exe";

        private static bool _hasAdminRights = false;
        private static ProcessMonitor _monitor = null;

        public static void Main(params string[] args)
        {
            // Stop and disable services - assumes has admin rights
            if (args.Any(a => a.ToLower() == "/stop_service"))
            {
                DisableService(false);
                return;
            }

            try
            {
                CheckAdminRights();
                Console.WriteLine("Running with Admin privileges");
            }
            catch (System.Security.SecurityException)
            {
                Console.WriteLine("Running without Admin privileges");
            }

            // Look for Oculus Home running
            _monitor = new ProcessMonitor(OCULUS_PROCESS_NAME);

            if (_monitor.HasProcess)
            {
                Console.WriteLine("Found process: " + OCULUS_PROCESS_NAME);
            }
            else
            {
                Console.WriteLine("No process. Starting: " + OCULUS_CLIENT_PATH);
                DisableService(!_hasAdminRights);
                StartServer();
                var appPath = Environment.ExpandEnvironmentVariables(OCULUS_CLIENT_PATH);
                _monitor.StartProcess(appPath);
            }

            if (_monitor.HasProcess)
            {
                Console.WriteLine("Waiting for process to exit");
                _monitor.ProcessExited += Monitor_ProcessExited;
                Application.Run();
            }
            else
            {
                Console.WriteLine("Exiting; No process: " + OCULUS_PROCESS_NAME);
            }
        }

        [PrincipalPermission(SecurityAction.Demand, Role = @"BUILTIN\Administrators")]
        public static void CheckAdminRights()
        {
            _hasAdminRights = true;
        }

        public static void RestartWithAdmin(string args)
        {
            Console.WriteLine("Restarting process with permissions for: " + args);
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = Application.ExecutablePath,
                    Arguments = args,
                    Verb = "runas"
                }
            };
            process.Start();
            System.Threading.Thread.Sleep(1000);
            process.WaitForExit();
            Console.WriteLine("Admin process complete");
        }

        private static void Monitor_ProcessExited(object sender, EventArgs e)
        {
            Console.WriteLine("Process exited");
            StopServer();
            Application.Exit();
        }

        private static void StartServer()
        {
            try
            {
                var svc = Process.GetProcessesByName(OCULUS_SERVER_NAME).FirstOrDefault();
                if (svc == null)
                {
                    Console.WriteLine("Starting server: " + OCULUS_SERVER_NAME);
                    var appPath = Environment.ExpandEnvironmentVariables(OCULUS_SERVER_PATH);
                    var process = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = appPath,
                            CreateNoWindow = true,
                            RedirectStandardOutput = true,
                            UseShellExecute = false
                        }
                    };
                    process.Start();
                    System.Threading.Thread.Sleep(1000);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[ERROR] Failed to start service: {0}" + ex.Message);
            }
        }

        private static void StopServer()
        {
            try
            {
                var svc = Process.GetProcessesByName(OCULUS_SERVER_NAME).FirstOrDefault();
                if (svc != null)
                {
                    Console.WriteLine("Stopping server: " + OCULUS_SERVER_NAME);
                    svc.Kill();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[ERROR] Failed to stop server: " + ex.Message);
            }
        }

        private static void DisableService(bool needsAdmin)
        {
            Console.WriteLine("Stopping service: " + OCULUS_SERVICE_NAME);
            try
            {
                var svc = new ServiceController(OCULUS_SERVICE_NAME);
                if (svc.Status != ServiceControllerStatus.Stopped)
                {
                    if (needsAdmin)
                    {
                        RestartWithAdmin("/stop_service");
                    }
                    else
                    {
                        svc.Stop();
                        svc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(60));
                    }
                    if (svc.Status != ServiceControllerStatus.Running)
                    {
                        Console.WriteLine("Service stopped");
                    }
                    else
                    {
                        Console.WriteLine("Service not stopped");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[ERROR] Failed to stop service: " + ex.Message);
            }
        }
    }
}
