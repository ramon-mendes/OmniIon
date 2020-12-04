#if OSX
using System;
using AppKit;
using Foundation;
using SciterSharp;
using SciterSharp.Interop;

namespace Omni
{
	class Program
	{
		static void Main(string[] args)
		{
			// Default GFX in Sciter v4 is Skia, switch to CoreGraphics (seems more stable)
			SciterX.API.SciterSetOption(IntPtr.Zero, SciterXDef.SCITER_RT_OPTIONS.SCITER_SET_GFX_LAYER, new IntPtr((int) SciterXDef.GFX_LAYER.GFX_LAYER_CG));

			NSApplication.Init();
			App.ParseArgs(args);

			using(var p = new NSAutoreleasePool())
			{
				var application = NSApplication.SharedApplication;
				application.Delegate = new AppDelegate();
				application.Run();
			}
		}
	}

	[Register("AppDelegate")]
	class AppDelegate : NSApplicationDelegate
	{
		static readonly SciterMessages sm = new SciterMessages();

		public override void DidFinishLaunching(NSNotification notification)
		{
			Mono.Setup();
			App.Setup();

			// Set our custom menu with Cocoa
			var menu = new NSMenu();
			menu.AddItem(new NSMenuItem("Quit", "q", (sender, e) => NSApplication.SharedApplication.Terminate(menu)));

			var menubar = new NSMenu();
			menubar.AddItem(new NSMenuItem { Submenu = menu });

			NSApplication.SharedApplication.MainMenu = menubar;
		}

		public override bool ApplicationShouldTerminateAfterLastWindowClosed(NSApplication sender)
		{
			return false;
		}

		public override void WillTerminate(NSNotification notification)
		{
			// Insert code here to tear down your application
		}
	}

	// In OSX/Xamarin Studio, make Sciter messages be shown at 'Application Output' panel
    class SciterMessages : SciterDebugOutputHandler
    {
        protected override void OnOutput(SciterSharp.Interop.SciterXDef.OUTPUT_SUBSYTEM subsystem, SciterSharp.Interop.SciterXDef.OUTPUT_SEVERITY severity, string text)
        {
            Console.WriteLine(text);
        }
    }
}
#endif