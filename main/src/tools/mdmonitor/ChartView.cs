// 
// InstrumentationViewerDialog.cs
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
using System.Linq;
using System.Collections.Generic;
using MonoDevelop.Core.Instrumentation;
using MonoDevelop.Core.Serialization;
using MonoDevelop.Components.Chart;
using Gtk;
using System.Globalization;
using MonoDevelop.Components;
using MonoDevelop.Core;
namespace Mono.Instrumentation.Monitor
{
class ChartView
	{
		[ItemProperty]
		public string Name { get; set; }
		
		[ItemProperty]
		public List<ChartSerieInfo> Series = new List<ChartSerieInfo> ();
		
		public ChartView EditedView;
		public bool Modified;
		
		public IEnumerable<Counter> GetCounters ()
		{
			foreach (ChartSerieInfo ci in Series)
				yield return ci.Counter;
		}
		
		public void CopyFrom (ChartView other)
		{
			Name = other.Name;
			Series.Clear ();
			foreach (ChartSerieInfo si in other.Series) {
				ChartSerieInfo c = new ChartSerieInfo ();
				c.CopyFrom (si);
				Series.Add (c);
			}
		}
		
		public bool Contains (Counter c)
		{
			foreach (ChartSerieInfo si in Series) {
				if (si.Name == c.Name)
					return true;
			}
			return false;
		}
		
		public void Add (Counter c)
		{
			ChartSerieInfo info = new ChartSerieInfo ();
			info.Init (c);
			Series.Add (info);
			Modified = true;
		}
		
/*		bool IsColorUsed (string color)
		{
			foreach (ChartSerieInfo info in Series)
				if (info.Color == color)
					return true;
			return false;
		}*/
		
		public void Remove (Counter c)
		{
			for (int n=0; n<Series.Count; n++) {
				if (Series [n].Name == c.Name) {
					Series.RemoveAt (n);
					return;
				}
			}
			Modified = true;
		}
		
		public void SetVisible (ChartSerieInfo info, bool visible)
		{
			info.Visible = visible;
			info.Serie.Visible = visible;
			Modified = true;
		}
		
		public void UpdateSeries ()
		{
			foreach (ChartSerieInfo info in Series)
				info.UpdateSerie ();
		}
		
		internal static Cairo.Color ParseColor (string s)
		{
			double r = ((double) int.Parse (s.Substring (0,2), System.Globalization.NumberStyles.HexNumber)) / 255;
			double g = ((double) int.Parse (s.Substring (2,2), System.Globalization.NumberStyles.HexNumber)) / 255;
			double b = ((double) int.Parse (s.Substring (4,2), System.Globalization.NumberStyles.HexNumber)) / 255;
			return new Cairo.Color (r, g, b);
		}
	}
}
