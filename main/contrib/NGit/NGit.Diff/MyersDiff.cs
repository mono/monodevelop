/*
This code is derived from jgit (http://eclipse.org/jgit).
Copyright owners are documented in jgit's IP log.

This program and the accompanying materials are made available
under the terms of the Eclipse Distribution License v1.0 which
accompanies this distribution, is reproduced below, and is
available at http://www.eclipse.org/org/documents/edl-v10.php

All rights reserved.

Redistribution and use in source and binary forms, with or
without modification, are permitted provided that the following
conditions are met:

- Redistributions of source code must retain the above copyright
  notice, this list of conditions and the following disclaimer.

- Redistributions in binary form must reproduce the above
  copyright notice, this list of conditions and the following
  disclaimer in the documentation and/or other materials provided
  with the distribution.

- Neither the name of the Eclipse Foundation, Inc. nor the
  names of its contributors may be used to endorse or promote
  products derived from this software without specific prior
  written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND
CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES,
INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,
STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/

using System;
using NGit;
using NGit.Diff;
using NGit.Util;
using Sharpen;

namespace NGit.Diff
{
	/// <summary>
	/// Diff algorithm, based on "An O(ND) Difference Algorithm and its
	/// Variations", by Eugene Myers.
	/// </summary>
	/// <remarks>
	/// Diff algorithm, based on "An O(ND) Difference Algorithm and its
	/// Variations", by Eugene Myers.
	/// The basic idea is to put the line numbers of text A as columns ("x") and the
	/// lines of text B as rows ("y").  Now you try to find the shortest "edit path"
	/// from the upper left corner to the lower right corner, where you can
	/// always go horizontally or vertically, but diagonally from (x,y) to
	/// (x+1,y+1) only if line x in text A is identical to line y in text B.
	/// Myers' fundamental concept is the "furthest reaching D-path on diagonal k":
	/// a D-path is an edit path starting at the upper left corner and containing
	/// exactly D non-diagonal elements ("differences").  The furthest reaching
	/// D-path on diagonal k is the one that contains the most (diagonal) elements
	/// which ends on diagonal k (where k = y - x).
	/// Example:
	/// H E L L O   W O R L D
	/// ____
	/// L     \___
	/// O         \___
	/// W             \________
	/// Since every D-path has exactly D horizontal or vertical elements, it can
	/// only end on the diagonals -D, -D+2, ..., D-2, D.
	/// Since every furthest reaching D-path contains at least one furthest
	/// reaching (D-1)-path (except for D=0), we can construct them recursively.
	/// Since we are really interested in the shortest edit path, we can start
	/// looking for a 0-path, then a 1-path, and so on, until we find a path that
	/// ends in the lower right corner.
	/// To save space, we do not need to store all paths (which has quadratic space
	/// requirements), but generate the D-paths simultaneously from both sides.
	/// When the ends meet, we will have found "the middle" of the path.  From the
	/// end points of that diagonal part, we can generate the rest recursively.
	/// This only requires linear space.
	/// The overall (runtime) complexity is
	/// O(N * D^2 + 2 * N/2 * (D/2)^2 + 4 * N/4 * (D/4)^2 + ...)
	/// = O(N * D^2 * 5 / 4) = O(N * D^2),
	/// (With each step, we have to find the middle parts of twice as many regions
	/// as before, but the regions (as well as the D) are halved.)
	/// So the overall runtime complexity stays the same with linear space,
	/// albeit with a larger constant factor.
	/// </remarks>
	/// <?></?>
	public class MyersDiff<S> where S:Sequence
	{
		private sealed class _LowLevelDiffAlgorithm_110 : LowLevelDiffAlgorithm
		{
			public _LowLevelDiffAlgorithm_110()
			{
			}

			public override void DiffNonCommon<S>(EditList edits, HashedSequenceComparator<S>
				 cmp, HashedSequence<S> a, HashedSequence<S> b, Edit region)
			{
				new NGit.Diff.MyersDiff<S>(edits, cmp, a, b, region);
			}
		}

		/// <summary>Singleton instance of MyersDiff.</summary>
		/// <remarks>Singleton instance of MyersDiff.</remarks>
		public static readonly DiffAlgorithm INSTANCE = new _LowLevelDiffAlgorithm_110();

		/// <summary>
		/// The list of edits found during the last call to
		/// <see cref="MyersDiff{S}.CalculateEdits(Edit)">MyersDiff&lt;S&gt;.CalculateEdits(Edit)
		/// 	</see>
		/// </summary>
		protected internal EditList edits;

		/// <summary>Comparison function for sequences.</summary>
		/// <remarks>Comparison function for sequences.</remarks>
		protected internal HashedSequenceComparator<S> cmp;

		/// <summary>The first text to be compared.</summary>
		/// <remarks>The first text to be compared. Referred to as "Text A" in the comments</remarks>
		protected internal HashedSequence<S> a;

		/// <summary>The second text to be compared.</summary>
		/// <remarks>The second text to be compared. Referred to as "Text B" in the comments</remarks>
		protected internal HashedSequence<S> b;

		private MyersDiff(EditList edits, HashedSequenceComparator<S> cmp, HashedSequence
			<S> a, HashedSequence<S> b, Edit region)
		{
			middle = new MyersDiff<S>.MiddleEdit(this);
			this.edits = edits;
			this.cmp = cmp;
			this.a = a;
			this.b = b;
			CalculateEdits(region);
		}

		internal MyersDiff<S>.MiddleEdit middle;

		// TODO: use ThreadLocal for future multi-threaded operations
		/// <summary>Entrypoint into the algorithm this class is all about.</summary>
		/// <remarks>
		/// Entrypoint into the algorithm this class is all about. This method triggers that the
		/// differences between A and B are calculated in form of a list of edits.
		/// </remarks>
		/// <param name="r">portion of the sequences to examine.</param>
		private void CalculateEdits(Edit r)
		{
			middle.Initialize(r.beginA, r.endA, r.beginB, r.endB);
			if (middle.beginA >= middle.endA && middle.beginB >= middle.endB)
			{
				return;
			}
			CalculateEdits(middle.beginA, middle.endA, middle.beginB, middle.endB);
		}

		/// <summary>Calculates the differences between a given part of A against another given part of B
		/// 	</summary>
		/// <param name="beginA">start of the part of A which should be compared (0&lt;=beginA&lt;sizeof(A))
		/// 	</param>
		/// <param name="endA">end of the part of A which should be compared (beginA&lt;=endA&lt;sizeof(A))
		/// 	</param>
		/// <param name="beginB">start of the part of B which should be compared (0&lt;=beginB&lt;sizeof(B))
		/// 	</param>
		/// <param name="endB">end of the part of B which should be compared (beginB&lt;=endB&lt;sizeof(B))
		/// 	</param>
		protected internal virtual void CalculateEdits(int beginA, int endA, int beginB, 
			int endB)
		{
			Edit edit = middle.Calculate(beginA, endA, beginB, endB);
			if (beginA < edit.beginA || beginB < edit.beginB)
			{
				int k = edit.beginB - edit.beginA;
				int x = middle.backward.Snake(k, edit.beginA);
				CalculateEdits(beginA, x, beginB, k + x);
			}
			if (edit.GetType() != Edit.Type.EMPTY)
			{
				edits.Add(edits.Count, edit);
			}
			// after middle
			if (endA > edit.endA || endB > edit.endB)
			{
				int k = edit.endB - edit.endA;
				int x = middle.forward.Snake(k, edit.endA);
				CalculateEdits(x, endA, k + x, endB);
			}
		}

		/// <summary>
		/// A class to help bisecting the sequences a and b to find minimal
		/// edit paths.
		/// </summary>
		/// <remarks>
		/// A class to help bisecting the sequences a and b to find minimal
		/// edit paths.
		/// As the arrays are reused for space efficiency, you will need one
		/// instance per thread.
		/// The entry function is the calculate() method.
		/// </remarks>
		internal class MiddleEdit
		{
			internal virtual void Initialize(int beginA, int endA, int beginB, int endB)
			{
				this.beginA = beginA;
				this.endA = endA;
				this.beginB = beginB;
				this.endB = endB;
				// strip common parts on either end
				int k = beginB - beginA;
				this.beginA = this.forward.Snake(k, beginA);
				this.beginB = k + this.beginA;
				k = endB - endA;
				this.endA = this.backward.Snake(k, endA);
				this.endB = k + this.endA;
			}

			// TODO: measure speed impact when this is synchronized
			internal virtual Edit Calculate(int beginA, int endA, int beginB, int endB)
			{
				if (beginA == endA || beginB == endB)
				{
					return new Edit(beginA, endA, beginB, endB);
				}
				this.beginA = beginA;
				this.endA = endA;
				this.beginB = beginB;
				this.endB = endB;
				int minK = beginB - endA;
				int maxK = endB - beginA;
				this.forward.Initialize(beginB - beginA, beginA, minK, maxK);
				this.backward.Initialize(endB - endA, endA, minK, maxK);
				for (int d = 1; ; d++)
				{
					if (this.forward.Calculate(d) || this.backward.Calculate(d))
					{
						return this.edit;
					}
				}
			}

			internal MyersDiff<S>.MiddleEdit.EditPaths forward;

			internal MyersDiff<S>.MiddleEdit.EditPaths backward;

			protected internal int beginA;

			protected internal int endA;

			protected internal int beginB;

			protected internal int endB;

			protected internal Edit edit;

			internal abstract class EditPaths
			{
				private IntList x = new IntList();

				private LongList snake = new LongList();

				internal int beginK;

				internal int endK;

				internal int middleK;

				internal int prevBeginK;

				internal int prevEndK;

				internal int minK;

				internal int maxK;

				// TODO: better explanation
				internal int GetIndex(int d, int k)
				{
					// TODO: remove
					if (((d + k - this.middleK) % 2) != 0)
					{
						throw new RuntimeException(MessageFormat.Format(JGitText.Get().unexpectedOddResult
							, d, k, this.middleK));
					}
					return (d + k - this.middleK) / 2;
				}

				internal int GetX(int d, int k)
				{
					// TODO: remove
					if (k < this.beginK || k > this.endK)
					{
						throw new RuntimeException(MessageFormat.Format(JGitText.Get().kNotInRange, k, this
							.beginK, this.endK));
					}
					return this.x.Get(this.GetIndex(d, k));
				}

				internal long GetSnake(int d, int k)
				{
					// TODO: remove
					if (k < this.beginK || k > this.endK)
					{
						throw new RuntimeException(MessageFormat.Format(JGitText.Get().kNotInRange, k, this
							.beginK, this.endK));
					}
					return this.snake.Get(this.GetIndex(d, k));
				}

				private int ForceKIntoRange(int k)
				{
					if (k < this.minK)
					{
						return this.minK + ((k ^ this.minK) & 1);
					}
					else
					{
						if (k > this.maxK)
						{
							return this.maxK - ((k ^ this.maxK) & 1);
						}
					}
					return k;
				}

				internal virtual void Initialize(int k, int x, int minK, int maxK)
				{
					this.minK = minK;
					this.maxK = maxK;
					this.beginK = this.endK = this.middleK = k;
					this.x.Clear();
					this.x.Add(x);
					this.snake.Clear();
					this.snake.Add(this.NewSnake(k, x));
				}

				internal abstract int Snake(int k, int x);

				internal abstract int GetLeft(int x);

				internal abstract int GetRight(int x);

				internal abstract bool IsBetter(int left, int right);

				internal abstract void AdjustMinMaxK(int k, int x);

				internal abstract bool Meets(int d, int k, int x, long snake);

				internal long NewSnake(int k, int x)
				{
					long y = k + x;
					long ret = ((long)x) << 32;
					return ret | y;
				}

				internal int Snake2x(long snake)
				{
					return (int)((long)(((ulong)snake) >> 32));
				}

				internal int Snake2y(long snake)
				{
					return (int)snake;
				}

				internal bool MakeEdit(long snake1, long snake2)
				{
					int x1 = this.Snake2x(snake1);
					int x2 = this.Snake2x(snake2);
					int y1 = this.Snake2y(snake1);
					int y2 = this.Snake2y(snake2);
					if (x1 > x2 || y1 > y2)
					{
						x1 = x2;
						y1 = y2;
					}
					this._enclosing.edit = new Edit(x1, x2, y1, y2);
					return true;
				}

				internal virtual bool Calculate(int d)
				{
					this.prevBeginK = this.beginK;
					this.prevEndK = this.endK;
					this.beginK = this.ForceKIntoRange(this.middleK - d);
					this.endK = this.ForceKIntoRange(this.middleK + d);
					// TODO: handle i more efficiently
					// TODO: walk snake(k, getX(d, k)) only once per (d, k)
					// TODO: move end points out of the loop to avoid conditionals inside the loop
					// go backwards so that we can avoid temp vars
					for (int k = this.endK; k >= this.beginK; k -= 2)
					{
						int left = -1;
						int right = -1;
						long leftSnake = -1L;
						long rightSnake = -1L;
						// TODO: refactor into its own function
						if (k > this.prevBeginK)
						{
							int i = this.GetIndex(d - 1, k - 1);
							left = this.x.Get(i);
							int end = this.Snake(k - 1, left);
							leftSnake = left != end ? this.NewSnake(k - 1, end) : this.snake.Get(i);
							if (this.Meets(d, k - 1, end, leftSnake))
							{
								return true;
							}
							left = this.GetLeft(end);
						}
						if (k < this.prevEndK)
						{
							int i = this.GetIndex(d - 1, k + 1);
							right = this.x.Get(i);
							int end = this.Snake(k + 1, right);
							rightSnake = right != end ? this.NewSnake(k + 1, end) : this.snake.Get(i);
							if (this.Meets(d, k + 1, end, rightSnake))
							{
								return true;
							}
							right = this.GetRight(end);
						}
						int newX;
						long newSnake;
						if (k >= this.prevEndK || (k > this.prevBeginK && this.IsBetter(left, right)))
						{
							newX = left;
							newSnake = leftSnake;
						}
						else
						{
							newX = right;
							newSnake = rightSnake;
						}
						if (this.Meets(d, k, newX, newSnake))
						{
							return true;
						}
						this.AdjustMinMaxK(k, newX);
						int i_1 = this.GetIndex(d, k);
						this.x.Set(i_1, newX);
						this.snake.Set(i_1, newSnake);
					}
					return false;
				}

				internal EditPaths(MiddleEdit _enclosing)
				{
					this._enclosing = _enclosing;
				}

				private readonly MiddleEdit _enclosing;
			}

			internal class ForwardEditPaths : MyersDiff<S>.MiddleEdit.EditPaths
			{
				internal sealed override int Snake(int k, int x)
				{
					for (; x < this._enclosing.endA && k + x < this._enclosing.endB; x++)
					{
						if (!this._enclosing._enclosing.cmp.Equals(this._enclosing._enclosing.a, x, this.
							_enclosing._enclosing.b, k + x))
						{
							break;
						}
					}
					return x;
				}

				internal sealed override int GetLeft(int x)
				{
					return x;
				}

				internal sealed override int GetRight(int x)
				{
					return x + 1;
				}

				internal sealed override bool IsBetter(int left, int right)
				{
					return left > right;
				}

				internal sealed override void AdjustMinMaxK(int k, int x)
				{
					if (x >= this._enclosing.endA || k + x >= this._enclosing.endB)
					{
						if (k > this._enclosing.backward.middleK)
						{
							this.maxK = k;
						}
						else
						{
							this.minK = k;
						}
					}
				}

				internal sealed override bool Meets(int d, int k, int x, long snake)
				{
					if (k < this._enclosing.backward.beginK || k > this._enclosing.backward.endK)
					{
						return false;
					}
					// TODO: move out of loop
					if (((d - 1 + k - this._enclosing.backward.middleK) % 2) != 0)
					{
						return false;
					}
					if (x < this._enclosing.backward.GetX(d - 1, k))
					{
						return false;
					}
					this.MakeEdit(snake, this._enclosing.backward.GetSnake(d - 1, k));
					return true;
				}

				internal ForwardEditPaths(MiddleEdit _enclosing) : base(_enclosing)
				{
					this._enclosing = _enclosing;
				}

				private readonly MiddleEdit _enclosing;
			}

			internal class BackwardEditPaths : MyersDiff<S>.MiddleEdit.EditPaths
			{
				internal sealed override int Snake(int k, int x)
				{
					for (; x > this._enclosing.beginA && k + x > this._enclosing.beginB; x--)
					{
						if (!this._enclosing._enclosing.cmp.Equals(this._enclosing._enclosing.a, x - 1, this
							._enclosing._enclosing.b, k + x - 1))
						{
							break;
						}
					}
					return x;
				}

				internal sealed override int GetLeft(int x)
				{
					return x - 1;
				}

				internal sealed override int GetRight(int x)
				{
					return x;
				}

				internal sealed override bool IsBetter(int left, int right)
				{
					return left < right;
				}

				internal sealed override void AdjustMinMaxK(int k, int x)
				{
					if (x <= this._enclosing.beginA || k + x <= this._enclosing.beginB)
					{
						if (k > this._enclosing.forward.middleK)
						{
							this.maxK = k;
						}
						else
						{
							this.minK = k;
						}
					}
				}

				internal sealed override bool Meets(int d, int k, int x, long snake)
				{
					if (k < this._enclosing.forward.beginK || k > this._enclosing.forward.endK)
					{
						return false;
					}
					// TODO: move out of loop
					if (((d + k - this._enclosing.forward.middleK) % 2) != 0)
					{
						return false;
					}
					if (x > this._enclosing.forward.GetX(d, k))
					{
						return false;
					}
					this.MakeEdit(this._enclosing.forward.GetSnake(d, k), snake);
					return true;
				}

				internal BackwardEditPaths(MiddleEdit _enclosing) : base(_enclosing)
				{
					this._enclosing = _enclosing;
				}

				private readonly MiddleEdit _enclosing;
			}

			public MiddleEdit(MyersDiff<S> _enclosing)
			{
				this._enclosing = _enclosing;
				forward = new MyersDiff<S>.MiddleEdit.ForwardEditPaths(this);
				backward = new MyersDiff<S>.MiddleEdit.BackwardEditPaths(this);
			}

			private readonly MyersDiff<S> _enclosing;
		}

		/// <param name="args">two filenames specifying the contents to be diffed</param>
		public static void Main(string[] args)
		{
			if (args.Length != 2)
			{
				System.Console.Error.WriteLine(JGitText.Get().need2Arguments);
				System.Environment.Exit(1);
			}
			try
			{
				RawText a = new RawText(new FilePath(args[0]));
				RawText b = new RawText(new FilePath(args[1]));
				EditList r = INSTANCE.Diff(RawTextComparator.DEFAULT, a, b);
				System.Console.Out.WriteLine(r.ToString());
			}
			catch (Exception e)
			{
				Sharpen.Runtime.PrintStackTrace(e);
			}
		}
	}
}
