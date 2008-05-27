//
// LanguageItem.cs
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
using System.IO;

using MonoDevelop.Core.Gui;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.ValaBinding.Parser
{
	public class LanguageItem
	{
		private Project project;
		private string name;
		private string file;
		private string pattern;
		private AccessModifier access;
		private LanguageItem parent;
		
		public LanguageItem (Tag tag, Project project)
		{
			this.project = project;
			this.name = tag.Name;
			this.file = tag.File;
			this.pattern = tag.Pattern;
			this.access = tag.Access;
		}
		
		/// <summary>
		/// Attempts to get the namespace encompassing the function
		/// <remarks>
		/// If it's a method then, even if the class it belongs to
		/// has a namespace, the method will not have a namespace, since
		/// it should be placed under the class node and not the namespace node
		/// </remarks>
		/// <returns>true on success and false if it does not have one.</returns>
		/// </summary>
		protected bool GetNamespace (Tag tag, string ctags_output)
		{
			string n;
			
			if ((n = tag.Namespace) != null) {
				int index = n.LastIndexOf (':');
				
				if (index > 0)
					n = n.Substring (index + 1);
				
				try {
					Tag namespaceTag = TagDatabaseManager.Instance.FindTag (
					    n, TagKind.Namespace, ctags_output);
					
					if (namespaceTag != null)
						parent = new Namespace (namespaceTag, project, ctags_output);
					
				} catch (IOException ex) {
					MessageService.ShowException (ex);
				}
				
				return true;
			}
			
			return false;
		}
		
		protected bool GetClass (Tag tag, string ctags_output)
		{
			string c;
			
			if ((c = tag.Class) != null) {
				int index = c.LastIndexOf (':');
				
				if (index > 0)
					c = c.Substring (index + 1);
				
				try {
					Tag classTag = TagDatabaseManager.Instance.FindTag (
					    c, TagKind.Class, ctags_output);
					
					if (classTag != null)
						parent = new Class (classTag, project, ctags_output);
					
				} catch (IOException ex) {
					MessageService.ShowException (ex);
				}
				
				return true;
			}
			
			return false;
		}
		
		protected bool GetStructure (Tag tag, string ctags_output)
		{
			string s;
			
			if ((s = tag.Structure) != null) {
				int index = s.LastIndexOf (':');
				
				if (index > 0)
					s = s.Substring (index + 1);
				
				try {
					Tag classTag = TagDatabaseManager.Instance.FindTag (
					    s, TagKind.Structure, ctags_output);
					
					if (classTag != null)
						parent = new Structure (classTag, project, ctags_output);
					
				} catch (IOException ex) {
					MessageService.ShowException (ex);
				}
				
				return true;
			}
			
			return false;
		}
		
		protected bool GetEnumeration (Tag tag, string ctags_output)
		{
			string e;
			
			if ((e = tag.Enum) != null) {
				int index = e.LastIndexOf (':');
				
				if (index > 0)
					e = e.Substring (index + 1);
				
				try {
					Tag enumTag = TagDatabaseManager.Instance.FindTag (
					    e, TagKind.Enumeration, ctags_output);
					
					if (enumTag != null)
						parent = new Enumeration (enumTag, project, ctags_output);
					
				} catch (IOException ex) {
					MessageService.ShowException (ex);
				}
				
				return true;
			}
			
			return false;
		}
		
		protected bool GetUnion (Tag tag, string ctags_output)
		{
			string u;
			
			if ((u = tag.Union) != null) {
				int index = u.LastIndexOf (':');
				
				if (index > 0)
					u = u.Substring (index + 1);
				
				try {
					Tag unionTag = TagDatabaseManager.Instance.FindTag (
					    u, TagKind.Union, ctags_output);
					
					if (unionTag != null)
						parent = new Union (unionTag, project, ctags_output);
					
				} catch (IOException ex) {
					MessageService.ShowException (ex);
				}
				
				return true;
			}
			
			return false;
		}
		
		public Project Project {
			get { return project; }
		}
		
		public LanguageItem Parent {
			get { return parent; }
			set { parent = value; }
		}
		
		public string Name {
			get { return name; }
		}
		
		public string FullName {
			get {
				if (Parent != null)
					return Parent.FullName + "." + Name;
				return Name;
			}
		}
		
		public string File {
			get { return file; }
		}
		
		public string Pattern {
			get { return pattern; }
		}
		
		public AccessModifier Access {
			get { return access; }
			set { access = value; }
		}
		
		public override bool Equals (object o)
		{
			LanguageItem other = o as LanguageItem;
			
			if (other != null &&
			    other.FullName.Equals (FullName) &&
			    other.Project.Equals (project))
				return true;
			
			return false;
		}
		
		public override int GetHashCode ()
		{
			return (name + file + pattern + project.Name).GetHashCode ();
		}
	}
}
