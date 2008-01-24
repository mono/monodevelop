namespace Metacity {

	using System;
	using System.Runtime.InteropServices;

	internal class Theme : GLib.Opaque {

		public Theme (IntPtr raw) : base (raw) {}

		[DllImport("libmetacity-private.so.0")]
		static extern IntPtr meta_theme_get_current ();

		public static Theme Current {
			get {
				IntPtr raw = meta_theme_get_current ();
				return (Theme)GetOpaque (raw, typeof (Metacity.Theme), true);
			}
		}

		[DllImport("libmetacity-private.so.0")]
		static extern IntPtr meta_theme_load (string theme_name, IntPtr err);

		public static Theme Load (string name)
		{
			IntPtr raw = meta_theme_load (name, IntPtr.Zero);
			if (raw == IntPtr.Zero)
				return null;
			else
				return (Theme)GetOpaque (raw, typeof (Metacity.Theme), true);
		}

		[DllImport("libmetacity-private.so.0")]
		static extern void meta_theme_free (IntPtr raw);

		protected override void Free (IntPtr raw)
		{
			meta_theme_free (Raw);
		}
	}
}
