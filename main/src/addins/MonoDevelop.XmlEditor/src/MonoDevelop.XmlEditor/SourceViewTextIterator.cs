//
// MonoDevelop XML Editor
//
// Copyright (C) 2006 MonoDevelop Team
//

using Gtk;
using GtkSourceView;
using MonoDevelop.Ide.Gui.Search;
using System;

namespace MonoDevelop.XmlEditor
{
	/// <summary>
	/// Taken from the MonoDevelop.SourceEditor since this
	/// class is private
	/// </summary>
	public class SourceViewTextIterator: ForwardTextIterator
	{
		bool initialBackwardsPosition;
		bool hasWrapped;
		
		public SourceViewTextIterator (IDocumentInformation docInfo, Gtk.TextView document, int endOffset)
		: base (docInfo, document, endOffset)
		{
			// Make sure the iterator is ready for use
			this.MoveAhead(1);
			this.hasWrapped = false;
		}
		
		public override bool SupportsSearch (SearchOptions options, bool reverse)
		{
			return !options.SearchWholeWordOnly;
		}
		
		public override void MoveToEnd ()
		{
			initialBackwardsPosition = true;
			base.MoveToEnd ();
		}
		
		public override bool SearchNext (string text, SearchOptions options, bool reverse)
		{
			// Make sure the backward search finds the first match when that match is just
			// at the left of the cursor. Position needs to be incremented in this case because it will be
			// at the last char of the match, and BackwardSearch don't return results that include
			// the initial search position.
			if (reverse && Position < BufferLength && initialBackwardsPosition) {
				Position++;
				initialBackwardsPosition = false;
			}
							
			// Use special search flags that work for both the old and new API
			// of gtksourceview (the enum values where changed in the API).
			// See bug #75770
			SourceSearchFlags flags = options.IgnoreCase ? (SourceSearchFlags)7 : (SourceSearchFlags)1;
			
			Gtk.TextIter matchStart, matchEnd, limit;
								
			
			if (reverse) {
				if (!hasWrapped)
					limit = Buffer.StartIter;
				else
					limit = Buffer.GetIterAtOffset (EndOffset);
			} else {
				if (!hasWrapped)
					limit = Buffer.EndIter;
				else
					limit = Buffer.GetIterAtOffset (EndOffset + text.Length);
			}
			
			// machEnd is the position of the last matched char + 1
			// When searching forward, the limit check is: matchEnd < limit
			// When searching backwards, the limit check is: matchEnd > limit
			
			TextIter iterator = Buffer.GetIterAtOffset (DocumentOffset);
			bool res = Find (reverse, iterator, text, flags, out matchStart, out matchEnd, limit);
			
			if (!res && !hasWrapped) {
				
				hasWrapped = true;																
								
				// Not found in the first half of the document, try the other half
				if (reverse && DocumentOffset <= EndOffset) {					
					limit = Buffer.GetIterAtOffset (EndOffset);
					res = Find (true, Buffer.EndIter, text, flags, out matchStart, out matchEnd, limit);
				// Not found in the second half of the document, try the other half
				} else if (!reverse && DocumentOffset >= EndOffset) {										
					limit = Buffer.GetIterAtOffset (EndOffset + text.Length);									
					res = Find (false, Buffer.StartIter, text, flags, out matchStart, out matchEnd, limit);
				}
			}
			
			if (!res) return false;
			
			DocumentOffset = matchStart.Offset;
			return true;
		}
		
		
		bool Find (bool reverse, Gtk.TextIter iter, string str, GtkSourceView.SourceSearchFlags flags, out Gtk.TextIter match_start, out Gtk.TextIter match_end, Gtk.TextIter limit)
		{
			if (reverse)
				return ((SourceBuffer)Buffer).BackwardSearch (iter, str, flags, out match_start, out match_end, limit);
			else
				return ((SourceBuffer)Buffer).ForwardSearch (iter, str, flags, out match_start, out match_end, limit);
		}
	}
	
}
