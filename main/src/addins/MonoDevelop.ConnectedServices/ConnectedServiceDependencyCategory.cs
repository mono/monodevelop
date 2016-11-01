using System;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using Xwt.Drawing;

namespace MonoDevelop.ConnectedServices
{
	/// <summary>
	/// Category used to group connected service dependencies.
	/// </summary>
	public class ConnectedServiceDependencyCategory
	{
		Image icon;
		IconId iconId;

		/// <summary>
		/// Gets the name of the category
		/// </summary>
		public string Name { get; private set; }

		/// <summary>
		/// Gets the Icon of the category.
		/// </summary>
		public Image Icon {
			get {
				if (icon == null && !iconId.IsNull)
					icon = ImageService.GetIcon (iconId).WithSize (Xwt.IconSize.Small);
				return icon;
			}
			private set {
				icon = value;
			}
		}

		public ConnectedServiceDependencyCategory (string name, Image icon)
		{
			if (string.IsNullOrEmpty (name))
				throw new ArgumentNullException (nameof (name));
			if (icon == null)
				throw new ArgumentNullException (nameof (icon));
			Name = name;
			Icon = icon;
		}

		public ConnectedServiceDependencyCategory (string name, IconId icon)
		{
			if (string.IsNullOrEmpty (name))
				throw new ArgumentNullException (nameof (name));
			if (icon.IsNull)
				throw new ArgumentNullException (nameof (icon));
			Name = name;
			iconId = icon;
		}
	}
}
