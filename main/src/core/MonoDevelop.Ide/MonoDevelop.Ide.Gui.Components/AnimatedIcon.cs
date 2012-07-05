//
// AnimatedIcon.cs
//
// Author:
//       Lluis Sanchez <lluis@xamarin.com>
//
// Copyright (c) 2012 Xamarin Inc
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
using Mono.Addins;
using System.IO;
using MonoDevelop.Ide.Extensions;
using MonoDevelop.Core;
using MonoDevelop.Components;
using System.Text;
using System.Linq;

namespace MonoDevelop.Ide.Gui.Components
{

	public class AnimatedIcon
	{
		/* An animation spec is a sequence of animation frames. Frames are separated using semicolons.
		 * Each frame can be an image (using a regular image spec), an effect, or a pause. For example:
		 * 
		 * res:build1.png;morph;res:build2.png;morph
		 * 
		 * Supported effects are:
		 * fade-out, fade-in, morph
		 * 
		 */

		string animationSpec;
		RuntimeAddin addin;
		Gtk.IconSize size;

		const int defaultPause = 200;
		List<Gdk.Pixbuf> images;
		List<int> pauses;

		static Dictionary<string,Type> animationItems = new Dictionary<string, Type> ();

		static AnimatedIcon ()
		{
			animationItems ["morph"] = typeof(MorphEffect);
			animationItems ["fade-in"] = typeof(FadeInEffect);
			animationItems ["fade-out"] = typeof(FadeOutEffect);
		}

		public AnimatedIcon (RuntimeAddin addin, string animationSpec, Gtk.IconSize size)
		{
			this.addin = addin;
			this.size = size;
			this.animationSpec = animationSpec;
			Parse (animationSpec);
		}

		void Parse (string animationSpec)
		{
			List<AnimationItem> parsedItems = new List<AnimationItem> ();
			string[] items = animationSpec.Split (';');
			AnimationItem last = null;

			foreach (var item in items) {
				int i = item.IndexOf (':');
				var tname = i != -1 ? item.Substring (0, i) : item;
				int pause;
				Type type;
				AnimationItem aitem = null;

				if (animationItems.TryGetValue (tname, out type)) {
					aitem = (AnimationItem) Activator.CreateInstance (type);
					aitem.Parse (item);
				}
				else if (int.TryParse (item, out pause)) {
					aitem = new PauseItem () { Pause = pause };
				}
				else {
					// It must be an image
					var id = ImageService.GetStockId (addin, item, size);
					var img = ImageService.GetPixbuf (id, size);
					if (img == null)
						continue;
					aitem = new ImageItem () { Image = img };
				}
				if (last != null)
					last.NextItem = aitem;
				aitem.PreviousItem = last;
				parsedItems.Add (aitem);
				last = aitem;
			}

			if (parsedItems.Count > 0) {
				// Close the chain
				parsedItems[0].PreviousItem = parsedItems [parsedItems.Count - 1];
				parsedItems [parsedItems.Count - 1].NextItem = parsedItems [0];
			}

			images = new List<Gdk.Pixbuf> ();
			pauses = new List<int> ();
			bool lastWasImage = false;

			foreach (var aitem in parsedItems) {
				foreach (var frame in aitem.GetFrames ()) {
					if (frame is Gdk.Pixbuf) {
						if (lastWasImage)
							pauses.Add (defaultPause);
						images.Add ((Gdk.Pixbuf)frame);
						lastWasImage = true;
					}
					else {
						if (!lastWasImage) {
							if (pauses.Count > 0)
								pauses [pauses.Count - 1] = pauses [pauses.Count - 1] + (int) frame;
							else {
								// Pause before any image. Add a dummy image
								images.Add (ImageService.GetPixbuf ("md-empty"));
								pauses.Add ((int) frame);
							}
						} else
							pauses.Add ((int)frame);
						lastWasImage = false;
					}
				}
			}
			if (pauses.Count < images.Count)
				pauses.Add (defaultPause);
		}

		public Gdk.Pixbuf FirstFrame {
			get {
				return images.Count > 0 ? images [0] : ImageService.GetPixbuf ("md-empty");
			}
		}

		public string AnimationSpec {
			get { return animationSpec; }
		}

		public IDisposable StartAnimation (Action<Gdk.Pixbuf> renderer)
		{
			int currentFrame = 0;
			return DispatchService.RunAnimation (delegate {
				renderer (images [currentFrame]);
				var res = pauses [currentFrame];
				currentFrame = (currentFrame + 1) % images.Count;
				return res;
			});
		}

		abstract class AnimationItem
		{
			List<object> frames;

			internal AnimationItem NextItem;
			internal AnimationItem PreviousItem;
			bool renderingFrames;

			public virtual void Parse (string spec)
			{
			}

			internal List<object> GetFrames ()
			{
				RenderFrames ();
				return frames;
			}

			public Gdk.Pixbuf PreviousFrame {
				get {
					PreviousItem.RenderFrames ();
					var last = (Gdk.Pixbuf) PreviousItem.frames.LastOrDefault (f => f is Gdk.Pixbuf);
					if (last != null)
						return last;
					else
						return PreviousItem.PreviousFrame;
				}
			}

			public Gdk.Pixbuf NextFrame {
				get {
					NextItem.RenderFrames ();
					var first = (Gdk.Pixbuf) NextItem.frames.FirstOrDefault (f => f is Gdk.Pixbuf);
					if (first != null)
						return first;
					else
						return NextItem.NextFrame;
				}
			}

			void RenderFrames ()
			{
				if (renderingFrames)
					throw new Exception ("Invalid animation sequence");
				if (frames == null) {
					renderingFrames = true;
					frames = new List<object> ();
					OnRenderFrames ();
					renderingFrames = false;
				}
			}
			
			public abstract void OnRenderFrames ();

			protected void AddImage (Gdk.Pixbuf image)
			{
				frames.Add (image);
			}

			protected void AddPause (int ms)
			{
				frames.Add (ms);
			}
		}

		class ImageItem: AnimationItem
		{
			public Gdk.Pixbuf Image { get; set; }

			public override void OnRenderFrames ()
			{
				AddImage (Image);
			}
		}

		class PauseItem: AnimationItem
		{
			public int Pause { get; set; }
			
			public override void OnRenderFrames ()
			{
				AddPause (Pause);
			}
		}
		
		class FadeOutEffect: AnimationItem
		{
			public override void OnRenderFrames ()
			{
				var icon = PreviousFrame;
				for (int n=0; n<10; n++) {
					AddImage (ImageService.MakeTransparent (icon, ((double)(9-n))/10.0));
					AddPause (60);
				}
			}
		}

		class FadeInEffect: AnimationItem
		{
			public override void OnRenderFrames ()
			{
				var icon = NextFrame;
				for (int n=0; n<10; n++) {
					AddImage (ImageService.MakeTransparent (icon, ((double)(n))/10.0));
					AddPause (60);
				}
			}
		}
		
		class MorphEffect: AnimationItem
		{
			public override void OnRenderFrames ()
			{
				var prev = PreviousFrame;
				var next = NextFrame;
				for (int n=0; n<10; n++) {
					var img1 = ImageService.MakeTransparent (next, ((double)(n))/10.0);
					var img2 = ImageService.MakeTransparent (prev, ((double)(9-n))/10.0);
					img1.Composite (img2,
					                0,  0,
					                img2.Width, img2.Width,
					                0, 0,
					                1, 1, Gdk.InterpType.Bilinear, 255); 
					AddImage (img2);
					AddPause (60);
				}
			}
		}
	}
}
