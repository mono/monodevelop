// 
// EditRemoteDialog.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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


namespace MonoDevelop.VersionControl.Git
{
	public partial class EditRemoteDialog : Gtk.Dialog
	{
		RemoteSource remote;
		bool updating;
		
		public EditRemoteDialog (RemoteSource remote, bool isNew)
		{
			this.Build ();
			this.remote = remote;
			
			updating = true;
			entryName.Text = remote.Name;
			entryUrl.Text = remote.FetchUrl ?? "";
			entryPushUrl.Text = string.IsNullOrEmpty (remote.PushUrl) ? remote.FetchUrl : remote.PushUrl;
			if (!isNew)
				checkImportTags.Visible = false;
			updating = false;
			UpdateButtons ();
		}
		
		public bool ImportTags {
			get { return checkImportTags.Active; }
		}
		
		void UpdateButtons ()
		{
			buttonOk.Sensitive = entryName.Text.Length > 0 && entryUrl.Text.Length > 0;
		}
		
		protected virtual void OnEntryNameChanged (object sender, System.EventArgs e)
		{
			if (updating)
				return;
			remote.Name = entryName.Text;
			UpdateButtons ();
		}
		
		protected virtual void OnEntryUrlChanged (object sender, System.EventArgs e)
		{
			if (updating)
				return;
			if (remote.FetchUrl == remote.PushUrl)
				entryPushUrl.Text = entryUrl.Text;
			remote.FetchUrl = entryUrl.Text;
			UpdateButtons ();
		}
		
		protected virtual void OnEntryPushUrlChanged (object sender, System.EventArgs e)
		{
			if (updating)
				return;
			remote.PushUrl = entryPushUrl.Text;
			UpdateButtons ();
		}
	}
}

