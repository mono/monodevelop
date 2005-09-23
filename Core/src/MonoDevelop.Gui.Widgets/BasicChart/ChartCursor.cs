//
// ChartCursor.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
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
using Gdk;

namespace MonoDevelop.Gui.Widgets.Chart
{
	public class ChartCursor
	{
		double val;
		internal AxisDimension Dimension;
		Gdk.Color color;
		int handleSize = 6;
		bool visible = true;
		bool showValueLabel;
		Axis labelAxis;
		
		public double Value {
			get { return val; }
			set { val = value; OnValueChanged (); }
		}
		
		public bool Visible {
			get { return visible; }
			set { visible = value; OnLayoutChanged (); }
		}
		
		public Gdk.Color Color {
			get { return color; }
			set { color = value; OnLayoutChanged (); }
		}
		
		public int HandleSize {
			get { return handleSize; }
			set { handleSize = value; OnLayoutChanged (); }
		}
		
		public bool ShowValueLabel {
			get { return showValueLabel; }
			set { showValueLabel = value; OnLayoutChanged (); }
		}
		
		public Axis LabelAxis {
			get { return labelAxis; }
			set { labelAxis = value; OnLayoutChanged (); }
		}
		
		public virtual void OnValueChanged ()
		{
			if (ValueChanged != null)
				ValueChanged (this, EventArgs.Empty);
		}
		
		public virtual void OnLayoutChanged ()
		{
			if (LayoutChanged != null)
				LayoutChanged (this, EventArgs.Empty);
		}
		
		public EventHandler ValueChanged;
		public EventHandler LayoutChanged;
	}
}
