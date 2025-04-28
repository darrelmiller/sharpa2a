

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace DomFactory
{
    public class UnrecognizedMemberRule : ValidationRule
    {
        public const string UnrecognizedMember = "Unrecognized member '{0}' with value '{1}' in JSON document";

        [SetsRequiredMembers]
        public UnrecognizedMemberRule()
        {
            Id = JsonDocumentRules.RuleIds.UnrecognizedMember;
            Message = UnrecognizedMember;
            Severity = Severity.Error;
        }

        internal override Type ElementType => typeof(JsonDocument);
    }
}
