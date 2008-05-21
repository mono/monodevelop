//
// CustomMakefile.cs
//
// Author:
//   Lluis Sanchez Gual
//   Ankit Jain <jankit@novell.com>
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

using MonoDevelop.Core;
using System.Text.RegularExpressions;
using MonoDevelop.Projects;

namespace MonoDevelop.Autotools
{
	public class CustomMakefile
	{
		string content;
		//FIXME: Improve the regex
		static string multilineMatch = @"(((?<content>.*)(?<!\\)\n)|((?<content>.*?)\s*\\\n([ \t]*(?<content>.*?)\s*\\\n)*[ \t]*(?<content>.*?)(?<!\\)\n))";
		string fileName;
		
		Dictionary<string, List<string>> varToValuesDict;
		List<string> dirtyVariables;

		public CustomMakefile (string file)
		{
			this.fileName = file;
			if (!File.Exists (file))
				throw new FileNotFoundException (file);

			StreamReader sr = new StreamReader (file);
			content = sr.ReadToEnd ();
			sr.Close ();
		}
		
		//This is absolute path
		public string FileName {
			get { return fileName; }
		}
		
		public string Content {
			get { return content; }
		}
		
		Dictionary<string, List<string>> VarToValuesDict {
			get {
				if (varToValuesDict == null)
					InitVarToValuesDict ();
				return varToValuesDict;
			}
		}

		List<string> DirtyVariables {
			get {
				if (dirtyVariables == null)
					dirtyVariables = new List<string> ();
				return dirtyVariables;
			}
		}

		static Regex varRegex = null;
		static Regex VariablesRegex {
			get {
				if (varRegex == null)
					varRegex = new Regex(@"[.|\n]*^(?<varname>[a-zA-Z_0-9]*)((?<sep>[ \t]*:?=[ \t]*$)|((?<sep>\s*:?=\s*)" +
						multilineMatch + "))", RegexOptions.Multiline);
				return varRegex;
			}
		}

		public ICollection<string> GetVariables ()
		{
			return VarToValuesDict.Keys;
		}

		string GetVariable (string var)
		{
			List<string> list = GetListVariable (var);
			if (list == null)
				return null;
			
			StringBuilder sb = new StringBuilder ();
			sb.AppendFormat ("{0} =", var);
			if (list.Count > 1) {
				sb.Append (" ");
				foreach (string s in list)
					sb.AppendFormat (" \\\n\t{0}", s);
			} else if (list.Count == 1) {
				sb.Append (" " + list [0]);
			}

			return sb.ToString ();
		}

		public List<string> GetListVariable (string var)
		{
			if (!VarToValuesDict.ContainsKey (var))
				return null;

			return VarToValuesDict [var];
		}

		public void SetListVariable (string var, List<string> val)
		{
			if (!VarToValuesDict.ContainsKey (var))
				return;

			VarToValuesDict [var] = val;
			if (!DirtyVariables.Contains (var))
				DirtyVariables.Add (var);
		}

		void InitVarToValuesDict ()
		{
			varToValuesDict = new Dictionary<string, List<string>> ();
			foreach (Match m in VariablesRegex.Matches (content)) {
				if (!m.Success)
					continue;

				string varname = m.Groups ["varname"].Value;
				if (String.IsNullOrEmpty (varname))
					continue;

				List<string> list = new List<string> ();
				foreach (Capture c in m.Groups["content"].Captures) {
					string val = c.Value.Trim ();
					if (val.Length == 0)
						continue;

					foreach (string s in val.Split (new char [] {' ', '\t'}, StringSplitOptions.RemoveEmptyEntries)) {
						if (s.Length > 0)
							list.Add (s);
					}
				}

				varToValuesDict [varname] = list;
			}
		}
	
		public string GetTarget (string var)
		{
			//FIXME: //FILES = \
			//\tabc.cs ---> the \t is not a must.. and there can be multiple \t's
			Regex targetExp = new Regex(@"[.|\n]*^" + var + @"(?<sep>\s*:\s*)" + multilineMatch + @"\t" + multilineMatch, 
				RegexOptions.Multiline);
			return GetValue (var, targetExp);
		}
		
		string GetValue (string var, Regex exp)
		{
			Match match = exp.Match (content);
			if (!match.Success) return null;
			string value = "";
			foreach (Capture c in match.Groups["content"].Captures)
				value += c.Value;

			return value;
		}

		public void AppendValueToVar (string var, string val)
		{
			List<string> list = GetListVariable (var);
			if (list == null)
				ThrowMakefileVarNotFound (var);

			list.Add (val);
		}

		public void SetListVariable (string var, IEnumerable<string> list)
		{
			//Set only if the variable exists in the makefile
			if (GetVariable (var) == null)
				ThrowMakefileVarNotFound (var);

			VarToValuesDict [var] = new List<string> (list);
		}

		public void ClearVariableValue (string var)
		{
			if (GetVariable (var) == null)
				return;
				//ThrowMakefileVarNotFound (var);
			VarToValuesDict [var].Clear ();
		}

		void SaveVariable (string var)
		{
			//FIXME: Make this static
			Regex varExp = new Regex(@"[.|\n]*^(?<var>" + var + @"((?<sep>\s*:?=\s*\n)|((?<sep>\s*:?=\s*)" + multilineMatch + ")))", 
				RegexOptions.Multiline);
			
			Match match = varExp.Match (content);
			if (!match.Success) 
				return;

			Group grp = match.Groups ["var"];
			int varLength = grp.ToString ().Trim (' ','\n').Length;
			
			//FIXME: Umm too expensive
			content = String.Concat ( 
					content.Substring (0, grp.Index),
					GetVariable (var),
					content.Substring (grp.Index + varLength));
		}

		public void Save ()
		{
			string oldContent = content;
			foreach (string var in DirtyVariables) {
				if (VarToValuesDict [var] != null)
					SaveVariable (var);
			}

			DirtyVariables.Clear ();

			if (String.Compare (oldContent, content) == 0)
				return;

			using (StreamWriter sw = new StreamWriter (fileName))
				sw.Write (content);
			
			FileService.NotifyFileChanged (fileName);
		}
		
		void ThrowMakefileVarNotFound (string var)
		{
			throw new InvalidOperationException (GettextCatalog.GetString (
					"Makefile variable {0} not found in the file.", var));
		}

	}
}
