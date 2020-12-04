#if WINDOWS
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ion;
using Omni.Hosting;
using SciterSharp;
using SciterSharp.Interop;

namespace Omni
{
	class DlgLicense : SciterWindow, IDisposable
	{
		public static DlgLicense Instance;
		public static DlgLicenseHost Host;

		public static void ShowDialog()
		{
			var wnd = Instance = new DlgLicense();
			wnd.CreateMainWindow(740, 520,
				SciterXDef.SCITER_CREATE_WINDOW_FLAGS.SW_MAIN |
				SciterXDef.SCITER_CREATE_WINDOW_FLAGS.SW_ENABLE_DEBUG |
				SciterXDef.SCITER_CREATE_WINDOW_FLAGS.SW_POPUP |
				SciterXDef.SCITER_CREATE_WINDOW_FLAGS.SW_TITLEBAR
				);
			wnd.CenterTopLevelWindow();
			wnd.Title = Consts.AppName;
#if WINDOWS
			wnd.Icon = Properties.Resources.IconMain;
			new Win32Hwnd(wnd._hwnd).ModifyStyle(0, PInvoke.User32.SetWindowLongFlags.WS_SYSMENU);
#endif

			Host = new DlgLicenseHost(wnd);

			// For testing to work
			wnd.Show();

#if false
			PInvokeWindows.MSG msg;
			while(PInvokeWindows.GetMessage(out msg, IntPtr.Zero, 0, 0) != 0)
			{
				PInvokeWindows.TranslateMessage(ref msg);
				PInvokeWindows.DispatchMessage(ref msg);
			}
#else
			wnd.ShowModal();
#endif
		}

		public void Dispose()
		{
			Destroy();

			Instance = null;
			Host = null;
		}
	}

	class DlgLicenseHost : BaseHost
	{
		public DlgLicenseHost(SciterWindow wnd)
		{
			Setup(wnd);
			AttachEvh(new DlgLicenseEVH());

			SetupPage("licensing.html");
		}

		protected override SciterXDef.LoadResult OnLoadData(SciterXDef.SCN_LOAD_DATA sld)
		{
			if(sld.uri.StartsWith("sciter:debug-peer.tis"))
			{
				return SciterXDef.LoadResult.LOAD_OK;
			}
			return base.OnLoadData(sld);
		}
	}

	class DlgLicenseEVH : SciterEventHandler
	{
        public SciterValue Host_IonExpando() => IonApp.GetUIExpando();
	}
}
#endif