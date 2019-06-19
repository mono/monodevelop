using System;
using System.ComponentModel.Composition;
using AppKit;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.DragDrop;
using Microsoft.VisualStudio.Utilities;
using MonoDevelop.DesignerSupport;
using MonoDevelop.DesignerSupport.Toolbox;
using MonoDevelop.Ide.Gui.Documents;

namespace MonoDevelop.TextEditor
{
	[Export (typeof (IDropHandlerProvider))]
	[DropFormat (ToolboxPad.ToolBoxDragDropFormat)]
	[Name (nameof(MonoDevelopToolBoxDropHandlerProvider))]
	sealed class MonoDevelopToolBoxDropHandlerProvider : IDropHandlerProvider
	{
		public IDropHandler GetAssociatedDropHandler (ICocoaTextView cocoaTextView)
		{
			return new MonoDevelopToolBoxDropHandler (cocoaTextView);
		}
	}

	sealed class MonoDevelopToolBoxDropHandler : IDropHandler
	{
		private ICocoaTextView cocoaTextView;

		public MonoDevelopToolBoxDropHandler (ICocoaTextView cocoaTextView)
		{
			this.cocoaTextView = cocoaTextView;
		}

		private bool TryGetConsumer (out IToolboxConsumer toolboxConsumer)
		{
			if (cocoaTextView.Properties.TryGetProperty<DocumentController> (typeof (DocumentController), out var controller)) {
				if (controller.GetContent<IToolboxConsumer> () is IToolboxConsumer consumer) {
					var selectedItem = DesignerSupport.DesignerSupport.Service.ToolboxService.SelectedItem;
					if (DesignerSupport.DesignerSupport.Service.ToolboxService.IsSupported (selectedItem, consumer)) {
						toolboxConsumer = consumer;
						return true;
					}
				}
			}
			toolboxConsumer = null;
			return false;
		}

		public DragDropPointerEffects HandleDataDropped (DragDropInfo dragDropInfo)
		{
			if (TryGetConsumer (out var consumer)) {
				cocoaTextView.Caret.MoveTo (dragDropInfo.VirtualBufferPosition);
				consumer.ConsumeItem (DesignerSupport.DesignerSupport.Service.ToolboxService.SelectedItem);
				return DragDropPointerEffects.Move | DragDropPointerEffects.Track;
			}
			return DragDropPointerEffects.None;
		}

		public void HandleDragCanceled ()
		{

		}

		public DragDropPointerEffects HandleDraggingOver (DragDropInfo dragDropInfo)
		{
			if (TryGetConsumer (out var _))
				return DragDropPointerEffects.Move | DragDropPointerEffects.Track;
			else
				return DragDropPointerEffects.None;
		}

		public DragDropPointerEffects HandleDragStarted (DragDropInfo dragDropInfo)
		{
			if (TryGetConsumer (out var _))
				return DragDropPointerEffects.Move | DragDropPointerEffects.Track;
			else
				return DragDropPointerEffects.None;
		}

		public bool IsDropEnabled (DragDropInfo dragDropInfo)
		{
			return true;
		}
	}

}
