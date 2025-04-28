namespace DomFactory
{
    public class Problem
    {
        public required ValidationRule Rule { get; init; }
        public int RuleId { get => Rule.Id; }
        public string Message { get => Rule.Message; }
        public Severity Severity { get => Rule.Severity; }
        public object?[] ProblemValues { get; init; } = [];
        public string? Path { get; init; }

        public override string ToString()
        {
            return string.Format(Message, ProblemValues);
        }
    }
}
