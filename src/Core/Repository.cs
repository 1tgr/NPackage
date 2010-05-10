using System;
using System.Collections.Generic;

namespace NPackage.Core
{
    public class Repository
    {
        private readonly List<Uri> repositoryImports = new List<Uri>();
        private readonly List<Uri> packageImports = new List<Uri>();
        private readonly List<Package> packages = new List<Package>();

        public IList<Uri> PackageImports
        {
            get { return packageImports; }
        }

        public IList<Uri> RepositoryImports
        {
            get { return repositoryImports; }
        }

        public IList<Package> Packages
        {
            get { return packages; }
        }
    }
}