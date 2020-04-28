using System;
using System.Collections.Specialized;
using Terradue.OpenSearch;
using Terradue.OpenSearch.Engine;
using Terradue.OpenSearch.Request;
using Terradue.OpenSearch.Result;
using Terradue.OpenSearch.Schema;

namespace Terradue.WebService.Ogc.Sos.OpenSearch
{
    public abstract class SosOpenSearchable : IOpenSearchable
    {
        string connectionString;

        public SosOpenSearchable(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public virtual bool CanCache
        {
            get
            {
                return true;
            }
        }

        public virtual string DefaultMimeType
        {
            get
            {
                return "application/atom+xml";
            }
        }

        public abstract string Identifier { get; }

        public abstract long TotalResults { get; }

        public virtual void ApplyResultFilters(OpenSearchRequest request, ref IOpenSearchResultCollection osr, string finalContentType)
        {
           
        }

        public abstract OpenSearchRequest Create(QuerySettings querySettings, NameValueCollection parameters);

        public OpenSearchDescription GetOpenSearchDescription()
        {
            throw new NotImplementedException();
        }

        public NameValueCollection GetOpenSearchParameters(string mimeType)
        {
            throw new NotImplementedException();
        }

        public abstract QuerySettings GetQuerySettings(OpenSearchEngine ose);
    }
}