// 
// FilePathEditorCell.cs
//  
// Author:
//       Michael Hutchinson <m.j.hutchinson@gmail.com>
// 
// Copyright (c) 2011 Michael Hutchinson
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
using MonoDevelop.Core;

namespace MonoDevelop.Components.PropertyGrid.PropertyEditors
{
	[PropertyEditorType (typeof (MonoDevelop.Core.FilePath))]
	public class FilePathEditor : PropertyEditorCell
	{
		public override bool DialogueEdit {
			get { return true; }
		}
		
		public override void LaunchDialogue ()
		{
			var kindAtt = this.Property.Attributes.OfType<FilePathIsFolderAttribute> ().FirstOrDefault ();
			FileChooserAction action;
			string title;
			if (kindAtt == null) {
				action = FileChooserAction.Open;
				title = GettextCatalog.GetString ("Select File...");
			} else {
				action = FileChooserAction.SelectFolder;
				title = GettextCatalog.GetString ("Select Folder...");
			}
			var fs = new MonoDevelop.Components.SelectFileDialog (title, action);
			if (fs.Run ())
				Property.SetValue (Instance, fs.SelectedFile);
		}
	}
	
	public class FilePathIsFolderAttribute : Attribute
	{
	}
}