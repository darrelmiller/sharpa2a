namespace DomFactory
{
    /// <summary>
    /// An abstract class that contains information about the validation rule but not the 
    /// implementation of the validation rule.
    /// </summary>
    public abstract class ValidationRule
    {
        public required int Id { get; init; }

        public string Message { get; init; } = "A validation rule failed.";

        public string Path { get; set; } = string.Empty;

        public Severity Severity { get; init; } = Severity.Error;
        internal abstract Type ElementType { get; }
    }

    /// <summary>
    /// A abstract validation rule that targets a specific type of object and exposes
    /// the Validate method for performing the validation.
    /// </summary>
    /// <typeparam name="TTarget"></typeparam>
    public class TypedValidationRule<TTarget> : ValidationRule
    {
        public virtual IEnumerable<Problem> Validate(TTarget item) { return []; }
        internal override Type ElementType => typeof(TTarget);
    }
}


