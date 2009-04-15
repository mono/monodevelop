using System;

namespace Mono.TextEditor
{
	public class Selection
	{
		DocumentLocation anchor;
		public DocumentLocation Anchor {
			get {
				return anchor;
			}
			set {
				anchor = value;
				OnChanged ();
			}
		}
		
		DocumentLocation lead;
		public DocumentLocation Lead {
			get {
				return lead;
			}
			set {
				lead = value;
				OnChanged ();
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
		
		public SelectionMode SelectionMode {
			get;
			set;
		}
		
		public bool Contains (DocumentLocation loc)
		{
			return anchor <= loc && loc <= lead || lead <= loc && loc <= anchor;
		}
		
		public Selection ()
		{
			SelectionMode = SelectionMode.Normal;
		}
		
		public static Selection Clone (Selection selection)
		{
			if (selection == null)
				return null;
			return new Selection (selection.Anchor, selection.Lead, selection.SelectionMode);
		}
		
		public Selection (DocumentLocation anchor, DocumentLocation lead) : this (anchor, lead, SelectionMode.Normal)
		{
		}
		
		public Selection (DocumentLocation anchor, DocumentLocation lead, SelectionMode selectionMode)
		{
			this.Anchor        = anchor;
			this.Lead          = lead;
			this.SelectionMode = selectionMode;
		}
		
		public ISegment GetSelectionRange (TextEditorData data)
		{
			int anchorOffset = GetAnchorOffset (data);
			int leadOffset   = GetLeadOffset (data);
			return new Segment (System.Math.Min (anchorOffset, leadOffset), System.Math.Abs (anchorOffset - leadOffset));
		}
		
		public int GetAnchorOffset (TextEditorData data)
		{
			return data.Document.LocationToOffset (Anchor);
		}
		
		public int GetLeadOffset (TextEditorData data)
		{
			return data.Document.LocationToOffset (Lead);
		}
		
		protected virtual void OnChanged ()
		{
			if (Changed != null)
				Changed (this, EventArgs.Empty);
		}
		
		public event EventHandler Changed;
	}
}
