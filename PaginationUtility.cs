using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;
using Algolia.Search.Clients;
using Algolia.Search.Models.Search;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

// ReSharper disable once CheckNamespace
namespace Lizelaser0310.Utilities
{
    public static class PaginationUtility
    {
        private const int DefaultPage = 1;
        private const int DefaultItemsPerPage = 5;

        private static int GetIntParam(NameValueCollection queryParams, string name, int def)
        {
            string p = queryParams.Get(name);
            if (p != null)
            {
                bool success = int.TryParse(p, out int result);
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
                catch
                {
                    return def;
                }
            }
            return def;
        }

        public static async Task<ActionResult<Paginator<T>>> AlgoliaPaginate<T>(
            string query,
            string indexUid,
            SearchClient algolia,
            int totalItems
        ) where T : class
        {
            NameValueCollection queryParams = HttpUtility.ParseQueryString(query);

            int page = GetIntParam(queryParams, "page", DefaultPage);
            int itemsPerPage = GetIntParam(queryParams, "itemsPerPage", DefaultItemsPerPage);
            string search = GetParam(queryParams, "search", "");

            SearchIndex index = algolia.InitIndex(indexUid);
            var result = await index.SearchAsync<T>(new Query(search)
            {
                HitsPerPage = itemsPerPage,
                Page = page - 1,
            });

            var paginator = new Paginator<T>()
            {
                CurrentPage = result.Page + 1,
                ItemsPerPage = result.HitsPerPage,
                Items = result.Hits,
                TotalItems = result.NbHits,
                TotalPages = result.NbPages
            };

            var obj = new ObjectResult(paginator);
            obj.StatusCode = StatusCodes.Status200OK;
            return obj;
        }

        public static async Task<ActionResult<Paginator<T>>> MeiliPaginate<T>(
            string query,
            string indexUid,
            string masterKey,
            int totalItems,
            string meiliUrl = "http://localhost:7700"
        ) where T : class
        {
            NameValueCollection queryParams = HttpUtility.ParseQueryString(query);

            int page = GetIntParam(queryParams, "page", DefaultPage);
            int itemsPerPage = GetIntParam(queryParams, "itemsPerPage", DefaultItemsPerPage);
            string search = GetParam(queryParams, "search", "");

            var client = new HttpClient();
            client.BaseAddress = new Uri(meiliUrl);
            client.DefaultRequestHeaders.Add("X-Meili-API-Key", masterKey);

            var body = new MeiliBody()
            {
                Q = search,
                Limit = itemsPerPage,
                Offset = (page - 1) * itemsPerPage
            };
            var jsonOptions = new JsonSerializerOptions();
            jsonOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            jsonOptions.PropertyNameCaseInsensitive = true;
            var json = JsonSerializer.Serialize(body, jsonOptions);

            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await client.PostAsync($"/indexes/{indexUid}/search", content);

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadAsByteArrayAsync();
                Console.WriteLine(data);
                var meili = JsonSerializer.Deserialize<MeiliResponse<T>>(data, jsonOptions);

                if (meili != null)
                {
                    var result = new Paginator<T>()
                    {
                        CurrentPage = page,
                        ItemsPerPage = itemsPerPage,
                        Items = meili.Hits,
                        TotalItems = meili.NbHits,
                        TotalPages = (int)Math.Ceiling((double)totalItems / itemsPerPage)
                    };
                    var obj = new ObjectResult(result);
                    obj.StatusCode = StatusCodes.Status200OK;
                    return obj;
                }
            }

            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }

        public static async Task<ActionResult<Paginator<T>>> Paginate<T>(
            string query,
            DbSet<T> dbSet,
            Func<IQueryable<T>, string, IQueryable<T>> searchProps = null,
            Func<IQueryable<T>, NameValueCollection, IQueryable<T>> before = null,
            Func<IQueryable<T>, NameValueCollection, IQueryable<T>> middle = null,
            Func<IQueryable<T>, NameValueCollection, IQueryable<T>> after = null
        ) where T : class
        {
            NameValueCollection queryParams = HttpUtility.ParseQueryString(query);

            int page = GetIntParam(queryParams, "page", DefaultPage);
            int itemsPerPage = GetIntParam(queryParams, "itemsPerPage", DefaultItemsPerPage);
            string search = GetParam(queryParams, "search", "");

            var beforeQuery = (before != null) ? before(dbSet, queryParams) : dbSet;

            var preMiddle = beforeQuery.OrderBy("Id");
            var middleQuery = (middle != null) ? middle(preMiddle, queryParams) : preMiddle;

            if (searchProps != null && !string.IsNullOrEmpty(search))
            {
                middleQuery = searchProps(middleQuery, search);
            }

            int totalItems = await middleQuery.CountAsync();

            int totalPages;
            List<T> items;

            if (itemsPerPage > 0)
            {
                IQueryable<T> preAfter = middleQuery.Skip((page - 1) * itemsPerPage).Take(itemsPerPage);
                items = await ((after != null) ? after(preAfter, queryParams) : preAfter).ToListAsync();

                totalPages = (int)Math.Ceiling((double)totalItems / itemsPerPage);
            }
            else
            {
                items = await ((after != null) ? after(middleQuery, queryParams) : middleQuery).ToListAsync();

                totalPages = 1;
                itemsPerPage = totalItems;
            }

            Paginator<T> result = new Paginator<T>()
            {
                ItemsPerPage = itemsPerPage,
                TotalItems = totalItems,
                TotalPages = totalPages,
                CurrentPage = page,
                Items = items
            };

            return new OkObjectResult(result);
        }
        // ReSharper disable once InconsistentNaming
        public static async Task<ActionResult<Paginator<R>>> Paginate<T, R>(
            string query,
            DbSet<T> dbSet,
            Func<IQueryable<T>, IQueryable<R>> mutation,
            Func<IQueryable<T>, string, IQueryable<T>> searchProps = null,
            Func<IQueryable<T>, NameValueCollection, IQueryable<T>> before = null,
            Func<IQueryable<T>, NameValueCollection, IQueryable<T>> middle = null,
            Func<IQueryable<T>, NameValueCollection, IQueryable<T>> after = null
        ) where T : class
          where R : class
        {
            NameValueCollection queryParams = HttpUtility.ParseQueryString(query);

            int page = GetIntParam(queryParams, "page", DefaultPage);
            int itemsPerPage = GetIntParam(queryParams, "itemsPerPage", DefaultItemsPerPage);
            string search = GetParam(queryParams, "search", "");

            var beforeQuery = (before != null) ? before(dbSet, queryParams) : dbSet;

            var preMiddle = beforeQuery.OrderBy("Id");
            var middleQuery = (middle != null) ? middle(preMiddle, queryParams) : preMiddle;

            int totalItems = await middleQuery.CountAsync();

            int totalPages;
            IQueryable<R> preList;

            if (itemsPerPage > 0)
            {
                IQueryable<T> preAfter = middleQuery.Skip((page - 1) * itemsPerPage).Take(itemsPerPage);
                preList = mutation((after != null) ? after(preAfter, queryParams) : preAfter);

                totalPages = (int)Math.Ceiling((double)totalItems / itemsPerPage);
            }
            else
            {
                preList = mutation((after != null) ? after(middleQuery, queryParams) : middleQuery);

                totalPages = 1;
                itemsPerPage = totalItems;
            }

            Paginator<R> resultado = new Paginator<R>()
            {
                ItemsPerPage = itemsPerPage,
                TotalItems = totalItems,
                TotalPages = totalPages,
                CurrentPage = page,
                Items = await preList.ToListAsync()
            };

            return new OkObjectResult(resultado);
        }

    }

    public static class PaginatorExtension
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
