//
// ConsoleCrayon.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;

namespace MonoDevelop.Core.Logging
{
	internal static class ConsoleCrayon
	{
	
		#region Public API
		
		private static ConsoleColor foreground_color;
		public static ConsoleColor ForegroundColor {
			get { return foreground_color; }
			set {
				foreground_color = value;
				SetColor (foreground_color, true);
			}
		}
		
		private static ConsoleColor background_color;
		public static ConsoleColor BackgroundColor {
			get { return background_color; }
			set {
				background_color = value;
				SetColor (background_color, false);
			}
		}
		
		public static void ResetColor ()
		{
			if (XtermColors) {
				Console.Write (GetAnsiResetControlCode ());
			} else if (Environment.OSVersion.Platform != PlatformID.Unix && !RuntimeIsMono) {
				Console.ResetColor ();
			}
		}
		
		private static void SetColor (ConsoleColor color, bool isForeground)
		{
			if (color < ConsoleColor.Black || color > ConsoleColor.White) {
				throw new ArgumentOutOfRangeException ("color", "Not a ConsoleColor value.");
			}
			
			if (XtermColors) {
				Console.Write (GetAnsiColorControlCode (color, isForeground));
			} else if (Environment.OSVersion.Platform != PlatformID.Unix && !RuntimeIsMono) {
				if (isForeground) {
					Console.ForegroundColor = color;
				} else {
					Console.BackgroundColor = color;
				}
			}
		}
		
		#endregion
		
		#region Ansi/VT Code Calculation
		
		// Modified from Mono's System.TermInfoDriver
		// License: MIT/X11
		// Authors: Gonzalo Paniagua Javier (gonzalo@ximian.com)
		// (C) 2005,2006 Novell, Inc (http://www.novell.com)
		
		static int TranslateColor (ConsoleColor desired, out bool light)
		{
			switch (desired) {
			// Dark colours
			case ConsoleColor.Black:
				light = false;
				return 0;
			case ConsoleColor.DarkRed:
				light = false;
				return 1;
			case ConsoleColor.DarkGreen:
				light = false;
				return 2;
			case ConsoleColor.DarkYellow:
				light = false;
				return 3;
			case ConsoleColor.DarkBlue:
				light = false;
				return 4;
			case ConsoleColor.DarkMagenta:
				light = false;
				return 5;
			case ConsoleColor.DarkCyan:
				light = false;
				return 6;
			case ConsoleColor.Gray:
				light = false;
				return 7;
			// Light colours
			case ConsoleColor.DarkGray:
				light = true;
				return 0;
			case ConsoleColor.Red:
				light = true;
				return 1;
			case ConsoleColor.Green:
				light = true;
				return 2;
			case ConsoleColor.Yellow:
				light = true;
				return 3;
			case ConsoleColor.Blue:
				light = true;
				return 4;
			case ConsoleColor.Magenta:
				light = true;
				return 5;
			case ConsoleColor.Cyan:
				light = true;
				return 6;
			default:
			case ConsoleColor.White:
				light = true;
				return 7;
			}
		}
		
		private static string GetAnsiColorControlCode (ConsoleColor colour, bool isForeground)
		{
			bool light;
			// lighter fg colours are 90 -> 97 rather than 30 -> 37
			// lighter bg colours are 100 -> 107 rather than 40 -> 47
			int code = TranslateColor (colour, out light) + (isForeground? 30 : 40) + (light? 60 : 0);
			return String.Format ("\x001b[{0}m", code);
		}
		
		private static string GetAnsiResetControlCode ()
		{
			return "\x001b[0m";
		}
		
		#endregion

		#region xterm Detection

		private static bool? xterm_colors = null;
		public static bool XtermColors { 
			get {
				if (xterm_colors == null) {
					DetectXtermColors ();
				}

				return xterm_colors.Value;
			}
		}
		
		[System.Runtime.InteropServices.DllImport ("libc", EntryPoint="isatty")]
		private extern static int _isatty (int fd);
		
		private static bool isatty (int fd)
		{
			try {
				return _isatty (fd) == 1;
			} catch {
				return false;
			}
		}
		
		private static void DetectXtermColors ()
		{
			bool _xterm_colors = false;
			
			switch (Environment.GetEnvironmentVariable ("TERM")) {
			case "xterm":
			case "linux":
				if (Environment.GetEnvironmentVariable ("COLORTERM") != null) {
					_xterm_colors = true;
				}
				break;
			case "xterm-color":
				_xterm_colors = true;
				break;
			}

			xterm_colors = _xterm_colors && isatty (1) && isatty (2);
		}

	#endregion
	
		#region Runtime Detection
		
		private static bool? runtime_is_mono;
		public static bool RuntimeIsMono {
			get { 
				if (runtime_is_mono == null) {
					runtime_is_mono = Type.GetType ("System.MonoType") != null;
				}

				return runtime_is_mono.Value;
			}
		}
	
	#endregion
	
	}
}
