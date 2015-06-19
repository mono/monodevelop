// FileReplaceDialog.cs
// 
// Author:
//   Michael Hutchinson <mhutchinson@novell.com>
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

using System;
using Gtk;

using MonoDevelop.Core;

namespace MonoDevelop.Deployment
{
	
	
	public partial class FileReplaceDialog : Gtk.Dialog
	{
		
		public FileReplaceDialog (ReplaceResponse preSelect, string sourceName, string sourceModified, string targetName, string targetModified)
		{
			this.Build();
			
			//WORKAROUND: Stetic doesn't yet support setting mnemonics in markup so had to handle it here
			replaceLabel.MarkupWithMnemonic = GettextCatalog.GetString ("<b>_Replace with source file</b>");
			keepLabel.MarkupWithMnemonic = GettextCatalog.GetString ("<b>_Keep existing target file</b>");
			newestLabel.MarkupWithMnemonic = GettextCatalog.GetString ("<b>Use the _newest file</b>");
			
			this.sourceName.Text = sourceName;
			this.sourceModified.Text = sourceModified;
			this.targetName.Text = targetName;
			this.targetModified.Text = targetModified;
			
			SetState (preSelect);
			okButton.GrabFocus ();
			
			this.Resize (1, 1); // make window as small as widgets allow
		}
		
		void SetState (ReplaceResponse response)
		{
			switch (response) {
			case ReplaceResponse.Replace:
			case ReplaceResponse.ReplaceAll:
				radioNewest.Active = true;
				break;
			case ReplaceResponse.Skip:
			case ReplaceResponse.SkipAll:
				radioKeep.Active = true;
				break;
			case ReplaceResponse.ReplaceOlder:
			case ReplaceResponse.ReplaceOlderAll:
				radioNewest.Active = true;
				break;
			}
			
			if (response == ReplaceResponse.ReplaceOlderAll || response == ReplaceResponse.SkipAll)
				applyAll.Active = true;
		}

		protected virtual void OkClicked (object sender, System.EventArgs e)
		{
			ReplaceResponse response = ReplaceResponse.Abort;
			bool all = applyAll.Active;
			if (radioKeep.Active && all)
				response = ReplaceResponse.SkipAll;
			else if (radioKeep.Active)
				response = ReplaceResponse.Skip;
			else if (radioReplace.Active && all)
				response = ReplaceResponse.ReplaceAll;
			else if (radioReplace.Active)
				response = ReplaceResponse.Replace;	
			else if (radioNewest.Active && all)
				response = ReplaceResponse.ReplaceOlderAll;
			else if (radioNewest.Active)
				response = ReplaceResponse.ReplaceOlder;
			Respond ((int) response);
		}

		void CancelClicked (object sender, System.EventArgs e)
		{
			Respond ((int) ReplaceResponse.Abort);
		}
		
		[GLib.ConnectBefore]
		protected virtual void DeleteActivated (object o, DeleteEventArgs args)
		{
			Respond ((int) ReplaceResponse.Abort);
		}
		
		public enum ReplaceResponse {
			Abort = 0,
			Skip,
			SkipAll,
			Replace,
			ReplaceAll,
			ReplaceOlder,
			ReplaceOlderAll
		}
	}
}
