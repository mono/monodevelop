// 
// PageInfo.cs
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

namespace MonoDevelop.AspNet.Parser
{
	
	public class PageInfo
	{
		string inheritedClass;
		string codeBehindFile;
		string codeFile;
		string language;
		string docType;
		WebSubtype type = WebSubtype.None;
	
		public string InheritedClass {
			get { return inheritedClass; }
			set { inheritedClass = value; }
		}
		
		public string CodeBehindFile {
			get { return codeBehindFile; }
			set { codeBehindFile = value; }
		}
		
		public string CodeFile {
			get { return codeFile; }
			set { codeFile = value; }
		}
		
		public string Language {
			get { return language; }
			set { language = value; }
		}
		
		public string DocType {
			get { return docType; }
			set { docType = value; }
		}
		
		public WebSubtype Subtype {
			get { return type; }
			set { type = value; }
		}
		
		public string MasterPageFile { get; set; }
		public string MasterPageTypeName { get; set; }
		public string MasterPageTypeVPath { get; set; }
		
		public void SetSubtypeFromDirective (string directiveName)
		{
			switch (directiveName.ToLower ()) {
				case "page":
					type = WebSubtype.WebForm;
					break;
				case "control":
					type = WebSubtype.WebControl;
					break;
				case "webservice":
					type = WebSubtype.WebService;
					break;
				case "webhandler":
					type = WebSubtype.WebHandler;
					break;
				case "application":
					type = WebSubtype.Global;
					break;
				case "master":
					type = WebSubtype.MasterPage;
					break;
				default:
					type = WebSubtype.None;
					break;
			}
		}
		
		public override string ToString ()
		{
			return string.Format("[PageInfo: InheritedClass={0}, CodeBehindFile={1}, CodeFile={2}, Language={3}, " +
			                     "DocType={4}, Subtype={5}, MasterPageFile={6}, MasterPageTypeName={7}, MasterPageTypeVPath={8}]",
			                     InheritedClass, CodeBehindFile, CodeFile, Language, DocType, Subtype, MasterPageFile, 
			                     MasterPageTypeName, MasterPageTypeVPath);
		}

	}
}
