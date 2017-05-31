// 
// Margin.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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
using Mono.MHex.Data;
using System.Text;
using Xwt.Drawing;
using Xwt;

namespace Mono.MHex.Rendering
{
	abstract class Margin : IDisposable
	{
		protected HexEditor Editor {
			get;
			private set;
		}
		
		protected Mono.MHex.Data.HexEditorData Data {
			get {
				return Editor.HexEditorData;
			}
		}
		
		protected HexEditorStyle Style {
			get {
				return Editor.HexEditorStyle;
			}
		}
		
		protected Caret Caret {
			get {
				return Data.Caret;
			}
		}
		
		protected int BytesInRow {
			get {
				return Editor.BytesInRow;
			}
		}
		
		public abstract double Width {
			get;
		}
			
		public abstract double CalculateWidth (int bytesInRow);
		
		public bool IsVisible { get; set; }
		
		// set by the text editor
		public virtual double XOffset {
			get;
			internal set;
		}

		/*
		protected Gdk.Cursor cursor = null;
		
		public Gdk.Cursor MarginCursor {
			get {
				return cursor;
			}
		}*/
		
		protected Margin (HexEditor hexEditor)
		{
			this.Editor = hexEditor;
			IsVisible = true;
		}
		
		#region Layout caching
		protected class LayoutWrapper : IDisposable
		{
			public TextLayout Layout {
				get;
				private set;
			}
			
			public bool IsUncached {
				get;
				set;
			}

			public LayoutWrapper (TextLayout layout)
			{
				this.Layout = layout;
				this.IsUncached = false;
			}
			
			public void Dispose ()
			{
				if (Layout != null) {
					Layout.Dispose ();
					Layout = null;
				}
			}
		}
		
		Dictionary<long, LayoutWrapper> layoutCache = new Dictionary<long, LayoutWrapper> ();
		protected virtual LayoutWrapper RenderLine (long line)
		{
			return null;
		}
		
		internal protected void PurgeLayoutCache ()
		{
			foreach (LayoutWrapper layout in layoutCache.Values) {
				layout.Dispose ();
			}
			layoutCache.Clear ();
		}
		
		
		internal protected void PurgeLayoutCache (long line)
		{
			layoutCache.Remove (line);
		}
		
		public void SetVisibleWindow (long startLine, long endLine)
		{
			List<long> toRemove = new List<long> ();
			foreach (long lineNumber in layoutCache.Keys) {
				if (lineNumber < startLine || lineNumber > endLine) 
					toRemove.Add (lineNumber);
			}
			toRemove.ForEach (line => layoutCache.Remove (line));
		}
		
		protected LayoutWrapper GetLayout (long line)
		{
			LayoutWrapper result;
			if (layoutCache.TryGetValue (line, out result)) {
				return result;
			}
			result = RenderLine (line);
			if (!result.IsUncached)
				layoutCache[line] = result;
			return result;
		}
		
		protected delegate void HandleSelectionDelegate (long start, long end);
		
		protected static void HandleSelection (long selectionStart, long selectionEnd, long startOffset, long endOffset, HandleSelectionDelegate handleNotSelected, HandleSelectionDelegate handleSelected)
		{
			if (startOffset >= selectionStart && endOffset <= selectionEnd) {
				if (handleSelected != null)
					handleSelected (startOffset, endOffset);
			} else if (startOffset >= selectionStart && startOffset < selectionEnd && endOffset > selectionEnd) {
				if (handleSelected != null)
					handleSelected (startOffset, selectionEnd);
				if (handleNotSelected != null)
					handleNotSelected (selectionEnd, endOffset);
			} else if (startOffset < selectionStart && endOffset > selectionStart && endOffset <= selectionEnd) {
				if (handleNotSelected != null)
					handleNotSelected (startOffset, selectionStart);
				if (handleSelected != null)
					handleSelected (selectionStart, endOffset);
			} else if (startOffset < selectionStart && endOffset > selectionEnd) {
				if (handleNotSelected != null)
					handleNotSelected (startOffset, selectionStart);
				if (handleSelected != null)
					handleSelected (selectionStart, selectionEnd);
				if (handleNotSelected != null)
					handleNotSelected (selectionEnd, endOffset);
			} else {
				if (handleNotSelected != null)
					handleNotSelected (startOffset, endOffset);
			}
		}

		protected delegate void HandleSelectionArgsDelegate<T> (long start, long end, Margin margin, T arg);
		protected struct LayoutOffsetPair
		{
			public readonly long StartOffset;
			public readonly TextLayout Layout;

			public LayoutOffsetPair (long startOffset, TextLayout layout)
			{
				StartOffset = startOffset;
				Layout = layout;
			}
		}
		protected static void HandleSelection<T> (long selectionStart, long selectionEnd, long startOffset, long endOffset, Margin margin, T args, HandleSelectionArgsDelegate<T> handleNotSelected, HandleSelectionArgsDelegate<T> handleSelected)
		{
			if (startOffset >= selectionStart && endOffset <= selectionEnd) {
				if (handleSelected != null)
					handleSelected (startOffset, endOffset, margin, args);
			} else if (startOffset >= selectionStart && startOffset < selectionEnd && endOffset > selectionEnd) {
				if (handleSelected != null)
					handleSelected (startOffset, selectionEnd, margin, args);
				if (handleNotSelected != null)
					handleNotSelected (selectionEnd, endOffset, margin, args);
			} else if (startOffset < selectionStart && endOffset > selectionStart && endOffset <= selectionEnd) {
				if (handleNotSelected != null)
					handleNotSelected (startOffset, selectionStart, margin, args);
				if (handleSelected != null)
					handleSelected (selectionStart, endOffset, margin, args);
			} else if (startOffset < selectionStart && endOffset > selectionEnd) {
				if (handleNotSelected != null)
					handleNotSelected (startOffset, selectionStart, margin, args);
				if (handleSelected != null)
					handleSelected (selectionStart, selectionEnd, margin, args);
				if (handleNotSelected != null)
					handleNotSelected (selectionEnd, endOffset, margin, args);
			} else {
				if (handleNotSelected != null)
					handleNotSelected (startOffset, endOffset, margin, args);
			}
		}
		
		protected static uint TranslateToUTF8Index (string text, uint textIndex, ref uint curIndex, ref uint byteIndex)
		{
			if (textIndex < curIndex) {
				unsafe {
					fixed (char *p = text)
						byteIndex = (uint)Encoding.UTF8.GetByteCount (p, (int)textIndex);
				}
			} else {
				int count = System.Math.Min ((int)(textIndex - curIndex), text.Length - (int)curIndex);
				
				if (count > 0) {
					unsafe {
						fixed (char *p = text)
							byteIndex += (uint)Encoding.UTF8.GetByteCount (p + curIndex, count);
					}
				}
			}
			curIndex = textIndex;
			return byteIndex;
		}
		#endregion
		
		internal protected virtual void OptionsChanged ()
		{
		}
		
//		internal protected virtual void OptionsCh
		
		internal protected abstract void Draw (Context ctx, Rectangle area, long line, double x, double y);
		
		internal protected virtual void MousePressed (MarginMouseEventArgs args)
		{
			if (ButtonPressed != null)
				ButtonPressed (this, args);
		}
		
		internal protected virtual void MouseReleased (MarginMouseEventArgs args)
		{
			if (ButtonReleased != null)
				ButtonReleased (this, args);
		}
		
		internal protected virtual void MouseHover (MarginMouseMovedEventArgs args)
		{
			if (MouseMoved != null)
				MouseMoved (this, args);
		}
		
		internal protected virtual void MouseLeft ()
		{
			if (MouseLeave != null)
				MouseLeave (this, EventArgs.Empty);
		}
		
		public virtual void Dispose ()
		{
			PurgeLayoutCache ();
		}

		public event EventHandler<MarginMouseEventArgs> ButtonPressed;
		public event EventHandler<MarginMouseEventArgs> ButtonReleased;
		public event EventHandler<MarginMouseMovedEventArgs> MouseMoved;
		public event EventHandler MouseLeave;
	}
	
	class MarginMouseEventArgs : EventArgs
	{
		Margin margin;

		public double X {
			get {
				return Args.X - margin.XOffset;
			}
		}

		public double Y {
			get {
				return Args.Y;
			}
		}

		public PointerButton Button {
			get {
				return Args.Button;
			}
		}

		public ButtonEventArgs Args {
			get;
			private set;
		}

		public HexEditor Editor {
			get;
			private set;
		}

		public long Line {
			get {
				return (long)(Y + Editor.HexEditorData.VAdjustment.Value) / Editor.LineHeight;
			}
		}

		public MarginMouseEventArgs (HexEditor editor, Margin margin, ButtonEventArgs args)
		{
			this.Editor = editor;
			this.margin = margin;
			this.Args = args;
		}
	}

	class MarginMouseMovedEventArgs : EventArgs
	{
		public double X {
			get {
				return Args.X - margin.XOffset;
			}
		}

		public double Y {
			get {
				return Args.Y;
			}
		}

		public MouseMovedEventArgs Args {
			get;
			private set;
		}

		public HexEditor Editor {
			get;
			private set;
		}

		public long Line {
			get {
				return (long)(Y + Editor.HexEditorData.VAdjustment.Value) / Editor.LineHeight;
			}
		}

		Margin margin;

		public MarginMouseMovedEventArgs (HexEditor editor, Margin margin, MouseMovedEventArgs args)
		{
			this.margin = margin;
			this.Editor = editor;
			this.Args = args;
		}
	}


}
