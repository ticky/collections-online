﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CollectionsOnline.WebSite.Factories
{
    public class HomeHeroUriFactory : IHomeHeroUriFactory
    {
        private IList<string> _homeHeroUris = new List<string>();

        public HomeHeroUriFactory()
        {
            // Cache home hero images
            var imagesPath = new DirectoryInfo(string.Format("{0}\\content\\img", AppDomain.CurrentDomain.BaseDirectory));
            var baseUri = new Uri(AppDomain.CurrentDomain.BaseDirectory);

            // Return if img directory does not exist
            if (!imagesPath.Exists) return;

            foreach (var image in imagesPath.EnumerateFiles().Where(x => x.Name.Contains("bg-home-hero")))
            {
                _homeHeroUris.Add(string.Format("/{0}", baseUri.MakeRelativeUri(new Uri(image.FullName))));
            }
        }

        public string GetCurrentUri()
        {
            // Return uri based on day of year
            return _homeHeroUris.Any() ? _homeHeroUris[DateTime.UtcNow.DayOfYear % _homeHeroUris.Count] : String.Empty;
        }
    }
}