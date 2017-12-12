using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Xwt;

namespace Mono.TextEditor
{
	partial class TextArea
	{
		internal IList<MouseHoverEventData> _mouseHoverEvents = new List<MouseHoverEventData> ();

		internal Timer _mouseHoverTimer;
		internal int _millisecondsSinceMouseMove = 0;

		internal int? _lastHoverPosition = null;

		private void InitializeMouse()
		{
			if (_mouseHoverTimer != null)
				return;
			_mouseHoverTimer = new Timer ();
			_mouseHoverTimer.Elapsed += delegate {
				MonoDevelop.Core.Runtime.RunInMainThread (() => {
					this.OnHoverTimer ();
				});
			};
		}

		internal void OnHoverTimer ()
		{
			if (!_isClosed) {
				_millisecondsSinceMouseMove += (int)_mouseHoverTimer.Interval;

				if (Visible && _lastHoverPosition.HasValue)
					this.RaiseHoverEvents ();
			}
		}

		/// <summary>
		/// Event raised whenever the mouse has hovered over the same character
		/// for 150 ms.
		/// </summary>
		/// <remarks>No hover events will be generated when the mouse is not over text in the buffer.</remarks>
		public event EventHandler<MouseHoverEventArgs> MouseHover {
			add {
				lock (_mouseHoverEvents) {
					InitializeMouse ();
					if (_mouseHoverEvents.Count == 0) {
						MotionNotifyEvent += OnMouseMove;
						ButtonPressEvent += this.OnMouseDown;
					}

					_mouseHoverEvents.Add (new MouseHoverEventData (value));
				}
			}

			remove {
				lock (_mouseHoverEvents) {
					for (int i = _mouseHoverEvents.Count - 1; (i >= 0); --i) {
						if (_mouseHoverEvents[i].EventHandler == value) {
							_mouseHoverEvents.RemoveAt (i);

							if (_mouseHoverEvents.Count == 0) {
								MotionNotifyEvent -= this.OnMouseMove;
								ButtonPressEvent -= this.OnMouseDown;
							}

							break;
						}
					}
				}
			}
		}

		internal void OnMouseMove (object o, Gtk.MotionNotifyEventArgs args)
		{
			if (!_isClosed) {
				MonoDevelop.Core.Runtime.AssertMainThread ();
				var but1 = (args.Event.State & Gdk.ModifierType.Button1Mask) == Gdk.ModifierType.Button1Mask;
				var but2 = (args.Event.State & Gdk.ModifierType.Button2Mask) == Gdk.ModifierType.Button2Mask;
				var but3 = (args.Event.State & Gdk.ModifierType.Button3Mask) == Gdk.ModifierType.Button3Mask;
				if (!but1 && !but2 && !but3) {
					this.HandleMouseMove (new Point (args.Event.X, args.Event.Y));
				}
			}
		}

		void OnMouseDown (object o, Gtk.ButtonPressEventArgs args)
		{
			if (!_isClosed) {
				_mouseHoverTimer.Stop ();
			}
		}


		#region Mouse Related Helpers

		internal class MouseHoverEventData
		{
			public readonly MouseHoverAttribute Attribute;
			public readonly EventHandler<MouseHoverEventArgs> EventHandler;
			public bool Fired;

			public MouseHoverEventData (EventHandler<MouseHoverEventArgs> eventHandler)
			{
				this.Attribute = GetMouseHoverAttribute (eventHandler);
				this.EventHandler = eventHandler;
				this.Fired = false;
			}

			private static MouseHoverAttribute GetMouseHoverAttribute (EventHandler<MouseHoverEventArgs> client)
			{
				object[] attributes = client.Method.GetCustomAttributes (typeof (MouseHoverAttribute), false);
				foreach (object attribute in attributes) {
					MouseHoverAttribute mouseHoverAttribute = attribute as MouseHoverAttribute;
					if (mouseHoverAttribute != null) {
						return mouseHoverAttribute;
					}
				}

				return new MouseHoverAttribute (150);
			}
		}

		// internal for exposure to unit tests
		internal void RaiseHoverEvents ()
		{
			MonoDevelop.Core.Runtime.AssertMainThread ();

			//See if there are any unfired events that are ready to fire.
			MouseHoverEventData nextEvent = null;
			IList<MouseHoverEventData> eventsToFire = new List<MouseHoverEventData> ();
			lock (_mouseHoverEvents) {
				foreach (var eventData in _mouseHoverEvents) {
					if (!eventData.Fired) {
						if (eventData.Attribute.Delay <= _millisecondsSinceMouseMove) {
							eventsToFire.Add (eventData);
						}
						else if ((nextEvent == null) || (eventData.Attribute.Delay < nextEvent.Attribute.Delay))
							nextEvent = eventData;
					}
				}
			}

			if (eventsToFire.Count > 0) {
				MouseHoverEventArgs args = new MouseHoverEventArgs (this, _lastHoverPosition.Value,
																   _bufferGraph.CreateMappingPoint (new SnapshotPoint (Document.TextBuffer.CurrentSnapshot, _lastHoverPosition.Value), PointTrackingMode.Positive));
				foreach (var eventData in eventsToFire) {
					eventData.Fired = true;

					try {
						eventData.EventHandler (this, args);
					}
					catch (Exception e) {
						_factoryService.GuardedOperations.HandleException (eventData.EventHandler, e);
					}
				}
			}

			if (nextEvent == null) {
				//No more events to fire ... stop the timer.
				_mouseHoverTimer.Stop ();
			}
			else {
				//Set the timer interval to match the delay to the next event.
				int newDelay = Math.Max (50, (nextEvent.Attribute.Delay - _millisecondsSinceMouseMove));
				_mouseHoverTimer.Interval = newDelay;
			}
		}

		internal void HandleMouseMove (Point pt)
		{
			if (_mouseHoverEvents.Count > 0) {
				int? newPosition = null;

				if ((pt.X >= 0.0) && (pt.X < this.ViewportWidth) &&
						(pt.Y >= 0.0) && (pt.Y < this.ViewportHeight)) {
					double y = pt.Y + this.ViewportTop;

					var line = TextViewLines.GetTextViewLineContainingYCoordinate (y);
					if ((line != null) && (y >= line.TextTop) && (y <= line.TextBottom)) {
						double x = pt.X + this.ViewportLeft;
						newPosition = line.GetBufferPositionFromXCoordinate (x, true);
						if ((!newPosition.HasValue) && (line.LineBreakLength == 0) && line.IsLastTextViewLineForSnapshotLine) {
							//For purposes of hover events, pretend the last line in the buffer
							//actually is padded by the EndOfLineWidth (even though it is not).
							if ((line.TextRight <= x) && (x < line.TextRight + line.EndOfLineWidth))
								newPosition = line.End;
						}
					}
				}

				if (newPosition != _lastHoverPosition) {
					_lastHoverPosition = newPosition;

					//The mouse moved to a different character, reset the timer.
					_mouseHoverTimer.Stop ();

					//If the mouse is over a character, reset the events & restart the timer.
					if (newPosition.HasValue) {
						int delay = int.MaxValue;
						lock (_mouseHoverEvents) {
							foreach (var eventData in _mouseHoverEvents) {
								eventData.Fired = false;
								if (eventData.Attribute.Delay < delay)
									delay = eventData.Attribute.Delay;
							}
						}

						//In theory the last event could have been removed on a background thread after we checked the count.
						if (delay != int.MaxValue) {
							_millisecondsSinceMouseMove = 0;
							_mouseHoverTimer.Interval = Math.Max (50, delay);
							_mouseHoverTimer.Start ();
						}
					}
				}
			}
		}

		#endregion //Mouse Related Helpers
	}
}
