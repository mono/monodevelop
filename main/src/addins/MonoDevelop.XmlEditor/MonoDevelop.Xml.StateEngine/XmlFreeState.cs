// 
// XmlFreeState.cs
// 
// Author:
//   Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
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
using System.Collections.Generic;
using System.Text;

namespace MonoDevelop.Xml.StateEngine
{
	public class XmlFreeState : RootState
	{
		const int FREE = 0;
		const int BRACKET = 1;
		const int BRACKET_EXCLAM = 2;
		const int COMMENT = 3;
		const int CDATA = 4;
		const int DOCTYPE = 5;
		
		public XmlFreeState () : this (new XmlTagState (), new XmlClosingTagState ()) {}
		
		public XmlFreeState (XmlTagState tagState, XmlClosingTagState closingTagState)
			: this (tagState, closingTagState, new XmlCommentState (), new XmlCDataState (),
			  new XmlDocTypeState (), new XmlProcessingInstructionState ()) {}
		
		public XmlFreeState (
			XmlTagState tagState,
			XmlClosingTagState closingTagState,
			XmlCommentState commentState,
			XmlCDataState cDataState,
			XmlDocTypeState docTypeState,
		        XmlProcessingInstructionState processingInstructionState)
		{
			this.TagState = tagState;
			this.ClosingTagState = closingTagState;
			this.CommentState = commentState;
			this.CDataState = cDataState;
			this.DocTypeState = docTypeState;
			this.ProcessingInstructionState = processingInstructionState;
			
			Adopt (this.TagState);
			Adopt (this.ClosingTagState);
			Adopt (this.CommentState);
			Adopt (this.CDataState);
			Adopt (this.DocTypeState);
			Adopt (this.ProcessingInstructionState);
		}
		
		protected XmlTagState TagState { get; private set; }
		protected XmlClosingTagState ClosingTagState { get; private set; }
		protected XmlCommentState CommentState { get; private set; }
		protected XmlCDataState CDataState { get; private set; }
		protected XmlDocTypeState DocTypeState { get; private set; }
		protected XmlProcessingInstructionState ProcessingInstructionState { get; private set; }
		
		public override State PushChar (char c, IParseContext context, ref bool reject)
		{
			switch (context.StateTag) {
			case FREE:
				if (c == '<') {
					context.StateTag = BRACKET;
				}
				//FIXME: handle entities?
				return null;
				
			case BRACKET:
				if (c == '?') {
					reject = true;
					return this.ProcessingInstructionState;
				} else if (c == '!') {
					context.StateTag = BRACKET_EXCLAM;
					return null;
				} else if (c == '/') {
					return this.ClosingTagState;
				} else if (char.IsLetter (c) || c == '_') {
					reject = true;
					return TagState;
				}
				break;
				
			case BRACKET_EXCLAM:
				if (c == '[') {
					context.StateTag = CDATA;
					return null;
				} else if (c == '-') {
					context.StateTag = COMMENT;
					return null;
				} else if (c == 'D') {
					context.StateTag = DOCTYPE;
					return null;
				}
				break;
			
			case COMMENT:
				if (c == '-')
					return CommentState;
				break;
				
			case CDATA:
				string cdataStr = "CDATA[";
				if (c == cdataStr [context.KeywordBuilder.Length]) {
					context.KeywordBuilder.Append (c);
					if (context.KeywordBuilder.Length < cdataStr.Length)
						return null;
					else
						return CDataState;
				} else {
					context.KeywordBuilder.Length = 0;
				}
				break;
				
			case DOCTYPE:
				string docTypeStr = "OCTYPE";
				if (c == docTypeStr [context.KeywordBuilder.Length]) {
					context.KeywordBuilder.Append (c);
					if (context.KeywordBuilder.Length < docTypeStr.Length)
						return null;
					else
						return DocTypeState;
				} else {
					context.KeywordBuilder.Length = 0;
				}
				break;
			}
			
			context.LogError ("Unexpected character '" + c + "' in tag opening.");
			context.StateTag = FREE;
			return null;
		}
		
		public override XDocument CreateDocument ()
		{
			return new XDocument ();
		}
	}
}
