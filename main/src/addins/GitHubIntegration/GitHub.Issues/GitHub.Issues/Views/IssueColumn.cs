using System;

namespace GitHub.Issues
{
	public class IssueColumn
	{
		public Type Type { get; set; }

		public String Title { get; set; }

		public String PropertyName { get; set; }

		public int OrderFromLeftIndex { get; set; }

		public IssueColumn (Type type, String title, String propertyName, int orderFromLeftIndex)
		{
			this.Type = type;
			this.Title = title;
			this.PropertyName = propertyName;
			this.OrderFromLeftIndex = orderFromLeftIndex;
		}
	}
}