﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Transactions;
using CollectionsOnline.Core.Config;
using CollectionsOnline.Core.Indexes;
using CollectionsOnline.Core.Models;
using CollectionsOnline.Core.Extensions;
using CollectionsOnline.Import.Factories;
using IMu;
using NLog;
using Raven.Abstractions.Extensions;
using Raven.Client;

namespace CollectionsOnline.Import.Imports
{
    public class MediaUpdateImport : IImport
    {
        private readonly Logger _log = LogManager.GetCurrentClassLogger();
        private readonly IDocumentStore _documentStore;
        private readonly Session _session;
        private readonly IMediaFactory _mediaFactory;

        public MediaUpdateImport(
            IDocumentStore documentStore,
            Session session,
            IMediaFactory mediaFactory)
        {
            _documentStore = documentStore;
            _session = session;
            _mediaFactory = mediaFactory;
        }

        public void Run()
        {
            var module = new Module("emultimedia", _session);
            var columns = new[]
                                {
                                    "irn",
                                    "MulTitle",
                                    "MulMimeType",
                                    "MulDescription",
                                    "MulCreator_tab",
                                    "MdaDataSets_tab",
                                    "MdaElement_tab",
                                    "MdaQualifier_tab",
                                    "MdaFreeText_tab",
                                    "ChaRepository_tab",
                                    "DetAlternateText",
                                    "AdmPublishWebNoPassword",
                                    "AdmDateModified",
                                    "AdmTimeModified"
                                };
            var associatedDocumentCount = 0;

            using (var documentSession = _documentStore.OpenSession())
            {
                // Check to see whether we need to run import, so grab the earliest previous date run of any imports that utilize multimedia.
                var previousDateRun = documentSession
                    .Load<Application>(Constants.ApplicationId)
                    .ImportStatuses.Where(x => x.ImportType.Contains(typeof(ImuImport<>).Name, StringComparison.OrdinalIgnoreCase))
                    .Select(x => x.PreviousDateRun)
                    .OrderBy(x => x)
                    .FirstOrDefault(x => x.HasValue);

                // Exit current import if it has never run.
                if(!previousDateRun.HasValue)
                    return;

                // Check for existing import in case we need to resume.
                var importStatus = documentSession.Load<Application>(Constants.ApplicationId).GetImportStatus(GetType().ToString());

                // Exit current import if it had completed previous time it was run.
                if (importStatus.IsFinished)
                {
                    return;
                }

                _log.Debug("Starting {0} import", GetType().Name);

                // Cache the search results
                if (importStatus.CachedResult == null)
                {
                    var terms = new Terms();
                    terms.Add("MdaDataSets_tab", Constants.ImuMultimediaQueryString);
                    terms.Add("AdmDateModified", previousDateRun.Value.ToString("MMM dd yyyy"), ">=");
                    importStatus.CachedResult = new List<long>();
                    importStatus.CachedResultDate = DateTime.Now;

                    var hits = module.FindTerms(terms);

                    _log.Debug("Caching {0} search results. {1} Hits", GetType().Name, hits);

                    var cachedCurrentOffset = 0;
                    while (true)
                    {
                        if (ImportCanceled())
                            return;

                        var results = module.Fetch("start", cachedCurrentOffset, Constants.CachedDataBatchSize, new[] { "irn" });

                        if (results.Count == 0)
                            break;

                        importStatus.CachedResult.AddRange(results.Rows.Select(x => long.Parse(x.GetString("irn"))));

                        cachedCurrentOffset += results.Count;

                        _log.Debug("{0} cache progress... {1}/{2}", GetType().Name, cachedCurrentOffset, hits);
                    }

                    // Store cached result
                    documentSession.SaveChanges();

                    _log.Debug("Caching of {0} search results complete, beginning import.", GetType().Name);
                }
                else
                {
                    _log.Debug("Cached search results found, resuming {0} import.", GetType().Name);
                }
            }

            // Perform import
            while (true)
            {
                using (var tx = new TransactionScope())
                using (var documentSession = _documentStore.OpenSession())
                {
                    if (ImportCanceled())
                        return;

                    var importStatus = documentSession.Load<Application>(Constants.ApplicationId).GetImportStatus(GetType().ToString());

                    var cachedResultBatch = importStatus.CachedResult
                        .Skip(importStatus.CurrentOffset)
                        .Take(Constants.DataBatchSize)
                        .ToList();

                    if (cachedResultBatch.Count == 0)
                        break;

                    module.FindKeys(cachedResultBatch);

                    var results = module.Fetch("start", 0, -1, columns);

                    foreach (var row in results.Rows)
                    {
                        var mediaIrn = long.Parse(row.GetString("irn"));

                        var count = 0;
                        while (true)
                        {
                            using (var associatedDocumentSession = _documentStore.OpenSession())
                            {
                                if (ImportCanceled())
                                    return;

                                var associatedDocumentBatch = associatedDocumentSession
                                    .Query<object, Combined>()
                                    .Where(x => ((CombinedResult)x).MediaIrns.Any(y => y == mediaIrn))
                                    .Skip(count)
                                    .Take(Constants.DataBatchSize)
                                    .ToList();

                                if (associatedDocumentBatch.Count == 0)
                                    break;

                                foreach (var document in associatedDocumentBatch)
                                {
                                    var media = _mediaFactory.Make(row);

                                    // Determine type of document
                                    var item = document as Item;
                                    if (item != null)
                                    {
                                        var existingMedia = item.Media.SingleOrDefault(x => x.Irn == mediaIrn);
                                        if (existingMedia != null)
                                            item.Media[item.Media.IndexOf(existingMedia)] = media;

                                        associatedDocumentCount++;
                                        continue;
                                    }

                                    var species = document as Species;
                                    if (species != null)
                                    {
                                        var existingMedia = species.Media.SingleOrDefault(x => x.Irn == mediaIrn);
                                        if (existingMedia != null)
                                            species.Media[species.Media.IndexOf(existingMedia)] = media;

                                        var author = species.Authors.SingleOrDefault(x => x.Media.Irn == mediaIrn);
                                        if (author != null)
                                            author.Media = media;

                                        associatedDocumentCount++;
                                        continue;
                                    }

                                    var specimen = document as Specimen;
                                    if (specimen != null)
                                    {
                                        var existingMedia = specimen.Media.SingleOrDefault(x => x.Irn == mediaIrn);
                                        if (existingMedia != null)
                                            specimen.Media[specimen.Media.IndexOf(existingMedia)] = media;

                                        associatedDocumentCount++;
                                        continue;
                                    }

                                    var story = document as Story;
                                    if (story != null)
                                    {
                                        var existingMedia = story.Media.SingleOrDefault(x => x.Irn == mediaIrn);
                                        if (existingMedia != null)
                                            story.Media[story.Media.IndexOf(existingMedia)] = media;

                                        var author = story.Authors.SingleOrDefault(x => x.Media.Irn == mediaIrn);
                                        if (author != null)
                                            author.Media = media;

                                        associatedDocumentCount++;
                                    }
                                }

                                // Save any changes
                                associatedDocumentSession.SaveChanges();
                                count += associatedDocumentBatch.Count;
                            }
                        }
                    }

                    importStatus.CurrentOffset += results.Count;

                    _log.Debug("{0} import progress... {1}/{2}", GetType().Name, importStatus.CurrentOffset, importStatus.CachedResult.Count);
                    documentSession.SaveChanges();

                    tx.Complete();
                }                
            }

            _log.Debug("{0} import complete, updated {1} associated documents", GetType().Name, associatedDocumentCount);

            using (var documentSession = _documentStore.OpenSession())
            {
                documentSession.Load<Application>(Constants.ApplicationId).ImportFinished(GetType().ToString());
                documentSession.SaveChanges();
            }
        }

        public int Order
        {
            get { return 10; }
        }

        private bool ImportCanceled()
        {
            if (DateTime.Now.TimeOfDay > Constants.ImuOfflineTimeSpan)
            {
                _log.Warn("Imu about to go offline, canceling all imports");
                Program.ImportCanceled = true;
            }

            return Program.ImportCanceled;
        }
    }
}