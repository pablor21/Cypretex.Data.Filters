using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace Cypretex.Data.Filters.Parsers.Linq
{
    public class LinqIncludeParser
    {

        private static readonly Assembly entityFrameworkAssembly = AppDomain.CurrentDomain.GetAssemblies().SingleOrDefault(assembly => assembly.GetName().Name == "Microsoft.EntityFrameworkCore");
        private static readonly Type entityFrameworkQueryableExtensions = entityFrameworkAssembly != null ? entityFrameworkAssembly.GetTypes().FirstOrDefault(type => type.Name == "EntityFrameworkQueryableExtensions") : null;

        private static readonly MethodInfo includeMethod = entityFrameworkQueryableExtensions != null ? entityFrameworkQueryableExtensions.GetMethods().FirstOrDefault(method => method.Name == "Include"
                        && method.IsGenericMethodDefinition
                        && method.GetGenericArguments().Length == 1
                        && method.GetParameters().Length == 2
                        && method.GetParameters().Last().ParameterType == typeof(string)
                        ) : null;
        private static readonly MethodInfo thenIncludeMethod = entityFrameworkQueryableExtensions != null ? entityFrameworkQueryableExtensions.GetMethods().FirstOrDefault(method => method.Name == "ThenInclude"
       && method.IsGenericMethodDefinition
       && method.GetParameters().Length == 2 && method.GetParameters()[1].ParameterType == typeof(Expression)) : null;


        public static IQueryable<T> Parse<T>(IList<IncludeFilter> includes, IQueryable<T> source, string paramName = null)
        {
            if (includeMethod != null && thenIncludeMethod != null)
            {
                return new LinqIncludeParser().Process<T>(includes, source, paramName);
            }
            return source;

        }

        //Expression visitor instance
        private readonly Visitor visitor;

        public LinqIncludeParser()
        {
            this.visitor = new Visitor();
        }

        private IQueryable<T> Process<T>(IList<IncludeFilter> includes, IQueryable<T> source, string prefix)
        {
            foreach (IncludeFilter include in includes)
            {
                source = this.Process<T>(include, source, prefix);
            }
            return source;
        }

        private IQueryable<T> Process<T>(IncludeFilter include, IQueryable<T> source, string prefix = "")
        {

            MethodInfo m = includeMethod.MakeGenericMethod(typeof(T));
            source = (IQueryable<T>)m.Invoke(source, new object[] { source, prefix + include.Field });
            prefix += include.Field + ".";
            source = Process<T>(include.With, source, prefix);

            return source;
        }

    }
}