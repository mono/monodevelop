
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
		public LanguageItemWindow (ILanguageItem item, IParserContext ctx) : base (WindowType.Popup)
		{
			string s;
			
			if (item is IMethod)
				s = PangoAmbience.Convert ((IMethod)item);
			else if (item is IField)
				s = PangoAmbience.Convert ((IField)item);
			else if (item is IProperty)
				s = PangoAmbience.Convert ((IProperty)item);
			else if (item is IIndexer)
				s = PangoAmbience.Convert ((IIndexer)item);
			else if (item is IClass)
				s = PangoAmbience.Convert ((IClass)item);
			else if (item is IEvent)
				s = PangoAmbience.Convert ((IEvent)item);
			else if (item is IParameter) {
				s = "<small><i>" + GettextCatalog.GetString ("Parameter") + "</i></small>\n";
				s += PangoAmbience.Convert ((IParameter)item);
			}
			else if (item is LocalVariable) {
				LocalVariable var = (LocalVariable) item;
				s = "<small><i>" + GettextCatalog.GetString ("Local variable") + "</i></small>\n" + var.ReturnType.FullyQualifiedName + " " + var.Name;
			} else if (item is Namespace)
				s = item.Name;
			else
				s = item.Name;

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
		
		static IAmbience PangoAmbience
		{
			get {
				IAmbience asvc = MonoDevelop.Projects.Services.Ambience.CurrentAmbience;
				asvc.ConversionFlags |= ConversionFlags.IncludePangoMarkup;
				asvc.ConversionFlags &= ~ConversionFlags.ShowInheritanceList;
				return asvc;
			}
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
