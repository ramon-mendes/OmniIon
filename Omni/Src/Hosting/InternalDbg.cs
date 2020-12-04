using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SciterSharp;
using SciterSharp.Interop;

namespace Omni.Hosting
{
	class InternalDbg : SciterDebugOutputHandler
	{
		private bool _in_output;
		private bool _pending_output;
		private List<LogItem> _logs = new List<LogItem>();
		public static bool _error_msg = false;

		public void InternalOutput(string text, bool post = false)// generates output by internal call
		{
			Utils.Assert_UIThread();
			Debug.Assert(!_in_output);
			Debug.WriteLine(text);
			Console.WriteLine(text);

			if(post)
			{
				_logs.Add(new LogItem
				{
					subsystem = SciterXDef.OUTPUT_SUBSYTEM.OT_DOM,
					severity = SciterXDef.OUTPUT_SEVERITY.OS_INFO,
					text = text
				});

				if(_pending_output == false)
				{
					_pending_output = true;
					App.AppHost.Notify(Host.NOTIFICATION.LOG_ARRIVED);
				}
			}
			else
			{
				SciterValue logval = new SciterValue();
				logval[0] = new SciterValue((int)SciterXDef.OUTPUT_SUBSYTEM.OT_DOM);
				logval[1] = new SciterValue((int)SciterXDef.OUTPUT_SEVERITY.OS_INFO);
				logval[2] = new SciterValue(text);

				SciterValue data = new SciterValue();
				data.Append(data);

				App.AppHost.CallFunction("Extern_ConsoleAppendLines", data);
			}
		}

		public void OnNotify_LOG_ARRIVED()
		{
			Utils.Assert_UIThread();
			Debug.Assert(_logs.Count != 0);

			SciterValue data = new SciterValue();
			foreach(var item in _logs)
			{
				SciterValue logval = new SciterValue();
				logval.SetItem(0, new SciterValue((int) item.subsystem));
				logval.SetItem(1, new SciterValue((int) item.severity));
				logval.SetItem(2, new SciterValue(item.text));
				data.Append(logval);
				
				Console.WriteLine(item.text);
			}

			_in_output = true;
			App.AppHost.CallFunction("Extern_ConsoleAppendLines", data);
			_in_output = false;

			_logs.Clear();
			_pending_output = false;
		}

		struct LogItem
		{
			public SciterXDef.OUTPUT_SUBSYTEM subsystem;
			public SciterXDef.OUTPUT_SEVERITY severity;
			public string text;
		}

		protected override void OnOutput(SciterXDef.OUTPUT_SUBSYTEM subsystem, SciterXDef.OUTPUT_SEVERITY severity, string text)
		{
			Console.WriteLine(text);

			if(severity == SciterXDef.OUTPUT_SEVERITY.OS_ERROR)
				_error_msg = true;

			if(_in_output)
				return;

			_logs.Add(new LogItem
			{
				subsystem = subsystem,
				severity = severity,
				text = text
			});

			if(_pending_output==false)
			{
				_pending_output = true;
				App.AppHost.Notify(Host.NOTIFICATION.LOG_ARRIVED);
			}
		}
	}
}