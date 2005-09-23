// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="?" email="?"/>
//     <version value="$version"/>
// </file>

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
