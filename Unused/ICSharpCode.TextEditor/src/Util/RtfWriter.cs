//  RtfWriter.cs
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
using System.Drawing;
using System.Text;
using System.Collections;
using MonoDevelop.TextEditor.Document;

namespace MonoDevelop.TextEditor.Util
{
	public class RtfWriter
	{
		static Hashtable     colors;
		static int           colorNum;
		static StringBuilder colorString;
		
		public static string GenerateRtf(TextArea textArea)
		{
			try {
				colors = new Hashtable();
				colorNum = 0;
				colorString = new StringBuilder();
				
				
				StringBuilder rtf = new StringBuilder();
				
				rtf.Append(@"{\rtf1\ansi\ansicpg1252\deff0\deflang1031");
				BuildFontTable(textArea.Document, rtf);
				rtf.Append('\n');
				
				string fileContent = BuildFileContent(textArea);
				BuildColorTable(textArea.Document, rtf);
				rtf.Append('\n');
				rtf.Append(@"\viewkind4\uc1\pard");
				rtf.Append(fileContent);
				rtf.Append("}");
				return rtf.ToString();
			} catch (Exception e) {
				Console.WriteLine(e.ToString());
			}
			return null;
		}
		
		static void BuildColorTable(IDocument doc, StringBuilder rtf)
		{
			rtf.Append(@"{\colortbl ;");
			rtf.Append(colorString.ToString());
			rtf.Append("}");
		}
		
		static void BuildFontTable(IDocument doc, StringBuilder rtf)
		{
			rtf.Append(@"{\fonttbl");
			rtf.Append(@"{\f0\fmodern\fprq1\fcharset0 " + FontContainer.DefaultFont.Family + ";}");
			rtf.Append("}");
		}
		
		static string BuildFileContent(TextArea textArea)
		{
			StringBuilder rtf = new StringBuilder();
			bool firstLine = true;
			Color curColor = Color.Black;
			bool  oldItalic = false;
			bool  oldBold   = false;
			bool  escapeSequence = false;
			
			foreach (ISelection selection in textArea.SelectionManager.SelectionCollection) {
				int selectionOffset    = textArea.Document.PositionToOffset(selection.StartPosition);
				int selectionEndOffset = textArea.Document.PositionToOffset(selection.EndPosition);
				for (int i = selection.StartPosition.Y; i <= selection.EndPosition.Y; ++i) {
					LineSegment line = textArea.Document.GetLineSegment(i);
					int offset = line.Offset;
					if (line.Words == null) {
						continue;
					}
					
					foreach (TextWord word in line.Words) {
						switch (word.Type) {
							case TextWordType.Space:
								if (selection.ContainsOffset(offset)) {
									rtf.Append(' ');
								}
								++offset;
								break;
							
							case TextWordType.Tab:
								if (selection.ContainsOffset(offset)) {
									rtf.Append(@"\tab");
								}
								++offset;
								escapeSequence = true;
								break;
							
							case TextWordType.Word:
								Color c = word.Color;
								
								if (offset + word.Word.Length > selectionOffset && offset < selectionEndOffset) {
									string colorstr = c.R + ", " + c.G + ", " + c.B;
									
									if (colors[colorstr] == null) {
										colors[colorstr] = ++colorNum;
										colorString.Append(@"\red" + c.R + @"\green" + c.G + @"\blue" + c.B + ";");
									}
									if (c != curColor || firstLine) {
										rtf.Append(@"\cf" + colors[colorstr].ToString());
										curColor = c;
										escapeSequence = true;
									}
									
/*									if (oldItalic != word.Font.Italic) {
										if (word.Font.Italic) {
											rtf.Append(@"\i");
										} else {
											rtf.Append(@"\i0");
										}
										oldItalic = word.Font.Italic;
										escapeSequence = true;
									//}
									
									if (oldBold != word.Font.Bold) {
										if (word.Font.Bold) {
											rtf.Append(@"\b");
										} else {
											rtf.Append(@"\b0");
										}
										oldBold = word.Font.Bold;
										escapeSequence = true;
									}
*/									
									if (firstLine) {
										rtf.Append(@"\f0\fs" + (FontContainer.DefaultFont.Size * 2));
										firstLine = false;
									}
									if (escapeSequence) {
										rtf.Append(' ');
										escapeSequence = false;
									}
									string printWord;
									if (offset < selectionOffset) {
										printWord = word.Word.Substring(selectionOffset - offset);
									} else if (offset + word.Word.Length > selectionEndOffset) {
										printWord = word.Word.Substring(0, (offset + word.Word.Length) - selectionEndOffset);
									} else {
										printWord = word.Word;
									}
									
									rtf.Append(printWord.Replace("{", "\\{").Replace("}", "\\}"));
								}
								offset += word.Length;
								break;
						}
					}
					if (offset < selectionEndOffset) {
						rtf.Append(@"\par");
					}
					rtf.Append('\n');
				}
			}
			
			return rtf.ToString();
		}
	}
}
