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
using System.Collections.ObjectModel;

namespace MonoDevelop.CSharp.SplitProject
{
	public class ProjectGraph
	{
		public sealed class Node : IEquatable<Node>
		{
			public readonly ProjectFile File;
			public bool Visited { get; set; }
			ISet<IType> typeDependencies = new HashSet<IType>();
			List<Node> destinationNodes = new List<Node>();
			List<Node> sourceNodes = new List<Node>();
			List<Node> activeDependentNodes = new List<Node>();

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

			public ReadOnlyCollection<Node> DestinationNodes {
				get { return destinationNodes.AsReadOnly (); }
			}

			public ReadOnlyCollection<Node> SourceNodes {
				get { return sourceNodes.AsReadOnly (); }
			}

			public List<Node> ActiveDependentNodes {
				get { return activeDependentNodes; }
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

			public override string ToString ()
			{
				int lastSlash = File.Name.LastIndexOf ('/');
				if (lastSlash == -1)
					return File.Name;
				return File.Name.Substring (lastSlash + 1);
			}

			public override bool Equals (object obj)
			{
				return Equals (obj as Node);
			}

			public bool Equals (Node node)
			{
				return node != null && File == node.File;
			}

			public override int GetHashCode ()
			{
				return File == null ? 0 : File.GetHashCode ();
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

		public void ResetVisitedNodes ()
		{
			foreach (var node in nodes) {
				node.Visited = false;
			}
		}

		public Node GetNodeForFile(ProjectFile file) {
			Node node;
			if (!nodesForProjectFiles.TryGetValue (file, out node)) {
				return null;
			}

			return node;
		}

		public ReadOnlyCollection<Node> Nodes {
			get {
				return nodes.AsReadOnly ();
			}
		}
	}
}

