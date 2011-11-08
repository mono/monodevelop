
using System;

namespace MonoDevelop.DesignerSupport
{
	/// <summary>
	/// Property providers can be used to create "proxy" objects which provide editable
	/// properties for a given object. When the designer support service finds a IPropertyPadProvider
	/// implementation, it gets the selected object from that pad provider, and then checks all
	/// registered property providers, looking one which supports the object. If it is found, it
	/// calls CreateProvider and the returned object will be given to the property pad.
	/// </summary>
	public interface IPropertyProvider
	{
		bool SupportsObject (object obj);
		object CreateProvider (object obj);
	}
}
