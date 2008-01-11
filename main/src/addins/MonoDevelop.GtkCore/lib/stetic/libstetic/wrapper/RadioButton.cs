using System;
using System.Collections;
using System.Xml;
using System.CodeDom;

namespace Stetic.Wrapper {

	public class RadioButton : CheckButton, IRadioGroupManagerProvider {

		static RadioGroupManager GroupManager = new RadioGroupManager (typeof (Gtk.RadioButton));

		public override void Wrap (object obj, bool initialized)
		{
			base.Wrap (obj, initialized);

			Gtk.RadioButton radiobutton = (Gtk.RadioButton)Wrapped;
			if (!initialized)
				Group = GroupManager.LastGroup;
			else if (radiobutton.Group == null)
				Group = radiobutton.Name;
		}
		
		IRadioGroupManager IRadioGroupManagerProvider.GetGroupManager ()
		{
			return GroupManager;
		}

		protected override void ReadProperties (ObjectReader reader, XmlElement elem)
		{
			bool active = (bool)GladeUtils.ExtractProperty (elem, "active", false);
			string group = (string)GladeUtils.ExtractProperty (elem, "group", "");
			base.ReadProperties (reader, elem);

			if (reader.Format == FileFormat.Glade) {
				if (group != "")
					Group = group;
				else
					Group = Wrapped.Name;
			}
				
			if (active)
				((Gtk.RadioButton)Wrapped).Active = true;
		}

		protected override XmlElement WriteProperties (ObjectWriter writer)
		{
			XmlElement elem = base.WriteProperties (writer);
			if (writer.Format == FileFormat.Glade) {
				string group = GroupManager.GladeGroupName (Wrapped);
				if (group != Wrapped.Name)
					GladeUtils.SetProperty (elem, "group", group);
			}
			return elem;
		}
		
		protected override void GeneratePropertySet (GeneratorContext ctx, CodeExpression var, PropertyDescriptor prop)
		{
			if (prop.Name == "Group") {
				CodeExpression groupExp = GroupManager.GenerateGroupExpression (ctx, (Gtk.Widget) Wrapped);
				ctx.Statements.Add (
					new CodeAssignStatement (
						new CodePropertyReferenceExpression (var, "Group"),
						groupExp)
				);
			}
			else
				base.GeneratePropertySet (ctx, var, prop);
		}

		public string Group {
			get {
				return GroupManager[Wrapped];
			}
			set {
				GroupManager[Wrapped] = value;
				EmitNotify ("Group");
			}
		}
	}
}
