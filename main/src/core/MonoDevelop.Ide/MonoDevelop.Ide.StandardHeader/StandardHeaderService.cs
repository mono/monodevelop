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
using MonoDevelop.Ide.Gui.Content;

namespace MonoDevelop.Ide.StandardHeader
{
	public static class StandardHeaderService
	{
		public static string GetHeader (SolutionFolderItem policyParent, string fileName, bool newFile)
		{
			StandardHeaderPolicy headerPolicy = policyParent != null ? policyParent.Policies.Get<StandardHeaderPolicy> () : MonoDevelop.Projects.Policies.PolicyService.GetDefaultPolicy<StandardHeaderPolicy> ();
			TextStylePolicy textPolicy = policyParent != null ? policyParent.Policies.Get<TextStylePolicy> ("text/plain") : MonoDevelop.Projects.Policies.PolicyService.GetDefaultPolicy<TextStylePolicy> ("text/plain");
			AuthorInformation authorInfo = policyParent != null ? policyParent.AuthorInformation : AuthorInformation.Default;
			
			return GetHeader (authorInfo, headerPolicy, textPolicy, fileName, newFile);
		}
		
		public static string GetHeader (AuthorInformation authorInfo, StandardHeaderPolicy policy, TextStylePolicy textPolicy,
		                                string fileName, bool newFile)
		{
			string[] comment = Document.GetCommentTags (fileName);
			if (comment == null)
				return "";
			
			if (string.IsNullOrEmpty (policy.Text) || (newFile && !policy.IncludeInNewFiles))
				return "";
			
			string result;
			string eolMarker = TextStylePolicy.GetEolMarker (textPolicy.EolMarker);
			
			if (comment.Length == 1) {
				string cmt = comment[0];
				//make sure there's a space between the comment char and the license text
				if (!char.IsWhiteSpace (cmt[cmt.Length - 1]))
					cmt = cmt + " ";
				
				StringBuilder sb = StringBuilderCache.Allocate ();
				string[] lines = policy.Text.Split ('\n');
				foreach (string line in lines) {
					if (string.IsNullOrWhiteSpace (line)) {
						sb.Append (cmt.TrimEnd ());
						sb.Append (eolMarker);
						continue;
					}
					sb.Append (cmt);
					sb.Append (line);
					sb.Append (eolMarker);
				}
				result = StringBuilderCache.ReturnAndFree (sb);
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
