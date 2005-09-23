//
// DateTimeAxis.cs
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

namespace MonoDevelop.Gui.Widgets.Chart
{
	public class DateTimeAxis: Axis
	{
		public DateTimeAxis ()
		{
		}
		
		public DateTimeAxis (bool showLabels): base (showLabels)
		{
		}
		
		protected override TickEnumerator CreateTickEnumerator (double minTickStep)
		{
			long val = (long) minTickStep;
			int scale;
			
			if (val > TimeSpan.TicksPerDay * 30 * 365)
				return null;
			else if (val > TimeSpan.TicksPerDay * 30)
				scale = 7;
			else if (val > TimeSpan.TicksPerDay)
				scale = 6;
			else if (val > TimeSpan.TicksPerHour)
				scale = 5;
			else if (val > TimeSpan.TicksPerMinute * 15)
				scale = 4;
			else if (val > TimeSpan.TicksPerMinute)
				scale = 3;
			else if (val > TimeSpan.TicksPerSecond * 15)
				scale = 2;
			else if (val > TimeSpan.TicksPerSecond)
				scale = 1;
			else
				scale = 0;
			
			return new DateTimeTickEnumerator (scale);
		}
	}
	
	internal class DateTimeTickEnumerator: TickEnumerator
	{
		int scale;
		DateTime current;
		
		public DateTimeTickEnumerator (int scale)
		{
			this.scale = scale;
		}
		
		public override void Init (double startValue)
		{
			DateTime t = new DateTime ((long)startValue);
			DateTime nt;
			switch (scale) {
				case 0: nt = new DateTime (t.Year, t.Month, t.Day, t.Hour, t.Minute, t.Second); break;
				case 1: nt = new DateTime (t.Year, t.Month, t.Day, t.Hour, t.Minute, (t.Second / 15) * 15); break;
				case 2: nt = new DateTime (t.Year, t.Month, t.Day, t.Hour, t.Minute, 0); break;
				case 3: nt = new DateTime (t.Year, t.Month, t.Day, t.Hour, (t.Minute / 15) * 15, 0); break;
				case 4: nt = new DateTime (t.Year, t.Month, t.Day, t.Hour, 0, 0); break;
				case 5: nt = new DateTime (t.Year, t.Month, t.Day); break;
				case 6: nt = new DateTime (t.Year, t.Month, 1); break;
				default: nt = new DateTime (t.Year, 1, 1); break;
			}
			current = nt;
		}
		
		public override void MoveNext ()
		{
			switch (scale) {
				case 0: current = current.AddSeconds (1); break;
				case 1: current = current.AddSeconds (15); break;
				case 2: current = current.AddMinutes (1); break;
				case 3: current = current.AddMinutes (15); break;
				case 4: current = current.AddHours (1); break;
				case 5: current = current.AddDays (1); break;
				case 6: current = current.AddMonths (1); break;
				case 7: current = current.AddYears (1); break;
			}
		}
		
		public override void MovePrevious ()
		{
			switch (scale) {
				case 0: current = current.AddSeconds (-1); break;
				case 1: current = current.AddSeconds (-15); break;
				case 2: current = current.AddMinutes (-1); break;
				case 3: current = current.AddMinutes (-15); break;
				case 4: current = current.AddHours (-1); break;
				case 5: current = current.AddDays (-1); break;
				case 6: current = current.AddMonths (-1); break;
				case 7: current = current.AddYears (-1); break;
			}
		}
		
		public override double CurrentValue {
			get { return (double) current.Ticks; }
		}
		
		public override string CurrentLabel {
			get {
				switch (scale) {
					case 0: case 1: return string.Format ("{0}:{1:00}:{2:00}", current.Hour, current.Minute, current.Second);
					case 2: case 3: return string.Format ("{0}:{1:00}", current.Hour, current.Minute);
					case 4: return string.Format ("{0}:00", current.Hour);
					case 5: return current.ToShortDateString ();
					case 6: return string.Format ("{0}/{1}", current.Month, current.Year);
					default: return string.Format ("{0}", current.Year);
				}
			}
		}
	}
}
