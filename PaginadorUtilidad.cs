using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Utilities
{

    public static class PaginadorUtilidad
    {
        private const int paginaDefecto = 1;
        private const int registrosPorPaginaDefecto = 10;

        private static int getIntParam(NameValueCollection queryParams, string name, int def)
        {
            string p = queryParams.Get(name);
            if (p != null)
            {
                int result;
                bool success = int.TryParse(p, out result);
                if (success) return result;
            }
            return def;
        }

        public static T GetParam<T>(this NameValueCollection queryParams, string value, T def)
        {
            string param = queryParams.Get(value);
            if (param != null)
            {
                try
                {
                    return (T)Convert.ChangeType(param, typeof(T));
                }
                catch { }
            }
            return def;
        }

        public static async Task<ActionResult<Paginador<T>>> Paginar<T>(
            string query,
            DbSet<T> dbSet,
            Func<IQueryable<T>, NameValueCollection, IQueryable<T>> before = null,
            Func<IQueryable<T>, NameValueCollection, IQueryable<T>> middle = null,
            Func<IQueryable<T>, NameValueCollection, IQueryable<T>> after = null
        ) where T : class
        {
            NameValueCollection queryParams = HttpUtility.ParseQueryString(query);

            int pagina = getIntParam(queryParams, "pagina", paginaDefecto);
            int registrosPorPagina = getIntParam(queryParams, "registrosPorPagina", registrosPorPaginaDefecto);

            int totalRegistros;
            int totalPaginas;

            List<T> listado;
            IQueryable<T> preAfter;
            IQueryable<T> afterQuery;

            try
            {
                var beforeQuery = (before != null) ? before(dbSet, queryParams) : dbSet;

                var preMiddle = beforeQuery.OrderBy("Id");
                var middleQuery = (middle != null) ? middle(preMiddle, queryParams) : preMiddle;

                totalRegistros = await middleQuery.CountAsync();

                if (registrosPorPagina > 0)
                {
                    preAfter = middleQuery.Skip((pagina - 1) * registrosPorPagina).Take(registrosPorPagina);
                    afterQuery = (after != null) ? after(preAfter, queryParams) : preAfter;

                    totalPaginas = (int)Math.Ceiling((double)totalRegistros / registrosPorPagina);
                }
                else
                {
                    preAfter = middleQuery;
                    afterQuery = (after != null) ? after(preAfter, queryParams) : preAfter;

                    totalPaginas = 1;
                    registrosPorPagina = totalRegistros;
                }

                listado = await afterQuery.ToListAsync();

                Paginador<T> resultado = new Paginador<T>()
                {
                    RegistrosPorPagina = registrosPorPagina,
                    TotalRegistros = totalRegistros,
                    TotalPaginas = totalPaginas,
                    PaginaActual = pagina,
                    Listado = listado
                };

                return new OkObjectResult(resultado);
            }
            catch (Exception e)
            {
                ObjectResult respuesta = new ObjectResult(e.Message);
                respuesta.StatusCode = StatusCodes.Status500InternalServerError;
                return respuesta;
            }
        }

    }

    public static class PaginadorExtension

    {
        public static IQueryable<T> OrderBy<T>(this IQueryable<T> source, string propertyName)
        {
            return (IQueryable<T>)OrderBy((IQueryable)source, propertyName);
        }

        public static IQueryable OrderBy(this IQueryable source, string propertyName)
        {
            var x = Expression.Parameter(source.ElementType, "x");
            var selector = Expression.Lambda(Expression.PropertyOrField(x, propertyName), x);

            return source.Provider.CreateQuery(
                Expression.Call(
                    typeof(Queryable),
                    "OrderBy",
                    new Type[] { source.ElementType, selector.Body.Type },
                    source.Expression, selector
                )
            );
        }
    }
}
