
using System;
using S = MonoDevelop.Xml.StateEngine;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Projects.Gui.Completion;

namespace MonoDevelop.AddinAuthoring.CodeCompletion
{
	public class BaseXmlEditorExtension: CompletionTextEditorExtension
	{
		DocumentStateTracker<S.Parser> tracker;
		
		#region Setup and teardown
		
		public override void Initialize ()
		{
			base.Initialize ();
			S.Parser parser = new S.Parser (new S.XmlFreeState (), true);
			tracker = new DocumentStateTracker<S.Parser> (parser, Editor);
		}
		
		public override void Dispose ()
		{
			if (tracker != null)
				tracker = null;
			base.Dispose ();
		}
		
		#endregion
		
		#region Convenience accessors
		
		protected ITextBuffer Buffer {
			get {
				if (Document == null)
					throw new InvalidOperationException ("Editor extension not yet initialized");
				return Document.GetContent<ITextBuffer> ();
			}
		}
		
		protected IEditableTextBuffer EditableBuffer {
			get {
				if (Document == null)
					throw new InvalidOperationException ("Editor extension not yet initialized");
				return Document.GetContent<IEditableTextBuffer> ();
			}
		}
		
		#endregion
			
		public override ICompletionDataProvider CodeCompletionCommand (CodeCompletionContext completionContext)
		{
			int pos = completionContext.TriggerOffset;
			string txt = Editor.GetText (pos - 1, pos);
			int triggerWordLength = 0;
			ICompletionDataProvider cp = null;
			if (txt.Length > 0)
				cp = HandleCodeCompletion ((CodeCompletionContext) completionContext, true, ref triggerWordLength);
			
			return cp;
		}
		
		public override ICompletionDataProvider HandleCodeCompletion (
		    CodeCompletionContext completionContext, char completionChar, ref int triggerWordLength)
		{
			int pos = completionContext.TriggerOffset;
			if (pos > 0 && Editor.GetCharAt (pos - 1) == completionChar) {
				return HandleCodeCompletion ((CodeCompletionContext) completionContext, false, ref triggerWordLength);
			}
			return null;
		}
		
		ICompletionDataProvider HandleCodeCompletion (
		    CodeCompletionContext completionContext, bool forced, ref int triggerWordLength)
		{
			tracker.UpdateEngine ();
			
			//FIXME: lines in completionContext are zero-indexed, but ILocation and buffer are 1-indexed.
			//This could easily cause bugs.
			int line = completionContext.TriggerLine + 1, col = completionContext.TriggerLineOffset;
			
			ITextBuffer buf = this.Buffer;
			
			// completionChar may be a space even if the current char isn't, when ctrl-space is fired t
			int currentPosition = buf.CursorPosition - 1;
			char currentChar = buf.GetCharAt (currentPosition);
			char previousChar = buf.GetCharAt (currentPosition - 1);
			
			//decide whether completion will be auto-activated, to avoid unnecessary
			//parsing, which hurts editor responsiveness
			if (!forced) {
				//
				if (tracker.Engine.CurrentState is S.XmlFreeState && !(currentChar == '<' || currentChar == '>'))
					return null;
				
				if (tracker.Engine.CurrentState is S.XmlNameState 
				    && tracker.Engine.CurrentState.Parent is S.XmlAttributeState && previousChar != ' ')
					return null;
				
				if (tracker.Engine.CurrentState is S.XmlAttributeValueState 
				    && !(previousChar == '\'' || previousChar == '"' || currentChar =='\'' || currentChar == '"'))
					return null;
			}
			
			//tag completion
			if (currentChar == '<') {
				CodeCompletionDataProvider cp = new CodeCompletionDataProvider (null, GetAmbience ());
				
				if (tracker.Engine.CurrentState is S.XmlFreeState) {
					
					S.XElement el = tracker.Engine.Nodes.Peek () as S.XElement;
					AddTagCompletionData (cp, el);
				}
				return cp;
			}
			
			//closing tag completion
			if (tracker.Engine.CurrentState is S.XmlFreeState && currentPosition - 1 > 0 && currentChar == '>') {
				//get name of current node in document that's being ended
				S.XElement el = tracker.Engine.Nodes.Peek () as S.XElement;
				if (el != null && el.Position.End >= currentPosition && !el.IsClosed && el.IsNamed) {
					CodeCompletionDataProvider cp = new CodeCompletionDataProvider (null, GetAmbience ());
					cp.AddCompletionData (
					    new MonoDevelop.XmlEditor.Completion.XmlTagCompletionData (
					        String.Concat ("</", el.Name.FullName, ">"), 0, true)
					    );
					return cp;
				}
			}
			
			//attributes names within tags
			if (tracker.Engine.CurrentState is S.XmlTagState && forced || 
				(tracker.Engine.CurrentState is S.XmlNameState 
			 	 && tracker.Engine.CurrentState.Parent is S.XmlAttributeState
			         && tracker.Engine.CurrentStateLength == 1)
			) {
				int peekp = (tracker.Engine.CurrentState is S.XmlTagState) ? 0 : 1;
				S.XElement el = (S.XElement) tracker.Engine.Nodes.Peek (peekp);

				// HACK
				S.XElement pel = tracker.Engine.Nodes.Peek (peekp + 1) as S.XElement;
				if (el.Parent == null && pel != null)
					pel.AddChildNode (el);
				
				//attributes
				if (el != null && el.Name.IsValid && (forced || char.IsWhiteSpace (currentChar) ||
					(char.IsWhiteSpace (previousChar) && char.IsLetter (currentChar))))
				{
					CodeCompletionDataProvider cp = new CodeCompletionDataProvider (null, GetAmbience ());
					if (!forced)
						triggerWordLength = 1;
					
					AddAttributeCompletionData (cp, el);
					return cp;
				}
			}
			
			//attribute values
			//determine whether to trigger completion within attribute values quotes
			if ((tracker.Engine.CurrentState is S.XmlDoubleQuotedAttributeValueState
			    || tracker.Engine.CurrentState is S.XmlSingleQuotedAttributeValueState)
			    //trigger on the opening quote
			    && (tracker.Engine.CurrentStateLength == 0
			        //or trigger on first letter of value, if unforced
			        || (!forced && tracker.Engine.CurrentStateLength == 1))
			    ) {
				S.XAttribute att = (S.XAttribute) tracker.Engine.Nodes.Peek ();
				
				if (att.IsNamed) {
					S.XElement el = (S.XElement) tracker.Engine.Nodes.Peek (1);
	
					// HACK
					S.XElement pel = tracker.Engine.Nodes.Peek (2) as S.XElement;
					if (el.Parent == null && pel != null)
						pel.AddChildNode (el);
					
					char next = ' ';
					if (currentPosition + 1 < buf.Length)
						next = buf.GetCharAt (currentPosition + 1);
					
					char compareChar = (tracker.Engine.CurrentStateLength == 0)? currentChar : previousChar;
					Console.WriteLine ("ppa: " + att.Value);
					
					if ((compareChar == '"' || compareChar == '\'') 
					    && (next == compareChar || char.IsWhiteSpace (next))
					) {
						//if triggered by first letter of value, grab that letter
						if (tracker.Engine.CurrentStateLength == 1)
							triggerWordLength = 1;

						CodeCompletionDataProvider cp = new CodeCompletionDataProvider (null, GetAmbience ());
						AddAttributeValueCompletionData (cp, el, att);
						return cp;
					}
				}
			}
			
			return null; 
		}

		protected virtual void AddTagCompletionData (CodeCompletionDataProvider cp, S.XElement element)
		{
		}

		protected virtual void AddAttributeCompletionData (CodeCompletionDataProvider cp, S.XElement element)
		{
		}

		protected virtual void AddAttributeValueCompletionData (CodeCompletionDataProvider cp, S.XElement element, S.XAttribute attribute)
		{
		}
	}
}
