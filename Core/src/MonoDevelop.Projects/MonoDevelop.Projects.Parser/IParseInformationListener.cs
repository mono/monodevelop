// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Drawing.Printing;
using MonoDevelop.Projects.Parser;
using MonoDevelop.Core;

namespace MonoDevelop.Projects.Parser
{
	public interface IParseInformationListener
	{
		void ParseInformationUpdated(IParseInformation parseInfo);
	}
}

