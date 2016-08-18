using System;
using Xwt.Drawing;

namespace MonoDevelop.ConnectedServices
{
	/// <summary>
	/// Category used to group connected service dependencies.
	/// </summary>
	public class ConnectedServiceDependencyCategory
	{
		/// <summary>
		/// Gets the name of the category
		/// </summary>
		public string Name { get; private set; }

		/// <summary>
		/// Gets the Icon of the category.
		/// </summary>
		public Image Icon { get; private set; }

		public ConnectedServiceDependencyCategory (string name, Image icon)
		{
			if (string.IsNullOrEmpty (name))
				throw new ArgumentNullException (nameof (name));
			if (icon == null)
				throw new ArgumentNullException (nameof (icon));
			Name = name;
			Icon = icon;
		}
	}
}
