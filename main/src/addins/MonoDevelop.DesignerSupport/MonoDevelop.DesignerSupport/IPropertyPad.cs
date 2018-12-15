
using System;

namespace MonoDevelop.DesignerSupport
{
	public interface IPropertyPad
	{
		bool IsPropertyGridEditing { get; }

		event EventHandler PropertyGridChanged;

		void SetCurrentObject (object lastComponent, object [] propertyProviders);
		void BlankPad ();
		void PopulateGrid (bool saveEditSession);
	}
}
