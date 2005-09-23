// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.IO;


namespace MonoDevelop.TextEditor.Document
{
	/// <summary>
	/// This interface represents a container which holds a text sequence and
	/// all necessary information about it. It is used as the base for a text editor.
	/// </summary>
	public class DocumentFactory
	{
		/// <remarks>
		/// Creates a new <see cref="IDocument"/> object. Only create
		/// <see cref="IDocument"/> with this method.
		/// </remarks>
		public IDocument CreateDocument()
		{
			DefaultDocument doc = new DefaultDocument();
			doc.TextBufferStrategy    = new GapTextBufferStrategy();
			doc.FormattingStrategy    = new DefaultFormattingStrategy();
			doc.FoldingManager        = new FoldingManager(doc);
			doc.FoldingManager.FoldingStrategy       = new ParserFoldingStrategy();
			
			doc.LineManager          = new DefaultLineManager(doc, null);
			doc.BookmarkManager      = new BookmarkManager(doc.LineManager);
			return doc;
		}
		
		/// <summary>
		/// Creates a new document and loads the given file
		/// </summary>
		public IDocument CreateFromFile(string fileName)
		{
			IDocument document = CreateDocument();
			StreamReader stream = File.OpenText(fileName);
			document.TextContent = stream.ReadToEnd();
			stream.Close();
			return document;
		}
	}
}
