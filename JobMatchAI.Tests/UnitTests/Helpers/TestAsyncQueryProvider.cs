using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using System.Linq.Expressions;
using System.Reflection;

namespace JobMatchAI.Tests.UnitTests.Helpers
{
    internal class TestAsyncQueryProvider<TEntity> : IAsyncQueryProvider
    {
        private readonly IQueryProvider _inner;
        private readonly IList<TEntity>? _backingList;

        internal TestAsyncQueryProvider(IQueryProvider inner, IList<TEntity>? backingList = null)
        {
            _inner = inner;
            _backingList = backingList;
        }

        public IQueryable CreateQuery(Expression expression)
        {
            return new TestAsyncEnumerable<TEntity>(expression, _backingList);
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            var backingList = _backingList is IList<TElement> list ? list : null;
            return new TestAsyncEnumerable<TElement>(expression, backingList);
        }

        public object? Execute(Expression expression)
        {
            if (expression is MethodCallExpression mc && mc.Method.Name.StartsWith("ExecuteDelete"))
            {
                var sourceQuery = mc.Arguments[0];
                var matchingItems = _inner.CreateQuery<TEntity>(sourceQuery).Cast<TEntity>().ToList();
                int count = matchingItems.Count;

                if (_backingList != null)
                {
                    foreach (var item in matchingItems)
                    {
                        _backingList.Remove(item);
                    }
                }

                return count;
            }

            if (expression is MethodCallExpression updateMc && updateMc.Method.Name.StartsWith("ExecuteUpdate"))
            {
                var sourceQuery = updateMc.Arguments[0];
                var matchingItems = _inner.CreateQuery<TEntity>(sourceQuery).Cast<TEntity>().ToList();
                return matchingItems.Count;
            }

            return _inner.Execute(expression);
        }

        public TResult Execute<TResult>(Expression expression)
        {
            return (TResult)Execute(expression)!;
        }

        public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken)
        {
            var result = Execute(expression);
            var expectedResultType = typeof(TResult).GetGenericArguments()[0];

            return (TResult)typeof(Task).GetMethod(nameof(Task.FromResult))!
                .MakeGenericMethod(expectedResultType)
                .Invoke(null, [result])!;
        }
    }

    internal class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
    {
        private readonly IList<T>? _backingList;

        public TestAsyncEnumerable(IEnumerable<T> enumerable, IList<T>? backingList = null) : base(enumerable)
        {
            _backingList = backingList;
        }

        public TestAsyncEnumerable(Expression expression, IList<T>? backingList = null) : base(expression)
        {
            _backingList = backingList;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            return new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
        }

        IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this, _backingList);
    }

    internal class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
    {
        private readonly IEnumerator<T> _inner;

        public TestAsyncEnumerator(IEnumerator<T> inner)
        {
            _inner = inner;
        }

        public T Current => _inner.Current;

        public ValueTask DisposeAsync()
        {
            _inner.Dispose();
            return ValueTask.CompletedTask;
        }

        public ValueTask<bool> MoveNextAsync()
        {
            return ValueTask.FromResult(_inner.MoveNext());
        }
    }

    public static class DbSetMockHelper
    {
        public static DbSet<T> CreateMockDbSet<T>(IQueryable<T> queryable) where T : class
        {
            IList<T> backingList = queryable as IList<T>
                ?? (queryable is EnumerableQuery<T> eq ? GetBackingListFromEnumerableQuery(eq) : queryable.ToList());

            return CreateInternal(queryable, backingList);
        }

        public static DbSet<T> CreateMockDbSet<T>(List<T> backingList) where T : class
        {
            return CreateInternal(backingList.AsQueryable(), backingList);
        }

        public static DbSet<T> CreateMockDbSet<T>(IList<T> backingList) where T : class
        {
            var list = backingList as List<T> ?? [.. backingList];
            return CreateInternal(list.AsQueryable(), list);
        }

        private static DbSet<T> CreateInternal<T>(IQueryable<T> queryable, IList<T> backingList) where T : class
        {
            var dbSetMock = new Mock<DbSet<T>>();
            dbSetMock.As<IQueryable<T>>().Setup(m => m.Provider).Returns(new TestAsyncQueryProvider<T>(queryable.Provider, backingList));
            dbSetMock.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryable.Expression);
            dbSetMock.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
            dbSetMock.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(queryable.GetEnumerator());
            dbSetMock.As<IAsyncEnumerable<T>>().Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
                .Returns(new TestAsyncEnumerator<T>(queryable.GetEnumerator()));

            dbSetMock.Setup(d => d.FindAsync(It.IsAny<object[]>()))
                .Returns((object[] ids) =>
                {
                    var id = (Guid)ids[0];
                    var idProp = typeof(T).GetProperty("Id");
                    var entity = backingList.FirstOrDefault(e =>
                    idProp != null && (Guid)idProp.GetValue(e)! == id);

                    return ValueTask.FromResult(entity);
                });

            dbSetMock.Setup(d => d.Add(It.IsAny<T>()))
                .Returns((T entity) =>
                {
                    backingList.Add(entity);
                    return (EntityEntry<T>)null!;
                });

            dbSetMock.Setup(d => d.AddAsync(It.IsAny<T>(), It.IsAny<CancellationToken>()))
                .Returns((T entity, CancellationToken ct) =>
                {
                    backingList.Add(entity);
                    return ValueTask.FromResult((EntityEntry<T>)null!);
                });

            return dbSetMock.Object;
        }

        private static IList<T> GetBackingListFromEnumerableQuery<T>(EnumerableQuery<T> eq)
        {
            var field = typeof(EnumerableQuery<T>).GetField("_enumerable", BindingFlags.NonPublic | BindingFlags.Instance)
                     ?? typeof(EnumerableQuery<T>).GetField("enumerable", BindingFlags.NonPublic | BindingFlags.Instance);
            if (field?.GetValue(eq) is IEnumerable<T> enumerable)
            {
                return enumerable as IList<T> ?? enumerable.ToList();
            }
            return eq.ToList();
        }
    }
}