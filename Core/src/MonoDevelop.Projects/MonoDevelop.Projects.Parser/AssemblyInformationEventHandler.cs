
using System;

namespace MonoDevelop.Projects.Parser
{
	public delegate void AssemblyInformationEventHandler (object sender, AssemblyInformationEventArgs args);
	
	public class AssemblyInformationEventArgs
	{
		readonly string assemblyFile;
		readonly string assemblyName;
		
		public AssemblyInformationEventArgs (string assemblyFile, string assemblyName)
		{
			this.assemblyFile = assemblyFile;
		}
		
		public string AssemblyFile {
			get { return assemblyFile; }
		}
		
		public string AssemblyName {
			get { return assemblyName; }
		}
	}
	
}
