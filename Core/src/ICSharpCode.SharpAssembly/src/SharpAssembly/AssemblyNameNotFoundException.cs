// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Georg Brandl" email="g.brandl@gmx.net"/>
//     <version value="$version"/>
// </file>

using System;

namespace MonoDevelop.SharpAssembly.Assembly
{
	/// <summary>
	/// Is thrown when the given assembly name could not be found.
	/// </summary>
	public class AssemblyNameNotFoundException : Exception
	{
		public AssemblyNameNotFoundException(string name) : base("Could not find assembly named " + name + " in the Global Assembly Cache.")
		{
		}
	}
}
