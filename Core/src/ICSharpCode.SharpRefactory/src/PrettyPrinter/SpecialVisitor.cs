// SpecialVisitor.cs
// Copyright (C) 2003 Mike Krueger (mike@icsharpcode.net)
// 
// This program is free software; you can redistribute it and/or
// modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; either version 2
// of the License, or (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.

using System;
using System.Collections;
using System.Diagnostics;

using ICSharpCode.SharpRefactory.Parser;
using ICSharpCode.SharpRefactory.Parser.AST;

namespace ICSharpCode.SharpRefactory.PrettyPrinter
{
//	
//	public class SpecialVisitor
//	{
//		PrettyPrintData prettyPrintData;
//		public SpecialVisitor(PrettyPrintData prettyPrintData)
//		{
//			this.prettyPrintData = prettyPrintData;
//		}
//		
//		public void VisitSpecial(object obj)
//		{
//			VisitSpecial(obj, true);
//		}
//		public void VisitSpecial(object obj, bool indent)
//		{
//			/*
//			if (obj == null) {
//				return;
//			}
//			ArrayList specials = (ArrayList)obj;
//			foreach (object special in specials) {
//				if (special is BlankLine) {
//					prettyPrintData.AppendNewLine();
//				} else if (special is PreProcessingDirective) {
//					PreProcessingDirective preProcessingDirective = (PreProcessingDirective)special;
//					prettyPrintData.AppendText("#");
//					prettyPrintData.AppendText(preProcessingDirective.Cmd);
//					if (preProcessingDirective.Arg != null && preProcessingDirective.Arg.Length > 0) {
//						prettyPrintData.AppendText(" ");
//						prettyPrintData.AppendText(preProcessingDirective.Arg);
//					}
//					prettyPrintData.AppendNewLine();
//				} else if (special is Comment) {
//					Comment comment = (Comment)special;
//					switch (comment.CommentType) {
//						case CommentType.SingleLine:
//							if (indent) {
//								prettyPrintData.AppendIndentation();
//							}
//							prettyPrintData.AppendText("// ");
//							prettyPrintData.AppendText(comment.CommentText);
//							if (indent) {
//								prettyPrintData.AppendNewLine();
//							}
//							break;
//						case CommentType.Documentation:
//							if (indent) {
//								prettyPrintData.AppendIndentation();
//							}
//							prettyPrintData.AppendText("/// ");
//							prettyPrintData.AppendText(comment.CommentText);
//							prettyPrintData.AppendNewLine();
//							break;
//						case CommentType.Block:
//							if (indent) {
//								prettyPrintData.AppendIndentation();
//							}
//							prettyPrintData.AppendText("* ");
//							prettyPrintData.AppendText(comment.CommentText);
//							prettyPrintData.AppendText(" *");
//							if (indent) {
//								prettyPrintData.AppendNewLine();
//							}
//							break;
//					}
//				}
//			}
//		}
//	}*/
}
