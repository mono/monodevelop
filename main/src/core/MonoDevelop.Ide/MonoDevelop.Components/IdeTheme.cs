//
// ThemeExtensions.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@xamarin.com>
//
// Copyright (c) 2015 Xamarin, Inc (http://www.xamarin.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Collections.Generic;
using System.IO;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;

#if MAC
using AppKit;
using Foundation;
using MonoDevelop.Components.Mac;
#endif

namespace MonoDevelop.Components
{
	public static class IdeTheme
	{
		public static Skin UserInterfaceSkin { get; private set; }

		static IdeTheme ()
		{
			IdeApp.Preferences.UserInterfaceTheme.Changed += Preferences_UserInterfaceThemeChanged;
		}

		internal static void SetupXwtTheme ()
		{
			Xwt.Drawing.Context.RegisterStyles ("dark", "sel", "disabled");
			Xwt.Toolkit.CurrentEngine.RegisterBackend <Xwt.Backends.IWindowBackend, ThemedGtkWindowBackend>();
			Xwt.Toolkit.CurrentEngine.RegisterBackend <Xwt.Backends.IDialogBackend, ThemedGtkDialogBackend>();
		}

		internal static void UpdateGtkTheme ()
		{
			if (!Platform.IsLinux)
				UserInterfaceSkin = IdeApp.Preferences.UserInterfaceTheme == "Dark" ? Skin.Dark : Skin.Light;
			
			// Use the bundled gtkrc only if the Xamarin theme is installed
			if (File.Exists (Path.Combine (Gtk.Rc.ModuleDir, "libxamarin.so")) || File.Exists (Path.Combine (Gtk.Rc.ModuleDir, "libxamarin.dll"))) {

				var gtkrc = "gtkrc";
				if (Platform.IsWindows) {
					gtkrc += ".win32";
					if (IdeApp.Preferences.UserInterfaceSkin == Skin.Dark)
						gtkrc += "-dark";
				} else if (Platform.IsMac) {
					gtkrc += ".mac";
					if (IdeApp.Preferences.UserInterfaceSkin == Skin.Dark)
						gtkrc += "-dark";
				}

				var gtkrcf = PropertyService.EntryAssemblyPath.Combine (gtkrc);
				LoggingService.LogInfo ("GTK: Using gtkrc from {0}", gtkrcf);

				if (Platform.IsWindows) {
					Environment.SetEnvironmentVariable ("GTK2_RC_FILES", gtkrcf);
				} else if (Platform.IsMac) {
					// Generate a dummy rc file and use that to include the real rc. This allows changing the rc
					// on the fly. All we have to do is rewrite the dummy rc changing the include and call ReparseAll
					var rcFile = UserProfile.Current.ConfigDir.Combine ("gtkrc");
					if (!Directory.Exists (UserProfile.Current.ConfigDir))
						Directory.CreateDirectory (UserProfile.Current.ConfigDir);
					File.WriteAllText (rcFile, "include \"" + gtkrcf + "\"");
					Environment.SetEnvironmentVariable ("GTK2_RC_FILES", rcFile);
				}
				Gtk.Rc.ReparseAll ();
			}

			// let Gtk realize the new theme
			// Style is being updated by DefaultWorkbench.OnStyleSet ()
			// This ensures that the theme and all styles have been loaded when
			// the Styles.Changed event is raised.
			//GLib.Timeout.Add (50, delegate { UpdateStyles(); return false; });
		}

		internal static void UpdateStyles ()
		{
			if (Platform.IsLinux) {
				var defaultStyle = Gtk.Rc.GetStyle (IdeApp.Workbench.RootWindow);
				var bgColor = defaultStyle.Background (Gtk.StateType.Normal);
				UserInterfaceSkin = HslColor.Brightness (bgColor) < 0.5 ? Skin.Dark : Skin.Light;
			}

			if (UserInterfaceSkin == Skin.Dark)
				Xwt.Drawing.Context.SetGlobalStyle ("dark");
			else
				Xwt.Drawing.Context.ClearGlobalStyle ("dark");

			Styles.LoadStyle ();
			#if MAC
			UpdateMacWindows ();
			#endif
		}

#if MAC
		static Dictionary<NSWindow, NSObject> nsWindows = new Dictionary<NSWindow, NSObject> ();

		public static void ApplyTheme (NSWindow window)
		{
			if (!nsWindows.ContainsKey(window)) {
				nsWindows [window] = NSNotificationCenter.DefaultCenter.AddObserver (NSWindow.WillCloseNotification, OnClose, window);
				SetTheme (window);
			}
		}

		static void SetTheme (NSWindow window)
		{
			if (IdeApp.Preferences.UserInterfaceSkin == Skin.Light)
				window.Appearance = NSAppearance.GetAppearance (NSAppearance.NameAqua);
			else
				window.Appearance = NSAppearance.GetAppearance (NSAppearance.NameVibrantDark);

			if (window is NSPanel)
				window.BackgroundColor = MonoDevelop.Ide.Gui.Styles.BackgroundColor.ToNSColor ();
			else {
				object[] platforms = Mono.Addins.AddinManager.GetExtensionObjects ("/MonoDevelop/Core/PlatformService");
				if (platforms.Length > 0) {
					var platformService = (MonoDevelop.Ide.Desktop.PlatformService)platforms [0];
					var image = Xwt.Drawing.Image.FromResource (platformService.GetType().Assembly, "maintoolbarbg.png");

					window.IsOpaque = false;
					window.BackgroundColor = NSColor.FromPatternImage (image.ToBitmap().ToNSImage());
				}
				if (window.ContentView.Class.Name != "GdkQuartzView") {
					window.ContentView.WantsLayer = true;
					window.ContentView.Layer.BackgroundColor = MonoDevelop.Ide.Gui.Styles.BackgroundColor.ToCGColor ();
				}
			}
			window.StyleMask |= NSWindowStyle.TexturedBackground;
		}

		static void OnClose (NSNotification note)
		{
			var w = (NSWindow)note.Object;
			NSNotificationCenter.DefaultCenter.RemoveObserver(nsWindows[w]);
			nsWindows.Remove (w);
		}

		static void UpdateMacWindows ()
		{
			foreach (var w in nsWindows.Keys)
				SetTheme (w);
		}

		static void OnGtkWindowRealized (object s, EventArgs a)
		{
			var nsw = MonoDevelop.Components.Mac.GtkMacInterop.GetNSWindow ((Gtk.Window) s);
			if (nsw != null)
				ApplyTheme (nsw);
		}
#endif

		static void Preferences_UserInterfaceThemeChanged (object sender, EventArgs e)
		{
			UpdateGtkTheme ();
		}

		public static void ApplyTheme (this Gtk.Window window)
		{
			#if MAC
			window.Realized += OnGtkWindowRealized;
			if (window.IsRealized) {
				var nsw = MonoDevelop.Components.Mac.GtkMacInterop.GetNSWindow (window);
				if (nsw != null)
					ApplyTheme (nsw);
			}
			#endif
		}
	}

	public class ThemedGtkWindowBackend : Xwt.GtkBackend.WindowBackend
	{
		public override void Initialize ()
		{
			base.Initialize ();
			IdeTheme.ApplyTheme (Window);
		}
	}

	public class ThemedGtkDialogBackend : Xwt.GtkBackend.DialogBackend
	{
		public override void Initialize ()
		{
			base.Initialize ();
			IdeTheme.ApplyTheme (Window);
		}
	}
}

