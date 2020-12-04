#if DEBUG
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Omni.Hosting;

namespace Omni
{
	static class Tests
	{
		public static void Run()
		{
			var SDK_path = App.AppWindow.EvalScript("Settings.ns_data.config.sdkpath").Get("");
			Debug.Assert(SDK_path != "");

			State.LoadLocalFile(SDK_path + @"\samples\carousel\carousel.htm");
			App.AppWindow.EvalScript("State.ClosePage()");
			Debug.Assert(InternalDbg._error_msg == false);
		}
	}
}
#endif
