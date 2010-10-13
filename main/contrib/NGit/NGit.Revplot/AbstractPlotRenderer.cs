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
using NGit.Revplot;
using NGit.Revwalk;
using Sharpen;

namespace NGit.Revplot
{
	/// <summary>Basic commit graph renderer for graphical user interfaces.</summary>
	/// <remarks>
	/// Basic commit graph renderer for graphical user interfaces.
	/// <p>
	/// Lanes are drawn as columns left-to-right in the graph, and the commit short
	/// message is drawn to the right of the lane lines for this cell. It is assumed
	/// that the commits are being drawn as rows of some sort of table.
	/// <p>
	/// Client applications can subclass this implementation to provide the necessary
	/// drawing primitives required to display a commit graph. Most of the graph
	/// layout is handled by this class, allowing applications to implement only a
	/// handful of primitive stubs.
	/// <p>
	/// This class is suitable for us within an AWT TableCellRenderer or within a SWT
	/// PaintListener registered on a Table instance. It is meant to rubber stamp the
	/// graphics necessary for one row of a plotted commit list.
	/// <p>
	/// Subclasses should call
	/// <see cref="AbstractPlotRenderer{TLane, TColor}.PaintCommit(PlotCommit{L}, int)">AbstractPlotRenderer&lt;TLane, TColor&gt;.PaintCommit(PlotCommit&lt;L&gt;, int)
	/// 	</see>
	/// after they have
	/// otherwise configured their instance to draw one commit into the current
	/// location.
	/// <p>
	/// All drawing methods assume the coordinate space for the current commit's cell
	/// starts at (upper left corner is) 0,0. If this is not true (like say in SWT)
	/// the implementation must perform the cell offset computations within the
	/// various draw methods.
	/// </remarks>
	/// <?></?>
	/// <?></?>
	public abstract class AbstractPlotRenderer<TLane, TColor> where TLane:PlotLane
	{
		private const int LANE_WIDTH = 14;

		private const int LINE_WIDTH = 2;

		private const int LEFT_PAD = 2;

		/// <summary>Paint one commit using the underlying graphics library.</summary>
		/// <remarks>Paint one commit using the underlying graphics library.</remarks>
		/// <param name="commit">the commit to render in this cell. Must not be null.</param>
		/// <param name="h">total height (in pixels) of this cell.</param>
		protected internal virtual void PaintCommit(PlotCommit<TLane> commit, int h)
		{
			int dotSize = ComputeDotSize(h);
			TLane myLane = commit.GetLane();
			int myLaneX = LaneC(myLane);
			TColor myColor = LaneColor(myLane);
			int maxCenter = 0;
			foreach (TLane passingLane in (TLane[])commit.passingLanes)
			{
				int cx = LaneC(passingLane);
				TColor c = LaneColor(passingLane);
				DrawLine(c, cx, 0, cx, h, LINE_WIDTH);
				maxCenter = Math.Max(maxCenter, cx);
			}
			int nParent = commit.ParentCount;
			for (int i = 0; i < nParent; i++)
			{
				PlotCommit<TLane> p;
				TLane pLane;
				TColor pColor;
				int cx;
				p = (PlotCommit<TLane>)commit.GetParent(i);
				pLane = p.GetLane();
				if (pLane == null)
				{
					continue;
				}
				pColor = LaneColor(pLane);
				cx = LaneC(pLane);
				if (Math.Abs(myLaneX - cx) > LANE_WIDTH)
				{
					if (myLaneX < cx)
					{
						int ix = cx - LANE_WIDTH / 2;
						DrawLine(pColor, myLaneX, h / 2, ix, h / 2, LINE_WIDTH);
						DrawLine(pColor, ix, h / 2, cx, h, LINE_WIDTH);
					}
					else
					{
						int ix = cx + LANE_WIDTH / 2;
						DrawLine(pColor, myLaneX, h / 2, ix, h / 2, LINE_WIDTH);
						DrawLine(pColor, ix, h / 2, cx, h, LINE_WIDTH);
					}
				}
				else
				{
					DrawLine(pColor, myLaneX, h / 2, cx, h, LINE_WIDTH);
				}
				maxCenter = Math.Max(maxCenter, cx);
			}
			int dotX = myLaneX - dotSize / 2 - 1;
			int dotY = (h - dotSize) / 2;
			if (commit.GetChildCount() > 0)
			{
				DrawLine(myColor, myLaneX, 0, myLaneX, dotY, LINE_WIDTH);
			}
			if (commit.Has(RevFlag.UNINTERESTING))
			{
				DrawBoundaryDot(dotX, dotY, dotSize, dotSize);
			}
			else
			{
				DrawCommitDot(dotX, dotY, dotSize, dotSize);
			}
			int textx = Math.Max(maxCenter + LANE_WIDTH / 2, dotX + dotSize) + 8;
			int n = commit.refs == null ? 0 : commit.refs.Length;
			for (int i_1 = 0; i_1 < n; ++i_1)
			{
				textx += DrawLabel(textx + dotSize, h / 2, commit.refs[i_1]);
			}
			string msg = commit.GetShortMessage();
			DrawText(msg, textx + dotSize + n * 2, h / 2);
		}

		/// <summary>Draw a decoration for the Ref ref at x,y</summary>
		/// <param name="x">left</param>
		/// <param name="y">top</param>
		/// <param name="ref">A peeled ref</param>
		/// <returns>width of label in pixels</returns>
		protected internal abstract int DrawLabel(int x, int y, Ref @ref);

		private int ComputeDotSize(int h)
		{
			int d = (int)(Math.Min(h, LANE_WIDTH) * 0.50f);
			d += (d & 1);
			return d;
		}

		/// <summary>Obtain the color reference used to paint this lane.</summary>
		/// <remarks>
		/// Obtain the color reference used to paint this lane.
		/// <p>
		/// Colors returned by this method will be passed to the other drawing
		/// primitives, so the color returned should be application specific.
		/// <p>
		/// If a null lane is supplied the return value must still be acceptable to a
		/// drawing method. Usually this means the implementation should return a
		/// default color.
		/// </remarks>
		/// <param name="myLane">the current lane. May be null.</param>
		/// <returns>graphics specific color reference. Must be a valid color.</returns>
		protected internal abstract TColor LaneColor(TLane myLane);

		/// <summary>Draw a single line within this cell.</summary>
		/// <remarks>Draw a single line within this cell.</remarks>
		/// <param name="color">the color to use while drawing the line.</param>
		/// <param name="x1">starting X coordinate, 0 based.</param>
		/// <param name="y1">starting Y coordinate, 0 based.</param>
		/// <param name="x2">ending X coordinate, 0 based.</param>
		/// <param name="y2">ending Y coordinate, 0 based.</param>
		/// <param name="width">number of pixels wide for the line. Always at least 1.</param>
		protected internal abstract void DrawLine(TColor color, int x1, int y1, int x2, int
			 y2, int width);

		/// <summary>Draw a single commit dot.</summary>
		/// <remarks>
		/// Draw a single commit dot.
		/// <p>
		/// Usually the commit dot is a filled oval in blue, then a drawn oval in
		/// black, using the same coordinates for both operations.
		/// </remarks>
		/// <param name="x">upper left of the oval's bounding box.</param>
		/// <param name="y">upper left of the oval's bounding box.</param>
		/// <param name="w">width of the oval's bounding box.</param>
		/// <param name="h">height of the oval's bounding box.</param>
		protected internal abstract void DrawCommitDot(int x, int y, int w, int h);

		/// <summary>Draw a single boundary commit (aka uninteresting commit) dot.</summary>
		/// <remarks>
		/// Draw a single boundary commit (aka uninteresting commit) dot.
		/// <p>
		/// Usually a boundary commit dot is a light gray oval with a white center.
		/// </remarks>
		/// <param name="x">upper left of the oval's bounding box.</param>
		/// <param name="y">upper left of the oval's bounding box.</param>
		/// <param name="w">width of the oval's bounding box.</param>
		/// <param name="h">height of the oval's bounding box.</param>
		protected internal abstract void DrawBoundaryDot(int x, int y, int w, int h);

		/// <summary>Draw a single line of text.</summary>
		/// <remarks>
		/// Draw a single line of text.
		/// <p>
		/// The font and colors used to render the text are left up to the
		/// implementation.
		/// </remarks>
		/// <param name="msg">the text to draw. Does not contain LFs.</param>
		/// <param name="x">
		/// first pixel from the left that the text can be drawn at.
		/// Character data must not appear before this position.
		/// </param>
		/// <param name="y">
		/// pixel coordinate of the centerline of the text.
		/// Implementations must adjust this coordinate to account for the
		/// way their implementation handles font rendering.
		/// </param>
		protected internal abstract void DrawText(string msg, int x, int y);

		private int LaneX(PlotLane myLane)
		{
			int p = myLane != null ? myLane.GetPosition() : 0;
			return LEFT_PAD + LANE_WIDTH * p;
		}

		private int LaneC(PlotLane myLane)
		{
			return LaneX(myLane) + LANE_WIDTH / 2;
		}
	}
}
