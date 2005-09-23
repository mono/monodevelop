using System;
using System.Text;
using System.Drawing;

using MonoDevelop.Core.Properties;
using MonoDevelop.Core.Services;
using MonoDevelop.Gui.Dialogs.OptionPanels;

using MonoDevelop.TextEditor;
using MonoDevelop.TextEditor.Document;

using Pango;

using MonoDevelop.EditorBindings.FormattingStrategy;
using MonoDevelop.EditorBindings.Properties;

namespace MonoDevelop.DefaultEditor.Gui.Editor
{
	public class SharpDevelopTextEditorProperties : ITextEditorProperties
	{	
		static SharpDevelopTextEditorProperties()
		{
			PropertyService propertyService = (PropertyService) ServiceManager.Services.GetService (typeof(PropertyService));
			IProperties properties2 = ((IProperties) propertyService.GetProperty("MonoDevelop.TextEditor.Document.Document.DefaultDocumentAggregatorProperties", new DefaultProperties()));
			properties2.PropertyChanged += new PropertyEventHandler (CheckFontChange);
			
			FontContainer.DefaultFont = TextEditorProperties.Font;
		}
		
		static void CheckFontChange(object sender, PropertyEventArgs e)
		{
			if (e.Key == "DefaultFont")
				FontContainer.DefaultFont = TextEditorProperties.Font;
		}
		
		public int TabIndent {
			get { return TextEditorProperties.TabIndent; }
			set { TextEditorProperties.TabIndent = value; }
		}
		
		public IndentStyle IndentStyle {
			get { return TextEditorProperties.IndentStyle; }
			set { TextEditorProperties.IndentStyle = value; }
		}
		
		public DocumentSelectionMode DocumentSelectionMode {
			get { return TextEditorProperties.DocumentSelectionMode; }
			set { TextEditorProperties.DocumentSelectionMode = value; }
		}
		
		public bool AllowCaretBeyondEOL {
			get { return TextEditorProperties.AllowCaretBeyondEOL; }
			set { TextEditorProperties.AllowCaretBeyondEOL = value; }
		}
		
		public bool ShowMatchingBracket {
			get { return TextEditorProperties.ShowMatchingBracket; }
			set { TextEditorProperties.ShowMatchingBracket = value; }
		}
		
		public bool ShowLineNumbers {
			get { return TextEditorProperties.ShowLineNumbers; }
			set { TextEditorProperties.ShowLineNumbers = value; }
		}
		
		public bool ShowSpaces {
			get { return TextEditorProperties.ShowSpaces; }
			set { TextEditorProperties.ShowSpaces = value; }
		}
		
		public bool ShowTabs {
			get { return TextEditorProperties.ShowTabs; }
			set { TextEditorProperties.ShowTabs = value; }
		}
		
		public bool ShowEOLMarker {
			get { return TextEditorProperties.ShowEOLMarker; }
			set { TextEditorProperties.ShowEOLMarker = value; }
		}
		
		public bool ShowInvalidLines {
			get { return TextEditorProperties.ShowInvalidLines; }
			set { TextEditorProperties.ShowInvalidLines = value; }
		}
		
		public bool IsIconBarVisible {
			get { return TextEditorProperties.IsIconBarVisible; }
			set { TextEditorProperties.IsIconBarVisible = value; }
		}
		
		public bool EnableFolding {
			get { return TextEditorProperties.EnableFolding; }
			set { TextEditorProperties.EnableFolding = value; }
		}
		
		public bool ShowHorizontalRuler {
			get { return TextEditorProperties.ShowHorizontalRuler; }
			set { TextEditorProperties.ShowHorizontalRuler = value; }
		}
		public bool ShowVerticalRuler {
			get { return TextEditorProperties.ShowVerticalRuler; }
			set { TextEditorProperties.ShowVerticalRuler = value; }
		}
		
		public bool ConvertTabsToSpaces {
			get { return TextEditorProperties.ConvertTabsToSpaces; }
			set { TextEditorProperties.ConvertTabsToSpaces = value; }
		}
		
		public bool UseAntiAliasedFont {
			get { return TextEditorProperties.UseAntiAliasedFont; }
			set { TextEditorProperties.UseAntiAliasedFont = value; }
		}
		
		public bool CreateBackupCopy {
			get { return TextEditorProperties.CreateBackupCopy; }
			set { TextEditorProperties.CreateBackupCopy = value; }
		}
		
		public bool MouseWheelScrollDown {
			get { return TextEditorProperties.MouseWheelScrollDown; }
			set { TextEditorProperties.MouseWheelScrollDown = value; }
		}
		
		public bool HideMouseCursor {
			get { return TextEditorProperties.HideMouseCursor; }
			set { TextEditorProperties.HideMouseCursor = value; }
		}
		
		public Encoding Encoding {
			get { return TextEditorProperties.Encoding; }
			set { TextEditorProperties.Encoding = value; }
		}
		
		public int VerticalRulerRow {
			get { return TextEditorProperties.VerticalRulerRow; }
			set { TextEditorProperties.VerticalRulerRow = value; }
		}
		
		public LineViewerStyle LineViewerStyle {
			get { return TextEditorProperties.LineViewerStyle; }
			set { TextEditorProperties.LineViewerStyle = value; }
		}
		
		public string LineTerminator {
			get { return TextEditorProperties.LineTerminator; }
			set { TextEditorProperties.LineTerminator = value; }
		}
		
		public bool AutoInsertCurlyBracket {
			get { return TextEditorProperties.AutoInsertCurlyBracket; }
			set { TextEditorProperties.AutoInsertCurlyBracket = value; }
		}
		
		public FontDescription Font {
			get { return TextEditorProperties.Font; }
			set {
				TextEditorProperties.Font = FontContainer.DefaultFont = value;
			}
		}		
	}
}
