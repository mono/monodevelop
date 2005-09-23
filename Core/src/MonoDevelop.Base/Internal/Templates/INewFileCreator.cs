// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Xml;
using System.Diagnostics;

using MonoDevelop.Internal.Project;

namespace MonoDevelop.Internal.Templates
{
	internal interface INewFileCreator
	{
		bool IsFilenameAvailable(string fileName);
		
		void SaveFile(string filename, string content, string languageName, bool showFile);
	}
}
