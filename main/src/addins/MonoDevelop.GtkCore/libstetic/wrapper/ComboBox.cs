using System;
using System.Xml;
using System.CodeDom;

namespace Stetic.Wrapper {

	public class ComboBox : Container {

		public static new Gtk.ComboBox CreateInstance ()
		{
			Gtk.ComboBox c = Gtk.ComboBox.NewText ();
			// Make sure all children are created, so the mouse events can be
			// bound and the widget can be selected.
			c.EnsureStyle ();
			return c;
		}
		
		public override void Wrap (object obj, bool initialized)
		{
			base.Wrap (obj, initialized);
			if (!initialized)
				textCombo = true;
		}


		string[] items = new string[0];
		bool textCombo;

		public string[] Items {
			get { return items; }
			set {
				Gtk.ComboBox combobox = (Gtk.ComboBox)Wrapped;
				int active = combobox.Active;

				int row = 0, oi = 0, ni = 0;
				while (oi < items.Length && ni < value.Length) {
					if (items [oi] == value [ni]) {
						oi++;
						ni++;
						row++;
					} else if (ni < value.Length - 1 &&
						   items [oi] == value [ni + 1]) {
						combobox.InsertText (row++, value [ni++]);
						if (active > row)
							active++;
					} else {
						combobox.RemoveText (row);
						if (active > row)
							active--;
						oi++;
					}
				}

				while (oi < items.Length) {
					combobox.RemoveText (row);
					oi++;
				}

				while (ni < value.Length)
					combobox.InsertText (row++, value [ni++]);

				items = value;
				combobox.Active = active;

				EmitNotify ("Items");
			}
		}
		
		public bool IsTextCombo {
			get { return textCombo; }
			set { textCombo = value; EmitNotify ("IsTextCombo"); }
		}
		
		internal protected override CodeExpression GenerateObjectCreation (GeneratorContext ctx)
		{
			if (textCombo) {
				return new CodeMethodInvokeExpression (
					new CodeTypeReferenceExpression ("Gtk.ComboBox"),
					"NewText"
				);
			} else
				return base.GenerateObjectCreation (ctx);
		}
		
		internal protected override void GenerateBuildCode (GeneratorContext ctx, CodeExpression var)
		{
			if (textCombo && Items != null && Items.Length > 0) {
				foreach (string str in Items) {
					ctx.Statements.Add (new CodeMethodInvokeExpression (
						var,
						"AppendText",
						ctx.GenerateValue (str, typeof(string), true)
					));
				}
			}
			
			base.GenerateBuildCode (ctx, var);
		}
		
		public override void Read (ObjectReader reader, XmlElement element)
		{
			base.Read (reader, element);
			if (reader.Format == FileFormat.Glade && items.Length > 0)
				IsTextCombo = true;
		}
	}
}
