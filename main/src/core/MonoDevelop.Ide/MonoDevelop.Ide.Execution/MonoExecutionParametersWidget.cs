// 
// MonoExecutionParametersWidget.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Ide.Execution;
using MonoDevelop.Components.PropertyGrid;
using MonoDevelop.Core;
using MonoDevelop.Projects;

namespace MonoDevelop.Ide.Execution
{
	[System.ComponentModel.ToolboxItem(true)]
	partial class MonoExecutionParametersWidget : Gtk.Bin
	{
		MonoExecutionParameters config;
		public MonoExecutionParametersWidget ()
		{
			this.Build ();
		}

		public void Load (MonoExecutionParameters config)
		{
			this.config = config;
			propertyGrid.CurrentObject = config;
		}
		
		public object Save ()
		{
			return config;
		}

		protected virtual void OnButtonPreviewClicked (object sender, System.EventArgs e)
		{
			propertyGrid.CommitPendingChanges ();
			using (var dlg = new MonoExecutionParametersPreview (config))
				MessageService.ShowCustomDialog (dlg, this.Toplevel as Gtk.Window);
		}

		protected virtual void OnButtonResetClicked (object sender, System.EventArgs e)
		{
			propertyGrid.CommitPendingChanges ();
			if (!MessageService.Confirm (GettextCatalog.GetString ("Are you sure you want to clear all specified options?"), AlertButton.Clear))
				return;
			config.ResetProperties ();
			propertyGrid.Refresh ();
		}
	}
}
