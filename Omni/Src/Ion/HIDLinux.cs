#if GTKMONO
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ion
{
	static class HIDLinux
	{
		public static string GetHID()
		{
			string hid = ExecLine("/bin/bash", "-c blkid | grep -oP 'UUID=\"\\K[^ \"]+' | sha256sum | awk '{print $1}'");
			if(hid == null)
				hid = ExecLine("cat", "/etc/machine-id");
			return hid;
		}

		public static string ExecLine(string path, string args = "")
		{
			var proc = new Process
			{
				StartInfo = new ProcessStartInfo
				{
					FileName = path,
					Arguments = args,
					UseShellExecute = false,
					RedirectStandardOutput = true,
					CreateNoWindow = true
				}
			};

			proc.Start();
			return proc.StandardOutput.ReadLine();
		}
	}
}
#endif