// 
// TextEditorContainer.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
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
using System.Linq;
using Gdk;
using Gtk;
using Mono.TextEditor.Highlighting;
using Mono.TextEditor.Theatrics;


namespace Mono.TextEditor
{
	/// <summary>
	/// Is a container that contains a text editor as background and floating widgets over the text editor.
	/// </summary>
	public class TextEditorContainer : Container
	{
		TextEditor textEditorWidget;
		
		public TextEditor TextEditorWidget {
			get {
				if (textEditorWidget == null)
					textEditorWidget = new TextEditor ();
				return this.textEditorWidget;
			}
		}
		
		public override ContainerChild this [Widget w] {
			get {
				return containerChildren.FirstOrDefault (info => info.Child == w || (info.Child is AnimatedWidget && ((AnimatedWidget)info.Child).Widget == w));
			}
		}
		
		public TextEditorContainer (TextEditor textEditorWidget)
		{
			GtkWorkarounds.FixContainerLeak (this);
			
			this.textEditorWidget = textEditorWidget;
			AddTopLevelWidget (textEditorWidget, 0, 0);
			stage.ActorStep += OnActorStep;
			ShowAll ();
			
			// bug on mac: search widget gets overdrawn in the scroll event.
			if (Platform.IsMac) {
				textEditorWidget.VScroll += delegate {
					for (int i = 1; i < containerChildren.Count; i++) {
						containerChildren[i].Child.QueueDraw ();
					}
				};
				textEditorWidget.HScroll += delegate {
					for (int i = 1; i < containerChildren.Count; i++) {
						containerChildren[i].Child.QueueDraw ();
					}
				};
			}
		}
		
		public class EditorContainerChild : Container.ContainerChild
		{
			public int X { get; set; }
			public int Y { get; set; }
			public bool FixedPosition { get; set; }
			public EditorContainerChild (Container parent, Widget child) : base (parent, child)
			{
			}
		}
		
		public override GLib.GType ChildType ()
		{
			return Gtk.Widget.GType;
		}
		
		List<EditorContainerChild> containerChildren = new List<EditorContainerChild> ();
		
		public void AddTopLevelWidget (Gtk.Widget widget, int x, int y)
		{
			widget.Parent = this;
			EditorContainerChild info = new EditorContainerChild (this, widget);
			info.X = x;
			info.Y = y;
			containerChildren.Add (info);
		}
		
		public void MoveTopLevelWidget (Gtk.Widget widget, int x, int y)
		{
			foreach (EditorContainerChild info in containerChildren.ToArray ()) {
				if (info.Child == widget || (info.Child is AnimatedWidget && ((AnimatedWidget)info.Child).Widget == widget)) {
					if (info.X == x && info.Y == y)
						break;
					info.X = x;
					info.Y = y;
					if (widget.Visible)
						ResizeChild (Allocation, info);
					break;
				}
			}
		}
		
		public void MoveToTop (Gtk.Widget widget)
		{
			EditorContainerChild editorContainerChild = containerChildren.FirstOrDefault (c => c.Child == widget);
			if (editorContainerChild == null)
				throw new Exception ("child " + widget + " not found.");
			List<EditorContainerChild> newChilds = new List<EditorContainerChild> (containerChildren.Where (child => child != editorContainerChild));
			newChilds.Add (editorContainerChild);
			this.containerChildren = newChilds;
			widget.GdkWindow.Raise ();
		}
		
		protected override void OnAdded (Widget widget)
		{
			AddTopLevelWidget (widget, 0, 0);
		}
		
		protected override void OnRemoved (Widget widget)
		{
			foreach (EditorContainerChild info in containerChildren.ToArray ()) {
				if (info.Child == widget) {
					widget.Unparent ();
					containerChildren.Remove (info);
					break;
				}
			}
		}
		
		protected override void ForAll (bool include_internals, Gtk.Callback callback)
		{
			containerChildren.ForEach (child => callback (child.Child));
		}
		
		protected override void OnMapped ()
		{
			WidgetFlags |= WidgetFlags.Mapped;
			// Note: SourceEditorWidget.ShowAutoSaveWarning() might have set TextEditor.Visible to false,
			// in which case we want to not map it (would cause a gtk+ critical error).
			containerChildren.ForEach (child => { if (child.Child.Visible) child.Child.Map (); });
			GdkWindow.Show ();
		}
		
		protected override void OnUnmapped ()
		{
			WidgetFlags &= ~WidgetFlags.Mapped;
			
			// We hide the window first so that the user doesn't see widgets disappearing one by one.
			GdkWindow.Hide ();
			
			containerChildren.ForEach (child => child.Child.Unmap ());
		}
		
		protected override void OnRealized ()
		{
			WidgetFlags |= WidgetFlags.Realized;
			WindowAttr attributes = new WindowAttr () {
				WindowType = Gdk.WindowType.Child,
				X = Allocation.X,
				Y = Allocation.Y,
				Width = Allocation.Width,
				Height = Allocation.Height,
				Wclass = WindowClass.InputOutput,
				Visual = this.Visual,
				Colormap = this.Colormap,
				EventMask = (int)(this.Events | Gdk.EventMask.ExposureMask),
				Mask = this.Events | Gdk.EventMask.ExposureMask,
			};
			
			WindowAttributesType mask = WindowAttributesType.X | WindowAttributesType.Y | WindowAttributesType.Colormap | WindowAttributesType.Visual;
			GdkWindow = new Gdk.Window (ParentWindow, attributes, mask);
			GdkWindow.UserData = Raw;
			Style = Style.Attach (GdkWindow);
		}
		
		protected override void OnUnrealized ()
		{
			WidgetFlags &= ~WidgetFlags.Realized;
			GdkWindow.Dispose ();
			base.OnUnrealized ();
		}
		
		protected override void OnSizeAllocated (Rectangle allocation)
		{
			base.OnSizeAllocated (allocation);
			if (this.GdkWindow != null)
				this.GdkWindow.MoveResize (allocation);
			allocation = new Rectangle (0, 0, allocation.Width, allocation.Height);
			if (textEditorWidget.Allocation != allocation)
				textEditorWidget.SizeAllocate (allocation);
			SetChildrenPositions (allocation);
		}

		void ResizeChild (Rectangle allocation, EditorContainerChild child)
		{
			Requisition req = child.Child.SizeRequest ();
			var childRectangle = new Gdk.Rectangle (child.X, child.Y, req.Width, req.Height);
			if (!child.FixedPosition) {
				double zoom = textEditorWidget.Options.Zoom;
				childRectangle.X = (int)(child.X * zoom - textEditorWidget.HAdjustment.Value);
				childRectangle.Y = (int)(child.Y * zoom - textEditorWidget.VAdjustment.Value);
			}
//			childRectangle.X += allocation.X;
//			childRectangle.Y += allocation.Y;
			child.Child.SizeAllocate (childRectangle);
		}

		void SetChildrenPositions (Rectangle allocation)
		{
			foreach (EditorContainerChild child in containerChildren.ToArray ()) {
				if (child.Child == textEditorWidget)
					continue;
				ResizeChild (allocation, child);
			}
		}
		
		#region Animated Widgets
		Stage<AnimatedWidget> stage = new Stage<AnimatedWidget> ();

		bool OnActorStep (Actor<AnimatedWidget> actor)
		{
			switch (actor.Target.AnimationState) {
			case AnimationState.Coming:
				actor.Target.QueueDraw ();
				actor.Target.Percent = actor.Percent;
				if (actor.Expired) {
					actor.Target.AnimationState = AnimationState.Idle;
					return false;
				}
				break;
			case AnimationState.IntendingToGo:
				actor.Target.AnimationState = AnimationState.Going;
				actor.Target.Bias = actor.Percent;
				actor.Reset ((uint)(actor.Target.Duration * actor.Percent));
				break;
			case AnimationState.Going:
				if (actor.Expired) {
					this.Remove (actor.Target);
					return false;
				}
				actor.Target.Percent = 1.0 - actor.Percent;
				break;
			}
			return true;
		}
		
		void OnWidgetDestroyed (object sender, EventArgs args)
		{
			RemoveCore ((AnimatedWidget)sender);
		}
		
		void RemoveCore (AnimatedWidget widget)
		{
			RemoveCore (widget, widget.Duration, 0, 0, false, false);
		}

		void RemoveCore (AnimatedWidget widget, uint duration, Easing easing, Blocking blocking, bool use_easing, bool use_blocking)
		{
			if (duration > 0)
				widget.Duration = duration;
			
			if (use_easing)
				widget.Easing = easing;
			
			if (use_blocking)
				widget.Blocking = blocking;
			
			if (widget.AnimationState == AnimationState.Coming) {
				widget.AnimationState = AnimationState.IntendingToGo;
			} else {
				if (widget.Easing == Easing.QuadraticIn) {
					widget.Easing = Easing.QuadraticOut;
				} else if (widget.Easing == Easing.QuadraticOut) {
					widget.Easing = Easing.QuadraticIn;
				} else if (widget.Easing == Easing.ExponentialIn) {
					widget.Easing = Easing.ExponentialOut;
				} else if (widget.Easing == Easing.ExponentialOut) {
					widget.Easing = Easing.ExponentialIn;
				}
				widget.AnimationState = AnimationState.Going;
				stage.Add (widget, widget.Duration);
			}
		}

		public void AddAnimatedWidget (Widget widget, uint duration, Easing easing, Blocking blocking, int x, int y)
		{
			AnimatedWidget animated_widget = new AnimatedWidget (widget, duration, easing, blocking, false);
			animated_widget.Parent = this;
			animated_widget.WidgetDestroyed += OnWidgetDestroyed;
			stage.Add (animated_widget, duration);
			animated_widget.StartPadding = 0;
			animated_widget.EndPadding = widget.Allocation.Height;
//			animated_widget.Node = animated_widget;
			
			EditorContainerChild info = new EditorContainerChild (this, animated_widget);
			info.X = x;
			info.Y = y;
			info.FixedPosition = true;
			containerChildren.Add (info);

//			RecalculateSpacings ();
		}
		#endregion
	
		Adjustment editorHAdjustement;
		Adjustment editorVAdjustement;
		
		protected override void OnSetScrollAdjustments (Adjustment hAdjustement, Adjustment vAdjustement)
		{
			if (editorHAdjustement != null)
				editorHAdjustement.ValueChanged -= HandleHAdjustementValueChanged;
			if (editorVAdjustement != null)
				editorVAdjustement.ValueChanged -= HandleHAdjustementValueChanged;
			
			if (hAdjustement != null)
				hAdjustement.ValueChanged += HandleHAdjustementValueChanged;
			if (vAdjustement != null)
				vAdjustement.ValueChanged += HandleHAdjustementValueChanged;
			
			editorHAdjustement = hAdjustement;
			editorVAdjustement = vAdjustement;
			textEditorWidget.SetScrollAdjustments (hAdjustement, vAdjustement);
		}
		
		void HandleHAdjustementValueChanged (object sender, EventArgs e)
		{
			var alloc = this.Allocation;
			alloc.X = alloc.Y = 0;
			SetChildrenPositions (alloc);
		}
		
		protected override void OnDestroyed ()
		{
			if (editorHAdjustement != null)
				editorHAdjustement.ValueChanged -= HandleHAdjustementValueChanged;
			if (editorVAdjustement != null)
				editorVAdjustement.ValueChanged -= HandleHAdjustementValueChanged;
			base.OnDestroyed ();
		}
	}
}
