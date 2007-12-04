using System;
using System.Drawing;
using System.Text;

using MonoDevelop.EditorBindings.FormattingStrategy;
using MonoDevelop.EditorBindings.Properties;

namespace MonoDevelop.TextEditor.Document
{
	public class DefaultTextEditorProperties : ITextEditorProperties
	{
		int                   tabIndent             = 4;
		IndentStyle           indentStyle           = IndentStyle.Smart;
		DocumentSelectionMode documentSelectionMode = DocumentSelectionMode.Normal;
		Encoding              encoding              = System.Text.Encoding.UTF8;
		
		bool        allowCaretBeyondEOL = false;
		
		bool        showMatchingBracket = true;
		bool        showLineNumbers     = true;
		
		bool        showSpaces          = true;
		bool        showTabs            = true;
		bool        showEOLMarker       = true;
		
		bool        showInvalidLines    = true;
		
		bool        isIconBarVisible    = true;
		bool        enableFolding       = true;
		bool        showHorizontalRuler = false;
		bool        showVerticalRuler   = true;
		bool        convertTabsToSpaces = false;
		bool        useAntiAliasedFont  = false;
		bool        createBackupCopy    = false;
		bool        mouseWheelScrollDown = true;
		bool        hideMouseCursor      = false;
		
		int         verticalRulerRow    = 80;
		LineViewerStyle  lineViewerStyle = LineViewerStyle.None;
		string      lineTerminator = "\r\n";
		bool        autoInsertCurlyBracket = true;
		
		public int TabIndent {
			get {
				return tabIndent;
			}
			set {
				tabIndent = value;
			}
		}
		public IndentStyle IndentStyle {
			get {
				return indentStyle;
			}
			set {
				indentStyle = value;
			}
		}
		public DocumentSelectionMode DocumentSelectionMode {
			get {
				return documentSelectionMode;
			}
			set {
				documentSelectionMode = value;
			}
		}
		public bool AllowCaretBeyondEOL {
			get {
				return allowCaretBeyondEOL;
			}
			set {
				allowCaretBeyondEOL = value;
			}
		}
		public bool ShowMatchingBracket {
			get {
				return showMatchingBracket;
			}
			set {
				showMatchingBracket = value;
			}
		}
		public bool ShowLineNumbers {
			get {
				return showLineNumbers;
			}
			set {
				showLineNumbers = value;
			}
		}
		public bool ShowSpaces {
			get {
				return showSpaces;
			}
			set {
				showSpaces = value;
			}
		}
		public bool ShowTabs {
			get {
				return showTabs;
			}
			set {
				showTabs = value;
			}
		}
		public bool ShowEOLMarker {
			get {
				return showEOLMarker;
			}
			set {
				showEOLMarker = value;
			}
		}
		public bool ShowInvalidLines {
			get {
				return showInvalidLines;
			}
			set {
				showInvalidLines = value;
			}
		}
		public bool IsIconBarVisible {
			get {
				return isIconBarVisible;
			}
			set {
				isIconBarVisible = value;
			}
		}
		public bool EnableFolding {
			get {
				return enableFolding;
			}
			set {
				enableFolding = value;
			}
		}
		public bool ShowHorizontalRuler {
			get {
				return showHorizontalRuler;
			}
			set {
				showHorizontalRuler = value;
			}
		}
		public bool ShowVerticalRuler {
			get {
				return showVerticalRuler;
			}
			set {
				showVerticalRuler = value;
			}
		}
		public bool ConvertTabsToSpaces {
			get {
				return convertTabsToSpaces;
			}
			set {
				convertTabsToSpaces = value;
			}
		}
		public bool UseAntiAliasedFont {
			get {
				return useAntiAliasedFont;
			}
			set {
				useAntiAliasedFont = value;
			}
		}
		public bool CreateBackupCopy {
			get {
				return createBackupCopy;
			}
			set {
				createBackupCopy = value;
			}
		}
		public bool MouseWheelScrollDown {
			get {
				return mouseWheelScrollDown;
			}
			set {
				mouseWheelScrollDown = value;
			}
		}
		public bool HideMouseCursor {
			get {
				return hideMouseCursor;
			}
			set {
				hideMouseCursor = value;
			}
		}
		public Encoding Encoding {
			get {
				return encoding;
			}
			set {
				encoding = value;
			}
		}
		public int VerticalRulerRow {
			get {
				return verticalRulerRow;
			}
			set {
				verticalRulerRow = value;
			}
		}
		public LineViewerStyle LineViewerStyle {
			get {
				return lineViewerStyle;
			}
			set {
				lineViewerStyle = value;
			}
		}
		public string LineTerminator {
			get {
				return lineTerminator;
			}
			set {
				lineTerminator = value;
			}
		}
		public bool AutoInsertCurlyBracket {
			get {
				return autoInsertCurlyBracket;
			}
			set {
				autoInsertCurlyBracket = value;
			}
		}
		
		public Pango.FontDescription Font {
			get {
				return FontContainer.DefaultFont;
			}
			set {
				FontContainer.DefaultFont = value;
			}
		}
		
	}
}
