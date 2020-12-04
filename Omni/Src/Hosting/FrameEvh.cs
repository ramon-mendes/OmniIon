using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Omni.UI;
using SciterSharp;
using SciterSharp.Interop;

namespace Omni.Hosting
{
	class FrameEvh : SciterEventHandler
	{
		protected override void Subscription(SciterElement se, out SciterXBehaviors.EVENT_GROUPS event_groups)
		{
			event_groups = SciterXBehaviors.EVENT_GROUPS.HANDLE_BEHAVIOR_EVENT | SciterXBehaviors.EVENT_GROUPS.HANDLE_MOUSE;
		}

		protected override bool OnEvent(SciterElement elSource, SciterElement elTarget, SciterXBehaviors.BEHAVIOR_EVENTS type, IntPtr reason, SciterValue data)
		{
			if(type == SciterXBehaviors.BEHAVIOR_EVENTS.CONTENT_CHANGED)
			{
				Inspecting.OnContentChanged();
			}
			else if(type == SciterXBehaviors.BEHAVIOR_EVENTS.DOCUMENT_CREATED)
			{
				Inspecting.OnContentChanged();
				//State.AppHost.Notify(HostMain.NOTIFICATION.PAGE_LOADED)
			}
			else if(type == SciterXBehaviors.BEHAVIOR_EVENTS.DOCUMENT_COMPLETE)
			{
				// Sciter always sends a BEHAVIOR_EVENTS.CONTENT_CHANGED even before BEHAVIOR_EVENTS.DOCUMENT_COMPLETE
				//g_host.PostNotification(Host.NOTIFICATION.PAGE_LOADED);
			}
			else if(type == SciterXBehaviors.BEHAVIOR_EVENTS.CLICK)
			{

			}
			return base.OnEvent(elSource, elTarget, type, reason, data);
		}

		private bool _wasCtrlShiftMouseDown;

		protected override bool OnMouse(SciterElement se, SciterXBehaviors.MOUSE_PARAMS prms)
		{
#if OSX
			bool control_shift = AppKit.NSEvent.CurrentModifierFlags == (AppKit.NSEventModifierMask.ShiftKeyMask | AppKit.NSEventModifierMask.CommandKeyMask);
#else
			bool control_shift = prms.alt_state == (SciterXBehaviors.KEYBOARD_STATES.CONTROL_KEY_PRESSED | SciterXBehaviors.KEYBOARD_STATES.SHIFT_KEY_PRESSED); 
#endif

			int cmd = ((int)SciterXBehaviors.MOUSE_EVENTS.MOUSE_DOWN) | ((int)SciterXBehaviors.PHASE_MASK.SINKING);
			if((int)prms.cmd == cmd
				&& control_shift
				&& prms.button_state == 1)
			{
				SciterElement target = new SciterElement(prms.target);
				Inspecting.UserPageElemInspect(target);
				if(target.Parent != null)// BUG workaround for <text> elements
					App.AppHost.CallFunction("Extern_InspectElement", Inspecting.g_el_inspected.ExpandoValue);

				_wasCtrlShiftMouseDown = true;
				return true;
			}

			bool sinking = prms.cmd.HasFlag((SciterXBehaviors.MOUSE_EVENTS)SciterXBehaviors.PHASE_MASK.SINKING);
			if(_wasCtrlShiftMouseDown && sinking)
			{
				if(prms.cmd.HasFlag(SciterXBehaviors.MOUSE_EVENTS.MOUSE_UP) || prms.cmd.HasFlag(SciterXBehaviors.MOUSE_EVENTS.MOUSE_LEAVE))
					App.AppHost.InvokePost(() => _wasCtrlShiftMouseDown = false);
				return true;
			}

			/*if(prms.cmd == SciterXBehaviors.MOUSE_EVENTS.MOUSE_DOWN && prms.button_state == (uint)SciterXBehaviors.MOUSE_BUTTONS.PROP_MOUSE_BUTTON)// right-click + MOUSE_DOWN on bubbling
			{
				// user disabled?
				SciterValue sv = App.AppHost.EvalScript("View.omnidata.right_inspect_disable");
				if(sv.Get(false))
					return true;

				SciterElement target = new SciterElement(prms.target);
				Inspecting.UserPageElemInspect(target);
				if(target.Parent != null)// BUG workaround for <text> elements
					App.AppHost.CallFunction("Extern_InspectElement", target.ExpandoValue);

				return true;
			}*/
			return false;
		}
	}
}
