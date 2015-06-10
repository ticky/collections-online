﻿using System;
using System.Collections.Generic;
using CollectionsOnline.Core.Config;
using CollectionsOnline.WebSite.Models;
using Nancy;
using Nancy.ModelBinding;

namespace CollectionsOnline.WebSite.ModelBinders
{
    public class SearchInputModelBinder : IModelBinder
    {
        public object Bind(NancyContext context, Type modelType, object instance, BindingConfig configuration, params string[] blackList)
        {
            var searchInputModel = new SearchInputModel();
            var query = context.Request.Query;

            // Bind and normalize the regular stuff
            if (query.Page.HasValue)
                searchInputModel.Page = query.Page;
            if (searchInputModel.Page <= 0)
                searchInputModel.Page = 1;

            if (query.PerPage.HasValue)
                searchInputModel.PerPage = query.PerPage;
            if (searchInputModel.PerPage != Constants.PagingPerPageDefault && searchInputModel.PerPage != Constants.PagingPerPageMax)
                searchInputModel.PerPage = Constants.PagingPerPageDefault;

            if (string.Equals(query.Sort, "quality", StringComparison.OrdinalIgnoreCase))
                searchInputModel.Sort = "quality";
            else if (string.Equals(query.Sort, "relevance", StringComparison.OrdinalIgnoreCase))
                searchInputModel.Sort = "relevance";

            if (string.Equals(query.View, "grid", StringComparison.OrdinalIgnoreCase))
                searchInputModel.View = "grid";
            else if (string.Equals(query.View, "list", StringComparison.OrdinalIgnoreCase))
                searchInputModel.View = "list";

            searchInputModel.Query = query.Query;

            searchInputModel.CurrentUrl = string.Format("{0}{1}", context.Request.Url.SiteBase, context.Request.Url.Path);
            searchInputModel.CurrentQueryString = context.Request.Url.Query;

            // Facets
            searchInputModel.Facets = new Dictionary<string, string>();
            if(query.RecordType.HasValue)
                searchInputModel.Facets.Add("RecordType", query.RecordType);
            if (query.Category.HasValue)
                searchInputModel.Facets.Add("Category", query.Category);
            if (query.HasImages.HasValue)
                searchInputModel.Facets.Add("HasImages", query.HasImages);
            if (query.ItemType.HasValue)
                searchInputModel.Facets.Add("ItemType", query.ItemType);
            if (query.SpeciesType.HasValue)
                searchInputModel.Facets.Add("SpeciesType", query.SpeciesType);
            if (query.SpeciesEndemicity.HasValue)
                searchInputModel.Facets.Add("SpeciesEndemicity", query.SpeciesEndemicity);
            if (query.SpecimenScientificGroup.HasValue)
                searchInputModel.Facets.Add("SpecimenScientificGroup", query.SpecimenScientificGroup);

            // Multi-select Facets
            searchInputModel.MultiFacets = new Dictionary<string, string[]>();
            if (query.ArticleType.HasValue)
                searchInputModel.MultiFacets.Add("ArticleType", ((string)query.ArticleType.Value).Split(','));
            if (query.CollectingArea.HasValue)
                searchInputModel.MultiFacets.Add("CollectingArea", ((string)query.CollectingArea.Value).Split(','));
            if (query.DisplayLocation.HasValue)
                searchInputModel.MultiFacets.Add("DisplayLocation", ((string)query.DisplayLocation.Value).Split(','));

            // Terms
            searchInputModel.Terms = new Dictionary<string, string>();
            if (query.Keyword.HasValue)
                searchInputModel.Terms.Add("Keyword", query.Keyword);
            if (query.Locality.HasValue)
                searchInputModel.Terms.Add("Locality", query.Locality);
            if (query.Collection.HasValue)
                searchInputModel.Terms.Add("Collection", query.Collection);
            if (query.Date.HasValue)
                searchInputModel.Terms.Add("Date", query.Date);
            if (query.CulturalGroup.HasValue)
                searchInputModel.Terms.Add("CulturalGroup", query.CulturalGroup);
            if (query.Classification.HasValue)
                searchInputModel.Terms.Add("Classification", query.Classification);
            if (query.Name.HasValue)
                searchInputModel.Terms.Add("Name", query.Name);
            if (query.Technique.HasValue)
                searchInputModel.Terms.Add("Technique", query.Technique);
            if (query.Denomination.HasValue)
                searchInputModel.Terms.Add("Denomination", query.Denomination);
            if (query.Habitat.HasValue)
                searchInputModel.Terms.Add("Habitat", query.Habitat);
            if (query.Taxon.HasValue)
                searchInputModel.Terms.Add("Taxon", query.Taxon);
            if (query.TypeStatus.HasValue)
                searchInputModel.Terms.Add("TypeStatus", query.TypeStatus);
            if (query.GeoType.HasValue)
                searchInputModel.Terms.Add("GeoType", query.GeoType);
            if (query.MuseumLocation.HasValue)
                searchInputModel.Terms.Add("MuseumLocation", query.MuseumLocation);
            if (query.Article.HasValue)
                searchInputModel.Terms.Add("Article", query.Article);

            return searchInputModel;
        }

        public bool CanBind(Type modelType)
        {
            return modelType == typeof(SearchInputModel);
        }
    }
}