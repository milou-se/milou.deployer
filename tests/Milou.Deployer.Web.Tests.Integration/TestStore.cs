using System;
using System.Collections.Generic;
using System.Data;
using Marten;
using Marten.Events;
using Marten.Events.Projections;
using Marten.Events.Projections.Async;
using Marten.Schema;
using Marten.Services;
using Marten.Storage;
using Marten.Transforms;

namespace Milou.Deployer.Web.Tests.Integration
{
    public class TestStore : IDocumentStore
    {
        public void Dispose()
        {
        }

        public void BulkInsert<T>(IReadOnlyCollection<T> documents, BulkInsertMode mode = BulkInsertMode.InsertsOnly, int batchSize = 1000) => throw new NotImplementedException();

        public void BulkInsert<T>(string tenantId,
            IReadOnlyCollection<T> documents,
            BulkInsertMode mode = BulkInsertMode.InsertsOnly,
            int batchSize = 1000) =>
            throw new NotImplementedException();

        public IDocumentSession OpenSession(DocumentTracking tracking = DocumentTracking.IdentityOnly, IsolationLevel isolationLevel = IsolationLevel.ReadCommitted) => new TestDocumentSession();

        public IDocumentSession OpenSession(string tenantId,
            DocumentTracking tracking = DocumentTracking.IdentityOnly,
            IsolationLevel isolationLevel = IsolationLevel.ReadCommitted) =>
            throw new NotImplementedException();

        public IDocumentSession OpenSession(SessionOptions options) => new TestDocumentSession();

        public IDocumentSession LightweightSession(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted) => throw new NotImplementedException();

        public IDocumentSession LightweightSession(string tenantId, IsolationLevel isolationLevel = IsolationLevel.ReadCommitted) => throw new NotImplementedException();

        public IDocumentSession DirtyTrackedSession(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted) => throw new NotImplementedException();

        public IDocumentSession DirtyTrackedSession(string tenantId, IsolationLevel isolationLevel = IsolationLevel.ReadCommitted) => throw new NotImplementedException();

        public IQuerySession QuerySession() => throw new NotImplementedException();

        public IQuerySession QuerySession(string tenantId) => throw new NotImplementedException();

        public IQuerySession QuerySession(SessionOptions options) => throw new NotImplementedException();

        public void BulkInsertDocuments(IEnumerable<object> documents, BulkInsertMode mode = BulkInsertMode.InsertsOnly, int batchSize = 1000) => throw new NotImplementedException();

        public void BulkInsertDocuments(string tenantId,
            IEnumerable<object> documents,
            BulkInsertMode mode = BulkInsertMode.InsertsOnly,
            int batchSize = 1000) =>
            throw new NotImplementedException();

        public IDaemon BuildProjectionDaemon(Type[] viewTypes = null,
            IDaemonLogger logger = null,
            DaemonSettings settings = null,
            IProjection[] projections = null) =>
            throw new NotImplementedException();

        public IDocumentSchema Schema { get; }
        public AdvancedOptions Advanced { get; }
        public IDiagnostics Diagnostics { get; }
        public IDocumentTransforms Transform { get; }
        public EventGraph Events { get; }
        public ITenancy Tenancy { get; }
    }
}