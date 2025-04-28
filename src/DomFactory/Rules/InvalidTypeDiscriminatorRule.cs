
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace DomFactory
{

    public class InvalidTypeDiscriminatorRule : ValidationRule
    {
        public const string InvalidTypeDiscriminator = "Invalid identifier '{0}' in '{1}' in {2}. Allowed values are: {3}";

        [SetsRequiredMembers]
        public InvalidTypeDiscriminatorRule()
        {
            Id = JsonDocumentRules.RuleIds.InvalidJsonSyntax;
            Message = InvalidTypeDiscriminator;
            Severity = Severity.Error;
        }

        internal override Type ElementType => typeof(JsonDocument);
    }

}

