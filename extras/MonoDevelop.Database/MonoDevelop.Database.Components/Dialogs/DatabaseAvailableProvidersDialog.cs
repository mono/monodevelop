// 
// DatabaseAvailableProvidersDialog.cs
//  
// Author:
//       Luciano N. Callero <lnc19@hotmail.com>
// 
// Copyright (c) 2010 Lucian0
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
using MonoDevelop.Database.Sql;
using System;

namespace MonoDevelop.Database.Components
{
	public partial class DatabaseAvailableProvidersDialog : Gtk.Dialog
	{
		ListStore providerStore;
		
		public IDbFactory SelectedProvider {
			get {
				TreeIter iter;
				if (comboProviders.GetActiveIter (out iter))
					return (IDbFactory)providerStore.GetValue (iter, 1);
				else
					return null;
			}
		}
		
		public DatabaseAvailableProvidersDialog ()
		{
			this.Build ();
			
			providerStore = new ListStore(typeof(string), typeof(IDbFactory));

			foreach (IDbFactory fac in DbFactoryService.DbFactories)
				providerStore.AppendValues (fac.Name, fac);

			CellRendererText textRenderer = new CellRendererText ();
			comboProviders.PackStart (textRenderer, true);
			comboProviders.AddAttribute (textRenderer, "text", 0);
			comboProviders.Model = providerStore;
		}
	}
}

