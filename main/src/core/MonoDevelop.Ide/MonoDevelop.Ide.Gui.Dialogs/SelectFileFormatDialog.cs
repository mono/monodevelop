// 
// SelectFileFormatDialog.cs
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
using System.Collections.Generic;
using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Projects.MSBuild;

namespace MonoDevelop.Ide.Gui.Dialogs
{
	partial class SelectFileFormatDialog : Gtk.Dialog
	{
		List<MSBuildFileFormat> formats = new List<MSBuildFileFormat> ();
		
		public SelectFileFormatDialog (IMSBuildFileObject item)
		{
			this.Build ();
			string warning = "";
			foreach (string msg in item.FileFormat.GetCompatibilityWarnings (item))
				warning += msg + "\n";
			if (warning.Length > 0)
				warning = warning.Substring (0, warning.Length - 1);
			
			labelWarnings.Text = warning;
			labelMessage.Text = string.Format (labelMessage.Text, item.Name);
			labelCurrentFormat.Text = item.FileFormat.ProductDescription;
			
			foreach (MSBuildFileFormat format in MSBuildFileFormat.GetSupportedFormats (item)) {
				comboNewFormat.AppendText (format.ProductDescription);
				formats.Add (format);
			}
			comboNewFormat.Active = 0;
		}
		
		public MSBuildFileFormat Format {
			get {
				if (comboNewFormat.Active == -1)
					return null;
				return formats [comboNewFormat.Active]; 
			}
		}
	}
}
