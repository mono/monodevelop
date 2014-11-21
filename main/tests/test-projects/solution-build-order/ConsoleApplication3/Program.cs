using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApplication3
{
	class Program
	{
		static void Main(string[] args)
		{
			var e = GetNumbers();
		}

		static IEnumerable<int> GetNumbers()
		{
			int n = 0;
			while (n<1000)
				yield return n++;
		}
	}
}
