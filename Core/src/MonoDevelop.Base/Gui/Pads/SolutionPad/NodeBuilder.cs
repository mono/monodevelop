//
// NodeBuilder.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;

namespace MonoDevelop.Gui.Pads
{
	public abstract class NodeBuilder: IDisposable
	{
		ITreeBuilderContext context;
		NodeCommandHandler commandHandler;
		
		internal NodeBuilder ()
		{
		}
		
		internal void SetContext (ITreeBuilderContext context)
		{
			this.context = context;
			Initialize ();
		}
		
		internal NodeCommandHandler CommandHandler {
			get {
//				if (commandHandler == null) {
					commandHandler = (NodeCommandHandler) Activator.CreateInstance (CommandHandlerType);
					commandHandler.Initialize (context.Tree);
//				}
				return commandHandler;
			}
		}
		
		protected virtual void Initialize ()
		{
		}
		
		protected ITreeBuilderContext Context {
			get { return context; }
		}
		
		public virtual Type CommandHandlerType {
			get { return typeof(NodeCommandHandler); }
		}
		
		public virtual void GetNodeAttributes (ITreeNavigator parentNode, object dataObject, ref NodeAttributes attributes)
		{
		}
		
		public virtual void BuildNode (ITreeBuilder treeBuilder, object dataObject, ref string label, ref Gdk.Pixbuf icon, ref Gdk.Pixbuf closedIcon)
		{
		}
		
		public virtual void BuildChildNodes (ITreeBuilder treeBuilder, object dataObject)
		{
		}
		
		public virtual bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			return false;
		}
		
		public virtual void OnNodeAdded (object dataObject)
		{
		}
		
		public virtual void OnNodeRemoved (object dataObject)
		{
		}

		public virtual void Dispose ()
		{
		}
		

		// Helper methods
		
		internal static bool HasAttribute (ITreeNavigator treeNavigator, NodeAttributes attr, NodeBuilder[] chain, object dataObject)
		{
			NodeAttributes nodeAttr = NodeAttributes.None;
			NodePosition pos = treeNavigator.CurrentPosition;
			
			foreach (NodeBuilder nb in chain) {
				nb.GetNodeAttributes (treeNavigator, dataObject, ref nodeAttr);
				treeNavigator.MoveToPosition (pos);
			}
			
			return (nodeAttr & attr) != 0;
		}
	}
}
