
using System;

using MonoDevelop.Ide.Gui;

namespace MonoDevelop.DesignerSupport.CodeBehind
{
	
	
	public class CodeBehindViewContent : MonoDevelop.DesignerSupport.WrapperDesignView, ISecondaryViewContent
	{
		Gtk.Label nameLabel;
		
		public CodeBehindViewContent (IViewContent content)
			: base (content)
		{
			Gtk.Label nameLabel = new Gtk.Label ("CodeBehind file: "+System.IO.Path.GetFileName (Content.ContentName));
			nameLabel.Xpad = 3;
			nameLabel.Show ();
			base.TopBar = nameLabel;
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
			get { return "CodeBehind"; }
		}
		
		public override bool CanReuseView (string fileName)
		{
			return (Content.ContentName == fileName);
		}
	}
}
