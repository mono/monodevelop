// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.IO;
using System.Drawing;
using System.Drawing.Printing;

using MonoDevelop.Gui;
using MonoDevelop.TextEditor;
using MonoDevelop.TextEditor.Document;
using MonoDevelop.Internal.Project;
using MonoDevelop.Internal.Undo;


using MonoDevelop.Core.Properties;
using MonoDevelop.Core.AddIns;
using MonoDevelop.Core.Services;
using MonoDevelop.Services;
using MonoDevelop.Core.AddIns.Codons;

namespace MonoDevelop.DefaultEditor.Gui.Editor
{
	public class TextEditorDisplayBinding : IDisplayBinding
	{	
		// load #D-specific syntax highlighting files here
		// don't know if this could be solved better by new codons,
		// but this will do
		static TextEditorDisplayBinding()
		{
			PropertyService propertyService = (PropertyService)ServiceManager.Services.GetService(typeof(PropertyService));
			
			string modeDir = Path.Combine(propertyService.ConfigDirectory, "modes");
			if (!Directory.Exists(modeDir)) Directory.CreateDirectory(modeDir);
			
			HighlightingManager.Manager.AddSyntaxModeFileProvider(new FileSyntaxModeProvider(modeDir));
		}
		
		public virtual bool CanCreateContentForFile(string fileName)
		{
			return false;
		}

		public virtual bool CanCreateContentForMimeType (string mimetype)
		{
			if (mimetype.StartsWith ("text")) return true;
			return false;
		}
		
		public virtual bool CanCreateContentForLanguage(string language)
		{
			return true;
		}
		
		public virtual IViewContent CreateContentForFile(string fileName)
		{
			TextEditorDisplayBindingWrapper b2 = new TextEditorDisplayBindingWrapper();

#if GTK
			// FIXME: GTKize
#else
			b2.textAreaControl.Dock = DockStyle.Fill;
#endif
			b2.Load(fileName);
			b2.textAreaControl.Document.HighlightingStrategy = HighlightingStrategyFactory.CreateHighlightingStrategyForFile(fileName);
			b2.textAreaControl.Document.Language = HighlightingStrategyFactory.LanguageFromFile (fileName);
			b2.textAreaControl.InitializeFormatter();
			return b2;
		}
		
		public virtual IViewContent CreateContentForLanguage(string language, string content)
		{
			TextEditorDisplayBindingWrapper b2 = new TextEditorDisplayBindingWrapper();
			StringParserService stringParserService = (StringParserService)ServiceManager.Services.GetService(typeof(StringParserService));
			b2.textAreaControl.Document.TextContent = stringParserService.Parse(content);
			b2.textAreaControl.Document.HighlightingStrategy = HighlightingStrategyFactory.CreateHighlightingStrategy(language);
			Console.WriteLine (language);
			b2.textAreaControl.Document.Language = language;
			b2.textAreaControl.InitializeFormatter();
			return b2;
		}
		
		public virtual IViewContent CreateContentForLanguage(string language, string content, string new_file_name)
		{
			TextEditorDisplayBindingWrapper b2 = new TextEditorDisplayBindingWrapper();
			StringParserService stringParserService = (StringParserService)ServiceManager.Services.GetService(typeof(StringParserService));
			b2.textAreaControl.Document.TextContent = stringParserService.Parse(content);
			b2.textAreaControl.Document.HighlightingStrategy = HighlightingStrategyFactory.CreateHighlightingStrategy(language);
			Console.WriteLine (language);
			b2.textAreaControl.Document.Language = language;
			b2.textAreaControl.FileName = new_file_name;
			b2.textAreaControl.InitializeFormatter();
			return b2;
		}	
	}
	
	public class TextEditorDisplayBindingWrapper : AbstractViewContent, IMementoCapable, IPrintable, IEditable, IPositionable, ITextEditorControlProvider, IParseInformationListener, IClipboardHandler,
		IBookmarkOperations
	{
		public SharpDevelopTextAreaControl textAreaControl = new SharpDevelopTextAreaControl();

		public TextEditorControl TextEditorControl {
			get {
				return textAreaControl;
			}
		}
		
		// KSL Start, New lines
		FileSystemWatcher watcher;
		bool wasChangedExternally = false;
		// KSL End 
		
		
		public string Text {
			get {
				return textAreaControl.Document.TextContent;
			}
			set {
				textAreaControl.Document.TextContent = value;
			}
		}
		
		public PrintDocument PrintDocument {
			get { 
				return textAreaControl.PrintDocument;
			}
		}
		
		public IClipboardHandler ClipboardHandler {
			get {
				return this;
			}
		}
		
		public override Gtk.Widget Control {
			get {
				return textAreaControl;
			}
		}
		
		public override string TabPageText {
			get {
				return "${res:FormsDesigner.DesignTabPages.SourceTabPage}";
			}
		}
		
		public void Undo()
		{
			this.textAreaControl.Undo();
		}
		
		public void Redo()
		{
			this.textAreaControl.Redo();
		}
		
		public TextEditorDisplayBindingWrapper()
		{
			textAreaControl.Document.DocumentChanged += new DocumentEventHandler(TextAreaChangedEvent);
			textAreaControl.ActiveTextAreaControl.Caret.CaretModeChanged += new EventHandler(CaretModeChanged);
#if GTK
			textAreaControl.FileNameChanged += new EventHandler(FileNameChangedEvent);
#else
			textAreaControl.ActiveTextAreaControl.Enter += new EventHandler(CaretUpdate);
			
			// KSL Start, New lines
			textAreaControl.FileNameChanged += new EventHandler(FileNameChangedEvent);
			textAreaControl.GotFocus += new EventHandler(GotFocusEvent);
			// KSL End 
#endif
			
			
		}
		// KSL Start, new event handlers

		
		void SetWatcher()
		{
			try {
				if (this.watcher == null) {
					this.watcher = new FileSystemWatcher();
					this.watcher.Changed += new FileSystemEventHandler(this.OnFileChangedEvent);
				}
				else {
					this.watcher.EnableRaisingEvents = false;
				}
				this.watcher.Path = Path.GetDirectoryName(textAreaControl.FileName);
				this.watcher.Filter = Path.GetFileName(textAreaControl.FileName);
				this.watcher.NotifyFilter = NotifyFilters.LastWrite;
				this.watcher.EnableRaisingEvents = true;
			} catch (Exception) {
				watcher = null;
			}
		}
		
		void FileNameChangedEvent(object sender, EventArgs e)
		{
			if (textAreaControl.FileName != null) {
				SetWatcher();
			} else {
				this.watcher = null;
			}
		}

		void GotFocusEvent(object sender, EventArgs e)
		{
			lock (this) {
				if (wasChangedExternally) {
					wasChangedExternally = false;
#if GTK
					MessageService msgService = (MessageService) ServiceManager.Services.GetService(typeof(MessageService));
					if (msgService.AskQuestion ("The file " + textAreaControl.FileName + " has been changed externally to SharpDevelop.\nDo you want to reload it?")) {
						Load(textAreaControl.FileName);
					}
#else
					if (MessageBox.Show("The file " + textAreaControl.FileName + " has been changed externally to SharpDevelop.\nDo you want to reload it?",
					                    "SharpDevelop",
					                    MessageBoxButtons.YesNo,
					                    MessageBoxIcon.Question) == DialogResult.Yes) {
						Load(textAreaControl.FileName);
					}
#endif
				}
			}
		}
		
		void OnFileChangedEvent(object sender, FileSystemEventArgs e)
		{
			lock (this) {
				wasChangedExternally = true;
			}
		}

		// KSL End
	
		void TextAreaChangedEvent(object sender, DocumentEventArgs e)
		{
			IsDirty = true;
		}
		
		public override void RedrawContent()
		{
			textAreaControl.OptionsChanged();
		}
		
		public override void Dispose()
		{
			textAreaControl.Dispose();
		}
		
		public override bool IsReadOnly {
			get {
				return textAreaControl.IsReadOnly;
			}
		}
		
		public override void Save(string fileName)
		{
			OnBeforeSave(EventArgs.Empty);
			// KSL, Start new line
			if (watcher != null) {
				this.watcher.EnableRaisingEvents = false;
			}
			// KSL End
			
			textAreaControl.SaveFile(fileName);
			ContentName = fileName;
			IsDirty     = false;
			
			// KSL, Start new lines
			if (this.watcher != null) {
				this.watcher.EnableRaisingEvents = true;
			}
			// KSL End
		}
		
		public override void Load(string fileName)
		{
			textAreaControl.IsReadOnly = (File.GetAttributes(fileName) & FileAttributes.ReadOnly) == FileAttributes.ReadOnly;
			    
			textAreaControl.LoadFile(fileName);
			ContentName = fileName;
			IsDirty     = false;
		}
		
		public IXmlConvertable CreateMemento()
		{
			DefaultProperties properties = new DefaultProperties();
//			properties.SetProperty("Bookmarks",   textAreaControl.Document.BookmarkManager.CreateMemento());
//			properties.SetProperty("CaretOffset", textAreaControl.Document.Caret.Offset);
//			properties.SetProperty("VisibleLine",   textAreaControl.FirstVisibleColumn);
//			properties.SetProperty("VisibleColumn", textAreaControl.FirstVisibleRow);
////			properties.SetProperty("Properties",    textAreaControl.Properties);
//			properties.SetProperty("HighlightingLanguage", textAreaControl.Document.HighlightingStrategy.Name);
			return properties;
		}
		
		public void SetMemento(IXmlConvertable memento)
		{
//			IProperties properties = (IProperties)memento;
//			BookmarkManagerMemento bookmarkMemento = (BookmarkManagerMemento)properties.GetProperty("Bookmarks", textAreaControl.Document.BookmarkManager.CreateMemento());
//			bookmarkMemento.CheckMemento(textAreaControl.Document);
//			textAreaControl.Document.BookmarkManager.SetMemento(bookmarkMemento);
//			textAreaControl.Document.Caret.Offset = Math.Min(textAreaControl.Document.TextLength, Math.Max(0, properties.GetProperty("CaretOffset", textAreaControl.Document.Caret.Offset)));
//			textAreaControl.Document.SetDesiredColumn();
//			textAreaControl.FirstVisibleColumn    = Math.Min(textAreaControl.Document.TotalNumberOfLines, Math.Max(0, properties.GetProperty("VisibleLine", textAreaControl.FirstVisibleColumn)));
//			textAreaControl.FirstVisibleRow       = Math.Max(0, properties.GetProperty("VisibleColumn", textAreaControl.FirstVisibleRow));
////			textAreaControl.Document.Properties   = (IProperties)properties.GetProperty("Properties",    textAreaControl.Properties);
//			IHighlightingStrategy highlightingStrategy = HighlightingStrategyFactory.CreateHighlightingStrategy(properties.GetProperty("HighlightingLanguage", textAreaControl.Document.HighlightingStrategy.Name));
//			
//			if (highlightingStrategy != null) {
//				textAreaControl.Document.HighlightingStrategy = highlightingStrategy;
//			}
//			
//			// insane check for cursor position, may be required for document reload.
//			int lineNr = textAreaControl.Document.GetLineNumberForOffset(textAreaControl.Document.Caret.Offset);
//			LineSegment lineSegment = textAreaControl.Document.GetLineSegment(lineNr);
//			textAreaControl.Document.Caret.Offset = Math.Min(lineSegment.Offset + lineSegment.Length, textAreaControl.Document.Caret.Offset);
//			
//			textAreaControl.OptionsChanged();
//			textAreaControl.Refresh();
		}
		
		void CaretUpdate(object sender, EventArgs e)
		{
			CaretChanged(null, null);
			CaretModeChanged(null, null);
		}
		
		void CaretChanged(object sender, EventArgs e)
		{
			Point    pos       = textAreaControl.Document.OffsetToPosition(textAreaControl.ActiveTextAreaControl.Caret.Offset);
			LineSegment line   = textAreaControl.Document.GetLineSegment(pos.Y);
			IStatusBarService statusBarService = (IStatusBarService)MonoDevelop.Core.Services.ServiceManager.Services.GetService(typeof(IStatusBarService));
			statusBarService.SetCaretPosition(pos.X + 1, pos.Y + 1, textAreaControl.ActiveTextAreaControl.Caret.Offset - line.Offset + 1);
		}
		
		void CaretModeChanged(object sender, EventArgs e)
		{
			IStatusBarService statusBarService = (IStatusBarService)MonoDevelop.Core.Services.ServiceManager.Services.GetService(typeof(IStatusBarService));
			statusBarService.SetInsertMode(textAreaControl.ActiveTextAreaControl.Caret.CaretMode == CaretMode.InsertMode);
		}
				
		public override string ContentName {
			set {
				if (Path.GetExtension(ContentName) != Path.GetExtension(value)) {
					if (textAreaControl.Document.HighlightingStrategy != null) {
						textAreaControl.Document.HighlightingStrategy = HighlightingStrategyFactory.CreateHighlightingStrategyForFile(value);
						textAreaControl.Refresh();
					}
				}
				base.ContentName = value;
			}
		}
				
		public void JumpTo(int line, int column)
		{
			textAreaControl.ActiveTextAreaControl.JumpTo(line, column);
		}
		delegate void VoidDelegate(AbstractMargin margin);
		
		public void ParseInformationUpdated(IParseInformation parseInfo)
		{
			if (textAreaControl.TextEditorProperties.EnableFolding) {
#if GTK
				textAreaControl.Document.FoldingManager.UpdateFoldings(ContentName, parseInfo);
				//textAreaControl.ActiveTextAreaControl.TextArea.Invoke(new VoidDelegate(textAreaControl.ActiveTextAreaControl.TextArea.Refresh), new object[] { textAreaControl.ActiveTextAreaControl.TextArea.FoldMargin});
				//FIXME: Should the above line compile or not?
#else
				textAreaControl.Document.FoldingManager.UpdateFoldings(ContentName, parseInfo);
				textAreaControl.ActiveTextAreaControl.TextArea.Invoke(new VoidDelegate(textAreaControl.ActiveTextAreaControl.TextArea.Refresh), new object[] { textAreaControl.ActiveTextAreaControl.TextArea.FoldMargin});
#endif
			}
		}
		

#region MonoDevelop.Gui.IClipboardHandler interface implementation
		public bool EnableCut {
			get {
				return textAreaControl.ActiveTextAreaControl.TextArea.ClipboardHandler.EnableCut;
			}
		}
		
		public bool EnableCopy {
			get {
				return textAreaControl.ActiveTextAreaControl.TextArea.ClipboardHandler.EnableCopy;
			}
		}
		
		public bool EnablePaste {
			get {
				return textAreaControl.ActiveTextAreaControl.TextArea.ClipboardHandler.EnablePaste;
			}
		}
		
		public bool EnableDelete {
			get {
				return textAreaControl.ActiveTextAreaControl.TextArea.ClipboardHandler.EnableDelete;
			}
		}
		
		public bool EnableSelectAll {
			get {
				return textAreaControl.ActiveTextAreaControl.TextArea.ClipboardHandler.EnableSelectAll;
			}
		}
		
		public void SelectAll(object sender, System.EventArgs e)
		{
			textAreaControl.ActiveTextAreaControl.TextArea.ClipboardHandler.SelectAll(sender, e);
		}
		
		public void Delete(object sender, System.EventArgs e)
		{
			textAreaControl.ActiveTextAreaControl.TextArea.ClipboardHandler.Delete(sender, e);
		}
		
		public void Paste(object sender, System.EventArgs e)
		{
			textAreaControl.ActiveTextAreaControl.TextArea.ClipboardHandler.Paste(sender, e);
		}
		
		public void Copy(object sender, System.EventArgs e)
		{
			textAreaControl.ActiveTextAreaControl.TextArea.ClipboardHandler.Copy(sender, e);
		}
		
		public void Cut(object sender, System.EventArgs e)
		{
			textAreaControl.ActiveTextAreaControl.TextArea.ClipboardHandler.Cut(sender, e);
		}
#endregion
		
#region IBookmarkOperations
		void IBookmarkOperations.ToggleBookmark () { new MonoDevelop.TextEditor.Actions.ToggleBookmark     ().Execute (TextEditorControl.ActiveTextAreaControl.TextArea); }
		void IBookmarkOperations.PrevBookmark ()   { new MonoDevelop.TextEditor.Actions.GotoPrevBookmark   ().Execute (TextEditorControl.ActiveTextAreaControl.TextArea); }
		void IBookmarkOperations.NextBookmark ()   { new MonoDevelop.TextEditor.Actions.GotoNextBookmark   ().Execute (TextEditorControl.ActiveTextAreaControl.TextArea); }
		void IBookmarkOperations.ClearBookmarks () { new MonoDevelop.TextEditor.Actions.ClearAllBookmarks  ().Execute (TextEditorControl.ActiveTextAreaControl.TextArea); }
#endregion
	}
}
