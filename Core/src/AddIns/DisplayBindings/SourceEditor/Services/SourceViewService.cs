using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;
using GtkSourceView;

using MonoDevelop.Core.Services;
using MonoDevelop.Services;
using MonoDevelop.DefaultEditor;

namespace MonoDevelop.Services
{
	public class SourceViewService : AbstractService
	{
		SourceLanguagesManager slm;
		static readonly string file = Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.Personal), ".config/MonoDevelop/SyntaxHighlighting.xml");

		public SourceViewService ()
		{
			slm = new SourceLanguagesManager ();
		}

		public SourceLanguage FindLanguage (string name)
		{
			foreach (SourceLanguage sl in AvailableLanguages)
			{
				if (sl.Name == name)
					return sl;
			}
			// not found
			return null;
		}

		public SourceLanguage GetLanguageFromMimeType (string mimetype)
		{
			return slm.GetLanguageFromMimeType (mimetype);
		}

		public override void InitializeService ()
		{
			base.InitializeService ();
			
			if (!File.Exists (file))
				return;

			// restore values
			XmlTextReader reader = new XmlTextReader (file);
			SourceLanguage lang = null;

			try {
				while (reader.Read ()) {
					if (reader.IsStartElement ()) {
						switch (reader.Name) {
							case "SourceTag":
								if (lang == null) continue;
								string name = reader.GetAttribute ("name");
								SourceTagStyle sts = lang.GetTagStyle (name);
								sts.Bold = bool.Parse (reader.GetAttribute ("bold"));
								sts.Italic = bool.Parse (reader.GetAttribute ("italic"));
								sts.Underline = bool.Parse (reader.GetAttribute ("underline"));
								sts.Strikethrough = bool.Parse (reader.GetAttribute ("strikethrough"));
								sts.IsDefault = false;
								ParseColor (reader.GetAttribute ("foreground"), ref sts.Foreground);
								ParseColor (reader.GetAttribute ("background"), ref sts.Background);
								lang.SetTagStyle (name, sts);
								Runtime.LoggingService.InfoFormat ("Overrode style {0} {1}", lang.Name, name);
								break;
							case "SourceLanguage":
								lang = FindLanguage (reader.GetAttribute ("name"));
								break;
							case "SyntaxHighlighting":
							default:
								break;
						}
					}
				}
			}
			catch (XmlException e) {
				Runtime.LoggingService.Warn (e.ToString ());
			}
			reader.Close ();
		}

		void ParseColor (string color, ref Gdk.Color col)
		{
			if (color.StartsWith ("rgb:")) {
				string[] parts = color.Substring (4).Split ('/');
				col.Red = ushort.Parse (parts[0], NumberStyles.HexNumber);
				col.Green = ushort.Parse (parts[1], NumberStyles.HexNumber);
				col.Blue = ushort.Parse (parts[2], NumberStyles.HexNumber);
			}
			else {
				Gdk.Color.Parse (color, ref col);
			}
		}

		public SourceLanguage RestoreDefaults (SourceLanguage lang)
		{
			foreach (SourceTag tag in lang.Tags)
			{
				lang.SetTagStyle (tag.Name, lang.GetTagDefaultStyle (tag.Name));
			}
			return lang;
		}

		public override void UnloadService ()
		{
			XmlTextWriter writer = new XmlTextWriter (file, Encoding.UTF8);
			writer.Formatting = Formatting.Indented;
			writer.WriteStartDocument ();
			writer.WriteStartElement (null, "SyntaxHighlighting", null);

			foreach (SourceLanguage sl in slm.AvailableLanguages)
			{
				writer.WriteStartElement (null, "SourceLanguage", null);
				writer.WriteStartAttribute (null, "name", null);
					writer.WriteString (sl.Name);
				writer.WriteEndAttribute ();

				foreach (SourceTag tag in sl.Tags)
				{
					if (tag.TagStyle.IsDefault)
						continue;
					writer.WriteStartElement (null, "SourceTag", null);

					writer.WriteStartAttribute (null, "name", null);
						writer.WriteString (tag.Id);
					writer.WriteEndAttribute ();

					writer.WriteStartAttribute (null, "bold", null);
						writer.WriteString (tag.TagStyle.Bold.ToString ());
					writer.WriteEndAttribute ();

					writer.WriteStartAttribute (null, "italic", null);
						writer.WriteString (tag.TagStyle.Italic.ToString ());
					writer.WriteEndAttribute ();

					writer.WriteStartAttribute (null, "underline", null);
						writer.WriteString (tag.TagStyle.Underline.ToString ());
					writer.WriteEndAttribute ();

					writer.WriteStartAttribute (null, "strikethrough", null);
						writer.WriteString (tag.TagStyle.Strikethrough.ToString ());
					writer.WriteEndAttribute ();

					writer.WriteStartAttribute (null, "foreground", null);
						writer.WriteString (tag.TagStyle.Foreground.ToString ());
					writer.WriteEndAttribute ();

					writer.WriteStartAttribute (null, "background", null);
						writer.WriteString (tag.TagStyle.Background.ToString ());
					writer.WriteEndAttribute ();

					writer.WriteEndElement ();
					Runtime.LoggingService.InfoFormat ("Preserved style {0} {1}", sl.Name, tag.Id);
				}

				writer.WriteEndElement ();
			}

			writer.WriteEndElement ();
			writer.WriteEndDocument ();
			writer.Flush ();
			writer.Close ();

			base.UnloadService ();
		}

		public SourceLanguage[] AvailableLanguages {
			get { return slm.AvailableLanguages; }
		}
	}
}

