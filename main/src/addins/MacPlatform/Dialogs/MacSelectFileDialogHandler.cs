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
using System.Text;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;

using Foundation;
using CoreGraphics;
using AppKit;

using MonoDevelop.Components;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Components.Extensions;
using MonoDevelop.MacInterop;

namespace MonoDevelop.MacIntegration
{
	class MacSelectFileDialogHandler : MacCommonFileDialogHandler<SelectFileDialogData, object>, ISelectFileDialogHandler
	{
		protected override NSSavePanel OnCreatePanel (SelectFileDialogData data)
		{
			if (data.Action == FileChooserAction.Save)
				return new NSSavePanel ();

			return new NSOpenPanel {
				CanChooseDirectories = (data.Action & FileChooserAction.FolderFlags) != 0,
				CanChooseFiles = (data.Action & FileChooserAction.FileFlags) != 0,
				CanCreateDirectories = (data.Action & FileChooserAction.CreateFolder) != 0,
				ResolvesAliases = false,
			};
		}

		public bool Run (SelectFileDialogData data)
		{
			using (var panel = CreatePanel (data, out var saveState)) {
				if (panel.RunModal () == 0) {
					GtkQuartz.FocusWindow (data.TransientFor ?? MessageService.RootWindow);
					return false;
				}

				data.SelectedFiles = GetSelectedFiles (panel);
				GtkQuartz.FocusWindow (data.TransientFor ?? MessageService.RootWindow);
				return true;
			}
		}

		internal static FilePath[] GetSelectedFiles (NSSavePanel panel)
		{
			var openPanel = panel as NSOpenPanel;
			if (openPanel != null && openPanel.AllowsMultipleSelection) {
				 return openPanel.Urls.Select (u => (FilePath) u.Path).ToArray ();
			} else {
				var url = panel.Url;
				return url != null ? new FilePath[] { panel.Url.Path } : new FilePath[0];
			}
		}

		
		static NSOpenSavePanelUrl GetFileFilter (SelectFileDialogFilter filter)
		{
			var globRegex = filter.Patterns == null || filter.Patterns.Count == 0?
				null : CreateGlobRegex (filter.Patterns);
			var mimetypes = filter.MimeTypes == null || filter.MimeTypes.Count == 0?
				null : filter.MimeTypes;
			
			return (sender, url) => {
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
					var mimetype = DesktopService.GetMimeTypeForUri (path);
					if (mimetype != null) {
						var chain = DesktopService.GetMimeTypeInheritanceChain (mimetype);
						if (mimetypes.Any (m => chain.Any (c => c == m)))
							return true;
					}
				}
				return false;
			};
		}
		
		//based on MonoDevelop.Ide.Extensions.MimeTypeNode
		static Regex CreateGlobRegex (IEnumerable<string> globs)
		{
			var globalPattern = new StringBuilder ();
			
			foreach (var glob in globs) {
				string pattern = Regex.Escape (glob);
				pattern = pattern.Replace ("\\*",".*");
				pattern = pattern.Replace ("\\?",".");
				pattern = pattern.Replace ("\\|","$|^");
				pattern = "^" + pattern + "$";
				if (globalPattern.Length > 0)
					globalPattern.Append ('|');
				globalPattern.Append (pattern);
			}

			return new Regex (globalPattern.ToString (), RegexOptions.IgnoreCase);
		}
		
		internal static NSPopUpButton CreateFileFilterPopup (SelectFileDialogData data, NSSavePanel panel)
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
			
			var popup = new NSPopUpButton (new CGRect (0, 6, 200, 18), false);
			popup.SizeToFit ();
			var rect = popup.Frame;
			popup.Frame = new CGRect (rect.X, rect.Y, 200, rect.Height);
			
			foreach (var filter in filters)
				popup.AddItem (filter.Name);
			
			var defaultIndex = data.DefaultFilter == null? 0 : Math.Max (0, filters.IndexOf (data.DefaultFilter));
			if (defaultIndex > 0) {
				popup.SelectItem (defaultIndex);
			}
			panel.ShouldEnableUrl = GetFileFilter (filters[defaultIndex]);
			
			popup.Activated += delegate {
				panel.ShouldEnableUrl = GetFileFilter (filters[(int)popup.IndexOfSelectedItem]);
				panel.Display ();
			};
			
			return popup;
		}
	}
}

