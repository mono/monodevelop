
using System;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Gui.Completion;
using MonoDevelop.Projects.Parser;
using MonoDevelop.Projects.Ambience;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Commands;

namespace MonoDevelop.Ide.Gui.Content
{
	public class TextEditorExtension: ITextEditorExtension, ICommandRouter, IDisposable
	{
		internal ITextEditorExtension Next;
		internal Document document;
		CodeCompletionContext currentCompletionContext;
		ICompletionWidget completionWidget;
		bool autoHideCompletionWindow = true;
		
		internal void Initialize (Document document)
		{
			this.document = document;
			completionWidget = document.GetContent <ICompletionWidget> ();
			if (completionWidget != null)
				completionWidget.CompletionContextChanged += OnCompletionContextChanged;
			Initialize ();
		}
		
		protected Document Document {
			get { return document; }
		}
		
		protected TextEditor Editor {
			get { return document.TextEditor; }
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
				currentCompletionContext = completionWidget.CreateCodeCompletionContext (Editor.CursorPosition);
				ICompletionDataProvider cp = HandleCodeCompletion (currentCompletionContext, (char)(uint)key);
					
				if (cp != null)
					CompletionListWindow.ShowWindow ((char)(uint)key, cp, completionWidget, currentCompletionContext, OnCompletionWindowClosed);
				else
					currentCompletionContext = null;
			}
			
			// Handle parameter completion
			
			if (ParameterInformationWindowManager.IsWindowVisible)
				ParameterInformationWindowManager.PostProcessKeyEvent (key, modifier);

			if (completionWidget != null) {
				ICodeCompletionContext ctx = completionWidget.CreateCodeCompletionContext (Editor.CursorPosition);
				IParameterDataProvider paramProvider = HandleParameterCompletion (ctx, (char)(uint)key);
				if (paramProvider != null)
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
		
		[CommandUpdateHandler (TextEditorCommands.ShowCompletionWindow)]
		internal void OnUpdateCompletionCommand (CommandInfo info)
		{
			info.Bypass = !CanRunCompletionCommand ();
		}
		
		[CommandHandler (TextEditorCommands.ShowCompletionWindow)]
		public virtual void RunCompletionCommand ()
		{
			ICompletionDataProvider cp = null;
			int cpos, wlen;
			if (!GetCompletionCommandOffset (out cpos, out wlen)) {
				cpos = Editor.CursorPosition;
				wlen = 0;
			}
			currentCompletionContext = completionWidget.CreateCodeCompletionContext (cpos);
			currentCompletionContext.TriggerWordLength = wlen;
			cp = CodeCompletionCommand (currentCompletionContext);
				
			if (cp != null)
				CompletionListWindow.ShowWindow ((char)0, cp, completionWidget, currentCompletionContext, OnCompletionWindowClosed);
			else
				currentCompletionContext = null;
		}
		
		public virtual bool CanRunCompletionCommand ()
		{
			return (completionWidget != null && currentCompletionContext == null);
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
		public virtual bool GetCompletionCommandOffset (out int cpos, out int wlen)
		{
			cpos = wlen = 0;
			int pos = Editor.CursorPosition - 1;
			while (pos >= 0) {
				char c = Editor.GetCharAt (pos);
				if (!char.IsLetterOrDigit (c) && c != '_')
					break;
				pos--;
			}
			if (pos == -1)
				return false;
			
			pos++;
			cpos = pos;
			int len = Editor.TextLength;
			
			while (pos < len) {
				char c = Editor.GetCharAt (pos);
				if (!char.IsLetterOrDigit (c) && c != '_')
					break;
				pos++;
			}
			wlen = pos - cpos;
			return true;
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
		
		object ITextEditorExtension.GetExtensionCommandTarget ()
		{
			return this;
		}
		
		object ICommandRouter.GetNextCommandTarget ()
		{
			if (Next != null)
				return Next.GetExtensionCommandTarget ();
			else
				return null;
		}
	}
	
	public interface ITextEditorExtension
	{
		bool KeyPress (Gdk.Key key, Gdk.ModifierType modifier);
		void CursorPositionChanged ();
		
		// Return the object that is going to process commands, or null
		// if commands don't need custom processing
		object GetExtensionCommandTarget ();
	}
	
	class TextEditorExtensionMarker: TextEditorExtension
	{
		public override bool ExtendsEditor (Document doc, IEditableTextBuffer editor)
		{
			return false;
		}
	}
}
