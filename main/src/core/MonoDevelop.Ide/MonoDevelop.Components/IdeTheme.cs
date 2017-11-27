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
using System.Linq;

#if MAC
using AppKit;
using Foundation;
using MonoDevelop.Components.Mac;
#endif

namespace MonoDevelop.Components
{
	public static class IdeTheme
	{
		internal static string DefaultTheme;
		internal static string DefaultGtkDataFolder;
		internal static string DefaultGtk2RcFiles;

		public static Theme UserInterfaceTheme { get; private set; }

		static IdeTheme ()
		{
			DefaultGtkDataFolder = Environment.GetEnvironmentVariable ("GTK_DATA_PREFIX");
			DefaultGtk2RcFiles = Environment.GetEnvironmentVariable ("GTK2_RC_FILES");
			// FIXME: Immediate theme switching disabled, until:
			//        MAC: NSAppearance issues are fixed
			//        WIN: spradic Gtk crashes on theme realoding are fixed
			//IdeApp.Preferences.UserInterfaceTheme.Changed += (sender, e) => UpdateGtkTheme ();
		}

		internal static void InitializeGtk (string progname, ref string[] args)
		{
			if (Gtk.Settings.Default != null)
				throw new InvalidOperationException ("Gtk already initialized!");
			
			//HACK: we must initilize some Gtk rc before Gtk.Application is initialized on Mac/Windows
			//      otherwise it will not be loaded correctly and theme switching won't work.
			if (!Platform.IsLinux)
				UpdateGtkTheme ();

#if MAC
			// Early init Cocoa through xwt
			var path = Path.GetDirectoryName (typeof (IdeTheme).Assembly.Location);
			System.Reflection.Assembly.LoadFrom (Path.Combine (path, "Xwt.XamMac.dll"));
			var loaded = Xwt.Toolkit.Load (Xwt.ToolkitType.XamMac);

			var disableA11y = Environment.GetEnvironmentVariable ("DISABLE_ATKCOCOA");
			if (Platform.IsMac && (NSUserDefaults.StandardUserDefaults.BoolForKey ("com.monodevelop.AccessibilityEnabled") && string.IsNullOrEmpty (disableA11y))) {
				// Load a private version of AtkCocoa stored in the XS app directory
				var appDir = Directory.GetParent (AppDomain.CurrentDomain.BaseDirectory);
				var gtkPath = $"{appDir.Parent.FullName}/lib/gtk-2.0";

				LoggingService.LogInfo ($"Loading modules from {gtkPath}");
				Environment.SetEnvironmentVariable ("GTK_MODULES", $"{gtkPath}/libatkcocoa.so");
			} else {
				// If we are restarted from a running instance when changing the accessibility setting then
				// we inherit the environment from it
				Environment.SetEnvironmentVariable ("GTK_MODULES", null);
				LoggingService.LogInfo ("Accessibility disabled");
			}
#endif
			Gtk.Application.Init (BrandingService.ApplicationName, ref args);

			// Reset our environment after initialization on Mac
			if (Platform.IsMac) {
				Environment.SetEnvironmentVariable ("GTK_MODULES", null);
				Environment.SetEnvironmentVariable ("GTK2_RC_FILES", DefaultGtk2RcFiles);
			}
		}

		internal static void SetupXwtTheme ()
		{
			Xwt.Drawing.Context.RegisterStyles ("dark", "disabled", "error");

			if (Core.Platform.IsMac) {
				Xwt.Drawing.Context.RegisterStyles ("mac", "sel");
				Xwt.Drawing.Context.SetGlobalStyle ("mac");
			} else if (Core.Platform.IsWindows) {
				Xwt.Drawing.Context.RegisterStyles ("win");
				Xwt.Drawing.Context.SetGlobalStyle ("win");
			} else if (Core.Platform.IsLinux) {
				Xwt.Drawing.Context.RegisterStyles ("linux");
				Xwt.Drawing.Context.SetGlobalStyle ("linux");
			}

			Xwt.Toolkit.CurrentEngine.RegisterBackend <Xwt.Backends.IWindowBackend, ThemedGtkWindowBackend>();
			Xwt.Toolkit.CurrentEngine.RegisterBackend <Xwt.Backends.IDialogBackend, ThemedGtkDialogBackend>();
		}

		internal static void SetupGtkTheme ()
		{
			if (Gtk.Settings.Default == null)
				return;
			
			if (Platform.IsLinux) {
				DefaultTheme = Gtk.Settings.Default.ThemeName;
				string theme = IdeApp.Preferences.UserInterfaceThemeName;
				if (string.IsNullOrEmpty (theme))
					theme = DefaultTheme;
				ValidateGtkTheme (ref theme);
				if (theme != DefaultTheme)
					Gtk.Settings.Default.ThemeName = theme;
				LoggingService.LogInfo ("GTK: Using Gtk theme from {0}", Path.Combine (Gtk.Rc.ThemeDir, Gtk.Settings.Default.ThemeName));
			} else
				DefaultTheme = "Light";

			// HACK: on Windows we have to load the theme twice on startup. During the first run we
			//       set the environment variables from InitializeGtk() and after Gtk initialization
			//       we set the active theme from here. Otherwise Gtk will preload the default theme with
			//       the Wimp engine, which can break our own configs.
			if (Platform.IsWindows)
				UpdateGtkTheme ();
		}

		internal static void UpdateGtkTheme ()
		{
			if (DefaultTheme == null)
				SetupGtkTheme ();

			string current_theme = IdeApp.Preferences.UserInterfaceThemeName;

			if (!Platform.IsLinux) {
				UserInterfaceTheme = IdeApp.Preferences.UserInterfaceThemeName == "Dark" ? Theme.Dark : Theme.Light;
				if (current_theme != UserInterfaceTheme.ToString ()) // Only theme names allowed on Win/Mac
					current_theme = UserInterfaceTheme.ToString ();
			}

			var use_bundled_theme = false;

			
			// Use the bundled gtkrc only if the Xamarin theme is installed
			if (File.Exists (Path.Combine (Gtk.Rc.ModuleDir, "libxamarin.so")) || File.Exists (Path.Combine (Gtk.Rc.ModuleDir, "libxamarin.dll")))
				use_bundled_theme = true;
			// on Windows we can't rely on Gtk.Rc.ModuleDir to be valid
			// and test additionally the default installation dir
			if (!use_bundled_theme && Platform.IsWindows) {
				var gtkBasePath = Environment.GetEnvironmentVariable ("GTK_BASEPATH");
				if (String.IsNullOrEmpty (gtkBasePath))
					gtkBasePath = "C:\\Program Files (x86)\\GtkSharp\\2.12\\";
				if (File.Exists (Path.Combine (gtkBasePath, "lib\\gtk-2.0\\2.10.0\\engines\\libxamarin.dll")))
				    use_bundled_theme = true;
			}
			
			if (use_bundled_theme) {
				
				if (!Directory.Exists (UserProfile.Current.ConfigDir))
					Directory.CreateDirectory (UserProfile.Current.ConfigDir);
				
				if (Platform.IsWindows) {
					// HACK: Gtk Bug: Rc.ReparseAll () and the include "[rcfile]" gtkrc statement are broken on Windows.
					//                We must provide our own XDG folder structure to switch bundled themes.
					var rc_themes = UserProfile.Current.ConfigDir.Combine ("share", "themes");
					var rc_theme_light = rc_themes.Combine ("Light", "gtk-2.0", "gtkrc");
					var rc_theme_dark = rc_themes.Combine ("Dark", "gtk-2.0", "gtkrc");
					if (!Directory.Exists (rc_theme_light.ParentDirectory))
						Directory.CreateDirectory (rc_theme_light.ParentDirectory);
					if (!Directory.Exists (rc_theme_dark.ParentDirectory))
						Directory.CreateDirectory (rc_theme_dark.ParentDirectory);

					string gtkrc = PropertyService.EntryAssemblyPath.Combine ("gtkrc");
					File.Copy (gtkrc + ".win32", rc_theme_light, true);
					File.Copy (gtkrc + ".win32-dark", rc_theme_dark, true);

					var themeDir = UserProfile.Current.ConfigDir;
					if (!themeDir.IsAbsolute)
						themeDir = themeDir.ToAbsolute (Environment.CurrentDirectory);
					Environment.SetEnvironmentVariable ("GTK_DATA_PREFIX", themeDir);

					// set the actual theme and reset the environment only after Gtk has been fully
					// initialized. See SetupGtkTheme ().
					if (Gtk.Settings.Default != null) {
						LoggingService.LogInfo ("GTK: Using Gtk theme from {0}", Path.Combine (Gtk.Rc.ThemeDir, current_theme));
						Gtk.Settings.Default.ThemeName = current_theme;
						Environment.SetEnvironmentVariable ("GTK_DATA_PREFIX", DefaultGtkDataFolder);
					}

				} else if (Platform.IsMac) {
					
					var gtkrc = "gtkrc.mac";
					if (IdeApp.Preferences.UserInterfaceTheme == Theme.Dark)
						gtkrc += "-dark";
					gtkrc = PropertyService.EntryAssemblyPath.Combine (gtkrc);

					LoggingService.LogInfo ("GTK: Using gtkrc from {0}", gtkrc);
					
					// Generate a dummy rc file and use that to include the real rc. This allows changing the rc
					// on the fly. All we have to do is rewrite the dummy rc changing the include and call ReparseAll
					var rcFile = UserProfile.Current.ConfigDir.Combine ("gtkrc");
					File.WriteAllText (rcFile, "include \"" + gtkrc + "\"");
					Environment.SetEnvironmentVariable ("GTK2_RC_FILES", rcFile);

					Gtk.Rc.ReparseAll ();

					// reset the environment only after Gtk has been fully initialized. See SetupGtkTheme ().
					if (Gtk.Settings.Default != null)
						Environment.SetEnvironmentVariable ("GTK2_RC_FILES", DefaultGtk2RcFiles);
				}

			} else if (Gtk.Settings.Default != null && current_theme != Gtk.Settings.Default.ThemeName) {
				LoggingService.LogInfo ("GTK: Using Gtk theme from {0}", Path.Combine (Gtk.Rc.ThemeDir, current_theme));
				Gtk.Settings.Default.ThemeName = current_theme;
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
				UserInterfaceTheme = HslColor.Brightness (bgColor) < 0.5 ? Theme.Dark : Theme.Light;
			}

			if (UserInterfaceTheme == Theme.Dark)
				Xwt.Drawing.Context.SetGlobalStyle ("dark");
			else
				Xwt.Drawing.Context.ClearGlobalStyle ("dark");

			Styles.LoadStyle ();
			UpdateXwtDefaults ();
			#if MAC
			UpdateMacWindows ();
			#endif
		}

		static void UpdateXwtDefaults ()
		{
			// Xwt default dialog icons
			Xwt.Toolkit.CurrentEngine.Defaults.MessageDialog.InformationIcon = ImageService.GetIcon ("gtk-dialog-info", Gtk.IconSize.Dialog);
			Xwt.Toolkit.CurrentEngine.Defaults.MessageDialog.WarningIcon = ImageService.GetIcon ("gtk-dialog-warning", Gtk.IconSize.Dialog);
			Xwt.Toolkit.CurrentEngine.Defaults.MessageDialog.ErrorIcon = ImageService.GetIcon ("gtk-dialog-error", Gtk.IconSize.Dialog);
			Xwt.Toolkit.CurrentEngine.Defaults.MessageDialog.QuestionIcon = ImageService.GetIcon ("gtk-dialog-question", Gtk.IconSize.Dialog);
			Xwt.Toolkit.CurrentEngine.Defaults.MessageDialog.ConfirmationIcon = ImageService.GetIcon ("gtk-dialog-question", Gtk.IconSize.Dialog);

			if (Platform.IsMac && UserInterfaceTheme == Theme.Dark) {
				// dark NSAppearance can not handle custom drawn images in dialogs
				Xwt.Toolkit.NativeEngine.Defaults.MessageDialog.InformationIcon = ImageService.GetIcon ("gtk-dialog-info", Gtk.IconSize.Dialog).ToBitmap (GtkWorkarounds.GetScaleFactor ());
				Xwt.Toolkit.NativeEngine.Defaults.MessageDialog.WarningIcon = ImageService.GetIcon ("gtk-dialog-warning", Gtk.IconSize.Dialog).ToBitmap (GtkWorkarounds.GetScaleFactor ());
				Xwt.Toolkit.NativeEngine.Defaults.MessageDialog.ErrorIcon = ImageService.GetIcon ("gtk-dialog-error", Gtk.IconSize.Dialog).ToBitmap (GtkWorkarounds.GetScaleFactor ());
				Xwt.Toolkit.NativeEngine.Defaults.MessageDialog.QuestionIcon = ImageService.GetIcon ("gtk-dialog-question", Gtk.IconSize.Dialog).ToBitmap (GtkWorkarounds.GetScaleFactor ());
				Xwt.Toolkit.NativeEngine.Defaults.MessageDialog.ConfirmationIcon = ImageService.GetIcon ("gtk-dialog-question", Gtk.IconSize.Dialog).ToBitmap (GtkWorkarounds.GetScaleFactor ());
			} else {
				Xwt.Toolkit.NativeEngine.Defaults.MessageDialog.InformationIcon = ImageService.GetIcon ("gtk-dialog-info", Gtk.IconSize.Dialog);
				Xwt.Toolkit.NativeEngine.Defaults.MessageDialog.WarningIcon = ImageService.GetIcon ("gtk-dialog-warning", Gtk.IconSize.Dialog);
				Xwt.Toolkit.NativeEngine.Defaults.MessageDialog.ErrorIcon = ImageService.GetIcon ("gtk-dialog-error", Gtk.IconSize.Dialog);
				Xwt.Toolkit.NativeEngine.Defaults.MessageDialog.QuestionIcon = ImageService.GetIcon ("gtk-dialog-question", Gtk.IconSize.Dialog);
				Xwt.Toolkit.NativeEngine.Defaults.MessageDialog.ConfirmationIcon = ImageService.GetIcon ("gtk-dialog-question", Gtk.IconSize.Dialog);
			}

			Xwt.Toolkit.CurrentEngine.Defaults.FallbackLinkColor = Styles.LinkForegroundColor;
			Xwt.Toolkit.NativeEngine.Defaults.FallbackLinkColor = Styles.LinkForegroundColor;
		}

		internal static string[] gtkThemeFallbacks = new string[] {
			"Xamarin",// the best!
			"Gilouche", // SUSE
			"Mint-X", // MINT
			"Radiance", // Ubuntu 'light' theme (MD looks better with the light theme in 4.0 - if that changes switch this one)
			"Clearlooks" // GTK theme
		};

		static void ValidateGtkTheme (ref string theme)
		{
			if (!MonoDevelop.Ide.Gui.OptionPanels.IDEStyleOptionsPanelWidget.IsBadGtkTheme (theme))
				return;

			var themes = MonoDevelop.Ide.Gui.OptionPanels.IDEStyleOptionsPanelWidget.InstalledThemes;

			string fallback = gtkThemeFallbacks
				.Select (fb => themes.FirstOrDefault (t => string.Compare (fb, t, StringComparison.OrdinalIgnoreCase) == 0))
				.FirstOrDefault (t => t != null);

			string message = "Theme Not Supported";

			string detail;
			if (themes.Count > 0) {
				detail =
					"Your system is using the '{0}' GTK+ theme, which is known to be very unstable. MonoDevelop will " +
					"now switch to an alternate GTK+ theme.\n\n" +
					"This message will continue to be shown at startup until you set a alternate GTK+ theme as your " +
					"default in the GTK+ Theme Selector or MonoDevelop Preferences.";
			} else {
				detail =
					"Your system is using the '{0}' GTK+ theme, which is known to be very unstable, and no other GTK+ " +
					"themes appear to be installed. Please install another GTK+ theme.\n\n" +
					"This message will continue to be shown at startup until you install a different GTK+ theme and " +
					"set it as your default in the GTK+ Theme Selector or MonoDevelop Preferences.";
			}

			MessageService.GenericAlert (Gtk.Stock.DialogWarning, message, BrandingService.BrandApplicationName (detail), AlertButton.Ok);

			theme = fallback ?? themes.FirstOrDefault () ?? theme;
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
			if (IdeApp.Preferences.UserInterfaceTheme == Theme.Light)
				window.Appearance = NSAppearance.GetAppearance (NSAppearance.NameAqua);
			else
				window.Appearance = NSAppearance.GetAppearance (NSAppearance.NameVibrantDark);

			if (IdeApp.Preferences.UserInterfaceTheme == Theme.Light) {
				window.StyleMask &= ~NSWindowStyle.TexturedBackground;
				window.BackgroundColor = MonoDevelop.Ide.Gui.Styles.BackgroundColor.ToNSColor ();
				return;
			}

			if (window is NSPanel || window.ContentView.Class.Name != "GdkQuartzView") {
				window.BackgroundColor = MonoDevelop.Ide.Gui.Styles.BackgroundColor.ToNSColor ();
				if (MacSystemInformation.OsVersion <= MacSystemInformation.Sierra)
					window.StyleMask |= NSWindowStyle.TexturedBackground;
			} else {
				object[] platforms = Mono.Addins.AddinManager.GetExtensionObjects ("/MonoDevelop/Core/PlatformService");
				if (platforms.Length > 0) {
					var platformService = (MonoDevelop.Ide.Desktop.PlatformService)platforms [0];
					var image = Xwt.Drawing.Image.FromResource (platformService.GetType().Assembly, "maintoolbarbg.png");

					window.IsOpaque = false;
					window.BackgroundColor = NSColor.FromPatternImage (image.ToBitmap().ToNSImage());
				}
				window.StyleMask |= NSWindowStyle.TexturedBackground;
			}
			if (MacSystemInformation.OsVersion >= MacSystemInformation.HighSierra && !window.IsSheet)
				window.TitlebarAppearsTransparent = true;
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

