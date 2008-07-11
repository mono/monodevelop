using System;

namespace Stetic.Editor {

	[PropertyEditor ("File", "Changed")]
	public class ImageFile : Image {

		public ImageFile () : base (false, true) { }
	}
}
