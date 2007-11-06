//  ProjectCreateInformation.cs
//
//  This file was derived from a file from #Develop. 
//
//  Copyright (C) 2001-2007 Mike Krüger <mkrueger@novell.com>
// 
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
//  
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
using System.IO;
using System.Collections;
using System.Xml;

namespace MonoDevelop.Projects
{
	/// <summary>
	/// This class holds all information the language binding need to create
	/// a predefined project for their language, if no project template for a 
	/// specific language is avaiable, the language binding shouldn't care about
	/// this stuff.
	/// </summary>
	public class ProjectCreateInformation
	{
		string projectName;
		string combineName;
		string combinePath;
		string projectBasePath;
		
		public string ProjectName {
			get {
				return projectName;
			}
			set {
				projectName = value;
			}
		}
		
		public string CombineName {
			get {
				return combineName;
			}
			set {
				combineName = value;
			}
		}
		
		public string BinPath {
			get {
				return Path.Combine(projectBasePath, "bin");
			}
		}
		
		public string CombinePath {
			get {
				return combinePath;
			}
			set {
				combinePath = value;
			}
		}
		
		public string ProjectBasePath {
			get {
				return projectBasePath;
			}
			set {
				projectBasePath = value;
			}
		}
	}
}
