// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;
using System.Drawing;
using System.Text;

namespace MonoDevelop.TextEditor.Document
{
	/// <summary>
	/// A highlighting strategy for a buffer.
	/// </summary>
	public interface IHighlightingStrategy
	{
		/// <value>
		/// The name of the highlighting strategy, must be unique
		/// </value>
		string Name {
			get;
		}
		
		/// <value>
		/// The file extenstions on which this highlighting strategy gets
		/// used
		/// </value>
		string[] Extensions {
			set;
			get;
		}
		
		Hashtable Properties {
			get;
		}
		
		// returns special color. (BackGround Color, Cursor Color and so on)
		
		/// <remarks>
		/// Used internally, do not call
		/// </remarks>
		HighlightColor   GetColorFor(string name);
		
		/// <remarks>
		/// Used internally, do not call
		/// </remarks>
		HighlightRuleSet GetRuleSet(Span span);
		
		/// <remarks>
		/// Used internally, do not call
		/// </remarks>
		HighlightColor   GetColor(IDocument document, LineSegment keyWord, int index, int length);
		
		/// <remarks>
		/// Used internally, do not call
		/// </remarks>
		void MarkTokens(IDocument document, ArrayList lines);
		
		/// <remarks>
		/// Used internally, do not call
		/// </remarks>
		void MarkTokens(IDocument document);
	}
}
