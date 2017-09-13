﻿using System;
using System.Linq;
using System.Linq.Expressions;
using Couchbase.Configuration.Client;
using Couchbase.Core;
using Couchbase.Core.Version;
using Couchbase.Linq.Execution;
using Couchbase.Linq.QueryGeneration;
using Couchbase.Linq.QueryGeneration.MemberNameResolvers;
using Moq;
using Newtonsoft.Json.Serialization;
using Remotion.Linq;

namespace Couchbase.Linq.UnitTests
{
// ReSharper disable once InconsistentNaming
    public class N1QLTestBase
    {
        protected static readonly ClusterVersion DefaultClusterVersion = new ClusterVersion(new Version(4, 0, 0));

        private IMemberNameResolver _memberNameResolver = new JsonNetMemberNameResolver(new DefaultContractResolver());
        internal IMemberNameResolver MemberNameResolver
        {
            get { return _memberNameResolver; }
        }

        private BucketQueryExecutorEmulator _queryExecutor;
        internal BucketQueryExecutorEmulator QueryExecutor
        {
            get
            {
                if (_queryExecutor == null)
                {
                    _queryExecutor = new BucketQueryExecutorEmulator(this);
                }

                return _queryExecutor;
            }
        }

        protected string CreateN1QlQuery(IBucket bucket, Expression expression)
        {
            return CreateN1QlQuery(bucket, expression, false);
        }

        protected string CreateN1QlQuery(IBucket bucket, Expression expression, ClusterVersion clusterVersion)
        {
            return CreateN1QlQuery(bucket, expression, clusterVersion, false);
        }

        protected string CreateN1QlQuery(IBucket bucket, Expression expression, bool selectDocumentMetadata)
        {
            return CreateN1QlQuery(bucket, expression, DefaultClusterVersion, selectDocumentMetadata);
        }

        protected string CreateN1QlQuery(IBucket bucket, Expression expression, ClusterVersion clusterVersion,
            bool selectDocumentMetadata)
        {
            ScalarResultBehavior resultBehavior;
            return CreateN1QlQuery(bucket, expression, clusterVersion, selectDocumentMetadata, out resultBehavior);
        }

        internal string CreateN1QlQuery(IBucket bucket, Expression expression, ClusterVersion clusterVersion,
            bool selectDocumentMetadata, out ScalarResultBehavior resultBehavior)
        {
            var queryModel = QueryParserHelper.CreateQueryParser().GetParsedQuery(expression);

            var queryGenerationContext = new N1QlQueryGenerationContext()
            {
                MemberNameResolver = MemberNameResolver,
                MethodCallTranslatorProvider = new DefaultMethodCallTranslatorProvider(),
                Serializer = new Core.Serialization.DefaultSerializer(),
                SelectDocumentMetadata = selectDocumentMetadata,
                ClusterVersion = clusterVersion
            };

            var visitor = new N1QlQueryModelVisitor(queryGenerationContext);
            visitor.VisitQueryModel(queryModel);

            resultBehavior = visitor.ScalarResultBehavior;
            return visitor.GetQuery();
        }

        protected virtual IQueryable<T> CreateQueryable<T>(string bucketName)
        {
            var mockBucket = new Mock<IBucket>();
            mockBucket.SetupGet(e => e.Name).Returns(bucketName);

            return new BucketQueryable<T>(mockBucket.Object,
                QueryParserHelper.CreateQueryParser(), QueryExecutor);
        }

        protected void SetContractResolver(IContractResolver contractResolver)
        {
            _memberNameResolver = new JsonNetMemberNameResolver(contractResolver);
        }
    }
}