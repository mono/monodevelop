//
// AbstractParser.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
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
using System.IO;

namespace MonoDevelop.Projects.Dom.Parser
{
	public abstract class AbstractParser : IParser
	{
		string projectType;
		string[] mimeTypes;
		string[] lexerTags;
		
		protected AbstractParser (string projectType, params string[] mimeTypes)
		{
			this.projectType = projectType;
			this.mimeTypes   = mimeTypes;
		}
		
		public ParsedDocument Parse (ProjectDom dom, string fileName)
		{
			return Parse (dom, fileName, System.IO.File.ReadAllText (fileName));
		}
		
		public virtual IExpressionFinder CreateExpressionFinder (ProjectDom dom)
		{
			return null;
		}
		
		public virtual IResolver CreateResolver (ProjectDom dom, object editor, string fileName)
		{
			return null;
		}
		
		public abstract ParsedDocument Parse (ProjectDom dom, string fileName, string content);
		
		public virtual ParsedDocument Parse (ProjectDom dom, string fileName, TextReader content)
		{
			return Parse (dom, fileName, content.ReadToEnd ());
		}
		
		public virtual bool CanParseMimeType (string mimeType)
		{
			if (mimeTypes == null)
				return false;
			foreach (string canParseType in mimeTypes) {
				if (canParseType == mimeType)
					return true;
			}
			return false;
		}
		
		public virtual bool CanParseProjectType (string projectType)
		{
			return this.projectType == projectType;
		}
		
		public virtual bool CanParse (string fileName)
		{
			return false;
//			return CanParseMimeType (IdeApp.Services.PlatformService.GetMimeTypeForUri (fileName));
		}

		public string[] LexerTags {
			get {
				return lexerTags;
			}
			set {
				lexerTags = value;
			}
		}
	}
}
