// Copyright (c) AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using ICSharpCode.NRefactory.TypeSystem;

namespace ICSharpCode.NRefactory.CSharp.Resolver
{
	/// <summary>
	/// Allows controlling which nodes are resolved by the resolve visitor.
	/// </summary>
	/// <seealso cref="ResolveVisitor"/>
	public interface IResolveVisitorNavigator
	{
		/// <summary>
		/// Asks the navigator whether to scan, skip, or resolve a node.
		/// </summary>
		ResolveVisitorNavigationMode Scan(AstNode node);
		
		/// <summary>
		/// Notifies the navigator that a node was resolved.
		/// </summary>
		void Resolved(AstNode node, ResolveResult result);
	}
	
	/// <summary>
	/// Represents the operation mode of the resolve visitor.
	/// </summary>
	/// <seealso cref="ResolveVisitor"/>
	public enum ResolveVisitorNavigationMode
	{
		/// <summary>
		/// Scan into the children of the current node, without resolving the current node.
		/// </summary>
		Scan,
		/// <summary>
		/// Skip the current node - do not scan into it; do not resolve it.
		/// </summary>
		Skip,
		/// <summary>
		/// Resolve the current node; but only scan subnodes which are not required for resolving the current node.
		/// </summary>
		Resolve,
		/// <summary>
		/// Resolves all nodes in the current subtree.
		/// </summary>
		ResolveAll
	}
}
