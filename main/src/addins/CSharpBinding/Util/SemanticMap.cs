// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace ICSharpCode.NRefactory6.CSharp
{
	public partial class SemanticMap
	{
		private readonly Dictionary<SyntaxNode, SymbolInfo> _expressionToInfoMap =
			new Dictionary<SyntaxNode, SymbolInfo>();

		private readonly Dictionary<SyntaxToken, SymbolInfo> _tokenToInfoMap =
			new Dictionary<SyntaxToken, SymbolInfo>();

		private SemanticMap()
		{
		}

		internal static SemanticMap From(SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken)
		{
			var map = new SemanticMap();
			var walker = new Walker(semanticModel, map, cancellationToken);
			walker.Visit(node);
			return map;
		}

		public IEnumerable<ISymbol> AllReferencedSymbols
		{
			get
			{
				return _expressionToInfoMap.Values.Concat(_tokenToInfoMap.Values).Select(info => info.Symbol).Distinct();
			}
		}

		private class Walker : SyntaxWalker
		{
			private readonly SemanticModel _semanticModel;
			private readonly SemanticMap _map;
			private readonly CancellationToken _cancellationToken;

			public Walker(SemanticModel semanticModel, SemanticMap map, CancellationToken cancellationToken) :
			base(SyntaxWalkerDepth.Token)
			{
				_semanticModel = semanticModel;
				_map = map;
				_cancellationToken = cancellationToken;
			}

			public override void Visit(SyntaxNode node)
			{
				var info = _semanticModel.GetSymbolInfo(node);
				if (!IsNone(info))
				{
					_map._expressionToInfoMap.Add(node, info);
				}

				base.Visit(node);
			}

			protected override void VisitToken(SyntaxToken token)
			{
				var info = _semanticModel.GetSymbolInfo(token, _cancellationToken);
				if (!IsNone(info))
				{
					_map._tokenToInfoMap.Add(token, info);
				}

				base.VisitToken(token);
			}

			private bool IsNone(SymbolInfo info)
			{
				return info.Symbol == null && info.CandidateSymbols.Length == 0;
			}
		}
	}
}
