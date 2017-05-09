//
// Selection.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2016 Xamarin Inc. (http://xamarin.com)
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
using MonoDevelop.Core.Text;

namespace MonoDevelop.Ide.Editor
{
	public struct Selection : IEquatable<Selection>
	{
		public static readonly Selection Empty = new Selection (true);

		public bool IsEmpty {
			get {
				return anchor.IsEmpty;
			}
		}

		readonly DocumentLocation anchor;
		public DocumentLocation Anchor {
			get {
				return anchor;
			}
		}

		readonly DocumentLocation lead;
		public DocumentLocation Lead {
			get {
				return lead;
			}
		}


		public int MinLine {
			get {
				return System.Math.Min (Anchor.Line, Lead.Line);
			}
		}

		public int MaxLine {
			get {
				return System.Math.Max (Anchor.Line, Lead.Line);
			}
		}

		public DocumentLocation Start {
			get {
				return anchor < lead ? anchor : lead;
			}
		}

		public DocumentLocation End {
			get {
				return anchor < lead ? lead : anchor;
			}
		}

		readonly SelectionMode selectionMode;
		public SelectionMode SelectionMode {
			get {
				return selectionMode;
			}
		}

		public bool Contains (DocumentLocation loc)
		{
			return anchor <= loc && loc <= lead || lead < loc && loc < anchor;
		}

		public bool Contains (int line, int column)
		{
			return Contains (new DocumentLocation (line, column));
		}

		Selection (bool empty)
		{
			anchor = lead = DocumentLocation.Empty;
			selectionMode = SelectionMode.Normal;
		}

		public Selection (int anchorLine, int anchorColumn, int leadLine, int leadColumn, SelectionMode mode = SelectionMode.Normal) : this (new DocumentLocation (anchorLine, anchorColumn), new DocumentLocation (leadLine, leadColumn), mode)
		{
		}

		public Selection (DocumentLocation anchor, DocumentLocation lead, SelectionMode selectionMode = SelectionMode.Normal)
		{
			if (anchor.Line < DocumentLocation.MinLine || anchor.Column < DocumentLocation.MinColumn)
				throw new ArgumentOutOfRangeException ("anchor", anchor + " is out of range.");
			if (lead.Line < DocumentLocation.MinLine || lead.Column < DocumentLocation.MinColumn)
				throw new ArgumentOutOfRangeException ("lead", lead + " is out of range.");
			this.anchor = anchor;
			this.lead = lead;
			this.selectionMode = selectionMode;
		}

		public Selection WithLead (DocumentLocation newLead)
		{
			return new Selection (Anchor, newLead, SelectionMode);
		}

		public Selection WithAnchor (DocumentLocation newAnchor)
		{
			return new Selection (newAnchor, Lead, SelectionMode);
		}

		public Selection WithRange (DocumentLocation newAnchor, DocumentLocation newLead)
		{
			return new Selection (newAnchor, newLead, SelectionMode);
		}

		public Selection WithSelectionMode (SelectionMode newSelectionMode)
		{
			return new Selection (Anchor, Lead, newSelectionMode);
		}

		public override bool Equals (object obj)
		{
			if (!(obj is Selection))
				return false;
			return Equals ((Selection)obj);
		}

		public bool Equals (Selection other)
		{
			return Anchor == other.Anchor && Lead == other.Lead && SelectionMode == other.SelectionMode;
		}

		public bool IsSelected (DocumentLocation loc)
		{
			return anchor <= loc && loc <= lead || lead <= loc && loc <= anchor;
		}

		public bool IsSelected (int line, int column)
		{
			return IsSelected (new DocumentLocation (line, column));
		}

		public bool IsSelected (DocumentLocation start, DocumentLocation end)
		{
			return IsSelected (start) && IsSelected (end);
		}

		public bool IsSelected (int startLine, int startColumn, int endLine, int endColumn)
		{
			return IsSelected (new DocumentLocation (startLine, startColumn), new DocumentLocation (endLine, endColumn));
		}

		public override int GetHashCode ()
		{
			unchecked {
				return Anchor.GetHashCode () ^ Lead.GetHashCode ();
			}
		}

		public override string ToString ()
		{
			return string.Format ("[Selection: Anchor={0}, Lead={1}, MinLine={2}, MaxLine={3}, SelectionMode={4}]", Anchor, Lead, MinLine, MaxLine, SelectionMode);
		}

		internal bool ContainsLine (int lineNr)
		{
			return anchor.Line <= lineNr && lineNr <= lead.Line || lead.Line <= lineNr && lineNr <= anchor.Line;
		}
	}
}

