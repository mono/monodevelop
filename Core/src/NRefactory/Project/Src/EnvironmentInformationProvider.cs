// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Daniel Grunwald" email="daniel@danielgrunwald.de"/>
//     <version>$Revision: 1965 $</version>
// </file>

using System;

namespace ICSharpCode.NRefactory
{
	public interface IEnvironmentInformationProvider
	{
		bool HasField(string fullTypeName, string fieldName);
	}
	
	class DummyEnvironmentInformationProvider : IEnvironmentInformationProvider
	{
		public bool HasField(string fullTypeName, string fieldName)
		{
			return false;
		}
	}
}
