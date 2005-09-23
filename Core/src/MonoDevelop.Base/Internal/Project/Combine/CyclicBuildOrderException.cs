// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

namespace MonoDevelop.Internal.Project
{
	public class CyclicBuildOrderException : System.Exception
	{
		string[] cycle = null;
		
		public string[] Cycle {
			get {
				return cycle;
			}
		}
		
		public CyclicBuildOrderException()
		{
		}
		public CyclicBuildOrderException(string[] cycle)
		{
			this.cycle = cycle;
		}
	}
}
