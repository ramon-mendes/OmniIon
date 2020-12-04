using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Ion;
using Omni.Hosting;
using SciterSharp;
using SciterSharp.Interop;

namespace Omni
{
	static class App
	{
		public static Window AppWindow { get; private set; }// must keep a reference to survive GC
		public static Host AppHost { get; private set; }
		public static DebuggerHost AppDebugger { get; private set; }

		public static readonly string g_exe_path = Process.GetCurrentProcess().MainModule.FileName;
		public static string g_arg_page;
		public static bool g_arg_r;
		public static bool g_arg_t;
		
		public static bool Setup()
		{
			Console.WriteLine("Sciter " + SciterX.Version);

            #region Ion
            if(IonApp.GetStatus() != EIonStatus.ACTIVE || IonApp.IsTrial)
                DlgLicense.ShowDialog();
            if(IonApp.GetStatus() != EIonStatus.ACTIVE)
                return false;
            UpdateControl.Setup();
            #endregion

            // Create the window
            AppWindow = new Window();
			AppDebugger = new DebuggerHost();
			AppHost = new Host(AppWindow);

			State.Setup();

			// Args passing
			var sv = new SciterValue();
			sv["t"] = new SciterValue(App.g_arg_t);
			sv["r"] = new SciterValue(App.g_arg_r);
			AppHost.CallFunction("View_Setup", sv);

			if(!string.IsNullOrEmpty(g_arg_page))
			{
				//MessageBox.ShowMessageBox(AppWindow, g_arg_page);
				State.LoadURL(g_arg_page);
			}

			AppWindow.CenterTopLevelWindow();
			AppWindow.Show();

#if DEBUG
			//AppHost.InvokePost(() => Tests.Run());
#endif
            return true;
		}

		public static bool ParseArgs(string[] args)// returns if app should exit
		{
			foreach(var str in args)
			{
				if(str == "-t")
					g_arg_t = true;
				if(str == "-r")
					g_arg_r = true;
				else if(str.StartsWith("-gfx:"))
				{
					string val = str.Substring(5);
					SciterXDef.GFX_LAYER gfx = SciterXDef.GFX_LAYER.GFX_LAYER_AUTO;
#if OSX
					switch(val)
					{
						case "cg":
							gfx = SciterXDef.GFX_LAYER.GFX_LAYER_D2D;
							break;

						case "skia":
							gfx = SciterXDef.GFX_LAYER.GFX_LAYER_SKIA;
							break;
						case "skia_opengl":
							gfx = SciterXDef.GFX_LAYER.GFX_LAYER_SKIA_OPENGL;
							break;
					}
#else
					switch(val)
					{
						case "auto":
							gfx = SciterXDef.GFX_LAYER.GFX_LAYER_D2D;
							break;
						case "gdi":
							gfx = SciterXDef.GFX_LAYER.GFX_LAYER_GDI;
							break;
                        case "warp":
                            gfx = SciterXDef.GFX_LAYER.GFX_LAYER_WARP;
                            break;
                        case "d2d":
                            gfx = SciterXDef.GFX_LAYER.GFX_LAYER_D2D;
                            break;
                    }
#endif

					SciterX.API.SciterSetOption(IntPtr.Zero, SciterXDef.SCITER_RT_OPTIONS.SCITER_SET_GFX_LAYER, new IntPtr((int)gfx));
				}
				else if(str.StartsWith("-"))// Xamarin -psn_0_5539144 things
				{
					continue;
				}
				else if(str == g_exe_path)
				{
					continue;
				}
				else
				{
					g_arg_page = str;
				}
			}

			return false;
		}
	}
}