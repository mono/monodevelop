// 
// BsdCTagsManager.cs
//  
// Author:
//       Levi Bard <levi@unity3d.com>
// 
// Copyright (c) 2010 Levi Bard
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;

using MonoDevelop.Core;
using MonoDevelop.Core.Execution;

namespace CBinding.Parser
{
	public class BsdCTagsManager: CTagsManager
	{
		#region implemented abstract members of CBinding.Parser.CTagsManager
		
		protected override IEnumerable<string> GetTags (FileInformation fileInfo)
		{
			string tagFileName = Path.GetFileName (fileInfo.FileName) + ".tag";
			string tagFullFileName = Path.Combine (SystemTagsDirectory, tagFileName);
			
			string ctags_options = string.Format ("-dtx '{0}'", fileInfo.FileName);
			string ctags_output = string.Empty;
			
			if (!File.Exists (tagFullFileName) || File.GetLastWriteTimeUtc (tagFullFileName) < File.GetLastWriteTimeUtc (fileInfo.FileName)) {
				ctags_output = GetOutputFromProcess (CTagsExecutable, ctags_options, Environment.CurrentDirectory);
				File.WriteAllText (tagFullFileName, ctags_output);
			}
			
			return ctags_output.Split (newlines, StringSplitOptions.RemoveEmptyEntries);
		}
		
		static readonly char[] newlines = {'\r','\n'};
		protected override IEnumerable<string> GetTags (MonoDevelop.Projects.Project project, string filename, IEnumerable<string> headers)
		{
			StringBuilder ctags_kinds = new StringBuilder ("-dtx");
			
			ctags_kinds.AppendFormat (" '{0}'", filename);
			foreach (string header in headers) {
				ctags_kinds.AppendFormat (" '{0}'", header);
			}
			
			string output = GetOutputFromProcess (CTagsExecutable, ctags_kinds.ToString (), project.BaseDirectory);
			if (output != null)
				return output.Split (newlines, StringSplitOptions.RemoveEmptyEntries);
			return null;
		}

		public override void FillFileInformation (FileInformation fileInfo)
		{
			IEnumerable<string> ctags_output = GetTags (fileInfo);
			if (ctags_output == null) return;
			
			foreach (string tagEntry in ctags_output) {
				if (tagEntry.StartsWith ("!_")) continue;
				
				Tag tag = ParseTag (tagEntry);
				
				if (tag != null)
					AddInfo (fileInfo, tag, tagEntry);
			}
			
			fileInfo.IsFilled = true;
		}
		
		
		// Format: symbol line file fulltext (there may not be any whitespace between symbol and line)
		static readonly Regex tagExpression = new Regex (@"\s*(?<symbol>[^\s]+?)\s*(?<line>\d+)\s+(?<file>[^\s]+)\s+(?<raw>.*)", RegexOptions.Compiled);
		
		public override Tag ParseTag (string tagEntry)
		{
			try {
				Match tagMatch = tagExpression.Match (tagEntry);
				if (tagMatch == null) return null;
				
				TagKind kind = TagKind.Member;
				string signature = tagMatch.Groups["raw"].Value;
				int start = signature.IndexOf ('(');
				int end = signature.LastIndexOf (')');
				
				if (start >= 0 && end > start) {
					// Attempt to parse out method parameter block
					signature = signature.Substring (start, end - start + 1);
					kind = TagKind.Function; // TODO: improve kind guessing
				}
				return new Tag (tagMatch.Groups["symbol"].Value,
				                tagMatch.Groups["file"].Value,
				                ulong.Parse (tagMatch.Groups["line"].Value)+1,
				                kind, AccessModifier.Public,
				                null, null, null, null, null, signature);
			} catch (Exception ex) {
				LoggingService.LogWarning (string.Format ("Error parsing tag {0}", tagEntry), ex);
			}
			return null;
		}
		
		#endregion
		
	}
}

