
using System;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Gui.Completion;
using MonoDevelop.Projects.Parser;
using MonoDevelop.Projects.Ambience;

namespace MonoDevelop.Ide.Gui.Content
{
	public class TextEditorExtension: ITextEditorExtension, IDisposable
	{
		internal ITextEditorExtension Next;
		internal Document document;
		ICodeCompletionContext currentCompletionContext;
		ICompletionWidget completionWidget;
		bool autoHideCompletionWindow = true;
		
		internal void Initialize (Document document)
		{
			this.document = document;
			IExtensibleTextEditor editor = document.GetContent <IExtensibleTextEditor> ();
			completionWidget = editor.GetCompletionWidget ();
			if (completionWidget != null)
				completionWidget.CompletionContextChanged += OnCompletionContextChanged;
			Initialize ();
		}
		
		protected Document Document {
			get { return document; }
		}
		
		protected IEditableTextBuffer Editor {
			get { return document.GetContent<IEditableTextBuffer> (); }
		}
		
		protected string FileName {
			get {
				IViewContent view = document.Window.ViewContent;
				return view.IsUntitled ? view.UntitledName : view.ContentName;
			}
		}
		
		protected IParserContext GetParserContext ()
		{
			CheckInitialized ();
			
			IViewContent view = document.Window.ViewContent;
			string file = view.IsUntitled ? view.UntitledName : view.ContentName;
			Project project = view.Project;
			IParserDatabase pdb = IdeApp.ProjectOperations.ParserDatabase;
			
			if (project != null)
				return pdb.GetProjectParserContext (project);
			else
				return pdb.GetFileParserContext (file);
		}
		
		protected MonoDevelop.Projects.Ambience.Ambience GetAmbience ()
		{
			CheckInitialized ();
			
			IViewContent view = document.Window.ViewContent;
			Project project = view.Project;
			
			if (project != null)
				return project.Ambience;
			else {
				string file = view.IsUntitled ? view.UntitledName : view.ContentName;
				return MonoDevelop.Projects.Services.Ambience.GetAmbienceForFile (file);
			}
		}
		
		public virtual bool ExtendsEditor (Document doc, IEditableTextBuffer editor)
		{
			return true;
		}
		
		// When a key is pressed, and before the key is processed by the editor, this method will be invoked.
		// Return true if the key press should be processed by the editor.
		public virtual bool KeyPress (Gdk.Key key, Gdk.ModifierType modifier)
		{
			CheckInitialized ();
			
			bool res;
			
			if (currentCompletionContext != null) {
				autoHideCompletionWindow = false;
				if (CompletionListWindow.ProcessKeyEvent (key, modifier)) {
					autoHideCompletionWindow = true;
					return true;
				}
				autoHideCompletionWindow = false;
			}
			
			if (ParameterInformationWindowManager.IsWindowVisible) {
				if (ParameterInformationWindowManager.ProcessKeyEvent (key, modifier))
					return true;
				autoHideCompletionWindow = false;
			}
			
			if (Next == null)
				res = true;
			else
				res = Next.KeyPress (key, modifier);

			// Handle code completion
			
			if (completionWidget != null && currentCompletionContext == null) {
				ICompletionDataProvider cp = null;
				if (key == Gdk.Key.space && (modifier & Gdk.ModifierType.ControlMask) != 0) {
					int cpos = GetCompletionCommandOffset ();
					if (cpos == -1) cpos = Editor.CursorPosition;
					currentCompletionContext = completionWidget.CreateCodeCompletionContext (cpos);
					cp = CodeCompletionCommand (currentCompletionContext);
				}
				else {
					currentCompletionContext = completionWidget.CreateCodeCompletionContext (Editor.CursorPosition);
					cp = HandleCodeCompletion (currentCompletionContext, (char)(uint)key);
				}
					
				if (cp != null)
					CompletionListWindow.ShowWindow ((char)(uint)key, cp, completionWidget, currentCompletionContext, OnCompletionWindowClosed);
				else
					currentCompletionContext = null;
			}
			
			// Handle parameter completion
			
			if (ParameterInformationWindowManager.IsWindowVisible)
				ParameterInformationWindowManager.PostProcessKeyEvent (key, modifier);

			ICodeCompletionContext ctx = completionWidget.CreateCodeCompletionContext (Editor.CursorPosition);
			IParameterDataProvider paramProvider = HandleParameterCompletion (ctx, (char)(uint)key);
			if (paramProvider != null) {
				ParameterInformationWindowManager.ShowWindow (ctx, paramProvider);
			}
			
			autoHideCompletionWindow = true;
			
			return res;
		}
		
		void OnCompletionWindowClosed ()
		{
			currentCompletionContext = null;
		}
		
		void OnCompletionContextChanged (object o, EventArgs a)
		{
			if (autoHideCompletionWindow) {
				CompletionListWindow.HideWindow ();
				ParameterInformationWindowManager.HideWindow ();
			}
		}

		public virtual void CursorPositionChanged ()
		{
			CheckInitialized ();
			
			if (Next != null)
				Next.CursorPositionChanged ();
		}
		
		public virtual ICompletionDataProvider HandleCodeCompletion (ICodeCompletionContext completionContext, char completionChar)
		{
			return null;
		}
		
		public virtual IParameterDataProvider HandleParameterCompletion (ICodeCompletionContext completionContext, char completionChar)
		{
			return null;
		}
		
		// return -1 if completion can't be shown
		public virtual int GetCompletionCommandOffset ()
		{
			int pos = Editor.CursorPosition;
			while (pos >= 0) {
				string txt = Editor.GetText (pos - 1, pos);
				if (txt == null || txt.Length == 0)
					return -1;
				char c = txt [0];
				if (!char.IsLetterOrDigit (c) && c != '_')
					return pos;
				pos--;
			}
			return -1;
		}
		
		public virtual ICompletionDataProvider CodeCompletionCommand (ICodeCompletionContext completionContext)
		{
			// This default implementation of CodeCompletionCommand calls HandleCodeCompletion providing
			// the char at the cursor position. If it returns a provider, just return it.
			
			int pos = completionContext.TriggerOffset;
			string txt = Editor.GetText (pos - 1, pos);
			if (txt.Length > 0) {
				ICompletionDataProvider cp = HandleCodeCompletion (completionContext, txt[0]);
				if (cp != null)
					return cp;
			}
			
			// If there is a parser context, try resolving by calling CtrlSpace.
			IParserContext ctx = GetParserContext();
			if (ctx != null) {
				CodeCompletionDataProvider completionProvider = new CodeCompletionDataProvider (ctx, GetAmbience ());
				completionProvider.AddResolveResults (ctx.CtrlSpace (completionContext.TriggerLine + 1, completionContext.TriggerLineOffset + 1, FileName));
				if (!completionProvider.IsEmpty)
					return completionProvider;
			}
			return null;
		}
		
		public virtual void Initialize ()
		{
			CheckInitialized ();
			
			TextEditorExtension next = Next as TextEditorExtension;
			if (next != null)
				next.Initialize ();
		}
		
		public virtual void Dispose ()
		{
			document = null;
		}
		
		void CheckInitialized ()
		{
			if (document == null)
				throw new InvalidOperationException ("Editor extension not yet initialized");
		}
	}
	
	public interface ITextEditorExtension
	{
		bool KeyPress (Gdk.Key key, Gdk.ModifierType modifier);
		void CursorPositionChanged ();
	}
}
