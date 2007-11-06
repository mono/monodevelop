//  CommentCompletionDataProvider.cs
//
//  This file was derived from a file from #Develop. 
//
//  Copyright (C) 2001-2007 Mike Krüger <mkrueger@novell.com>
// 
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
//  
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA

using System;
using System.Reflection;
using System.Collections;

using MonoDevelop.Core;
using MonoDevelop.Core.Gui;

using MonoDevelop.Projects.Parser;
using MonoDevelop.Projects;
using Stock = MonoDevelop.Core.Gui.Stock;

namespace MonoDevelop.Projects.Gui.Completion
{
	/// <summary>
	/// Data provider for code completion.
	/// </summary>
/*	public class CommentCompletionDataProvider : ICompletionDataProvider
	{
		int caretLineNumber;
		int caretColumn;
		
		string[][] commentTags = new string[][] {
			new string[] {"c", "marks text as code"},
			new string[] {"code", "marks text as code"},
			new string[] {"example", "A description of the code example\n(must have a <code> tag inside)"},
			new string[] {"exception cref=\"\"", "description to an exception thrown"},
			new string[] {"list type=\"\"", "A list"},
			new string[] {"listheader", "The header from the list"},
			new string[] {"item", "A list item"},
			new string[] {"term", "A term in a list"},
			new string[] {"description", "A description to a term in a list"},
			new string[] {"param name=\"\"", "A description for a parameter"},
			new string[] {"paramref name=\"\"", "A reference to a parameter"},
			new string[] {"permission cref=\"\"", ""},
			new string[] {"remarks", "Gives description for a member"},
			new string[] {"include file=\"\" path=\"\"", "Includes comments from other files"},
			new string[] {"returns", "Gives description for a return value"},
			new string[] {"see cref=\"\"", "A reference to a member"},
			new string[] {"seealso cref=\"\"", "A reference to a member in the seealso section"},
			new string[] {"summary", "A summary of the object"},
			new string[] {"value", "A description of a property"}
		};

		public ICompletionData[] GenerateCompletionData (ICompletionWidget widget, char charTyped)
		{
			caretLineNumber = textArea.Caret.Line;
			caretColumn     = textArea.Caret.Column;
			LineSegment caretLine = textArea.Document.GetLineSegment(caretLineNumber);
			string lineText = textArea.Document.GetText(caretLine.Offset, caretLine.Length);
			if (!lineText.Trim().StartsWith("///")) {
				return null;
			}
			
			ArrayList completionData = new ArrayList ();
			foreach (string[] tag in commentTags) {
				completionData.Add(new CommentCompletionData(tag[0], tag[1]));
			}
			return (ICompletionData[])completionData.ToArray (typeof (ICompletionData));
		}
		
		class CommentCompletionData : ICompletionData
		{
			string text;
			string description;
			
			public string Image
			{
				get {
					return Stock.Method;
				}
			}
			
			public string[] Text
			{
				get {
					return new string[] { text };
				}
			}

			public string CompletionString
			{
				get 
				{
					return "";
				}
			}
			
			public string Description
			{
				get {
					return description;
				}
			}
			
			public void InsertAction (ICompletionWidget widget)
			{
				//((SharpDevelopTextAreaControl)control).ActiveTextAreaControl.TextArea.InsertString(text);
			}
			
			public CommentCompletionData (string text, string description) 
			{
				this.text = text;
				this.description = description;
			}
		}
	}
	*/
}
