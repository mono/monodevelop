// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>
using System;
using System.Reflection;

namespace MonoDevelop.Internal.Parser
{
	public interface IProperty : IMember
	{
		IRegion BodyRegion {
			get;
		}
		
		IRegion GetterRegion {
			get;
		}

		IRegion SetterRegion {
			get;
		}

		bool CanGet {
			get;
		}

		bool CanSet {
			get;
		}
		
		IMethod GetterMethod {
			get;
		}

		IMethod SetterMethod {
			get;
		}
		
		ParameterCollection Parameters {
			get;
		}
	}
}
