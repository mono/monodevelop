//  BrowserDisplayBinding.cs
//
//  This file was derived from a file from #Develop. 
//
//  Copyright (C) 2001-2007 Mike Krüger <mkrueger@novell.com>
// 
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
//  
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA

using System;
using System.Drawing;
using Gtk;

using MonoDevelop.Ide.Gui.Undo;
using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Ide.Codons;

namespace MonoDevelop.Ide.Gui.BrowserDisplayBinding
{
	public class BrowserDisplayBinding : IDisplayBinding, ISecondaryDisplayBinding
	{
		public string DisplayName {
			get { return "Web Browser"; }
		}
		

		public bool CanCreateContentForFile(string fileName)
		{
			return fileName.StartsWith("http") || fileName.StartsWith("ftp");
		}

		public bool CanCreateContentForMimeType (string mimetype)
		{
			/*switch (mimetype) {
				case "text/html":
					return true;
				default:
					return false;
			}*/
			return false;
		}
		
		public IViewContent CreateContentForFile(string fileName)
		{
			BrowserPane browserPane = new BrowserPane();
			return browserPane;
		}
		
		public IViewContent CreateContentForMimeType (string mimeType, System.IO.Stream content)
		{
			return null;
		}
		
		public bool CanAttachTo (IViewContent parent)
		{
			string filename = parent.ContentName;
			if (filename == null)
				return false;
			string mimetype = Gnome.Vfs.MimeType.GetMimeTypeForUri (filename);
			if (mimetype == "text/html")
				return parent.GetContent (typeof(MonoDevelop.Ide.Gui.Content.ITextBuffer)) != null;
			return false;
		}

		public ISecondaryViewContent CreateSecondaryViewContent (IViewContent parent)
		{
			return new BrowserPane (false, parent);
		}
	}
}
