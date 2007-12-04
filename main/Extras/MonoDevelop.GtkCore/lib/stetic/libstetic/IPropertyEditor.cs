using System;

namespace Stetic
{
	// Property editors must be Gtk Widgets and implement this interface
	
	public interface IPropertyEditor: IDisposable
	{
		// Called once to initialize the editor.
		void Initialize (PropertyDescriptor descriptor);
		
		// Called when the object to be edited changes.
		void AttachObject (object obj);
		
		// Gets/Sets the value of the editor. If the editor supports
		// several value types, it is the responsibility of the editor 
		// to return values with the expected type.
		object Value { get; set; }

		// To be fired when the edited value changes.
		event EventHandler ValueChanged;
	}
}
