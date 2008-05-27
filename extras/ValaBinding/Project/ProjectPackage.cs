//
// ProjectPackage.cs: A pkg-config package
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

using Mono.Addins;

using MonoDevelop.Projects.Serialization;

namespace MonoDevelop.ValaBinding
{
	public class ProjectPackage
	{
		[ItemProperty ("file")]
		private string file;
		
		[ItemProperty ("name")]
		private string name;
		
		[ItemProperty ("IsProject")]
		private bool is_project;
		
		public ProjectPackage (string file)
		{
			this.file = file;
			this.name = file;
			this.is_project = false;
		}
		
		public ProjectPackage (ValaProject project)
		{
			name = project.Name;
			file = Path.Combine (project.BaseDirectory, name + ".md.pc");
			is_project = true;
		}
		
		public ProjectPackage ()
		{
		}
		
		public string File {
			get { return file; }
			set { file = value; }
		}
		
		public string Name {
			get { return name; }
			set { name = value; }
		}
		
		public bool IsProject {
			get { return is_project; }
			set { is_project = value; }
		}
		
		public override bool Equals (object o)
		{
			ProjectPackage other = o as ProjectPackage;
			
			if (other == null) return false;
			
			return other.File.Equals (file);
		}
		
		public override int GetHashCode ()
		{
			return (file + name).GetHashCode ();
		}
	}
}
