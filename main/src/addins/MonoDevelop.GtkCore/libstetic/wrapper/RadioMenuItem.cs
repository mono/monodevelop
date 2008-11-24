using System;
using System.Collections;
using System.Xml;
using System.CodeDom;

namespace Stetic.Wrapper {

	public class RadioMenuItem : MenuItem, IRadioGroupManagerProvider {

		static RadioGroupManager GroupManager = new RadioGroupManager (typeof (Gtk.RadioMenuItem));

		public override void Wrap (object obj, bool initialized)
		{
			base.Wrap (obj, initialized);

			Gtk.RadioMenuItem radio = (Gtk.RadioMenuItem)Wrapped;
			if (!initialized)
				Group = GroupManager.LastGroup;
			else if (radio.Group == null)
				Group = radio.Name;
		}
		
		IRadioGroupManager IRadioGroupManagerProvider.GetGroupManager ()
		{
			return GroupManager;
		}

		protected override void ReadProperties (ObjectReader reader, XmlElement elem)
		{
			string group = (string)GladeUtils.ExtractProperty (elem, "group", "");
			bool active = (bool)GladeUtils.ExtractProperty (elem, "active", false);
			base.ReadProperties (reader, elem);

			if (group != "")
				Group = group;
			else
				Group = Wrapped.Name;
			if (active)
				((Gtk.RadioMenuItem)Wrapped).Active = true;
		}

		protected override XmlElement WriteProperties (ObjectWriter writer)
		{
			XmlElement elem = base.WriteProperties (writer);
			string group = GroupManager.GladeGroupName (Wrapped);
			if (group != Wrapped.Name)
				GladeUtils.SetProperty (elem, "group", group);
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
