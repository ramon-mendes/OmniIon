using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using SciterSharp;
using Omni.UI;
using SciterSharp.Interop;
using Ion;

namespace Omni.Hosting
{
	class HostEvh : SciterEventHandler
	{
		public bool HandleScriptCall(SciterElement se, string name, SciterValue[] args, out SciterValue result)
		{
			// No ssh allowed -> override ir to skip naming reflection from SciterSharp
			return OnScriptCall(se, name, args, out result);
		}

		public void Host_Dbg(SciterValue[] args)
		{
            //string arg = args[0].ToJSONString();
			Debugger.Break();
		}

        public SciterValue Host_IonExpando() => IonApp.GetUIExpando();

        public SciterValue Host_IsRelease() => new SciterValue(false);

		public void Host_CheckInspector()
		{
			var p = Process.GetProcessesByName("inspector");
			if(p.Length == 0)
			{

			}
		}

		public SciterValue Host_AppExePath() => new SciterValue(App.g_exe_path);

		public void Host_RevealPath(SciterValue[] args)
		{
			string path = args[0].Get("");
			if(Environment.OSVersion.Platform == PlatformID.Win32NT)
				Process.Start("explorer.exe", "/select," + path.Replace('/', '\\'));
			else
				Process.Start("open", $"-R \"{path}\"");
		}

#if OSX
		public void Host_AppHide() => AppKit.NSApplication.SharedApplication.Hide(AppKit.NSApplication.SharedApplication.CurrentEvent);
#endif

        #region SysInfo / ChangeGFX
        public SciterValue Host_AppVersion() => new SciterValue(Consts.Version);
        public SciterValue Host_AppVersionInt() => new SciterValue(Consts.VersionInt);

        public void Host_ChangeGFX(SciterValue[] args)
        {
            var gfx = args[0].Get("");
            Process.Start(App.g_exe_path, "-gfx:" + gfx);
        }
        #endregion

        public SciterValue Host_Exec(SciterValue[] args)
		{
			var startInfo = new ProcessStartInfo
			{
				FileName = args[0].Get(""),
				Arguments = args[1].Get(""),
				UseShellExecute = false,
				RedirectStandardOutput = true
			};

			var p = Process.Start(startInfo);

			bool wait = args[2].Get(false);
			if(wait)
			{
				p.WaitForExit();
				var output = p.StandardOutput.ReadToEnd();
				return new SciterValue(output);
			}

			return null;
		}
		
		public void Host_OpenFile(SciterValue[] args)
		{
#if WINDOWS
			//PInvoke.User32.SetForegroundWindow(App.AppWindow._hwnd);
#endif
			string path = args[0].Get("").Trim('\0');
			if(path.StartsWith("file://"))
				path = path.Substring(7);
			State.LoadLocalFile(path);
		}
		public void Host_OpenURL(SciterValue[] args)
		{
#if WINDOWS
			//PInvoke.User32.SetForegroundWindow(App.AppWindow._hwnd);
#endif
			State.LoadURL(args[0].Get(""));
		}
		public void Host_ClosePage() => State.ClosePage();
		public void Host_ReloadPageForced()// TODO
		{
			Process.Start(App.g_exe_path, State.page_current_url);
			App.AppWindow.Close();
		}

		public SciterValue Host_GetPageRootElement() => State.g_el_frameroot[0].ExpandoValue;
		public SciterValue Host_GetInspectedElement() => Inspecting.g_el_inspected != null ? Inspecting.g_el_inspected.ExpandoValue : null;
		public void Host_InspectElement(SciterValue[] args) => Inspecting.UserPageElemInspect(new SciterElement(args[0]));
		public void Host_RefreshElemDetails() => Inspecting.RefreshElemDetails();
		public SciterValue Host_GetFileLastWrite(SciterValue[] args) => new SciterValue(new FileInfo(args[0].Get("")).LastWriteTime.ToString());
	}
}