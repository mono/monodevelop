// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Drawing;
using Gtk;

using MonoDevelop.Ide.Gui.Undo;
using MonoDevelop.Core.Properties;
using MonoDevelop.Core.Gui;
using MonoDevelop.Ide.Codons;

namespace MonoDevelop.Ide.Gui.BrowserDisplayBinding
{
	public class BrowserDisplayBinding : IDisplayBinding, ISecondaryDisplayBinding
	{

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
			browserPane.Load (fileName);
			return browserPane;
		}
		
		public IViewContent CreateContentForMimeType (string mimeType, string content)
		{
			return null;
		}
		
		public bool CanAttachTo (IViewContent parent)
		{
			string filename = parent.ContentName;
			string mimetype = Gnome.Vfs.MimeType.GetMimeTypeForUri (filename);
			if (mimetype == "text/html")
				return true;
			return false;
		}

		public ISecondaryViewContent CreateSecondaryViewContent (IViewContent parent)
		{
			return new BrowserPane (false, parent);
		}
	}
}
