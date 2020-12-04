using Omni.Hosting;
using SciterSharp;
using SciterSharp.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Omni.UI
{
	/*
	-toda a árvore será nativa!
	-é mais facil debugar
	-não tem mto cross-scripting
	*/

	static class DOMTree
	{
		private static SciterElement el_tree;
		private static bool dbg_on_navigate;
		private static TreeEvh tree_evh;

		public static void Setup()
		{
			el_tree = State.g_el_root.SelectFirstById("dom-tree");
			Debug.Assert(el_tree != null);
			el_tree.AttachEvh(tree_evh = new TreeEvh());
		}

		public static void Clear()
		{
			el_tree.Clear();
			WidgetTreePath.Instance.ResetTagPath();// TODO: uncomment, started crashing on Sciter 3.3.2.12
		}

		public static void Rebuild()
		{
			Stopwatch sw = new Stopwatch(); ;
			sw.Start();

			Clear();

			if(State.g_el_frameroot.ChildrenCount == 0)
				return;// you removed the <html> element bastard

			Inspecting.g_dom_count_elem = 0;
			Inspecting.g_dom_count_node = 0;

			SciterElement origin_el_root = State.g_el_frameroot[0];
			Debug.Assert(origin_el_root != null);
			origin_el_root.SetAttribute("omni", "");

			AddElement(el_tree, origin_el_root);

			sw.Stop();

			SciterValue sv = App.AppHost.EvalScript("View.omnidata.show_tree_timing");
			if(sv.Get(false))
				Host._sdh.InternalOutput($"[Omni internal] DOM-tree Rebuild time: {sw.ElapsedMilliseconds}ms");
		}

		public static void Test()
		{
			//OnPageElemHightligth(SciterHost.g_el_frameroot[0][0][0]);
			//auto el = el_tree[0][2];
			//el.set_state(ELEMENT_STATE_BITS.STATE_EXPANDED);

			SciterElement origin_el = State.g_el_frameroot[0][1][0];
			//TreeNavigateTo(origin_el);
		}

		public static List<SciterElement> TreeNavigateTo(SciterElement origin_el, bool tree_nav)
		{
			Debug.Assert(origin_el != null);

			List<SciterElement> elstack = new List<SciterElement>();
			List<uint> uidstack = new List<uint>();
			List<string> tagnames = new List<string>();

			SciterElement el_it = origin_el;
			do
			{
				elstack.Add(el_it);
				tagnames.Add(el_it.TagName());
				uidstack.Add(el_it.UID);

				var el_parent = el_it.Parent;
				if(el_parent == null)
					return null;

				el_it = el_parent;
			} while(el_it._he != State.g_el_frameroot._he);

			uidstack.Reverse();
			tagnames.Reverse();

			SciterValue sv_uid = new SciterValue(origin_el.UID);
			SciterValue sv_uidstack = SciterValue.FromList(uidstack);
			SciterValue sv_tagnames = SciterValue.FromList(tagnames);

			if(tree_nav)
			{
				dbg_on_navigate = true;
				el_tree.CallMethod("NavigateToPath", sv_uid, sv_uidstack);// does not generate Event.SELECT_SELECTION_CHANGED
				dbg_on_navigate = false;
			}

			WidgetTreePath.Instance.RecreateTagPath(origin_el.UID, uidstack, tagnames);

			SciterValue sv_details = Inspecting.ElemDetails();
			if(sv_details!=null && !sv_details.IsUndefined)// happens while waiting for CONTENT_CHANGED arriving
			{
				el_tree.CallMethod("RenderDetails", sv_details);
			}

			return elstack;
		}

		// tree behavior and events
		private class TreeEvh : SciterEventHandler
		{
			protected override void Attached(SciterElement se)
			{
				base.Attached(se);
			}

			protected override void Subscription(SciterElement se, out SciterXBehaviors.EVENT_GROUPS event_groups)
			{
				event_groups = SciterXBehaviors.EVENT_GROUPS.HANDLE_BEHAVIOR_EVENT;
			}

			protected override bool OnScriptCall(SciterElement se, string name, SciterValue[] args, out SciterValue result)
			{
				result = null;

				switch(name)
				{
					case "Host_OnOptionSelect":
						{
							Debug.Assert(dbg_on_navigate == false);
							uint uid = (uint)el_tree.Value.Get(0);
							SciterElement el_origin = App.AppWindow.ElementByUID(uid);
							Inspecting.PageElemInspect(el_origin, false);
							return true;
						}

					case "Host_OnOptionDelete":
						{
							uint uid = (uint)el_tree.Value.Get(0);
							SciterElement el_origin = App.AppWindow.ElementByUID(uid);
							if(el_origin != State.g_el_frameroot[0])// not allowed to delete <html>
								el_origin.Delete();
							Inspecting.PageElemRemoveHighlight();// avoids an A/V
							return true;
						}

					case "Host_OnOptionHover":
						{
							uint uid = (uint)args[0].Get(0);
							SciterElement el_origin = App.AppWindow.ElementByUID(uid);
							if(el_origin != null)// happens when deleting element and the mouse is over it in the DOM tree
							{
								Inspecting.PageElemHighlight(el_origin);
							}
							return true;
						}

					case "Host_OnEndHover":
						Inspecting.PageElemRemoveHighlight();
						return true;

					case "Host_OnTagPathClick":
						el_tree.Value = args[0];
						el_tree.SendEvent((uint)SciterXBehaviors.BEHAVIOR_EVENTS.SELECT_SELECTION_CHANGED);
						return true;
				}
				return false;
			}
		}

		// tree DOM handling
		private static void AddElement(SciterElement tree_el_parent, SciterElement origin_el_add)
		{
			SciterNode origin_nd = origin_el_add.ToNode();
			Debug.Assert(origin_nd.IsElement);

			string tag = origin_el_add.Tag;
			SciterElement tree_el_new = SciterElement.Create("option");
			tree_el_parent.Append(tree_el_new);
			uint uid = origin_el_add.UID; Debug.Assert(uid != 0);
			tree_el_new.SetAttribute("value", uid.ToString());
			tree_el_new.SetHTML(UI_ElementCaption(origin_el_add));


			bool has_real_childs = false;
			uint nchilds = origin_nd.ChildrenCount;
			for(uint i = 0; i < nchilds; i++)
			{
				SciterNode nd = origin_nd[i];
				if(nd.IsElement)
				{
					AddElement(tree_el_new, nd.ToElement());
					has_real_childs = true;
				}
				else
				{
					bool new_option = AddNode(tree_el_new, uid, nd);
					if(new_option)
						has_real_childs = true;
				}
			}

			if(has_real_childs)
			{
				if(tag == "html" || tag == "body")
					tree_el_new.SetState(SciterXDom.ELEMENT_STATE_BITS.STATE_EXPANDED);//tree_el_new.set_attribute("expanded", "");
				else
					tree_el_new.SetState(SciterXDom.ELEMENT_STATE_BITS.STATE_COLLAPSED);
			}
		}

		private static bool AddNode(SciterElement tree_el_parent, uint origin_parent_uid, SciterNode origin_nd)
		{
			Debug.Assert(origin_nd.ChildrenCount == 0);

			if(origin_nd.IsComment)
			{
				return false;
			}

			if(string.IsNullOrWhiteSpace(origin_nd.Text))
			{
				return false;
			}

			SciterElement tree_el_new = SciterElement.Create("option");
			tree_el_parent.Append(tree_el_new);
			tree_el_new.SetAttribute("value", origin_parent_uid.ToString());// UID
			tree_el_new.Text = origin_nd.Text;
			return true;
		}

		public static string UI_ElementCaption(SciterElement origin_el_add)
		{
			string tag = origin_el_add.Tag;
			StringBuilder sb = new StringBuilder();
			sb.Append($"<span.head>&lt;{tag}</span>");

			foreach(var item in origin_el_add.Attributes)
			{
				sb.Append($" <span.attrn>{item.Key}=</span>\"<span.attrv>{item.Value}</span>\"");
			}

			sb.Append($"<span.head>&gt;&lt;/{tag}&gt;</span>");

			if(origin_el_add.Visible)
				return "<text>" + sb.ToString() + "</text>";
			return "<text .invisible>" + sb.ToString() + "</text>";
		}

		/*
		This is a reminiscence of CONTENT_CHANGED handling where only the subtree is update, something for the future

		void RefreshNode(element origin_el_parent)
		{
			auto t = origin_el_parent.get_tagname;
			auto uid = origin_el_parent.get_element_uid;

			element tree_el_from = el_tree.find_first("[value=%d]".format(origin_el_parent.get_element_uid()));
			assert(tree_el_from);

			//element tree_parent = tree_el_from.parent();
			tree_el_from.clear;
			//AddNode(tree_parent, origin_el_parent);

			int nchilds = origin_el_parent.children_count();
			if(nchilds!=0)
			{
				for(int i=0; i<nchilds; i++)
				AddNode(tree_el_from, element(origin_el_parent[i]));
			}
		}*/
	}
}
