//
// UpdateInProgressDialog.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
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
using MonoDevelop.Core;
using MonoDevelop.Core.Gui;

namespace MonoDevelop.RegexToolkit
{
	public partial class UpdateInProgressDialog : Gtk.Dialog
	{
		Expression[] expressions;
		
		public Expression[] Expressions {
			get {
				return expressions;
			}
		}
		bool cancled = false;
		public UpdateInProgressDialog() 
		{
			this.Build();
			this.TransientFor = MonoDevelop.Ide.Gui.IdeApp.Workbench.RootWindow;
			
			this.buttonOk.Sensitive = false;
			this.buttonOk.Clicked += delegate {
				Destroy ();
			};
			this.buttonCancel.Clicked += delegate {
				cancled = true;
				this.expressions = null;
				Destroy ();
			}; 
			
			DispatchService.BackgroundDispatch (
				delegate {
				Webservices services = new Webservices ();
				this.expressions = services.ListAllAsXml (System.Int32.MaxValue);
				if (!cancled) {
					this.buttonCancel.Sensitive = false;
					this.label.Text = MonoDevelop.Core.GettextCatalog.GetString ("Update done.");
					this.buttonOk.Sensitive = true;
				}
			});
		}
	}
}
