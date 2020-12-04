using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SciterSharp;

namespace Omni
{
	static class UtilsExtension
	{
		public static string TagName(this SciterElement el)
		{
			Debug.Assert(el != null);

			string name = el.Tag;
			string id = el.GetAttribute("id");
			string classes = el.GetAttribute("class");
			if(id != null)
				name += id;
			if(classes != null)
				name += string.Join(".", classes.Split(' '));
			return name;
		}
	}
}