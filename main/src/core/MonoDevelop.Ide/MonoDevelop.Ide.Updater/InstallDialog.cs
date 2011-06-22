// 
// InstallDialog.cs
//  
// Author:
//       Lluis Sanchez <lluis@xamarin.com>
// 
// Copyright (c) 2011 Xamarin Inc.
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

namespace MonoDevelop.Ide.Updater
{
	public partial class InstallDialog : Gtk.Dialog
	{
		List<Update> toInstall = new List<Update> ();
		
		public InstallDialog (IEnumerable<Update> updates)
		{
			this.Build ();
			toInstall.AddRange (updates);
			
			var coreUpdates = updates.Where (u => !u.IsThirdParty);
			var extraUpdates = updates.Where (u => u.IsThirdParty);
			
			if (coreUpdates.Any ())
				AddUpdates (tableUpdates, coreUpdates);
			else
				boxUpdates.Hide ();

			if (extraUpdates.Any ())
				AddUpdates (tableUpdatesExtra, extraUpdates);
			else
				boxUpdatesExtra.Hide ();
		}
		
		void AddUpdates (Gtk.Table table, IEnumerable<Update> updates)
		{
			uint r = 0;
			foreach (var up in updates) {
				var cb = new Gtk.CheckButton (up.Name);
				var cup = up;
				cb.Active = true;
				cb.Toggled += delegate {
					if (cb.Active)
						toInstall.Add (cup);
					else
						toInstall.Remove (cup);
				};
				table.Attach (cb, 0, 1, r, r + 1);
			}
			table.ShowAll ();
		}
		
		public IEnumerable<Update> UpdatesToInstall {
			get { return toInstall; }
		}
	}
}

