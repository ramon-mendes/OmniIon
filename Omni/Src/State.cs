using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SciterSharp;
using SciterSharp.Interop;
using Omni.Hosting;
using Omni.UI;

namespace Omni
{
	static class State
	{
		public static SciterElement g_el_root;
		public static SciterElement g_el_frameroot;
		public static SciterElement g_el_homeroot;

		public static void Setup()
		{
			ViewSetup();

#if DEBUG
			//App.AppHost.CallFunction("UX_ConsolePopup");
#endif
		}

		private static void ViewSetup()
		{
			// append to master CSS
			string css = "::mark(omni-hilite) { background-color: lime; } ::mark(omni-hiactual) { background-color: #2c63cb; color: white; }";
			byte[] cssbytes = Encoding.UTF8.GetBytes(css);
			SciterX.API.SciterAppendMasterCSS(cssbytes, (uint)cssbytes.Length);
			//SciterX.Use3264DLLNaming

			// access elements
			g_el_root = App.AppWindow.RootElement;
			g_el_frameroot = g_el_root.SelectFirstById("frameph");
			g_el_frameroot.AttachEvh(Host.FrameEvh);
			g_el_homeroot = g_el_root.SelectFirstById("frame-home");
			g_el_homeroot = g_el_homeroot[0];

			DOMTree.Setup();
		}

		public static void Reset()
		{
			g_el_frameroot.Clear();
			Inspecting.Reset();
			App.AppHost.CallFunction("Extern_InspectorReset");
			App.AppDebugger.CallFunction("Extern_ResetDebugger");
		}

		public static void ClosePage()
		{
			if(page_loaded)
			{
				Reset();
				
				page_loaded = false;
				page_current_file = null;
				page_current_url = null;
			}
		}

		public static void LoadURL(string url)
		{
			url = url.Trim();
			url = url.Trim('"');
			if(url.StartsWith("file://"))
				LoadLocalFile(url.Substring(7));
			else if(File.Exists(url))
				LoadLocalFile(url);
			else
				InternalLoad(url, false);
		}

		public static void LoadLocalFile(string filepath)
		{
#if WINDOWS
			filepath = filepath.Replace('/', '\\');
#endif
			if(!File.Exists(filepath))
			{
				App.AppWindow.ShowMessageBox(filepath + "\nThis file doesn't exists anymore.", "Omni");
				return;
			}

			Environment.CurrentDirectory = Path.GetDirectoryName(filepath);
			InternalLoad("file://" + filepath, true);

			page_current_file = filepath;
		}

		public static void ReloadPage()
		{
			Debug.Assert(page_loaded);

			Reset();
			if(page_current_file != null)
				LoadLocalFile(page_current_file);
			else
				LoadURL(page_current_url);
		}

		public static void OnPostedLoadPage()
		{
			//InternalLoad(g_pending_load_url, g_pending_load_is_file);
			LoadURL(pending_load_url);
		}

		// ----------------------------------------------------------------------------------------
		public static bool page_loaded { get; private set; }
		public static string page_current_file { get; private set; }
		public static string page_current_url { get; private set; }
		public static string pending_load_url { get; private set; }

		static void InternalLoad(string url, bool is_file)
		{
			if(page_loaded)
			{
				pending_load_url = url;
				ClosePage();
				App.AppHost.PostNotification(new IntPtr((int) Host.NOTIFICATION.LOAD_PAGE), IntPtr.Zero);
				return;
			}

			long took_load;
			long took_inspecting;
			Stopwatch sw = new Stopwatch();

			{
				sw.Start();
				page_loaded = true;
				page_current_url = url;
				page_current_file = null;

				App.AppDebugger.CallFunction("Extern_ReloadDebugger", new SciterValue(url));
				App.AppHost.CallFunction("View_LoadURL", new SciterValue(url), new SciterValue(is_file));
				took_load = sw.ElapsedMilliseconds;
			}

			if(page_loaded)// page inside debugger, and was closed
			{
				sw.Restart();
				Inspecting.Reload();
				took_inspecting = sw.ElapsedMilliseconds;
				sw.Stop();

				//g_host.eval_script("Settings.ns_data.config.show()");
				/*var sv_stats = AppHost.EvalScript("(Settings.ns_data.config#output-load-statistics)");
				if(sv_stats.Get(false))
				{
					Task.Run(() =>
					{
						Thread.Sleep(100);
						AppHost.InvokePost(() =>
						{
							Inspecting.CountElements();
							DbgOutput.InternalOutput($"{took_load}ms page load time; DOM count: {Inspecting.g_dom_count_elem} elements, {Inspecting.g_dom_count_node} nodes", true);
						});
					});
				}*/
			}
		}
	}
}