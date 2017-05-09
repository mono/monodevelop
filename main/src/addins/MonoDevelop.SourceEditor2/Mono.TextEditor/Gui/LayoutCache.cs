//
// LayoutCache.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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
using MonoDevelop.Components;

namespace Mono.TextEditor
{
	/// <summary>
	/// Caches native pango layout objects.
	/// </summary>
	class LayoutCache : IDisposable
	{
		readonly MonoTextEditor widget;
		readonly Queue<LayoutProxy> layoutQueue = new Queue<LayoutProxy> ();
		bool isDisposed;

		public LayoutCache (MonoTextEditor widget)
		{
			if (widget == null)
				throw new ArgumentNullException ("widget");
			this.widget = widget;
		}

		public LayoutProxy RequestLayout ()
		{
			if (layoutQueue.Count == 0) {
				layoutQueue.Enqueue (new LayoutProxy (this, PangoUtil.CreateLayout (widget)));
			}
			return layoutQueue.Dequeue ();
		}

		#region IDisposable implementation
		public void Dispose ()
		{
			isDisposed = true;
			foreach (var proxy in layoutQueue) {
				proxy.DisposeNativeObject ();
			}
			layoutQueue.Clear ();
		}
		#endregion

		internal class LayoutProxy : IDisposable
		{
			readonly LayoutCache layoutCache;
			readonly Pango.Layout layout;
			int width = -1, height = -1, lineCount = -1;

			public LayoutProxy (LayoutCache layoutCache, Pango.Layout layout)
			{
				if (layoutCache == null)
					throw new ArgumentNullException ("layoutCache");
				if (layout == null)
					throw new ArgumentNullException ("layout");
				this.layoutCache = layoutCache;
				this.layout = layout;
			}

			internal void DisposeNativeObject ()
			{
				layout.Dispose ();
			}

			#region IDisposable implementation

			public void Dispose ()
			{
				var attributes = layout.Attributes;
				if (attributes != null)
					attributes.Dispose ();
				layout.Attributes = null;
				layout.Tabs = null;
				layout.Width = -1;
				layout.Alignment = Pango.Alignment.Left;
				if (layoutCache.isDisposed)
					DisposeNativeObject ();
				else
					layoutCache.layoutQueue.Enqueue (this);
			}

			#endregion

			public static implicit operator Pango.Layout (LayoutProxy proxy)
			{
				return proxy.layout;
			}

			public string Text {
				get {
					return layout.Text;
				}
			}

			public Pango.Alignment Alignment {
				get { return layout.Alignment; }
				set { layout.Alignment = value; }
			}

			public Pango.FontDescription FontDescription {
				get { return layout.FontDescription; }
				set { layout.FontDescription = value; }
			}

			public Pango.TabArray Tabs {
				get { return layout.Tabs; }
				set { layout.Tabs = value; }
			}

			public Pango.WrapMode Wrap {
				get { return layout.Wrap; }
				set { layout.Wrap = value; }
			}

			public int Width {
				get { return layout.Width; }
				set { layout.Width = value; }
			}

			public int LineCount {
				get { return lineCount < 0 ? lineCount = layout.LineCount : lineCount; }
			}

			public void SetText (string text)
			{
				this.width = this.height = lineCount = -1;
				layout.SetText (text); 
			}

			public Pango.Rectangle IndexToPos (int index_)
			{
				return layout.IndexToPos (index_);
			}

			public void IndexToLineX (int index_, bool trailing, out int line, out int x_pos)
			{
				layout.IndexToLineX (index_, trailing, out line, out x_pos); 
			}

			public Pango.LayoutLine GetLine (int line)
			{
				return layout.GetLine (line);
			}

			public void GetSize (out int width, out int height)
			{
				if (this.width >= 0) {
					width = this.width;
					height = this.height;
					return;
				}
				layout.GetSize (out width, out height);
				this.width = width;
				this.height = height;
			}

			public bool XyToIndex (int x, int y, out int index, out int trailing)
			{
				return layout.XyToIndex (x, y, out index, out trailing); 
			}

			public void GetCursorPos (int index_, out Pango.Rectangle strong_pos, out Pango.Rectangle weak_pos)
			{
				layout.GetCursorPos (index_, out strong_pos, out weak_pos); 
			}

			public void GetPixelSize (out int width, out int height)
			{
				if (this.width >= 0) {
					width = (int)(this.width / Pango.Scale.PangoScale);
					height = (int)(this.height / Pango.Scale.PangoScale);
					return;
				}

				layout.GetPixelSize (out width, out height);
				this.width = (int)(width * Pango.Scale.PangoScale);
				this.height = (int)(height * Pango.Scale.PangoScale);
			}

			public void GetExtents (out Pango.Rectangle ink_rect, out Pango.Rectangle logical_rect)
			{
				if (this.width >= 0) {
					ink_rect = logical_rect = new Pango.Rectangle {
						X = 0,
						Y = 0,
						Width = width,
						Height = height
					};
					return;
				}

				layout.GetExtents (out ink_rect, out logical_rect); 
			}
		}
	}
}
