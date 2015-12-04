// 
// ProjectFileEntry.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2009 Novell, Inc. (http://www.novell.com)
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
using Gtk;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Projects;
using MonoDevelop.Core;

namespace MonoDevelop.Ide.Gui.Components
{
	[System.ComponentModel.ToolboxItem (true)]
	public class ProjectFileEntry : Gtk.HBox
	{
		Entry entry;
		Button button;
		
		public ProjectFileEntry ()
		{
			entry = new Entry ();
			PackStart (entry, true, true, 0);
			
			button = new Button () { Label = "..." };
			button.Clicked += ButtonClicked;
			PackStart (button, false, false, 2);
			entry.Changed += delegate {
				OnChanged (EventArgs.Empty);
			};
			ShowAll ();
		}
		
		void CheckProject ()
		{
			if (Project == null)
				throw new InvalidOperationException ("The ProjectFileEntry has not been initialised with a project");
		}
		
		public Project Project { get; set; }
		public string DialogTitle { get; set; }
		public string DefaultFilter { get; set; }
		
		/// <summary>Makes SelectedFile return Null if the file does not exist in the project.</summary>
		public bool VerifyFileExistsInProject { get; set; }
		
		public bool EntryIsEditable {
			get { return entry.IsEditable; }
			set { entry.IsEditable = value; }
		}
		
		public FilePath SelectedFile {
			get {
				CheckProject ();
				FilePath value = entry.Text;
				if (value.IsNullOrEmpty)
					return FilePath.Null;
				if (!VerifyFileExistsInProject)
					return value.ToAbsolute (Project.BaseDirectory);
				var pf = Project.Files.GetFileWithVirtualPath (value);
				if (pf != null)
					return pf.FilePath;
				return FilePath.Null;
			}
			set {
				CheckProject ();
				if (!value.IsNullOrEmpty)
					entry.Text = RealEmpty (value.ToRelative (Project.BaseDirectory));
				else
					entry.Text = "";
			}
		}
		
		static FilePath RealEmpty (FilePath filePath)
		{
			if (filePath.ToString () == ".")
				return FilePath.Empty;
			return filePath;
		}
		
		void ButtonClicked (object sender, EventArgs e)
		{
			CheckProject ();
			
			var dialog = new ProjectFileSelectorDialog (Project, null, DefaultFilter);
			try {
				if (DialogTitle != null)
					dialog.Title = DialogTitle;
				int response = MessageService.RunCustomDialog (dialog);
				if (response == (int)Gtk.ResponseType.Ok && dialog.SelectedFile != null) {
					entry.Text = dialog.SelectedFile.ProjectVirtualPath;
				}
			} finally {
				dialog.Destroy ();
				dialog.Dispose ();
			}
		}
		
		protected virtual void OnChanged (System.EventArgs e)
		{
			EventHandler handler = this.Changed;
			if (handler != null)
				handler (this, e);
		}
		
		public event EventHandler Changed;
	}
}
