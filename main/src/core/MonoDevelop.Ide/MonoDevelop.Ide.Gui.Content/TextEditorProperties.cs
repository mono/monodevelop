using System;
using System.Text;

using Pango;

using MonoDevelop.Core;

namespace MonoDevelop.Ide.Gui.Content {
	public enum LineViewerStyle {
		// No line viewer will be displayed
		None,
		
		// The row in which the caret is will be marked
		FullRow
	}
	
	public enum BracketHighlightingStyle {
		// Brackets won't be highlighted
		None,
		
		// Brackets will be highlighted if the caret is on the bracket
		OnBracket,
		
		// Brackets will be highlighted if the caret is after the bracket
		AfterBracket
	}

	public enum DocumentSelectionMode {
		// The 'normal' selection mode.
		Normal,
		
		// Selections will be added to the current selection or new
		// ones will be created (multi-select mode)
		Additive
	}
	
	public enum EditorFontType {
		// Default Monospace font as set in the user's GNOME font properties
		DefaultMonospace,
		
		// Default Sans font as set in the user's GNOME font properties
		DefaultSans,
		
		// Custom font, will need to get the FontName property for more specifics
		UserSpecified
	}
	
	public class TextEditorProperties {
		static Properties properties = ((Properties) PropertyService.Get (
			"MonoDevelop.TextEditor.Document.Document.DefaultDocumentAggregatorProperties",
			new Properties()));
		
		public static Properties Properties {
			get {
				return properties;
			}
		}
		
		public static int TabIndent {
			get {
				return PropertyService.Get ("TabIndent", 4);
			}
			set {
				PropertyService.Set ("TabIndent", value);
			}
		}
		
		public static bool ConvertTabsToSpaces {
			get {
				return PropertyService.Get ("TabsToSpaces", false);
			}
			set {
				PropertyService.Set ("TabsToSpaces", value);
			}
		}
		
		public static string IndentString {
			get { 
				return ConvertTabsToSpaces ? new string(' ', TabIndent) : "\t"; 
			}
		}
		
		public static bool SyntaxHighlight {
			get {
				return properties.Get ("SyntaxHighlight", true);
			}
			set {
				properties.Set ("SyntaxHighlight", value);
			}
		}
		
		public static bool EnableCodeCompletion {
			get {
				return properties.Get ("EnableCodeCompletion", true);
			}
			set {
				properties.Set ("EnableCodeCompletion", value);
			}
		}
		
		public static bool EnableAutoCorrection {
			get {
				return properties.Get ("EnableAutoCorrection", true);
			}
			set {
				properties.Set ("EnableAutoCorrection", value);
			}
		}
		
		public static IndentStyle IndentStyle {
			get {
				switch (PropertyService.Get ("IndentStyle", "Smart")) {
				case "Smart":
					return IndentStyle.Smart;
				case "Auto":
					return IndentStyle.Auto;
				}
				return IndentStyle.None;
			}
			set {
				PropertyService.Set ("IndentStyle", value);
			}
		}
		
		public static DocumentSelectionMode DocumentSelectionMode {
			get {
				return (DocumentSelectionMode) properties.Get ("DocumentSelectionMode", DocumentSelectionMode.Normal);
			}
			set {
				properties.Set ("DocumentSelectionMode", value);
			}
		}
		
		public static bool AllowCaretBeyondEOL {
			get {
				return properties.Get ("CursorBehindEOL", false);
			}
			set {
				properties.Set ("CursorBehindEOL", value);
			}
		}
		
		public static bool ShowMatchingBracket {
			get {
				return properties.Get ("ShowBracketHighlight", true);
			}
			set {
				properties.Set ("ShowBracketHighlight", value);
			}
		}
		
		public static bool ShowLineNumbers {
			get {
				return properties.Get ("ShowLineNumbers", true);
			}
			set {
				properties.Set ("ShowLineNumbers", value);
			}
		}
		
		public static bool ShowSpaces {
			get {
				return properties.Get ("ShowSpaces", false);
			}
			set {
				properties.Set ("ShowSpaces", value);
			}
		}
		
		public static bool ShowTabs {
			get {
				return properties.Get ("ShowTabs", false);
			}
			set {
				properties.Get ("ShowTabs", value);
			}
		}
		
		public static bool ShowEOLMarker {
			get {
				return properties.Get ("ShowEOLMarkers", false);
			}
			set {
				properties.Set ("ShowEOLMarkers", value);
			}
		}
		
		public static bool ShowInvalidLines {
			get {
				return properties.Get ("ShowInvalidLines", false);
			}
			set {
				properties.Set ("ShowInvalidLines", value);
			}
		}
		
		public static bool IsIconBarVisible {
			get {
				return properties.Get ("IconBarVisible", true);
			}
			set {
				properties.Set ("IconBarVisible", value);
			}
		}
		
		public static bool EnableFolding {
			get {
				return properties.Get ("EnableFolding", true);
			}
			set {
				properties.Set ("EnableFolding", value);
			}
		}
		
		public static bool ShowHorizontalRuler {
			get {
				return properties.Get ("ShowHRuler", false);
			}
			set {
				properties.Set ("ShowHRuler", value);
			}
		}
		
		public static bool ShowVerticalRuler {
			get {
				return properties.Get ("ShowVRuler", false);
			}
			set {
				properties.Set ("ShowVRuler", value);
			}
		}
		
		
		public static bool UseAntiAliasedFont {
			get {
				return properties.Get ("UseAntiAliasFont", false);
			}
			set {
				properties.Set ("UseAntiAliasFont", value);
			}
		}
		
		public static bool CreateBackupCopy {
			get {
				return properties.Get ("CreateBackupCopy", false);
			}
			set {
				properties.Set ("CreateBackupCopy", value);
			}
		}
		
		public static bool MouseWheelScrollDown {
			get {
				return properties.Get ("MouseWheelScrollDown", false);
			}
			set {
				properties.Set ("MouseWheelScrollDown", value);
			}
		}
		
		public static bool HideMouseCursor {
			get {
				return properties.Get ("HideMouseCursor", false);
			}
			set {
				properties.Set ("HideMouseCursor", value);
			}
		}
		
		public static Encoding Encoding {
			get {
				return Encoding.GetEncoding (properties.Get ("Encoding", 1252));
			}
			set {
				properties.Set ("Encoding", value.CodePage);
			}
		}
		
		public static int VerticalRulerRow {
			get {
				return properties.Get ("VRulerRow", 80);
			}
			set {
				properties.Set ("VRulerRow", value);
			}
		}
		
		public static LineViewerStyle LineViewerStyle {
			get {
				return (LineViewerStyle) properties.Get ("LineViewerStyle", LineViewerStyle.None);
			}
			set {
				properties.Set ("LineViewerStyle", value);
			}
		}
		
		public static string LineTerminator {
			get {
				return "\n";
			}
			set {
				throw new System.NotImplementedException();
			}
		}
		
		public static bool AutoInsertCurlyBracket {
			get {
				return properties.Get("AutoInsertCurlyBracket", true);
			}
			set {
				properties.Set("AutoInsertCurlyBracket", value);
			}
		}
		
		public static bool AutoInsertTemplates {
			get {
				return properties.Get("AutoInsertTemplates", true);
			}
			set {
				properties.Set("AutoInsertTemplates", value);
			}
		}
		
		public static bool UnderlineErrors {
			get {
				return properties.Get("ShowErrors", true);
			}
			set {
				properties.Set("ShowErrors", value);
			}
		}
		
		public static Gtk.WrapMode WrapMode {
			get {
				return (Gtk.WrapMode) properties.Get ("WrapMode", Gtk.WrapMode.None);
			}
			set {
				properties.Set ("WrapMode", value);
			}
		}
		
		public static EditorFontType FontType {
			get {
				string name = FontName;
				
				switch (name) {
				case "__default_monospace":
					return EditorFontType.DefaultMonospace;
				case "__default_sans":
					return EditorFontType.DefaultSans;
				default:
					return EditorFontType.UserSpecified;
				}
			}
			set {
				switch (value) {
				case EditorFontType.DefaultMonospace:
					FontName = "__default_monospace";
					break;
				case EditorFontType.DefaultSans:
					FontName = "__default_sans";
					break;
				default:
					// no-op - caller must set FontName himself
					break;
				}
			}
		}
		
		public static string FontName {
			get {
				return properties.Get ("DefaultFont", "__default_monospace");
			}
			set {
				properties.Set ("DefaultFont", value != null ? value : "__default_monospace");
			}
		}
		
		public static FontDescription Font {
			get {
				string s = FontName;
				
				switch (s) {
				case "__default_monospace":
					try {
						string fontName = IdeApp.Services.PlatformService.DefaultMonospaceFont;
						return FontDescription.FromString (fontName);
					} catch (Exception ex) {
						LoggingService.LogWarning ("Could not load platform's default monospace font.", ex);
						goto case "__default_sans";
					}
				case "__default_sans":
					return new Gtk.Label ("").Style.FontDescription;
				default:
					return FontDescription.FromString (s);
				}
			}
			set {
				properties.Set ("DefaultFont", value.ToString ());
			}
		}
		
		public static bool ShowClassBrowser {
			get {
				return properties.Get("ShowClassBrowser", true);
			}
			set {
				properties.Set("ShowClassBrowser", value);
			}
		}
		
		public static bool HighlightCurrentLine {
			get {
				return properties.Get ("HighlightCurrentLine", true);
			}
			set {
				properties.Set ("HighlightCurrentLine", value);
			}
		}
		
		public static bool HighlightSpaces {
			get {
				return properties.Get ("HighlightSpaces", false);
			}
			set {
				properties.Set ("HighlightSpaces", value);
			}
		}

		public static bool HighlightTabs {
			get {
				return properties.Get ("HighlightTabs", false);
			}
			set {
				properties.Set ("HighlightTabs", value);
			}
		}
	
		public static bool HighlightNewlines {
			get {
				return properties.Get ("HighlightNewlines", false);
			}
			set {
				properties.Set ("HighlightNewlines", value);
			}
		}
		
	}
}
