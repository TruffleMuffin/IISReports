using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using IISContracts;
using IISReports.Models;
using PlainElastic.Net;
using PlainElastic.Net.Mappings;
using PlainElastic.Net.Queries;
using PlainElastic.Net.Serialization;
using PlainElastic.Net.Utils;

namespace IISReports.Services
{
    public class LogService
    {
        private const string LOG_TYPE = "iis-log";
        private const string HIT_HISTOGRAM_DSL = "\"{0}\": { \"date_histogram\": { \"key_field\": \"Timestamp\",\"value_field\": \"ResponseStatusCode\",\"interval\": \"day\"}, \"facet_filter\": { \"term\": { \"ResponseStatusCode\" : \"{1}\" } } }";

        private readonly IElasticConnection connection;

        private static LogService instance;
        public static LogService Instance { get { return instance ?? (instance = new LogService()); } }

        /// <summary>
        /// Initializes a new instance of the <see cref="LogService" /> class.
        /// </summary>
        public LogService()
        {
            this.connection = new ElasticConnection("localhost", 9200);
        }

        public void Import(IEnumerable<IISViewModel> models)
        {
            // Identify Logs bounded by Month-Year
            var logsByMonthYear = models
                .GroupBy(a => GenerateIndexName(a.Timestamp.Year, a.Timestamp.Month))
                .ToArray();

            foreach (var grouping in logsByMonthYear)
            {
                EnsureIndexIntiailised(grouping.Key);

                // setup variable to group all error messages together
                var errorBuilder = new StringBuilder();
                var serializer = new JsonNetSerializer();

                // Build a batch based update to avoid memory overhead
                var bulkCommand = new BulkCommand(grouping.Key, LOG_TYPE);
                var bulkJsons =
                    new BulkBuilder(serializer)
                    .PipelineCollection(grouping.ToList(), (builder, entity) => builder.Index(entity, id: entity.Id))
                    .JoinInBatches(batchSize: 100);

                foreach (var bulk in bulkJsons)
                {
                    string result = connection.Post(bulkCommand, bulk);

                    var bulkResult = serializer.ToBulkResult(result);

                    // Check for errors
                    foreach (var operation in bulkResult.items)
                    {
                        if (operation.Result.ok == false)
                        {
                            errorBuilder.AppendFormat("Id: {0} Error: {1} {2}", operation.Result._id, operation.Result.error, Environment.NewLine);
                        }
                    }
                }

                // Check for any errors that are reported by the ElasticSearch
                var error = errorBuilder.ToString();
                if (string.IsNullOrWhiteSpace(error) == false) throw new ApplicationException(error);
            }
        }

        public IEnumerable<HitAndResponseViewModel> GetHits(int year, int month)
        {
            var serializer = new JsonNetSerializer();

            // Build the Search Query based on the input
            var elasticQuery = new QueryBuilder<IISViewModel>()
                .Facets(f => f
                    .TermsStats(t => t.FacetName("ResponseCodes").KeyField(k => k.ResponseStatusCode).ValueField(k => k.ResponseStatusCode))
                )
                .Build();

            // Execute the search
            string result = connection.Post(Commands.Search(GenerateIndexName(year, month), LOG_TYPE), elasticQuery);
            var searchResult = serializer.ToSearchResult<IISViewModel>(result);


            return searchResult.facets.Facet<TermsStatsFacetResult>("ResponseCodes").terms.Select(a => new HitAndResponseViewModel { Code = a.term, Count = a.count }).ToArray();
        }

        public IEnumerable<AgentViewModel> GetAgents(int year, int month)
        {
            var serializer = new JsonNetSerializer();

            // Build the Search Query based on the input
            var elasticQuery = new QueryBuilder<IISViewModel>()
                .Facets(f => f
                    .Terms(t => t.FacetName("Agents").Field(k => k.UserAgent))
                )
                .Build();

            // Execute the search
            string result = connection.Post(Commands.Search(GenerateIndexName(year, month), LOG_TYPE), elasticQuery);
            var searchResult = serializer.ToSearchResult<IISViewModel>(result);


            return searchResult.facets.Facet<TermsFacetResult>("Agents").terms.Select(a => new AgentViewModel(a.term, a.count)).ToArray();
        }

        private static string GenerateIndexName(int year, int month)
        {
            return "logs-" + year + "-" + month;
        }

        private void EnsureIndexIntiailised(string index)
        {
            if (IsIndexExists(index) == false)
            {
                var mapping = new MapBuilder<IISViewModel>()
                .RootObject(
                    typeName: LOG_TYPE,
                    map: r =>
                         r.Properties(p => p
                             .String(s => s.UserAgent, s => s.Analyzer(DefaultAnalyzers.whitespace))
                             .String(s => s.Url, s => s.Analyzer(DefaultAnalyzers.whitespace))
                             .String(s => s.IpAddress, s => s.Analyzer(DefaultAnalyzers.whitespace))
                             .String(s => s.UserName, s => s.Analyzer(DefaultAnalyzers.whitespace))
                             .String(s => s.QueryString, s => s.Analyzer(DefaultAnalyzers.whitespace))
                        )
                )
                .BuildBeautified();

                // Create Index with settings.
                connection.Put(Commands.Index(index).Refresh());
                connection.Put(Commands.PutMapping(index, LOG_TYPE), mapping);
            }
        }

        /// <summary>
        /// Determines whether if the specified index exists.
        /// </summary>
        /// <param name="indexName">Name of the index.</param>
        /// <returns>
        ///   <c>true</c> if if the specified index exists; otherwise, <c>false</c>.
        /// </returns>
        private bool IsIndexExists(string indexName)
        {
            try
            {
                connection.Head(new IndexExistsCommand(indexName));
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

    }
}