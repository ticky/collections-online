﻿using System;
using System.Globalization;
using System.Linq;
using AutoMapper;
using CollectionsOnline.Core.Config;
using CollectionsOnline.Core.Extensions;
using CollectionsOnline.Core.Models;
using CollectionsOnline.Import.Extensions;
using IMu;
using Raven.Abstractions.Extensions;
using Raven.Client;
using Serilog;

namespace CollectionsOnline.Import.Factories
{
    public class SpecimenFactory : IEmuAggregateRootFactory<Specimen>
    {
        private readonly IPartiesNameFactory _partiesNameFactory;
        private readonly ITaxonomyFactory _taxonomyFactory;
        private readonly ICollectionEventFactory _collectionEventFactory;
        private readonly ICollectionSiteFactory _collectionSiteFactory;
        private readonly IMediaFactory _mediaFactory;
        private readonly IAssociationFactory _associationFactory;
        private readonly IMuseumLocationFactory _museumLocationFactory;
        private readonly ISummaryFactory _summaryFactory;
        private readonly ILicenceFactory _licenceFactory;

        public SpecimenFactory(
            IPartiesNameFactory partiesNameFactory,
            ITaxonomyFactory taxonomyFactory,
            ICollectionEventFactory collectionEventFactory,
            ICollectionSiteFactory collectionSiteFactory,
            IMediaFactory mediaFactory,
            IAssociationFactory associationFactory,
            IMuseumLocationFactory museumLocationFactory,
            ISummaryFactory summaryFactory,
            ILicenceFactory licenceFactory)
        {
            _partiesNameFactory = partiesNameFactory;
            _taxonomyFactory = taxonomyFactory;
            _collectionEventFactory = collectionEventFactory;
            _collectionSiteFactory = collectionSiteFactory;
            _mediaFactory = mediaFactory;
            _associationFactory = associationFactory;
            _museumLocationFactory = museumLocationFactory;
            _summaryFactory = summaryFactory;
            _licenceFactory = licenceFactory;
        }

        public string ModuleName
        {
            get { return "ecatalogue"; }
        }

        public string[] Columns
        {
            get
            {
                return new[]
                    {
                        "irn",
                        "AdmPublishWebNoPassword",
                        "ColRegPrefix",
                        "ColRegNumber",
                        "ColRegPart",
                        "ColTypeOfItem",
                        "AdmDateModified",
                        "AdmTimeModified",
                        "colevent=ColCollectionEventRef.(irn,ExpExpeditionName,ColCollectionEventCode,ColCollectionMethod,ColDateVisitedFrom,ColDateVisitedTo,ColTimeVisitedFrom,ColTimeVisitedTo,AquDepthToMet,AquDepthFromMet,site=ColSiteRef.(irn,SitSiteCode,SitSiteNumber,EraEra,EraAge1,EraAge2,EraMvStage,EraMvGroup_tab,EraMvRockUnit_tab,EraMvMember_tab,EraLithology_tab,geo=[LocOcean_tab,LocContinent_tab,LocCountry_tab,LocProvinceStateTerritory_tab,LocDistrictCountyShire_tab,LocTownship_tab,LocNearestNamedPlace_tab],LocPreciseLocation,LocElevationASLFromMt,LocElevationASLToMt,latlong=[LatLongitudeDecimal_nesttab,LatLatitudeDecimal_nesttab,LatDatum_tab,LatRadiusNumeric_tab,determinedBy=LatDeterminedByRef_tab.(NamPartyType,NamFullName,NamOrganisation,NamBranch,NamDepartment,NamOrganisation,NamOrganisationOtherNames_tab,NamSource,AddPhysStreet,AddPhysCity,AddPhysState,AddPhysCountry,ColCollaborationName),LatDetDate0,LatLatLongDetermination_tab,LatDetSource_tab],AdmPublishWebNoPassword),collectors=ColParticipantRef_tab.(NamPartyType,NamFullName,NamOrganisation,NamBranch,NamDepartment,NamOrganisation,NamOrganisationOtherNames_tab,NamSource,AddPhysStreet,AddPhysCity,AddPhysState,AddPhysCountry,ColCollaborationName))",
                        "SpeNoSpecimens",
                        "BirTotalClutchSize",
                        "SpeSex_tab",
                        "SpeStageAge_tab",
                        "storage=[StrSpecimenNature_tab,StrSpecimenForm_tab,StrFixativeTreatment_tab,StrStorageMedium_tab]",
                        "site=SitSiteRef.(irn,SitSiteCode,SitSiteNumber,EraEra,EraAge1,EraAge2,EraMvStage,EraMvGroup_tab,EraMvRockUnit_tab,EraMvMember_tab,EraLithology_tab,geo=[LocOcean_tab,LocContinent_tab,LocCountry_tab,LocProvinceStateTerritory_tab,LocDistrictCountyShire_tab,LocTownship_tab,LocNearestNamedPlace_tab],LocPreciseLocation,LocElevationASLFromMt,LocElevationASLToMt,latlong=[LatLongitudeDecimal_nesttab,LatLatitudeDecimal_nesttab,LatDatum_tab,LatRadiusNumeric_tab,determinedBy=LatDeterminedByRef_tab.(NamPartyType,NamFullName,NamOrganisation,NamBranch,NamDepartment,NamOrganisation,NamOrganisationOtherNames_tab,NamSource,AddPhysStreet,AddPhysCity,AddPhysState,AddPhysCountry,ColCollaborationName),LatDetDate0,LatLatLongDetermination_tab,LatDetSource_tab],AdmPublishWebNoPassword)",
                        "identifications=[IdeTypeStatus_tab,IdeCurrentNameLocal_tab,identifiers=IdeIdentifiedByRef_nesttab.(NamPartyType,NamFullName,NamOrganisation,NamBranch,NamDepartment,NamOrganisation,NamOrganisationOtherNames_tab,NamSource,AddPhysStreet,AddPhysCity,AddPhysState,AddPhysCountry,ColCollaborationName),IdeDateIdentified0,IdeQualifier_tab,IdeQualifierRank_tab,taxa=TaxTaxonomyRef_tab.(irn,ClaKingdom,ClaPhylum,ClaSubphylum,ClaSuperclass,ClaClass,ClaSubclass,ClaSuperorder,ClaOrder,ClaSuborder,ClaInfraorder,ClaSuperfamily,ClaFamily,ClaSubfamily,ClaGenus,ClaSubgenus,ClaSpecies,ClaSubspecies,AutAuthorString,ClaApplicableCode,comname=[ComName_tab,ComStatus_tab],relatedspecies=<enarratives:TaxTaxaRef_tab>.(irn,DetPurpose_tab))]",
                        "media=MulMultiMediaRef_tab.(irn,MulTitle,MulIdentifier,MulMimeType,MdaDataSets_tab,metadata=[MdaElement_tab,MdaQualifier_tab,MdaFreeText_tab],DetAlternateText,RigCreator_tab,RigSource_tab,RigAcknowledgementCredit,RigCopyrightStatement,RigCopyrightStatus,RigLicence,RigLicenceDetails,ChaRepository_tab,ChaMd5Sum,AdmPublishWebNoPassword,AdmDateModified,AdmTimeModified)",
                        "ColCategory",
                        "ColScientificGroup",
                        "ColDiscipline",
                        "ColCollectionName_tab",
                        "ClaPrimaryClassification",
                        "ClaSecondaryClassification",
                        "ClaTertiaryClassification",
                        "ClaObjectName",
                        "ClaObjectSummary",
                        "Con1Description",
                        "SubHistoryTechSignificance",
                        "SubThemes_tab",
                        "SubSubjects_tab",
                        "associations=[AssAssociationType_tab,party=AssAssociationNameRef_tab.(NamPartyType,NamFullName,NamOrganisation,NamBranch,NamDepartment,NamOrganisation,NamOrganisationOtherNames_tab,NamSource,AddPhysStreet,AddPhysCity,AddPhysState,AddPhysCountry,ColCollaborationName),AssAssociationCountry_tab,AssAssociationState_tab,AssAssociationRegion_tab,AssAssociationLocality_tab,AssAssociationStreetAddress_tab,AssAssociationDate_tab,AssAssociationComments0]",
                        "relateditemspecimens=ColRelatedRecordsRef_tab.(irn,MdaDataSets_tab)",
                        "attacheditemspecimens=ColPhysicallyAttachedToRef.(irn,MdaDataSets_tab)",
                        "parentitemspecimens=ColParentRecordRef.(irn,MdaDataSets_tab)",
                        "accession=AccAccessionLotRef.(AcqAcquisitionMethod,AcqDateReceived,AcqDateOwnership,AcqCreditLine,source=[name=AcqSourceRef_tab.(NamPartyType,NamFullName,NamOrganisation,NamBranch,NamDepartment,NamOrganisation,NamOrganisationOtherNames_tab,NamSource,AddPhysStreet,AddPhysCity,AddPhysState,AddPhysCountry,ColCollaborationName),AcqSourceRole_tab],AdmPublishWebNoPassword)",
                        "RigText0",
                        "location=LocCurrentLocationRef.(LocLocationType,location=LocHolderLocationRef.(LocLocationType,location=LocHolderLocationRef.(LocLocationType,location=LocHolderLocationRef.(LocLocationType,location=LocHolderLocationRef.(LocLocationType,location=LocHolderLocationRef.(LocLocationType,LocLevel1,LocLevel2,LocLevel3,LocLevel4),LocLevel1,LocLevel2,LocLevel3,LocLevel4),LocLevel1,LocLevel2,LocLevel3,LocLevel4),LocLevel1,LocLevel2,LocLevel3,LocLevel4),LocLevel1,LocLevel2,LocLevel3,LocLevel4),LocLevel1,LocLevel2,LocLevel3,LocLevel4)",
                        "LocDateCollectedFrom",
                        "LocDateCollectedTo",
                        "LocSamplingMethod",
                        "MinSpecies",
                        "MinVariety",
                        "MinGroup",
                        "MinClass",
                        "MinAssociatedMatrix",
                        "MinType",
                        "MinTypeType",
                        "MetName",
                        "MetClass",
                        "MetGroup",
                        "MetType",
                        "MetMainMineralsPresent",
                        "MetSpecimenWeight",
                        "MetTotalWeight",
                        "MetDateSpecimenFell",
                        "MetDateSpecimenFound",
                        "TekName",
                        "TekClassification",
                        "TekShape",
                        "TekLocalStrewnfield",
                        "TekGlobalStrewnfield",
                        "RocClass",
                        "RocGroup",
                        "RocRockName",
                        "RocRockDescription",
                        "RocMainMineralsPresent",
                        "relatedarticlespecies=<enarratives:ObjObjectsRef_tab>.(irn,DetPurpose_tab)",
                        "relatedpartyarticles=AssAssociationNameRef_tab.(relatedarticles=<enarratives:ParPartiesRef_tab>.(irn,DetPurpose_tab))",
                        "relatedsitearticles=ArcSiteNameRef.(relatedarticles=<enarratives:SitSitesRef_tab>.(irn,DetPurpose_tab))",
                        "relatedcolleventarticles=ColCollectionEventRef.(relatedarticles=<enarratives:ColCollectionEventsRef_tab>.(irn,DetPurpose_tab))"
                    };
            }
        }

        public Terms Terms
        {
            get
            {
                var terms = new Terms();

                terms.Add("MdaDataSets_tab", Constants.ImuSpecimenQueryString);

                return terms;
            }
        }

        public Specimen MakeDocument(Map map)
        {
            var specimen = new Specimen();

            specimen.Id = "specimens/" + map.GetEncodedString("irn");

            specimen.IsHidden = string.Equals(map.GetEncodedString("AdmPublishWebNoPassword"), "no", StringComparison.OrdinalIgnoreCase);
            
            specimen.DateModified = DateTime.ParseExact(
                string.Format("{0} {1}", map.GetEncodedString("AdmDateModified"), map.GetEncodedString("AdmTimeModified")),
                "dd/MM/yyyy HH:mm",
                new CultureInfo("en-AU")).ToUniversalTime();

            specimen.Category = map.GetEncodedString("ColCategory");
            specimen.ScientificGroup = map.GetEncodedString("ColScientificGroup");
            specimen.Discipline = map.GetEncodedString("ColDiscipline");
            specimen.RegistrationNumber = map["ColRegPart"] != null
                             ? string.Format("{0} {1}.{2}", map["ColRegPrefix"], map["ColRegNumber"], map["ColRegPart"])
                             : string.Format("{0} {1}", map["ColRegPrefix"], map["ColRegNumber"]);
            specimen.CollectionNames = map.GetEncodedStrings("ColCollectionName_tab");
            specimen.Type = map.GetEncodedString("ColTypeOfItem");

            // Classifications
            if (map.GetEncodedString("ClaPrimaryClassification") != null && !map.GetEncodedString("ClaPrimaryClassification").Contains("to be classified", StringComparison.OrdinalIgnoreCase))
                specimen.Classifications.Add(map.GetEncodedString("ClaPrimaryClassification").ToSentenceCase());
            if (map.GetEncodedString("ClaSecondaryClassification") != null && !map.GetEncodedString("ClaSecondaryClassification").Contains("to be classified", StringComparison.OrdinalIgnoreCase))
                specimen.Classifications.Add(map.GetEncodedString("ClaSecondaryClassification").ToSentenceCase());
            if (map.GetEncodedString("ClaTertiaryClassification") != null && !map.GetEncodedString("ClaTertiaryClassification").Contains("to be classified", StringComparison.OrdinalIgnoreCase))
                specimen.Classifications.Add(map.GetEncodedString("ClaTertiaryClassification").ToSentenceCase());

            specimen.ObjectName = map.GetEncodedString("ClaObjectName");
            specimen.ObjectSummary = map.GetEncodedString("ClaObjectSummary");
            specimen.IsdDescriptionOfContent = map.GetEncodedString("Con1Description");
            specimen.Significance = map.GetEncodedString("SubHistoryTechSignificance");

            // Tags
            specimen.Keywords.AddRange(map.GetEncodedStrings("SubSubjects_tab"));

            // Collection areas (remove problematic characters used for multi-select facets)
            specimen.CollectingAreas = map.GetEncodedStrings("SubThemes_tab")
                .Select(x => x.CleanForMultiFacets())
                .ToList();

            // Associations
            specimen.Associations = _associationFactory.Make(map.GetMaps("associations"));

            // Acquisition information
            // TODO: make factory method as code duplicated in ItemFactory
            var accessionMap = map.GetMap("accession");
            if (accessionMap != null &&
                string.Equals(accessionMap.GetEncodedString("AdmPublishWebNoPassword"), "yes", StringComparison.OrdinalIgnoreCase))
            {
                var method = accessionMap.GetEncodedString("AcqAcquisitionMethod");

                if (!string.IsNullOrWhiteSpace(method))
                {
                    var sources = accessionMap.GetMaps("source")
                    .Where(x => string.IsNullOrWhiteSpace(x.GetEncodedString("AcqSourceRole_tab")) ||
                        (!x.GetEncodedString("AcqSourceRole_tab").Contains("confindential", StringComparison.OrdinalIgnoreCase) &&
                         !x.GetEncodedString("AcqSourceRole_tab").Contains("contact", StringComparison.OrdinalIgnoreCase) &&
                         !x.GetEncodedString("AcqSourceRole_tab").Contains("vendor", StringComparison.OrdinalIgnoreCase)))
                    .Select(x => _partiesNameFactory.Make(x.GetMap("name"))).ToList();

                    if (sources.Any())
                    {
                        if (!string.IsNullOrWhiteSpace(accessionMap.GetEncodedString("AcqDateReceived")))
                            sources.Add(accessionMap.GetEncodedString("AcqDateReceived"));
                        else if (!string.IsNullOrWhiteSpace(accessionMap.GetEncodedString("AcqDateOwnership")))
                            sources.Add(accessionMap.GetEncodedString("AcqDateOwnership"));

                        specimen.AcquisitionInformation = string.Format("{0} from {1}", method, sources.Concatenate(", "));
                    }
                    else
                    {
                        specimen.AcquisitionInformation = method;
                    }
                }

                var rights = map.GetEncodedStrings("RigText0").FirstOrDefault();

                if (!string.IsNullOrWhiteSpace(accessionMap.GetEncodedString("AcqCreditLine")))
                    specimen.Acknowledgement = accessionMap.GetEncodedString("AcqCreditLine");
                else if (!string.IsNullOrWhiteSpace(rights))
                    specimen.Acknowledgement = rights;
            }

            // Object Location
            specimen.MuseumLocation = _museumLocationFactory.Make(map.GetMap("location"));

            // Number Of Specimens
            if(!string.IsNullOrWhiteSpace(map.GetEncodedString("SpeNoSpecimens")) && map.GetEncodedString("SpeNoSpecimens") != "0" )
                specimen.NumberOfSpecimens = map.GetEncodedString("SpeNoSpecimens");

            // Clutch Size
            specimen.ClutchSize = map.GetEncodedString("BirTotalClutchSize");
            // Sex
            specimen.Sex = map.GetEncodedStrings("SpeSex_tab").Concatenate(", ");
            // Stage Or Age
            specimen.StageOrAge = map.GetEncodedStrings("SpeStageAge_tab").Concatenate(", ");
            // Storages
            specimen.Storages.AddRange(
                map.GetMaps("storage")
                    .Select(x => new Storage
                    {
                        Nature = x.GetEncodedString("StrSpecimenNature_tab"),
                        Form = x.GetEncodedString("StrSpecimenForm_tab"),
                        FixativeTreatment = x.GetEncodedString("StrFixativeTreatment_tab"),
                        Medium = x.GetEncodedString("StrStorageMedium_tab")
                    })
                    .Where(x => x != null));

            // Taxonomy
            // TODO: make factory method as code duplicated in ItemFactory
            var identificationMap = map.GetMaps("identifications").FirstOrDefault(x => Constants.TaxonomyTypeStatuses.Contains(x.GetEncodedString("IdeTypeStatus_tab"), StringComparison.OrdinalIgnoreCase)) ??
                                    map.GetMaps("identifications").FirstOrDefault(x => string.Equals(x.GetEncodedString("IdeCurrentNameLocal_tab"), "yes", StringComparison.OrdinalIgnoreCase)) ??
                                    map.GetMaps("identifications").FirstOrDefault();
            if (identificationMap != null)
            {
                // Type Status
                specimen.TypeStatus = identificationMap.GetEncodedString("IdeTypeStatus_tab");
                // Identified By
                if (identificationMap.GetMaps("identifiers") != null)
                {
                    specimen.IdentifiedBy = identificationMap.GetMaps("identifiers").Where(x => x != null).Select(x => _partiesNameFactory.Make(x)).Concatenate("; ");
                }
                // Date Identified
                specimen.DateIdentified = identificationMap.GetEncodedString("IdeDateIdentified0");
                
                // Identification Qualifier and Rank
                specimen.Qualifier = identificationMap.GetEncodedString("IdeQualifier_tab");
                if (string.Equals(identificationMap.GetEncodedString("IdeQualifierRank_tab"), "Genus", StringComparison.OrdinalIgnoreCase))
                    specimen.QualifierRank = QualifierRankType.Genus;
                else if (string.Equals(identificationMap.GetEncodedString("IdeQualifierRank_tab"), "species", StringComparison.OrdinalIgnoreCase))
                    specimen.QualifierRank = QualifierRankType.Species;

                // Taxonomy
                var taxonomyMap = identificationMap.GetMap("taxa");
                specimen.Taxonomy = _taxonomyFactory.Make(taxonomyMap);

                if (taxonomyMap != null)
                {
                    // Species profile Relationship
                    var relatedSpeciesMaps = taxonomyMap.GetMaps("relatedspecies");
                    specimen.RelatedSpeciesIds.AddRange(relatedSpeciesMaps
                        .Where(x => x != null && x.GetEncodedStrings("DetPurpose_tab").Contains(Constants.ImuSpeciesQueryString))
                        .Select(x => string.Format("species/{0}", x.GetEncodedString("irn"))));
                }
            }

            // Collection Event
            var collectionEventMap = map.GetMap("colevent");
            specimen.CollectionEvent = _collectionEventFactory.Make(collectionEventMap);

            // Sites
            var collectionSiteMap = map.GetMap("site");
            if (collectionSiteMap == null && collectionEventMap != null)
                collectionSiteMap = collectionEventMap.GetMap("site");

            specimen.CollectionSite = _collectionSiteFactory.Make(collectionSiteMap, specimen.Discipline, specimen.ScientificGroup);
                
            // Discipline specific fields
            // Palaeontology
            specimen.PalaeontologyDateCollectedFrom = map.GetEncodedString("LocDateCollectedFrom");
            specimen.PalaeontologyDateCollectedTo = map.GetEncodedString("LocDateCollectedTo");
            
            // Geology
            specimen.MineralogySpecies = map.GetEncodedString("MinSpecies");
            specimen.MineralogyVariety = map.GetEncodedString("MinVariety");
            specimen.MineralogyGroup = map.GetEncodedString("MinGroup");
            specimen.MineralogyClass = map.GetEncodedString("MinClass");
            specimen.MineralogyAssociatedMatrix = map.GetEncodedString("MinAssociatedMatrix");
            specimen.MineralogyType = map.GetEncodedString("MinType");
            specimen.MineralogyTypeOfType = map.GetEncodedString("MinTypeType");
            
            specimen.MeteoritesName = map.GetEncodedString("MetName");
            specimen.MeteoritesClass = map.GetEncodedString("MetClass");
            specimen.MeteoritesGroup = map.GetEncodedString("MetGroup");
            specimen.MeteoritesType = map.GetEncodedString("MetType");
            specimen.MeteoritesMinerals = map.GetEncodedString("MetMainMineralsPresent");
            specimen.MeteoritesSpecimenWeight = map.GetEncodedString("MetSpecimenWeight");
            specimen.MeteoritesTotalWeight = map.GetEncodedString("MetTotalWeight");
            specimen.MeteoritesDateFell = map.GetEncodedString("MetDateSpecimenFell");
            specimen.MeteoritesDateFound = map.GetEncodedString("MetDateSpecimenFound");
            
            specimen.TektitesName = map.GetEncodedString("TekName");
            specimen.TektitesClassification = map.GetEncodedString("TekClassification");
            specimen.TektitesShape = map.GetEncodedString("TekShape");
            specimen.TektitesLocalStrewnfield = map.GetEncodedString("TekLocalStrewnfield");
            specimen.TektitesGlobalStrewnfield = map.GetEncodedString("TekGlobalStrewnfield");

            specimen.PetrologyRockClass = map.GetEncodedString("RocClass");
            specimen.PetrologyRockGroup = map.GetEncodedString("RocGroup");
            specimen.PetrologyRockName = map.GetEncodedString("RocRockName");
            specimen.PetrologyRockDescription = map.GetEncodedString("RocRockDescription");
            specimen.PetrologyMineralsPresent = map.GetEncodedString("RocMainMineralsPresent");

            // Media
            specimen.Media = _mediaFactory.Make(map.GetMaps("media"));

            // Assign thumbnail
            var media = specimen.Media.OfType<IHasThumbnail>().FirstOrDefault();
            if (media != null)
                specimen.ThumbnailUri = media.Thumbnail.Uri;

            // Licence
            specimen.Licence = _licenceFactory.MakeSpecimenLicence(map);

            // Relationships

            // Related items/specimens (directly related)
            foreach (var relatedItemSpecimen in map.GetMaps("relateditemspecimens").Where(x => x != null && !string.IsNullOrWhiteSpace(x.GetEncodedString("irn"))))
            {
                if (relatedItemSpecimen.GetEncodedStrings("MdaDataSets_tab").Contains(Constants.ImuItemQueryString))
                    specimen.RelatedItemIds.Add(string.Format("items/{0}", relatedItemSpecimen.GetEncodedString("irn")));
                if (relatedItemSpecimen.GetEncodedStrings("MdaDataSets_tab").Contains(Constants.ImuSpecimenQueryString))
                    specimen.RelatedSpecimenIds.Add(string.Format("specimens/{0}", relatedItemSpecimen.GetEncodedString("irn")));
            }
            // Physically attached
            var attachedItemSpecimenMap = map.GetMap("attacheditemspecimens");
            if (attachedItemSpecimenMap != null)
            {
                if (attachedItemSpecimenMap.GetEncodedStrings("MdaDataSets_tab").Contains(Constants.ImuItemQueryString))
                    specimen.RelatedItemIds.Add(string.Format("items/{0}", attachedItemSpecimenMap.GetEncodedString("irn")));
                if (attachedItemSpecimenMap.GetEncodedStrings("MdaDataSets_tab").Contains(Constants.ImuSpecimenQueryString))
                    specimen.RelatedSpecimenIds.Add(string.Format("specimens/{0}", attachedItemSpecimenMap.GetEncodedString("irn")));
            }
            // Parent record
            var parentItemSpecimenMap = map.GetMap("parentitemspecimens");
            if (parentItemSpecimenMap != null)
            {
                if (parentItemSpecimenMap.GetEncodedStrings("MdaDataSets_tab").Contains(Constants.ImuItemQueryString))
                    specimen.RelatedItemIds.Add(string.Format("items/{0}", parentItemSpecimenMap.GetEncodedString("irn")));
                if (parentItemSpecimenMap.GetEncodedStrings("MdaDataSets_tab").Contains(Constants.ImuSpecimenQueryString))
                    specimen.RelatedSpecimenIds.Add(string.Format("specimens/{0}", parentItemSpecimenMap.GetEncodedString("irn")));
            }

            // Related articles/species (direct attached)
            var relatedArticleSpeciesMap = map.GetMaps("relatedarticlespecies");
            if (relatedArticleSpeciesMap != null)
            {
                specimen.RelatedArticleIds.AddRangeUnique(relatedArticleSpeciesMap
                    .Where(x => x != null && x.GetEncodedStrings("DetPurpose_tab").Contains(Constants.ImuArticleQueryString))
                    .Select(x => string.Format("articles/{0}", x.GetEncodedString("irn"))));

                specimen.RelatedSpeciesIds.AddRangeUnique(relatedArticleSpeciesMap
                    .Where(x => x != null && x.GetEncodedStrings("DetPurpose_tab").Contains(Constants.ImuSpeciesQueryString))
                    .Select(x => string.Format("species/{0}", x.GetEncodedString("irn"))));
            }

            // Related articles (via party relationship)
            var relatedPartyArticlesMap = map.GetMaps("relatedpartyarticles");
            if (relatedPartyArticlesMap != null)
            {
                specimen.RelatedArticleIds.AddRangeUnique(relatedPartyArticlesMap
                        .Where(x => x != null)
                        .SelectMany(x => x.GetMaps("relatedarticles"))
                        .Where(x => x != null && x.GetEncodedStrings("DetPurpose_tab").Contains(Constants.ImuArticleQueryString))
                        .Select(x => string.Format("articles/{0}", x.GetEncodedString("irn"))));
            }

            // Related articles (via sites relationship)
            var relatedSiteArticlesMap = map.GetMap("relatedsitearticles");
            if (relatedSiteArticlesMap != null)
            {
                specimen.RelatedArticleIds.AddRangeUnique(relatedSiteArticlesMap
                        .GetMaps("relatedarticles")
                        .Where(x => x != null && x.GetEncodedStrings("DetPurpose_tab").Contains(Constants.ImuArticleQueryString))
                        .Select(x => string.Format("articles/{0}", x.GetEncodedString("irn"))));
            }

            // Related articles (via collection event relationship)
            var relatedCollectionEventArticlesMap = map.GetMap("relatedcolleventarticles");
            if (relatedCollectionEventArticlesMap != null)
            {
                specimen.RelatedArticleIds.AddRangeUnique(relatedCollectionEventArticlesMap
                        .GetMaps("relatedarticles")
                        .Where(x => x != null && x.GetEncodedStrings("DetPurpose_tab").Contains(Constants.ImuArticleQueryString))
                        .Select(x => string.Format("articles/{0}", x.GetEncodedString("irn"))));
            }

            // Build summary
            specimen.Summary = _summaryFactory.Make(specimen);

            // Display Title 
            // TODO: Move to display title factory and encapsulate entire process
            if (string.Equals(specimen.Discipline, "Tektites", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(specimen.TektitesName))
                specimen.DisplayTitle = specimen.TektitesName;
            else if (string.Equals(specimen.Discipline, "Meteorites", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(specimen.MeteoritesName))
                specimen.DisplayTitle = string.Format("{0} meteorite", specimen.MeteoritesName);
            else if (string.Equals(specimen.Discipline, "Petrology", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(specimen.PetrologyRockName))
                specimen.DisplayTitle = specimen.PetrologyRockName;
            else if (string.Equals(specimen.Discipline, "Mineralogy", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(specimen.MineralogySpecies))
                specimen.DisplayTitle = specimen.MineralogySpecies;
            else if (specimen.Taxonomy != null)
            {
                var scientificName = _taxonomyFactory.MakeScientificName(specimen.QualifierRank, specimen.Qualifier, specimen.Taxonomy);

                if (!string.IsNullOrWhiteSpace(scientificName))
                    specimen.DisplayTitle = scientificName;
            }

            specimen.DisplayTitle = new[]
            {
                specimen.ObjectName,
                specimen.DisplayTitle
            }.Concatenate(" ");

            if (string.IsNullOrWhiteSpace(specimen.DisplayTitle))
                specimen.DisplayTitle = "Specimen";

            Log.Logger.Debug("Completed {Id} creation with {MediaCount} media", specimen.Id, specimen.Media.Count);
            
            return specimen;
        }

        public void UpdateDocument(Specimen newDocument, Specimen existingDocument, IDocumentSession documentSession)
        {
            // Map over existing document
            Mapper.Map(newDocument, existingDocument);
        }
    }
}