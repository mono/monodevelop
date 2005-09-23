//
// TestChart.cs
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
using Gtk;
using Gdk;
using MonoDevelop.Gui.Widgets.Chart;

namespace MonoDevelop.NUnit
{
	public enum TestChartType {
		Results,
		Time
	}
	
	class TestRunAxis: IntegerAxis
	{
		public UnitTestResult[] CurrentResults;
		
		public TestRunAxis (bool showLabel): base (showLabel)
		{
		}
		
		public override string GetValueLabel (double value)
		{
			if (CurrentResults == null)
				return "";

			int val = (int) value;
			if (val >= CurrentResults.Length)
				return "";
				
			UnitTestResult res = CurrentResults [CurrentResults.Length - val - 1];
			return string.Format ("{0}/{1}", res.TestDate.Day, res.TestDate.Month);
		}
	}
	
	public class TestChart: BasicChart
	{
		Serie serieFailed;
		Serie serieSuccess;
		Serie serieIgnored;
		
		Serie serieTime;
		
		bool timeScale = false;
		bool singleDayResult = false;
		TestChartType type;
		
		TimeSpan currentSpan = TimeSpan.FromDays (5);
		int testCount = 20;
		UnitTest test;
		bool showLastTest = true;
		bool resetCursors = true;
		double lastDateValue;
		double lastTestNumber;
		UnitTestResult[] currentResults;
		TestRunAxis testRunAxis;
		
		public TestChart ()
		{
			AllowSelection = true;
			SetAutoScale (AxisDimension.Y, false, true);
			StartY = 0;
			
			serieFailed = new Serie ("Failed tests");
			serieFailed.Color = new Color (255, 0, 0);
			serieSuccess = new Serie ("Successful tests");
			serieSuccess.Color = new Color (0, 164, 0);
			serieIgnored = new Serie ("Ignored tests");
			serieIgnored.Color = new Color (206, 206, 0);
			
			serieTime = new Serie ("Time");
			serieTime.Color = new Color (0, 0, 255);
			
			UpdateMode ();
			
/*			EndX = DateTime.Now.Ticks;
			StartX = EndX - currentSpan.Ticks;
			*/
			EndX = 5;
			StartX = 0;
		}
		
		public bool ShowSuccessfulTests {
			get { return serieSuccess.Visible; }
			set { serieSuccess.Visible = value; }
		}
		
		public bool ShowFailedTests {
			get { return serieFailed.Visible; }
			set { serieFailed.Visible = value; }
		}
		
		public bool ShowIgnoredTests {
			get { return serieIgnored.Visible; }
			set { serieIgnored.Visible = value; }
		}
		
		public bool UseTimeScale {
			get { return timeScale; }
			set { timeScale = value; UpdateMode (); }
		}
		
		public bool SingleDayResult {
			get { return singleDayResult; }
			set { singleDayResult = value; UpdateMode (); }
		}
		
		public TestChartType Type {
			get { return type; }
			set { type = value; UpdateMode (); }
		}
		
		public DateTime CurrentDate {
			get {
				if (timeScale)
					return new DateTime ((long) SelectionEnd.Value);
				else {
					int n = (int) SelectionStart.Value;
					if (currentResults != null && n >= 0 && n < currentResults.Length)
						return currentResults [currentResults.Length - n - 1].TestDate;
					else
						return DateTime.MinValue;
				}
			}
		}
		
		public DateTime ReferenceDate {
			get {
				if (timeScale)
					return new DateTime ((long) SelectionStart.Value);
				else {
					int n = (int) SelectionEnd.Value;
					if (currentResults != null && n >= 0 && n < currentResults.Length)
						return currentResults [currentResults.Length - n - 1].TestDate;
					else
						return DateTime.MinValue;
				}
			}
		}
		
		void UpdateMode ()
		{
			AllowSelection = false;
			
			Reset ();

			if (type == TestChartType.Results) {
				AddSerie (serieIgnored);
				AddSerie (serieFailed);
				AddSerie (serieSuccess);
			} else {
				AddSerie (serieTime);
			}
			
			if (timeScale) {
				ReverseXAxis = false;
				Axis ax = new DateTimeAxis (true);
				AddAxis (new DateTimeAxis (false), AxisPosition.Top);
				AddAxis (ax, AxisPosition.Bottom);
				SelectionEnd.Value = SelectionStart.Value = DateTime.Now.Ticks;
				SelectionStart.LabelAxis = ax;
				SelectionEnd.LabelAxis = ax;
			} else {
				ReverseXAxis = true;
				AddAxis (new TestRunAxis (false), AxisPosition.Top);
				testRunAxis = new TestRunAxis (true);
				AddAxis (testRunAxis, AxisPosition.Bottom);
				SelectionEnd.Value = SelectionStart.Value = 0;
				SelectionStart.LabelAxis = testRunAxis;
				SelectionEnd.LabelAxis = testRunAxis;
			}
			showLastTest = true;
			resetCursors = true;
			
			AddAxis (new IntegerAxis (true), AxisPosition.Left);
			AddAxis (new IntegerAxis (true), AxisPosition.Right);
			
			if (test != null)
				Fill (test);
			
			AllowSelection = true;
		}
		
		public new void Clear ()
		{
			base.Clear ();
			test = null;
		}
		
		public void ZoomIn ()
		{
			if (test == null)
				return;
			if (timeScale) {
				currentSpan = new TimeSpan (currentSpan.Ticks / 2);
				if (currentSpan.TotalSeconds < 60)
					currentSpan = TimeSpan.FromSeconds (60);
			} else {
				testCount = testCount / 2;
				if (testCount < 5)
					testCount = 5;
			}
			Fill (test);
		}
		
		public void ZoomOut ()
		{
			if (test == null)
				return;
			if (timeScale) {
				currentSpan = new TimeSpan (currentSpan.Ticks * 2);
				if (currentSpan.TotalDays > 50 * 365)
					currentSpan = TimeSpan.FromDays (50 * 365);
			} else {
				testCount *= 2;
				if (testCount > 100000)
					testCount = 100000;
			}
			Fill (test);
		}
		
		public void GoNext ()
		{
			if (showLastTest)
				return;

			if (timeScale) {
				lastDateValue += (EndX - StartX) / 3;
				UnitTestResult lastResult = test.Results.GetLastResult (DateTime.Now);
				if (lastResult != null && new DateTime ((long)lastDateValue) > lastResult.TestDate)
					showLastTest = true;
			} else {
				lastTestNumber -= (EndX - StartX) / 3;
				if (lastTestNumber < 0)
					showLastTest = true;
			}
			Fill (test);
		}
		
		public void GoPrevious ()
		{
			if (timeScale) {
				lastDateValue -= (EndX - StartX) / 3;
			} else {
				lastTestNumber += (EndX - StartX) / 3;
			}
			showLastTest = false;
			Fill (test);
		}
		
		public void GoLast ()
		{
			showLastTest = true;
			resetCursors = true;
			Fill (test);
		}
		
		public void Fill (UnitTest test)
		{
			serieFailed.Clear ();
			serieSuccess.Clear ();
			serieIgnored.Clear ();
			serieTime.Clear ();
			
			this.test = test;
			
			if (showLastTest) {
				if (timeScale)
					lastDateValue = DateTime.Now.Ticks;
				else
					lastTestNumber = 0;
			}
			
			UnitTestResult first = null;
			UnitTestResult[] results;
			UnitTestResult lastResult = test.Results.GetLastResult (DateTime.Now);
			if (lastResult == null)
				return;
			
			if (timeScale) {
				DateTime startDate;
				if (showLastTest) {
					startDate = lastResult.TestDate - currentSpan;
					StartX = startDate.Ticks;
					EndX = lastResult.TestDate.Ticks;
					first = test.Results.GetLastResult (startDate);
					results = test.Results.GetResults (startDate, lastResult.TestDate);
				} else {
					DateTime endDate = new DateTime ((long)lastDateValue);
					startDate = endDate - currentSpan;
					StartX = (double) startDate.Ticks;
					EndX = (double) endDate.Ticks;
					first = test.Results.GetLastResult (startDate);
					results = test.Results.GetResults (startDate, lastResult.TestDate);
				}
				if (singleDayResult) {
					first = test.Results.GetPreviousResult (new DateTime (startDate.Year, startDate.Month, startDate.Day));
					ArrayList list = new ArrayList ();
					if (first != null)
						list.Add (first);
					for (int n=0; n<results.Length - 1; n++) {
						DateTime d1 = results [n].TestDate;
						DateTime d2 = results [n + 1].TestDate;
						if (d1.Day != d2.Day || d1.Month != d2.Month || d1.Year != d2.Year)
							list.Add (results[n]);
					}
					list.Add (results [results.Length - 1]);
					results = (UnitTestResult[]) list.ToArray (typeof(UnitTestResult));
				}
				
				if (resetCursors) {
					SelectionEnd.Value = EndX;
					if (results.Length > 1)
						SelectionStart.Value = results [results.Length - 2].TestDate.Ticks;
					else
						SelectionStart.Value = EndX;
					resetCursors = false;
				}
			} else {
				if (singleDayResult) {
					ArrayList list = new ArrayList ();
					list.Add (lastResult);
					while (list.Count < testCount + (int)lastTestNumber + 1) {
						UnitTestResult res = test.Results.GetPreviousResult (lastResult.TestDate);
						if (res == null) break;
						if (res.TestDate.Day != lastResult.TestDate.Day || res.TestDate.Month != lastResult.TestDate.Month || res.TestDate.Year != lastResult.TestDate.Year)
							list.Add (res);
						lastResult = res;
					}
					results = (UnitTestResult[]) list.ToArray (typeof(UnitTestResult));
					Array.Reverse (results);
				} else {
					results = test.Results.GetResultsToDate (DateTime.Now, testCount + (int)lastTestNumber + 1);
				}
				EndX = lastTestNumber + testCount;
				StartX = lastTestNumber;
				
				if (resetCursors) {
					SelectionStart.Value = StartX;
					SelectionEnd.Value = StartX + 1;
					resetCursors = false;
				}
			}
			
			
			currentResults = results;
			if (testRunAxis != null)
				testRunAxis.CurrentResults = currentResults;
			
			if (Type == TestChartType.Results) {
				if (first != null) {
					double x = timeScale ? first.TestDate.Ticks : results.Length;
					serieFailed.AddData (x, first.TotalFailures);
					serieSuccess.AddData (x, first.TotalSuccess);
					serieIgnored.AddData (x, first.TotalIgnored);
				}
				
				for (int n=0; n < results.Length; n++) {
					UnitTestResult res = results [n];
					double x = timeScale ? res.TestDate.Ticks : results.Length - n - 1;
					serieFailed.AddData (x, res.TotalFailures);
					serieSuccess.AddData (x, res.TotalSuccess);
					serieIgnored.AddData (x, res.TotalIgnored);
				}
			} else {
				if (first != null) {
					double x = timeScale ? first.TestDate.Ticks : results.Length;
					serieTime.AddData (x, first.Time.TotalMilliseconds);
				}
				for (int n=0; n < results.Length; n++) {
					UnitTestResult res = results [n];
					double x = timeScale ? res.TestDate.Ticks : results.Length - n - 1;
					serieTime.AddData (x, results [n].Time.TotalMilliseconds);
				}
			}
		}
	}
}
