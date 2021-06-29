#if OSX
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using AppKit;
using Foundation;

namespace Ion
{
	class HIDOSX
	{
		[DllImport("/System/Library/Frameworks/IOKit.framework/IOKit")]
		static extern uint IOServiceGetMatchingService(uint masterPort, IntPtr matching);

		[DllImport("/System/Library/Frameworks/IOKit.framework/IOKit")]
		static extern IntPtr IOServiceMatching(string s);

		[DllImport("/System/Library/Frameworks/IOKit.framework/IOKit")]
		static extern IntPtr IORegistryEntryCreateCFProperty(uint entry, IntPtr key, IntPtr allocator, uint options);

		[DllImport("/System/Library/Frameworks/IOKit.framework/IOKit")]
		static extern int IOObjectRelease(uint o);

		public static string GetSerialNumber()
		{
			string serial = string.Empty;
			uint platformExpert = IOServiceGetMatchingService(0, IOServiceMatching("IOPlatformExpertDevice"));
			if(platformExpert != 0)
			{
				NSString key = new NSString("IOPlatformSerialNumber");
				IntPtr serialNumber = IORegistryEntryCreateCFProperty(platformExpert, key.Handle, IntPtr.Zero, 0);
				if(serialNumber != IntPtr.Zero)
				{
					//serial = new NSString (serialNumber);
					serial = NSString.FromHandle(serialNumber);
				}
				IOObjectRelease(platformExpert);
			}

			return serial;
		}

		public static string MacUUID()
		{
			var startInfo = new ProcessStartInfo()
			{
				FileName = "sh",
				Arguments = "-c \"ioreg -rd1 -c IOPlatformExpertDevice | awk '/IOPlatformUUID/'\"",
				UseShellExecute = false,
				CreateNoWindow = true,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				RedirectStandardInput = true,
				UserName = Environment.UserName
			};
			var builder = new StringBuilder();
			using(Process process = Process.Start(startInfo))
			{
				process.WaitForExit();
				builder.Append(process.StandardOutput.ReadToEnd());
			}
			return builder.ToString().Trim();
		}
	}

	public class OSXUUID
	{
		public static string Method1()
		{
			string[] args = new string[] { "-rd1", "-c", "IOPlatformExpertDevice", "|", "grep", "model" };
			NSTask task = new NSTask();
			task.LaunchPath = @"/usr/sbin/ioreg";
			task.Arguments = args;

			NSPipe pipe = new NSPipe();
			task.StandardOutput = pipe;
			task.Launch();

			string[] args2 = new string[] { "/IOPlatformUUID/ { split($0, line, \"\\\"\"); printf(\"%s\\n\", line[4]); }" };

			NSTask task2 = new NSTask();
			task2.LaunchPath = @"/usr/bin/awk";
			task2.Arguments = args2;

			NSPipe pipe2 = new NSPipe();
			task2.StandardInput = pipe;
			task2.StandardOutput = pipe2;
			NSFileHandle fileHandle2 = pipe2.ReadHandle;
			task2.Launch();

			NSData data = fileHandle2.ReadDataToEndOfFile();
			NSString uuid = NSString.FromData(data, NSStringEncoding.UTF8);
			return uuid.ToString().Replace("\n", "");
		}

		[DllImport("/System/Library/Frameworks/IOKit.framework/IOKit")]
		static extern uint IOServiceGetMatchingService(uint masterPort, IntPtr matching);

		[DllImport("/System/Library/Frameworks/IOKit.framework/IOKit")]
		static extern IntPtr IOServiceMatching(string s);

		[DllImport("/System/Library/Frameworks/IOKit.framework/IOKit")]
		static extern IntPtr IORegistryEntryCreateCFProperty(uint entry, IntPtr key, IntPtr allocator, uint options);

		[DllImport("/System/Library/Frameworks/IOKit.framework/IOKit")]
		static extern int IOObjectRelease(uint o);

		public static string GetSerialNumber()
		{
			string serial = string.Empty;
			uint platformExpert = IOServiceGetMatchingService(0, IOServiceMatching("IOPlatformExpertDevice"));
			if (platformExpert != 0)
			{
				NSString key = (NSString)"IOPlatformSerialNumber";
				IntPtr serialNumber = IORegistryEntryCreateCFProperty(platformExpert, key.Handle, IntPtr.Zero, 0);
				if (serialNumber != IntPtr.Zero)
				{
					serial = NSString.FromHandle(serialNumber);
				}
				IOObjectRelease(platformExpert);
			}

			return serial;
		}
	}
}
#endif
