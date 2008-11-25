using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;
using GtkSourceView;

using MonoDevelop.Core;
using MonoDevelop.SourceEditor;

namespace MonoDevelop.SourceEditor
{
	public static class SourceViewService
	{
		public static SourceLanguage FindLanguage (string name)
		{
			foreach (SourceLanguage sl in AvailableLanguages)
			{
				if (sl.Name == name)
					return sl;
			}
			// not found
			return null;
		}

		public static SourceLanguage GetLanguageFromMimeType (string mimetype)
		{
			foreach (SourceLanguage sl in AvailableLanguages) {
				string[] supportedMimeTypes = sl.MimeTypes;
				if (supportedMimeTypes != null)
					foreach (string mt in supportedMimeTypes)
						if (mt == mimetype)
							return sl;
			}
			return null;
		}
		
		public static SourceLanguageManager LanguageManager {
			get {
				return SourceLanguageManager.Default;
			}
		}
		
		public static SourceStyleSchemeManager StyleSchemeManager {
			get {
				return SourceStyleSchemeManager.Default;
			}
		}
		
		public static IEnumerable<SourceLanguage> AvailableLanguages {
			get {
				foreach (string id in LanguageManager.LanguageIds)
					yield return LanguageManager.GetLanguage (id);
			}
		}
		
		public static IEnumerable<SourceStyleScheme> AvailableStyleSchemes {
			get {
				foreach (string id in StyleSchemeManager.SchemeIds)
					yield return StyleSchemeManager.GetScheme (id);
			}
		}
	}
}

