using System;
using System.CodeDom;
using System.Collections;
using System.Xml;

namespace Stetic.Wrapper {

	public class Button : Container {
		
		ImageInfo imageInfo;
		
		public override void Wrap (object obj, bool initialized)
		{
			base.Wrap (obj, initialized);

			if (!initialized)
				UseUnderline = true;

			if (button.UseStock) {
				type = ButtonType.StockItem;
				StockId = button.Label;
			} else if (!initialized) {
				type = ButtonType.TextOnly;
				Label = button.Name;
			} else if (button.Child is Gtk.Label) {
				type = ButtonType.TextOnly; 
				label = button.Label;
				useUnderline = button.UseUnderline;
			} else {
				type = ButtonType.Custom;
				FixupGladeChildren ();
			}
		}

		public override void Read (ObjectReader reader, XmlElement elem)
		{
			base.Read (reader, elem);
			if (reader.Format == FileFormat.Glade)
				UseUnderline = true;
		}
		protected override ObjectWrapper ReadChild (ObjectReader reader, XmlElement child_elem)
		{
			ObjectWrapper ret = null;
			if (Type == ButtonType.Custom || reader.Format == FileFormat.Glade) {
				if (button.Child != null)
					button.Remove (button.Child);
				ret = base.ReadChild (reader, child_elem);
				FixupGladeChildren ();
			} else if (Type == ButtonType.TextAndIcon) {
				UpdateImage ();
			}
			return ret;
		}

		protected override XmlElement WriteChild (ObjectWriter writer, Widget wrapper)
		{
			if (writer.Format == FileFormat.Glade || Type == ButtonType.Custom)
				return base.WriteChild (writer, wrapper);
			else
				return null;
		}
		
		void FixupGladeChildren ()
		{
			Gtk.Alignment alignment = button.Child as Gtk.Alignment;
			if (alignment == null)
				return;
			Gtk.HBox box = alignment.Child as Gtk.HBox;
			if (box == null)
				return;

			Gtk.Widget[] children = box.Children;
			if (children == null || children.Length != 2)
				return;

			Gtk.Image image = children[0] as Gtk.Image;
			Gtk.Label label = children[1] as Gtk.Label;
			if (image == null || label == null)
				return;
			Stetic.Wrapper.Image iwrap = Stetic.ObjectWrapper.Lookup (image) as Stetic.Wrapper.Image;
			if (iwrap == null)
				return;

			this.label = label.LabelProp;
			button.UseUnderline = label.UseUnderline;

			imageInfo = iwrap.Pixbuf;
			Type = ButtonType.TextAndIcon;
		}

		protected override XmlElement WriteProperties (ObjectWriter writer)
		{
			XmlElement elem = base.WriteProperties (writer);
			if (Type == ButtonType.StockItem)
				GladeUtils.SetProperty (elem, "label", stockId);
			return elem;
		}

		public override IEnumerable RealChildren {
			get {
				if (type == ButtonType.Custom)
					return base.RealChildren;
				else
					return new Gtk.Widget[0];
			}
		}

		public override IEnumerable GladeChildren {
			get {
				if (type == ButtonType.StockItem || type == ButtonType.TextOnly)
					return new Gtk.Widget[0];
				else
					return base.GladeChildren;
			}
		}

		private Gtk.Button button {
			get {
				return (Gtk.Button)Wrapped;
			}
		}

		public enum ButtonType {
			StockItem,
			TextOnly,
			TextAndIcon,
			Custom
		};

		ButtonType type;
		public ButtonType Type {
			get {
				return type;
			}
			set {
				type = value;
				EmitNotify ("Type");
				switch (type) {
				case ButtonType.StockItem:
					button.UseStock = true;
					StockId = stockId;
					break;
				case ButtonType.TextOnly:
					button.UseStock = false;
					Label = label;
					UseUnderline = useUnderline;
					break;
				case ButtonType.TextAndIcon:
					button.UseStock = false;
					Label = label;
					UseUnderline = useUnderline;
					break;
				case ButtonType.Custom:
					button.UseStock = false;
					if (button.Child != null)
						ReplaceChild (button.Child, CreatePlaceholder (), true);
					break;
				}
				if (!Loading) {
					UpdateImage ();
				}
			}
		}

		public ImageInfo Icon {
			get { return imageInfo; }
			set { 
				imageInfo = value;
				if (!Loading) {
					UpdateImage ();
					EmitNotify ("Image");
				}
			}
		}

		protected override void OnEndRead (FileFormat format)
		{
			base.OnEndRead (format);
			if (format == FileFormat.Native && Type == ButtonType.TextAndIcon) {
				Loading = true;
				UpdateImage ();
				Loading = false;
			}
		}
		
		void UpdateImage ()
		{
			if (type != ButtonType.TextAndIcon) {
				button.Image = null;
				return;
			}
			var imageWidget = button.Image as Gtk.Image;
			if (imageWidget == null) {
				button.Image = imageWidget = (Gtk.Image)Registry.NewInstance ("Gtk.Image", proj);
				// force them to display even if hidden by the theme
				button.Image.Show ();
			}
			Image imageWrapper = (Image)Widget.Lookup (imageWidget);
			imageWrapper.Unselectable = true;
			imageWrapper.Pixbuf = imageInfo;
		}

		string stockId = Gtk.Stock.Ok;
		public string StockId {
			get {
				return stockId;
			}
			set {
				if (responseId == ResponseIdForStockId (stockId))
					responseId = 0;

				if (value != null) {
					string sid = value;
					if (sid.StartsWith ("stock:"))
						sid = sid.Substring (6);
					button.Label = stockId = sid;
					button.UseStock = true;
					Gtk.StockItem item = Gtk.Stock.Lookup (sid);
					if (item.StockId == sid) {
						label = item.Label;
						useUnderline = true;
					}
				} else {
					stockId = value;
				}
				
				EmitNotify ("StockId");

				if (responseId == 0)
					ResponseId = ResponseIdForStockId (stockId);
			}
		}

		string label;
		public string Label {
			get {
				return label;
			}
			set {
				label = value;
				button.Label = value;
			}
		}

		bool useUnderline;
		public bool UseUnderline {
			get {
				return useUnderline;
			}
			set {
				useUnderline = value;
				button.UseUnderline = value;
			}
		}

		public bool IsDialogButton {
			get {
				ButtonBox box = this.ParentWrapper as ButtonBox;
				return (box != null && box.InternalChildProperty != null && box.InternalChildProperty.Name == "ActionArea");
			}
		}

		int responseId;
		public int ResponseId {
			get {
				return responseId;
			}
			set {
				responseId = value;
				EmitNotify ("ResponseId");
			}
		}

		int ResponseIdForStockId (string stockId)
		{
			if (stockId == Gtk.Stock.Ok)
				return (int)Gtk.ResponseType.Ok;
			else if (stockId == Gtk.Stock.Cancel)
				return (int)Gtk.ResponseType.Cancel;
			else if (stockId == Gtk.Stock.Close)
				return (int)Gtk.ResponseType.Close;
			else if (stockId == Gtk.Stock.Yes)
				return (int)Gtk.ResponseType.Yes;
			else if (stockId == Gtk.Stock.No)
				return (int)Gtk.ResponseType.No;
			else if (stockId == Gtk.Stock.Apply)
				return (int)Gtk.ResponseType.Apply;
			else if (stockId == Gtk.Stock.Help)
				return (int)Gtk.ResponseType.Help;
			else
				return 0;
		}
		
		internal protected override void GenerateBuildCode (GeneratorContext ctx, CodeExpression var)
		{
			base.GenerateBuildCode (ctx, var);

			string text = button.Label;
			if (!string.IsNullOrEmpty (text)) {
				CodePropertyReferenceExpression cprop = new CodePropertyReferenceExpression (var, "Label");
				PropertyDescriptor prop = (PropertyDescriptor)this.ClassDescriptor ["Label"];
				bool trans = Type != ButtonType.StockItem && prop.IsTranslated (Wrapped);
				CodeExpression val = ctx.GenerateValue (text, typeof(string), trans);
				ctx.Statements.Add (new CodeAssignStatement (cprop, val));
			}

			if (Type == ButtonType.TextAndIcon) {
				var imageWidget = (Gtk.Image) button.Image;
				if (imageWidget != null) {
					Image imageWrapper = (Image)Widget.Lookup (imageWidget);
					var imgVar = ctx.GenerateNewInstanceCode (imageWrapper);
					var imgProp = new CodePropertyReferenceExpression (var, "Image");
					ctx.Statements.Add (new CodeAssignStatement (imgProp, imgVar));
				}
			}
		}
		
		protected override void GeneratePropertySet (GeneratorContext ctx, CodeExpression var, PropertyDescriptor prop)
		{
			if (prop.Name != "Label")
				base.GeneratePropertySet (ctx, var, prop);
		}
	}
}
