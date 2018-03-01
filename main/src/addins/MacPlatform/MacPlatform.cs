//
// MacPlatformService.cs
//
// Author:
//   Geoff Norton  <gnorton@novell.com>
//   Michael Hutchinson <m.j.hutchinson@gmail.com>
//
// Copyright (C) 2007-2011 Novell, Inc (http://www.novell.com)
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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

using AppKit;
using Foundation;
using CoreGraphics;

using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.Core.Instrumentation;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Ide.Desktop;
using MonoDevelop.MacInterop;
using MonoDevelop.Components;
using MonoDevelop.Components.MainToolbar;
using MonoDevelop.Components.Extensions;
using System.Runtime.InteropServices;
using ObjCRuntime;
using System.Diagnostics;
using Xwt.Mac;
using MonoDevelop.Components.Mac;

namespace MonoDevelop.MacIntegration
{
	public class MacPlatformService : PlatformService
	{
		const string monoDownloadUrl = "http://www.mono-project.com/download/";

		TimerCounter timer = InstrumentationService.CreateTimerCounter ("Mac Platform Initialization", "Platform Service");
		TimerCounter mimeTimer = InstrumentationService.CreateTimerCounter ("Mac Mime Database", "Platform Service");

		static bool initedGlobal;
		bool setupFail, initedApp;

		Lazy<Dictionary<string, string>> mimemap;

		static string applicationMenuName;

		public static string ApplicationMenuName {
			get {
				return applicationMenuName ?? BrandingService.ApplicationName;
			}
			set {
				if (applicationMenuName != value) {
					applicationMenuName = value;
					OnApplicationMenuNameChanged ();
				}
			}
		}

		[DllImport("/usr/lib/libobjc.dylib")]
		private static extern IntPtr class_getInstanceMethod(IntPtr classHandle, IntPtr Selector);

		[DllImport("/usr/lib/libobjc.dylib")]
		private static extern IntPtr method_getImplementation(IntPtr method);

		[DllImport("/usr/lib/libobjc.dylib")]
		private static extern IntPtr imp_implementationWithBlock(ref BlockLiteral block);

		[DllImport("/usr/lib/libobjc.dylib")]
		private static extern void method_setImplementation(IntPtr method, IntPtr imp);

		[MonoNativeFunctionWrapper]
		delegate void AccessibilitySetValueForAttributeDelegate (IntPtr self, IntPtr selector, IntPtr valueHandle, IntPtr attributeHandle);
		delegate void SwizzledAccessibilitySetValueForAttributeDelegate (IntPtr block, IntPtr self, IntPtr valueHandle, IntPtr attributeHandle);

		static IntPtr originalAccessibilitySetValueForAttributeMethod;
		void SwizzleNSApplication ()
		{
			// Swizzle accessibilitySetValue:forAttribute: so that we can detect when VoiceOver gets enabled
			var nsApplicationClassHandle = Class.GetHandle ("NSApplication");
			var accessibilitySetValueForAttributeSelector = Selector.GetHandle ("accessibilitySetValue:forAttribute:");

			var accessibilitySetValueForAttributeMethod = class_getInstanceMethod (nsApplicationClassHandle, accessibilitySetValueForAttributeSelector);
			originalAccessibilitySetValueForAttributeMethod = method_getImplementation (accessibilitySetValueForAttributeMethod);

			var block = new BlockLiteral ();

			SwizzledAccessibilitySetValueForAttributeDelegate d = accessibilitySetValueForAttribute;
			block.SetupBlock (d, null);
			var imp = imp_implementationWithBlock (ref block);
			method_setImplementation (accessibilitySetValueForAttributeMethod, imp);
		}

		[MonoPInvokeCallback (typeof (SwizzledAccessibilitySetValueForAttributeDelegate))]
		static void accessibilitySetValueForAttribute (IntPtr block, IntPtr self, IntPtr valueHandle, IntPtr attributeHandle)
		{
			var d = Marshal.GetDelegateForFunctionPointer<AccessibilitySetValueForAttributeDelegate> (originalAccessibilitySetValueForAttributeMethod);
			d (self, Selector.GetHandle ("accessibilitySetValue:forAttribute:"), valueHandle, attributeHandle);

			NSString attrString = (NSString)ObjCRuntime.Runtime.GetNSObject (attributeHandle);
			var val = (NSNumber)ObjCRuntime.Runtime.GetNSObject (valueHandle);

			if (attrString == "AXEnhancedUserInterface" && !IdeTheme.AccessibilityEnabled) {
				if (val.BoolValue) {
					ShowVoiceOverNotice ();
				}
			}
			AccessibilityInUse = val.BoolValue;
		}

		public MacPlatformService ()
		{
			if (initedGlobal)
				throw new Exception ("Only one MacPlatformService instance allowed");
			initedGlobal = true;

			timer.BeginTiming ();

			var dir = Path.GetDirectoryName (typeof(MacPlatformService).Assembly.Location);

			if (ObjCRuntime.Dlfcn.dlopen (Path.Combine (dir, "libxammac.dylib"), 0) == IntPtr.Zero)
				LoggingService.LogFatalError ("Unable to load libxammac");

			mimemap = new Lazy<Dictionary<string, string>> (LoadMimeMapAsync);

			//make sure the menu app name is correct even when running Mono 2.6 preview, or not running from the .app
			Carbon.SetProcessName (BrandingService.ApplicationName);

			CheckGtkVersion (2, 24, 14);

			Xwt.Toolkit.CurrentEngine.RegisterBackend<IExtendedTitleBarWindowBackend,ExtendedTitleBarWindowBackend> ();
			Xwt.Toolkit.CurrentEngine.RegisterBackend<IExtendedTitleBarDialogBackend,ExtendedTitleBarDialogBackend> ();
		}

		static void CheckGtkVersion (uint major, uint minor, uint micro)
		{
			// to require exact version, also check
			//: || Gtk.Global.CheckVersion (major, minor, micro + 1) == null
			//
			if (Gtk.Global.CheckVersion (major, minor, micro) != null) {

				LoggingService.LogFatalError (
					"GTK+ version is incompatible with required version {0}.{1}.{2}.",
					major, minor, micro
				);

				var downloadButton = new AlertButton ("Download Mono Framework", null);
				if (downloadButton == MessageService.GenericAlert (
					Stock.Error,
					GettextCatalog.GetString ("Some dependencies need to be updated"),
					GettextCatalog.GetString (
						"{0} requires a newer version of GTK+, which is included with the Mono Framework. Please " +
						"download and install the latest stable Mono Framework package and restart {0}.",
						BrandingService.ApplicationName
					),
					new AlertButton ("Quit", null), downloadButton))
				{
					OpenUrl (monoDownloadUrl);
				}

				Environment.Exit (1);
			}
		}

		public override Xwt.Toolkit LoadNativeToolkit ()
		{
			var path = Path.GetDirectoryName (GetType ().Assembly.Location);
			System.Reflection.Assembly.LoadFrom (Path.Combine (path, "Xwt.XamMac.dll"));
			var loaded = Xwt.Toolkit.Load (Xwt.ToolkitType.XamMac);

			loaded.RegisterBackend<Xwt.Backends.IDialogBackend, ThemedMacDialogBackend> ();
			loaded.RegisterBackend<Xwt.Backends.IWindowBackend, ThemedMacWindowBackend> ();
			loaded.RegisterBackend<Xwt.Backends.IAlertDialogBackend, ThemedMacAlertDialogBackend> ();


			// We require Xwt.Mac to initialize MonoMac before we can execute any code using MonoMac
			timer.Trace ("Installing App Event Handlers");
			GlobalSetup ();
			timer.EndTiming ();

			var appDelegate = NSApplication.SharedApplication.Delegate as Xwt.Mac.AppDelegate;
			if (appDelegate != null) {
				appDelegate.Terminating += (object o, TerminationEventArgs e) => {
					if (MonoDevelop.Ide.IdeApp.IsRunning) {
						// If GLib the mainloop is still running that means NSApplication.Terminate() was called
						// before Gtk.Application.Quit(). Cancel Cocoa termination and exit the mainloop.
						e.Reply = NSApplicationTerminateReply.Cancel;
						Gtk.Main.Quit ();
					} else {
						// The mainloop has already exited and we've already cleaned up our application state
						// so it's now safe to terminate Cocoa.
						e.Reply = NSApplicationTerminateReply.Now;
					}
				};
			}

			// Listen to the AtkCocoa notification for the presence of VoiceOver
			SwizzleNSApplication ();

			var nc = NSNotificationCenter.DefaultCenter;
			nc.AddObserver ((NSString)"AtkCocoaAccessibilityEnabled", (NSNotification) => {
				Console.WriteLine ($"VoiceOver on {IdeTheme.AccessibilityEnabled}");
				if (!IdeTheme.AccessibilityEnabled) {
					Console.WriteLine ("Showing notice");
					ShowVoiceOverNotice ();
				}
			}, NSApplication.SharedApplication);

			// Now that Cocoa has been initialized we can check whether the keyboard focus mode is turned on
			// See System Preferences - Keyboard - Shortcuts - Full Keyboard Access
			var keyboardMode = NSUserDefaults.StandardUserDefaults.IntForKey ("AppleKeyboardUIMode");
			// 0 - Text boxes and lists only
			// 2 - All controls
			// 3 - All controls + keyboard access
			if (keyboardMode != 0) {
				Gtk.Rc.ParseString ("style \"default\" { engine \"xamarin\" { focusstyle = 2 } }");
				Gtk.Rc.ParseString ("style \"radio-or-check-box\" { engine \"xamarin\" { focusstyle = 2 } } ");
			}

			return loaded;
		}

		const string EnabledKey = "com.monodevelop.AccessibilityEnabled";
		static void ShowVoiceOverNotice ()
		{
			var alert = new NSAlert ();
			alert.MessageText = GettextCatalog.GetString ("Assistive Technology Detected");
			alert.InformativeText = GettextCatalog.GetString ("{0} has detected an assistive technology (such as VoiceOver) is running. Do you want to restart {0} and enable the accessibility features?", BrandingService.ApplicationName);
			alert.AddButton (GettextCatalog.GetString ("Restart and enable"));
			alert.AddButton (GettextCatalog.GetString ("No"));

			var result = alert.RunModal ();
			switch (result) {
			case 1000:
				NSUserDefaults defaults = NSUserDefaults.StandardUserDefaults;
				defaults.SetBool (true, EnabledKey);
				defaults.Synchronize ();

				IdeApp.Restart ();
				break;

			default:
				break;
			}
		}

		protected override string OnGetMimeTypeForUri (string uri)
		{
			var ext = Path.GetExtension (uri);
			string mime;
			if (ext != null && mimemap.Value.TryGetValue (ext, out mime))
				return mime;
			return null;
		}

		public override void ShowUrl (string url)
		{
			OpenUrl (url);
		}

		internal static void OpenUrl (string url)
		{
			Gtk.Application.Invoke ((o, args) => {
				NSWorkspace.SharedWorkspace.OpenUrl (new NSUrl (url));
			});
		}

		public override void OpenFile (string filename)
		{
			Gtk.Application.Invoke ((o, args) => {
				NSWorkspace.SharedWorkspace.OpenFile (filename);
			});
		}

		public override string DefaultMonospaceFont {
			get { return "Menlo 12"; }
		}

		public override string Name {
			get { return "OSX"; }
		}

		Dictionary<string, string> LoadMimeMapAsync ()
		{
			var map = new Dictionary<string, string> ();
			// All recent Macs should have this file; if not we'll just die silently
			if (!File.Exists ("/etc/apache2/mime.types")) {
				LoggingService.LogError ("Apache mime database is missing");
				return map;
			}

			mimeTimer.BeginTiming ();
			try {
				var loader = new MimeMapLoader (map);
				loader.LoadMimeMap ("/etc/apache2/mime.types");
			} catch (Exception ex){
				LoggingService.LogError ("Could not load Apache mime database", ex);
			}
			mimeTimer.EndTiming ();
			return map;
		}

		public override bool SetGlobalMenu (CommandManager commandManager, string commandMenuAddinPath, string appMenuAddinPath)
		{
			if (setupFail)
				return false;

			try {
				InitApp (commandManager);

				NSApplication.SharedApplication.HelpMenu = null;

				var rootMenu = NSApplication.SharedApplication.MainMenu;
				if (rootMenu == null) {
					rootMenu = new NSMenu ();
					NSApplication.SharedApplication.MainMenu = rootMenu;
				} else {
					rootMenu.RemoveAllItems ();
				}

				CommandEntrySet appCes = commandManager.CreateCommandEntrySet (appMenuAddinPath);
				rootMenu.AddItem (new MDSubMenuItem (commandManager, appCes));

				CommandEntrySet ces = commandManager.CreateCommandEntrySet (commandMenuAddinPath);
				foreach (CommandEntry ce in ces) {
					rootMenu.AddItem (new MDSubMenuItem (commandManager, (CommandEntrySet)ce));
				}
			} catch (Exception ex) {
				try {
					var m = NSApplication.SharedApplication.MainMenu;
					if (m != null) {
						m.Dispose ();
					}
					NSApplication.SharedApplication.MainMenu = null;
				} catch {}
				LoggingService.LogError ("Could not install global menu", ex);
				setupFail = true;
				return false;
			}

			return true;
		}

		static void OnCommandActivating (object sender, CommandActivationEventArgs args)
		{
			if (args.Source != CommandSource.Keybinding)
				return;
			var m = NSApplication.SharedApplication.MainMenu;
			if (m != null) {
				foreach (NSMenuItem item in m.ItemArray ()) {
					var submenu = item.Submenu as MDMenu;
					if (submenu != null && submenu.FlashIfContainsCommand (args.CommandId))
						return;
				}
			}
		}

		void InitApp (CommandManager commandManager)
		{
			if (initedApp)
				return;

			commandManager.CommandActivating += OnCommandActivating;

			//mac-ify these command names
			commandManager.GetCommand (EditCommands.MonodevelopPreferences).Text = GettextCatalog.GetString ("Preferences...");
			commandManager.GetCommand (EditCommands.DefaultPolicies).Text = GettextCatalog.GetString ("Policies...");
			commandManager.GetCommand (HelpCommands.About).Text = GetAboutCommandText ();
			commandManager.GetCommand (MacIntegrationCommands.HideWindow).Text = GetHideWindowCommandText ();
			commandManager.GetCommand (ToolCommands.AddinManager).Text = GettextCatalog.GetString ("Extensions...");

			initedApp = true;

			IdeApp.Workbench.RootWindow.DeleteEvent += HandleDeleteEvent;

			if (MacSystemInformation.OsVersion >= MacSystemInformation.Lion) {
				IdeApp.Workbench.RootWindow.Realized += (sender, args) => {
					var win = GtkQuartz.GetWindow ((Gtk.Window) sender);
					win.CollectionBehavior |= NSWindowCollectionBehavior.FullScreenPrimary;
				};
			}

			PatchGtkTheme ();
			NSNotificationCenter.DefaultCenter.AddObserver (NSCell.ControlTintChangedNotification, notif => Core.Runtime.RunInMainThread (
				delegate {
					Styles.LoadStyle();
					PatchGtkTheme();
				}));


			Styles.Changed += (s, a) => {
				var colorPanel = NSColorPanel.SharedColorPanel;
				if (colorPanel.ContentView?.Superview?.Window == null)
					LoggingService.LogWarning ("Updating shared color panel appearance failed, no valid window.");
				IdeTheme.ApplyTheme (colorPanel.ContentView.Superview.Window);
				var appearance = colorPanel.ContentView.Superview.Window.Appearance;
				if (appearance == null)
					appearance = NSAppearance.GetAppearance (IdeApp.Preferences.UserInterfaceTheme == Theme.Light ? NSAppearance.NameAqua : NSAppearance.NameVibrantDark);
				// The subviews of the shared NSColorPanel do not inherit the appearance of the main panel window
				// and need to be updated recursively.
				UpdateColorPanelSubviewsAppearance (colorPanel.ContentView.Superview, appearance);
			};
			
			// FIXME: Immediate theme switching disabled, until NSAppearance issues are fixed 
			//IdeApp.Preferences.UserInterfaceTheme.Changed += (s,a) => PatchGtkTheme ();
		}

		static void UpdateColorPanelSubviewsAppearance (NSView view, NSAppearance appearance)
		{
			if (view.Class.Name == "NSPageableTableView")
					((NSTableView)view).BackgroundColor = Xwt.Mac.Util.ToNSColor (Styles.BackgroundColor);
			view.Appearance = appearance;

			foreach (var subview in view.Subviews)
				UpdateColorPanelSubviewsAppearance (subview, appearance);
		}

		static string GetAboutCommandText ()
		{
			return GettextCatalog.GetString ("About {0}", ApplicationMenuName);
		}

		static string GetHideWindowCommandText ()
		{
			return GettextCatalog.GetString ("Hide {0}", ApplicationMenuName);
		}

		static void OnApplicationMenuNameChanged ()
		{
			Command aboutCommand = IdeApp.CommandService.GetCommand (HelpCommands.About);
			if (aboutCommand != null)
				aboutCommand.Text = GetAboutCommandText ();

			Command hideCommand = IdeApp.CommandService.GetCommand (MacIntegrationCommands.HideWindow);
			if (hideCommand != null)
				hideCommand.Text = GetHideWindowCommandText ();

			Carbon.SetProcessName (ApplicationMenuName);
		}

		// VV/VK: Disable tint based color generation
		// This will dynamically generate a gtkrc for certain widgets using system control colors.
		void PatchGtkTheme ()
		{
//			string color_hex, text_hex;
//
//			if (MonoDevelop.Core.Platform.OSVersion >= MonoDevelop.Core.MacSystemInformation.Yosemite) {
//				NSControlTint tint = NSColor.CurrentControlTint;
//				NSColor text = NSColor.SelectedMenuItemText.UsingColorSpace (NSColorSpace.GenericRGBColorSpace);
//				NSColor color = tint == NSControlTint.Blue ? NSColor.SelectedMenuItem.UsingColorSpace (NSColorSpace.GenericRGBColorSpace) : NSColor.SelectedMenuItem.UsingColorSpace (NSColorSpace.DeviceWhite);
//
//				color_hex = ConvertColorToHex (color);
//				text_hex = ConvertColorToHex (text);
//			} else {
//				color_hex = "#c5d4e0";
//				text_hex = "#000";
//			}
//
//			string gtkrc = String.Format (@"
//				style ""treeview"" = ""default"" {{
//					base[SELECTED] = ""{0}""
//					base[ACTIVE] = ""{0}""
//					text[SELECTED] = ""{1}""
//					text[ACTIVE] = ""{1}""
//					engine ""xamarin"" {{
//						roundness = 0
//						gradient_shades = {{ 1.01, 1.01, 1.01, 1.01 }}
//						glazestyle = 1
//					}}
//				}}
//
//				widget_class ""*.<GtkTreeView>*"" style ""treeview""
//				",
//				color_hex,
//				text_hex
//			);
//
//			Gtk.Rc.ParseString (gtkrc);
		}

		void GlobalSetup ()
		{
			//FIXME: should we remove these when finalizing?
			try {
				ApplicationEvents.Quit += delegate (object sender, ApplicationQuitEventArgs e)
				{
					// We can only attempt to quit safely if all windows are GTK windows and not modal
					if (!IsModalDialogRunning ()) {
						e.UserCancelled = !IdeApp.Exit ().Result; // FIXME: could this block in rare cases?
						e.Handled = true;
						return;
					}

					// When a modal dialog is running, things are much harder. We can't just shut down MD behind the
					// dialog, and aborting the dialog may not be appropriate.
					//
					// There's NSTerminateLater but I'm not sure how to access it from carbon, maybe
					// we need to swizzle methods into the app's NSApplicationDelegate.
					// Also, it stops the main CFRunLoop and enters a special runloop mode, not sure how that would
					// interact with GTK+.

					// For now, just bounce
					NSApplication.SharedApplication.RequestUserAttention (NSRequestUserAttentionType.CriticalRequest);
					// and abort the quit.
					e.UserCancelled = true;
					e.Handled = true;
				};

				ApplicationEvents.Reopen += delegate (object sender, ApplicationEventArgs e) {
					if (IdeApp.Workbench != null && IdeApp.Workbench.RootWindow != null) {
						IdeApp.Workbench.RootWindow.Deiconify ();
						IdeApp.Workbench.RootWindow.Visible = true;

						IdeApp.Workbench.RootWindow.Present ();
						e.Handled = true;
					}
				};

				ApplicationEvents.OpenDocuments += delegate (object sender, ApplicationDocumentEventArgs e) {
					//OpenFiles may pump the mainloop, but can't do that from an AppleEvent, so use a brief timeout
					GLib.Timeout.Add (10, delegate {
						IdeApp.OpenFiles (e.Documents.Select (
							doc => new FileOpenInformation (doc.Key, null, doc.Value, 1, OpenDocumentOptions.DefaultInternal))
						);
						return false;
					});
					e.Handled = true;
				};

				ApplicationEvents.OpenUrls += delegate (object sender, ApplicationUrlEventArgs e) {
					GLib.Timeout.Add (10, delegate {
						// Open files via the monodevelop:// URI scheme, compatible with the
						// common TextMate scheme: http://blog.macromates.com/2007/the-textmate-url-scheme/
						IdeApp.OpenFiles (e.Urls.Select (url => {
							try {
								var uri = new Uri (url);
								if (uri.Host != "open")
									return null;

								var qs = System.Web.HttpUtility.ParseQueryString (uri.Query);
								var fileUri = new Uri (qs ["file"]);

								int line, column;
								if (!Int32.TryParse (qs ["line"], out line))
									line = 1;
								if (!Int32.TryParse (qs ["column"], out column))
									column = 1;

								return new FileOpenInformation (Uri.UnescapeDataString(fileUri.AbsolutePath), null,
									line, column, OpenDocumentOptions.DefaultInternal);
							} catch (Exception ex) {
								LoggingService.LogError ("Invalid TextMate URI: " + url, ex);
								return null;
							}
						}).Where (foi => foi != null));
						return false;
					});
				};

				//if not running inside an app bundle (at dev time), need to do some additional setup
				if (NSBundle.MainBundle.InfoDictionary ["CFBundleIdentifier"] == null) {
					SetupWithoutBundle ();
				} else {
					SetupDockIcon ();
				}
			} catch (Exception ex) {
				LoggingService.LogError ("Could not install app event handlers", ex);
				setupFail = true;
			}
		}

		[DllImport ("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
		public extern static IntPtr IntPtr_objc_msgSend_IntPtr (IntPtr receiver, IntPtr selector, IntPtr arg1);

		static void SetupDockIcon ()
		{
			NSObject initialBundleIconFileValue;

			// Don't do anything if we're inside an app bundle.
			if (NSBundle.MainBundle.InfoDictionary.TryGetValue (new NSString ("CFBundleIconFile"), out initialBundleIconFileValue)) {
				return;
			}

			// Setup without bundle.
			FilePath exePath = System.Reflection.Assembly.GetExecutingAssembly ().Location;
			string iconName = BrandingService.GetString ("ApplicationIcon");
			string iconFile = null;

			if (iconName != null) {
				iconFile = BrandingService.GetFile (iconName);
			} else {
				// assume running from build directory
				var mdSrcMain = exePath.ParentDirectory.ParentDirectory.ParentDirectory;
				iconFile = mdSrcMain.Combine ("theme-icons", "Mac", "monodevelop.icns");
			}

			if (File.Exists (iconFile)) {
				var image = new NSImage ();
				var imageFile = new NSString (iconFile);

				IntPtr p = IntPtr_objc_msgSend_IntPtr (image.Handle, Selector.GetHandle ("initByReferencingFile:"), imageFile.Handle);
				NSApplication.SharedApplication.ApplicationIconImage = ObjCRuntime.Runtime.GetNSObject<NSImage> (p);
			}
		}

		static void SetupWithoutBundle ()
		{
			// set a bundle IDE to prevent NSProgress crash
			// https://bugzilla.xamarin.com/show_bug.cgi?id=8850
			NSBundle.MainBundle.InfoDictionary ["CFBundleIdentifier"] = new NSString ("com.xamarin.monodevelop");

			SetupDockIcon ();
		}

		static FilePath GetAppBundleRoot (FilePath path)
		{
			do {
				if (path.Extension == ".app")
					return path;
			} while ((path = path.ParentDirectory).IsNotNull);
			return null;
		}

		[GLib.ConnectBefore]
		static void HandleDeleteEvent (object o, Gtk.DeleteEventArgs args)
		{
			args.RetVal = true;
			NSApplication.SharedApplication.Hide (NSApplication.SharedApplication);
		}

		public static Gdk.Pixbuf GetPixbufFromNSImageRep (NSImageRep rep, int width, int height)
		{
			var rect = new CGRect (0, 0, width, height);

			var bitmap = rep as NSBitmapImageRep;
			try {
				if (bitmap == null) {
					using (var cgi = rep.AsCGImage (ref rect, null, null)) {
						if (cgi == null)
							return null;
						bitmap = new NSBitmapImageRep (cgi);
					}
				}
				return GetPixbufFromNSBitmapImageRep (bitmap, width, height);
			} finally {
				if (bitmap != null)
					bitmap.Dispose ();
			}
		}

		public static Gdk.Pixbuf GetPixbufFromNSImage (NSImage icon, int width, int height)
		{
			var rect = new CGRect (0, 0, width, height);

			var rep = icon.BestRepresentation (rect, null, null);
			var bitmap = rep as NSBitmapImageRep;
			try {
				if (bitmap == null) {
					if (rep != null)
						rep.Dispose ();
					using (var cgi = icon.AsCGImage (ref rect, null, null)) {
						if (cgi == null)
							return null;
						bitmap = new NSBitmapImageRep (cgi);
					}
				}
				return GetPixbufFromNSBitmapImageRep (bitmap, width, height);
			} finally {
				if (bitmap != null)
					bitmap.Dispose ();
			}
		}

		static Gdk.Pixbuf GetPixbufFromNSBitmapImageRep (NSBitmapImageRep bitmap, int width, int height)
		{
			byte[] data;
			using (var tiff = bitmap.TiffRepresentation) {
				data = new byte[tiff.Length];
				System.Runtime.InteropServices.Marshal.Copy (tiff.Bytes, data, 0, data.Length);
			}

			int pw = (int)bitmap.PixelsWide, ph = (int)bitmap.PixelsHigh;
			var pixbuf = new Gdk.Pixbuf (data, pw, ph);

			// if one dimension matches, and the other is same or smaller, use as-is
			if ((pw == width && ph <= height) || (ph == height && pw <= width))
				return pixbuf;

			// otherwise scale proportionally such that the largest dimension matches the desired size
			if (pw == ph) {
				pw = width;
				ph = height;
			} else if (pw > ph) {
				ph = (int) (width * ((float) ph / pw));
				pw = width;
			} else {
				pw = (int) (height * ((float) pw / ph));
				ph = height;
			}

			var scaled = pixbuf.ScaleSimple (pw, ph, Gdk.InterpType.Bilinear);
			pixbuf.Dispose ();

			return scaled;
		}

		protected override Xwt.Drawing.Image OnGetIconForFile (string filename)
		{
			//this only works on MacOS 10.6.0 and greater
			if (MacSystemInformation.OsVersion < MacSystemInformation.SnowLeopard)
				return base.OnGetIconForFile (filename);

			NSImage icon = null;

			if (Path.IsPathRooted (filename) && File.Exists (filename)) {
				icon = NSWorkspace.SharedWorkspace.IconForFile (filename);
			} else {
				string extension = Path.GetExtension (filename);
				if (!string.IsNullOrEmpty (extension))
					icon = NSWorkspace.SharedWorkspace.IconForFileType (extension);
			}

			if (icon == null) {
				return base.OnGetIconForFile (filename);
			}

			int w, h;
			if (!Gtk.Icon.SizeLookup (Gtk.IconSize.Menu, out w, out h)) {
				w = h = 22;
			}

			var res = GetPixbufFromNSImage (icon, w, h);
			return res != null ? res.ToXwtImage () : base.OnGetIconForFile (filename);
		}

		public override ProcessAsyncOperation StartConsoleProcess (string command, string arguments, string workingDirectory,
		                                                            IDictionary<string, string> environmentVariables,
		                                                            string title, bool pauseWhenFinished)
		{
			return new MacExternalConsoleProcess (command, arguments, workingDirectory, environmentVariables,
			                                   title, pauseWhenFinished);
		}

		public override bool CanOpenTerminal {
			get {
				return true;
			}
		}

		public override void OpenTerminal (FilePath directory, IDictionary<string, string> environmentVariables, string title)
		{
			string tabId, windowId;
			MacExternalConsoleProcess.RunTerminal (
				null, null, directory, environmentVariables, title, false, out tabId, out windowId
			);
		}

		public override IEnumerable<DesktopApplication> GetApplications (string filename)
		{
			//FIXME: we should disambiguate dupliacte apps in different locations and display both
			//for now, just filter out the duplicates
			var checkUniqueName = new HashSet<string> ();
			var checkUniquePath = new HashSet<string> ();

			//FIXME: bundle path is wrong because of how MD is built into an app
			//var thisPath = NSBundle.MainBundle.BundleUrl.Path;
			//checkUniquePath.Add (thisPath);

			checkUniqueName.Add ("MonoDevelop");
			checkUniqueName.Add (BrandingService.ApplicationName);

			string def = MonoDevelop.MacInterop.CoreFoundation.GetApplicationUrl (filename,
				MonoDevelop.MacInterop.CoreFoundation.LSRolesMask.All);

			var apps = new List<DesktopApplication> ();

			foreach (var app in MonoDevelop.MacInterop.CoreFoundation.GetApplicationUrls (filename,
				MonoDevelop.MacInterop.CoreFoundation.LSRolesMask.All)) {
				if (string.IsNullOrEmpty (app) || !checkUniquePath.Add (app))
					continue;
				var name = NSFileManager.DefaultManager.DisplayName (app);
				if (checkUniqueName.Add (name))
					apps.Add (new MacDesktopApplication (app, name, def != null && def == app));
			}

			apps.Sort ((DesktopApplication a, DesktopApplication b) => {
				int r = a.IsDefault.CompareTo (b.IsDefault);
				if (r != 0)
					return -r;
				return a.DisplayName.CompareTo (b.DisplayName);
			});

			return apps;
		}

		class MacDesktopApplication : DesktopApplication
		{
			public MacDesktopApplication (string app, string name, bool isDefault) : base (app, name, isDefault)
			{
			}

			public override void Launch (params string[] files)
			{
				foreach (var file in files)
					NSWorkspace.SharedWorkspace.OpenFile (file, Id);
			}
		}

		public override Xwt.Rectangle GetUsableMonitorGeometry (int screenNumber, int monitorNumber)
		{
			var screen = Gdk.Display.Default.GetScreen (screenNumber);
			Gdk.Rectangle ygeometry = screen.GetMonitorGeometry (monitorNumber);
			Gdk.Rectangle xgeometry = screen.GetMonitorGeometry (0);
			NSScreen nss = NSScreen.Screens[monitorNumber];
			var visible = nss.VisibleFrame;
			var frame = nss.Frame;

			// Note: Frame and VisibleFrame rectangles are relative to monitor 0, but we need absolute
			// coordinates.
			visible.X += xgeometry.X;
			frame.X += xgeometry.X;

			// VisibleFrame.Y is the height of the Dock if it is at the bottom of the screen, so in order
			// to get the menu height, we just figure out the difference between the visibleFrame height
			// and the actual frame height, then subtract the Dock height.
			//
			// We need to swap the Y offset with the menu height because our callers expect the Y offset
			// to be from the top of the screen, not from the bottom of the screen.
			nfloat x, y, width, height;

			if (visible.Height <= frame.Height) {
				var dockHeight = visible.Y - frame.Y;
				var menubarHeight = (frame.Height - visible.Height) - dockHeight;

				height = frame.Height - menubarHeight - dockHeight;
				y = ygeometry.Y + menubarHeight;
			} else {
				height = frame.Height;
				y = ygeometry.Y;
			}

			// Takes care of the possibility of the Dock being positioned on the left or right edge of the screen.
			width = NMath.Min (visible.Width, frame.Width);
			x = NMath.Max (visible.X, frame.X);

			return new Xwt.Rectangle ((int) x, (int) y, (int) width, (int) height);
		}

		internal override void GrabDesktopFocus (Gtk.Window window)
		{
			window.Present ();
			NSApplication.SharedApplication.ActivateIgnoringOtherApps (true);
		}

		static Cairo.Color ConvertColor (NSColor color)
		{
			nfloat r, g, b, a;
			if (color.ColorSpaceName == NSColorSpace.DeviceWhite) {
				a = 1.0f;
				r = g = b = color.WhiteComponent;
			} else {
				color.GetRgba (out r, out g, out b, out a);
			}
			return new Cairo.Color (r, g, b, a);
		}

		static string ConvertColorToHex (NSColor color)
		{
			nfloat r, g, b, a;

			if (color.ColorSpaceName == NSColorSpace.DeviceWhite) {
				a = 1.0f;
				r = g = b = color.WhiteComponent;
			} else {
				color.GetRgba (out r, out g, out b, out a);
			}

			return String.Format ("#{0}{1}{2}",
				((int)(r * 255)).ToString ("x2"),
				((int)(g * 255)).ToString ("x2"),
				((int)(b * 255)).ToString ("x2")
			);
		}

		internal static int GetTitleBarHeight ()
		{
			var frame = new CGRect (0, 0, 100, 100);
			var rect = NSWindow.ContentRectFor (frame, NSWindowStyle.Titled);
			return (int)(frame.Height - rect.Height);
		}

		internal static int GetTitleBarHeight (NSWindow w)
		{
			int height = 0;
			if (w.StyleMask.HasFlag (NSWindowStyle.Titled))
				height += GetTitleBarHeight ();
			if (w.Toolbar != null) {
				var rect = NSWindow.ContentRectFor (w.Frame, w.StyleMask);
				height += (int)(rect.Height - w.ContentView.Frame.Height);
			}
			return height;
		}


		internal static NSImage LoadImage (string resource)
		{
			using (var stream = typeof (MacPlatformService).Assembly.GetManifestResourceStream (resource))
			using (NSData data = NSData.FromStream (stream)) {
				return new NSImage (data);
			}
		}

		internal override void SetMainWindowDecorations (Gtk.Window window)
		{
			NSWindow w = GtkQuartz.GetWindow (window);
			w.IsOpaque = true;
			w.StyleMask |= NSWindowStyle.UnifiedTitleAndToolbar;
			IdeTheme.ApplyTheme (w);
		}

		internal override void RemoveWindowShadow (Gtk.Window window)
		{
			if (window == null)
				throw new ArgumentNullException ("window");
			NSWindow w = GtkQuartz.GetWindow (window);
			w.HasShadow = false;
		}

		internal override IMainToolbarView CreateMainToolbar (Gtk.Window window)
		{
			return new MonoDevelop.MacIntegration.MainToolbar.MainToolbar (window);
		}

		internal override void AttachMainToolbar (Gtk.VBox parent, IMainToolbarView toolbar)
		{
			var nativeToolbar = (MonoDevelop.MacIntegration.MainToolbar.MainToolbar)toolbar;
			NSWindow w = GtkQuartz.GetWindow (nativeToolbar.gtkWindow);
			if (MacSystemInformation.OsVersion >= MacSystemInformation.Yosemite)
				w.TitleVisibility = NSWindowTitleVisibility.Hidden;

			w.Toolbar = nativeToolbar.widget;
			nativeToolbar.Initialize ();
		}

		protected override RecentFiles CreateRecentFilesProvider ()
		{
			return new FdoRecentFiles (UserProfile.Current.LocalConfigDir.Combine ("RecentlyUsed.xml"));
		}

		public override bool GetIsFullscreen (Window window)
		{
			if (MacSystemInformation.OsVersion < MacSystemInformation.Lion) {
				return base.GetIsFullscreen (window);
			}

			NSWindow nswin = GtkQuartz.GetWindow (window);
			return (nswin.StyleMask & NSWindowStyle.FullScreenWindow) != 0;
		}

		public override void SetIsFullscreen (Window window, bool isFullscreen)
		{
			if (MacSystemInformation.OsVersion < MacSystemInformation.Lion) {
				base.SetIsFullscreen (window, isFullscreen);
				return;
			}

			NSWindow nswin = GtkQuartz.GetWindow (window);
			if (isFullscreen != ((nswin.StyleMask & NSWindowStyle.FullScreenWindow) != 0))
				nswin.ToggleFullScreen (null);
		}

		public override bool IsModalDialogRunning ()
		{
			var toplevels = GtkQuartz.GetToplevels ();

			// Check GtkWindow's Modal flag or for a visible NSPanel
			var ret = toplevels
				.Where (x => !x.Key.DebugDescription.StartsWith ("<_NSFullScreenTileDividerWindow", StringComparison.Ordinal))
				.Any (t => (t.Value != null && t.Value.Modal && t.Value.Visible));

			return ret;
		}

		internal override void AddChildWindow (Gtk.Window parent, Gtk.Window child)
		{
			NSWindow w = GtkQuartz.GetWindow (parent);
			child.Realize ();
			NSWindow overlay = GtkQuartz.GetWindow (child);
			overlay.SetExcludedFromWindowsMenu (true);
			w.AddChildWindow (overlay, NSWindowOrderingMode.Above);
		}

		internal override void PlaceWindow (Gtk.Window window, int x, int y, int width, int height)
		{
			if (window.GdkWindow == null)
				return; // Not yet realized

			NSWindow w = GtkQuartz.GetWindow (window);
			y += GetTitleBarHeight (w);
			var dr = FromDesktopRect (new Gdk.Rectangle (x, y, width, height));
			var r = w.FrameRectFor (dr);
			w.SetFrame (r, true);
			base.PlaceWindow (window, x, y, width, height);
		}

		static CGRect FromDesktopRect (Gdk.Rectangle r)
		{
			var desktopBounds = CalcDesktopBounds ();
			r.Y = desktopBounds.Height - r.Y - r.Height;
			if (desktopBounds.Y < 0)
				r.Y += desktopBounds.Y;
			return new CGRect (desktopBounds.X + r.X, r.Y, r.Width, r.Height);
		}

		static Gdk.Rectangle CalcDesktopBounds ()
		{
			var desktopBounds = new Gdk.Rectangle ();
			foreach (var s in NSScreen.Screens) {
				var r = s.Frame;
				desktopBounds = desktopBounds.Union (new Gdk.Rectangle ((int)r.X, (int)r.Y, (int)r.Width, (int)r.Height));
			}
			return desktopBounds;
		}

		public override void OpenFolder (FilePath folderPath, FilePath[] selectFiles)
		{
			if (selectFiles.Length == 0) {
				System.Diagnostics.Process.Start (folderPath);
			} else {
				NSWorkspace.SharedWorkspace.ActivateFileViewer (selectFiles.Select ((f) => NSUrl.FromFilename (f)).ToArray ());
			}
		}

		internal override void RestartIde (bool reopenWorkspace)
		{
			FilePath bundlePath = NSBundle.MainBundle.BundlePath;

			if (bundlePath.Extension != ".app") {
				base.RestartIde (reopenWorkspace);
				return;
			}

			var reopen = reopenWorkspace && IdeApp.Workspace != null && IdeApp.Workspace.Items.Count > 0;

			var proc = new Process ();

			var path = bundlePath.Combine ("Contents", "MacOS");
			//assume renames of mdtool end with "tool"
			var mdtool = Directory.EnumerateFiles (path, "*tool").Single();
			var psi = new ProcessStartInfo (mdtool) {
				CreateNoWindow = true,
				UseShellExecute = false,
				WorkingDirectory = path,
				Arguments = "--start-app-bundle",
			};

			var recentWorkspace = reopen ? DesktopService.RecentFiles.GetProjects ().FirstOrDefault ()?.FileName : string.Empty;
			if (!string.IsNullOrEmpty (recentWorkspace))
				psi.Arguments += " " + recentWorkspace;

			proc.StartInfo = psi;
			proc.Start ();
		}
	}

	public class ThemedMacWindowBackend : Xwt.Mac.WindowBackend
	{
		public override void InitializeBackend (object frontend, Xwt.Backends.ApplicationContext context)
		{
			base.InitializeBackend (frontend, context);
			IdeTheme.ApplyTheme (this);
		}
	}

	public class ThemedMacDialogBackend : Xwt.Mac.DialogBackend
	{
		public override void InitializeBackend (object frontend, Xwt.Backends.ApplicationContext context)
		{
			base.InitializeBackend (frontend, context);
			IdeTheme.ApplyTheme (this);
		}
	}

	public class ThemedMacAlertDialogBackend : Xwt.Mac.AlertDialogBackend
	{
		public override void Initialize (Xwt.Backends.ApplicationContext actx)
		{
			base.Initialize (actx);
			IdeTheme.ApplyTheme (this.Window);
		}
	}
}
