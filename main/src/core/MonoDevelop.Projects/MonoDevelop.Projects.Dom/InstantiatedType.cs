// InstantiatedType.cs created with MonoDevelop
// User: mkrueger at 23:17 21.10.2008
//
// To change standard headers go to Edit->Preferences->Coding->Standard Headers
//

using System;
using System.Collections.Generic;

namespace MonoDevelop.Projects.Dom
{
	public class InstantiatedType : DomType
	{
		IType             uninstantiatedType;
		IList<IReturnType> genericParameters;
		
		public IList<IReturnType> GenericParameters {
			get {
				return genericParameters;
			}
			set {
				genericParameters = value;
			}
		}

		public IType UninstantiatedType {
			get {
				return uninstantiatedType;
			}
			set {
				uninstantiatedType = value;
			}
		}

		
		public InstantiatedType()
		{
		}
	}
}
