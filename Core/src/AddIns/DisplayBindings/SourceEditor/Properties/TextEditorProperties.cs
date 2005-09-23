using System;
using System.Text;

using Pango;

using MonoDevelop.Core.Properties;
using MonoDevelop.Core.Services;
using MonoDevelop.Gui.Dialogs.OptionPanels;
using MonoDevelop.TextEditor;
using MonoDevelop.TextEditor.Document;
using MonoDevelop.EditorBindings.FormattingStrategy;

namespace MonoDevelop.EditorBindings.Properties {
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
	
	public class TextEditorProperties {
		static PropertyService propertyService = (PropertyService)ServiceManager.GetService(typeof(PropertyService));
		static IProperties properties = ((IProperties) propertyService.GetProperty (
			"MonoDevelop.TextEditor.Document.Document.DefaultDocumentAggregatorProperties",
			new DefaultProperties()));
		
		public static int TabIndent {
			get {
				return properties.GetProperty ("TabIndent", 4);

			}
			set {
				properties.SetProperty ("TabIndent", value);
			}
		}

		public static bool SyntaxHighlight {
			get {
				return properties.GetProperty ("SyntaxHighlight", true);
			}
			set {
				properties.SetProperty ("SyntaxHighlight", value);
			}
		}

		public static bool EnableCodeCompletion {
			get {
				return properties.GetProperty ("EnableCodeCompletion", true);
			}
			set {
				properties.SetProperty ("EnableCodeCompletion", value);
			}
		}
		
		public static IndentStyle IndentStyle {
			get {
				return (IndentStyle)properties.GetProperty ("IndentStyle", IndentStyle.Auto);
			}
			set {
				properties.SetProperty ("IndentStyle", value);
			}
		}
		
		public static DocumentSelectionMode DocumentSelectionMode {
			get {
				return (DocumentSelectionMode) properties.GetProperty ("DocumentSelectionMode", DocumentSelectionMode.Normal);
			}
			set {
				properties.SetProperty ("DocumentSelectionMode", value);
			}
		}
		
		public static bool AllowCaretBeyondEOL {
			get {
				return properties.GetProperty ("CursorBehindEOL", false);
			}
			set {
				properties.SetProperty ("CursorBehindEOL", value);
			}
		}
		
		public static bool ShowMatchingBracket {
			get {
				return properties.GetProperty ("ShowBracketHighlight", true);
			}
			set {
				properties.SetProperty ("ShowBracketHighlight", value);
			}
		}
		
		public static bool ShowLineNumbers {
			get {
				return properties.GetProperty ("ShowLineNumbers", true);
			}
			set {
				properties.SetProperty ("ShowLineNumbers", value);
			}
		}
		
		public static bool ShowSpaces {
			get {
				return properties.GetProperty ("ShowSpaces", false);
			}
			set {
				properties.SetProperty ("ShowSpaces", value);
			}
		}
		
		public static bool ShowTabs {
			get {
				return properties.GetProperty ("ShowTabs", false);
			}
			set {
				properties.GetProperty ("ShowTabs", value);
			}
		}
		
		public static bool ShowEOLMarker {
			get {
				return properties.GetProperty ("ShowEOLMarkers", false);
			}
			set {
				properties.SetProperty ("ShowEOLMarkers", value);
			}
		}
		
		public static bool ShowInvalidLines {
			get {
				return properties.GetProperty ("ShowInvalidLines", false);
			}
			set {
				properties.SetProperty ("ShowInvalidLines", value);
			}
		}
		
		public static bool IsIconBarVisible {
			get {
				return properties.GetProperty ("IconBarVisible", true);
			}
			set {
				properties.SetProperty ("IconBarVisible", value);
			}
		}
		
		public static bool EnableFolding {
			get {
				return properties.GetProperty ("EnableFolding", true);
			}
			set {
				properties.SetProperty ("EnableFolding", value);
			}
		}
		
		public static bool ShowHorizontalRuler {
			get {
				return properties.GetProperty ("ShowHRuler", false);
			}
			set {
				properties.SetProperty ("ShowHRuler", value);
			}
		}
		
		public static bool ShowVerticalRuler {
			get {
				return properties.GetProperty ("ShowVRuler", false);
			}
			set {
				properties.SetProperty ("ShowVRuler", value);
			}
		}
		
		public static bool ConvertTabsToSpaces {
			get {
				return properties.GetProperty ("TabsToSpaces", false);
			}
			set {
				properties.SetProperty ("TabsToSpaces", value);
			}
		}
		
		public static bool UseAntiAliasedFont {
			get {
				return properties.GetProperty ("UseAntiAliasFont", false);
			}
			set {
				properties.SetProperty ("UseAntiAliasFont", value);
			}
		}
		
		public static bool CreateBackupCopy {
			get {
				return properties.GetProperty ("CreateBackupCopy", false);
			}
			set {
				properties.SetProperty ("CreateBackupCopy", value);
			}
		}
		
		public static bool MouseWheelScrollDown {
			get {
				return properties.GetProperty ("MouseWheelScrollDown", false);
			}
			set {
				properties.SetProperty ("MouseWheelScrollDown", value);
			}
		}
		
		public static bool HideMouseCursor {
			get {
				return properties.GetProperty ("HideMouseCursor", false);
			}
			set {
				properties.SetProperty ("HideMouseCursor", value);
			}
		}
		
		public static Encoding Encoding {
			get {
				return Encoding.GetEncoding (properties.GetProperty ("Encoding", 1252));
			}
			set {
				properties.SetProperty ("Encoding", value.CodePage);
			}
		}
		
		public static int VerticalRulerRow {
			get {
				return properties.GetProperty ("VRulerRow", 80);
			}
			set {
				properties.SetProperty ("VRulerRow", value);
			}
		}
		
		public static LineViewerStyle LineViewerStyle {
			get {
				return (LineViewerStyle) properties.GetProperty ("LineViewerStyle", LineViewerStyle.None);
			}
			set {
				properties.SetProperty ("LineViewerStyle", value);
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
				return properties.GetProperty("AutoInsertCurlyBracket", true);
			}
			set {
				properties.SetProperty("AutoInsertCurlyBracket", value);
			}
		}

		public static bool AutoInsertTemplates {
			get {
				return properties.GetProperty("AutoInsertTemplates", true);
			}
			set {
				properties.SetProperty("AutoInsertTemplates", value);
			}
		}

		public static bool UnderlineErrors {
			get {
				return properties.GetProperty("ShowErrors", true);
			}
			set {
				properties.SetProperty("ShowErrors", value);
			}
		}
		
		public static FontDescription Font {
			get {
				string s = properties.GetProperty ("DefaultFont", "__default_monospace");
				
				switch (s) {
				case "__default_monospace":
					try {
						return FontDescription.FromString ((string) new GConf.Client ().Get ("/desktop/gnome/interface/monospace_font_name"));
					} catch {
						goto case "__default_sans";
					}
				case "__default_sans":
					return new Gtk.Label ("").Style.FontDescription;
				default:
					return FontDescription.FromString (s);
				}
			}
			set {
				properties.SetProperty ("DefaultFont", value.ToString ());
			}
		}
	}
}
