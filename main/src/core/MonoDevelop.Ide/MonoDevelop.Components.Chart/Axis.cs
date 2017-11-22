//
// Axis.cs
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

namespace MonoDevelop.Components.Chart
{
	public abstract class Axis
	{
		int tickSize = 6;
		internal ChartWidget Owner;
		AxisPosition position;
		AxisDimension dim;
		bool showLabels = true;
		
		public Axis ()
		{
		}
		
		public Axis (bool showLabels)
		{
			this.showLabels = showLabels;
		}
		
		public int TickSize {
			get { return tickSize; }
			set {
				tickSize = value;
				if (Owner != null) Owner.QueueDraw ();
			}
		}
		
		public bool ShowLabels {
			get { return showLabels; }
			set {
				showLabels = value;
				if (Owner != null) Owner.OnLayoutChanged ();
			}
		}
		
		internal AxisPosition Position {
			get { return position; }
			set {
				position = value;
				if (position == AxisPosition.Top || position == AxisPosition.Bottom)
					dim = AxisDimension.X;
				else
					dim = AxisDimension.Y;
			}
		}
		
		internal AxisDimension Dimension {
			get { return dim; }
		}
		
		public TickEnumerator GetTickEnumerator (double minTickStep)
		{
			TickEnumerator e = CreateTickEnumerator (minTickStep);
			if (e != null)
				e.axis = this;
			return e;
		}
		
		protected abstract TickEnumerator CreateTickEnumerator (double minTickStep);
		
		public virtual string GetValueLabel (double value)
		{
			return value.ToString ();
		}
	}
}
