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

using MonoDevelop.Xml.Dom;
using MonoDevelop.Ide.Editor;

namespace MonoDevelop.Xml.Parser
{
	public class XmlRootState : XmlParserState
	{
		protected const int FREE = 0;
		protected const int BRACKET = FREE + 1;
		protected const int BRACKET_EXCLAM = BRACKET + 1;
		protected const int COMMENT = BRACKET_EXCLAM + 1;
		protected const int CDATA = COMMENT + 1;
		protected const int DOCTYPE = CDATA + 1;
		protected const int MAXCONST = DOCTYPE;
		
		XmlTagState tagState;
		XmlClosingTagState closingTagState;
		XmlCommentState commentState;
		XmlCDataState cDataState;
		XmlDocTypeState docTypeState;
		XmlProcessingInstructionState processingInstructionState;
		
		public XmlRootState () : this (new XmlTagState (), new XmlClosingTagState ()) {}
		
		public XmlRootState (XmlTagState tagState, XmlClosingTagState closingTagState)
			: this (tagState, closingTagState, new XmlCommentState (), new XmlCDataState (),
			  new XmlDocTypeState (), new XmlProcessingInstructionState ()) {}
		
		public XmlRootState (
			XmlTagState tagState,
			XmlClosingTagState closingTagState,
			XmlCommentState commentState,
			XmlCDataState cDataState,
			XmlDocTypeState docTypeState,
		        XmlProcessingInstructionState processingInstructionState)
		{
			this.tagState = tagState;
			this.closingTagState = closingTagState;
			this.commentState = commentState;
			this.cDataState = cDataState;
			this.docTypeState = docTypeState;
			this.processingInstructionState = processingInstructionState;
			
			Adopt (this.TagState);
			Adopt (this.ClosingTagState);
			Adopt (this.CommentState);
			Adopt (this.CDataState);
			Adopt (this.DocTypeState);
			Adopt (this.ProcessingInstructionState);
		}
		
		protected XmlTagState TagState { 
			get { return tagState; } 
		}
		
		protected XmlClosingTagState ClosingTagState { 
			get { return closingTagState; } 
		}
		
		protected XmlCommentState CommentState { 
			get { return commentState; }
		}
		
		protected XmlCDataState CDataState { 
			get { return cDataState; } 
		}
		
		protected XmlDocTypeState DocTypeState { 
			get { return docTypeState; }
		}
		
		protected XmlProcessingInstructionState ProcessingInstructionState { 
			get { return processingInstructionState; } 
		}
		
		public override XmlParserState PushChar (char c, IXmlParserContext context, ref string rollback)
		{
			if (c == '<') {
				if (context.StateTag != FREE)
					context.LogError ("Incomplete tag opening; encountered unexpected '<'.",
						new DocumentRegion (
							context.LocationMinus (LengthFromOpenBracket (context) + 1),
							context.LocationMinus (1)));
				context.StateTag = BRACKET;
				return null;
			}
			
			switch (context.StateTag) {
			case FREE:
				//FIXME: handle entities?
				return null;
				
			case BRACKET:
				if (c == '?') {
					rollback = string.Empty;
					return this.ProcessingInstructionState;
				} else if (c == '!') {
					context.StateTag = BRACKET_EXCLAM;
					return null;
				} else if (c == '/') {
					return this.ClosingTagState;
				} else if (char.IsLetter (c) || c == '_' || char.IsWhiteSpace (c)) {
					rollback = string.Empty;
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
					return CDataState;
				}
				context.KeywordBuilder.Length = 0;
				break;
				
			case DOCTYPE:
				string docTypeStr = "OCTYPE";
				if (c == docTypeStr [context.KeywordBuilder.Length]) {
					context.KeywordBuilder.Append (c);
					if (context.KeywordBuilder.Length < docTypeStr.Length)
						return null;
					return DocTypeState;
				} else {
					context.KeywordBuilder.Length = 0;
				}
				break;
			}
			
			context.LogError ("Incomplete tag opening; encountered unexpected character '" + c + "'.",
				new DocumentRegion (
					context.LocationMinus (LengthFromOpenBracket (context)),
					context.Location));
			
			context.StateTag = FREE;
			return null;
		}
		
		static int LengthFromOpenBracket (IXmlParserContext context)
		{
			switch (context.StateTag) {
			case BRACKET:
				return 1;
			case BRACKET_EXCLAM:
				return 2;
			case COMMENT:
				return 3;
			case CDATA:
			case DOCTYPE:
				return 3 + context.KeywordBuilder.Length;
			default:
				return 1;
			}
		}
		
		public virtual XDocument CreateDocument ()
		{
			return new XDocument ();
		}
	}
}
