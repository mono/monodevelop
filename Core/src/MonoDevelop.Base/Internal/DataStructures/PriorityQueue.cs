// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>
using System.Collections;

public class PriorityQueue // min key on top
{
	ArrayList elements;
	
	public PriorityQueue()
	{
		elements = new ArrayList();
	}
	public PriorityQueue(int capacity)
	{
		elements = new ArrayList(capacity);
	}
	
	public IEnumerator GetEnumerator()
	{
		return elements.GetEnumerator();
	}
	
	void UpHeap(int k)
	{
		Pair v = (Pair)elements[k];
		while(k > 0 && (Pair)elements[(k-1) / 2] >= v){
			elements[k] = elements[(k-1) / 2];
			k = (k-1) / 2;
		}
		elements[k] = v;
	}
	
	void DownHeap(int k)
	{
		Pair v = (Pair)elements[k];
		int j;
		while(2 * k + 1 < elements.Count){
			j = 2 * k + 1;
			if(j < elements.Count - 1 &&  (Pair)elements[j] > (Pair)elements[j + 1])
				++j;
			if(v <= (Pair)elements[j])
				break;
			elements[k] = elements[j];
			k = j;
		}
		elements[k] = v;
	}
	
	public int Count {
		get {
			return elements.Count;
		}
	}
	
	public void Insert(System.IComparable key, object val)
	{
		Pair p = new Pair(key, val);
		elements.Add(p);
		UpHeap(elements.Count - 1);
	}
	
	public object Remove()
	{
		object ret = ((Pair)elements[0]).Val;
		elements[0] = elements[elements.Count - 1];
		elements.RemoveAt(elements.Count - 1);
		if(elements.Count > 0)
			DownHeap(0);
		return ret;
	}
	
	public class Pair
	{
		public System.IComparable Key;
		public object Val;
		public Pair(System.IComparable key, object val)
		{
			Key = key;
			Val = val;
		}
		public static bool operator < (Pair a, Pair b)
		{
			return (a.Key.CompareTo(b.Key) < 0);
		}
		public static bool operator > (Pair a, Pair b)
		{
			return (a.Key.CompareTo(b.Key) > 0);
		}
		public static bool operator <= (Pair a, Pair b)
		{
			return (a.Key.CompareTo(b.Key) <= 0);
		}
		public static bool operator >= (Pair a, Pair b)
		{
			return (a.Key.CompareTo(b.Key) >= 0);
		}
	}	
}
