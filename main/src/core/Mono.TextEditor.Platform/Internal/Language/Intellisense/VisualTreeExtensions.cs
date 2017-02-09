using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    public static class VisualTreeExtensions
    {
        /// <summary>
        /// This uses the visual tree first, then diverts to the logical tree if the visual tree ends.
        /// This is necessary for the TabControl, where using either the logical tree or visual tree
        /// by themselves is insufficient.
        /// </summary>
        /// <param name="sourceElement">The object to begin searching from.</param>
        /// <returns>The visual tree parent of the object, or the logical tree parent if
        /// the visual tree parent is null, or null if the logical tree parent is null.</returns>
        public static DependencyObject GetVisualOrLogicalParent(DependencyObject sourceElement)
        {
            if (sourceElement == null)
            {
                return null;
            }

            // Some elements in the WPF tree (like ContentElement) derive only from DependencyObject,
            // and are only a part of the logical tree.  Only use the VisualTreeHelper is the element
            // is a Visual.
            if (sourceElement is Visual)
            {
                return VisualTreeHelper.GetParent(sourceElement) ?? LogicalTreeHelper.GetParent(sourceElement);
            }
            else
            {
                return LogicalTreeHelper.GetParent(sourceElement);
            }
        }

        /// <summary>
        /// Helper method that finds the first ancestor of a given Type in the
        /// logical or visual tree, or the object itself if it matches in type.
        /// </summary>
        /// <typeparam name="TAncestorType">The type of ancestor to find.</typeparam>
        /// <param name="obj">The object at which to begin searching.</param>
        /// <returns>The object itself, if it matches in type, else the first ancestor of type T in the parent chain of obj,
        /// or null if no ancestor is found.</returns>
        /// <remarks>
        /// The type of <paramref name="obj"/> is Visual rather than DependencyObject in order to disambiguate this method 
        /// from Microsoft.VisualStudio.PlatformUI.Shell.ExtensionMethods.FindAncestor(ViewElement element).  If you need
        /// to find an ancestor of a non-Visual DependencyObject you should call 
        /// FindAncestor&lt;TAncestorType, DependencyObject&gt;(obj, GetVisualOrLogicalParent) directly.
        /// </remarks>
        public static TAncestorType FindAncestorOrSelf<TAncestorType>(Visual obj) where TAncestorType : DependencyObject
        {
            return FindAncestorOrSelf<TAncestorType, DependencyObject>(obj, GetVisualOrLogicalParent);
        }

        /// <summary>
        /// Helper method that finds the first ancestor of a given Type in the
        /// logical or visual tree, or the object itself if it matches in type.
        /// </summary>
        /// <typeparam name="TAncestorType">The type of ancestor to find.</typeparam>
        /// <typeparam name="TElementType">The base type of intermediate elements in the ancestor tree.</typeparam>
        /// <param name="obj">The object at which to begin searching.</param>
        /// <returns>The object itself, if it matches in type, else the first ancestor of type T in the parent chain of obj,
        /// or null if no ancestor is found.</returns>
        /// <remarks>
        /// The type of <paramref name="obj"/> is Visual rather than DependencyObject in order to disambiguate this method 
        /// from Microsoft.VisualStudio.PlatformUI.Shell.ExtensionMethods.FindAncestor(ViewElement element).  If you need
        /// to find an ancestor of a non-Visual DependencyObject you should call 
        /// FindAncestor&lt;TAncestorType, DependencyObject&gt;(obj, GetVisualOrLogicalParent) directly.
        /// </remarks>
        public static TAncestorType FindAncestorOrSelf<TAncestorType, TElementType>(TElementType obj, Func<TElementType, TElementType> parentEvaluator) where TAncestorType : DependencyObject
        {
            return obj as TAncestorType ?? FindAncestor<TAncestorType, TElementType>(obj, parentEvaluator);
        }

        /// <summary>
        /// Helper method that finds the first ancestor in the logical or visual tree, or the object itself, that is accepted by the ancestor selector function.
        /// </summary>
        /// <param name="obj">The object at which to begin searching.</param>
        /// <param name="parentEvaluator">The method used to determine the parent of an element.</param>
        /// <param name="ancestorSelector">The method used to select an ancestor of interest.</param>
        /// <typeparam name="TElementType">The base type of intermediate elements in the ancestor tree.</typeparam>
        /// <returns>The first ancestor in the parent chain of obj accepted by the the ancestor selector function, or null if no ancestor is found.</returns>
        public static object FindAncestorOrSelf<TElementType>(TElementType obj, Func<TElementType, TElementType> parentEvaluator, Func<TElementType, bool> ancestorSelector)
        {
            if (ancestorSelector(obj))
                return obj;

            return FindAncestor<TElementType>(obj, parentEvaluator, ancestorSelector);
        }

        /// <summary>
        /// Helper method that finds the first ancestor of a given Type in the
        /// logical or visual tree.
        /// </summary>
        /// <typeparam name="TAncestorType">The type of ancestor to find.</typeparam>
        /// <param name="obj">The object at which to begin searching.</param>
        /// <returns>The first ancestor of type T in the parent chain of obj, or null
        /// if no ancestor is found.</returns>
        /// <remarks>
        /// The type of <paramref name="obj"/> is Visual rather than DependencyObject in order to disambiguate this method 
        /// from Microsoft.VisualStudio.PlatformUI.Shell.ExtensionMethods.FindAncestor(ViewElement element).  If you need
        /// to find an ancestor of a non-Visual DependencyObject you should call 
        /// FindAncestor&lt;TAncestorType, DependencyObject&gt;(obj, GetVisualOrLogicalParent) directly.
        /// </remarks>
        public static TAncestorType FindAncestor<TAncestorType>(Visual obj) where TAncestorType : DependencyObject
        {
            return FindAncestor<TAncestorType, DependencyObject>(obj, GetVisualOrLogicalParent);
        }

        /// <summary>
        /// Helper method that finds the first ancestor of a given Type in the
        /// logical or visual tree.
        /// </summary>
        /// <typeparam name="TAncestorType">The type of ancestor to find.</typeparam>
        /// <typeparam name="TElementType">The base type of intermediate elements in the ancestor tree.</typeparam>
        /// <param name="obj">The object at which to begin searching.</param>
        /// <param name="parentEvaluator">The method used to determine the parent of an element.</param>
        /// <returns>The first ancestor of type T in the parent chain of obj, or null
        /// if no ancestor is found.</returns>
        public static TAncestorType FindAncestor<TAncestorType, TElementType>(TElementType obj, Func<TElementType, TElementType> parentEvaluator) where TAncestorType : class
        {
            return FindAncestor<TElementType>(obj, parentEvaluator, ancestor => ancestor is TAncestorType) as TAncestorType;
        }

        /// <summary>
        /// Helper method that finds the first ancestor in the logical or visual tree that is accepted by the ancestor selector function.
        /// </summary>
        /// <param name="obj">The object at which to begin searching.</param>
        /// <param name="parentEvaluator">The method used to determine the parent of an element.</param>
        /// <param name="ancestorSelector">The method used to select an ancestor of interest.</param>
        /// <typeparam name="TElementType">The base type of intermediate elements in the ancestor tree.</typeparam>
        /// <returns>The first ancestor in the parent chain of obj accepted by the the ancestor selector function.</returns>
        public static object FindAncestor<TElementType>(TElementType obj, Func<TElementType, TElementType> parentEvaluator, Func<TElementType, bool> ancestorSelector)
        {
            TElementType parent = parentEvaluator(obj);
            while (parent != null)
            {
                if (ancestorSelector(parent))
                {
                    return parent;
                }

                parent = parentEvaluator(parent);
            }

            return null;
        }
    }
}
