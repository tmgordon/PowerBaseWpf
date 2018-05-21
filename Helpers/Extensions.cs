using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using PowerBaseWpf.Models;

namespace PowerBaseWpf.Helpers
{
    public static class Extensions
    {
        public static IEnumerable<string> Props = new List<string>()
        {
            "Identity",
            "ParentPath",
            "Name",
            "MoveTo",
            "FromParentPath",
            "FromName",
            "FromIdentity"
        };
        public static List<string> ValidTokens(this string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return new List<string>();
            }
            return Props.Where(t => Regex.Match(value, $@"({{[L,U]?{{)(\[\d+\])?({t})(\[\d+\])?(\..*)?(\[\d+\])?({{.}})?({{\d}})?(\[\d+\])?(}}}})").Success).ToList();
        }

        public static bool ContainsValidTokens(this string value)
        {
            return !string.IsNullOrWhiteSpace(value) && Props.Any(t => Regex.Match(value, $@"({{[L,U]?{{)(\[\d+\])?({t})(\[\d+\])?(\..*)?(\[\d+\])?({{.}})?({{\d}})?(\[\d+\])?(}}}})").Success);
        }
       
        /// Converts a DataTable to a list with generic objects
        /// </summary>
        /// <typeparam name="T">Generic object</typeparam>
        /// <param name="table">DataTable</param>
        /// <returns>List with generic objects</returns>
        public static IEnumerable<T> DataTableToList<T>(this DataTable table) where T : class, new()
        {
            IDictionary<Type, ICollection< PropertyInfo >> propertyDictionary = new Dictionary<Type, ICollection<PropertyInfo>>();
            try
            {
                var objType = typeof(T);
                ICollection<PropertyInfo> properties;

                lock (propertyDictionary)
                {
                    if (!propertyDictionary.TryGetValue(objType, out properties))
                    {
                        properties = objType.GetProperties().Where(property => property.CanWrite).ToList();
                        propertyDictionary.Add(objType, properties);
                    }
                }

                var list = new List<T>(table.Rows.Count);

                foreach (var row in table.AsEnumerable())
                {
                    var obj = new T();

                    foreach (var prop in properties)
                    {
                        try
                        {
                            List<string> mappings = new List<string> {prop.Name};
                            object[] attrs = prop.GetCustomAttributes(true);
                            foreach (object attr in attrs)
                            {
                                MappingAttribute mapping = attr as MappingAttribute;
                                if (mapping != null)
                                {
                                    mappings.AddRange(mapping.AliasList);
                                }
                            }
                            var propType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                            object safeValue = null;
                            foreach (var map in mappings)
                            {
                                try
                                {
                                    if (row[map] != null && safeValue == null)
                                    {
                                        try
                                        {
                                            safeValue = Convert.ChangeType(row[map], propType);
                                            break;
                                        }
                                        catch (Exception err)
                                        {
                                            safeValue = null;
                                        }
                                    }
                                }
                                catch (Exception err)
                                {
                                    // ignore
                                    safeValue = null;
                                }
                                
                            }
                            prop.SetValue(obj, safeValue, null);
                        }
                        catch
                        {
                            // ignored
                        }
                    }

                    list.Add(obj);
                }

                return list;
            }
            catch
            {
                return Enumerable.Empty<T>();
            }
        }


        public static List<T> ToList<T>(this DataTable table) where T : new()
        {
            IList<PropertyInfo> properties = typeof(T).GetProperties().ToList();
            List<T> result = new List<T>();

            foreach (var row in table.Rows)
            {
                var item = CreateItemFromRow<T>((DataRow) row, properties);
                result.Add(item);
            }
            
            return result;
        }

        private static T CreateItemFromRow<T>(DataRow row, IList<PropertyInfo> properties) where T : new()
        {
            T item = new T();
            foreach (var property in properties)
            {
                if (property.PropertyType == typeof(System.DayOfWeek))
                {   
                    DayOfWeek day = (DayOfWeek) Enum.Parse(typeof(DayOfWeek), row[property.Name].ToString());
                    property.SetValue(item, day, null);
                }
                else
                {
                    if (row[property.Name] == DBNull.Value)
                        property.SetValue(item, null, null);
                    else
                        property.SetValue(item, row[property.Name], null);
                }
            }
            return item;
        }

        

        public static void AddRange<T>(this ObservableCollection<T> observableCollection, IEnumerable<T> collection)
        {
            if (collection == null) return;
            foreach (var item in collection)
            {
                observableCollection.Add(item);
            }

            return;
        }

        public static int RemoveAll<T>(this ObservableCollection<T> observableCollection, Func<T, bool> condition)
        {
            // Find all elements satisfying the condition, i.e. that will be removed
            var toRemove = observableCollection
                .Where(condition)
                .ToList();

            // Remove the elements from the original collection, using the Count method to iterate through the list, 
            // incrementing the count whenever there's a successful removal
            return toRemove.Count(observableCollection.Remove);
        }

        public static ObservableCollection<T> ToObservableCollection<T>(this IEnumerable<T> enumerable)
        {
            var col = new ObservableCollection<T>();
            foreach (var cur in enumerable)
            {
                col.Add(cur);
            }
            return col;
        }
        public static void Sort<T>(this ObservableCollection<T> collection, Comparison<T> comparison = null)
        {
            var sortableList = new List<T>(collection);
            if (comparison == null)
                sortableList.Sort();
            else
                sortableList.Sort(comparison);

            for (var i = 0; i < sortableList.Count; i++)
            {
                var oldIndex = collection.IndexOf(sortableList[i]);
                var newIndex = i;
                if (oldIndex != newIndex)
                    collection.Move(oldIndex, newIndex);
            }
        }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class NoReplacementAttribute : System.Attribute
    {
    }

}
