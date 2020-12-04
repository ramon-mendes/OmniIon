using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Omni.Hosting;
using SciterSharp;
using SciterSharp.Interop;

namespace Omni.UI
{
	static class Inspecting
	{
		// TODO C#: DOMTree modifies these variables, WTF?
		public static int g_dom_count_elem;
		public static int g_dom_count_node;

		public static SciterElement g_el_inspected;
		public static SciterElement g_el_highlighted;
		public static List<SciterElement> g_parentstack;
		public static bool in_content_change;

		static public void Reload()
		{
			Reset();
			DOMTree.Clear();
		}

		public static void Reset()
		{
			PageElemRemoveHighlight();
			g_el_inspected = null;
		}

		public static void UserPageElemInspect(SciterElement el)
		{
			App.AppHost.CallFunction("Inspector.ShowInspector", new SciterValue(0));

			// tree not constructed?
			SciterValue sv = App.AppHost.EvalScript("View.omnidata.dom_tree_disable");
			if(sv.Get(false) == true)
				DOMTree.Rebuild();

			/*wstring tag = el.get_tagname(); - why I had this? commented...
			if(tag=="text")
			{
				if(cast(int) el.parent)
					el = el.parent;
				else
					return;
			}*/

			PageElemInspect(el, true);
			PageElemHighlight(el);
		}

		public static void PageElemInspect(SciterElement el, bool tree_nav)
		{
			Debug.Assert(el != null);
			g_el_inspected = el;
			g_parentstack = DOMTree.TreeNavigateTo(el, tree_nav);
		}

		public static void PageElemHighlight(SciterElement el)
		{
			if(g_el_highlighted == el)// else it is called too many time when mouse moved
				return;

			if(el.Visible)
			{
				g_el_highlighted = el;
				App.AppHost.CallFunction("Extern_OutlineElement", el.ExpandoValue);
			}
			else
			{
				PageElemRemoveHighlight();
			}
		}

		public static void PageElemRemoveHighlight()
		{
			if(g_el_highlighted != null)
			{
				App.AppHost.CallFunction("Extern_OutlineClear");
				g_el_highlighted = null;
			}
		}

		public static void CountElements()
		{
			g_dom_count_elem = 0;
			g_dom_count_node = 0;

			Action<SciterNode> f_VisitNode = (SciterNode origin_nd) =>
			{
				g_dom_count_node++;
				Debug.Assert(origin_nd.ChildrenCount == 0);
			};

			Action<SciterElement> f_VisitElement = null;
			f_VisitElement = (SciterElement origin_el) =>
			{
				g_dom_count_elem++;

				SciterNode origin_nd = origin_el.ToNode();
				uint nchilds = origin_nd.ChildrenCount;
				for(uint i = 0; i < nchilds; i++)
				{
					SciterNode nd = origin_nd[i];
					if(nd.IsElement)
						f_VisitElement(nd.ToElement());
					else
						f_VisitNode(nd);
				}
			};

			SciterElement origin_el_root = State.g_el_frameroot[0];
			//debug assert(origin_el_root);
			if(origin_el_root != null)
				f_VisitElement(origin_el_root);
		}

		public static void OnContentChanged()
		{
			if(!in_content_change)
			{
				in_content_change = true;
				App.AppHost.Notify(Host.NOTIFICATION.CONTENT_CHANGED);
				PageElemRemoveHighlight();
			}
		}

		public static void OnPostedContentChange()
		{
			in_content_change = false;

			SciterValue sv = App.AppHost.EvalScript("View.omnidata.dom_tree_disable");
			if(sv.Get(false) == false)
			{
				DOMTree.Rebuild();
			}

			if(g_el_inspected != null)
			{
				// element might have been removed, so restore selection to its closest parent from its parent stack (g_parentstack)
				SciterElement el_sel = null;
				if(g_parentstack != null)
				{
					foreach(var item in g_parentstack)
					{
						IntPtr hwnd;
						if(SciterX.API.SciterGetElementHwnd(item._he, out hwnd, true) == SciterXDom.SCDOM_RESULT.SCDOM_OK)
						{
							el_sel = item;
							break;
						}
					}
				}
				if(el_sel != null)
				{
					//if(el_sel==g_el_inspected)
					//	PageElemHighlight(el_sel);// something to test out, it is anoying
					PageElemInspect(el_sel, true);
				}
			}
		}

		public static SciterValue RefreshElemDetails()
		{
			if(g_el_inspected != null)// && g_el_inspected.visible()
				return ElemDetails();
			return null;
		}

		public static SciterValue ElemDetails()
		{
			if(in_content_change)
				return null;

			SciterElement el = g_el_inspected;
			SciterValue r = new SciterValue();
			r["applied_rules"] = el.CallMethod("_applied_style_rules_");
			r["used_style"] = el.CallMethod("_used_style_properties_");
			r["states"] = new SciterValue((int)el.State);

			//import std.stdio; writeln("_applied_style_rules_ ", v1.to_string(), v1.is_undefined()); stdout.flush();
			//import std.stdio; writeln("_used_style_properties_ ", v2.to_string()); stdout.flush();

			return r;
		}
	}
}