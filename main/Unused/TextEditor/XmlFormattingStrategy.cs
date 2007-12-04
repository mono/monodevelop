//  XmlFormattingStrategy.cs
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

using System.Drawing;
using System.Diagnostics;
using System.Text;
using System.Xml;
using System;

using MonoDevelop.Core.Properties;
using MonoDevelop.DefaultEditor.Gui.Editor;
using MonoDevelop.TextEditor.Document;
using MonoDevelop.TextEditor.Actions;
using MonoDevelop.TextEditor;

using MonoDevelop.EditorBindings.FormattingStrategy;

namespace MonoDevelop.DefaultEditor
{
	/// <summary>
	/// This class currently only inserts the closing tags to 
	/// typed openening tags.
	/// </summary>
	public class XmlFormattingStrategy : DefaultFormattingStrategy
	{
		public override int FormatLine(IFormattableDocument d, int lineNr, int caretOffset, char charTyped) // used for comment tag formater/inserter
		{
			try {
				if (charTyped == '>') {
					StringBuilder stringBuilder = new StringBuilder();
					int offset = Math.Min(caretOffset - 2, d.TextLength - 1);
					while (true) {
						if (offset < 0) {
							break;
						}
						char ch = d.GetCharAt(offset);
						if (ch == '<') {
							string reversedTag = stringBuilder.ToString().Trim();
							if (!reversedTag.StartsWith("/") && !reversedTag.EndsWith("/")) {
								bool validXml = true;
								try {
									XmlDocument doc = new XmlDocument();
									doc.LoadXml(d.TextContent);
								} catch (Exception) {
									validXml = false;
								}
								// only insert the tag, if something is missing
								if (!validXml) {
									StringBuilder tag = new StringBuilder();
									for (int i = reversedTag.Length - 1; i >= 0 && !Char.IsWhiteSpace(reversedTag[i]); --i) {
										tag.Append(reversedTag[i]);
									}
									string tagString = tag.ToString();
									if (tagString.Length > 0) {
										d.Insert(caretOffset, "</" + tagString + ">");
									}
								}
							}
							break;
						}
						stringBuilder.Append(ch);
						--offset;
					}
				}
			} catch (Exception e) { // Insanity check
				Debug.Assert(false, e.ToString());
			}
			return charTyped == '\n' ? IndentLine(d, lineNr) : 0;
		}
	}	
}
