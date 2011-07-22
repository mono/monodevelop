// 
// CTagsManager.cs
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
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;

using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;

namespace CBinding.Parser
{
	public abstract class CTagsManager
	{
		public abstract Tag ParseTag (string tagEntry);
		protected abstract IEnumerable<string> GetTags (FileInformation fileInfo);
		protected abstract IEnumerable<string> GetTags (Project project, string filename, IEnumerable<string> headers);
		
		public static readonly string SystemTagsDirectory = UserProfile.Current.CacheDir.Combine ("CTagsData");
		
		public static string CTagsExecutable {
			get { return PropertyService.Get<string> ("CBinding.CTagsExecutable", "ctags"); }
		}
		
		static CTagsManager ()
		{
			try {
				FileService.EnsureDirectoryExists (SystemTagsDirectory);
			} catch (IOException ioe) {
				LoggingService.LogError ("Error creating system tags directory", ioe);
			}
		}
		
		protected virtual void AddInfo (FileInformation info, Tag tag, string ctags_output)
		{
			switch (tag.Kind)
			{
			case TagKind.Class:
				Class c = new Class (tag, info.Project, ctags_output);
				if (!info.Classes.Contains (c))
					info.Classes.Add (c);
				break;
			case TagKind.Enumeration:
				Enumeration e = new Enumeration (tag, info.Project, ctags_output);
				if (!info.Enumerations.Contains (e))
					info.Enumerations.Add (e);
				break;
			case TagKind.Enumerator:
				Enumerator en= new Enumerator (tag, info.Project, ctags_output);
				if (!info.Enumerators.Contains (en))
					info.Enumerators.Add (en);
				break;
			case TagKind.ExternalVariable:
				break;
			case TagKind.Function:
				Function f = new Function (tag, info.Project, ctags_output);
				if (!info.Functions.Contains (f))
					info.Functions.Add (f);
				break;
			case TagKind.Local:
				Local lo = new Local (tag, info.Project, ctags_output);
				if(!info.Locals.Contains (lo))
					info.Locals.Add (lo);
				break;
			case TagKind.Macro:
				Macro m = new Macro (tag, info.Project);
				if (!info.Macros.Contains (m))
					info.Macros.Add (m);
				break;
			case TagKind.Member:
				Member me = new Member (tag, info.Project, ctags_output);
				if (!info.Members.Contains (me))
					info.Members.Add (me);
				break;
			case TagKind.Namespace:
				Namespace n = new Namespace (tag, info.Project, ctags_output);
				if (!info.Namespaces.Contains (n))
					info.Namespaces.Add (n);
				break;
			case TagKind.Prototype:
				Function fu = new Function (tag, info.Project, ctags_output);
				if (!info.Functions.Contains (fu))
					info.Functions.Add (fu);
				break;
			case TagKind.Structure:
				Structure s = new Structure (tag, info.Project, ctags_output);
				if (!info.Structures.Contains (s))
					info.Structures.Add (s);
				break;
			case TagKind.Typedef:
				Typedef t = new Typedef (tag, info.Project, ctags_output);
				if (!info.Typedefs.Contains (t))
					info.Typedefs.Add (t);
				break;
			case TagKind.Union:
				Union u = new Union (tag, info.Project, ctags_output);
				if (!info.Unions.Contains (u))
					info.Unions.Add (u);
				break;
			case TagKind.Variable:
				Variable v = new Variable (tag, info.Project);
				if (!info.Variables.Contains (v))
					info.Variables.Add (v);
				break;
			default:
				break;
			}
		}
		
		public virtual void FillFileInformation (FileInformation fileInfo)
		{
			IEnumerable<string> ctags_output = GetTags (fileInfo);
			
			foreach (string tagEntry in ctags_output) {
				if (tagEntry.StartsWith ("!_")) continue;
				
				Tag tag = ParseTag (tagEntry);
				
				if (tag != null)
					AddInfo (fileInfo, tag, tagEntry);
			}
			
			fileInfo.IsFilled = true;
		}
		
		public virtual void DoUpdateFileTags (MonoDevelop.Projects.Project project, string filename, IEnumerable<string> headers)
		{
			ProjectInformation info = ProjectInformationManager.Instance.Get (project);
			
			lock (info) {
				info.RemoveFileInfo (filename);
	
				IEnumerable<string> tags = GetTags (project, filename, headers);
				if (tags == null) return;
				
				foreach (string tagEntry in tags) {
					if (tagEntry.StartsWith ("!_")) continue;
					
					Tag tag = ParseTag (tagEntry);
					
					if (tag != null)
						AddInfo (info, tag, tagEntry);
				}
			}
		}
		
		public static string GetOutputFromProcess (string executable, string args, string baseDirectory)
		{
			string processOutput = null;
			ProcessWrapper p = null;
			StringWriter output = null,
			             error = null;
			try {
				output = new StringWriter ();
				error = new StringWriter ();
				
				p = Runtime.ProcessService.StartProcess (executable, args, baseDirectory, output, error, null);
				p.WaitForOutput (10000);
				
				if (p.HasExited) {
					processOutput = output.ToString ();
				}
			} finally {
				if (p != null)
					p.Dispose ();
				if (output != null)
					output.Dispose ();
			}
			
			return processOutput;
		}
	}
}

