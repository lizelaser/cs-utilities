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

        public static async Task<ActionResult> Paginate<T,I>(
            string query,
            DbSet<T> dbSet,
            SearchClient algolia,
            string indexUid,
            int totalItems,
            Func<IQueryable<T>, string, IQueryable<T>> searchProps = null,
            Func<IQueryable<T>, NameValueCollection, IQueryable<T>> before = null,
            Func<IQueryable<T>, NameValueCollection, IQueryable<T>> middle = null,
            Func<IQueryable<T>, NameValueCollection, IQueryable<T>> after = null
        ) where T:class
          where I:class
        {
            NameValueCollection queryParams = HttpUtility.ParseQueryString(query);
            bool useDB = GetParam(queryParams, "useDB", false);
            
            if (useDB)
            {
                return await Paginate(query, dbSet, searchProps, before, middle, after);
            }

            return await AlgoliaPaginate<I>(query, indexUid, algolia, totalItems);
        }

        public static async Task<ActionResult> AlgoliaPaginate<T>(
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
            int first = GetIntParam(queryParams, "first", 0);
            
            SearchIndex index = algolia.InitIndex(indexUid);

            bool hasFirst = false;

            List<T> items = new List<T>();
            if (first>0)
            {
                var firstEntity = await index.GetObjectAsync<T>(first.ToString());
                if (firstEntity != null)
                {
                    itemsPerPage -= 1;
                    items.Add(firstEntity);
                    hasFirst = true;
                }
            }
            
            var result = await index.SearchAsync<T>(new Query(search)
            {
                HitsPerPage = itemsPerPage>=0?itemsPerPage:1000,
                Page = page - 1,
            });
            
            items.AddRange(result.Hits);

            var paginator = new Paginator<T>()
            {
                CurrentPage = result.Page + 1,
                ItemsPerPage = hasFirst ? result.HitsPerPage+1 : result.HitsPerPage,
                Items = items,
                TotalItems = result.NbHits,
                TotalPages = result.NbPages
            };

            var obj = new ObjectResult(paginator);
            obj.StatusCode = StatusCodes.Status200OK;
            return obj;
        }

        public static async Task<ActionResult> MeiliPaginate<T>(
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

        public static async Task<ActionResult> Paginate<T>(
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
            int first = GetIntParam(queryParams, "first", 0);

            List<T> items = new List<T>();
            var hasFirst = false;
            
            if (first>0)
            {
                var firstEntity = await dbSet.FindAsync(first);
                if (firstEntity != null)
                {
                    itemsPerPage -= 1;
                    items.Add(firstEntity);
                    hasFirst = true;
                }
            }

            var beforeQuery = (before != null) ? before(dbSet, queryParams) : dbSet;

            var preMiddle = beforeQuery.OrderBy("Id");
            var middleQuery = (middle != null) ? middle(preMiddle, queryParams) : preMiddle;

            if (searchProps != null && !string.IsNullOrEmpty(search))
            {
                middleQuery = searchProps(middleQuery, search);
            }

            int totalItems = await middleQuery.CountAsync();

            int totalPages;

            if (itemsPerPage > 0)
            {
                IQueryable<T> preAfter = middleQuery.Skip((page - 1) * itemsPerPage).Take(itemsPerPage);
                var dbItems = await ((after != null) ? after(preAfter, queryParams) : preAfter).ToListAsync();
                items.AddRange(dbItems);
                totalPages = (int)Math.Ceiling((double)totalItems / itemsPerPage);
            }
            else
            {
                var dbItems = await ((after != null) ? after(middleQuery, queryParams) : middleQuery).ToListAsync();
                items.AddRange(dbItems);
                totalPages = 1;
                itemsPerPage = totalItems;
            }

            Paginator<T> result = new Paginator<T>()
            {
                ItemsPerPage = hasFirst ? itemsPerPage+1 : itemsPerPage,
                TotalItems = totalItems,
                TotalPages = totalPages,
                CurrentPage = page,
                Items = items
            };

            return new OkObjectResult(result);
        }
        // ReSharper disable once InconsistentNaming
        public static async Task<ActionResult> Paginate<T, R>(
            string query,
            DbSet<T> dbSet,
            Func<T, R> mutation,
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
            int first = GetIntParam(queryParams, "first", 0);

            List<R> items = new List<R>();
            var hasFirst = false;

            if (first>0)
            {
                var firstEntity = await dbSet.FindAsync(first);
                if (firstEntity != null)
                {
                    itemsPerPage -= 1;
                    items.Add(mutation(firstEntity));
                    hasFirst = true;
                }
            }

            var beforeQuery = (before != null) ? before(dbSet, queryParams) : dbSet;

            var preMiddle = beforeQuery.OrderBy("Id");
            var middleQuery = (middle != null) ? middle(preMiddle, queryParams) : preMiddle;

            int totalItems = await middleQuery.CountAsync();

            int totalPages;
            IQueryable<R> preList;

            if (itemsPerPage > 0)
            {
                IQueryable<T> preAfter = middleQuery.Skip((page - 1) * itemsPerPage).Take(itemsPerPage);
                preList = (after != null) 
                    ? after(preAfter, queryParams).Select(x=>mutation(x)) 
                    : preAfter.Select(x=>mutation(x));

                totalPages = (int)Math.Ceiling((double)totalItems / itemsPerPage);
            }
            else
            {
                preList = (after != null) 
                    ? after(middleQuery, queryParams).Select(x=>mutation(x)) 
                    : middleQuery.Select(x=>mutation(x));

                totalPages = 1;
                itemsPerPage = totalItems;
            }
            items.AddRange(await preList.ToListAsync());

            Paginator<R> resultado = new Paginator<R>()
            {
                ItemsPerPage = hasFirst ? itemsPerPage+1 : itemsPerPage,
                TotalItems = totalItems,
                TotalPages = totalPages,
                CurrentPage = page,
                Items = items
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
