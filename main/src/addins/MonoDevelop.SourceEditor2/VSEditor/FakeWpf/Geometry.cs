using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xwt;

namespace System.Windows.Media
{
	public abstract class Geometry
	{
		public static Geometry Empty { get; } = new EmptyGeometry ();
		public abstract Rectangle Bounds { get; }

		public abstract bool IsEmpty ();
	}

	class EmptyGeometry : Geometry
	{
		public override Rectangle Bounds => Rectangle.Zero;

		public override bool IsEmpty ()
		{
			return true;
		}
	}

	public class GeometryGroup : Geometry
	{
		public List<Geometry> Children { get; } = new List<Geometry> ();

		public override Rectangle Bounds {
			get {
				Rectangle union = new Rectangle ();
				foreach (var c in Children)
					union = union.Union (c.Bounds);
				return union;
			}
		}
		public override bool IsEmpty ()
		{
			return !Children.Any (c => !c.IsEmpty ());
		}
	}

	public class RectangleGeometry : Geometry
	{
		public Rectangle Rectangle;
		public RectangleGeometry (Rectangle rectangle)
		{
			this.Rectangle = rectangle;
		}

		public override Rectangle Bounds => Rectangle;

		public override bool IsEmpty ()
		{
			return Rectangle.IsEmpty;
		}
	}
}
