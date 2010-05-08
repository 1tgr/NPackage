using System.Collections.Generic;

namespace NPackage.Core
{
    public class Repository
    {
        private readonly List<Package> packages = new List<Package>();

        public IList<Package> Packages
        {
            get { return packages; }
        }
    }
}