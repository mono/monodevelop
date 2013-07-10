// 
// AddMimeTypeDialog.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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
using System.Linq;
using System.Collections.Generic;
using MonoDevelop.Core;
using MonoDevelop.Components;
using System.IO;

namespace MonoDevelop.Ide.Projects
{
	internal partial class AddMimeTypeDialog : Gtk.Dialog
	{
		string mimeType;
		IEnumerable<string> currentTypes;
		
		public AddMimeTypeDialog (IEnumerable<string> types)
		{
			this.Build ();
			currentTypes = types;
			buttonOk.Sensitive = false;
		}

		protected virtual void OnEntryChanged (object sender, System.EventArgs e)
		{
			string name = entry.Text;
			if (string.IsNullOrEmpty (name)) {
				buttonOk.Sensitive = false;
				labelDesc.Text = string.Empty;
				image.Visible = false;
				return;
			}
			
			image.Visible = true;
			
			Xwt.Drawing.Image img;
			string desc;
			
			string mt = TryGetFileType (name);
			if (mt != null && mt != "text/plain") {
				desc = DesktopService.GetMimeTypeDescription (mt);
				img = DesktopService.GetIconForType (mt, Gtk.IconSize.Menu);
				mimeType = mt;
			}
			else if (name.IndexOf ('/') != -1) {
				desc = DesktopService.GetMimeTypeDescription (name);
				img = DesktopService.GetIconForType (name, Gtk.IconSize.Menu);
				mimeType = name;
			} else {
				img = ImageService.GetIcon (Gtk.Stock.DialogError, Gtk.IconSize.Menu);
				desc = GettextCatalog.GetString ("Unknown type");
				mimeType = null;
			}
			if (mimeType != null && currentTypes.Contains (mimeType)) {
				img = ImageService.GetIcon (Gtk.Stock.DialogError, Gtk.IconSize.Menu);
				desc = GettextCatalog.GetString ("Type '{0}' already registered", desc);
				mimeType = null;
			}
			if (string.IsNullOrEmpty (desc))
				desc = mt;
			buttonOk.Sensitive = mimeType != null;
			labelDesc.Text = desc ?? string.Empty;
			image.Pixbuf = img.ToPixbuf (Gtk.IconSize.Menu);
		}
		
		string TryGetFileType (string name)
		{
			if (name.StartsWith ("."))
				name = name.Substring (1);
			
			string tmpFile = null;
			try {
				tmpFile = System.IO.Path.GetTempFileName ();
				string f = System.IO.Path.ChangeExtension (tmpFile, "." + name);
				File.Move (tmpFile, f);
				tmpFile = f;
				return DesktopService.GetMimeTypeForUri (tmpFile);
			} catch {
				return null;
			} finally {
				if (tmpFile != null) {
					try {
						if (File.Exists (tmpFile))
							File.Delete (tmpFile);
					} catch {
					}
				}
			}
		}
		
		public string MimeType {
			get {
				return mimeType;
			}
		}
	}
}
