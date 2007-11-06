//  TipSection.cs
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

using System.Diagnostics;
using System.Drawing;

namespace MonoDevelop.TextEditor.Util
{
	abstract class TipSection
	{
		SizeF    tipAllocatedSize;
		Graphics tipGraphics;
		SizeF    tipMaxSize;
		SizeF    tipRequiredSize;
		
		public TipSection(Graphics graphics)
		{
			tipGraphics = graphics;
		}
		
		public abstract void Draw(PointF location);
		
		public SizeF GetRequiredSize()
		{
			return tipRequiredSize;
		}
		
		public void SetAllocatedSize(SizeF allocatedSize)
		{
			Debug.Assert(allocatedSize.Width >= tipRequiredSize.Width &&
			             allocatedSize.Height >= tipRequiredSize.Height);
			
			tipAllocatedSize = allocatedSize; OnAllocatedSizeChanged();
		}
		
		public void SetMaximumSize(SizeF maximumSize)
		{
			tipMaxSize = maximumSize; OnMaximumSizeChanged();
		}
		
		protected virtual void OnAllocatedSizeChanged()
		{
			
		}
		
		protected virtual void OnMaximumSizeChanged()
		{
			
		}
		
		protected void SetRequiredSize(SizeF requiredSize)
		{
			Debug.Assert(requiredSize.Width >= 0 &&
			             requiredSize.Width <= tipMaxSize.Width &&
			             requiredSize.Height >= 0 &&
			             requiredSize.Height <= tipMaxSize.Height);
			
			tipRequiredSize = requiredSize;
		}
		
		protected Graphics Graphics	{
			get {
				return tipGraphics;
			}
		}
		
		protected SizeF AllocatedSize {
			get {
				return tipAllocatedSize;
			}
		}
		
		protected SizeF MaximumSize {
			get {
				return tipMaxSize;
			}
		}
	}
}
