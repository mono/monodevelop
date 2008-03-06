//
// MonoDevelop XML Editor
//
// Copyright (C) 2006 Matthew Ward
//

using Gtk;
using GtkSourceView;
using System;

namespace MonoDevelop.XmlEditor
{
	/// <summary>
	/// A text marker for an XPath query match.
	/// </summary>
	public class XPathNodeTextMarker
	{	
		SourceBuffer buffer;
		TextTag xpathMatchTextTag;
		
		public XPathNodeTextMarker(SourceBuffer buffer)
		{
			this.buffer = buffer;
			
			xpathMatchTextTag = new TextTag("XPathMatch");
			xpathMatchTextTag.Background = "lightgrey";
			buffer.TagTable.Add(xpathMatchTextTag);
		}
		
		/// <summary>
		/// Adds markers for each XPathNodeMatch.
		/// </summary>
		public void AddMarkers(XPathNodeMatch[] nodes)
		{
			foreach (XPathNodeMatch node in nodes) {
				AddMarker(node);
			}
		}
		
		/// <summary>
		/// Adds a single marker for the XPathNodeMatch.
		/// </summary>
		public void AddMarker(XPathNodeMatch node)
		{
			if (node.HasLineInfo() && node.Value.Length > 0) {
				TextIter lineIter = buffer.GetIterAtLine(node.LineNumber);
				int offset = lineIter.Offset + node.LinePosition;
				TextIter startIter = buffer.GetIterAtOffset(offset);
				offset += node.Value.Length;
				TextIter endIter = buffer.GetIterAtOffset(offset);
				buffer.ApplyTag(xpathMatchTextTag, startIter, endIter);
			}
		}
		
		/// <summary>
		/// Removes all the XPathNodeMarkers.
		/// </summary>
		public void RemoveMarkers()
		{
			buffer.RemoveTag(xpathMatchTextTag, buffer.StartIter, buffer.EndIter);
		}
	}
}
