// HelpCommands.cs
//
// Author:
//   Carlo Kok (ck@remobjects.com)
//
// Copyright (c) 2009 RemObjects Software
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
//
//


using System;
using System.Timers;

using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Components.Commands;

namespace MonoDevelop.Ide.Commands
{
	/// <summary>
	/// Copied from MonoDevelop.Ide.addin.xml
	/// </summary>
	public enum HelpCommands
	{
		Help,
		TipOfTheDay,
		OpenLogDirectory,
		About
	}

	// MonoDevelop.Ide.Commands.HelpCommands.Help
	public class HelpHandler : CommandHandler
	{
		protected override void Run ()
		{
			IdeApp.HelpOperations.ShowHelp ("root:");
		}

		protected override void Update (CommandInfo info)
		{
			if (!IdeApp.HelpOperations.CanShowHelp ("root:"))
				info.Visible = false;
		}
	}

	// MonoDevelop.Ide.Commands.HelpCommands.OpenLogDirectory
	public class OpenLogDirectoryHandler : CommandHandler
	{
		protected override void Run ()
		{
			try {
				var profile = MonoDevelop.Core.UserProfile.Current;
				if (profile != null && System.IO.Directory.Exists (profile.LogDir))
					System.Diagnostics.Process.Start (profile.LogDir);
			} catch (Exception ex) {
				MonoDevelop.Core.LoggingService.LogError ("Could not open the Log Directory", ex);
			}
		}
	}

	// MonoDevelop.Ide.Commands.HelpCommands.TipOfTheDay
	public class TipOfTheDayHandler : CommandHandler
	{
		protected override void Run ()
		{
			TipOfTheDayWindow dlg = new TipOfTheDayWindow ();
			dlg.Show ();
		}
	}

	// MonoDevelop.Ide.Commands.HelpCommands.About
	public class AboutHandler : CommandHandler
	{
		protected override void Run ()
		{
			CommonAboutDialog.ShowAboutDialog ();
		}

		protected override void Update (CommandInfo info)
		{
			base.Update (info);
			info.Icon = MonoDevelop.Core.BrandingService.HelpAboutIconId;
		}
	}

	class SendFeedbackHandler : CommandHandler
	{
		protected override void Run ()
		{
			FeedbackService.ShowFeedbackWindow ();
		}

		protected override void Update (CommandInfo info)
		{
			info.Visible = FeedbackService.Enabled;
		}
	}

	class DumpUITreeHandler : CommandHandler
	{
		void DumpGtkWidget (Gtk.Widget widget, int indent = 0)
		{
			string spacer = new string (' ', indent);
			Console.WriteLine ($"{spacer} {widget.Accessible.Name} - {widget.GetType ()}");
			if (widget.GetType () == typeof (Gtk.Label)) {
				var label = (Gtk.Label)widget;
				Console.WriteLine ($"{spacer}   {label.Text}");
			} else if (widget.GetType () == typeof (Gtk.Button)) {
				var button = (Gtk.Button)widget;
				Console.WriteLine ($"{spacer}   {button.Label}");
			}

			var container = widget as Gtk.Container;
			if (container != null) {
				var children = container.Children;
				Console.WriteLine ($"{spacer}   Number of children: {children.Length}");

				foreach (var child in children) {
					DumpGtkWidget (child, indent + 3);
				}
			}
		}

		protected override void Run ()
		{
			var windows = Gtk.Window.ListToplevels ();
			Console.WriteLine ($"---------\nNumber of windows: {windows}");
			foreach (var window in windows) {
				Console.WriteLine ($"Window: {window.Title} - {window.GetType ()}");
				DumpGtkWidget (window);
			}
		}
	}

	class DumpA11yTreeHandler : CommandHandler
	{
		protected override void Run ()
		{
#if MAC
			Components.AtkCocoaHelper.AtkCocoaMacExtensions.DumpAccessibilityTree ();
#endif
		}
	}

	class DumpA11yTreeDelayedHandler : CommandHandler
	{
#if MAC
		Timer t;
		protected override void Run ()
		{
			t = new Timer (10000);
			t.Elapsed += (sender, e) => {
				Components.AtkCocoaHelper.AtkCocoaMacExtensions.DumpAccessibilityTree ();
				t.Dispose ();
				t = null;
			};
			t.Start ();
		}
#endif
	}
}
