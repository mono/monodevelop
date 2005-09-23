using System.Collections;

namespace System.Collections.Utility
{
	///<summary>
	/// Utility class to aid in sorting collections
	/// </summary>
	public class SortUtility: SortUtilityBase {
		///****************************************************************************************
		/// InsertionSort *************************************************************************
		///****************************************************************************************
		
		///<param name="list">List to sort</param>
		public static void InsertionSort(IList list) {
			InsertionSort(list, 0, list.Count-1, Comparer.Default);
		}
		
		///<param name="list">List to sort</param>
		///<param name="lbound">Lower bound, smallest index to include in sort</param>
		///<param name="ubound">Upper bound, larges index to include in sort</param>
		public static void InsertionSort(IList list, int lbound, int ubound) {
			InsertionSort(list, lbound, ubound, Comparer.Default);
		}
		
		///<param name="list">List to sort</param>
		///<param name="comparer">Object to compare collections elements and determine sort order</param>
		public static void InsertionSort(IList list, IComparer comparer) {
			InsertionSort(list, 0, list.Count-1, comparer);
		}
		
		///<param name="list">List to sort</param>
		///<param name="lbound">Lower bound, smallest index to include in sort</param>
		///<param name="ubound">Upper bound, larges index to include in sort</param>
		///<param name="comparer">Object to compare collections elements and determine sort order</param>
		public static void InsertionSort(IList list, int lbound, int ubound, IComparer comparer) {
			for(int i=lbound + 1; i <= ubound; i++) {
				object item = list[i];
				if(comparer.Compare(item, list[i-1]) < 0) {
					//i is not in sorted order
					list.RemoveAt(i);
					SortedInsert(item, list, lbound, i-1, comparer);
				}
			}
		}
		
		
		public static void SelectionSort(IList list, IComparer comparer)
		{
			for (int i = 0; i < list.Count - 1; ++i) {
				for (int j = i + 1; j < list.Count; ++j) {
					if (comparer.Compare(list[i], list[j]) > 0) {
						object tmp = list[i];
						list[i] = list[j];
						list[j] = tmp;
					}
				}
			}
//			QuickSort(list, 0, list.Count-1, comparer);
		}

		
		///****************************************************************************************
		/// QuickSort *****************************************************************************
		///****************************************************************************************
		
		///<param name="list">List to sort</param>
		public static void QuickSort(IList list) {
			QuickSort(list, 0, list.Count-1, Comparer.Default);
		}
		
		///<param name="list">List to sort</param>
		///<param name="lbound">Lower bound, smallest index to include in sort</param>
		///<param name="ubound">Upper bound, larges index to include in sort</param>
		public static void QuickSort(IList list, int lbound, int ubound) {
			QuickSort(list, lbound, ubound, Comparer.Default);
		}
		
		///<param name="list">List to sort</param>
		///<param name="comparer">Object to compare collections elements and determine sort order</param>
		public static void QuickSort(IList list, IComparer comparer) {
			QuickSort(list, 0, list.Count-1, comparer);
		}
		
		///<param name="list">List to sort</param>
		///<param name="lbound">Lower bound, smallest index to include in sort</param>
		///<param name="ubound">Upper bound, larges index to include in sort</param>
		///<param name="comparer">Object to compare collections elements and determine sort order</param>
		public static void QuickSort(IList list, int lbound, int ubound, IComparer comparer) {
			InsertionSort(list, lbound, ubound, comparer);
			#if false
			if(lbound < ubound) {
				//select pivot
				object pivot = list[(lbound + ubound) / 2];
				
				//begin pivoting
				int left  = lbound;
				int right = ubound;
				while(true) {
					//look for larger than pivot items on left side
					while((left < right) && (comparer.Compare(list[left], pivot) <= 0)) {
						left++;
					}
					
					//look for smaller than pivot items on right side
					while((left < right) && (comparer.Compare(list[right], pivot) >= 0)) {
						right--;
					}
					
					if(left < right) {
						//swap so that left only has smaller than pivot items
						//and right only has larger than pivot items
						object tmp = list[left];
						list[left] = list[right];
						list[right] = tmp;
					}
					else {
						left--;
						right++;
						//elements lbound to left are less than or equal to pivot
						//elements right to ubound are greater than or equal to pivot
						if(lbound < left) {
							QuickSort(list, lbound, left, comparer);
						}
						if(right < ubound) {
							QuickSort(list, right, ubound, comparer);
						}
					}
				}
			}
			#endif
		}
		
		///****************************************************************************************
		/// HeapSort ******************************************************************************
		///****************************************************************************************
		
		///****************************************************************************************
		/// RadixSort *****************************************************************************
		///****************************************************************************************
	}
}
