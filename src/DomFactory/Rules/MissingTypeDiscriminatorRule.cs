
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace DomFactory
{

    public class MissingTypeDiscriminatorRule : ValidationRule
    {
        public const string MissingTypeDiscriminator = "Missing type discriminator '{0}' in '{1} object.";

        [SetsRequiredMembers]
        public MissingTypeDiscriminatorRule()
        {
            Id = JsonDocumentRules.RuleIds.InvalidJsonSyntax;
            Message = MissingTypeDiscriminator;
            Severity = Severity.Error;
        }

        internal override Type ElementType => typeof(JsonDocument);
    }

}

