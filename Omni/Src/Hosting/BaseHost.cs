using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SciterSharp;
using SciterSharp.Interop;
using System.Reflection;
using System.Drawing;

namespace Omni.Hosting
{
	class BaseHost : SciterHost
	{
		protected static SciterX.ISciterAPI _api = SciterX.API;
		protected static SciterArchive _archive = new SciterArchive();
		protected SciterWindow _wnd;
		public static readonly string _rescwd;

		static BaseHost()
		{
			_archive.Open(SciterAppResource.ArchiveResource.resources);

#if DEBUG
			_rescwd = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location).Replace('\\', '/');
#if OSX
			_rescwd += "/../../../../../res/";
#else
			_rescwd += "/../../res/";
#endif
			_rescwd = Path.GetFullPath(_rescwd).Replace('\\', '/');
			Debug.Assert(Directory.Exists(_rescwd));
#endif
		}

		public void Setup(SciterWindow wnd)
		{
			_wnd = wnd;
			SetupWindow(wnd._hwnd);

#if DEBUG
			wnd.SetMediaVars(SciterValue.FromDictionary(new Dictionary<string, string> {
				{ "debug", "true" }
			}));
#endif
		}

		public void SetupPage(string page_from_res_folder)
		{
			string path = _rescwd + page_from_res_folder;
			Debug.Assert(File.Exists(path));

#if DEBUG
			string url = "file://" + path;
#else
			string url = "archive://app/" + page_from_res_folder;
#endif

			bool res = _wnd.LoadPage(url);
			Debug.Assert(res);
		}

		protected override SciterXDef.LoadResult OnLoadData(SciterXDef.SCN_LOAD_DATA sld)
		{
#if WINDOWS
			// Go figure
			//State.AppWindow.Icon = Properties.Resources.IconMain;
			_wnd.Icon = Properties.Resources.IconMain;
			PInvoke.User32.SendMessage(_wnd._hwnd, PInvoke.User32.WindowMessage.WM_SETICON, IntPtr.Zero, new Icon(Properties.Resources.IconMain, 16, 16).Handle);
#endif

			if(sld.uri.StartsWith("archive://app/"))
			{
				// load resource from SciterArchive
				string path = sld.uri.Substring(14);
				byte[] data = _archive.Get(path);
				if(data != null)
					_api.SciterDataReady(_wnd._hwnd, sld.uri, data, (uint)data.Length);
			}
			return base.OnLoadData(sld);
		}

		public static byte[] LoadResource(string path)
		{
#if DEBUG
			path = _rescwd + path;
			Debug.Assert(File.Exists(path));
			return File.ReadAllBytes(path);
#else
			return _archive.Get(path);
#endif
		}

		public static string LoadResourceString(string path)
		{
			return Encoding.UTF8.GetString(_archive.Get(path));
		}
	}
}