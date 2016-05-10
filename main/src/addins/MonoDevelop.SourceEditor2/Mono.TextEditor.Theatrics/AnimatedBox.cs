//
// AnimatedBox.cs
//
// Authors:
//   Scott Peterson <lunchtimemama@gmail.com>
//
// Copyright (C) 2008 Scott Peterson
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
using System.Collections.Generic;
using Gdk;
using Gtk;
using MonoDevelop.Components;

namespace Mono.TextEditor.Theatrics
{
	abstract class AnimatedBox : Container
	{
		private readonly Stage<AnimatedWidget> stage = new Stage<AnimatedWidget> ();
		private readonly LinkedList<AnimatedWidget> children = new LinkedList<AnimatedWidget> ();
		private readonly SingleActorStage border_stage = new SingleActorStage ();
		private readonly bool horizontal;

		private uint duration = 500;
		private Easing easing = Easing.Linear;
		private Blocking blocking = Blocking.Upstage;
		private int start_padding;
		private int end_padding;
		private int spacing;
		private int start_spacing;
		private int end_spacing;
		private int active_count;

		private int start_border;
		private int end_border;
		private double border_bias;
		private Easing border_easing;
		private AnimationState border_state;

		protected AnimatedBox (bool horizontal)
		{
			GtkWorkarounds.FixContainerLeak (this);
			
			WidgetFlags |= WidgetFlags.NoWindow;
			this.horizontal = horizontal;
			stage.ActorStep += OnActorStep;
			border_stage.Iteration += OnBorderIteration;
		}

		#region Private

		private double Percent {
			get { return border_stage.Actor.Percent * border_bias + (1.0 - border_bias); }
		}

		private bool OnActorStep (Actor<AnimatedWidget> actor)
		{
			switch (actor.Target.AnimationState) {
			case AnimationState.Coming:
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
					actor.Target.Unparent ();
					children.Remove (actor.Target.Node);
					return false;
				} else {
					actor.Target.Percent = 1.0 - actor.Percent;
				}
				break;
			}
			
			return true;
		}

		private void OnBorderIteration (object sender, EventArgs args)
		{
			if (border_stage.Actor == null) {
				if (border_state == AnimationState.Coming) {
					start_border = start_padding;
					end_border = end_padding;
				} else {
					start_border = end_border = 0;
				}
				border_state = AnimationState.Idle;
			} else {
				double percent = border_state == AnimationState.Coming ? Percent : 1.0 - Percent;
				start_border = Choreographer.PixelCompose (percent, start_padding, border_easing);
				end_border = Choreographer.PixelCompose (percent, end_padding, border_easing);
			}
			QueueResizeNoRedraw ();
		}

		private void OnWidgetDestroyed (object sender, EventArgs args)
		{
			RemoveCore ((AnimatedWidget)sender);
		}

		private void RecalculateSpacings ()
		{
			int skip_count = 0;
			
			foreach (AnimatedWidget animated_widget in Widgets) {
				animated_widget.QueueResizeNoRedraw ();
				if (skip_count > 1) {
					skip_count--;
					continue;
				}
				AnimatedWidget widget = animated_widget;
				
				if (skip_count == 0) {
					widget.StartPadding = start_spacing;
				} else {
					skip_count--;
				}
				widget.EndPadding = end_spacing;
				
				if (widget.Node.Previous == null) {
					while (true) {
						widget.StartPadding = 0;
						if (widget.AnimationState == AnimationState.Coming || widget.AnimationState == AnimationState.Idle || widget.Node.Next == null) {
							break;
						}
						widget.EndPadding = spacing;
						widget = widget.Node.Next.Value;
						skip_count++;
					}
				}
				
				if (widget.Node.Next == null) {
					while (true) {
						widget.EndPadding = 0;
						if (widget.AnimationState == AnimationState.Coming || widget.AnimationState == AnimationState.Idle || widget.Node.Previous == null) {
							break;
						}
						widget.StartPadding = spacing;
						widget = widget.Node.Previous.Value;
					}
				}
			}
		}

		#endregion

		#region Protected Overrides

		protected override void OnAdded (Widget widget)
		{
			PackStart (widget, duration, easing, blocking);
		}

		protected override void OnSizeRequested (ref Requisition requisition)
		{
			int width = 0;
			int height = 0;
			
			if (horizontal) {
				width = start_border + end_border;
			} else {
				height = start_border + end_border;
			}
			
			foreach (AnimatedWidget widget in Widgets) {
				Requisition req = widget.SizeRequest ();
				if (horizontal) {
					width += req.Width;
					height = System.Math.Max (height, req.Height);
				} else {
					width = System.Math.Max (width, req.Width);
					height += req.Height;
				}
			}
			
			requisition.Width = width;
			requisition.Height = height;
		}

		protected override void OnSizeAllocated (Rectangle allocation)
		{
			base.OnSizeAllocated (allocation);
			if (horizontal) {
				allocation.X += start_border;
				allocation.Y += (int)BorderWidth;
				allocation.Height -= (int)BorderWidth * 2;
			} else {
				allocation.X += (int)BorderWidth;
				allocation.Y += start_border;
				allocation.Width -= (int)BorderWidth * 2;
			}
			
			foreach (AnimatedWidget widget in Widgets) {
				if (horizontal) {
					allocation.Width = widget.Width;
					widget.SizeAllocate (allocation);
					allocation.X += allocation.Width;
				} else {
					allocation.Height = widget.Height;
					widget.SizeAllocate (allocation);
					allocation.Y += allocation.Height;
				}
			}
		}

		protected override void ForAll (bool include_internals, Callback callback)
		{
			foreach (AnimatedWidget child in Widgets) {
				callback (child);
			}
		}

		#endregion

		#region Public

		#region Properties

		public uint Duration {
			get { return duration; }
			set { duration = value; }
		}

		public Easing Easing {
			get { return easing; }
			set { easing = value; }
		}

		public Blocking Blocking {
			get { return blocking; }
			set { blocking = value; }
		}

		public int Spacing {
			get { return spacing; }
			set {
				spacing = value;
				double half = (double)value / 2.0;
				start_spacing = (int)System.Math.Ceiling (half);
				end_spacing = (int)System.Math.Floor (half);
			}
		}

		public int StartPadding {
			get { return start_padding - (int)BorderWidth; }
			set { start_padding = value + (int)BorderWidth; }
		}

		public int EndPadding {
			get { return end_padding - (int)BorderWidth; }
			set { end_padding = value + (int)BorderWidth; }
		}

		internal IEnumerable<AnimatedWidget> Widgets {
			get {
				foreach (AnimatedWidget child in children) {
					yield return child;
				}
			}
		}

		#endregion

		#region Pack Methods

		public void PackStart (Widget widget)
		{
			PackStart (widget, duration, easing, blocking);
		}

		public void PackStart (Widget widget, uint duration)
		{
			PackStart (widget, duration, easing, blocking);
		}

		public void PackStart (Widget widget, Easing easing)
		{
			PackStart (widget, duration, easing, blocking);
		}

		public void PackStart (Widget widget, uint duration, Easing easing)
		{
			PackStart (widget, duration, easing, blocking);
		}

		public void PackStart (Widget widget, Blocking blocking)
		{
			PackStart (widget, duration, easing, blocking);
		}

		public void PackStart (Widget widget, uint duration, Blocking blocking)
		{
			PackStart (widget, duration, easing, blocking);
		}

		public void PackStart (Widget widget, Easing easing, Blocking blocking)
		{
			PackStart (widget, duration, easing, blocking);
		}

		public void PackStart (Widget widget, uint duration, Easing easing, Blocking blocking)
		{
			Pack (widget, duration, easing, blocking, false);
		}

		public void PackEnd (Widget widget)
		{
			PackEnd (widget, duration, easing, blocking);
		}

		public void PackEnd (Widget widget, uint duration)
		{
			PackEnd (widget, duration, easing, blocking);
		}

		public void PackEnd (Widget widget, Easing easing)
		{
			PackEnd (widget, duration, easing, blocking);
		}

		public void PackEnd (Widget widget, uint duration, Easing easing)
		{
			PackEnd (widget, duration, easing, blocking);
		}

		public void PackEnd (Widget widget, Blocking blocking)
		{
			PackEnd (widget, duration, easing, blocking);
		}

		public void PackEnd (Widget widget, uint duration, Blocking blocking)
		{
			PackEnd (widget, duration, easing, blocking);
		}

		public void PackEnd (Widget widget, Easing easing, Blocking blocking)
		{
			PackEnd (widget, duration, easing, blocking);
		}

		public void PackEnd (Widget widget, uint duration, Easing easing, Blocking blocking)
		{
			Pack (widget, duration, easing, blocking, true);
		}

		private void Pack (Widget widget, uint duration, Easing easing, Blocking blocking, bool end)
		{
			if (widget == null) {
				throw new ArgumentNullException ("widget");
			}
			
			AnimatedWidget animated_widget = new AnimatedWidget (widget, duration, easing, blocking, horizontal);
			animated_widget.Parent = this;
			animated_widget.WidgetDestroyed += OnWidgetDestroyed;
			stage.Add (animated_widget, duration);
			animated_widget.Node = end ? children.AddLast (animated_widget) : children.AddFirst (animated_widget);
			
			RecalculateSpacings ();
			if (active_count == 0) {
				if (border_state == AnimationState.Going) {
					border_bias = Percent;
				} else {
					border_easing = easing;
					border_bias = 1.0;
				}
				border_state = AnimationState.Coming;
				border_stage.Reset ((uint)(duration * border_bias));
			}
			active_count++;
		}

		#endregion

		#region Remove Methods

		public new void Remove (Widget widget)
		{
			RemoveCore (widget, 0, 0, 0, false, false);
		}

		public void Remove (Widget widget, uint duration)
		{
			RemoveCore (widget, duration, 0, 0, false, false);
		}

		public void Remove (Widget widget, Easing easing)
		{
			RemoveCore (widget, 0, easing, 0, true, false);
		}

		public void Remove (Widget widget, uint duration, Easing easing)
		{
			RemoveCore (widget, duration, easing, 0, true, false);
		}

		public void Remove (Widget widget, Blocking blocking)
		{
			RemoveCore (widget, 0, 0, blocking, false, true);
		}

		public void Remove (Widget widget, uint duration, Blocking blocking)
		{
			RemoveCore (widget, duration, 0, blocking, false, true);
		}

		public void Remove (Widget widget, Easing easing, Blocking blocking)
		{
			RemoveCore (widget, 0, easing, blocking, true, true);
		}

		public void Remove (Widget widget, uint duration, Easing easing, Blocking blocking)
		{
			RemoveCore (widget, duration, easing, blocking, true, true);
		}

		private void RemoveCore (Widget widget, uint duration, Easing easing, Blocking blocking, bool use_easing, bool use_blocking)
		{
			if (widget == null) {
				throw new ArgumentNullException ("widget");
			}
			
			AnimatedWidget animated_widget = null;
			foreach (AnimatedWidget child in Widgets) {
				if (child.Widget == widget) {
					animated_widget = child;
					break;
				}
			}
			
			if (animated_widget == null) {
				throw new ArgumentException ("Cannot remove the specified widget because it has not been added to this container or it has already been removed.", "widget");
			}
			
			RemoveCore (animated_widget, duration, easing, blocking, use_easing, use_blocking);
			RecalculateSpacings ();
		}

		private void RemoveCore (AnimatedWidget widget)
		{
			RemoveCore (widget, widget.Duration, 0, 0, false, false);
		}

		private void RemoveCore (AnimatedWidget widget, uint duration, Easing easing, Blocking blocking, bool use_easing, bool use_blocking)
		{
			if (duration > 0) {
				widget.Duration = duration;
			}
			
			if (use_easing) {
				widget.Easing = easing;
			}
			
			if (use_blocking) {
				widget.Blocking = blocking;
			}
			
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
			
			active_count--;
			if (active_count == 0) {
				if (border_state == AnimationState.Coming) {
					border_bias = Percent;
				} else {
					border_easing = easing;
					border_bias = 1.0;
				}
				border_state = AnimationState.Going;
				border_stage.Reset ((uint)(duration * border_bias));
			}
		}

		public void RemoveAll ()
		{
			foreach (AnimatedWidget child in Widgets) {
				if (child.AnimationState != AnimationState.Going) {
					RemoveCore (child);
				}
			}
			RecalculateSpacings ();
		}

		#endregion

		public bool Contains (Widget widget)
		{
			foreach (AnimatedWidget child in Widgets) {
				if (child.AnimationState != AnimationState.Going && child.Widget == widget) {
					return true;
				}
			}
			return false;
		}
		
		#endregion
		
	}
}
