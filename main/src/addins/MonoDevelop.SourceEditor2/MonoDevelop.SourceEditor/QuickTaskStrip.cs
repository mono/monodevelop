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
		Suggestion,
		Todo
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
		public TextEditor TextEditor {
			get;
			set;
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
			WidthRequest = 17;
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
				return new Cairo.Color (1, 0, 0);
			case QuickTaskSeverity.Warning:
				return new Cairo.Color (1, 0.65, 0);
			case QuickTaskSeverity.Suggestion:
				return new Cairo.Color (34 / 255.0, 139 / 255.0, 34 / 255.0);
			case QuickTaskSeverity.Todo:
				return new Cairo.Color (173 / 255.0, 216 / 255.0, 230 / 255.0);
			case QuickTaskSeverity.None:
				return new Cairo.Color (75 / 255.0, 255 / 255.0, 75 / 255.0);
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
			if (evnt.Y < Allocation.Width - 3) {
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
				foreach (var tasks in providerTasks.Values) {
					foreach (var task in tasks) {
						int y = Allocation.Height * task.Location.Line / TextEditor.LineCount;
						if (Math.Abs (y - evnt.Y) < 2) {
							hoverTask = task;
						}
					}
				}
				base.TooltipText = hoverTask != null ? hoverTask.Description : null;
			}
			
			return base.OnMotionNotifyEvent (evnt);
		}
		
		protected override bool OnButtonPressEvent (EventButton evnt)
		{
			if (evnt.Button == 1 && hoverTask != null) {
				TextEditor.Caret.Location = new DocumentLocation (hoverTask.Location.Line, hoverTask.Location.Column);
				TextEditor.CenterToCaret ();
			} 
			return base.OnButtonPressEvent (evnt);
		}
		
		public void DrawIndicator (Cairo.Context cr, QuickTaskSeverity severity)
		{
			cr.Rectangle (3, 3, Allocation.Width - 6, Allocation.Width - 6);
			
			var darkColor = (HslColor)GetIndicatorColor (severity);
			darkColor.L *= 0.5;
			
			var pattern = new Cairo.LinearGradient (0, 0, Allocation.Width - 3, Allocation.Width - 3);
			pattern.AddColorStop (0, darkColor);
			pattern.AddColorStop (1, GetIndicatorColor (severity));
			cr.Pattern = pattern;
			cr.FillPreserve ();
			pattern.Dispose ();
			
			cr.Color = darkColor;
			cr.Stroke ();
		}
		
		protected override bool OnExposeEvent (Gdk.EventExpose e)
		{
			using (Cairo.Context cr = Gdk.CairoHelper.Create (e.Window)) {
				cr.LineWidth = 1;
				cr.Rectangle (0, 0, Allocation.Width, Allocation.Height);
				cr.Color = (HslColor)Style.Base (StateType.Normal);
				cr.Fill ();
				
				cr.Color = (HslColor)Style.Dark (State);
				cr.MoveTo (0.5, 0.5);
				cr.LineTo (Allocation.Width, 0.5);
				cr.Stroke ();
				
				if (TextEditor == null)
					return true;
				
				QuickTaskSeverity severity = QuickTaskSeverity.None;

				foreach (var task in AllTasks) {
					int y = Allocation.Height * task.Location.Line / TextEditor.LineCount;
						
					cr.Color = GetBarColor (task.Severity);
					cr.Rectangle (3, y, Allocation.Width - 6, 2);
					cr.Fill ();
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
				
				DrawIndicator (cr, severity);
			}
			
			return true;
		}
	}
}

