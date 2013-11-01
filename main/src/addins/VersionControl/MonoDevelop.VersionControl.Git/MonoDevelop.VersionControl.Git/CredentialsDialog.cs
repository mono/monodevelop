// 
// CredentialsDialog.cs
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
using System.Linq;
using System.Collections.Generic;
using Gtk;
using NGit.Transport;

namespace MonoDevelop.VersionControl.Git
{
	partial class CredentialsDialog : Gtk.Dialog
	{
		readonly CredentialItem.YesNoType singleYesNoCred;
		
		public CredentialsDialog (URIish uri, IEnumerable<CredentialItem> credentials)
		{
			this.Build ();
			
			labelTop.Text = string.Format (labelTop.Text, uri.ToString ());
			
			Gtk.Table table = new Gtk.Table (0, 0, false);
			table.ColumnSpacing = 6;
			vbox.PackStart (table, true, true, 0);
			
			uint r = 0;
			Widget firstEditor = null;
			foreach (CredentialItem c in credentials) {
				Label lab = new Label (c.GetPromptText () + ":");
				lab.Xalign = 0;
				table.Attach (lab, 0, 1, r, r + 1);
				Table.TableChild tc = (Table.TableChild) table [lab];
				tc.XOptions = AttachOptions.Shrink;
				
				Widget editor = null;
				
				if (c is CredentialItem.YesNoType) {
					CredentialItem.YesNoType cred = (CredentialItem.YesNoType) c;
					if (credentials.Count (i => i is CredentialItem.YesNoType) == 1) {
						singleYesNoCred = cred;
						buttonOk.Hide ();
						buttonYes.Show ();
						buttonNo.Show ();
						// Remove the last colon
						lab.Text = lab.Text.Substring (0, lab.Text.Length - 1);
					}
					else {
						CheckButton btn = new CheckButton ();
						editor = btn;
						btn.Toggled += delegate {
							cred.SetValue (btn.Active);
						};
					}
				}
				else if (c is CredentialItem.StringType || c is CredentialItem.CharArrayType) {
					CredentialItem cred = c;
					Entry e = new Entry ();
					editor = e;
					e.ActivatesDefault = true;
					if (cred.IsValueSecure ())
						e.Visibility = false;
					e.Changed += delegate {
						if (cred is CredentialItem.StringType)
							((CredentialItem.StringType)cred).SetValue (e.Text);
						else
							((CredentialItem.CharArrayType)cred).SetValue (e.Text.ToCharArray ());
					};
					
					if (c is CredentialItem.Username)
						e.Text = uri.GetUser () ?? "";
				}
				if (editor != null) {
					table.Attach (editor, 1, 2, r, r + 1);
					tc = (Table.TableChild) table [lab];
					tc.XOptions = AttachOptions.Fill;
					if (firstEditor == null)
						firstEditor = editor;
				}
				
				r++;
			}
			table.ShowAll ();
			Focus = firstEditor;
			Default = buttonOk;
		}
		
		protected virtual void OnButtonYesClicked (object sender, System.EventArgs e)
		{
			singleYesNoCred.SetValue (true);
			Respond (ResponseType.Ok);
		}
		
		protected virtual void OnButtonNoClicked (object sender, System.EventArgs e)
		{
			singleYesNoCred.SetValue (false);
			Respond (ResponseType.Ok);
		}
	}
}

