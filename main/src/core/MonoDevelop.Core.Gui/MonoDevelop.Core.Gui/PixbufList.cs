using System.Collections;
using MonoDevelop.Core;
using Gdk;

namespace MonoDevelop.Core.Gui
{
	public class PixbufList : ArrayList
	{
		public PixbufList (params string [] resIcons) : base (resIcons.Length)
		{
			foreach (string s in resIcons)
				Add (ImageService.GetPixbuf (s));
		}
		
		public new Pixbuf this[int idx] {
			get {
				return (Pixbuf) base[idx];
			}
			set {
				base[idx] = value;
			}
		}

		public void Add (Pixbuf item) {
			base.Add (item);
		}
		
		public IList Images {
			get {
				return this; // Hack to allow to compile original code
			}
		}
	}
}
