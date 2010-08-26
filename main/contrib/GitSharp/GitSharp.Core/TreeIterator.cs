/*
 * Copyright (C) 2008, Robin Rosenberg <robin.rosenberg@dewire.com>
 * Copyright (C) 2006, Shawn O. Pearce <spearce@spearce.org>
 * Copyright (C) 2008, Kevin Thompson <kevin.thompson@theautomaters.com>
 * 
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or
 * without modification, are permitted provided that the following
 * conditions are met:
 *
 * - Redistributions of source code must retain the above copyright
 *   notice, this list of conditions and the following disclaimer.
 *
 * - Redistributions in binary form must reproduce the above
 *   copyright notice, this list of conditions and the following
 *   disclaimer in the documentation and/or other materials provided
 *   with the distribution.
 *
 * - Neither the name of the Git Development Community nor the
 *   names of its contributors may be used to endorse or promote
 *   products derived from this software without specific prior
 *   written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND
 * CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES,
 * INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
 * OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
 * CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
 * NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
 * CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,
 * STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
 * ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.IO;

namespace GitSharp.Core
{
	public class TreeIterator
	{
		private readonly Tree _tree;
		private readonly Order _order;
		private readonly bool _visitTreeNodes;

		private int _index;
		private TreeIterator _sub;
		private bool _hasVisitedTree;

		/// <summary>
		/// Traversal order
		/// </summary>
		[Serializable]
		public enum Order
		{
			/// <summary>
			/// Visit node first, then leaves
			/// </summary>
			PREORDER,

			/// <summary>
			/// Visit leaves first, then node
			/// </summary>
			POSTORDER
		};

		/// <summary>
		/// Construct a <see cref="TreeIterator"/> for visiting all non-tree nodes.
		/// </summary>
		/// <param name="start"></param>
		public TreeIterator(Tree start)
			: this(start, Order.PREORDER, false)
		{
		}

		/// <summary>
		/// Construct a <see cref="TreeIterator"/> for visiting all nodes in a
		/// tree in a given order
		/// </summary>
		/// <param name="start">Root node</param>
		/// <param name="order"><see cref="Order"/></param>
		public TreeIterator(Tree start, Order order)
			: this(start, order, true)
		{
		}

		/// <summary>
		/// Construct a <see cref="TreeIterator"/>.
		/// </summary>
		/// <param name="start">First node to visit</param>
		/// <param name="order">Visitation <see cref="Order"/></param>
		/// <param name="visitTreeNode">True to include tree node</param>
		private TreeIterator(Tree start, Order order, bool visitTreeNode)
		{
			_tree = start;
			_visitTreeNodes = visitTreeNode;
			_index = -1;
			_order = order;
			if (!_visitTreeNodes)
			{
				_hasVisitedTree = true;
			}

			try
			{
				Step();
			}
			catch (IOException e)
			{
				throw new Exception(string.Empty, e);
			}
		}

		private TreeEntry NextTreeEntry()
		{
			if (_sub != null)
				return _sub.NextTreeEntry();

			if (_index < 0 && _order == Order.PREORDER)
				return _tree;

			if (_order == Order.POSTORDER && _index == _tree.MemberCount)
				return _tree;

			if (_tree.Members.Length <= _index)
				return null;

			return _tree.Members[_index];
		}

		private bool HasNextTreeEntry()
		{
			if (_tree == null) return false;

			return _sub != null || _index < _tree.MemberCount || _order == Order.POSTORDER && _index == _tree.MemberCount;
		}

		private bool Step()
		{
			if (_tree == null) return false;
			if (_sub != null)
			{
				if (_sub.Step()) return true;
				_sub = null;
			}

			if (_index < 0 && !_hasVisitedTree && _order == Order.PREORDER)
			{
				_hasVisitedTree = true;
				return true;
			}

			while (++_index < _tree.MemberCount)
			{
				Tree e = (_tree.Members[_index] as Tree);
				if (e != null)
				{
					_sub = new TreeIterator(e, _order, _visitTreeNodes);
					if (_sub.HasNextTreeEntry()) return true;
					_sub = null;
					continue;
				}
				return true;
			}

			if (_index == _tree.MemberCount && !_hasVisitedTree && _order == Order.POSTORDER)
			{
				_hasVisitedTree = true;
				return true;
			}

			return false;
		}


		public bool hasNext()
		{
			return HasNextTreeEntry();
		}

		public TreeEntry next()
		{
			TreeEntry ret = NextTreeEntry();
			Step();
			return ret;
		}
	}
}