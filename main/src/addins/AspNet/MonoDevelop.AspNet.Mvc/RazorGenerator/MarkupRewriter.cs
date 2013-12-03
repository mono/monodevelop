// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Web.Razor.Parser.SyntaxTree;
using System.Web.Razor.Parser;
using System;

namespace MonoDevelop.RazorGenerator
{
	// based on System.Web.Razor.Parser.MarkupRewriter
	// Copyright (c) Microsoft Open Technologies, Inc.
	// Licensed under the Apache License, Version 2.0
	//
	// VisitBlock has been rewritten so it actually visits the children of rewritten blocks
	//
	abstract class MarkupRewriter : ParserVisitor, ISyntaxTreeRewriter
	{
		private Stack<BlockBuilder> _blocks = new Stack<BlockBuilder>();

		protected MarkupRewriter()
		{
		}

		protected BlockBuilder Parent
		{
			get { return _blocks.Count > 0 ? _blocks.Peek() : null; }
		}

		public virtual Block Rewrite(Block input)
		{
			input.Accept(this);
			Debug.Assert(_blocks.Count == 1);
			return _blocks.Pop().Build();
		}

		public override void VisitBlock(Block block)
		{
			if (CanRewrite(block))
			{
				SyntaxTreeNode newNode = RewriteBlock(_blocks.Peek(), block);
				block = newNode as Block;
				if (block == null) {
					_blocks.Peek ().Children.Add (newNode);
					return;
				}
			}

			BlockBuilder builder = new BlockBuilder(block);
			builder.Children.Clear();
			_blocks.Push(builder);
			base.VisitBlock(block);
			Debug.Assert(ReferenceEquals(builder, _blocks.Peek()));

			if (_blocks.Count > 1)
			{
				_blocks.Pop();
				_blocks.Peek().Children.Add(builder.Build());
			}
		}

		public override void VisitSpan(Span span)
		{
			if (CanRewrite(span))
			{
				SyntaxTreeNode newNode = RewriteSpan(_blocks.Peek(), span);
				if (newNode != null)
				{
					_blocks.Peek().Children.Add(newNode);
				}
			}
			else
			{
				_blocks.Peek().Children.Add(span);
			}
		}

		protected virtual bool CanRewrite(Block block)
		{
			return false;
		}

		protected virtual bool CanRewrite(Span span)
		{
			return false;
		}

		protected virtual SyntaxTreeNode RewriteBlock(BlockBuilder parent, Block block)
		{
			throw new NotImplementedException();
		}

		protected virtual SyntaxTreeNode RewriteSpan(BlockBuilder parent, Span span)
		{
			throw new NotImplementedException();
		}
	}

	interface ISyntaxTreeRewriter
	{
		Block Rewrite(Block input);
	}
}