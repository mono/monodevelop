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
			
			//TODO: add a combo box so that the user can actually use different filters?
			if (data.Filters.Count > 0)
				panel.ShouldEnableUrl = GetFileFilter (data.Filters);
			
			if (!string.IsNullOrEmpty (data.CurrentFolder))
				panel.DirectoryUrl = new MonoMac.Foundation.NSUrl (data.CurrentFolder, true);
			
			var openPanel = panel as NSOpenPanel;
			if (openPanel != null) {
				openPanel.AllowsMultipleSelection = data.SelectMultiple;
			}
		}
		
		static NSOpenSavePanelUrl GetFileFilter (IList<SelectFileDialogFilter> filters)
		{
			var globRegexes = filters.Select (f => CreateGlobRegex (f.Patterns)).ToList ();
			var mimetypes = filters.Where (f => f.MimeTypes != null && f.MimeTypes.Count > 0)
				.SelectMany (f => f.MimeTypes).ToList ();
			
			return (NSSavePanel sender, NSUrl url) => {
				//never show non-file URLs
				if (!url.IsFileUrl)
					return false;
				
				string path = url.Path;
				
				//always make directories selectable, unless they're app bundles
				if (System.IO.Directory.Exists (path))
					return !path.EndsWith (".app", StringComparison.OrdinalIgnoreCase);
				
				if (globRegexes.Any (r => r.IsMatch (path)))
					return true;
				
				if (mimetypes.Count > 0) {
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
				
				var view = new NSView (new RectangleF (0, 0, 200, 28)) {
					AutoresizesSubviews = true,
					AutoresizingMask = NSViewResizingMask.WidthSizable | NSViewResizingMask.MaxXMargin,
				};
				
				var text = new NSTextField (new RectangleF (0, 6, 100, 20)) {
					StringValue = GettextCatalog.GetString ("Override build action:"),
					DrawsBackground = false,
					Bordered = false,
					Editable = false,
					Selectable = false
				};
				text.SizeToFit ();
				float textWidth = text.Frame.Width;
				
				var combo = new NSPopUpButton (new RectangleF (0, 6, 200, 18), false) {
					AutoresizingMask = NSViewResizingMask.WidthSizable | NSViewResizingMask.MaxXMargin,
				};
				combo.SizeToFit ();
				var rect = combo.Frame;
				combo.Frame = new RectangleF (textWidth + 5, 0, 200, rect.Height);
				
				rect = view.Frame;
				rect.Width = combo.Frame.Width + textWidth + 5;
				view.Frame = rect;
				
				view.AddSubview (text);
				view.AddSubview (combo);
				panel.AccessoryView = view;
				
				var defaultTitle = GettextCatalog.GetString ("(Default)");
				/*
				var attribStr = new NSMutableAttributedString (defaultTitle);
				var boldFont = NSFontManager.SharedFontManager.ConvertFont (combo.Menu.Font, NSFontTraitMask.Bold);
				attribStr.AddAttribute (NSAttributedString.FontAttributeName, boldFont, new NSRange (0, defaultTitle.Length));
				
				combo.Menu.AddItem (new NSMenuItem () { AttributedTitle = attribStr });
				*/
				combo.AddItem (defaultTitle);
				combo.Menu.AddItem (NSMenuItem.SeparatorItem);
				
				foreach (var b in data.BuildActions) {
					if (b == "--")
						combo.Menu.AddItem (NSMenuItem.SeparatorItem);
					else
						combo.AddItem (b);
				}
				
				var action = panel.RunModal ();
				if (action == 0)
					return false;
				
				data.SelectedFiles = MacSelectFileDialogHandler.GetSelectedFiles (panel);
				
				var comboIndex = combo.IndexOfSelectedItem - 2;
				if (comboIndex >= 0)
					data.OverrideAction = data.BuildActions[comboIndex];
				
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

