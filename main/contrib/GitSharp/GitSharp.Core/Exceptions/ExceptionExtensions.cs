using System;
using System.Text;

namespace GitSharp.Core.Exceptions
{
    public static class ExceptionExtensions
    {
        public static void printStackTrace(this Exception self)
        {
            Console.Error.WriteLine(self.FormatPretty());
        }

        public static string FormatPretty(this Exception exception)
        {
            if (exception == null) return string.Empty;
            var sb = new StringBuilder();
            PrintRecursive(exception, sb, string.Empty);
            return sb.ToString();
        }

        private static void PrintRecursive(Exception exception, StringBuilder sb, string indent)
        {
            var stars = new string('*', 80);
            sb.AppendLine(indent + stars);
			sb.AppendFormat(indent + "{0}: \"{1}\"\n", exception.GetType().Name, exception.Message);
			sb.AppendLine(indent + new string('-', 80));

            if (exception.InnerException != null)
            {
				sb.AppendLine(indent + "InnerException:");
				PrintRecursive(exception.InnerException, sb, indent + "   ");
            }

            foreach (string line in exception.StackTrace.Split(new[] { " at " }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (string.IsNullOrEmpty(line.Trim())) continue;

            	string[] parts = line.Trim().Split(new[] { " in " }, StringSplitOptions.RemoveEmptyEntries);
                string classInfo = parts[0];

                if (parts.Length == 2)
                {
                    parts = parts[1].Trim().Split(new[] { "line" }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 2)
                    {
                        string srcFile = parts[0];
                        int lineNr = int.Parse(parts[1]);
						sb.AppendFormat(indent + "  {0}({1},1):   {2}\n", srcFile.TrimEnd(':'), lineNr, classInfo);
                    }
                    else
                    {
						sb.AppendLine(indent + "  " + classInfo);
                    }
                }
                else
                {
                	sb.AppendLine(indent + "  " + classInfo);
                }
            }

			sb.AppendLine(indent + stars);
        }
    }
}