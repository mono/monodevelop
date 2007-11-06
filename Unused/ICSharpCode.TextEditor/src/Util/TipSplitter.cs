//  TipSplitter.cs
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
using System.Diagnostics;
using System.Drawing;

namespace MonoDevelop.TextEditor.Util
{
	class TipSplitter: TipSection
	{
		bool         isHorizontal;
		float     [] offsets;
		TipSection[] tipSections;
		
		public TipSplitter(Graphics graphics, bool horizontal,
		                   params TipSection[] sections): base(graphics)
		{
			Debug.Assert(sections != null);
			
			isHorizontal = horizontal;
			offsets = new float[sections.Length];
			tipSections = (TipSection[])sections.Clone();	
		}
		
		public override void Draw(PointF location)
		{
			if (isHorizontal) {
				for (int i = 0; i < tipSections.Length; i ++) {
					tipSections[i].Draw
						(new PointF(location.X + offsets[i], location.Y));
				}
			} else {
				for (int i = 0; i < tipSections.Length; i ++) {
					tipSections[i].Draw
						(new PointF(location.X, location.Y + offsets[i]));
				}
			}
		}
		
		protected override void OnMaximumSizeChanged()
		{
			base.OnMaximumSizeChanged();
			
			float currentDim = 0;
			float otherDim = 0;
			SizeF availableArea = MaximumSize;
			
			for (int i = 0; i < tipSections.Length; i ++) {
				TipSection section = (TipSection)tipSections[i];
			
				section.SetMaximumSize(availableArea);
				
				SizeF requiredArea = section.GetRequiredSize();
				offsets[i] = currentDim;

				// It's best to start on pixel borders, so this will
				// round up to the nearest pixel. Otherwise there are
				// weird cutoff artifacts.
				float pixelsUsed;
				
				if (isHorizontal) {
					pixelsUsed  = (float)Math.Ceiling(requiredArea.Width);
					currentDim += pixelsUsed;
					
					availableArea.Width = Math.Max
						(0, availableArea.Width - pixelsUsed);
					
					otherDim = Math.Max(otherDim, requiredArea.Height);
				} else {
					pixelsUsed  = (float)Math.Ceiling(requiredArea.Height);
					currentDim += pixelsUsed;
					
					availableArea.Height = Math.Max
						(0, availableArea.Height - pixelsUsed);
					
					otherDim = Math.Max(otherDim, requiredArea.Width);
				}
			}
			
			foreach (TipSection section in tipSections) {
				if (isHorizontal) {
					section.SetAllocatedSize
						(new SizeF(section.GetRequiredSize().Width, otherDim));
				} else {
					section.SetAllocatedSize
						(new SizeF(otherDim, section.GetRequiredSize().Height));
				}
			}

			if (isHorizontal) {
				SetRequiredSize(new SizeF(currentDim, otherDim));
			} else {
				SetRequiredSize(new SizeF(otherDim, currentDim));
			}
		}
	}
}
