using System;

namespace Gdl
{
	[Flags]
	public enum DockItemBehavior
	{
		Normal,
		NeverFloating = 1 << 0,
		NeverVertical = 1 << 1,
		NeverHorizontal = 1 << 2,
		Locked = 1 << 3,
		CantDockTop = 1 << 4,
		CantDockBottom = 1 << 5,
		CantDockLeft = 1 << 6,
		CantDockRight = 1 << 7,
		CantDockCenter = 1 << 8,
		CantClose = 1 << 9,
		CantIconify = 1 << 10,
		NoGrip = 1 << 11,
	}
}
