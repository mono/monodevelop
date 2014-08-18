using System;

namespace GitHub.Issues
{
	/// <summary>
	/// Data structure for representing a column which we can view in the Issues View
	/// </summary>
	public class IssueColumn
	{
		/// <summary>
		/// Data type of the column - type which will be used to display the data
		/// So if it's a class but you are using ToString to display it then type is String
		/// </summary>
		/// <value>The type.</value>
		public Type Type { get; set; }

		/// <summary>
		/// Title of the column - what will be displayed in the header of the table
		/// </summary>
		/// <value>The title.</value>
		public String Title { get; set; }

		/// <summary>
		/// Property name (can be a chained property) - uses reflection to retrieve the value
		/// which is then cast by the Type (above)
		/// </summary>
		/// <value>The name of the property.</value>
		public String PropertyName { get; set; }

		/// <summary>
		/// Column which it represents in the list store (index)
		/// </summary>
		/// <value>The index of the list store column.</value>
		public int ListStoreColumnIndex { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="GitHub.Issues.IssueColumn"/> class.
		/// </summary>
		/// <param name="type">Type.</param>
		/// <param name="title">Title.</param>
		/// <param name="propertyName">Property name.</param>
		/// <param name="listStoreColumnIndex">List store column index.</param>
		public IssueColumn (Type type, String title, String propertyName, int listStoreColumnIndex)
		{
			this.Type = type;
			this.Title = title;
			this.PropertyName = propertyName;
			this.ListStoreColumnIndex = listStoreColumnIndex;
		}
	}
}