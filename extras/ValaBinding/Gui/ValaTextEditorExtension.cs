//
// ValaTextEditorExtension.cs
//
// Authors:
//  Levi Bard <taktaktaktaktaktaktaktaktaktak@gmail.com> 
//
// Copyright (C) 2008 Levi Bard
// Based on CBinding by Marcos David Marin Amador <MarcosMarin@gmail.com>
//
// This source code is licenced under The MIT License:
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
//


using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Threading;

using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Ide.CodeCompletion;

using MonoDevelop.ValaBinding.Parser;

namespace MonoDevelop.ValaBinding
{
	public class ValaTextEditorExtension : CompletionTextEditorExtension
	{
		// Allowed chars to be next to an identifier
		private static char[] allowedChars = new char[] { ' ', '\t', '\r', '\n', 
			':', '=', '*', '+', '-', '/', '%', ',', '&',
			'|', '^', '{', '}', '[', ']', '(', ')', '\n', '!', '?', '<', '>'
		};
		
		private static char[] operators = new char[] {
			'=', '+', '-', ',', '&', '|',
			'^', '[', '!', '?', '<', '>', ':'
		};
		
		private ProjectInformation Parser {
			get {
				ValaProject project = Document.Project as ValaProject;
				return (null == project)? null: ProjectInformationManager.Instance.Get (project);
			}
		}// Parser
		
		public override bool KeyPress (Gdk.Key key, char keyChar, Gdk.ModifierType modifier)
		{
			string lineText = Editor.GetLineText (Editor.Caret.Line);
			
			// smart formatting strategy
			if (TextEditorProperties.IndentStyle == IndentStyle.Smart) {
				if (key == Gdk.Key.Return) {
					if (lineText.TrimEnd ().EndsWith ("{")) {
						Editor.InsertAtCaret ("\n" + TextEditorProperties.IndentString + Editor.Document.GetLineIndent (Editor.Caret.Line));
						return false;
					}
				} else if (key == Gdk.Key.braceright && AllWhiteSpace (lineText) 
				    && lineText.StartsWith (TextEditorProperties.IndentString)) {
					if (lineText.Length > 0)
						lineText = lineText.Substring (TextEditorProperties.IndentString.Length);
					var lineSegment = Editor.Document.GetLine (Editor.Caret.Line);
					Editor.Replace (lineSegment.Offset, lineSegment.EditableLength, lineText + "}");
					return false;
				}
			}
			
			return base.KeyPress (key, keyChar, modifier);
		}
		
		/// <summary>
		/// Expression to match instance construction/initialization
		/// </summary>
		private static Regex initializationRegex = new Regex (@"(((?<typename>\w[\w\d\.<>]*)\s+)?(?<variable>\w[\w\d]*)\s*=\s*)?new\s*(?<constructor>\w[\w\d\.<>]*)?", RegexOptions.Compiled);
		
		public override ICompletionDataList HandleCodeCompletion (
		    CodeCompletionContext completionContext, char completionChar)
		{
			string lineText = null;
			ProjectInformation parser = Parser;
			var loc = Editor.Document.OffsetToLocation (completionContext.TriggerOffset);
			int line = loc.Line + 1, column = loc.Column + 1;
			switch (completionChar) {
			case '.': // foo.[complete]
				lineText = Editor.GetLineText (line);
				if (column > lineText.Length){ column = lineText.Length; }
				lineText = lineText.Substring (0, column - 1);
				
				string itemName = GetTrailingSymbol (lineText);
				
				if (string.IsNullOrEmpty (itemName))
					return null;
				
				return GetMembersOfItem (itemName, line, column);
			case '\t':
			case ' ':
				lineText = Editor.GetLineText (line);
				if (column > lineText.Length){ column = lineText.Length; }
				lineText = lineText.Substring (0, column-1).Trim ();
				
				if (lineText.EndsWith ("new")) {
					return CompleteConstructor (lineText, line, column);
				} else if (lineText.EndsWith ("is")) {
					ValaCompletionDataList list = new ValaCompletionDataList ();
					ThreadPool.QueueUserWorkItem (delegate {
						parser.GetTypesVisibleFrom (Document.FileName, line, column, list);
					});
					return list;
				} else if (0 < lineText.Length) {
					char lastNonWS = lineText[lineText.Length-1];
					if (0 <= Array.IndexOf (operators, lastNonWS) || 
				          (1 == lineText.Length && 0 > Array.IndexOf (allowedChars, lastNonWS))) { 
						return GlobalComplete (completionContext); 
					}
				}
				
				break;
			default:
				if (0 <= Array.IndexOf (operators, completionChar)) {
					return GlobalComplete (completionContext);
				}
				break;
			}
			
			return null;
		}
		
		static string GetTrailingSymbol (string text)
		{
				// remove the trailing '.'
				if (text.EndsWith (".", StringComparison.Ordinal))
					text = text.Substring (0, text.Length-1);
				
				int nameStart = text.LastIndexOfAny (allowedChars);
				return text.Substring (nameStart+1).Trim ();
		}
		
		/// <summary>
		/// Perform constructor-specific completion
		/// </summary>
		private ValaCompletionDataList CompleteConstructor (string lineText, int line, int column)
		{
			ProjectInformation parser = Parser;
			Match match = initializationRegex.Match (lineText);
			ValaCompletionDataList list = new ValaCompletionDataList ();
			
			ThreadPool.QueueUserWorkItem (delegate {
				if (match.Success) {
					// variable initialization
					if (match.Groups["typename"].Success || "var" != match.Groups["typename"].Value) {
						// simultaneous declaration and initialization
						parser.GetConstructorsForType (match.Groups["typename"].Value, Document.FileName, line, column, list);
					} else if (match.Groups["variable"].Success) {
						// initialization of previously declared variable
						parser.GetConstructorsForExpression (match.Groups["variable"].Value, Document.FileName, line, column, list);
					}
					if (0 == list.Count) { 
						// Fallback to known types
						parser.GetTypesVisibleFrom (Document.FileName, line, column, list);
					}
				}
			});
			
			return list;
		}// CompleteConstructor
		
		public override ICompletionDataList CodeCompletionCommand (
		    CodeCompletionContext completionContext)
		{
			if (null == (Document.Project as ValaProject)){ return null; }
			
			int pos = completionContext.TriggerOffset;
			
			ICompletionDataList list = HandleCodeCompletion(completionContext, Editor.GetTextBetween (pos - 1, pos)[0]);
			if (null == list) {
				list = GlobalComplete (completionContext);
			}
			return list;
		}
		
		/// <summary>
		/// Get the members of a symbol
		/// </summary>
		private ValaCompletionDataList GetMembersOfItem (string itemFullName, int line, int column)
		{
			ProjectInformation info = Parser;
			if (null == info){ return null; }
			
			ValaCompletionDataList list = new ValaCompletionDataList ();
			ThreadPool.QueueUserWorkItem (delegate {
				info.Complete (itemFullName, Document.FileName, line, column, list);
			});
			return list;
		}
		
		/// <summary>
		/// Complete all symbols visible from a given location
		/// </summary>
		private ValaCompletionDataList GlobalComplete (CodeCompletionContext context)
		{
			ProjectInformation info = Parser;
			if (null == info){ return null; }
			
			ValaCompletionDataList list = new ValaCompletionDataList ();
			var loc = Editor.Document.OffsetToLocation (context.TriggerOffset);
			ThreadPool.QueueUserWorkItem (delegate {
				info.GetSymbolsVisibleFrom (Document.FileName, loc.Line + 1, loc.Column + 1, list);
			});
			return list;
		}
		
		public override  IParameterDataProvider HandleParameterCompletion (
		    CodeCompletionContext completionContext, char completionChar)
		{
			if (completionChar != '(')
				return null;
			
			ProjectInformation info = Parser;
			if (null == info){ return null; }
			
			int position = Editor.Document.GetLine (Editor.Caret.Line).Offset;
			string lineText = Editor.GetTextBetween (position, Editor.Caret.Offset - 1).TrimEnd ();
			string functionName = string.Empty;
			
			Match match = initializationRegex.Match (lineText);
			if (match.Success && match.Groups["constructor"].Success) {
				string[] tokens = match.Groups["constructor"].Value.Split('.');
				string overload = tokens[tokens.Length-1];
				string typename = (match.Groups["typename"].Success? match.Groups["typename"].Value: null);
				int index = 0;
				
				if (1 == tokens.Length || null == typename) {
					// Ideally if typename is null and token length is longer than 1, 
					// we have an expression like: var w = new x.y.z(); and 
					// we would check whether z is the type or if y.z is an overload for type y
					typename = overload;
				} else if ("var".Equals (typename, StringComparison.Ordinal)) {
					typename = match.Groups["constructor"].Value;
				} else {
					// Foo.Bar bar = new Foo.Bar.blah( ...
					for (string[] typeTokens = typename.Split ('.'); index < typeTokens.Length && index < tokens.Length; ++index) {
						if (!typeTokens[index].Equals (tokens[index], StringComparison.Ordinal)) {
							break;
						}
					}
					List<string> overloadTokens = new List<string> ();
					for (int i=index; i<tokens.Length; ++i) {
						overloadTokens.Add (tokens[i]);
					}
					overload = string.Join (".", overloadTokens.ToArray ());
				} 
				
				// HACK: Generics
				if (0 < (index = overload.IndexOf ("<", StringComparison.Ordinal))) {
					overload = overload.Substring (0, index);
				}
				if (0 < (index = typename.IndexOf ("<", StringComparison.Ordinal))) {
					typename = typename.Substring (0, index);
				}
				
				// Console.WriteLine ("Constructor: type {0}, overload {1}", typename, overload);
				return new ParameterDataProvider (Document, info, typename, overload); 
			} 
			
			int nameStart = lineText.LastIndexOfAny (allowedChars) + 1;
			functionName = lineText.Substring (nameStart).Trim ();
			return (string.IsNullOrEmpty (functionName)? null: new ParameterDataProvider (Document, info, functionName));
		}
		
		private bool AllWhiteSpace (string lineText)
		{
			foreach (char c in lineText)
				if (!char.IsWhiteSpace (c))
					return false;
			
			return true;
		}
	}
}
