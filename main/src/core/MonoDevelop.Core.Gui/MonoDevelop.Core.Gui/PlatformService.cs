//
// PlatformService.cs
//
// Author:
//   Geoff Norton  <gnorton@novell.com>
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
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
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;

using Mono.Addins;
using MonoDevelop.Core;
using MonoDevelop.Core.Gui.Codons;

namespace MonoDevelop.Core.Gui
{
	public abstract class PlatformService
	{
		Hashtable iconHash = new Hashtable ();
		
		public abstract DesktopApplication GetDefaultApplication (string mimetype);
		public abstract DesktopApplication [] GetAllApplications (string mimetype);
		public abstract string DefaultMonospaceFont { get; }
		public abstract string Name { get; }

		/// <summary>
		/// Used in the text editor. Valid values are found in MonoDevelop.SourceEditor.ControlLeftRightMode in the
		/// source editor project.
		/// </summary>
		public virtual string DefaultControlLeftRightBehavior {
			get {
				return "MonoDevelop";
			}
		}
		
		public virtual void ShowUrl (string url)
		{
			Process.Start (url);
		}

		public string GetMimeTypeForUri (string uri)
		{
			if (!String.IsNullOrEmpty (uri)) {
				FileInfo file = new FileInfo (uri);
				MimeTypeNode mt = FindMimeTypeForFile (file.Name);
				if (mt != null)
					return mt.Id;
			}
			return OnGetMimeTypeForUri (uri) ?? "text/plain";
		}
		
		public string GetMimeTypeDescription (string mimeType)
		{
			MimeTypeNode mt = FindMimeType (mimeType);
			if (mt != null && mt.Description != null)
				return mt.Description;
			else
				return OnGetMimeTypeDescription (mimeType) ?? string.Empty;
		}
		
		public bool GetMimeTypeIsText (string mimeType)
		{
			if (mimeType.StartsWith ("text") || mimeType.EndsWith ("+xml"))
				return true;
			
			MimeTypeNode mt = FindMimeType (mimeType);
			if (mt != null)
				return mt.IsText;
			else
				return OnGetMimeTypeIsText (mimeType);
		}
		
		public Gdk.Pixbuf GetPixbufForFile (string filename, Gtk.IconSize size)
		{
			Gdk.Pixbuf pic = null;
			
			string icon = GetIconForFile (filename);
			if (icon != null)
				pic = Services.Resources.GetBitmap (icon, size);
			
			if (pic == null)
				pic = OnGetPixbufForFile (filename, size);
			
			if (pic == null) {
				string mt = GetMimeTypeForUri (filename);
				if (mt != null)
					pic = GetPixbufForType (mt, size);
			}
			return pic ?? GetDefaultIcon (size);
		}
		
		public Gdk.Pixbuf GetPixbufForType (string type, Gtk.IconSize size)
		{
			Gdk.Pixbuf bf = (Gdk.Pixbuf) iconHash [type+size];
			if (bf != null)
				return bf;
			
			// Try getting an icon name for the type
			string icon = GetIconForType (type);
			if (icon != null)
				bf = Services.Resources.GetBitmap (icon, size);
			
			if (bf == null) {
				// Try getting a pixbuff
				bf = OnGetPixbufForType (type, size);
			}
			
			if (bf == null) {
				bf = Services.Resources.GetBitmap (type, size);
				if (bf == null)
					bf = GetDefaultIcon (size);
			}
			iconHash [type+size] = bf;
			return bf;
		}
		
		Gdk.Pixbuf GetDefaultIcon (Gtk.IconSize size)
		{
			string id = "__default" + size;
			Gdk.Pixbuf bf = (Gdk.Pixbuf) iconHash [id];
			if (bf != null)
				return bf;

			string icon = DefaultFileIcon;
			if (icon != null)
				bf = Services.Resources.GetBitmap (icon, size);
			if (bf == null)
				bf = OnGetDefaultFileIcon (size);
			if (bf == null)
				bf = Services.Resources.GetBitmap ("md-regular-file", size);
			iconHash [id] = bf;
			return bf;
		}
		
		string GetIconForFile (string fileName)
		{
			MimeTypeNode mt = FindMimeTypeForFile (fileName);
			if (mt != null)
				return mt.Icon;
			else
				return OnGetIconForFile (fileName);
		}
		
		string GetIconForType (string type)
		{
			MimeTypeNode mt = FindMimeType (type);
			if (mt != null)
				return mt.Icon;
			else
				return OnGetIconForType (type);
		}
		
		MimeTypeNode FindMimeTypeForFile (string fileName)
		{
			foreach (MimeTypeNode mt in AddinManager.GetExtensionNodes ("/MonoDevelop/Core/MimeTypes")) {
				if (mt.SupportsFile (fileName))
					return mt;
			}
			return null;
		}
		
		MimeTypeNode FindMimeType (string type)
		{
			foreach (MimeTypeNode mt in AddinManager.GetExtensionNodes ("/MonoDevelop/Core/MimeTypes")) {
				if (mt.Id == type)
					return mt;
			}
			return null;
		}
		
		protected virtual string OnGetMimeTypeForUri (string uri)
		{
			return null;
		}

		protected virtual string OnGetMimeTypeDescription (string mimeType)
		{
			return null;
		}
		
		protected virtual bool OnGetMimeTypeIsText (string mimeType)
		{
			return false;
		}
		
		protected virtual string OnGetIconForFile (string filename)
		{
			return null;
		}
		
		protected virtual string OnGetIconForType (string type)
		{
			return null;
		}
		
		protected virtual Gdk.Pixbuf OnGetPixbufForFile (string filename, Gtk.IconSize size)
		{
			return null;
		}
		
		protected virtual Gdk.Pixbuf OnGetPixbufForType (string type, Gtk.IconSize size)
		{
			return null;
		}
		
		protected virtual string DefaultFileIcon {
			get { return null; }
		}
		
		protected virtual Gdk.Pixbuf OnGetDefaultFileIcon (Gtk.IconSize size)
		{
			return null;
		}
	}
}
