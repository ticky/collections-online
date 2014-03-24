﻿using System;
using System.IO;
using CollectionsOnline.Core.Models;
using CollectionsOnline.Import.Factories;
using ImageResizer;
using IMu;
using NLog;

namespace CollectionsOnline.Import.Utilities
{
    public class MediaHelper : IMediaHelper
    {
        private readonly Logger _log = LogManager.GetCurrentClassLogger();
        private readonly Module _module;

        public MediaHelper(Session session)
        {
            _module = new Module("emultimedia", session);
        }

        public bool Save(long irn, FileFormatType fileFormat, ResizeSettings resizeSettings, string derivative = null)
        {
            try
            {
                _module.FindKey(irn);
                var result = _module.Fetch("start", 0, -1, new[] { "resource" }).Rows[0];
                var resource = result.GetMap("resource");
                var fileStream = resource["file"] as FileStream;

                var destPath = PathFactory.GetDestPath(irn, fileFormat, derivative);
                var destPathDir = destPath.Remove(destPath.LastIndexOf('\\') + 1);

                // Create directory
                if (!Directory.Exists(destPathDir))
                {
                    Directory.CreateDirectory(destPathDir);
                }

                // Delete file if it exists as we want to ensure it is overwritten
                if (File.Exists(destPath))
                {
                    File.Delete(destPath);
                }

                // Save file
                if (resizeSettings != null)
                {
                    ImageBuilder.Current.Build(fileStream, destPath, resizeSettings);
                }
                else
                    fileStream.CopyTo(File.Create(destPath));

                return true;
            }
            catch (Exception exception)
            {
                if (exception is IMuException && ((IMuException) exception).ID == "MultimediaResolutionNotFound")
                {
                    // Error is a known issue that will be picked up in subsequent imports once the data is fixed. So we don't need to re-throw exception.
                    _log.Warn("Unable to save image at this time {0}, {1}", irn, exception.ToString());
                }
                else
                {
                    // Error is unexpected therefore we want the entire import to fail, re-throw the error.
                    _log.Error("Error saving image {0}, un-recoverable error, {1}", irn, exception.ToString());
                    throw;
                }
            }

            return false;
        }
    }
}