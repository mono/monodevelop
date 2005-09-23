using System;
using System.Collections;

class T
{
	static IEnumerator GetIt ()
	{
		yield return 1;
		yield break;
	}
	
	static void Main ()
	{
		GetIt ();
	}
}

