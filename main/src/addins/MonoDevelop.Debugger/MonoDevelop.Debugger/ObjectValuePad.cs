// ObjectValuePad.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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
//
//

using System;

using Gtk;
using MonoDevelop.Ide.Gui;
using Mono.Debugging.Client;
using MonoDevelop.Components;

namespace MonoDevelop.Debugger
{
	public class ObjectValuePad : PadContent
	{
		protected bool UseNewTreeView = false;

		protected ObjectValueTreeViewController controller;
		protected ObjectValueTreeView tree;

		readonly ScrolledWindow scrolled;
		bool needsUpdateValues;
		bool needsUpdateFrame;
		bool initialResume;
		StackFrame lastFrame;
		PadFontChanger fontChanger;

		public override Control Control {
			get {
				return scrolled;
			}
		}

		public ObjectValuePad (bool useNewTreeView = false)
		{
			UseNewTreeView = useNewTreeView;

			scrolled = new ScrolledWindow ();
			scrolled.HscrollbarPolicy = PolicyType.Automatic;
			scrolled.VscrollbarPolicy = PolicyType.Automatic;

			if (UseNewTreeView) {
				controller = new ObjectValueTreeViewController ();
				controller.AllowEditing = true;
				controller.AllowAdding = false;

				var treeView = controller.GetControl () as GtkObjectValueTreeView;
				fontChanger = new PadFontChanger (treeView, treeView.SetCustomFont, treeView.QueueResize);

				scrolled.Add (treeView);
			} else {
				tree = new ObjectValueTreeView ();
				tree.AllowEditing = true;
				tree.AllowAdding = false;

				fontChanger = new PadFontChanger (tree, tree.SetCustomFont, tree.QueueResize);

				scrolled.Add (tree);
			}

			scrolled.ShowAll ();

			DebuggingService.CurrentFrameChanged += OnFrameChanged;
			DebuggingService.PausedEvent += OnDebuggerPaused;
			DebuggingService.ResumedEvent += OnDebuggerResumed;
			DebuggingService.StoppedEvent += OnDebuggerStopped;
			DebuggingService.EvaluationOptionsChanged += OnEvaluationOptionsChanged;
			DebuggingService.VariableChanged += OnVariableChanged;

			needsUpdateValues = false;
			needsUpdateFrame = true;

			//If pad is created/opened while debugging...
			initialResume = !DebuggingService.IsDebugging;
		}

		public override void Dispose ()
		{
			if (fontChanger == null)
				return;

			fontChanger.Dispose ();
			fontChanger = null;
			DebuggingService.CurrentFrameChanged -= OnFrameChanged;
			DebuggingService.PausedEvent -= OnDebuggerPaused;
			DebuggingService.ResumedEvent -= OnDebuggerResumed;
			DebuggingService.StoppedEvent -= OnDebuggerStopped;
			DebuggingService.EvaluationOptionsChanged -= OnEvaluationOptionsChanged;
			DebuggingService.VariableChanged -= OnVariableChanged;
			base.Dispose ();
		}

		protected override void Initialize (IPadWindow window)
		{
			window.PadContentShown += delegate {
				if (needsUpdateFrame)
					OnUpdateFrame ();
				else if (needsUpdateValues)
					OnUpdateValues ();
			};
		}

		public virtual void OnUpdateFrame ()
		{
			needsUpdateValues = false;
			needsUpdateFrame = false;

			if (DebuggingService.CurrentFrame != lastFrame) {
				if (UseNewTreeView) {
					controller.SetStackFrame (DebuggingService.CurrentFrame);
				} else {
					tree.Frame = DebuggingService.CurrentFrame;
				}
			}

			lastFrame = DebuggingService.CurrentFrame;
		}

		public virtual void OnUpdateValues ()
		{
			needsUpdateValues = false;
		}

		protected virtual void OnFrameChanged (object s, EventArgs a)
		{
			if (Window != null && Window.ContentVisible) {
				OnUpdateFrame ();
			} else {
				needsUpdateFrame = true;
				needsUpdateValues = false;
			}
		}

		protected virtual void OnVariableChanged (object s, EventArgs e)
		{
			if (Window != null && Window.ContentVisible) {
				OnUpdateValues ();
			} else {
				needsUpdateValues = true;
			}
		}

		protected virtual void OnDebuggerPaused (object s, EventArgs a)
		{
		}

		protected virtual void OnDebuggerResumed (object s, EventArgs a)
		{
			if (UseNewTreeView) {
				if (!initialResume) {
					controller.ChangeCheckpoint ();
				}

				controller.ClearValues ();
			} else {
				if (!initialResume) {
					tree.ChangeCheckpoint ();
				}

				tree.ClearValues ();
			}

			initialResume = false;
		}

		protected virtual void OnDebuggerStopped (object s, EventArgs a)
		{
			if (DebuggingService.IsDebugging)
				return;

			if (UseNewTreeView) {
				controller.ResetChangeTracking ();
				controller.ClearAll ();
			} else {
				tree.ResetChangeTracking ();
				tree.ClearAll ();
			}

			lastFrame = null;
			initialResume = true;
		}

		protected virtual void OnEvaluationOptionsChanged (object s, EventArgs a)
		{
			if (!DebuggingService.IsRunning) {
				lastFrame = null;
				if (Window != null && Window.ContentVisible)
					OnUpdateFrame ();
				else
					needsUpdateFrame = true;
			}
		}
	}
}
