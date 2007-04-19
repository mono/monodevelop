
using System;

namespace MonoDevelop.DesignerSupport
{
	public interface IPropertyProvider
	{
		bool SupportsObject (object obj);
		object CreateProvider (object obj);
	}
}
