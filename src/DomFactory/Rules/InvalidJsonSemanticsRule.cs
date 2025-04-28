
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace DomFactory
{

    public class InvalidJsonSemanticsRule : ValidationRule
    {
        public const string InvalidJsonSemantics = "Invalid value '{1}' for member '{0}' in JSON document. Reason: {2}";

        [SetsRequiredMembers]
        public InvalidJsonSemanticsRule()
        {
            Id = JsonDocumentRules.RuleIds.InvalidJsonSemantics;
            Message = InvalidJsonSemantics;
            Severity = Severity.Error;
        }

        internal override Type ElementType => typeof(JsonDocument);
    }

}

