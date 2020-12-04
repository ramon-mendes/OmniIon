using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;
using SciterSharp;
using SciterSharp.Interop;

namespace Omni.UI
{
	class WidgetTreePath : SciterEventHandler
	{
		public static WidgetTreePath Instance { get; private set; }

		// TagPath handling -----------------------------------------------------------------------------
		private SciterElement _se;
		private List<uint> actual_uidstack;

		public WidgetTreePath()
		{
			Instance = this;
		}

		protected override void Attached(SciterElement se)
		{
			_se = se;
		}

		protected override void Detached(SciterElement se)
		{
		}

		public void ResetTagPath()
		{
			actual_uidstack = null;

			var lis = _se.SelectAll("li");
			foreach(var item in lis)
			{
				item.Delete();
			}
		}

		public void RecreateTagPath(uint uid, List<uint> uidstack, List<string> tagpath)
		{
			if(actual_uidstack != null
				&& actual_uidstack.Count >= uidstack.Count
				&& Enumerable.SequenceEqual(uidstack, actual_uidstack.Take(uidstack.Count)))
			{
				var r = _se.ChildrenCount;
				var rrrr = _se.SelectAll("*");

				var el_li = _se.SelectAll("li").Single(li => li.ExpandoValue["uuid"].Get(-1) == uid);
				el_li.SetState(SciterXDom.ELEMENT_STATE_BITS.STATE_CURRENT);
			}
			else
			{
				ResetTagPath();
				actual_uidstack = uidstack;

				for(int i = 0; i < tagpath.Count; i++)
				{
					var el_li = SciterElement.Create("li");
					_se.Append(el_li);

					el_li.SetHTML("<text>" + tagpath[i] + "</text>");
					el_li.ExpandoValue["uuid"] = new SciterValue(uidstack[i]);

					if(uidstack[i] == uid)
						el_li.SetState(SciterXDom.ELEMENT_STATE_BITS.STATE_CURRENT);
				}

				App.AppHost.InvokePost(() => _se.Refresh());
			}
		}
	}
}