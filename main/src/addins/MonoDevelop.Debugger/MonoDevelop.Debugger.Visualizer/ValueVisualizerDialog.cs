// 
// ValueViewerDialog.cs
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
using System;
using System.Collections.Generic;
using Mono.Debugging.Client;
namespace MonoDevelop.Debugger.Viewers
{
	public partial class ValueVisualizerDialog : Gtk.Dialog
	{
		List<IValueVisualizer> visualizers;
		Gtk.Widget currentWidget;
		IValueVisualizer currentVisualizer;
		ObjectValue value;
		
		public ValueVisualizerDialog ()
		{
			this.Build ();
		}
		
		public void Show (ObjectValue val)
		{
			value = val;
			visualizers = new List<IValueVisualizer> (DebuggingService.GetValueVisualizers (val));
			visualizers.Sort (delegate (IValueVisualizer v1, IValueVisualizer v2) {
				return v1.Name.CompareTo (v2.Name);
			});
			foreach (IValueVisualizer vis in visualizers)
				comboVisualizers.AppendText (vis.Name);
			comboVisualizers.Active = 0;
		}
		
		protected virtual void OnComboVisualizersChanged (object sender, System.EventArgs e)
		{
			if (currentWidget != null)
				mainBox.Remove (currentWidget);
			if (comboVisualizers.Active == -1) {
				buttonOk.Sensitive = false;
				return;
			}
			buttonOk.Sensitive = true;
			currentVisualizer = visualizers [comboVisualizers.Active];
			currentWidget = currentVisualizer.GetVisualizerWidget (value);
			mainBox.PackStart (currentWidget, true, true, 0);
			currentWidget.Show ();
		}
		
		protected virtual void OnButtonOkClicked (object sender, System.EventArgs e)
		{
			if (currentVisualizer.StoreValue (value))
				Respond (Gtk.ResponseType.Ok);
		}
	}
}

