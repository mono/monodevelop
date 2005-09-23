using System.Collections;

namespace System.Collections.Utility {
   ///<summary>
   /// Utility class to aid in sorting collections
   /// Contains most common operations in sorting
   ///</summary>
   ///
   public class SortUtilityBase {
      ///****************************************************************************************
      /// SortedInsert **************************************************************************
      ///****************************************************************************************

      ///<summary>Inserts an item in a list in sorted order assuming the list is already sorted</summary>
      ///<param name="item">Item to insert</param>
      ///<param name="list">List to sort</param>
      ///<returns>index of inserted item</returns>
      public static int SortedInsert(object item, IList list) {
         return SortedInsert(item, list, 0, list.Count-1, Comparer.Default);
      }

      ///<summary>Inserts an item in a list in sorted order assuming the list is already sorted</summary>
      ///<param name="item">Item to insert</param>
      ///<param name="list">List to insert into</param>
      ///<param name="lbound">Lower bound, smallest index to include in insertion</param>
      ///<param name="ubound">Upper bound, larges index to include in insertion</param>
      ///<returns>index of inserted item</returns>
      public static int SortedInsert(object item, IList list, int lbound, int ubound) {
         return SortedInsert(item, list, lbound, ubound, Comparer.Default);
      }

      ///<summary>Inserts an item in a list in sorted order assuming the list is already sorted</summary>
      ///<param name="item">Item to insert</param>
      ///<param name="list">List to insert into</param>
      ///<param name="comparer">Object to compare collections elements and determine sort order</param>
      ///<returns>index of inserted item</returns>
      public static int SortedInsert(object item, IList list, IComparer comparer) {
         return SortedInsert(item, list, 0, list.Count-1, comparer);
      }

      ///<summary>Inserts an item in a list in sorted order assuming the list is already sorted</summary>
      ///<param name="item">Item to insert</param>
      ///<param name="list">List to insert into</param>
      ///<param name="lbound">Lower bound, smallest index to include in insertion</param>
      ///<param name="ubound">Upper bound, larges index to include in insertion</param>
      ///<param name="comparer">Object to compare collections elements and determine sort order</param>
      ///<returns>index of inserted item</returns>
      public static int SortedInsert(object item, IList list, int lbound, int ubound, IComparer comparer) {
         int index = BinarySearch(item, list, lbound, ubound, comparer);
         list.Insert((index < 0) ? ~index : index, item);
         return index;
      }

      ///****************************************************************************************
      /// BinarySearch **************************************************************************
      ///****************************************************************************************

      ///<summary>Finds index of item a list assuming the list is sorted</summary>
      ///<param name="item">Item to find</param>
      ///<param name="list">List to search</param>
      ///<returns>
      /// if found
      ///   index of found item
      /// else
      ///   a negative number that is bitwise complement (~) of index where the item should have been
      ///</returns>
      public static int BinarySearch(object item, IList list) {
         return BinarySearch(item, list, 0, list.Count-1, Comparer.Default);
      }

      ///<summary>Finds index of item a list assuming the list is sorted</summary>
      ///<param name="item">Item to find</param>
      ///<param name="list">List to search</param>
      ///<param name="lbound">Lower bound, smallest index to include in search</param>
      ///<param name="ubound">Upper bound, larges index to include in search</param>
      ///<returns>
      /// if found
      ///   index of found item
      /// else
      ///   a negative number that is bitwise complement (~) of index where the item should have been
      ///</returns>
      public static int BinarySearch(object item, IList list, int lbound, int ubound) {
         return BinarySearch(item, list, lbound, ubound, Comparer.Default);
      }

      ///<summary>Finds index of item a list assuming the list is sorted</summary>
      ///<param name="item">Item to find</param>
      ///<param name="list">List to search</param>
      ///<param name="comparer">Object to compare collections elements and determine sort order</param>
      ///<returns>
      /// if found
      ///   index of found item
      /// else
      ///   a negative number that is bitwise complement (~) of index where the item should have been
      ///</returns>
      public static int BinarySearch(object item, IList list, IComparer comparer) {
         return BinarySearch(item, list, 0, list.Count-1, comparer);
      }

      ///<summary>Finds index of item a list assuming the list is sorted</summary>
      ///<param name="item">Item to find</param>
      ///<param name="list">List to search</param>
      ///<param name="lbound">Lower bound, smallest index to include in search</param>
      ///<param name="ubound">Upper bound, larges index to include in search</param>
      ///<param name="comparer">Object to compare collections elements and determine sort order</param>
      ///<returns>
      /// if found
      ///   index of found item
      /// else
      ///   a negative number that is bitwise complement (~) of index where the item should have been
      ///</returns>
      public static int BinarySearch(object item, IList list, int lbound, int ubound, IComparer comparer) {
         int middle = 0;
         while(lbound <= ubound) {
            middle = (lbound + ubound) / 2;
            int cmp = comparer.Compare(item, list[middle]);
            if(cmp < 0) {
               ubound = middle-1;
            }
            else if(cmp > 0) {
               lbound = middle+1;
            }
            else {
               return middle;
            }
         }
         return ~lbound;
      }
   }
}
