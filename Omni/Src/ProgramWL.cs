#if WINDOWS
using SciterSharp;
using SciterSharp.Interop;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace Omni
{
	class Program
	{
		[STAThread]
		static void Main(string[] args)
		{
			if(IntPtr.Size == 4)
			{
				Debug.Assert(false, "sciter.dll that comes bundled in Omni is the x64 version, make sure to change it to the x86 version if building for x86 (Windows only)");
			}

		#if WINDOWS
			// Sciter needs this for drag'n'drop support; STAThread is required for OleInitialize succeess
			int oleres = PInvokeWindows.OleInitialize(IntPtr.Zero);
			Debug.Assert(oleres == 0);
		#endif
		#if GTKMONO
			PInvokeGTK.gtk_init(IntPtr.Zero, IntPtr.Zero);
			Mono.Setup();
		#endif
			
			/*
				NOTE:
				In Linux, if you are getting a System.TypeInitializationException below, it is because you don't have 'libsciter-gtk-64.so' in your LD_LIBRARY_PATH.
				Run 'sudo bash install-libsciter.sh' contained in this package to install it in your system.
			*/
			App.ParseArgs(args);

            if(App.Setup())
            {
                SaveRegistryRunPath();
                PInvokeUtils.RunMsgLoop();

				GC.Collect();
				GC.WaitForPendingFinalizers();
			}
		}

		public static void SaveRegistryRunPath()
		{
			var key = Registry.CurrentUser.CreateSubKey("Software\\MI Software\\Omni", RegistryKeyPermissionCheck.ReadWriteSubTree);
			key.SetValue("exe_path", App.g_exe_path);
		}
	}
}
#endif