// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>
using System.IO;
using System.Collections;
using System.Xml;

using MonoDevelop.Internal.Templates;
using MonoDevelop.Gui;

namespace MonoDevelop.Internal.Templates
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
		
		public string BinPath {
			get {
				return combinePath + Path.DirectorySeparatorChar + "bin";
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
