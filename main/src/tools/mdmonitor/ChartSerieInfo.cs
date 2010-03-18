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
using System.Collections.Generic;
using MonoDevelop.Core.Instrumentation;
using MonoDevelop.Core.Serialization;
using MonoDevelop.Components.Chart;
using MonoDevelop.Components;

namespace Mono.Instrumentation.Monitor
{
	class ChartSerieInfo
	{
		Serie serie;
		Counter counter;
		
		[ItemProperty]
		public string Name;
		
//		[ItemProperty]
//		public string Color;
		
		[ItemProperty (DefaultValue=true)]
		public bool Visible = true;
		
		DateTime lastUpdateTime = DateTime.MinValue;
		
		public void Init (Counter counter)
		{
			this.counter = counter;
			Name = counter.Name;
		}
		
		public void CopyFrom (ChartSerieInfo other)
		{
			Name = other.Name;
			Visible = other.Visible;
			serie = other.serie;
			counter = other.counter;
		}
		
		public Counter Counter {
			get {
				if (counter == null && Name != null)
					counter = App.Service.GetCounter (Name);
				return counter;
			}
		}
		
		public Serie Serie {
			get {
				if (serie == null) {
					serie = new Serie (Name);
					if (Counter == null)
						return serie;
					if (Counter.DisplayMode == CounterDisplayMode.Block) {
						serie.ExtendBoundingValues = true;
						serie.InitialValue = 0;
						serie.DisplayMode = DisplayMode.BlockLine;
					} else
						serie.DisplayMode = DisplayMode.Line;
					serie.Color = GdkColor.ToCairoColor ();
					foreach (CounterValue val in Counter.GetValues ()) {
						serie.AddData (val.TimeStamp.Ticks, val.Value);
						lastUpdateTime = val.TimeStamp;
					}
					serie.Visible = Visible;
				}
				return serie;
			}
		}
		
		public bool UpdateCounter ()
		{
			if (!Counter.Disposed)
				return false;
			serie = null;
			counter = null;
			lastUpdateTime = DateTime.MinValue;
			return true;
		}
		
		public void UpdateSerie ()
		{
			if (serie == null || Counter == null)
				return;
			foreach (CounterValue val in Counter.GetValuesAfter (lastUpdateTime)) {
				serie.AddData (val.TimeStamp.Ticks, val.Value);
				lastUpdateTime = val.TimeStamp;
			}
		}
		
		public Gdk.Color GdkColor {
			get {
				return Counter.GetColor ();
			}
		}
		
		public Gdk.Pixbuf ColorIcon {
			get {
				return Counter.GetIcon ();
			}
		}
	}
}
