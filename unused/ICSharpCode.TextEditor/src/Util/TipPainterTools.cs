//  TipPainterTools.cs
//
//  This file was derived from a file from #Develop. 
//
//  Copyright (C) 2001-2007 http://www.icsharpcode.net/ <#Develop>
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
using System.Drawing;

namespace MonoDevelop.TextEditor.Util
{
	class TipPainterTools
	{
		const int SpacerSize = 4;
		
		private TipPainterTools()
		{
			
		}
		
		public static void DrawHelpTipFromCombinedDescription(Gtk.Widget control,
		                                                      Graphics graphics,
		                                                      Font font,
		                                                      string countMessage,
		                                                      string description)
		{
			string basicDescription = null;
			string documentation = null;

			if (IsVisibleText(description)) {
	     		string[] splitDescription = description.Split
	     			(new char[] { '\n' }, 2);
						
				if (splitDescription.Length > 0) {
					basicDescription = splitDescription[0];
					
					if (splitDescription.Length > 1) {
						documentation = splitDescription[1].Trim();
					}
				}
			}
			
			DrawHelpTip(control, graphics, font, countMessage,
			            basicDescription, documentation);
 		}

		public static void DrawHelpTip(Gtk.Widget control,
		                               Graphics graphics, Font font,
		                               string countMessage,
		                               string basicDescription,
		                               string documentation)
		{
			if (IsVisibleText(countMessage)     ||
			    IsVisibleText(basicDescription) ||
			    IsVisibleText(documentation)) {
				// Create all the TipSection objects.
				TipText countMessageTip = new TipText(graphics, font,
				                                      countMessage);
				
				TipSpacer countSpacer = new TipSpacer
					(graphics, new SizeF(IsVisibleText(countMessage) ? 4 : 0, 0));
				
				TipText descriptionTip = new TipText(graphics, font,
				                                     basicDescription);
				
				TipSpacer docSpacer = new TipSpacer
					(graphics, new SizeF(0, IsVisibleText(documentation) ? 4 : 0));
				
				TipText docTip = new TipText(graphics, font, documentation);
				
				// Now put them together.
				TipSplitter descSplitter = new TipSplitter(graphics, false,
				                                           descriptionTip,
				                                           docSpacer, docTip);
				
				TipSplitter mainSplitter = new TipSplitter(graphics, true,
				                                           countMessageTip,
				                                           countSpacer,
				                                           descSplitter);
				
				// Show it.
				TipPainter.DrawTip(control, graphics, mainSplitter);
			}
		}
		
		static bool IsVisibleText(string text)
		{
			return text != null && text.Length > 0;
		}
	}
}
