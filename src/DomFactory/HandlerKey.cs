namespace DomFactory
{
    public class HandlerKey
    {
        public string MemberName { get; set; }
        public VersionsList SupportedVersions { get; set; }

        public HandlerKey(string memberName, VersionsList? supportedVersions = null)
        {
            MemberName = memberName;
            SupportedVersions = supportedVersions ?? new(["1.0"]);
        }
    }
}
