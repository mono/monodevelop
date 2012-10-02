//
// Tweener.cs
//
// Author:
//       Jason Smith <jason.smith@xamarin.com>
//
// Copyright (c) 2012 Xamarin Inc.
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
using System.Diagnostics;

namespace MonoDevelop.Components
{

	static class Easing
	{
		public static readonly Func<float, float> Linear = x => x;

		public static readonly Func<float, float> SinOut = x => (float)Math.Sin (x * Math.PI * 0.5f);
		public static readonly Func<float, float> SinIn = x => 1.0f - (float)Math.Cos (x * Math.PI * 0.5f);
		public static readonly Func<float, float> SinInOut = x => -(float)Math.Cos (Math.PI * x) / 2.0f + 0.5f;

		public static readonly Func<float, float> CubicIn = x => x * x * x;
		public static readonly Func<float, float> CubicOut = x => (float)Math.Pow (x - 1.0f, 3.0f) + 1.0f;
		public static readonly Func<float, float> CubicInOut = x => x < 0.5f ? (float)Math.Pow (x * 2.0f, 3.0f) / 2.0f :
																			   (float)(Math.Pow ((x-1)*2.0f, 3.0f) + 2.0f) / 2.0f;
	}

	static class Animation
	{
		class Info
		{
			public Func<float, float> Easing { get; set; }
			public uint Rate { get; set; }
			public uint Length { get; set; }
			public Gtk.Widget Owner { get; set; }
			public Action<float> callback;
			public Action<float> finished;
			public Tweener tweener;
		}

		static Dictionary<string, Info> animations;

		static Animation ()
		{
			animations = new Dictionary<string, Info> ();
		}

		public static Func<float, float> Interpolate (float start, float end = 1.0f, float reverseVal = 0.0f, bool reverse = false)
		{
			float target = (reverse ? reverseVal : end);
			return x => start + (target - start) * x;
		}

		public static void Animate (this Gtk.Widget self, string name, float start, float end, uint rate = 16, uint length = 250, 
		                            Func<float, float> easing = null, Action<float> callback = null, Action<float> finished = null)
		{
			self.Animate<float> (name, rate, length, easing, Interpolate (start, end), callback, finished);
		}

		public static void Animate (this Gtk.Widget self, string name, uint rate = 16, uint length = 250, 
		                               Func<float, float> easing = null, Action<float> callback = null, Action<float> finished = null)
		{
			self.Animate<float> (name, rate, length, easing, x => x, callback, finished);
		}

		public static void Animate<T> (this Gtk.Widget self, string name, uint rate = 16, uint length = 250, 
		                               Func<float, float> easing = null, Func<float, T> transform = null, Action<T> callback = null, Action<T> finished = null) 
		{
			if (transform == null)
				throw new ArgumentNullException ("transform");
			if (callback == null)
				throw new ArgumentNullException ("callback");
			if (self == null)
				throw new ArgumentNullException ("widget");

			self.AbortAnimation (name);
			name += self.GetHashCode ().ToString ();

			Action<float> step = f => callback (transform(f));
			Action<float> final = null;
			if (finished != null)
				final = f => finished (transform(f));

			var info = new Info {
				Rate = rate,
				Length = length,
				Easing = easing ?? Easing.Linear
			};

			Tweener tweener = new Tweener (info.Length, info.Rate);
			tweener.Easing = info.Easing;
			tweener.Handle = name;
			tweener.ValueUpdated += HandleTweenerUpdated;
			tweener.Finished += HandleTweenerFinished;

			info.tweener = tweener;
			info.callback = step;
			info.finished = final;
			info.Owner = self;

			animations[name] = info;
			tweener.Start ();
		}

		public static bool AbortAnimation (this Gtk.Widget self, string handle)
		{
			handle += self.GetHashCode ().ToString ();
			if (!animations.ContainsKey (handle))
				return false;

			Info info = animations[handle];
			info.tweener.ValueUpdated -= HandleTweenerUpdated;
			info.tweener.Finished -= HandleTweenerFinished;
			info.tweener.Stop ();

			animations.Remove (handle);
			return true;
		}

		public static bool AnimationIsRunning (this Gtk.Widget self, string handle)
		{
			handle += self.GetHashCode ().ToString ();
			return animations.ContainsKey (handle);
		}

		static void HandleTweenerUpdated (object o, EventArgs args)
		{
			Tweener tweener = o as Tweener;
			Info info = animations[tweener.Handle];

			info.callback (tweener.Value);
			info.Owner.QueueDraw ();
		}

		static void HandleTweenerFinished (object o, EventArgs args)
		{
			Tweener tweener = o as Tweener;
			Info info = animations[tweener.Handle];

			info.callback (tweener.Value);

			animations.Remove (tweener.Handle);
			tweener.ValueUpdated -= HandleTweenerUpdated;
			tweener.Finished -= HandleTweenerFinished;

			if (info.finished != null)
				info.finished (tweener.Value);
			info.Owner.QueueDraw ();
		}
	}

	class Tweener
	{
		public uint Length { get; private set; }
		public uint Rate { get; private set; }
		public float Value { get; private set; }
		public Func<float, float> Easing { get; set; }
		public bool Loop { get; set; }
		public string Handle { get; set; }

		public bool IsRunning {
			get { return runningTime.IsRunning; }
		}

		public event EventHandler ValueUpdated;
		public event EventHandler Finished;

		Stopwatch runningTime;
		uint timeoutHandle;

		public Tweener (uint length, uint rate)
		{
			Value = 0.0f;
			Length = length;
			Loop = false;
			Rate = rate;
			runningTime = new Stopwatch ();
			Easing = Components.Easing.Linear;
		}

		~Tweener ()
		{
			if (timeoutHandle > 0)
				GLib.Source.Remove (timeoutHandle);
		}

		public void Start ()
		{
			Pause ();

			runningTime.Start ();
			timeoutHandle = GLib.Timeout.Add (Rate, () => { 
				Value = Math.Min (1.0f, runningTime.ElapsedMilliseconds / (float) Length);
				if (ValueUpdated != null)
					ValueUpdated (this, EventArgs.Empty);

				if (Value >= 1.0f)
				{
					if (Loop) {
						Value = 0.0f;
						runningTime.Reset ();
						runningTime.Start ();
						return true;
					}

					runningTime.Stop ();
					runningTime.Reset ();
					timeoutHandle = 0;
					if (Finished != null)
						Finished (this, EventArgs.Empty);
					Value = 0.0f;
					return false;
				}
				return true;
			});
		}

		public void Stop ()
		{
			Pause ();
			runningTime.Reset ();
			Value = 1.0f;
			if (Finished != null)
				Finished (this, EventArgs.Empty);
			Value = 0.0f;
		}

		public void Reset ()
		{
			runningTime.Reset ();
			runningTime.Start ();
		}

		public void Pause ()
		{
			runningTime.Stop ();

			if (timeoutHandle > 0) {
				GLib.Source.Remove (timeoutHandle);
				timeoutHandle = 0;
			}
		}
	}
}
