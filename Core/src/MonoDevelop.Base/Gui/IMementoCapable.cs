// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using MonoDevelop.Core.Properties;

namespace MonoDevelop.Gui
{
	/// <summary>
	/// This interface flags an object beeing "mementocapable". This means that the
	/// state of the object could be saved to an <see cref="IXmlConvertable"/> object
	/// and set from a object from the same class.
	/// This is used to save and restore the state of GUI objects.
	/// </summary>
	public interface IMementoCapable
	{
		/// <summary>
		/// Creates a new memento from the state.
		/// </summary>
		IXmlConvertable CreateMemento();
		
		/// <summary>
		/// Sets the state to the given memento.
		/// </summary>
		void SetMemento(IXmlConvertable memento);
	}
}
