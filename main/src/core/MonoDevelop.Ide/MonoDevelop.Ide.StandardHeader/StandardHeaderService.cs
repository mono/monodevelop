//
// StandardHeaderService.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//   Michael Hutchinson <mhutchinson@novell.com>
//
// Copyright (C) 2007, 2009 Novell, Inc (http://www.novell.com)
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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Xml;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.Ide.StandardHeader
{
	public static class StandardHeaderService
	{
	
		static string[] GetComment (string language)
		{
			ILanguageBinding binding = LanguageBindingService.GetBindingPerLanguageName (language);
			if (binding != null) {
				if (!String.IsNullOrEmpty (binding.SingleLineCommentTag))
					return new string[] { binding.SingleLineCommentTag };
				if (!String.IsNullOrEmpty (binding.BlockCommentStartTag) && !String.IsNullOrEmpty (binding.BlockCommentEndTag))
					return new string[] { binding.BlockCommentStartTag, binding.BlockCommentEndTag };
			}
			return null;
		}
		
		public static string GetHeader (SolutionItem policyParent, string language, string fileName, bool newFile)
		{
			StandardHeaderPolicy policy = policyParent != null
				? policyParent.Policies.Get<StandardHeaderPolicy> ()
				: MonoDevelop.Projects.Policies.PolicyService.GetDefaultPolicy<StandardHeaderPolicy> ();
			AuthorInformation authorInfo = IdeApp.Workspace.GetAuthorInformation (policyParent);
			
			return GetHeader (authorInfo, policy, language, fileName, newFile);
		}
		
		public static string GetHeader (AuthorInformation authorInfo, StandardHeaderPolicy policy,
		                                string language, string fileName, bool newFile)
		{
			string[] comment = GetComment (language);
			if (comment == null)
				return "";
				
			if (string.IsNullOrEmpty (policy.Text) || (newFile && !policy.IncludeInNewFiles))
				return "";
			
			string result;
			
			if (comment.Length == 1) {
				string cmt = comment[0];
				//make sure there's a space between the comment char and the license text
				if (!char.IsWhiteSpace (cmt[cmt.Length -1]))
					cmt = cmt + " ";
			
				StringBuilder sb = new StringBuilder (policy.Text.Length);
				string[] lines = policy.Text.Split ('\n');
				foreach (string line in lines) {
					sb.Append (cmt);
					sb.Append (line);
					// the text editor should take care of conversions to preferred newline char
					sb.Append ('\n');
				}
				result = sb.ToString ();
			} else {
				//multiline comment
				result = String.Concat (comment[0], "\n", policy.Text, "\n", comment[1], "\n");
				
			}
			
			return StringParserService.Parse (result, new string[,] { 
				{ "FileName", Path.GetFileName (fileName) }, 
				{ "FileNameWithoutExtension", Path.GetFileNameWithoutExtension (fileName) }, 
				{ "Directory", Path.GetDirectoryName (fileName) }, 
				{ "FullFileName", fileName },
				{ "AuthorName", authorInfo.Name },
				{ "AuthorEmail", authorInfo.Email },
				{ "CopyrightHolder", authorInfo.Copyright },
			});
		}
	}
}
