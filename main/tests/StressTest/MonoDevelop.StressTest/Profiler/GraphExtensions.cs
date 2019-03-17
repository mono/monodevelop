using System;
using QuickGraph;
using QuickGraph.Algorithms.Search;
using QuickGraph.Graphviz;

namespace MonoDevelop.StressTest
{
	public static class GraphExtensions
	{
		public static IEdgeListGraph<TVertex, TEdge> GetObjectGraph<TVertex, TEdge> (this IVertexListGraph<TVertex, TEdge> graph, TVertex obj) where TEdge : IEdge<TVertex>
		{
			var bfsa = new BreadthFirstSearchAlgorithm<TVertex, TEdge> (graph);

			var partialGraph = new AdjacencyGraph<TVertex, TEdge> ();
			bfsa.ExamineVertex += (vertex) => {
				partialGraph.AddVertex (vertex);
			};
			bfsa.ExamineEdge += (edge) => {
				partialGraph.AddEdge (edge);
			};
			bfsa.Compute (obj);

			return partialGraph;
		}

		public static GraphvizAlgorithm<HeapObject, TEdge> ToLeakGraphviz<TEdge> (this IEdgeListGraph<HeapObject, TEdge> graph, Heapshot heapshot) where TEdge : IEdge<HeapObject>
		{
			var graphviz = new GraphvizAlgorithm<HeapObject, TEdge> (graph);

			graphviz.FormatVertex += (sender, e) => {
				var currentObj = e.Vertex;

				// Look up the object and set its type name.
				var typeName = currentObj.TypeInfo.Name;

				var formatter = e.VertexFormatter;

				e.VertexFormatter.Label = typeName;
				// Append root information.
				if (heapshot.Roots.TryGetValue (currentObj.Address, out var rootRegisterEvent)) {
					e.VertexFormatter.Label = $"{typeName}\\nRoot Kind: {rootRegisterEvent.Source.ToString ()}";
					e.VertexFormatter.Shape = QuickGraph.Graphviz.Dot.GraphvizVertexShape.Box;
				} else {
					e.VertexFormatter.Label = typeName;
				}
			};

			return graphviz;
		}
	}
}
