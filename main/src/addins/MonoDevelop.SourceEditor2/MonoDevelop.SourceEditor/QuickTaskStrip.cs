// 
// QuickTaskStrip.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2011 Novell, Inc (http://www.novell.com)
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
using Gtk;
using Mono.TextEditor;
using System.Collections.Generic;
using Gdk;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Core;

namespace MonoDevelop.SourceEditor
{
	public enum QuickTaskSeverity
	{
		None,
		Error,
		Warning,
		Hint,
		Suggestion
	}
	
	public interface IQuickTaskProvider
	{
		IEnumerable<QuickTask> QuickTasks {
			get;
		}
		
		event EventHandler TasksUpdated;
	}
	
	public class QuickTask
	{
		public string Description {
			get;
			private set;
		}
		
		public DomLocation Location {
			get;
			private set;
		}
		
		public QuickTaskSeverity Severity {
			get;
			private set;
		}
		
		public QuickTask (string description, DomLocation location, QuickTaskSeverity severity)
		{
			this.Description = description;
			this.Location = location;
			this.Severity = severity;
		}
		
		public override string ToString ()
		{
			return string.Format ("[QuickTask: Description={0}, Location={1}, Severity={2}]", Description, Location, Severity);
		}
	}
	
	public class QuickTaskStrip : DrawingArea
	{
		Adjustment adj;

		public Adjustment VAdjustment {
			get {
				return this.adj;
			}
			set {
				adj = value;
				adj.ValueChanged += (sender, e) => QueueDraw ();
				adj.Changed += (sender, e) => QueueDraw ();
			}
		}
		
		Mono.TextEditor.TextEditor textEditor;
		public TextEditor TextEditor {
			get {
				return textEditor;
			}
			set {
				if (value == null)
					throw new ArgumentNullException ();
				textEditor = value;
				textEditor.HighlightSearchPatternChanged += (sender, e) => QueueDraw ();
				textEditor.TextViewMargin.SearchRegionsUpdated += (sender, e) => QueueDraw ();
				textEditor.TextViewMargin.MainSearchResultChanged += (sender, e) => QueueDraw ();
			}
		}
		
		Dictionary<IQuickTaskProvider, List<QuickTask>> providerTasks = new Dictionary<IQuickTaskProvider, List<QuickTask>> ();
		
		IEnumerable<QuickTask> AllTasks {
			get {
				foreach (var tasks in providerTasks.Values) {
					foreach (var task in tasks) {
						yield return task;
					}
				}
			}
		}
		
		public QuickTaskStrip ()
		{
			Events |= EventMask.ButtonPressMask | EventMask.ButtonReleaseMask | EventMask.ButtonMotionMask | EventMask.PointerMotionMask;
		}
		
		public void Update (IQuickTaskProvider provider)
		{
			providerTasks [provider] = new List<QuickTask> (provider.QuickTasks);
			QueueDraw ();
		}
		
		Cairo.Color GetBarColor (QuickTaskSeverity severity)
		{
			switch (severity) {
			case QuickTaskSeverity.Error:
				return TextEditor.ColorStyle.ErrorUnderline;
			case QuickTaskSeverity.Warning:
				return TextEditor.ColorStyle.WarningUnderline;
			case QuickTaskSeverity.Suggestion:
				return TextEditor.ColorStyle.SuggestionUnderline;
			case QuickTaskSeverity.Hint:
				return TextEditor.ColorStyle.HintUnderline;
			case QuickTaskSeverity.None:
				return TextEditor.ColorStyle.Default.CairoColor;
			default:
				throw new ArgumentOutOfRangeException ();
			}
		}
		
		Cairo.Color GetIndicatorColor (QuickTaskSeverity severity)
		{
			switch (severity) {
			case QuickTaskSeverity.Error:
				return new Cairo.Color (254 / 255.0, 58 / 255.0, 22 / 255.0);
			case QuickTaskSeverity.Warning:
				return new Cairo.Color (251 / 255.0, 247 / 255.0, 88 / 255.0);
			default:
				return new Cairo.Color (75 / 255.0, 255 / 255.0, 75 / 255.0);
			}
		}
		
		QuickTask hoverTask = null;
		
		protected override bool OnMotionNotifyEvent (EventMotion evnt)
		{
			if (button != 0)
				MouseMove (evnt.Y);
			
			int h = Allocation.Height - Allocation.Width - 3;
			if (TextEditor.HighlightSearchPattern) {
				if (evnt.Y > h)
					this.TooltipText = string.Format (GettextCatalog.GetPluralString ("{0} match", "{0} matches", TextEditor.TextViewMargin.SearchResultMatchCount), TextEditor.TextViewMargin.SearchResultMatchCount);
			} else { 
				if (evnt.Y > h) {
					int errors = 0, warnings = 0;
					foreach (var task in AllTasks) {
						switch (task.Severity) {
						case QuickTaskSeverity.Error:
							errors++;
							break;
						case QuickTaskSeverity.Warning:
							warnings++;
							break;
						}
					}
					string text = null;
					if (errors == 0 && warnings == 0) {
						text = GettextCatalog.GetString ("No errors or warnings");
					} else if (errors == 0) {
						text = string.Format (GettextCatalog.GetPluralString ("{0} warning", "{0} warnings", warnings), warnings);
					} else if (warnings == 0) {
						text = string.Format (GettextCatalog.GetPluralString ("{0} error", "{0} errors", errors), errors);
					} else {
						text = string.Format (GettextCatalog.GetString ("{0} errors and {1} warnings"), errors, warnings);
					}
					this.TooltipText = text;
				} else {
					TextEditorData editorData = TextEditor.GetTextEditorData ();
					foreach (var tasks in providerTasks.Values) {
						foreach (var task in tasks) {
							double y = h * TextEditor.LineToY (task.Location.Line) / Math.Max (TextEditor.EditorLineThreshold * editorData.LineHeight + editorData.TotalHeight, TextEditor.Allocation.Height);
							if (Math.Abs (y - evnt.Y) < 3) {
								hoverTask = task;
							}
						}
					}
					base.TooltipText = hoverTask != null ? hoverTask.Description : null;
				}
			}
			
			return base.OnMotionNotifyEvent (evnt);
		}
		
		void MouseMove (double y)
		{
			if (button != 1)
				return;
			int markerHeight = Allocation.Width + 6;
			double position = (y / (Allocation.Height - markerHeight)) * adj.Upper - adj.PageSize / 2;
			position = Math.Max (adj.Lower, Math.Min (position, adj.Upper - adj.PageSize));
			adj.Value = position;
		}
		
		uint button;

		protected override bool OnButtonPressEvent (EventButton evnt)
		{
			button |= evnt.Button;
			
			if (evnt.Button == 1 && hoverTask != null) {
				TextEditor.Caret.Location = new DocumentLocation (hoverTask.Location.Line, Math.Max (DocumentLocation.MinColumn, hoverTask.Location.Column));
				TextEditor.CenterToCaret ();
				TextEditor.StartCaretPulseAnimation ();
				TextEditor.GrabFocus ();
			} 
			
			MouseMove (evnt.Y);
			
			return base.OnButtonPressEvent (evnt);
		}
		
		protected override bool OnButtonReleaseEvent (EventButton evnt)
		{
			button &= ~evnt.Button;
			return base.OnButtonReleaseEvent (evnt);
		}

		void DrawIndicator (Cairo.Context cr, QuickTaskSeverity severity)
		{
			cr.Rectangle (3, Allocation.Height - Allocation.Width + 3, Allocation.Width - 6, Allocation.Width - 6);
			
			var darkColor = (HslColor)GetIndicatorColor (severity);
			darkColor.L *= 0.5;
			
			using (var pattern = new Cairo.LinearGradient (0, 0, Allocation.Width - 3, Allocation.Width - 3)) {
				pattern.AddColorStop (0, darkColor);
				pattern.AddColorStop (1, GetIndicatorColor (severity));
				cr.Pattern = pattern;
				cr.FillPreserve ();
			}
			
			cr.Color = darkColor;
			cr.Stroke ();
		}

		void DrawSearchIndicator (Cairo.Context cr)
		{
			int x1 = 1 + Allocation.Width / 2;
			int y1 = Allocation.Height - Allocation.Width + (Allocation.Width + 3) / 2;
			cr.Arc (x1, 
				y1, 
				(Allocation.Width - 5) / 2, 
				0, 
				2 * Math.PI);
			
			var darkColor = (HslColor)TextEditor.ColorStyle.SearchTextBg;
			darkColor.L *= 0.5;
			
			using (var pattern = new Cairo.RadialGradient (x1, y1, Allocation.Width / 2, x1 - Allocation.Width, y1 - Allocation.Width, Allocation.Width)) {
				pattern.AddColorStop (0, darkColor);
				pattern.AddColorStop (1, TextEditor.ColorStyle.SearchTextMainBg);
				cr.Pattern = pattern;
				cr.FillPreserve ();
			}
			
			cr.Color = darkColor;
			cr.Stroke ();
		}

		protected override void OnSizeRequested (ref Requisition requisition)
		{
			base.OnSizeRequested (ref requisition);
			requisition.Width = 17;
		}

		QuickTaskSeverity DrawQuickTasks (Cairo.Context cr)
		{
			QuickTaskSeverity severity = QuickTaskSeverity.None;
			int h = Allocation.Height - Allocation.Width - 6;
			foreach (var task in AllTasks) {
				double y = h * TextEditor.LineToY (task.Location.Line) / Math.Max (TextEditor.EditorLineThreshold * TextEditor.LineHeight + TextEditor.GetTextEditorData ().TotalHeight, TextEditor.Allocation.Height);
					
				var color = (HslColor)GetBarColor (task.Severity);
				cr.Color = color;
				cr.Rectangle (3, y - 1, Allocation.Width - 5, 4);
				cr.FillPreserve ();
				
				color.L *= 0.7;
				cr.Color = color;
				cr.Rectangle (3, y - 1, Allocation.Width - 5, 4);
				cr.Stroke ();
				
				switch (task.Severity) {
				case QuickTaskSeverity.Error:
					severity = QuickTaskSeverity.Error;
					break;
				case QuickTaskSeverity.Warning:
					if (severity == QuickTaskSeverity.None)
						severity = QuickTaskSeverity.Warning;
					break;
				}
			}
			return severity;
		}

		void DrawLeftBorder (Cairo.Context cr)
		{
			cr.MoveTo (0.5, 0);
			cr.LineTo (0.5, Allocation.Height);
			if (TextEditor.ColorStyle != null)
				cr.Color = TextEditor.ColorStyle.FoldLine.CairoColor;
			cr.Stroke ();
		}

		void DrawBar (Cairo.Context cr)
		{
			if (adj == null || adj.Upper <= adj.PageSize) 
				return;
			int h = Allocation.Height - Allocation.Width - 6;
			cr.Rectangle (1.5,
				              h * adj.Value / adj.Upper + cr.LineWidth + 0.5,
				              Allocation.Width - 2,
				              h * (adj.PageSize / adj.Upper));
			Cairo.Color color = (TextEditor.ColorStyle != null) ? TextEditor.ColorStyle.Default.CairoColor : new Cairo.Color (0, 0, 0);
			color.A = 0.5;
			cr.Color = color;
			cr.StrokePreserve ();
			
			color.A = 0.05;
			cr.Color = color;
			cr.Fill ();
		}
		
		void DrawSearchResults (Cairo.Context cr)
		{
			int h = Allocation.Height - Allocation.Width - 6;
			foreach (var region in TextEditor.TextViewMargin.SearchResults) {
				int line = TextEditor.OffsetToLineNumber (region.Offset);
				double y = h * TextEditor.LineToY (line) / Math.Max (TextEditor.EditorLineThreshold * TextEditor.LineHeight + TextEditor.GetTextEditorData ().TotalHeight, TextEditor.Allocation.Height);
				bool isMainSelection = false;
				if (TextEditor.TextViewMargin.MainSearchResult != null)
					isMainSelection = region.Offset == TextEditor.TextViewMargin.MainSearchResult.Offset;
				var color = (HslColor)(isMainSelection ? TextEditor.ColorStyle.SearchTextMainBg : TextEditor.ColorStyle.SearchTextBg);
				cr.Color = color;
				cr.Rectangle (3, y - 1, Allocation.Width - 5, 4);
				cr.FillPreserve ();
			
				color.L *= 0.7;
				cr.Color = color;
				cr.Rectangle (3, y - 1, Allocation.Width - 5, 4);
				cr.Stroke ();
			}
		}
		
		protected override bool OnExposeEvent (Gdk.EventExpose e)
		{
			using (Cairo.Context cr = Gdk.CairoHelper.Create (e.Window)) {
				cr.LineWidth = 1;
				cr.Rectangle (0, 0, Allocation.Width, Allocation.Height);
				if (TextEditor.ColorStyle != null)
					cr.Color = TextEditor.ColorStyle.Default.CairoBackgroundColor;
				cr.Fill ();
				
				cr.Color = (HslColor)Style.Dark (State);
				cr.MoveTo (0.5, 0.5);
				cr.LineTo (Allocation.Width, 0.5);
				cr.Stroke ();
				
				if (TextEditor == null)
					return true;
				
				if (TextEditor.HighlightSearchPattern) {
					DrawSearchResults (cr);
					DrawSearchIndicator (cr);
				} else {
					var severity = DrawQuickTasks (cr);
					DrawIndicator (cr, severity);
				}
				
				DrawBar (cr);
				DrawLeftBorder (cr);
			}
			
			return true;
		}
	}
}

