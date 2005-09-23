// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="?" email="?"/>
//     <version value="$version"/>
// </file>

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
