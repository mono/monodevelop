
using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

using Gtk;

using MonoDevelop.Ide.Gui;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Ambience;
using MonoDevelop.Projects.Parser;

namespace MonoDevelop.SourceEditor
{
	public class LanguageItemWindow: Gtk.Window
	{
		static ConversionFlags WindowConversionFlags = ConversionFlags.StandardConversionFlags | ConversionFlags.IncludePangoMarkup;
		
		static string paramStr = GettextCatalog.GetString ("Parameter");
		static string localStr = GettextCatalog.GetString ("Local variable");
		static string fieldStr = GettextCatalog.GetString ("Field");
		static string propertyStr = GettextCatalog.GetString ("Property");
		
		public LanguageItemWindow (ILanguageItem item, IParserContext ctx, Ambience ambience,
		                           string errorInformations) : base (WindowType.Popup)
		{
			Name = "gtk-tooltips";
			
			// Approximate value for usual case
			StringBuilder s = new StringBuilder(150);
			
			if (item != null) {
				if (item is IParameter) {
					s.Append ("<small><i>");
					s.Append (paramStr);
					s.Append ("</i></small>\n");
					s.Append (ambience.Convert ((IParameter)item, WindowConversionFlags));
				} else if (item is LocalVariable) {
					s.Append ("<small><i>");
					s.Append (localStr);
					s.Append ("</i></small>\n");
					s.Append (ambience.Convert ((LocalVariable)item, WindowConversionFlags));
				} else if (item is IField) {				
					s.Append ("<small><i>");
					s.Append (fieldStr);
					s.Append ("</i></small>\n");
					s.Append (ambience.Convert ((IField)item, WindowConversionFlags));
				} else if (item is IProperty) {				
					s.Append ("<small><i>");
					s.Append (propertyStr);
					s.Append ("</i></small>\n");
					s.Append (ambience.Convert ((IProperty)item, WindowConversionFlags));
				} else if (item is Namespace) {
					s.Append ("namespace <b>");
					s.Append (item.Name);
					s.Append ("</b>");
				} else
					s.Append (ambience.Convert (item, WindowConversionFlags));
				
				string doc = GetDocumentation (item.Documentation).Trim ('\n');
				if (!string.IsNullOrEmpty (doc)) {
					s.Append ("\n<small>");
					s.Append (doc);
					s.Append ("</small>");
				}
			}			
			
			if (!string.IsNullOrEmpty (errorInformations)) {
				if (s.Length != 0)
					s.Append ("\n\n");
				s.Append ("<small>");
				s.Append (errorInformations);
				s.Append ("</small>");
			}
			
			Label lab = new Label ();
			lab.Markup = s.ToString ();
			lab.Xalign = 0;
			lab.Xpad = 3;
			lab.Ypad = 3;
			Add (lab);
		}
		
		protected override bool OnExposeEvent (Gdk.EventExpose ev)
		{
			base.OnExposeEvent (ev);
			Gtk.Requisition req = SizeRequest ();
			Gtk.Style.PaintFlatBox (this.Style, this.GdkWindow, Gtk.StateType.Normal, Gtk.ShadowType.Out, Gdk.Rectangle.Zero, this, "tooltip", 0, 0, req.Width, req.Height);
			return true;
		}
		
		public static string GetDocumentation (string doc)
		{
			System.IO.StringReader reader = new System.IO.StringReader("<docroot>" + doc + "</docroot>");
			XmlTextReader xml   = new XmlTextReader(reader);
			StringBuilder ret   = new StringBuilder(70);
			
			try {
				xml.Read();
				do {
					if (xml.NodeType == XmlNodeType.Element) {
						string elname = xml.Name.ToLower();
						if (elname == "remarks") {
							ret.Append("Remarks:\n");
						// skip <example>-nodes
						} else if (elname == "example") {
							xml.Skip();
							xml.Skip();							
						} else if (elname == "exception") {
							ret.Append("Exception: " + GetCref(xml["cref"]) + ":\n");
						} else if (elname == "returns") {
							ret.Append("Returns: ");
						} else if (elname == "see") {
							ret.Append(GetCref(xml["cref"]) + xml["langword"]);
						} else if (elname == "seealso") {
							ret.Append("See also: " + GetCref(xml["cref"]) + xml["langword"]);
						} else if (elname == "paramref") {
							ret.Append(xml["name"]);
						} else if (elname == "param") {
							ret.Append(xml["name"].Trim() + ": ");
						} else if (elname == "value") {
							ret.Append("Value: ");
						}
					} else if (xml.NodeType == XmlNodeType.EndElement) {
						string elname = xml.Name.ToLower();
						if (elname == "para" || elname == "param") {
							ret.Append("\n");
						}
					} else if (xml.NodeType == XmlNodeType.Text) {
						ret.Append(xml.Value);
					}
				} while (xml.Read ());
			} catch (Exception ex) {
				LoggingService.LogError (ex.ToString ());
				return doc;
			}
			return ret.ToString ();
		}
		
		static string GetCref (string cref)
		{
			if (cref == null)
				return "";
			
			if (cref.Length < 2)
				return cref;
			
			if (cref.Substring(1, 1) == ":")
				return cref.Substring (2, cref.Length - 2);
			
			return cref;
		}
	}
}
