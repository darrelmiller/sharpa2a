using System.Collections;

namespace DomFactory
{
    public class VersionsList : IEnumerable<string>
    {
        private readonly List<string> _defaultVersions = [];

        public VersionsList(string[] initialVersions)
        {
            if (initialVersions.Length != 0)
            {
                _defaultVersions = initialVersions.ToList();
            }

        }

        public IEnumerator<string> GetEnumerator()
        {
            return _defaultVersions.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void AddVersion(string version)
        {
            _defaultVersions.Add(version);
        }
    }
}
