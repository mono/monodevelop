/*
 * Copyright (C) 2004 Todd Berman <tberman@off.net>
 * Copyright (C) 2004 Jeroen Zwartepoorte <jeroen@xs4all.nl>
 * Copyright (C) 2005 John Luke <john.luke@gmail.com>
 *
 * based on work by:
 * Copyright (C) 2002 Gustavo Giráldez <gustavo.giraldez@gmx.net>
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330,
 * Boston, MA 02111-1307, USA.
 */

using System;
using Gtk;
using Gdk;

namespace Gdl
{
	public class DockRequest
	{
		private DockObject applicant;
		private DockObject target;
		private DockPlacement position;
		private int x, y, width, height;
		private object extra;
		
		public DockRequest ()
		{
		}
		
		public DockRequest (DockRequest copy)
		{
			applicant = copy.Applicant;
			target = copy.Target;
			x = copy.X;
			y = copy.Y;
			width = copy.Width;
			height = copy.Height;
			position = copy.Position;
			
			extra = copy.Extra;
		}
		
		public DockObject Applicant {
			get { return applicant; }
			set { applicant = value; }
		}
		
		public DockObject Target {
			get { return target; }
			set { target = value; }
		}
		
		public DockPlacement Position {
			get { return position; }
			set { position = value; }
		}

		public int X {
			get {
				return x;
			}
			set {
				x = value;
			}
		}
		
		public int Y {
			get {
				return y;
			}
			set {
				y = value;
			}
		}
		
		public int Width {
			get {
				return width;
			}
			set {
				width = value;
			}
		}
		
		public int Height {
			get {
				return height;
			}
			set {
				height = value;
			}
		}
						
		public object Extra {
			get { return extra; }
			set { extra = value; }
		}
	}
}
