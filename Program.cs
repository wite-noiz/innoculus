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
		private static bool _hasAdminRights = false;
		private static ProcessMonitor _monitor = null;
		private static OculusManager _mgr = null;

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

			_mgr = new OculusManager();

			// Look for Oculus Home running
			_monitor = new ProcessMonitor(OculusManager.OCULUS_PROCESS_NAME);

			if (_monitor.HasProcess)
			{
				Console.WriteLine("Found process: " + OculusManager.OCULUS_PROCESS_NAME);
			}
			else
			{
				Console.WriteLine("No process. Starting: " + _mgr.InstallationPath);
				DisableService(!_hasAdminRights);
				_mgr.StartOculusHome(_monitor);
			}

			if (_monitor.HasProcess)
			{
				Console.WriteLine("Waiting for process to exit");
				_monitor.ProcessExited += Monitor_ProcessExited;
				Application.Run();
			}
			else
			{
				Console.WriteLine("Exiting; No process: " + OculusManager.OCULUS_PROCESS_NAME);
			}
			_mgr.StopOculusHome();
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
			_mgr.StopOculusHome();
			Application.Exit();
		}

		private static void DisableService(bool needsAdmin)
		{
			Console.WriteLine("Stopping service: " + OculusManager.OCULUS_SERVICE_NAME);
			try
			{
				var svc = new ServiceController(OculusManager.OCULUS_SERVICE_NAME);
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
