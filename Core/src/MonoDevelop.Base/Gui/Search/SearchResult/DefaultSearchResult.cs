// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;

namespace MonoDevelop.Gui.Search
{
	internal class DefaultSearchResult : ISearchResult
	{
		IDocumentInformation documentInformation;
		int offset;
		int length;
		int line;
		int column;
		int position;
		
		public DefaultSearchResult (ITextIterator iter, int length)
		{
			position = iter.Position;
			offset = iter.DocumentOffset;
			line = iter.Line + 1;
			column = iter.Column + 1;
			this.length = length;
			documentInformation = iter.DocumentInformation;
		}
		
		public string FileName {
			get {
				return documentInformation.FileName;
			}
		}
		
		public IDocumentInformation DocumentInformation {
			get {
				return documentInformation;
			}
		}
		
		public int DocumentOffset {
			get {
				return offset;
			}
		}
		
		public int Position {
			get {
				return position;
			}
		}
		
		public int Length {
			get {
				return length;
			}
		}
		
		public int Line {
			get { return line; }
		}
		
		public int Column {
			get { return column; }
		}
		
		public virtual string TransformReplacePattern (string pattern)
		{
			return pattern;
		}
		
		public override string ToString()
		{
			return String.Format("[DefaultLocation: FileName={0}, Offset={1}, Length={2}]",
			                     FileName,
			                     DocumentOffset,
			                     Length);
		}
	}
}
