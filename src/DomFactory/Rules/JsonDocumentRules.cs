namespace DomFactory
{
    public static class JsonDocumentRules
    {
        public static class RuleIds
        {
            public const int InvalidJsonSyntax = 10000;
            public const int InvalidJsonSemantics = 10001;
            public const int UnrecognizedMember = 10002;
            public const int DocumentSizeExceedsLimit = 10003; // referenced OpenAPI document shouldn't exceed 100KB in size
        }

    }

}

