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
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Components.Commands;
using ICSharpCode.NRefactory;
using System.Linq;
using ICSharpCode.NRefactory.Refactoring;

namespace MonoDevelop.SourceEditor.QuickTasks
{
	public class QuickTaskStrip : VBox
	{
		// move that one to AnalysisOptions when the new features are enabled by default.
		public readonly static PropertyWrapper<bool> EnableFancyFeatures = new PropertyWrapper<bool> ("MonoDevelop.AnalysisCore.AnalysisEnabled", false);
		public readonly static bool MergeScrollBarAndQuickTasks = !Platform.IsMac;

		static QuickTaskStrip ()
		{
			EnableFancyFeatures.Changed += delegate {
				PropertyService.Set ("ScrollBar.Mode", ScrollBarMode.Overview);
			};
		}
		Adjustment adj;
		
		public Adjustment VAdjustment {
			get {
				return this.adj;
			}
			set {
				adj = value;
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
				SetupMode ();
			}
		}
		
		ScrollBarMode mode;
		public ScrollBarMode ScrollBarMode {
			get {
				return this.mode;
			}
			set {
				mode = value;
				PropertyService.Set ("ScrollBar.Mode", value);
				SetupMode ();
			}
		}
		
		Dictionary<IQuickTaskProvider, List<QuickTask>> providerTasks = new Dictionary<IQuickTaskProvider, List<QuickTask>> ();
		Dictionary<IUsageProvider, List<Usage>> providerUsages = new Dictionary<IUsageProvider, List<Usage>> ();

		public IEnumerable<QuickTask> AllTasks {
			get {
				if (providerTasks == null)
					yield break;
				foreach (var tasks in providerTasks.Values) {
					foreach (var task in tasks) {
						yield return task;
					}
				}
			}
		}
		public IEnumerable<Usage> AllUsages {
			get {
				if (providerUsages == null)
					yield break;
				foreach (var tasks in providerUsages.Values) {
					foreach (var task in tasks) {
						yield return task;
					}
				}
			}
		}

		public QuickTaskStrip ()
		{
			ScrollBarMode = PropertyService.Get ("ScrollBar.Mode", ScrollBarMode.Overview);
			PropertyService.AddPropertyHandler ("ScrollBar.Mode", ScrollBarModeChanged);
			EnableFancyFeatures.Changed += HandleChanged;
			Events |= EventMask.ButtonPressMask;
		}

		void HandleChanged (object sender, EventArgs e)
		{
			SetupMode ();
		}
		
		Widget mapMode;
		void SetupMode ()
		{
			if (adj == null || textEditor == null)
				return;

			if (mapMode != null) {
				mapMode.Destroy ();
				mapMode = null;
			}
			if (EnableFancyFeatures) {
				switch (ScrollBarMode) {
				case ScrollBarMode.Overview:
					mapMode = new QuickTaskOverviewMode (this);
					PackStart (mapMode, true, true, 0);
					break;
				case ScrollBarMode.Minimap:
					mapMode = new QuickTaskMiniMapMode (this);
					PackStart (mapMode, true, true, 0);
					break;
				default:
					throw new ArgumentOutOfRangeException ();
				}
			}
			ShowAll ();
		}
		
		protected override void OnDestroyed ()
		{
			base.OnDestroyed ();
			adj = null;
			textEditor = null;
			providerTasks = null;
			PropertyService.RemovePropertyHandler ("ScrollBar.Mode", ScrollBarModeChanged);
			EnableFancyFeatures.Changed -= HandleChanged;
		}
		
		void ScrollBarModeChanged (object sender, PropertyChangedEventArgs args)
		{
			var newMode =  (ScrollBarMode)args.NewValue;
			this.ScrollBarMode = newMode;
		}
		
		public void Update (IQuickTaskProvider provider)
		{
			if (providerTasks == null)
				return;
			providerTasks [provider] = new List<QuickTask> (provider.QuickTasks);
			OnTaskProviderUpdated (EventArgs.Empty);
		}
		
		public void Update (IUsageProvider provider)
		{
			if (providerTasks == null)
				return;
			providerUsages [provider] = new List<Usage> (provider.Usages);
			OnTaskProviderUpdated (EventArgs.Empty);
		}

		protected virtual void OnTaskProviderUpdated (EventArgs e)
		{
			var handler = this.TaskProviderUpdated;
			if (handler != null)
				handler (this, e);
		}
		
		public event EventHandler TaskProviderUpdated;
		
		protected override bool OnButtonPressEvent (EventButton evnt)
		{
			if (evnt.Button == 3) {
				IdeApp.CommandService.ShowContextMenu (this, evnt, "/MonoDevelop/SourceEditor2/ContextMenu/Scrollbar");
			}
			return base.OnButtonPressEvent (evnt);
		}
		
		#region Command handlers
		[CommandHandler (ScrollbarCommand.Top)]
		internal void GotoTop ()
		{
			VAdjustment.Value = VAdjustment.Lower;
		}

		[CommandHandler (ScrollbarCommand.Bottom)]
		internal void GotoBottom ()
		{
			VAdjustment.Value = Math.Max (VAdjustment.Lower, VAdjustment.Upper - VAdjustment.PageSize / 2);
		}

		[CommandHandler (ScrollbarCommand.PgUp)]
		internal void GotoPgUp ()
		{
			VAdjustment.Value = Math.Max (VAdjustment.Lower, VAdjustment.Value - VAdjustment.PageSize);
		}

		[CommandHandler (ScrollbarCommand.PgDown)]
		internal void GotoPgDown ()
		{
			VAdjustment.Value = Math.Min (VAdjustment.Upper, VAdjustment.Value + VAdjustment.PageSize);
		}	

		[CommandUpdateHandler (ScrollbarCommand.ShowTasks)]
		internal void UpdateShowMap (CommandInfo info)
		{
			info.Visible = EnableFancyFeatures;
			info.Checked = ScrollBarMode == ScrollBarMode.Overview;
		}

		[CommandHandler (ScrollbarCommand.ShowTasks)]
		internal void ShowMap ()
		{
			ScrollBarMode = ScrollBarMode.Overview; 
		}

		[CommandUpdateHandler (ScrollbarCommand.ShowMinimap)]
		internal void UpdateShowFull (CommandInfo info)
		{
			info.Visible = EnableFancyFeatures;
			info.Checked = ScrollBarMode == ScrollBarMode.Minimap;
		}

		[CommandHandler (ScrollbarCommand.ShowMinimap)]
		internal void ShowFull ()
		{
			ScrollBarMode = ScrollBarMode.Minimap; 
		}

		internal enum HoverMode { NextMessage, NextWarning, NextError }
		internal QuickTask SearchNextTask (HoverMode mode)
		{
			var curLoc = (TextLocation)TextEditor.Caret.Location;
			QuickTask firstTask = null;
			foreach (var task in AllTasks.OrderBy (t => t.Location) ) {
				bool isNextTask = task.Location > curLoc;
				if (mode == HoverMode.NextMessage ||
					mode == HoverMode.NextWarning && task.Severity == Severity.Warning ||
					mode == HoverMode.NextError && task.Severity == Severity.Error) {
					if (isNextTask)
						return task;
					if (firstTask == null)
						firstTask = task;
				}
			}
			return firstTask;
		}

		internal QuickTask SearchPrevTask (HoverMode mode)
		{
			var curLoc = (TextLocation)TextEditor.Caret.Location;
			QuickTask firstTask = null;
			foreach (var task in AllTasks.OrderByDescending (t => t.Location) ) {
				bool isNextTask = task.Location < curLoc;
				if (mode == HoverMode.NextMessage ||
					mode == HoverMode.NextWarning && task.Severity == Severity.Warning ||
					mode == HoverMode.NextError && task.Severity == Severity.Error) {
					if (isNextTask)
						return task;
					if (firstTask == null)
						firstTask = task;
				}
			}
			return firstTask;
		}

		internal void GotoTask (QuickTask quickTask)
		{
			if (quickTask == null)
				return;
			var line = quickTask.Location.Line;
			if (line < 1 || line >= TextEditor.LineCount)
				return;
			TextEditor.Caret.Location = new TextLocation (line, Math.Max (1, quickTask.Location.Column));
			TextEditor.CenterToCaret ();
			TextEditor.StartCaretPulseAnimation ();
			TextEditor.GrabFocus ();
		}

		#endregion
	}
}
