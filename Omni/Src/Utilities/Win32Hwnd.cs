#if WINDOWS
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using PInvoke;

namespace Omni
{
	struct Win32Hwnd
	{
		public IntPtr Handle { get; private set; }

		public Win32Hwnd(IntPtr hwnd)
		{
			Handle = hwnd;
		}

		public bool ModifyStyle(User32.SetWindowLongFlags dwRemove, User32.SetWindowLongFlags dwAdd)
		{
			User32.SetWindowLongFlags dwStyle = (User32.SetWindowLongFlags) User32.GetWindowLong(Handle, User32.WindowLongIndexFlags.GWL_STYLE);
			User32.SetWindowLongFlags dwNewStyle = (dwStyle & ~dwRemove) | dwAdd;
			if(dwStyle == dwNewStyle)
				return false;

			User32.SetWindowLong(Handle, User32.WindowLongIndexFlags.GWL_STYLE, dwNewStyle);
			return true;
		}

		[DllImport("user32.dll", SetLastError = true)]
		public static extern IntPtr SetFocus(IntPtr hWnd);

		[DllImport("user32.dll", SetLastError = true)]
		public static extern IntPtr OpenIcon(IntPtr hWnd);
	}
}
#endif