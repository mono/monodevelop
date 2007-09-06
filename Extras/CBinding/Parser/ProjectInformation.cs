//
// ProjectInformation.cs
//
// Authors:
//   Marcos David Marin Amador <MarcosMarin@gmail.com>
//
// Copyright (C) 2007 Marcos David Marin Amador
//
//
// This source code is licenced under The MIT License:
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

using MonoDevelop.Projects;

using CBinding.Navigation;

namespace CBinding.Parser
{
	public class FileInformation
	{
		protected Project project;
		
		protected List<Namespace> namespaces = new List<Namespace> ();
		protected List<Function> functions = new List<Function> ();
		protected List<Class> classes = new List<Class> ();
		protected List<Structure> structures = new List<Structure> ();
		protected List<Member> members = new List<Member> ();
		protected List<Variable> variables = new List<Variable> ();
		protected List<Macro> macros = new List<Macro> ();
		protected List<Enumeration> enumerations = new List<Enumeration> ();
		protected List<Enumerator> enumerators = new List<Enumerator> ();
		protected List<Union> unions = new List<Union> ();
		protected List<Typedef> typedefs = new List<Typedef> ();
		
		private string file_name;
		private bool is_filled = false;
		
		public FileInformation (Project project)
		{
			this.project = project;
			this.file_name = null;
		}
		
		public FileInformation (Project project, string filename)
		{
			this.project = project;
			this.file_name = filename;
		}
		
		public void Clear ()
		{
			namespaces.Clear ();
			functions.Clear ();
			classes.Clear ();
			structures.Clear ();
			members.Clear ();
			variables.Clear ();
			macros.Clear ();
			enumerations.Clear ();
			enumerators.Clear ();
			unions.Clear ();
			typedefs.Clear ();
		}
		
		public IEnumerable<LanguageItem> Containers ()
		{
			foreach (Namespace n in namespaces)
				yield return n;
			
			foreach (Class c in classes)
				yield return c;
			
			foreach (Structure s in structures)
				yield return s;
			
			foreach (Enumeration e in enumerations)
				yield return e;
			
			foreach (Union u in unions)
				yield return u;
		}
		
		// All items except macros
		public IEnumerable<LanguageItem> AllItems ()
		{
			foreach (Namespace n in namespaces)
				yield return n;
			
			foreach (Class c in classes)
				yield return c;
			
			foreach (Structure s in structures)
				yield return s;
			
			foreach (Enumeration e in enumerations)
				yield return e;
			
			foreach (Union u in unions)
				yield return u;
			
			foreach (Function f in functions)
				yield return f;
			
			foreach (Member m in members)
				yield return m;
			
			foreach (Variable v in variables)
				yield return v;
			
			foreach (Enumerator e in enumerators)
				yield return e;
			
			foreach (Typedef t in typedefs)
				yield return t;
		}
		
		public Project Project {
			get { return project; }
		}
		
		public List<Namespace> Namespaces {
			get { return namespaces; }
		}
		
		public List<Function> Functions {
			get { return functions; }
		}
		
		public List<Class> Classes {
			get { return classes; }
		}
		
		public List<Structure> Structures {
			get { return structures; }
		}
		
		public List<Member> Members {
			get { return members; }
		}
		
		public List<Variable> Variables {
			get { return variables; }
		}
		
		public List<Macro> Macros {
			get { return macros; }
		}
		
		public List<Enumeration> Enumerations {
			get { return enumerations; }
		}
		
		public List<Enumerator> Enumerators {
			get { return enumerators; }
		}
		
		public List<Union> Unions {
			get { return unions; } 
		}
		
		public List<Typedef> Typedefs {
			get { return typedefs; }
		}
		
		public string FileName {
			get { return file_name; }
			set { file_name = value; }
		}
		
		public bool IsFilled {
			get { return is_filled; }
			set { is_filled = value; }
		}
	}
	
	public class ProjectInformation : FileInformation
	{
		private Globals globals;
		private MacroDefinitions macroDefs;
		
		private Dictionary<string, List<FileInformation>> includedFiles = new Dictionary<string, List<FileInformation>> ();
		
		public ProjectInformation (Project project) : base (project)
		{
			globals = new Globals (project);
			macroDefs = new MacroDefinitions (project);
		}
		
		public Globals Globals {
			get { return globals; }
		}
		
		public MacroDefinitions MacroDefinitions {
			get { return macroDefs; }
		}
		
		public Dictionary<string, List<FileInformation>> IncludedFiles {
			get { return includedFiles; }
		}
	}
}
