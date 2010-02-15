using System;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Mono.TextEditor.Highlighting;
using Mono.TextEditor.PopupWindow;
using Mono.TextEditor.Theatrics;

using Gdk;
using Gtk;

namespace Mono.TextEditor
{
	/// <summary>
	/// Is a container that contains a text editor as background and floating widgets over the text editor.
	/// </summary>
	public class TextEditorContainer : Gtk.Container
	{
		TextEditor textEditorWidget = new TextEditor ();
		public TextEditor TextEditorWidget {
			get { return this.textEditorWidget; }
		}
		
		public override ContainerChild this [Widget w] {
			get {
				foreach (EditorContainerChild info in containerChildren.ToArray ()) {
					if (info.Child == w || (info.Child is AnimatedWidget && ((AnimatedWidget)info.Child).Widget == w))
						return info;
				}
				return null;
			}
		}
		
		public TextEditorContainer (IntPtr raw) : base (raw)
		{
		}
		
		public TextEditorContainer (TextEditor textEditorWidget)
		{
			WidgetFlags |= WidgetFlags.NoWindow;
			this.textEditorWidget = textEditorWidget;
			AddTopLevelWidget (textEditorWidget, 0, 0);
			stage.ActorStep += OnActorStep;
			ShowAll ();
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
					info.X = x;
					info.Y = y;
					QueueResize ();
					break;
				}
			}
		}
		
		public void MoveTopLevelWidgetX (Gtk.Widget widget, int x)
		{
			foreach (EditorContainerChild info in containerChildren.ToArray ()) {
				if (info.Child == widget || (info.Child is AnimatedWidget && ((AnimatedWidget)info.Child).Widget == widget)) {
					info.X = x;
					QueueResize ();
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
		
		protected override void OnRealized ()
		{
			WidgetFlags |= WidgetFlags.Realized;
			WindowAttr attributes = new WindowAttr ();
			attributes.WindowType = Gdk.WindowType.Child;
			attributes.X = Allocation.X;
			attributes.Y = Allocation.Y;
			attributes.Width = Allocation.Width;
			attributes.Height = Allocation.Height;
			attributes.Wclass = WindowClass.InputOutput;
			attributes.Visual = this.Visual;
			attributes.Colormap = this.Colormap;
			attributes.EventMask = (int)(this.Events | Gdk.EventMask.ExposureMask);
			attributes.Mask = this.Events | Gdk.EventMask.ExposureMask;
//			attributes.Mask = EventMask;
			
			WindowAttributesType mask = WindowAttributesType.X | WindowAttributesType.Y | WindowAttributesType.Colormap | WindowAttributesType.Visual;
			this.GdkWindow = new Gdk.Window (ParentWindow, attributes, mask);
			this.GdkWindow.UserData = this.Raw;
			this.Style = Style.Attach (this.GdkWindow);
			this.WidgetFlags &= ~WidgetFlags.NoWindow;
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
		
		protected override void OnSizeRequested (ref Gtk.Requisition requisition)
		{
			base.OnSizeRequested (ref requisition);
			
			// Ignore the size of top levels. They are supposed to fit the available space
			foreach (EditorContainerChild tchild in containerChildren.ToArray ())
				tchild.Child.SizeRequest ();
		}

		
		protected override void ForAll (bool include_internals, Gtk.Callback callback)
		{
			foreach (EditorContainerChild child in containerChildren.ToArray ()) {
				callback (child.Child);
			}
		}
		
		protected override void OnSizeAllocated (Rectangle allocation)
		{
			base.OnSizeAllocated (allocation);
			if (this.GdkWindow != null) 
				this.GdkWindow.MoveResize (allocation);
			allocation = new Rectangle (0, 0, allocation.Width, allocation.Height);
			if (textEditorWidget.Allocation != allocation)
				textEditorWidget.SizeAllocate (allocation);
			foreach (EditorContainerChild child in containerChildren.ToArray ()) {
				if (child.Child == textEditorWidget)
					continue;
				Requisition req = child.Child.SizeRequest ();
				Rectangle childRectangle = new Gdk.Rectangle (allocation.X + (int)(child.FixedPosition ? child.X : child.X * textEditorWidget.Options.Zoom - textEditorWidget.HAdjustment.Value), 
				                                              allocation.Y + (int)(child.FixedPosition ? child.Y : child.Y * textEditorWidget.Options.Zoom - textEditorWidget.VAdjustment.Value), req.Width, req.Height);
			//	if (childRectangle != child.Child.Allocation)
				child.Child.SizeAllocate (childRectangle);
			}
		}
		
		#region Animated Widgets
		Stage<AnimatedWidget> stage = new Stage<AnimatedWidget> ();
		
/*		uint duration = 500;
		Easing easing = Easing.Linear;
		Blocking blocking = Blocking.Upstage;
		int start_padding;
		int end_padding;
		int spacing;
		int start_spacing;
		int end_spacing;*/
		
//		int start_border;
//		int end_border;
		
	/*	double Percent {
			get { return border_stage.Actor == null ? 0 : border_stage.Actor.Percent * border_bias + (1.0 - border_bias); }
		}
		*/
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
			
			duration = widget.Duration;
			easing = widget.Easing;
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
	
		protected override void OnSetScrollAdjustments (Adjustment hAdjustement, Adjustment vAdjustement)
		{
			if (hAdjustement != null) 
				hAdjustement.ValueChanged += HandleHAdjustementValueChanged;
			if (vAdjustement != null) 
				vAdjustement.ValueChanged += HandleHAdjustementValueChanged;
			textEditorWidget.SetScrollAdjustments (hAdjustement, vAdjustement);
		}
		
		void HandleHAdjustementValueChanged (object sender, EventArgs e)
		{
			if (this.containerChildren.Count > 0)
				QueueResize ();
		}
	}
}