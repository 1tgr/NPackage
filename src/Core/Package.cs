using System.Collections.Generic;

namespace NPackage.Core
{
    public class Package
    {
        private readonly List<string> masterSites = new List<string>();
        private readonly List<string> requires = new List<string>();
        private readonly Dictionary<string, Library> libraries = new Dictionary<string, Library>();

        public string Name { get; set; }
        public string Version { get; set; }
        public string Description { get; set; }
        public string Author { get; set; }
        public string Maintainer { get; set; }

        public IList<string> MasterSites
        {
            get { return masterSites; }
        }

        public IList<string> Requires
        {
            get { return requires; }
        }

        public IDictionary<string, Library> Libraries
        {
            get { return libraries; }
        }
    }
}