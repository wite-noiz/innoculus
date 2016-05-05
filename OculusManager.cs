using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace innoculus
{
	public class OculusManager
	{
		public const string OCULUS_PROCESS_NAME = "OculusClient";
		public const string OCULUS_SERVICE_NAME = "Oculus VR Runtime Service";
		public const string OCULUS_SERVER_NAME = "OVRServer_x64";
		public const string OCULUS_SERVER_SUBPATH = @"Support\oculus-runtime\OVRServer_x64.exe";
		public const string OCULUS_PROCESS_SUBPATH = @"Support\oculus-client\OculusClient.exe";

		public string InstallationPath { get; private set; }

		public OculusManager()
		{
			// Figure out main installation path based on where service is running from
			var svcs = ServiceController.GetServices().Where(s => s.DisplayName.Contains("Oculus")).ToArray();
			foreach (var svcPath in svcs.Select(s => GetImagePath(s)))
			{
				var path = Path.GetDirectoryName(svcPath);
				path = Path.GetFullPath(Path.Combine(path, "..", ".."));
				if (File.Exists(Path.Combine(path, "Oculus.ico")))
				{
					InstallationPath = path;
					break;
				}
			}
		}

		public static string GetImagePath(ServiceController service)
		{
			using (var rk = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\" + service.ServiceName))
			{
				var path = rk.GetValue("ImagePath").ToString();

				if (path[0] == '"')
				{
					path = path.Substring(1, path.IndexOf('"', 1) - 1);
				}
				path = Environment.ExpandEnvironmentVariables(path);
				return path;
			}
		}

		public void StopOculusHome()
		{
			try
			{
				// Stop service
				var svc = new ServiceController(OCULUS_SERVICE_NAME);
				if (svc != null && svc.Status == ServiceControllerStatus.Running)
				{
					svc.Stop();
				}

				// Stop processes
				var appProc = Process.GetProcessesByName(OCULUS_PROCESS_NAME);
				Array.ForEach(appProc, p => p.Kill());
				var svcProc = Process.GetProcessesByName(OCULUS_SERVER_NAME);
				Array.ForEach(svcProc, p => p.Kill());
			}
			catch (Exception ex)
			{
				Console.WriteLine("[ERROR] Failed to stop server: " + ex.Message);
			}
		}

		public void StartOculusHome(ProcessMonitor monitor)
		{
			try
			{
				// Start service or server
				var svc = new ServiceController(OCULUS_SERVICE_NAME);
				var startedSvc = false;
				if (svc != null && svc.Status == ServiceControllerStatus.Stopped)
				{
					try
					{
						svc.Stop();
						startedSvc = true;
					}
					catch { }
				}
				if (!startedSvc)
				{
					var process = new Process
					{
						StartInfo = new ProcessStartInfo
						{
							FileName = Path.Combine(InstallationPath, OCULUS_SERVER_SUBPATH),
							CreateNoWindow = true,
							RedirectStandardOutput = true,
							UseShellExecute = false
						}
					};
					process.Start();
				}

				System.Threading.Thread.Sleep(1000);

				var appPath = Path.Combine(InstallationPath, OCULUS_PROCESS_SUBPATH);
				monitor.StartProcess(appPath);
			}
			catch (Exception ex)
			{
				Console.WriteLine("[ERROR] Failed to start service: {0}" + ex.Message);
			}
		}
	}
}
