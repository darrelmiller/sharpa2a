
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace DomFactory
{

    public class InvalidJsonSyntaxRule : ValidationRule
    {
        public const string InvalidJsonSyntax = "Malformed JSON document, at line {0}, position {1}, with error message: {2}";
        [SetsRequiredMembers]
        public InvalidJsonSyntaxRule()
        {
            Id = JsonDocumentRules.RuleIds.InvalidJsonSyntax;
            Message = InvalidJsonSyntax;
            Severity = Severity.Error;
        }

        internal override Type ElementType => typeof(JsonDocument);
    }

}

