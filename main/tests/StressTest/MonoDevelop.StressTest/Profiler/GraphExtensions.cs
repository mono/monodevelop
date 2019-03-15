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

		public static GraphvizAlgorithm<long, TEdge> ToLeakGraphviz<TEdge> (this IEdgeListGraph<long, TEdge> graph, Heapshot heapshot) where TEdge : IEdge<long>
		{
			var graphviz = new GraphvizAlgorithm<long, TEdge> (graph);

			graphviz.FormatVertex += (sender, e) => {
				var currentObj = e.Vertex;

				// Look up the object and set its type name.
				var currentType = heapshot.ObjectToType[currentObj];
				var typeName = heapshot.ClassInfos[currentType].Name;

				var formatter = e.VertexFormatter;

				e.VertexFormatter.Label = typeName;
				// Append root information.
				if (heapshot.Roots.TryGetValue (currentObj, out var rootRegisterEvent)) {
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
