﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using NuKeeper.Logging;

namespace NuKeeper.RepositoryInspection
{
    public class ProjectFileReader
    {
        private readonly INuKeeperLogger _logger;

        public ProjectFileReader(INuKeeperLogger logger)
        {
            _logger = logger;
        }

        public IEnumerable<PackageInProject> ReadFile(string baseDirectory, string relativePath)
        {
            var packagePath = new PackagePath(baseDirectory, relativePath, PackageReferenceType.ProjectFile);
            using (var fileContents = File.OpenRead(packagePath.FullName))
            {
                return Read(fileContents, packagePath);
            }
        }

        public IEnumerable<PackageInProject> Read(Stream fileContents, PackagePath path)
        {
            var xml = XDocument.Load(fileContents);
            var ns = xml.Root.GetDefaultNamespace();
            var project = xml.Element(ns + "Project");

            if (project == null)
            {
                return Enumerable.Empty<PackageInProject>();
            }

            var itemGroups = project.Elements(ns + "ItemGroup");
            var packageRefs = itemGroups.SelectMany(ig => ig.Elements(ns + "PackageReference"));

            return packageRefs
                .Select(el => XmlToPackage(el, path))
                .Where(el => el != null)
                .ToList();
        }

        private PackageInProject XmlToPackage(XElement el, PackagePath path)
        {
            try
            {
                var id = el.Attribute("Include")?.Value;
                var version = el.Attribute("Version")?.Value ?? el.Value;

                return new PackageInProject(id, version, path);
            }
            catch (Exception ex)
            {
                _logger.Error($"Could not read package from {el} in file {path.FullName}", ex);
                return null;
            }
        }
    }
}
