
using System;

using MonoDevelop.Ide.Gui;

namespace MonoDevelop.DesignerSupport.CodeBehind
{
	
	
	public class CodeBehindViewContent : MonoDevelop.DesignerSupport.WrapperDesignView, ISecondaryViewContent
	{
		
		public CodeBehindViewContent (IViewContent content)
			: base (content)
		{
		}
		
		public virtual void Selected()
		{
		}
		
		public virtual void Deselected()
		{
		}
		
		public virtual void NotifyBeforeSave()
		{
		}

		public virtual void BaseContentChanged ()
		{
		}
		
		public override string TabPageLabel {
			get { return "CodeBehind: " + System.IO.Path.GetFileName (Content.ContentName); }
		}
		
		public override bool CanReuseView (string fileName)
		{
			return (Content.ContentName == fileName);
		}
	}
}
