// 
// NewFormattingProfileDialog.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
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
using System;
using System.Collections.Generic;
using System.Linq;

namespace MonoDevelop.CSharp.Formatting
{
	partial class NewFormattingProfileDialog  : Gtk.Dialog
	{
		public string NewProfileName {
			get;
			private set;
		}
		
		
		public CSharpFormattingPolicy InitializeFrom {
			get {
				return policies[comboboxInitFrom.Active];
			}
		}
		
		List<CSharpFormattingPolicy> policies;
		public NewFormattingProfileDialog (List<CSharpFormattingPolicy> policies)
		{
			this.Build ();
			this.policies = policies;
			this.entryProfileName.Changed += delegate {
				NewProfileName = entryProfileName.Text;
				buttonOk.Sensitive = !string.IsNullOrEmpty (NewProfileName) && !this.policies.Any (p => p.Name == NewProfileName);
			};
			
			Gtk.ListStore model = new Gtk.ListStore (typeof(string));
			foreach (var p in policies) {
				model.AppendValues (p.Name);
			}
			comboboxInitFrom.Model = model;
			comboboxInitFrom.Active = 0;
		}
	}
}

