using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Marten;
using Marten.Events;
using Marten.Linq;
using Marten.Patching;
using Marten.Services;
using Marten.Services.BatchQuerying;
using Marten.Storage;
using Npgsql;

namespace Milou.Deployer.Web.Tests.Integration
{
    public class TestDocumentSession : IDocumentSession
    {
        public void Dispose()
        {
        }

        public T Load<T>(string id) => throw new NotImplementedException();

        public async Task<T> LoadAsync<T>(string id, CancellationToken token = new CancellationToken())
        {
            return default(T);
        }

        public T Load<T>(int id) => throw new NotImplementedException();

        public T Load<T>(long id) => throw new NotImplementedException();

        public T Load<T>(Guid id) => throw new NotImplementedException();

        public Task<T> LoadAsync<T>(int id, CancellationToken token = new CancellationToken()) => throw new NotImplementedException();

        public Task<T> LoadAsync<T>(long id, CancellationToken token = new CancellationToken()) => throw new NotImplementedException();

        public Task<T> LoadAsync<T>(Guid id, CancellationToken token = new CancellationToken()) => throw new NotImplementedException();

        public IMartenQueryable<T> Query<T>() => throw new NotImplementedException();

        public IReadOnlyList<T> Query<T>(string sql, params object[] parameters) => throw new NotImplementedException();

        public Task<IReadOnlyList<T>> QueryAsync<T>(string sql, CancellationToken token = new CancellationToken(), params object[] parameters) => throw new NotImplementedException();

        public IBatchedQuery CreateBatchQuery() => throw new NotImplementedException();

        public TOut Query<TDoc, TOut>(ICompiledQuery<TDoc, TOut> query) => throw new NotImplementedException();

        public Task<TOut> QueryAsync<TDoc, TOut>(ICompiledQuery<TDoc, TOut> query, CancellationToken token = new CancellationToken()) => throw new NotImplementedException();

        public IReadOnlyList<T> LoadMany<T>(params string[] ids) => throw new NotImplementedException();
        public IReadOnlyList<T> LoadMany<T>(IEnumerable<string> ids) => throw new NotImplementedException();

        public IReadOnlyList<T> LoadMany<T>(params Guid[] ids) => throw new NotImplementedException();
        public IReadOnlyList<T> LoadMany<T>(IEnumerable<Guid> ids) => throw new NotImplementedException();

        public IReadOnlyList<T> LoadMany<T>(params int[] ids) => throw new NotImplementedException();
        public IReadOnlyList<T> LoadMany<T>(IEnumerable<int> ids) => throw new NotImplementedException();

        public IReadOnlyList<T> LoadMany<T>(params long[] ids) => throw new NotImplementedException();
        public IReadOnlyList<T> LoadMany<T>(IEnumerable<long> ids) => throw new NotImplementedException();

        public Task<IReadOnlyList<T>> LoadManyAsync<T>(params string[] ids) => throw new NotImplementedException();
        public async Task<IReadOnlyList<T>> LoadManyAsync<T>(IEnumerable<string> ids) => throw new NotImplementedException();

        public Task<IReadOnlyList<T>> LoadManyAsync<T>(params Guid[] ids) => throw new NotImplementedException();
        public async Task<IReadOnlyList<T>> LoadManyAsync<T>(IEnumerable<Guid> ids) => throw new NotImplementedException();

        public Task<IReadOnlyList<T>> LoadManyAsync<T>(params int[] ids) => throw new NotImplementedException();
        public async Task<IReadOnlyList<T>> LoadManyAsync<T>(IEnumerable<int> ids) => throw new NotImplementedException();

        public Task<IReadOnlyList<T>> LoadManyAsync<T>(params long[] ids) => throw new NotImplementedException();
        public async Task<IReadOnlyList<T>> LoadManyAsync<T>(IEnumerable<long> ids) => throw new NotImplementedException();

        public Task<IReadOnlyList<T>> LoadManyAsync<T>(CancellationToken token, params string[] ids) => throw new NotImplementedException();
        public async Task<IReadOnlyList<T>> LoadManyAsync<T>(CancellationToken token, IEnumerable<string> ids) => throw new NotImplementedException();

        public Task<IReadOnlyList<T>> LoadManyAsync<T>(CancellationToken token, params Guid[] ids) => throw new NotImplementedException();
        public async Task<IReadOnlyList<T>> LoadManyAsync<T>(CancellationToken token, IEnumerable<Guid> ids) => throw new NotImplementedException();

        public Task<IReadOnlyList<T>> LoadManyAsync<T>(CancellationToken token, params int[] ids) => throw new NotImplementedException();
        public async Task<IReadOnlyList<T>> LoadManyAsync<T>(CancellationToken token, IEnumerable<int> ids) => throw new NotImplementedException();

        public Task<IReadOnlyList<T>> LoadManyAsync<T>(CancellationToken token, params long[] ids) => throw new NotImplementedException();
        public async Task<IReadOnlyList<T>> LoadManyAsync<T>(CancellationToken token, IEnumerable<long> ids) => throw new NotImplementedException();

        public Guid? VersionFor<TDoc>(TDoc entity) => throw new NotImplementedException();

        public IReadOnlyList<TDoc> Search<TDoc>(string queryText, string regConfig = "english") => throw new NotImplementedException();

        public Task<IReadOnlyList<TDoc>> SearchAsync<TDoc>(string queryText,
            string regConfig = "english",
            CancellationToken token = new CancellationToken()) =>
            throw new NotImplementedException();

        public IReadOnlyList<TDoc> PlainTextSearch<TDoc>(string searchTerm, string regConfig = "english") => throw new NotImplementedException();

        public Task<IReadOnlyList<TDoc>> PlainTextSearchAsync<TDoc>(string searchTerm,
            string regConfig = "english",
            CancellationToken token = new CancellationToken()) =>
            throw new NotImplementedException();

        public IReadOnlyList<TDoc> PhraseSearch<TDoc>(string searchTerm, string regConfig = "english") => throw new NotImplementedException();

        public Task<IReadOnlyList<TDoc>> PhraseSearchAsync<TDoc>(string searchTerm,
            string regConfig = "english",
            CancellationToken token = new CancellationToken()) =>
            throw new NotImplementedException();

        public IReadOnlyList<TDoc> WebStyleSearch<TDoc>(string searchTerm, string regConfig = "english") => throw new NotImplementedException();

        public Task<IReadOnlyList<TDoc>> WebStyleSearchAsync<TDoc>(string searchTerm,
            string regConfig = "english",
            CancellationToken token = new CancellationToken()) =>
            throw new NotImplementedException();

        public NpgsqlConnection Connection { get; }
        public IMartenSessionLogger Logger { get; set; }
        public int RequestCount { get; }
        public IDocumentStore DocumentStore { get; }
        public IJsonLoader Json { get; }
        public ITenant Tenant { get; }
        public ISerializer Serializer { get; }
        public void Delete<T>(T entity) => throw new NotImplementedException();

        public void Delete<T>(int id) => throw new NotImplementedException();

        public void Delete<T>(long id) => throw new NotImplementedException();

        public void Delete<T>(Guid id) => throw new NotImplementedException();

        public void Delete<T>(string id) => throw new NotImplementedException();

        public void DeleteWhere<T>(Expression<Func<T, bool>> expression) => throw new NotImplementedException();

        public void SaveChanges() => throw new NotImplementedException();

        public Task SaveChangesAsync(CancellationToken token = new CancellationToken()) => Task.CompletedTask;
        public void Store<T>(IEnumerable<T> entities) => throw new NotImplementedException();

        public void Store<T>(params T[] entities)  {
        }

        public void Store<T>(string tenantId, IEnumerable<T> entities) => throw new NotImplementedException();

        public void Store<T>(string tenantId, params T[] entities) => throw new NotImplementedException();

        public void Store<T>(T entity, Guid version) => throw new NotImplementedException();
        public void Insert<T>(IEnumerable<T> entities) => throw new NotImplementedException();

        public void Insert<T>(params T[] entities) => throw new NotImplementedException();
        public void Update<T>(IEnumerable<T> entities) => throw new NotImplementedException();

        public void Update<T>(params T[] entities) => throw new NotImplementedException();

        public void InsertObjects(IEnumerable<object> documents) => throw new NotImplementedException();

        public void StoreObjects(IEnumerable<object> documents) => throw new NotImplementedException();

        public IPatchExpression<T> Patch<T>(int id) => throw new NotImplementedException();

        public IPatchExpression<T> Patch<T>(long id) => throw new NotImplementedException();

        public IPatchExpression<T> Patch<T>(string id) => throw new NotImplementedException();

        public IPatchExpression<T> Patch<T>(Guid id) => throw new NotImplementedException();

        public IPatchExpression<T> Patch<T>(Expression<Func<T, bool>> @where) => throw new NotImplementedException();

        public IPatchExpression<T> Patch<T>(IWhereFragment fragment) => throw new NotImplementedException();

        public void QueueOperation(IStorageOperation storageOperation) => throw new NotImplementedException();

        public void Eject<T>(T document) => throw new NotImplementedException();

        public void EjectAllOfType(Type type) => throw new NotImplementedException();

        public IUnitOfWork PendingChanges { get; }
        public IEventStore Events { get; }
        public ConcurrencyChecks Concurrency { get; }
        public IList<IDocumentSessionListener> Listeners { get; }
    }
}