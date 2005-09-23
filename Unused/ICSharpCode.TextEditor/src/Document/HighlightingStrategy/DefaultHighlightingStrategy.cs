// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Drawing;
using System.Diagnostics;
using System.Collections.Specialized;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Xml;
using System.Text;

namespace MonoDevelop.TextEditor.Document
{
	public class DefaultHighlightingStrategy : IHighlightingStrategy
	{
		string    name;
		ArrayList rules = new ArrayList();
		
		HighlightBackground defaultColor;
		
		HighlightColor selectionColor   = null;
		HighlightColor vRulerColor      = null;
		HighlightColor spaceMarkerColor = null;
		HighlightColor tabMarkerColor   = null;
		HighlightColor invalidLineColor = null;
		HighlightColor lineNumberColor  = null;
		HighlightColor eolMarkerColor   = null;
		HighlightColor digitColor       = null;
		HighlightColor caretmarkerColor = null;
		HighlightColor foldLine         = null;
		HighlightColor foldMarker       = null;
		
		Hashtable      properties       = new Hashtable();
		string[]    extensions;
		
		HighlightRuleSet defaultRuleSet = null;
		
		
		public DefaultHighlightingStrategy()
		{
			name = "Default";
			defaultColor    = new HighlightBackground("WindowText", "Window", false, false);
			digitColor      = new HighlightBackground("WindowText", "Window", false, false);
			
			lineNumberColor = new HighlightBackground("ControlDark", "Window", false, false);
			
			eolMarkerColor  = new HighlightColor("ControlLight", "Window", false, false);
			spaceMarkerColor= new HighlightColor("ControlLight", "Window", false, false);
			tabMarkerColor  = new HighlightColor("ControlLight", "Window", false, false);
			
			selectionColor  = new HighlightColor("HighlightText", "Highlight", false, false);
			vRulerColor     = new HighlightColor("ControlLight", "Window", false, false);
			
			
			invalidLineColor= new HighlightColor(Color.Red, false, false);
			caretmarkerColor= new HighlightColor(Color.Yellow, false, false);
			foldLine        = new HighlightColor(Color.Black, false, false);
			foldMarker      = new HighlightColor(Color.White, false, false);
		}
		
		public DefaultHighlightingStrategy(string name)
		{
			this.name = name;
		}
		
		public Hashtable Properties {
			get {
				return properties;
			}
		}
		
		public string Name
		{
			get {
				return name;
			}
		}
		
		public string[] Extensions
		{
			set {
				extensions = value;
			}
			get {
				return extensions;
			}
		}
		
		public ArrayList Rules
		{
			get {
				return rules;
			}
		}
		
		public HighlightRuleSet FindHighlightRuleSet(string name)
		{
			foreach(HighlightRuleSet ruleSet in rules) {
				if (ruleSet.Name == name) {
					return ruleSet;
				}
			}
			return null;
		}
		
		public void AddRuleSet(HighlightRuleSet aRuleSet)
		{
			rules.Add(aRuleSet);
		}
		
		internal void ResolveReferences()
		{
			// Resolve references from Span definitions to RuleSets
			ResolveRuleSetReferences();
			// Resolve references from RuleSet defintitions to Highlighters defined in an external mode file
			ResolveExternalReferences();
		}
		
		void ResolveRuleSetReferences() 
		{
			foreach (HighlightRuleSet ruleSet in Rules) {
				if (ruleSet.Name == null) {
					defaultRuleSet = ruleSet;
				}
				
				foreach (Span aSpan in ruleSet.Spans) {
					if (aSpan.Rule != null) {
						bool found = false;
						foreach (HighlightRuleSet refSet in Rules) {
							if (refSet.Name == aSpan.Rule) {
								found = true;
								aSpan.RuleSet = refSet;
								break;
							}
						}
						if (!found) {
							//MessageBox.Show("The RuleSet " + aSpan.Rule + " could not be found in mode definition " + this.Name, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1);
							aSpan.RuleSet = null;
						}
					} else {
						aSpan.RuleSet = null;
					}
				}
			}
			
			if (defaultRuleSet == null) {
				//MessageBox.Show("No default RuleSet is defined for mode definition " + this.Name, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1);
			}
		}
		
		void ResolveExternalReferences() 
		{
			foreach (HighlightRuleSet ruleSet in Rules) {
				if (ruleSet.Reference != null) {
					IHighlightingStrategy highlighter = HighlightingManager.Manager.FindHighlighter (ruleSet.Reference);
					
					if (highlighter != null) {
						ruleSet.Highlighter = highlighter;
					} else {
						//MessageBox.Show("The mode defintion " + ruleSet.Reference + " which is refered from the " + this.Name + " mode definition could not be found", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1);
						ruleSet.Highlighter = this;
					}
				} else {
					ruleSet.Highlighter = this;
				}
			}
		}
		
		internal void SetDefaultColor(HighlightBackground color)
		{
			defaultColor = color;
		}		
		
		internal void SetColorFor(string name, HighlightColor color)
		{
			switch (name) {
				case "InvalidLines":
					invalidLineColor = color;
					return;
				case "EOLMarkers":
					eolMarkerColor = color;
					return;
				case "Selection":
					selectionColor = color;
					return;
				case "VRuler":
					vRulerColor = color;
					return;
				case "SpaceMarkers":
					spaceMarkerColor = color;
					return;
				case "LineNumbers":
					lineNumberColor = color;
					return;
				case "TabMarkers":
					tabMarkerColor = color;
					return;
				case "Digits":
					digitColor = color;
					return;
				case "CaretMarker":
					caretmarkerColor = color;
					return;
				case "FoldLine":
					foldLine = color;
					return;
				case "FoldMarker":
					foldMarker = color;
					return;
			}
			throw new HighlightingColorNotFoundException(name);
		}

		public HighlightColor GetColorFor(string name)
		{
			// Svante Lidman, clean up the code and use only the first ones here
			switch (name) {
				case "DefaultColor":
					return defaultColor;
				case "InvalidLines" :
				case "InvalideLines":
					return invalidLineColor;
				case "EOLMarkers":
				case "EolMarker":
					return eolMarkerColor;
				case "Selection":
					return selectionColor;
				case "VRuler":
				case "VRulerColor":
					return vRulerColor;
				case "SpaceMarkers":
				case "SpaceMarker":
					return spaceMarkerColor;
				case "LineNumbers":
				case "LineNumber":
					return lineNumberColor;
				case "TabMarkers":
				case "TabMarker":
					return tabMarkerColor;
				case "Digits":
				case "Digit":
					return digitColor;
				case "CaretMarker":
				case "Caretmarker":
					return caretmarkerColor;
				case "FoldLine":
					return foldLine;
				case "FoldMarker":
					return foldMarker;
			}
			throw new HighlightingColorNotFoundException(name);
		}
		
		public HighlightColor DigitColor {
			get {
				return digitColor;
			}
		}

		public HighlightColor GetColor(IDocument document, LineSegment currentSegment, int currentOffset, int currentLength)
		{
			return GetColor(defaultRuleSet, document, currentSegment, currentOffset, currentLength);
		}

		HighlightColor GetColor(HighlightRuleSet ruleSet, IDocument document, LineSegment currentSegment, int currentOffset, int currentLength)
		{
			if (ruleSet != null) {
				if (ruleSet.Reference != null) {
					return ruleSet.Highlighter.GetColor(document, currentSegment, currentOffset, currentLength);
				} else {
					return (HighlightColor)ruleSet.KeyWords[document,  currentSegment, currentOffset, currentLength];
				}				
			}
			return null;
		}
		
		public HighlightRuleSet GetRuleSet(Span aSpan)
		{
			if (aSpan == null) {
				return this.defaultRuleSet;
			} else {
				if (aSpan.RuleSet != null)
				{
					if (aSpan.RuleSet.Reference != null) {
						return aSpan.RuleSet.Highlighter.GetRuleSet(null);
					} else {
						return aSpan.RuleSet;
					}
				} else {
					return null;
				}
			}
		}

		// Line state variable
		LineSegment currentLine;
		
		// Span stack state variable
		Stack currentSpanStack;

		public void MarkTokens(IDocument document)
		{
			if (Rules.Count == 0) {
				return;
			}
			
			int lineNumber = 0;
			
			while (lineNumber < document.TotalNumberOfLines) {
				LineSegment previousLine = (lineNumber > 0 ? document.GetLineSegment(lineNumber - 1) : null);
				if (lineNumber >= document.LineSegmentCollection.Count) { // may be, if the last line ends with a delimiter
					break;                                                // then the last line is not in the collection :)
				}
				
				currentSpanStack = ((previousLine != null && previousLine.HighlightSpanStack != null) ? ((Stack)(previousLine.HighlightSpanStack.Clone())) : null);
				
				if (currentSpanStack != null) {
					while (currentSpanStack.Count > 0 && ((Span)currentSpanStack.Peek()).StopEOL)
					{
						currentSpanStack.Pop();
					}
					if (currentSpanStack.Count == 0) currentSpanStack = null;
				}
				
				currentLine = (LineSegment)document.LineSegmentCollection[lineNumber];
				
				if (currentLine.Length == -1) { // happens when buffer is empty !
					return;
				}
				
				ArrayList words = ParseLine(document);
				currentLine.Words = words;
				currentLine.HighlightSpanStack = (currentSpanStack==null || currentSpanStack.Count==0) ? null : currentSpanStack;
				
				++lineNumber;
			}
			document.RequestUpdate(new TextAreaUpdate(TextAreaUpdateType.WholeTextArea));
			document.CommitUpdate();
		}
		
		bool MarkTokensInLine(IDocument document, int lineNumber, ref bool spanChanged)
		{
			bool processNextLine = false;
			LineSegment previousLine = (lineNumber > 0 ? document.GetLineSegment(lineNumber - 1) : null);
			
			currentSpanStack = ((previousLine != null && previousLine.HighlightSpanStack != null) ? ((Stack)(previousLine.HighlightSpanStack.Clone())) : null);
			if(currentSpanStack != null) {
				while (currentSpanStack.Count > 0 && ((Span)currentSpanStack.Peek()).StopEOL) {
					currentSpanStack.Pop();
				}
				if (currentSpanStack.Count == 0) {
					currentSpanStack = null;
				}
			}
			
			currentLine = (LineSegment)document.LineSegmentCollection[lineNumber];
			
			if (currentLine.Length == -1) { // happens when buffer is empty !
				return false;
			}
			
			ArrayList words = ParseLine(document);
			
			if (currentSpanStack != null && currentSpanStack.Count == 0) {
				currentSpanStack = null;
			}
			
			// Check if the span state has changed, if so we must re-render the next line
			// This check may seem utterly complicated but I didn't want to introduce any function calls
			// or alllocations here for perf reasons.
			if(currentLine.HighlightSpanStack != currentSpanStack) {
				if (currentLine.HighlightSpanStack == null) {
					processNextLine = false;
					foreach (Span sp in currentSpanStack) {
						if (!sp.StopEOL) {
							spanChanged = true;
							processNextLine = true;
							break;
						}
					}
				} else if (currentSpanStack == null) {
					processNextLine = false;
					foreach (Span sp in currentLine.HighlightSpanStack) {
						if (!sp.StopEOL) {
							spanChanged = true;
							processNextLine = true;
							break;
						}
					}
				} else {
					IEnumerator e1 = currentSpanStack.GetEnumerator();
					IEnumerator e2 = currentLine.HighlightSpanStack.GetEnumerator();
					bool done = false;
					while (!done) {
						bool blockSpanIn1 = false;
						while (e1.MoveNext()) {
							if (!((Span)e1.Current).StopEOL) {
								blockSpanIn1 = true;
								break;
							}
						}
						bool blockSpanIn2 = false;
						while (e2.MoveNext()) {
							if (!((Span)e2.Current).StopEOL) {
								blockSpanIn2 = true;
								break;
							}
						}
						if (blockSpanIn1 || blockSpanIn2) {
							if (blockSpanIn1 && blockSpanIn2) {
								if (e1.Current != e2.Current) {
									done = true;
									processNextLine = true;
									spanChanged = true;
								}											
							} else {
								spanChanged = true;
								done = true;
								processNextLine = true;
							}
						} else {
							done = true;
							processNextLine = false;
						}
					}
				}
			} else {
				processNextLine = false;
			}
			
			currentLine.Words = words;
			currentLine.HighlightSpanStack = (currentSpanStack != null && currentSpanStack.Count > 0) ? currentSpanStack : null;
			
			return processNextLine;
		}
		
		public void MarkTokens(IDocument document, ArrayList inputLines)
		{
			if (Rules.Count == 0) {
				return;
			}
			
			Hashtable processedLines = new Hashtable();
			
			bool spanChanged = false;
			
			foreach (LineSegment lineToProcess in inputLines) {
				if (processedLines[lineToProcess] == null) {
					int lineNumber = document.GetLineNumberForOffset(lineToProcess.Offset);
					bool processNextLine = true;
					
					if (lineNumber != -1) {
						while (processNextLine && lineNumber < document.TotalNumberOfLines) {
							if (lineNumber >= document.LineSegmentCollection.Count) { // may be, if the last line ends with a delimiter
								break;                                                // then the last line is not in the collection :)
							}
							
							processNextLine = MarkTokensInLine(document, lineNumber, ref spanChanged);
 							processedLines[currentLine] = String.Empty;
							++lineNumber;
						}
					}
				} 
			}
			
			if (spanChanged) {
				document.RequestUpdate(new TextAreaUpdate(TextAreaUpdateType.WholeTextArea));
			} else {
//				document.Caret.ValidateCaretPos();
//				document.RequestUpdate(new TextAreaUpdate(TextAreaUpdateType.SingleLine, document.GetLineNumberForOffset(document.Caret.Offset)));
//				
				foreach (LineSegment lineToProcess in inputLines) {
					document.RequestUpdate(new TextAreaUpdate(TextAreaUpdateType.SingleLine, document.GetLineNumberForOffset(lineToProcess.Offset)));
				}
				
			}
			document.CommitUpdate();
		}
		
		// Span state variables
		bool inSpan;
		Span activeSpan;
		HighlightRuleSet activeRuleSet;
		
		// Line scanning state variables
		int currentOffset;
		int currentLength;
		
		void UpdateSpanStateVariables() 
		{
			inSpan = (currentSpanStack != null && currentSpanStack.Count > 0);
			activeSpan = inSpan ? (Span)currentSpanStack.Peek() : null;
			activeRuleSet = GetRuleSet(activeSpan);
		}

		ArrayList ParseLine(IDocument document)
		{
			ArrayList words = new ArrayList();
			HighlightColor markNext = null;
			
			currentOffset = 0;
			currentLength = 0;
			UpdateSpanStateVariables();
			
			for (int i = 0; i < currentLine.Length; ++i) {
				char ch = document.GetCharAt(currentLine.Offset + i);
				switch (ch) {
					case '\n':
					case '\r':
						PushCurWord(document, ref markNext, words);
						++currentOffset;
						break;
					case ' ':
						PushCurWord(document, ref markNext, words);
						words.Add(TextWord.Space);
						++currentOffset;
						break;
					case '\t':
						PushCurWord(document, ref markNext, words);
						words.Add(TextWord.Tab);
						++currentOffset;
						break;
					case '\\': // handle escape chars
						if ((activeRuleSet != null && activeRuleSet.NoEscapeSequences) || 
						    (activeSpan != null && activeSpan.NoEscapeSequences)) {
							goto default;
						}
						++currentLength;
						if (i + 1 < currentLine.Length) {
							++currentLength;
						}
						PushCurWord(document, ref markNext, words);
						++i;
						continue;
					default: {
						// highlight digits
						if (!inSpan && (Char.IsDigit(ch) || (ch == '.' && i + 1 < currentLine.Length && Char.IsDigit(document.GetCharAt(currentLine.Offset + i + 1)))) && currentLength == 0) {
							bool ishex = false;
							bool isfloatingpoint = false;
							
							if (ch == '0' && i + 1 < currentLine.Length && Char.ToUpper(document.GetCharAt(currentLine.Offset + i + 1)) == 'X') { // hex digits
								const string hex = "0123456789ABCDEF";
								++currentLength;
								++i; // skip 'x'
								++currentLength; 
								ishex = true;
								while (i + 1 < currentLine.Length && hex.IndexOf(Char.ToUpper(document.GetCharAt(currentLine.Offset + i + 1))) != -1) {
									++i;
									++currentLength;
								}
							} else {
								++currentLength; 
								while (i + 1 < currentLine.Length && Char.IsDigit(document.GetCharAt(currentLine.Offset + i + 1))) {
									++i;
									++currentLength;
								}
							}
							if (!ishex && i + 1 < currentLine.Length && document.GetCharAt(currentLine.Offset + i + 1) == '.') {
								isfloatingpoint = true;
								++i;
								++currentLength;
								while (i + 1 < currentLine.Length && Char.IsDigit(document.GetCharAt(currentLine.Offset + i + 1))) {
									++i;
									++currentLength;
								}
							} 
								
							if (i + 1 < currentLine.Length && Char.ToUpper(document.GetCharAt(currentLine.Offset + i + 1)) == 'E') {
								isfloatingpoint = true;
								++i;
								++currentLength;
								if (i + 1 < currentLine.Length && (document.GetCharAt(currentLine.Offset + i + 1) == '+' || document.GetCharAt(currentLine.Offset + i + 1) == '-')) {
									++i;
									++currentLength;
								}
								while (i + 1 < currentLine.Length && Char.IsDigit(document.GetCharAt(currentLine.Offset + i + 1))) {
									++i;
									++currentLength;
								}
							}
							
							if (i + 1 < currentLine.Length) {
								char nextch = Char.ToUpper(document.GetCharAt(currentLine.Offset + i + 1));
								if (nextch == 'F' || nextch == 'M' || nextch == 'D') {
									isfloatingpoint = true;
									++i;
									++currentLength;
								}
							}
							
							if (!isfloatingpoint) {
								bool isunsigned = false;
								if (i + 1 < currentLine.Length && Char.ToUpper(document.GetCharAt(currentLine.Offset + i + 1)) == 'U') {
									++i;
									++currentLength;
									isunsigned = true;
								}
								if (i + 1 < currentLine.Length && Char.ToUpper(document.GetCharAt(currentLine.Offset + i + 1)) == 'L') {
									++i;
									++currentLength;
									if (!isunsigned && i + 1 < currentLine.Length && Char.ToUpper(document.GetCharAt(currentLine.Offset + i + 1)) == 'U') {
										++i;
										++currentLength;
									}
								}
							}
							
							words.Add(new TextWord(document, currentLine, currentOffset, currentLength, DigitColor, false));
							currentOffset += currentLength;
							currentLength = 0;
							continue;
						}

						// Check for SPAN ENDs
						if (inSpan) {
							if (activeSpan.End != null && !activeSpan.End.Equals("")) {
								if (currentLine.MatchExpr(activeSpan.End, i, document)) {
									PushCurWord(document, ref markNext, words);
									string regex = currentLine.GetRegString(activeSpan.End, i, document);
									currentLength += regex.Length;
									words.Add(new TextWord(document, currentLine, currentOffset, currentLength, activeSpan.EndColor, false));
									currentOffset += currentLength;
									currentLength = 0;
									i += regex.Length - 1;
									currentSpanStack.Pop();
									UpdateSpanStateVariables();
									continue;
								}
							}
						}
						
						// check for SPAN BEGIN
						if (activeRuleSet != null) {
							foreach (Span span in activeRuleSet.Spans) {
								if (currentLine.MatchExpr(span.Begin, i, document)) {
									PushCurWord(document, ref markNext, words);
									string regex = currentLine.GetRegString(span.Begin, i, document);
									currentLength += regex.Length;
									words.Add(new TextWord(document, currentLine, currentOffset, currentLength, span.BeginColor, false));
									currentOffset += currentLength;
									currentLength = 0;
									
									i += regex.Length - 1;
									if( currentSpanStack == null) currentSpanStack = new Stack();
									currentSpanStack.Push(span);
									
									UpdateSpanStateVariables();
									
									goto skip;
								}
							}
						}
						
						// check if the char is a delimiter
						if (activeRuleSet != null && (int)ch < 256 && activeRuleSet.Delimiters[(int)ch]) {
							PushCurWord(document, ref markNext, words);
							if (currentOffset + currentLength +1 < currentLine.Length) {
								++currentLength;
								PushCurWord(document, ref markNext, words);
								goto skip;
							}
						}
						
						++currentLength;
						skip: continue;
					}
				}
			}
			
			PushCurWord(document, ref markNext, words);			
			
			return words;
		}		
		
		/// <summary>
		/// pushes the curWord string on the word list, with the
		/// correct color.
		/// </summary>
		void PushCurWord(IDocument document, ref HighlightColor markNext, ArrayList words)
		{
			// Svante Lidman : Need to look through the next prev logic.
			if (currentLength > 0) {
				if (words.Count > 0 && activeRuleSet != null) {
					TextWord prevWord = null;
					int pInd = words.Count - 1;
					while (pInd >= 0) {
						if (!((TextWord)words[pInd]).IsWhiteSpace) {
							prevWord = (TextWord)words[pInd];
							if (prevWord.HasDefaultColor) {
								PrevMarker marker = (PrevMarker)activeRuleSet.PrevMarkers[document, currentLine, currentOffset, currentLength];
								if (marker != null) {
									prevWord.SyntaxColor = marker.Color;
//									document.Caret.ValidateCaretPos();
//									document.RequestUpdate(new TextAreaUpdate(TextAreaUpdateType.SingleLine, document.GetLineNumberForOffset(document.Caret.Offset)));
								}
							}
							break;
						}
						pInd--;
					}
				}
				
				if (inSpan) {
					HighlightColor c = null;
					bool hasDefaultColor = true;
					if (activeSpan.Rule == null) {
						c = activeSpan.Color;
					} else {
						c = GetColor(activeRuleSet, document, currentLine, currentOffset, currentLength);
						hasDefaultColor = false;
					}
					
					if (c == null) {
						c = activeSpan.Color;
						if (c.Color == Color.Transparent) {
							c = defaultColor;
						}
						hasDefaultColor = true;
					}
					words.Add(new TextWord(document, currentLine, currentOffset, currentLength, markNext != null ? markNext : c, hasDefaultColor));
				} else {
					HighlightColor c = markNext != null ? markNext : GetColor(activeRuleSet, document, currentLine, currentOffset, currentLength);
					if (c == null) {
						words.Add(new TextWord(document, currentLine, currentOffset, currentLength, defaultColor, true));
					} else {
						words.Add(new TextWord(document, currentLine, currentOffset, currentLength, c, false));
					}
				}
				
				if (activeRuleSet != null) {
					NextMarker nextMarker = (NextMarker)activeRuleSet.NextMarkers[document, currentLine, currentOffset, currentLength];
					if (nextMarker != null) {
						if (nextMarker.MarkMarker && words.Count > 0) {
							TextWord prevword = ((TextWord)words[words.Count - 1]);
							prevword.SyntaxColor = nextMarker.Color;
						}
						markNext = nextMarker.Color;
					} else {
						markNext = null;
					}
				}
				currentOffset += currentLength;
				currentLength = 0;					
			}
		}		
	}
}
