using Netigent.Utils.FileStoreIO.Helpers;
using Netigent.Utils.FileStoreIO.Models;
using System;
using System.IO;

namespace Netigent.Utils.FileStoreIO.Extensions
{
    internal static class FileStoreItemExtensions
    {
        internal static FileStoreItem? UpdateLocation(this FileStoreItem? value, string newPath, string includeAppPrefix = "")
        {
            if (value == null)
                return null;

            var relativeFile = newPath.ToRelativeFile(includePrefix: includeAppPrefix, useRelativeRoot: string.Empty);
            var relativeFolder = newPath.ToRelativeFolder(includePrefix: includeAppPrefix);
            var extension = Path.GetExtension(newPath);

            return new FileStoreItem
            {
                Created = value.Created,
                MimeType = MimeHelper.GetMimeType(extension),
                Extension = extension,
                Name = Path.GetFileNameWithoutExtension(newPath),
                Description = value.Description,
                FileLocation = value.FileLocation,
                Folder = relativeFolder,
                ExtClientRef = relativeFile,
                FileRef = value.FileRef,
                Data = value.Data,
                Modified = DateTime.UtcNow,
            };
        }
    }
}
