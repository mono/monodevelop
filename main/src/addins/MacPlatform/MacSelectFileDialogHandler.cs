// 
// MacSelectFileDialogHandler.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2010 Novell, Inc. (http://www.novell.com)
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
using MonoDevelop.Components.Extensions;
using OSXIntegration.Framework;
using MonoDevelop.Ide.Extensions;
using MonoMac.AppKit;
using MonoDevelop.Core;
using System.Collections.Generic;
using MonoMac.Foundation;
using System.Linq;
using System.Drawing;

namespace MonoDevelop.Platform.Mac
{
	class MacSelectFileDialogHandler : ISelectFileDialogHandler
	{
		public bool Run (SelectFileDialogData data)
		{
			NSSavePanel panel = null;
			
			try {
				switch (data.Action) {
				case Gtk.FileChooserAction.Save:
					panel = new NSSavePanel ();
					break;
				case Gtk.FileChooserAction.Open:
					panel = new NSOpenPanel () {
						CanChooseDirectories = false,
						CanChooseFiles = true,
					};
					break;
				case Gtk.FileChooserAction.SelectFolder:
				case Gtk.FileChooserAction.CreateFolder:
					panel = new NSOpenPanel () {
						CanChooseDirectories = true,
						CanChooseFiles = false,
						CanCreateDirectories = (data.Action == Gtk.FileChooserAction.CreateFolder),
					};
					break;
				default:
					throw new InvalidOperationException ("Unknown action " + data.Action.ToString ());
				}
				
				SetCommonPanelProperties (data, panel);
				
				var popup = CreateFileFilterPopup (data, panel);
				if (popup != null) {
					panel.AccessoryView = popup;
				}
				
				var action = panel.RunModal ();
				if (action == 0)
					return false;
				
				data.SelectedFiles = GetSelectedFiles (panel);
				
				return true;
			} finally {
				if (panel != null)
					panel.Dispose ();
			}
		}
		
		internal static FilePath[] GetSelectedFiles (NSSavePanel panel)
		{
			var openPanel = panel as NSOpenPanel;
			if (openPanel != null && openPanel.AllowsMultipleSelection) {
				 return openPanel.Urls.Select (u => (FilePath) u.Path).ToArray ();
			} else {
				 return new FilePath[] { panel.Url.Path };
			}
		}
		
		internal static void SetCommonPanelProperties (SelectFileDialogData data, NSSavePanel panel)
		{
			if (!string.IsNullOrEmpty (data.Title))
				panel.Title = data.Title;
			
			if (!string.IsNullOrEmpty (data.InitialFileName))
				panel.NameFieldStringValue = data.InitialFileName;
			
			if (!string.IsNullOrEmpty (data.CurrentFolder))
				panel.DirectoryUrl = new MonoMac.Foundation.NSUrl (data.CurrentFolder, true);
			
			var openPanel = panel as NSOpenPanel;
			if (openPanel != null) {
				openPanel.AllowsMultipleSelection = data.SelectMultiple;
			}
		}
		
		static NSOpenSavePanelUrl GetFileFilter (SelectFileDialogFilter filter)
		{
			var globRegex = filter.Patterns == null || filter.Patterns.Count == 0?
				null : CreateGlobRegex (filter.Patterns);
			var mimetypes = filter.MimeTypes == null || filter.MimeTypes.Count == 0?
				null : filter.MimeTypes;
			
			return (NSSavePanel sender, NSUrl url) => {
				//never show non-file URLs
				if (!url.IsFileUrl)
					return false;
				
				string path = url.Path;
				
				//always make directories selectable, unless they're app bundles
				if (System.IO.Directory.Exists (path))
					return !path.EndsWith (".app", StringComparison.OrdinalIgnoreCase);
				
				if (globRegex != null && globRegex.IsMatch (path))
					return true;
				
				if (mimetypes != null) {
					var mimetype = MonoDevelop.Ide.DesktopService.GetMimeTypeForUri (path);
					if (mimetype != null) {
						var chain = MonoDevelop.Ide.DesktopService.GetMimeTypeInheritanceChain (mimetype);
						if (mimetypes.Any (m => chain.Any (c => c == m)))
							return true;
					}
				}
				return false;
			};
		}
		
		//based on MonoDevelop.Ide.Extensions.MimeTypeNode
		static System.Text.RegularExpressions.Regex CreateGlobRegex (IEnumerable<string> globs)
		{
			var globalPattern = new System.Text.StringBuilder ();
			
			foreach (var glob in globs) {
				string pattern = System.Text.RegularExpressions.Regex.Escape (glob);
				pattern = pattern.Replace ("\\*",".*");
				pattern = pattern.Replace ("\\?",".");
				pattern = pattern.Replace ("\\|","$|^");
				pattern = "^" + pattern + "$";
				if (globalPattern.Length > 0)
					globalPattern.Append ('|');
				globalPattern.Append (pattern);
			}
			return new System.Text.RegularExpressions.Regex (globalPattern.ToString (),
				System.Text.RegularExpressions.RegexOptions.Compiled);
		}
		
		static NSPopUpButton CreateFileFilterPopup (SelectFileDialogData data, NSSavePanel panel)
		{
			var filters = data.Filters;
			
			//no filtering
			if (filters == null || filters.Count == 0) {
				return null;
			}
			
			//filter, but no choice
			if (filters.Count == 1) {
				panel.ShouldEnableUrl = GetFileFilter (filters[0]);
				return null;
			}
			
			var popup = new NSPopUpButton (new RectangleF (0, 6, 200, 18), false);
			popup.SizeToFit ();
			var rect = popup.Frame;
			popup.Frame = new RectangleF (rect.X, rect.Y, 200, rect.Height);
			
			foreach (var filter in filters)
				popup.AddItem (filter.Name);
			
			var defaultIndex = data.DefaultFilter == null? 0 : Math.Max (0, filters.IndexOf (data.DefaultFilter));
			if (defaultIndex > 0) {
				popup.SelectItem (defaultIndex);
			}
			panel.ShouldEnableUrl = GetFileFilter (filters[defaultIndex]);
			
			ActivationReceiver.Bind (popup).Activated += delegate {
				panel.ShouldEnableUrl = GetFileFilter (filters[popup.IndexOfSelectedItem]);
				panel.Display ();
			};
			
			return popup;
		}
		
		class ActivationReceiver : NSObject
		{
			NSControl parent;
			
			public static ActivationReceiver Bind (NSControl parent)
			{
				return new ActivationReceiver (parent);
			}
			
			ActivationReceiver (NSControl parent)
			{
				this.parent = parent;
				parent.Target = this;
				parent.Action = new MonoMac.ObjCRuntime.Selector ("activated");
			}
			
			[Export ("activated")]
			void OnActivated ()
			{
				if (Activated != null)
					Activated (parent, EventArgs.Empty);
			}
			
			public event EventHandler Activated;
		}
		
		internal static NSView CreateLabelledDropdown (string label, float popupWidth, out NSPopUpButton popup)
		{
			var view = new NSView (new RectangleF (0, 0, popupWidth, 28)) {
				AutoresizesSubviews = true,
				AutoresizingMask = NSViewResizingMask.WidthSizable | NSViewResizingMask.MaxXMargin,
			};
			
			var text = new NSTextField (new RectangleF (0, 6, 100, 20)) {
				StringValue = label,
				DrawsBackground = false,
				Bordered = false,
				Editable = false,
				Selectable = false
			};
			text.SizeToFit ();
			float textWidth = text.Frame.Width;
			float textHeight = text.Frame.Height;
			
			popup = new NSPopUpButton (new RectangleF (0, 6, popupWidth, 18), false) {
				AutoresizingMask = NSViewResizingMask.WidthSizable | NSViewResizingMask.MaxXMargin,
			};
			popup.SizeToFit ();
			var rect = popup.Frame;
			float popupHeight = rect.Height;
			popup.Frame = new RectangleF (textWidth + 5, 0, popupWidth, rect.Height);
			
			rect = view.Frame;
			rect.Width = popup.Frame.Width + textWidth + 5;
			rect.Height = Math.Max (popupHeight, textHeight);
			view.Frame = rect;
			
			view.AddSubview (text);
			view.AddSubview (popup);
			
			return view;
		}
	}
	
	class MacAddFileDialogHandler : IAddFileDialogHandler
	{
		public bool Run (AddFileDialogData data)
		{
			using (var panel = new NSOpenPanel () {
				CanChooseDirectories = false,
				CanChooseFiles = true,
			}) {
				MacSelectFileDialogHandler.SetCommonPanelProperties (data, panel);
				
				NSPopUpButton popup;
				panel.AccessoryView = MacSelectFileDialogHandler.CreateLabelledDropdown (
					GettextCatalog.GetString ("Override build action:"), 200, out popup);
				
				popup.AddItem (GettextCatalog.GetString ("(Default)"));
				popup.Menu.AddItem (NSMenuItem.SeparatorItem);
				
				foreach (var b in data.BuildActions) {
					if (b == "--")
						popup.Menu.AddItem (NSMenuItem.SeparatorItem);
					else
						popup.AddItem (b);
				}
				
				var action = panel.RunModal ();
				if (action == 0)
					return false;
				
				data.SelectedFiles = MacSelectFileDialogHandler.GetSelectedFiles (panel);
				
				var idx = popup.IndexOfSelectedItem - 2;
				if (idx >= 0)
					data.OverrideAction = data.BuildActions[idx];
				
				return true;
			}
		}
	}
	
	class MacOpenFileDialogHandler : IOpenFileDialogHandler
	{
		public bool Run (OpenFileDialogData data)
		{
			throw new NotImplementedException ();
		}
	}
}

