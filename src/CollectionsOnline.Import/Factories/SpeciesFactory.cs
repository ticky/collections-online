﻿using System;
using System.Drawing;
using System.Globalization;
using System.Linq;
using AutoMapper;
using CollectionsOnline.Core.Config;
using CollectionsOnline.Core.Extensions;
using CollectionsOnline.Core.Models;
using CollectionsOnline.Import.Utilities;
using ImageResizer;
using IMu;

namespace CollectionsOnline.Import.Factories
{
    public class SpeciesFactory : IEmuAggregateRootFactory<Species>
    {
        private readonly IMediaHelper _mediaHelper;
        private readonly ITaxonomyFactory _taxonomyFactory;

        public SpeciesFactory(
            IMediaHelper mediaHelper,
            ITaxonomyFactory taxonomyFactory)
        {
            _mediaHelper = mediaHelper;
            _taxonomyFactory = taxonomyFactory;
        }

        public string ModuleName
        {
            get { return "enarratives"; }
        }

        public string[] Columns
        {
            get
            {
                return new[]
                {
                    "irn",
                    "AdmPublishWebNoPassword",
                    "AdmDateModified",
                    "AdmTimeModified",
                    "SpeTaxonGroup",
                    "SpeTaxonSubGroup",
                    "SpeColour_tab",
                    "SpeMaximumSize",
                    "SpeUnit",
                    "SpeHabitat_tab",
                    "SpeWhereToLook_tab",
                    "SpeWhenActive_tab",
                    "SpeNationalParks_tab",
                    "SpeDiet",
                    "SpeDietCategories_tab",
                    "SpeFastFact",
                    "SpeHabitatNotes",
                    "SpeDistribution",
                    "SpeBiology",
                    "SpeIdentifyingCharacters",
                    "SpeBriefID",
                    "SpeHazards",
                    "SpeEndemicity",
                    "SpeCommercialSpecies",
                    "conservation=[SpeConservationList_tab,SpeStatus_tab]",
                    "SpeScientificDiagnosis",
                    "SpeWeb",
                    "SpePlant_tab",
                    "SpeFlightStart",
                    "SpeFlightEnd",
                    "SpeDepth_tab",
                    "SpeWaterColumnLocation_tab",
                    "taxa=TaxTaxaRef_tab.(irn,ClaKingdom,ClaPhylum,ClaSubphylum,ClaSuperclass,ClaClass,ClaSubclass,ClaSuperorder,ClaOrder,ClaSuborder,ClaInfraorder,ClaSuperfamily,ClaFamily,ClaSubfamily,ClaGenus,ClaSubgenus,ClaSpecies,ClaSubspecies,AutAuthorString,ClaApplicableCode,comname=[ComName_tab,ComStatus_tab])",
                    "specimens=TaxTaxaRef_tab.(specimens=<ecatalogue:TaxTaxonomyRef_tab>.(irn,sets=MdaDataSets_tab))",
                    "authors=NarAuthorsRef_tab.(NamFullName,BioLabel,media=MulMultiMediaRef_tab.(irn,MulTitle,MulMimeType,MulDescription,MulCreator_tab,MdaDataSets_tab,MdaElement_tab,MdaQualifier_tab,MdaFreeText_tab,ChaRepository_tab,DetAlternateText,AdmPublishWebNoPassword,AdmDateModified,AdmTimeModified))",
                    "media=MulMultiMediaRef_tab.(irn,MulTitle,MulMimeType,MulDescription,MulCreator_tab,MdaDataSets_tab,MdaElement_tab,MdaQualifier_tab,MdaFreeText_tab,ChaRepository_tab,DetAlternateText,AdmPublishWebNoPassword,AdmDateModified,AdmTimeModified)"
                };
            }
        }

        public Terms Terms
        {
            get
            {
                var terms = new Terms();

                terms.Add("DetPurpose_tab", Constants.ImuSpeciesQueryString);

                return terms;
            }
        }

        public Species MakeDocument(Map map)
        {
            var species = new Species();

            species.Id = "species/" + map.GetString("irn");

            species.IsHidden = string.Equals(map.GetString("AdmPublishWebNoPassword"), "no", StringComparison.OrdinalIgnoreCase);

            species.DateModified = DateTime.ParseExact(
                string.Format("{0} {1}", map.GetString("AdmDateModified"), map.GetString("AdmTimeModified")),
                "dd/MM/yyyy HH:mm",
                new CultureInfo("en-AU"));

            species.AnimalType = map.GetString("SpeTaxonGroup");
            species.AnimalSubType = map.GetString("SpeTaxonSubGroup");

            species.Colours = map.GetStrings("SpeColour_tab") ?? new string[] { };
            species.MaximumSize = string.Format("{0} {1}", map.GetString("SpeMaximumSize"), map.GetString("SpeUnit")).Trim();

            species.Habitats = map.GetStrings("SpeHabitat_tab") ?? new string[] { };
            species.WhereToLook = map.GetStrings("SpeWhereToLook_tab") ?? new string[] { };
            species.WhenActive = map.GetStrings("SpeWhereToLook_tab") ?? new string[] { };
            species.NationalParks = map.GetStrings("SpeNationalParks_tab") ?? new string[] { };

            species.Diet = map.GetString("SpeDiet");
            species.DietCategories = map.GetStrings("SpeDietCategories_tab") ?? new string[] { };

            species.FastFact = map.GetString("SpeFastFact");
            species.Habitat = map.GetString("SpeHabitatNotes");
            species.Distribution = map.GetString("SpeDistribution");
            species.Biology = map.GetString("SpeBiology");
            species.IdentifyingCharacters = map.GetString("SpeIdentifyingCharacters");
            species.BriefId = map.GetString("SpeBriefID");
            species.Hazards = map.GetString("SpeHazards");
            species.Endemicity = map.GetString("SpeEndemicity");
            species.Commercial = map.GetString("SpeCommercialSpecies");

            // Get Conservation Status
            foreach (var conservationMap in map.GetMaps("conservation"))
            {
                var authority = conservationMap.GetString("SpeConservationList_tab");
                var status = conservationMap.GetString("SpeStatus_tab");

                species.ConservationStatuses.Add(string.Format("{0} {1}", authority, status));
            }

            species.ScientificDiagnosis = map.GetString("SpeScientificDiagnosis");

            // Animal specific fields (spider/butterflies) 
            species.Web = map.GetString("SpeWeb");
            species.Plants = map.GetStrings("SpePlant_tab") ?? new string[] { };
            species.FlightStart = map.GetString("SpeFlightStart");
            species.FlightEnd = map.GetString("SpeFlightEnd");
            species.Depths = map.GetStrings("SpeDepth_tab") ?? new string[] { };
            species.WaterColumnLocations = map.GetStrings("SpeWaterColumnLocation_tab") ?? new string[] { };

            // Get Taxonomy
            species.Taxonomy = _taxonomyFactory.Make(map.GetMaps("taxa").FirstOrDefault());
            
            // Relationships
            var specimensMap = map
                .GetMaps("specimens")
                .FirstOrDefault();

            if (specimensMap != null)
                species.SpecimenIds = specimensMap
                    .GetMaps("specimens")
                    .Where(x => x != null && x.GetStrings("sets").Contains(Constants.ImuSpecimenQueryString))
                    .Select(x => "specimens/" + x.GetString("irn"))
                    .ToList();

            // Authors
            foreach (var authorMap in map.GetMaps("authors"))
            {
                var author = new Author
                {
                    Name = authorMap.GetString("NamFullName"),
                    Biography = authorMap.GetString("BioLabel")
                };

                var mediaMap = authorMap.GetMaps("media").FirstOrDefault(x => 
                    x != null &&
                    string.Equals(x.GetString("AdmPublishWebNoPassword"), "yes", StringComparison.OrdinalIgnoreCase) &&
                    x.GetStrings("MdaDataSets_tab").Contains(Constants.ImuMultimediaQueryString) &&
                    x.GetString("MulMimeType") == "image");
                
                if (mediaMap != null)
                {
                    var irn = long.Parse(mediaMap.GetString("irn"));

                    var url = PathFactory.MakeUrlPath(irn, FileFormatType.Jpg, "thumb");
                    var thumbResizeSettings = new ResizeSettings
                    {
                        Format = FileFormatType.Jpg.ToString(),
                        Height = 365,
                        Width = 365,
                        Mode = FitMode.Crop,
                        PaddingColor = Color.White,
                        Quality = 65
                    };

                    if (_mediaHelper.Save(irn, FileFormatType.Jpg, thumbResizeSettings, "thumb"))
                    {
                        author.Media = new Media
                        {
                            Irn = irn,
                            DateModified =
                                DateTime.ParseExact(
                                    string.Format("{0} {1}", mediaMap.GetString("AdmDateModified"),
                                                  mediaMap.GetString("AdmTimeModified")), "dd/MM/yyyy HH:mm",
                                    new CultureInfo("en-AU")),
                            Title = mediaMap.GetString("MulTitle"),
                            AlternateText = mediaMap.GetString("DetAlternateText"),
                            Type = mediaMap.GetString("MulMimeType"),
                            Url = url
                        };

                        species.Authors.Add(author);
                    }
                }
            }

            // Media
            // TODO: Be more selective in what media we assign to item and how (change metadataset check)
            foreach (var mediaMap in map.GetMaps("media").Where(x =>
                x != null &&
                string.Equals(x.GetString("AdmPublishWebNoPassword"), "yes", StringComparison.OrdinalIgnoreCase) &&
                x.GetString("MulMimeType") == "image" && 
                x.GetStrings("MdaDataSets_tab").Any(y => y == "App - Field Guide")))
            {
                var irn = long.Parse(mediaMap.GetString("irn"));

                var url = PathFactory.MakeUrlPath(irn, FileFormatType.Jpg, "thumb");
                var thumbResizeSettings = new ResizeSettings
                {
                    Format = FileFormatType.Jpg.ToString(),
                    Height = 365,
                    Width = 365,
                    Mode = FitMode.Crop,
                    PaddingColor = Color.White,
                    Quality = 65
                };

                if (_mediaHelper.Save(irn, FileFormatType.Jpg, thumbResizeSettings, "thumb"))
                {
                    species.Media.Add(new Media
                    {
                        Irn = irn,
                        DateModified = DateTime.ParseExact(string.Format("{0} {1}", mediaMap.GetString("AdmDateModified"),
                            mediaMap.GetString("AdmTimeModified")),
                            "dd/MM/yyyy HH:mm", new CultureInfo("en-AU")),
                        Title = mediaMap.GetString("MulTitle"),
                        AlternateText = mediaMap.GetString("DetAlternateText"),
                        Type = mediaMap.GetString("MulMimeType"),
                        Url = url
                    });
                }
            }

            // Build summary
            if (!string.IsNullOrWhiteSpace(species.IdentifyingCharacters))
                species.Summary = species.IdentifyingCharacters;
            else if (!string.IsNullOrWhiteSpace(species.Biology))
                species.Summary = species.Biology;
            
            return species;
        }

        public void RegisterAutoMapperMap()
        {
            Mapper.CreateMap<Species, Species>()
                .ForMember(x => x.Id, options => options.Ignore());
        }
    }
}