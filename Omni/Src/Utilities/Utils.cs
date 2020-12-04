using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Omni
{
	static class Utils
	{
		private static int mainThreadId = Thread.CurrentThread.ManagedThreadId;

		public static void Assert_UIThread()
		{
			Debug.Assert(Thread.CurrentThread.ManagedThreadId == mainThreadId);
		}
	}

	internal class StopwatchAuto : Stopwatch
	{
		public StopwatchAuto()
		{
			Start();
		}

		[Conditional("DEBUG")]
		public void StopAndLog(string what = null)
		{
			Stop();

			if(what == null)
			{
				StackTrace stackTrace = new StackTrace();
				what = stackTrace.GetFrame(1).GetMethod().Name + "()";
			}
			Debug.WriteLine(what + " took " + ElapsedMilliseconds + "ms");
		}
	}
}