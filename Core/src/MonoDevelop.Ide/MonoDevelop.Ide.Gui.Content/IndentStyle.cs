namespace MonoDevelop.Ide.Gui.Content {
	/// <summary>
	/// Describes the indent style
	/// </summary>
	public enum IndentStyle {
		/// <summary>
		/// No indentation occurs
		/// </summary>
		None,
		
		/// <summary>
		/// The indentation from the line above will be
		/// taken to indent the current line
		/// </summary>
		Auto, 
		
		/// <summary>
		/// Intelligent, context sensitive indentation will occur
		/// </summary>
		Smart
	}
}
