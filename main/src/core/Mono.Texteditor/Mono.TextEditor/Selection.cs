using System;
using Mono.TextEditor.Highlighting;

namespace Mono.TextEditor
{
	public struct Selection
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
			return  Contains (new DocumentLocation (line, column));
		}

		Selection(bool empty)
		{
			anchor = lead = DocumentLocation.Empty;
			selectionMode = SelectionMode.Normal;
		}

		public Selection (int anchorLine, int anchorColumn, int leadLine, int leadColumn, SelectionMode mode = SelectionMode.Normal) : this(new DocumentLocation (anchorLine, anchorColumn), new DocumentLocation (leadLine, leadColumn), mode)
		{
		}
		
		public Selection (DocumentLocation anchor, DocumentLocation lead, SelectionMode selectionMode = SelectionMode.Normal)
		{
			if (anchor.Line < DocumentLocation.MinLine || anchor.Column < DocumentLocation.MinColumn)
				throw new ArgumentOutOfRangeException ("anchor", anchor + " is out of range.");
			if (lead.Line < DocumentLocation.MinLine || lead.Column < DocumentLocation.MinColumn)
				throw new ArgumentOutOfRangeException ("lead", lead + " is out of range.");
			this.anchor        = anchor;
			this.lead          = lead;
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

		public TextSegment GetSelectionRange (TextEditorData data)
		{
			int anchorOffset = GetAnchorOffset (data);
			int leadOffset = GetLeadOffset (data);
			return new TextSegment (System.Math.Min (anchorOffset, leadOffset), System.Math.Abs (anchorOffset - leadOffset));
		}
		
		// for markup syntax mode the syntax highlighting information need to be taken into account
		// when calculating the selection offsets.
		int PosToOffset (TextEditorData data, DocumentLocation loc) 
		{
			DocumentLine line = data.GetLine (loc.Line);
			if (line == null)
				return 0;
			var startChunk = data.GetChunks (line, line.Offset, line.LengthIncludingDelimiter);
			int col = 1;
			foreach (Chunk chunk in startChunk) {
				if (col <= loc.Column && loc.Column < col + chunk.Length)
					return chunk.Offset - col + loc.Column;
				col += chunk.Length;
			}
			return line.Offset + line.Length;
		}
		
		public int GetAnchorOffset (TextEditorData data)
		{
			return data.Document.LocationToOffset (Anchor);
		}
		
		public int GetLeadOffset (TextEditorData data)
		{
			return data.Document.LocationToOffset (Lead);
		}
		
		public override bool Equals (object obj)
		{
			if (obj == null)
				return false;
			if (obj.GetType () != typeof(Selection))
				return false;
			Mono.TextEditor.Selection other = (Mono.TextEditor.Selection)obj;
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
			return string.Format("[Selection: Anchor={0}, Lead={1}, MinLine={2}, MaxLine={3}, SelectionMode={4}]", Anchor, Lead, MinLine, MaxLine, SelectionMode);
		}
	}
}
