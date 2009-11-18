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

namespace Mono.MHex.Rendering
{
	public abstract class Margin : IDisposable
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
		
		public abstract int Width {
			get;
		}
			
		public abstract int CalculateWidth (int bytesInRow);
		
		public bool IsVisible { get; set; }
		
		// set by the text editor
		public virtual int XOffset {
			get;
			internal set;
		}
		
		protected Gdk.Cursor cursor = null;
		
		public Gdk.Cursor MarginCursor {
			get {
				return cursor;
			}
		}
		
		protected Margin (HexEditor hexEditor)
		{
			this.Editor = hexEditor;
			IsVisible = true;
		}
		
		#region Layout caching
		protected class LayoutWrapper : IDisposable
		{
			public Pango.Layout Layout {
				get;
				private set;
			}
			
			public bool IsUncached {
				get;
				set;
			}
			
			Pango.AttrList attrList;
			List<Pango.Attribute> attributes = new List<Pango.Attribute> ();
			
			public void Add (Pango.Attribute attribute)
			{
				attributes.Add (attribute);
			}
			
			public LayoutWrapper (Pango.Layout layout)
			{
				this.Layout = layout;
				this.IsUncached = false;
			}
			
			public void SetAttributes ()
			{
				this.attrList = new Pango.AttrList ();
				attributes.ForEach (attr => attrList.Insert (attr));
				Layout.Attributes = attrList;
			}
			
			public void Dispose ()
			{
				if (attributes != null) {
					attributes.ForEach (attr => attr.Dispose ());
					attributes.Clear ();
					attributes = null;
				}
				if (attrList != null) {
					attrList.Dispose ();
					attrList = null;
				}

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
		
		protected static uint TranslateToUTF8Index (char[] charArray, uint textIndex, ref uint curIndex, ref uint byteIndex)
		{
			if (textIndex < curIndex) {
				byteIndex = (uint)Encoding.UTF8.GetByteCount (charArray, 0, (int)textIndex);
			} else {
				int count = System.Math.Min ((int)(textIndex - curIndex), charArray.Length - (int)curIndex);
				
				if (count > 0)
					byteIndex += (uint)Encoding.UTF8.GetByteCount (charArray, (int)curIndex, count);
			}
			curIndex = textIndex;
			return byteIndex;
		}
		#endregion
		
		
		Dictionary<Gdk.Color, Gdk.GC> gcDictionary = new Dictionary<Gdk.Color, Gdk.GC> ();
		protected Gdk.GC GetGC (Gdk.Color color)
		{
			Gdk.GC result;
			if (gcDictionary.TryGetValue (color, out result))
				return result;
			result = new Gdk.GC (Editor.GdkWindow);
			result.RgbFgColor = color;
			gcDictionary[color] = result;
			return result;
		}
		
		internal protected virtual void OptionsChanged ()
		{
		}
		
//		internal protected virtual void OptionsCh
		
		internal protected abstract void Draw (Gdk.Drawable drawable, Gdk.Rectangle area, long line, int x, int y);
		
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
		
		internal protected virtual void MouseHover (MarginMouseEventArgs args)
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
			if (cursor != null) {
				cursor.Dispose ();
				cursor = null;
			}
			PurgeLayoutCache ();
			PurgeGCs ();
		}

		internal protected void PurgeGCs ()
		{
				foreach (Gdk.GC gc in gcDictionary.Values) {
				gc.Dispose ();
			}
			gcDictionary.Clear ();
		}

		
		public event EventHandler<MarginMouseEventArgs> ButtonPressed;
		public event EventHandler<MarginMouseEventArgs> ButtonReleased;
		public event EventHandler<MarginMouseEventArgs> MouseMoved;
		public event EventHandler MouseLeave;
	}
	
	public class MarginMouseEventArgs : EventArgs
	{
		public int X {
			get;
			private set;
		}
		
		public int Y {
			get;
			private set;
		}
		
		public Gdk.EventType Type {
			get;
			private set;
		}
		
		public Gdk.ModifierType ModifierState {
			get;
			private set;
		}
		
		public int Button {
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
		
		public MarginMouseEventArgs (HexEditor editor, int button, int x, int y, Gdk.EventType type, Gdk.ModifierType modifierState)
		{
			this.Editor = editor;
			this.Button = button;
			this.X = x;
			this.Y = y;
			this.Type = type;
			this.ModifierState = modifierState;
		}
	}
}
