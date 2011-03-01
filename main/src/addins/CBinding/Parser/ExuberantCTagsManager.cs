// 
// ExuberantCTagsManager.cs
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
using System.Linq;
using System.Collections.Generic;

using MonoDevelop.Core;
using MonoDevelop.Core.Execution;

namespace CBinding.Parser
{
	public class ExuberantCTagsManager: CTagsManager
	{
		#region implemented abstract members of CBinding.Parser.CTagsManager

		protected override IEnumerable<string> GetTags (FileInformation fileInfo)
		{
			string tagFileName = Path.GetFileName (fileInfo.FileName) + ".tag";
			string tagFullFileName = Path.Combine (SystemTagsDirectory, tagFileName);
			string ctags_kinds = "--C++-kinds=+px";
			string ctags_output = string.Empty;
			
			if (PropertyService.Get<bool> ("CBinding.ParseLocalVariables", true))
				ctags_kinds += "l";
			
			string ctags_options = ctags_kinds + " --fields=+aStisk-fz --language-force=C++ --excmd=number --line-directives=yes -f '" + tagFullFileName + "' '" + fileInfo.FileName + "'";
			
			if (!File.Exists (tagFullFileName) || File.GetLastWriteTimeUtc (tagFullFileName) < File.GetLastWriteTimeUtc (fileInfo.FileName)) {
				GetOutputFromProcess (CTagsExecutable, ctags_options, Environment.CurrentDirectory);
			}
			try {
				ctags_output = File.ReadAllText (tagFullFileName);
			} catch (IOException ioe) {
				LoggingService.LogError ("Error updating system tags", ioe);
			}
			
			return ctags_output.Split (newlines, StringSplitOptions.RemoveEmptyEntries);
		}
		
		public override Tag ParseTag (string tagEntry)
		{
			string file;
			UInt64 line;
			string name;
			string tagField;
			TagKind kind;
			AccessModifier access = AccessModifier.Public;
			string _class = null;
			string _namespace = null;
			string _struct = null;
			string _union = null;
			string _enum = null;
			string signature = null;
			
			int i1 = tagEntry.IndexOf ('\t');
			name = tagEntry.Substring (0, tagEntry.IndexOf ('\t'));
			
			i1 += 1;
			int i2 = tagEntry.IndexOf ('\t', i1);
			file = tagEntry.Substring (i1, i2 - i1);
			
			i1 = i2 + 1;
			i2 = tagEntry.IndexOf (";\"", i1);
			line = UInt64.Parse(tagEntry.Substring (i1, i2 - i1));

			i1 = i2 + 3;	
			kind = (TagKind)tagEntry[i1];
			
			i1 += 2;
			tagField = (tagEntry.Length > i1? tagField = tagEntry.Substring(i1) : String.Empty);
			
			string[] fields = tagField.Split ('\t');
			int index;
			
			foreach (string field in fields) {
				index = field.IndexOf (':');
				
				// TODO: Support friend modifier
				if (index > 0) {
					string key = field.Substring (0, index);
					string val = field.Substring (index + 1);
					
					switch (key) {
						case "access":
							try {
								access = (AccessModifier)System.Enum.Parse (typeof(AccessModifier), val, true);
							} catch (ArgumentException) {
							}
							break;
						case "class":
							_class = val;
							break;
						case "namespace":
							_namespace = val;
							break;
						case "struct":
							_struct = val;
							break;
						case "union":
							_union = val;
							break;
						case "enum":
							_enum = val;
							break;
						case "signature":
							signature = val;
							break;
					}
				}
			}
			
			return new Tag (name, file, line, kind, access, _class, _namespace, _struct, _union, _enum, signature);
		}
		
		static readonly char[] newlines = {'\r','\n'};
		protected override IEnumerable<string> GetTags (MonoDevelop.Projects.Project project, string filename, IEnumerable<string> headers)
		{
			StringBuilder ctags_kinds = new StringBuilder ("--C++-kinds=+px");
			
			if (PropertyService.Get<bool> ("CBinding.ParseLocalVariables", true))
				ctags_kinds.Append ("+l");
			
			// Maybe we should only ask for locals for 'local' files? (not external #includes?)
			ctags_kinds.AppendFormat (" --fields=+aStisk-fz --language-force=C++ --excmd=number --line-directives=yes -f - '{0}'", filename);
			foreach (string header in headers) {
				ctags_kinds.AppendFormat (" '{0}'", header);
			}
			
			string output = GetOutputFromProcess (CTagsExecutable, ctags_kinds.ToString (), project.BaseDirectory);
			if (output != null)
				return output.Split (newlines, StringSplitOptions.RemoveEmptyEntries);
			return null;
		}
		
		#endregion
	}
}

