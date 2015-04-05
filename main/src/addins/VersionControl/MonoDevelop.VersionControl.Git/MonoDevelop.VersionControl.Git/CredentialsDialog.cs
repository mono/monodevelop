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
using Gtk;
using System;
using LibGit2Sharp;

namespace MonoDevelop.VersionControl.Git
{
	partial class CredentialsDialog : Dialog
	{
		uint r;
		public CredentialsDialog (string uri, SupportedCredentialTypes type, Credentials cred)
		{
			this.Build ();

			labelTop.Text = string.Format (labelTop.Text, uri);

			var table = new Table (0, 0, false);
			table.ColumnSpacing = 6;
			vbox.PackStart (table, true, true, 0);

			Widget firstEditor = null;
			switch (type) {
			case SupportedCredentialTypes.UsernamePassword:
				upcred = (UsernamePasswordCredentials)cred;
				firstEditor = CreateEntry (table, "Username:", false);
				CreateEntry (table, "Password:", true);
				break;
			case SupportedCredentialTypes.Ssh:
				sshcred = (SshUserKeyCredentials)cred;
				firstEditor = CreateEntry (table, "Passphrase:", true);
				break;
			}
			table.ShowAll ();
			Focus = firstEditor;
			Default = buttonOk;
		}

		Widget CreateEntry (Table table, string text, bool password)
		{
			var lab = new Label (text);
			lab.Xalign = 0;
			table.Attach (lab, 0, 1, r, r + 1);
			var tc = (Table.TableChild)table [lab];
			tc.XOptions = AttachOptions.Shrink;

			var e = new Entry ();
			Widget editor = e;
			e.ActivatesDefault = true;
			if (password)
				e.Visibility = false;

			e.Changed += delegate {
				if (password) {
					if (upcred != null)
						upcred.Password = e.Text ?? "";
					else
						sshcred.Passphrase = e.Text ?? "";
				} else {
					if (upcred != null)
						upcred.Username = e.Text;
				}
			};

			if (editor != null) {
				table.Attach (editor, 1, 2, r, r + 1);
				tc = (Table.TableChild)table [lab];
				tc.XOptions = AttachOptions.Fill;
			}
			r++;
			return editor;
		}

		readonly UsernamePasswordCredentials upcred;
		readonly SshUserKeyCredentials sshcred;
	}
}
