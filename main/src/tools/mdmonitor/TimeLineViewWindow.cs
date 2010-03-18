// 
// TimeLimeViewWindow.cs
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
using MonoDevelop.Core.Instrumentation;

namespace Mono.Instrumentation.Monitor
{
	partial class TimeLineViewWindow : Gtk.Window
	{
		Counter mainCounter;
		CounterValue mainValue;
		
		public TimeLineViewWindow (Counter c, CounterValue value) : base(Gtk.WindowType.Toplevel)
		{
			this.Build ();
			this.mainCounter = c;
			this.mainValue = value;
			timeView.Scale = 300;
			
			Update ();
		}
		
		void Update ()
		{
			timeView.Clear ();
			timeView.SetMainCounter (mainCounter, mainValue);
		}
		
		protected virtual void OnCheckSingleThreadToggled (object sender, System.EventArgs e)
		{
			timeView.SingleThread = checkSingleThread.Active;
		}
		
		protected virtual void OnButtonExpandClicked (object sender, System.EventArgs e)
		{
			timeView.ExpandAll ();
		}
		
		protected virtual void OnButtonCollapseClicked (object sender, System.EventArgs e)
		{
			timeView.CollapseAll ();
		}
		
		protected virtual void OnButtonResetScaleClicked (object sender, System.EventArgs e)
		{
			timeView.TimeScale = 100;
		}
		
		protected virtual void OnVscaleScaleChangeValue (object o, Gtk.ChangeValueArgs args)
		{
			timeView.TimeScale = vscaleScale.Value;
		}
	}
}

