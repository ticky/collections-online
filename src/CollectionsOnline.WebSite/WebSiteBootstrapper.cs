﻿using System;
using AutoMapper;
using CollectionsOnline.Core.Factories;
using CollectionsOnline.Core.Infrastructure;
using CollectionsOnline.Core.Models;
using CollectionsOnline.WebSite.Infrastructure;
using CollectionsOnline.WebSite.Models;
using CollectionsOnline.WebSite.Transformers;
using Nancy;
using Nancy.Bootstrapper;
using Nancy.Bootstrappers.Ninject;
using Nancy.Json;
using Newtonsoft.Json;
using Ninject.Extensions.Conventions;
using Ninject;
using NLog;
using Raven.Client;
using Raven.Client.Indexes;
using StackExchange.Profiling;
using StackExchange.Profiling.RavenDb;

namespace CollectionsOnline.WebSite
{
    public class WebSiteBootstrapper : NinjectNancyBootstrapper 
    {
        private readonly Logger _log = LogManager.GetCurrentClassLogger();

        protected override void ConfigureApplicationContainer(IKernel kernel)
        {
            kernel.Bind<IDocumentStore>().ToProvider<NinjectRavenDocumentStoreProvider>().InSingletonScope();
            kernel.Bind<JsonSerializer>().To<ApiJsonSerializer>();

            var documentStore = kernel.Get<IDocumentStore>();

            // Register view transformers
            IndexCreation.CreateIndexes(typeof(ItemViewTransformer).Assembly, documentStore);

            // Bind ravendb miniprofiler
            MiniProfilerRaven.InitializeFor(documentStore);
        }

        protected override void ConfigureRequestContainer(IKernel kernel, NancyContext context)
        {
            kernel.Bind<IDocumentSession>().ToProvider<NinjectRavenDocumentSessionProvider>();            
            kernel.Bind(x => x
                .FromAssemblyContaining(typeof(WebSiteBootstrapper), typeof(SlugFactory))
                .SelectAllClasses()
                .InNamespaces(new[] { "CollectionsOnline" })
                .BindAllInterfaces());
        }

        protected override void ApplicationStartup(IKernel container, IPipelines pipelines)
        {
            JsonSettings.MaxJsonLength = Int32.MaxValue;

            pipelines.OnError += (ctx, ex) =>
            {
                _log.Error(ex);

                return null;
            };

            pipelines.BeforeRequest += ctx =>
            {
                MiniProfiler.Start();

                return null;
            };

            pipelines.AfterRequest += ctx =>
            {
                MiniProfiler.Stop();
                _log.Trace(MiniProfiler.Current.RenderPlainText().Replace(Environment.NewLine, ""));
            };

            // Automapper configuration
            Mapper.Initialize(cfg =>
            {
                cfg.CreateMap<Article, ArticleApiViewModel>();
                cfg.CreateMap<Item, ItemApiViewModel>();
                cfg.CreateMap<Comment, CommentApiViewModel>();
            });
        }
    }
}