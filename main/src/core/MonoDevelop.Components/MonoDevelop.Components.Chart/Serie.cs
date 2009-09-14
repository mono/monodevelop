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
using System.Collections.Generic;
using Gdk;

namespace MonoDevelop.Components.Chart
{
	public class Serie
	{
		string title;
		List<Data> dataArray = new List<Data> ();
		bool visible = true;
		internal BasicChart Owner;
		Cairo.Color color;
		bool extendBoundingValues;
		DisplayMode mode;
		double initialValue;
		bool averageData;
		double averageSpan;
		double averageOrigin;
		int lineWidth = 2;
		
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
		
		public bool Visible {
			get { return visible; }
			set { visible = value; OnSerieChanged (); }
		}
		
		public bool ExtendBoundingValues {
			get { return extendBoundingValues; }
			set { extendBoundingValues = value; OnSerieChanged (); }
		}
		
		// Initial value to use when ExtendBoundingValues is set to true
		public double InitialValue {
			get { return initialValue; }
			set { initialValue = value; OnSerieChanged (); }
		}
		
		public DisplayMode DisplayMode {
			get { return mode; }
			set { mode = value; OnSerieChanged (); }
		}
		
		public bool AverageData {
			get { return this.averageData; }
			set { this.averageData = value; OnSerieChanged ();}
		}

		public double AverageSpan {
			get { return this.averageSpan; }
			set { this.averageSpan = value; OnSerieChanged ();}
		}

		public double AverageOrigin {
			get { return this.averageOrigin; }
			set { this.averageOrigin = value; OnSerieChanged (); }
		}
		
		public int LineWidth {
			get { return this.lineWidth; }
			set { this.lineWidth = value; }
		}
		
		public bool HasData {
			get { return dataArray.Count > 0; }
		}
		
		public virtual void OnSerieChanged ()
		{
			if (Owner != null)
				Owner.OnSerieChanged ();
		}
		
		public Cairo.Color Color {
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
		
		internal IEnumerable<Data> GetData (double startX, double endX)
		{
			if (dataArray.Count == 0)
				yield break;
			
			if (extendBoundingValues) {
				Data dfirst = dataArray [0];
				if (dfirst.X > startX)
					yield return new Data (startX, initialValue);
			}
			
			foreach (Data d in dataArray)
				yield return d;
			
			if (extendBoundingValues) {
				Data dlast = dataArray [dataArray.Count - 1];
				if (dlast.X < endX)
					yield return new Data (endX, dlast.Y);
			}
		}
	}
	
	public enum DisplayMode
	{
		Line,
		BlockLine,
		Bar
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
