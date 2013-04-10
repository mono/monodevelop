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

namespace MonoDevelop.Debugger
{
	public class ObjectValuePad: IPadContent
	{
		protected ObjectValueTreeView tree;
		ScrolledWindow scrolled;
		bool needsUpdate;
		IPadWindow container;
		bool initialResume;
		StackFrame lastFrame;
		PadFontChanger fontChanger;
		
		public Gtk.Widget Control {
			get {
				return scrolled;
			}
		}
		
		public ObjectValuePad()
		{
			scrolled = new ScrolledWindow ();
			scrolled.HscrollbarPolicy = PolicyType.Automatic;
			scrolled.VscrollbarPolicy = PolicyType.Automatic;
			
			tree = new ObjectValueTreeView ();
			
			fontChanger = new PadFontChanger (tree, tree.SetCustomFont, tree.QueueResize);
			
			tree.AllowEditing = true;
			tree.AllowAdding = false;
			tree.HeadersVisible = true;
			tree.RulesHint = true;
			scrolled.Add (tree);
			scrolled.ShowAll ();
			
			DebuggingService.CurrentFrameChanged += OnFrameChanged;
			DebuggingService.PausedEvent += OnDebuggerPaused;
			DebuggingService.ResumedEvent += OnDebuggerResumed;
			DebuggingService.StoppedEvent += OnDebuggerStopped;
			DebuggingService.EvaluationOptionsChanged += OnEvaluationOptionsChanged;

			needsUpdate = true;
			initialResume = true;
		}

		public void Dispose ()
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
		}

		public void Initialize (IPadWindow container)
		{
			this.container = container;
			container.PadContentShown += delegate {
				if (needsUpdate)
					OnUpdateList ();
			};
		}

		public void RedrawContent ()
		{
		}
		
		public virtual void OnUpdateList ()
		{
			needsUpdate = false;
			if (DebuggingService.CurrentFrame != lastFrame)
				tree.Frame = DebuggingService.CurrentFrame;
			lastFrame = DebuggingService.CurrentFrame;
		}
		
		protected virtual void OnFrameChanged (object s, EventArgs a)
		{
			if (container != null && container.ContentVisible)
				OnUpdateList ();
			else
				needsUpdate = true;
		}
		
		protected virtual void OnDebuggerPaused (object s, EventArgs a)
		{
			tree.Sensitive = true;
		}
		
		protected virtual void OnDebuggerResumed (object s, EventArgs a)
		{
			if (!initialResume)
				tree.ChangeCheckpoint ();

			initialResume = false;
			tree.Sensitive = false;
		}
		
		protected virtual void OnDebuggerStopped (object s, EventArgs a)
		{
			tree.ResetChangeTracking ();
			tree.Sensitive = false;
			initialResume = true;
		}
		
		protected virtual void OnEvaluationOptionsChanged (object s, EventArgs a)
		{
			if (!DebuggingService.IsRunning) {
				lastFrame = null;
				if (container != null && container.ContentVisible)
					OnUpdateList ();
				else
					needsUpdate = true;
			}
		}
	}
}
