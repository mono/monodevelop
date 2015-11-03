// 
// ProgressBarMonitor.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2011 Novell, Inc (http://www.novell.com)
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
using System.Linq;
using MonoDevelop.Core;

namespace MonoDevelop.Ide.ProgressMonitoring
{
	[System.ComponentModel.ToolboxItem(true)]
	public partial class ProgressBarMonitor : Gtk.Bin
	{
		public ProgressBarMonitor ()
		{
			this.Build ();
		}
		
		public bool AllowCancel {
			get { return buttonCancel.Visible; }
			set { buttonCancel.Visible = value; }
		}
		
		public bool ShowErrorsDialog { get; set; }
		
		public IProgressMonitor CreateProgressMonitor ()
		{
			return new InternalProgressBarMonitor (this);
		}
		
		class InternalProgressBarMonitor: BaseProgressMonitor
		{
			ProgressBarMonitor widget;
			
			public InternalProgressBarMonitor (ProgressBarMonitor widget)
			{
				this.widget = widget;
				widget.buttonCancel.Clicked += OnCancelClicked;
			}

			void OnCancelClicked (object sender, EventArgs e)
			{
				AsyncOperation.Cancel ();
			}
			
			protected override void OnProgressChanged ()
			{
				widget.progressBar.Fraction = GlobalWork;
				RunPendingEvents ();
			}
			
			protected override void OnCompleted ()
			{
				base.OnCompleted ();
				if (!AsyncOperation.Success && widget.ShowErrorsDialog) {
					string err = string.Join ("\n", Errors.Cast<string> ().ToArray ());
					MessageService.ShowError (GettextCatalog.GetString ("Add-in installation failed"), err);
				}
			}
			
			public override void Dispose ()
			{
				widget.buttonCancel.Clicked -= OnCancelClicked;
				base.Dispose ();
			}
		}
	}
}

