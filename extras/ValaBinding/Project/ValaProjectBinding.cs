//
// ValaProjectBinding.cs: binding for a ValaProject
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


using System.Xml;
using System.IO;

using Mono.Addins;

using MonoDevelop.Projects;

namespace MonoDevelop.ValaBinding
{
	public class ValaProjectBinding : IProjectBinding
	{
		public string Name {
			get { return "Vala"; }
		}
		
		public Project CreateProject (ProjectCreateInformation info,
									  XmlElement projectOptions)
		{
			string language = projectOptions.GetAttribute ("language");
			return new ValaProject (info, projectOptions, language);
		}
		
		public Project CreateSingleFileProject (string sourceFile)
		{
			ProjectCreateInformation info = new ProjectCreateInformation ();
			info.ProjectName = Path.GetFileNameWithoutExtension (sourceFile);
			info.CombinePath = Path.GetDirectoryName (sourceFile);
			info.ProjectBasePath = Path.GetDirectoryName (sourceFile);
			
			string language = "Vala";
			
			Project project =  new ValaProject (info, null, language);
			project.Files.Add (new ProjectFile (sourceFile));
			return project;
		}
		
		public bool CanCreateSingleFileProject (string sourceFile)
		{
			return true;
		}
	}
}
