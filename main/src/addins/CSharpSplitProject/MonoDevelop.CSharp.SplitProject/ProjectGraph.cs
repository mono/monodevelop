//
// ProjectGraph.cs
//
// Author:
//       Luís Reis <luiscubal@gmail.com>
//
// Copyright (c) 2013 Luís Reis
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Linq;
using MonoDevelop.Projects;
using System.Collections.Generic;
using ICSharpCode.NRefactory.TypeSystem;

namespace MonoDevelop.CSharp.SplitProject
{
	public class ProjectGraph
	{
		public class Node
		{
			public readonly ProjectFile File;
			ISet<IType> typeDependencies = new HashSet<IType>();
			List<Node> destinationNodes = new List<Node>();
			List<Node> sourceNodes = new List<Node>();

			public Node(ProjectFile file) {
				File = file;
			}

			public void AddTypeDependency(IType type) {
				typeDependencies.Add (type);
			}

			public void AddTypeDependencies (IEnumerable<IType> types)
			{
				foreach (var type in types) {
					AddTypeDependency (type);
				}
			}

			public IEnumerable<IType> TypeDependencies {
				get { return typeDependencies; }
			}

			internal void AddDestination(Node node) {
				destinationNodes.Add (node);
			}

			public void AddEdgeTo(Node destination) {
				if (destination == this) {
					//No need to add edges to itself
					return;
				}

				if (destinationNodes.Contains (destination)) {
					//Edge already exists
					return;
				}

				destination.sourceNodes.Add (this);
				destinationNodes.Add (destination);
			}
		}

		List<Node> nodes = new List<Node>();
		Dictionary<ProjectFile, Node> nodesForProjectFiles = new Dictionary<ProjectFile, Node>();

		public void AddNode(Node node) {
			if (nodesForProjectFiles.ContainsKey (node.File)) {
				throw new InvalidOperationException ("There is already a node for that file");
			}

			nodes.Add (node);
			nodesForProjectFiles.Add (node.File, node);
		}

		public Node GetNodeForFile(ProjectFile file) {
			Node node;
			if (!nodesForProjectFiles.TryGetValue (file, out node)) {
				return null;
			}

			return node;
		}

		public IEnumerable<Node> Nodes {
			get {
				return nodes.AsReadOnly ();
			}
		}
	}
}

