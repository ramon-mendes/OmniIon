using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using SciterSharp;
using SciterSharp.Interop;
using Omni.UI;

namespace Omni.Hosting
{
	class Host : BaseHost
	{
		public static HostEvh Evh { get; } = new HostEvh();
		public static FrameEvh FrameEvh { get; } = new FrameEvh();
		public static InternalDbg _sdh { get; } = new InternalDbg();

		public enum NOTIFICATION
		{
			LOG_ARRIVED,
			CONTENT_CHANGED,
			//PAGE_LOADED,
			LOAD_PAGE,
			CLOSE_PAGE,
		}

		public Host(SciterWindow wnd)
		{
            // inject console.tis and then omni.tis for every page
            byte[] byteArray = Encoding.UTF8.GetBytes("include \"scitersharp:console.tis\"; include \"archive://app/tis/omni.tis\";");
            GCHandle pinnedArray = GCHandle.Alloc(byteArray, GCHandleType.Pinned);
            IntPtr pointer = pinnedArray.AddrOfPinnedObject();
            SciterX.API.SciterSetOption(IntPtr.Zero, SciterXDef.SCITER_RT_OPTIONS.SCITER_SET_INIT_SCRIPT, pointer);
            pinnedArray.Free();

            var host = this;
			host.Setup(wnd);// injects LibConsole
			host.RegisterBehaviorHandler(typeof(WidgetTreePath));
			host.AttachEvh(Evh);
			host.SetupPage("index.html");
		}

		public void Notify(NOTIFICATION what)
		{
			PostNotification(new IntPtr((int)what), IntPtr.Zero);
		}

		protected override IntPtr OnPostedNotification(IntPtr wparam, IntPtr lparam)
		{
			Utils.Assert_UIThread();

			NOTIFICATION what = (NOTIFICATION)wparam.ToInt32();
			switch(what)
			{
				case NOTIFICATION.LOG_ARRIVED:
					_sdh.OnNotify_LOG_ARRIVED();
					break;
				case NOTIFICATION.CONTENT_CHANGED:
					Inspecting.OnPostedContentChange();
					break;
				case NOTIFICATION.LOAD_PAGE:
					State.OnPostedLoadPage();
					break;
				case NOTIFICATION.CLOSE_PAGE:
					State.ClosePage();
					break;
			}
			return IntPtr.Zero;
		}

		#region OnLoadData handling
		private Dictionary<string, ResourceLoad> _loading_resources = new Dictionary<string, ResourceLoad>();

		private struct ResourceLoad
		{
			public int index;
			public Stopwatch sw;
			public bool is_internal;
		}

		protected override SciterXDef.LoadResult OnLoadData(SciterXDef.SCN_LOAD_DATA sld)
		{
			string url = sld.uri;
			bool is_internal =
				State.g_el_root == null ||
				sld.principal == State.g_el_root._he || sld.principal == State.g_el_homeroot._he ||
#if DEBUG
				url.StartsWith(BaseHost._rescwd) ||
#endif
				url.StartsWith("archive://app/");
			
#if DEBUG
			bool bOK = true;
			//Debug.Assert(!is_internal || url.EndsWith("html") || url.EndsWith("tis"));
#else
			bool bOK = State.page_loaded && !is_internal;
#endif

			if(bOK && url.Length != 0)
			{
				SciterElement el_pr = sld.principal != IntPtr.Zero ? new SciterElement(sld.principal) : null;
				SciterElement el_in = sld.initiator != IntPtr.Zero ? new SciterElement(sld.initiator) : null;

				SciterValue sv = new SciterValue();
				sv["url"] = new SciterValue(url);
				if(el_pr != null)
					sv["principal"] = el_pr.ExpandoValue;
				if(el_in != null)
					sv["initiator"] = el_in.ExpandoValue;
				sv["dataType"] = new SciterValue((int)sld.dataType);

				ResourceLoad resource = new ResourceLoad()
				{
					sw = new Stopwatch(),
					is_internal = is_internal
				};
				resource.sw.Start();

				if(!is_internal)
				{
					var sv_idx = App.AppHost.CallFunction("Extern_NetworkNewResource", sv);
					resource.index = sv_idx.Get(-1);
				}

				_loading_resources[url] = resource;

				//bool is_page_source = el_pr.is_child_of(State.g_el_frameroot);
				//if(is_page_source)
			}

			return base.OnLoadData(sld);
		}

		private enum DEBUGGER_RESTYPE
		{
			INVALID,
			MARKUP,
			SCRIPT,
			INTERNAL
		}

		protected override void OnDataLoaded(SciterXDef.SCN_DATA_LOADED sdl)
		{
			string url = sdl.uri;

			uint status = sdl.status;
			uint size = sdl.dataSize;

			if(_loading_resources.ContainsKey(url))
			{
				ResourceLoad resource = _loading_resources[url];
				_loading_resources.Remove(url);
				resource.sw.Stop();

				SciterValue sv = new SciterValue();
				sv["url"] = new SciterValue(url);
				sv["index"] = new SciterValue(resource.index);
				sv["status"] = new SciterValue(sdl.status);
				sv["size"] = new SciterValue(sdl.dataSize);
				sv["dataType"] = new SciterValue((int)sdl.dataType);
				sv["loadTime"] = new SciterValue((int)resource.sw.ElapsedMilliseconds);

				if(sdl.dataSize != 0 &&
					(sdl.dataType == SciterXRequest.SciterResourceType.RT_DATA_HTML ||
					sdl.dataType == SciterXRequest.SciterResourceType.RT_DATA_STYLE ||
					sdl.dataType == SciterXRequest.SciterResourceType.RT_DATA_SCRIPT ||
					sdl.dataType == SciterXRequest.SciterResourceType.RT_DATA_RAW))
				{
					byte[] buffer = new byte[sdl.dataSize];
					Marshal.Copy(sdl.data, buffer, 0, buffer.Length);

					string txt;
					try
					{
						txt = Encoding.UTF8.GetString(buffer);
					}
					catch(Exception)
					{
						txt = "ERROR: file is not a correctly UTF-8 encoded";
					}

					sv["data"] = new SciterValue(txt);

					DEBUGGER_RESTYPE type = DEBUGGER_RESTYPE.INVALID;
					if(resource.is_internal)
						type = DEBUGGER_RESTYPE.INTERNAL;
					else if(sdl.dataType == SciterXRequest.SciterResourceType.RT_DATA_HTML)
						type = DEBUGGER_RESTYPE.MARKUP;
					else if(sdl.dataType == SciterXRequest.SciterResourceType.RT_DATA_SCRIPT)
						type = DEBUGGER_RESTYPE.SCRIPT;

					if(type != DEBUGGER_RESTYPE.INVALID && App.AppDebugger != null)
						App.AppDebugger.CallFunction("Extern_LoadedResource", sv, new SciterValue((int) type));
				}

				if(!resource.is_internal)
				{
					App.AppHost.CallFunction("Extern_NetworkLoadedResource", sv);
				}
			}
		}
		#endregion
	}
}