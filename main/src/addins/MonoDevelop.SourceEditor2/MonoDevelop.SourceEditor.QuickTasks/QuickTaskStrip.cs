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
using MonoDevelop.Components.AtkCocoaHelper;
using MonoDevelop.Components.Commands;
using System.Linq;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Editor.Extension;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace MonoDevelop.SourceEditor.QuickTasks
{
	interface IMapMode
	{
		void ForceDraw ();
	}
	class QuickTaskStrip : VBox
	{
		public readonly static ConfigurationProperty<bool> EnableFancyFeatures = IdeApp.Preferences.EnableSourceAnalysis;
		public readonly static bool MergeScrollBarAndQuickTasks = !MonoDevelop.Core.Platform.IsMac;

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

		Mono.TextEditor.MonoTextEditor textEditor;
		public Mono.TextEditor.MonoTextEditor TextEditor {
			get {
				return textEditor;
			}
			set {
				if (value == null)
					throw new ArgumentNullException ();
				if (textEditor != null)
					textEditor.EditorOptionsChanged -= TextEditor_EditorOptionsChanged;
				textEditor = value;
				textEditor.EditorOptionsChanged += TextEditor_EditorOptionsChanged;
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

		ImmutableDictionary<IQuickTaskProvider, ImmutableArray<QuickTask>> providerTasks = ImmutableDictionary<IQuickTaskProvider, ImmutableArray<QuickTask>>.Empty;
		ImmutableDictionary<UsageProviderEditorExtension, ImmutableArray<Usage>> providerUsages = ImmutableDictionary<UsageProviderEditorExtension, ImmutableArray<Usage>>.Empty;

		public IEnumerable<QuickTask> AllTasks {
			get {
				if (providerTasks == null)
					yield break;
				foreach (var tasks in providerTasks.Values) {
					foreach (var task in tasks) {
						if (task.Severity != DiagnosticSeverity.Hidden)
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

			Accessible.Name = "MainWindow.QuickTaskStrip";
			Accessible.SetShouldIgnore (false);
			Accessible.SetRole (AtkCocoa.Roles.AXRuler);
			Accessible.SetLabel (GettextCatalog.GetString ("Quick Task Strip"));
			Accessible.Description = GettextCatalog.GetString ("An overview of the current file's messages, warnings and errors");

			var actionHandler = new ActionDelegate (this);
			actionHandler.PerformShowMenu += PerformShowMenu;
		}

		void HandleChanged (object sender, EventArgs e)
		{
			SetupMode ();
		}

		Widget mapMode;
		QuickTaskOverviewMode overviewMode;

		void SetupMode ()
		{
			if (adj == null || textEditor == null)
				return;

			if (mapMode != null) {
				mapMode.Destroy ();
				mapMode = null;
			}
			if (overviewMode != null) {
				overviewMode.Destroy ();
				overviewMode = null;
			}

			if (EnableFancyFeatures) {
				switch (ScrollBarMode) {
				case ScrollBarMode.Overview:
					mapMode = overviewMode = new QuickTaskOverviewMode (this);
					PackStart (mapMode, true, true, 0);
					break;
				case ScrollBarMode.Minimap:
					mapMode = new QuickTaskMiniMapMode (this);
					overviewMode = null;
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
			adj = null;
			if (textEditor != null)
				textEditor.EditorOptionsChanged -= TextEditor_EditorOptionsChanged;
			textEditor = null;
			providerTasks = null;
			PropertyService.RemovePropertyHandler ("ScrollBar.Mode", ScrollBarModeChanged);
			EnableFancyFeatures.Changed -= HandleChanged;
			base.OnDestroyed ();
		}

		void ScrollBarModeChanged (object sender, PropertyChangedEventArgs args)
		{
			var newMode = (ScrollBarMode)args.NewValue;
			this.ScrollBarMode = newMode;
		}

		void UpdateAccessibility ()
		{
			AccessibilityElementProxy [] children = null;

			if (overviewMode != null && AccessibilityElementProxy.Enabled) {
				children = overviewMode.UpdateAccessibility ();
			}

			Accessible.SetAccessibleChildren (children);
		}

		public void Update (IQuickTaskProvider provider)
		{
			if (providerTasks == null)
				return;
			providerTasks = providerTasks.SetItem (provider, provider.QuickTasks);

			UpdateAccessibility ();
			OnTaskProviderUpdated (EventArgs.Empty);
		}

		public void Update (UsageProviderEditorExtension provider)
		{
			if (providerTasks == null)
				return;
			providerUsages = providerUsages.SetItem (provider, provider.Usages);

			UpdateAccessibility ();
			OnTaskProviderUpdated (EventArgs.Empty);
		}

		protected virtual void OnTaskProviderUpdated (EventArgs e)
		{
			var handler = this.TaskProviderUpdated;
			if (handler != null)
				handler (this, e);
		}

		public event EventHandler TaskProviderUpdated;

		internal void ShowMenu ()
		{
			int x, y;
			TranslateCoordinates (Toplevel, 0, 0, out x, out y);
			IdeApp.CommandService.ShowContextMenu (this, x, y, IdeApp.CommandService.CreateCommandEntrySet ("/MonoDevelop/SourceEditor2/ContextMenu/Scrollbar"), this);
		}

		void PerformShowMenu (object sender, EventArgs e)
		{
			ShowMenu (); 
		}

		protected override bool OnButtonPressEvent (EventButton evnt)
		{
			if (evnt.Button == 3) {
				IdeApp.CommandService.ShowContextMenu (this, evnt, IdeApp.CommandService.CreateCommandEntrySet ("/MonoDevelop/SourceEditor2/ContextMenu/Scrollbar"), this);
			}
			return base.OnButtonPressEvent (evnt);
		}

		void TextEditor_EditorOptionsChanged (object sender, EventArgs e)
		{
			(mapMode as IMapMode)?.ForceDraw ();
			QueueDraw ();
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

		#endregion

		internal enum HoverMode { NextMessage, NextWarning, NextError }
		internal QuickTask SearchNextTask (HoverMode mode)
		{
			var curLoc = TextEditor.Caret.Offset;
			QuickTask firstTask = null;
			foreach (var task in AllTasks.OrderBy (t => t.Location)) {
				bool isNextTask = task.Location > curLoc;
				if (mode == HoverMode.NextMessage ||
					mode == HoverMode.NextWarning && task.Severity == DiagnosticSeverity.Warning ||
					mode == HoverMode.NextError && task.Severity == DiagnosticSeverity.Error) {
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
			var curLoc = TextEditor.Caret.Offset;
			QuickTask firstTask = null;
			foreach (var task in AllTasks.OrderByDescending (t => t.Location)) {
				bool isNextTask = task.Location < curLoc;
				if (mode == HoverMode.NextMessage ||
					mode == HoverMode.NextWarning && task.Severity == DiagnosticSeverity.Warning ||
					mode == HoverMode.NextError && task.Severity == DiagnosticSeverity.Error) {
					if (isNextTask)
						return task;
					if (firstTask == null)
						firstTask = task;
				}
			}
			return firstTask;
		}

		internal void GotoTask (QuickTask quickTask, bool grabFocus = true)
		{
			if (quickTask == null)
				return;

			GotoLocation (quickTask.Location, grabFocus);
		}

		void GotoLocation (int location, bool grabFocus = true)
		{
			TextEditor.Caret.Offset = location;
			TextEditor.CenterToCaret ();
			TextEditor.StartCaretPulseAnimation ();

			if (grabFocus) {
				TextEditor.GrabFocus ();
			}
		}

		internal void GotoUsage (Usage usage, bool grabFocus = true)
		{
			if (usage == null)
				return;

			GotoLocation (usage.Offset, grabFocus);
		}
	}
}
