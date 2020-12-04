using System;
using SciterSharp;

namespace Omni.Hosting
{
	public class Window : SciterWindow
	{
		public Window()
		{
			var wnd = this;
			wnd.CreateMainWindow(100, 100);// size is adjusted at index.html
			wnd.Title = "Omni";
			#if WINDOWS
			wnd.Icon = Properties.Resources.IconMain;
			#endif
		}
	}
}