namespace MonoDevelop.EditorBindings.FormattingStrategy {
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
		/// taken to indent the curent line
		/// </summary>
		Auto, 
		
		/// <summary>
		/// Inteligent, context sensitive indentation will occur
		/// </summary>
		Smart
	}
}
