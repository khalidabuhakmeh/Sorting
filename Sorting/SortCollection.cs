#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Sorting
{
    public static class OrderByExtensions
    {
        public static IQueryable<TSource> OrderBy<TSource>(this IQueryable<TSource> queryable, string sorts)
        {
            var sort = new SortCollection<TSource>(sorts);
            return sort.Apply(queryable);
        }

        public static IQueryable<TSource> OrderBy<TSource>(this IQueryable<TSource> queryable, params string[] sorts)
        {
            var sort = new SortCollection<TSource>(sorts);
            return sort.Apply(queryable);
        }
    }
    
    public class SortCollection<TSource>
    {
        private List<ISortProperty<TSource>> Properties { get; set; }  
            = new List<ISortProperty<TSource>>();

        public IReadOnlyList<ISort> Sorts => Properties.AsReadOnly();
        
        public SortCollection()
        {}
        
        public SortCollection(string value)
            :this(value?.Split(new[] {","}, StringSplitOptions.RemoveEmptyEntries))
        {
        }

        public SortCollection(IEnumerable<string> elements)
        {
            if (elements == null)
                return;
            
            foreach (var element in elements)
            {
                var sortElement = Parse(element);
                
                // garbage property name, skip it
                if (sortElement is null) continue;
                
                Properties.Add(sortElement);
            }
        }
        
        private static ISortProperty<TSource>? Parse(string element)
        {
            var name = element.Trim();
            var properties = typeof(TSource).GetProperties().ToList();
            var direction = ListSortDirection.Ascending;

            if (element.StartsWith("-"))
            {
                direction = ListSortDirection.Descending;
                name = name.Substring(1);
            }
            
            // property name cased properly
            var propertyInfo = properties.Find(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

            if (propertyInfo == null)
                return null;

            var type = typeof(SortProperty<>);
            var sortedElementType = type.MakeGenericType(typeof(TSource), propertyInfo.PropertyType);
            var ctor = sortedElementType.GetConstructor(new[] {typeof(PropertyInfo), typeof(ListSortDirection)});

            return ctor.Invoke(new object?[] {propertyInfo, direction}) as ISortProperty<TSource>;
        }

        public IQueryable<TSource> Apply(IQueryable<TSource> queryable)
        {
            var query = queryable;
            
            foreach (var element in Properties)
            {
               query = element.Apply(query);
            }
            
            return query;
        }
        
        public override string ToString()
        {
            return string.Join(",", Properties);
        }

        /// <summary>
        /// Creates a string with the Property Sort or Flips the direction if it exists
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        public string AddOrUpdate(string element)
        {
            var parse = Parse(element);

            if (parse == null)
                return ToString();
            
            var sort = new SortCollection<TSource>(ToString());
            var property = sort.Properties.Find(x => x.PropertyName == parse.PropertyName);

            if (property == null)
            {
                sort.Properties.Add(parse);
                return sort.ToString();
            }

            property.Direction =
                property.Direction == ListSortDirection.Ascending
                    ? ListSortDirection.Descending
                    : ListSortDirection.Ascending;

            return sort.ToString();
        }

        public string Remove(string element)
        {
            var parse = Parse(element);

            if (parse == null)
                return ToString();
            
            var sort = new SortCollection<TSource>(ToString());
            var property = sort.Properties.Find(x => x.PropertyName == parse.PropertyName);

            if (property != null)
            {
                sort.Properties.Remove(property);
            }

            return sort.ToString();
        }

        private class SortProperty<TKey> : ISortProperty<TSource>
        {
            public SortProperty(PropertyInfo propertyInfo, ListSortDirection direction)
            {
                PropertyName = propertyInfo.Name;
                Direction = direction;

                var source = Expression.Parameter(typeof(TSource), "x");
                var member = Expression.Property(source, propertyInfo);
                Filter = Expression.Lambda<Func<TSource, TKey>>(member, source);
            }

            public string PropertyName { get; private set; }
            public ListSortDirection Direction { get; set; }
            public Expression<Func<TSource, TKey>> Filter { get; private set; }

            public IQueryable<TSource> Apply(IQueryable<TSource> queryable)
            {
                var visitor = new OrderingMethodFinder();
                visitor.Visit(queryable.Expression);
                
                if (visitor.OrderingMethodFound)
                {
                    queryable = Direction == ListSortDirection.Ascending 
                        ? ((IOrderedQueryable<TSource>)queryable).ThenBy(Filter) 
                        : ((IOrderedQueryable<TSource>)queryable).ThenByDescending(Filter);
                }
                else
                {
                    queryable = Direction == ListSortDirection.Ascending 
                        ? queryable.OrderBy(Filter) 
                        : queryable.OrderByDescending(Filter);
                }
                return queryable;
            }

            public override string ToString()
            {
                return Direction == ListSortDirection.Ascending
                    ? PropertyName
                    : $"-{PropertyName}";
            }
        
            private class OrderingMethodFinder : ExpressionVisitor
            {
                public bool OrderingMethodFound { get; set; }

                protected override Expression VisitMethodCall(MethodCallExpression node)
                {
                    var name = node.Method.Name;

                    if (node.Method.DeclaringType == typeof(Queryable) && (
                        name.StartsWith("OrderBy", StringComparison.Ordinal) ||
                        name.StartsWith("ThenBy", StringComparison.Ordinal)))
                    {
                        OrderingMethodFound = true;
                    }
                    return base.VisitMethodCall(node);
                }
            }
        }

        private interface ISortProperty<T> : ISort
        {
            new ListSortDirection Direction { get; set; }
            IQueryable<T> Apply(IQueryable<T> queryable);
        }

        public interface ISort
        {
            string PropertyName { get; }
            ListSortDirection Direction { get; }
        }
    }
}