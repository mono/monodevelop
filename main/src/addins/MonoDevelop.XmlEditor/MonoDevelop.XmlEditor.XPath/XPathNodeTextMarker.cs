//
// MonoDevelop XML Editor
//
// Copyright (C) 2006 Matthew Ward
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
/*
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
 */