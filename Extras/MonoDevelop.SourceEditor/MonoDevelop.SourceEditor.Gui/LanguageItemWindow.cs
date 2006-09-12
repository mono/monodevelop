
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
		
		public LanguageItemWindow (ILanguageItem item, IParserContext ctx, Ambience ambience) : base (WindowType.Popup)
		{
			string s;
			
			if (item is IParameter) {
				s = "<small><i>" + GettextCatalog.GetString ("Parameter") + "</i></small>\n";
				s += ambience.Convert((IParameter)item, WindowConversionFlags);
			}
			else if (item is LocalVariable) {
				s = "<small><i>" + GettextCatalog.GetString ("Local variable") + "</i></small>\n";
				s += ambience.Convert((LocalVariable)item, WindowConversionFlags);
			}
			else if (item is IField) {				
				s = "<small><i>" + GettextCatalog.GetString ("Field") + "</i></small>\n";
				s += ambience.Convert((IField)item, WindowConversionFlags);
			}
			else if (item is IProperty) {				
				s = "<small><i>" + GettextCatalog.GetString ("Property") + "</i></small>\n";
				s += ambience.Convert((IProperty)item, WindowConversionFlags);
			}
			else if (item is Namespace)
				s = "namespace " + "<b>" + item.Name + "</b>";
			else
				s = ambience.Convert(item, WindowConversionFlags);

			string doc = GetDocumentation (item.Documentation).Trim ('\n');
			if (doc.Length > 0)
				s += "\n<small>" + doc + "</small>";
			
			Label lab = new Label ();
			lab.Markup = s;
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
			StringBuilder ret   = new StringBuilder();
			Regex whitespace    = new Regex(@"(\s|\n)+", RegexOptions.Singleline);
			
			try {
				xml.Read();
				do {
					if (xml.NodeType == XmlNodeType.Element) {
						string elname = xml.Name.ToLower();
						if (elname == "remarks") {
							ret.Append("Remarks:\n");
						} else if (elname == "example") {
							ret.Append("Example:\n");
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
						ret.Append(whitespace.Replace(xml.Value, " "));
					}
				} while (xml.Read ());
			} catch {
				Console.WriteLine ("DocBoom");
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
