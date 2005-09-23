//
// Serie.cs
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
using System.Collections;
using Gdk;

namespace MonoDevelop.Gui.Widgets.Chart
{
	public class Serie
	{
		string title;
		ArrayList dataArray = new ArrayList ();
		bool visible = true;
		internal BasicChart Owner;
		Color color;
		
		public Serie ()
		{
		}
		
		public Serie (string title)
		{
			this.title = title;
		}
		
		public void AddData (double x, double y)
		{
			dataArray.Add (new Data (x, y));
			OnSerieChanged ();
		}
		
		public void Clear ()
		{
			dataArray.Clear ();
			OnSerieChanged ();
		}
		
		public string Title {
			get { return title; }
			set { title = value; OnSerieChanged (); }
		}
		
		internal ArrayList Data {
			get { return dataArray; }
		}
		
		public bool Visible {
			get { return visible; }
			set { visible = value; OnSerieChanged (); }
		}
		
		public bool HasData {
			get { return dataArray.Count > 0; }
		}
		
		public virtual void OnSerieChanged ()
		{
			if (Owner != null)
				Owner.OnSerieChanged ();
		}
		
		public Color Color {
			get { return color; }
			set { color = value; OnSerieChanged (); }
		}
		
		public void GetRange (AxisDimension axis, out double min, out double max)
		{
			min = double.MaxValue;
			max = double.MinValue;
			foreach (Data d in dataArray) {
				double v = d.GetValue (axis);
				if (v > max) max = v;
				if (v < min) min = v;
			}
		}
	}

	internal class Data
	{
		double x;
		double y;
		
		internal Data (double x, double y)
		{
			this.x = x;
			this.y = y;
		}
		
		public double X {
			get { return x; }
			set { x = value; }
		}
		
		public double Y {
			get { return y; }
			set { y = value; }
		}
		
		public double GetValue (AxisDimension a) {
			if (a == AxisDimension.X) return x;
			else return y;
		}
	}	
}
