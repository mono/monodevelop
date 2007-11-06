//  CurrentPanelPanel.cs
//
//  This file was derived from a file from #Develop. 
//
//  Copyright (C) 2001-2007 Mike Krüger <mkrueger@novell.com>
// 
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
//  
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA

using System;
using System.Diagnostics;
using System.Collections;
using Gtk;
using Gdk;
using Pango;
using System.Xml;

using MonoDevelop.Core;
using Mono.Addins;

namespace MonoDevelop.Core.Gui.Dialogs
{
	public class CurrentPanelPanel : DrawingArea
	{
//		WizardDialog wizard;
//		Pixbuf backGround = null;
		
//		FontDescription normalFont;
		
		public CurrentPanelPanel(WizardDialog wizard)
		{
//			normalFont = Style.FontDescription;

//			this.wizard = wizard;
			//backGround = resourceService.GetBitmap("GeneralWizardBackground");
			//RequestSize = new Size (wizard.Width - 220, 30);
		}
		/*
		
		protected override void OnPaintBackground(PaintEventArgs pe)
		{
			//    		base.OnPaintBackground(pe);
			Graphics g = pe.Graphics;
			//			g.FillRectangle(new SolidBrush(SystemColors.Control), pe.ClipRectangle);
			
			g.FillRectangle(new LinearGradientBrush(new Point(0, 0), new Point(Width, Height),
			                                        Color.White,
			                                        SystemColors.Control),
			                                        new Rectangle(0, 0, Width, Height));
		}
		
		protected override void OnPaint(PaintEventArgs pe)
		{
			//    		base.OnPaint(pe);
/*
			Graphics g = pe.Graphics;
			g.DrawString(((IDialogPanelDescriptor)wizard.WizardPanels[wizard.ActivePanelNumber]).Label, normalFont, Brushes.Black,
			             10,
			             24 - normalFont.Height,
			             StringFormat.GenericTypographic);
			g.DrawLine(Pens.Black, 10, 24, Width - 10, 24);
 
		}*/
	}
}
