//
// ProjectInformation.cs
//
// Authors:
//  Levi Bard <taktaktaktaktaktaktaktaktaktak@gmail.com> 
//
// Copyright (C) 2008 Levi Bard
// Based on CBinding by Marcos David Marin Amador <MarcosMarin@gmail.com>
//
//    This program is free software: you can redistribute it and/or modify
//    it under the terms of the GNU Lesser General Public License as published by
//    the Free Software Foundation, either version 2 of the License, or
//    (at your option) any later version.
//
//    This program is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//    GNU Lesser General Public License for more details.
//
//    You should have received a copy of the GNU Lesser General Public License
//    along with this program.  If not, see <http://www.gnu.org/licenses/>.
//


using System;
using System.Collections.Generic;

using MonoDevelop.Projects;

namespace MonoDevelop.ValaBinding.Parser
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
		protected List<Local> locals = new List<Local> ();
		
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
		
		// Functions, fields
		public IEnumerable<LanguageItem> InstanceMembers ()
		{
			foreach (Function f in functions)
				yield return f;
			
			foreach (Member m in members)
				yield return m;
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
		
		public List<Local> Locals {
			get { return locals; }
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
//		private Globals globals;
//		private MacroDefinitions macroDefs;
		
		private Dictionary<string, List<FileInformation>> includedFiles = new Dictionary<string, List<FileInformation>> ();
		
		public ProjectInformation (Project project) : base (project)
		{
//			globals = new Globals (project);
//			macroDefs = new MacroDefinitions (project);
		}
		
//		public Globals Globals {
//			get { return globals; }
//		}
//		
//		public MacroDefinitions MacroDefinitions {
//			get { return macroDefs; }
//		}
		
		public Dictionary<string, List<FileInformation>> IncludedFiles {
			get { return includedFiles; }
		}
	}
}
