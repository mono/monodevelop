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

using MonoDevelop.Core;

namespace MonoDevelop.Ide.Gui.Components
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
			try {
				Initialize ();
			} catch (Exception ex) {
				LoggingService.LogError (ex.ToString ());
			}
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
		
		public virtual void PrepareChildNodes (object dataObject)
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
		
		/// <summary>
		/// Return this constant in CompareToObject to instruct the tree view to
		/// use the default sorting rules for the compared objects.
		/// </summary>
		public const int DefaultSort = int.MinValue;
		
		/// <summary>
		/// Compares two nodes. Used when sorting the nodes in the tree.
		/// </summary>
		/// <returns>
		/// A value < 0 if thisNode is less than otherNode, 0 if equal, 1 if greater, <c>DefaultSort</c>
		/// if the default sort order has to be used.
		/// </returns>
		/// <param name='thisNode'>
		/// A node handled by this builder
		/// </param>
		/// <param name='otherNode'>
		/// Another node (which may not be handled by this builder)
		/// </param>
		/// <remarks>
		/// This method is used by ExtensibleTreeView to sort nodes. <c>thisNode</c> always points to a node
		/// which is handled by this builder, but <c>otherNode</c> can be any node, handled or not by this builder.
		/// The value <c>DefaultSort</c> can be returned to instruct that no order can be decided in this node, and
		/// that the default ordering should be used (by default, node names are compared)
		/// </remarks>
		public virtual int CompareObjects (ITreeNavigator thisNode, ITreeNavigator otherNode)
		{
			return DefaultSort;
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
