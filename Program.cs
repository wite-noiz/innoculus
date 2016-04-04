using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Permissions;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Noculus
{
    /// <summary>
    /// When started:
    /// * Look for Oculus Home
    ///     * If exists, attach and wait
    ///     * Else exit, unless /start when we should start service then Home
    /// * If Oculus Home ends, stop services
    /// 
    /// Since the service runs as admin, need to start a new process with privileges for handling them.
    /// Interestingly, Home cannot be started as admin (probably a profile thing), so Noculus cannot run as admin and then start Home.
    /// </summary>
    public static class Program
    {
        private const string OCULUS_PROCESS_NAME = "OculusClient";
        private const string OCULUS_CLIENT_PATH = @"%ProgramFiles(x86)%\Oculus\Support\oculus-client\OculusClient.exe";
        private const string OCULUS_SERVICE_NAME = "Oculus VR Runtime Service";

        private static bool _hasAdminRights = false;
        private static ProcessMonitor _monitor = null;

        public static void Main(params string[] args)
        {
            // Stop/start services - assumes has admin rights
            if (args.Any(a => a.ToLower() == "/start_service"))
            {
                StartServices(false);
                return;
            }
            else if (args.Any(a => a.ToLower() == "/stop_service"))
            {
                StopServices(false);
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
            else if (args.Any(a => a.ToLower() == "/start"))
            {
                Console.WriteLine("No process. Starting: " + OCULUS_CLIENT_PATH);
                StartServices(!_hasAdminRights);
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

        private static void Monitor_ProcessExited(object sender, EventArgs e)
        {
            Console.WriteLine("Process exited");
            StopServices(!_hasAdminRights);
            Application.Exit();
        }

        [PrincipalPermission(SecurityAction.Demand, Role = @"BUILTIN\Administrators")]
        public static void CheckAdminRights()
        {
            _hasAdminRights = true;
        }

        public static void RestartWithAdmin(string args)
        {
            Console.WriteLine("Restarting process with permissions for: " + args);
            var psi = new ProcessStartInfo
            {
                FileName = Application.ExecutablePath,
                Arguments = args,
                Verb = "runas"
            };

            var process = new Process
            {
                StartInfo = psi
            };
            process.Start();
            System.Threading.Thread.Sleep(1000);
            process.WaitForExit();
            Console.WriteLine("Admin process complete");
        }

        private static void StartServices(bool needsAdmin)
        {
            Console.WriteLine("Starting service: " + OCULUS_SERVICE_NAME);
            try
            {
                var svc = new ServiceController(OCULUS_SERVICE_NAME);
                if (svc.Status != ServiceControllerStatus.Running)
                {
                    if (needsAdmin)
                    {
                        RestartWithAdmin("/start_service");
                    }
                    else
                    {
                        svc.Start();
                        svc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(60));
                    }
                    if (svc.Status != ServiceControllerStatus.Running)
                    {
                        Console.WriteLine("Service started");
                    }
                    else
                    {
                        Console.WriteLine("Service not started");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[ERROR] Failed to start service: {0}" + ex.Message);
            }
        }

        private static void StopServices(bool needsAdmin)
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
