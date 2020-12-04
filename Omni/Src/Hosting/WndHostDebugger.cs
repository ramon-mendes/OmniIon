using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SciterSharp;
using SciterSharp.Interop;
using System.Diagnostics;
#if WINDOWS
using PInvoke;
#endif

namespace Omni.Hosting
{
	class DebuggerHost : BaseHost
	{
		public static DebuggerWindow Wnd { get; } = new DebuggerWindow();
		public static DebuggerEvh Evh { get; } = new DebuggerEvh();

		public DebuggerHost()
		{
			Setup(Wnd);
			AttachEvh(Evh);
			SetupPage("debugger/debugger.html");
		}
	}

	class DebuggerWindow : SciterWindow
	{
		public DebuggerWindow()
		{
			var flags = SciterXDef.SCITER_CREATE_WINDOW_FLAGS.SW_TOOL |
				SciterXDef.SCITER_CREATE_WINDOW_FLAGS.SW_RESIZEABLE |
				SciterXDef.SCITER_CREATE_WINDOW_FLAGS.SW_TITLEBAR |
				SciterXDef.SCITER_CREATE_WINDOW_FLAGS.SW_CONTROLS;

			var wnd = this;
			wnd.CreateOwnedWindow(App.AppWindow._hwnd, 1200, 700, flags);
			wnd.CenterTopLevelWindow();
			wnd.Title = "Debugger";
#if WINDOWS
			wnd.Icon = Properties.Resources.IconMain;
#endif
		}

#if WINDOWS
		protected override bool ProcessWindowMessage(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam, ref IntPtr lResult)
		{
			switch((User32.WindowMessage) msg)
			{
				case User32.WindowMessage.WM_SYSCOMMAND:
					if(wParam.ToInt32() == (int) User32.SysCommands.SC_KEYMENU)
						return true;
					return false;

				case User32.WindowMessage.WM_DESTROY:
					//Debug.Assert(false);
					return false;
			}
			return false;
		}
#endif
	}

	class DebuggerEvh : SciterEventHandler
	{
		protected override bool OnScriptCall(SciterElement se, string name, SciterValue[] args, out SciterValue result)
		{
			result = null;

			switch(name)
			{
				case "Host_OpenDebugger":
#if WINDOWS
					if(DebuggerHost.Wnd.IsVisible)
						User32.SetForegroundWindow(DebuggerHost.Wnd._hwnd);
#endif
					DebuggerHost.Wnd.Show();
					return true;
			}
			
			return Host.Evh.HandleScriptCall(se, name, args, out result);
		}
	}
}