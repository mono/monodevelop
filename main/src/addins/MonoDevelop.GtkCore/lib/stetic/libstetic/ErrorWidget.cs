using System;
using System.Xml;
using System.CodeDom;
using Mono.Unix;

namespace Stetic
{
	// This widget is shown in place of widgets with unknown classes. 
	
	public class ErrorWidget: Gtk.Frame
	{
		readonly string className;
		readonly Exception exc;
		
		public ErrorWidget (Exception ex, string id)
		{
			exc = ex;
			Init (Catalog.GetString ("Load Error:") + " " + ex.Message, id);
		}
		
		public ErrorWidget (string className, string id)
		{
			this.className = className;
			Init (Catalog.GetString ("Unknown widget:") + " " + className, id);
		}
		
		public ErrorWidget (string className, string minGtkVersion, string foundGtkVersion, string id)
		{
			this.className = className;
			Init (string.Format (Catalog.GetString ("Widget '{0}' not available in GTK# {1}"), className, foundGtkVersion), id);
		}
		
		void Init (string message, string id)
		{
			Gtk.Label lab = new Gtk.Label ();
			lab.Markup = "<b><span foreground='red'>" + message + "</span></b>";
			this.CanFocus = false;
			Add (lab);
			this.ShadowType = Gtk.ShadowType.In;
			ShowAll ();
			if (id != null && id.Length > 0)
				Name = id;
		}
		
		public string ClassName {
			get { return className; }
		}
		
		public Exception Exception {
			get { return exc; }
		}
	}
	
	internal class ErrorWidgetWrapper: Wrapper.Widget
	{
		XmlElement elementData;
		FileFormat format;
		
		public override void Read (ObjectReader reader, XmlElement elem)
		{
			elementData = elem;
			this.format = reader.Format;
		}

		public override XmlElement Write (ObjectWriter writer)
		{
			if (writer.Format != this.format) {
				ErrorWidget ew = (ErrorWidget) Wrapped;
				XmlElement elem = writer.XmlDocument.CreateElement ("widget");
				elem.SetAttribute ("class", "Gtk.Label");
				elem.SetAttribute ("id", Wrapped.Name);
				XmlElement ce = writer.XmlDocument.CreateElement ("property");
				string msg;
				if (ew.Exception != null)
					msg = "Invalid widget";
				else
					msg = "Unknown widget: " + ew.ClassName;
				ce.SetAttribute ("name", "LabelProp");
				ce.InnerText = msg;
				elem.AppendChild (ce);
				return elem;
			}
			else
				return (XmlElement) writer.XmlDocument.ImportNode (elementData, true);
		}
		
		public override string WrappedTypeName {
			get {
				ErrorWidget ew = (ErrorWidget) Wrapped;
				return ew.ClassName;
			}
		}
		
		internal protected override CodeExpression GenerateObjectCreation (GeneratorContext ctx)
		{
			ErrorWidget ew = (ErrorWidget) Wrapped;
			string msg;
			if (ew.Exception != null)
				msg = Project.FileName + ": Could not generate code for an invalid widget. The widget failed to load: " + ew.Exception.Message + ". The generated code may be invalid.";
			else
				msg = Project.FileName + ": Could not generate code for widgets of type: " + ew.ClassName + ". The widget could not be found in any referenced library. The generated code may be invalid.";
			
			if (ctx.Options.FailForUnknownWidgets) {
				throw new InvalidOperationException (msg);
			} else {
				ctx.ReportWarning (msg);
				return new CodePrimitiveExpression (null);
			}
		}
	}
}
