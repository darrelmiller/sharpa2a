
namespace DomFactory
{
    public class ValidationContext
    {
        public string? Version { get; set; }
        public List<Problem> Problems { get; set; } = [];

        public int[] RuleIDs { get; set; } = [];
        public string? NodeName { get; set; } = "#";

        public ValidationContext(string version)
        {
            Version = version;
        }

        public void AddProblem(Problem problem)
        {
            Problems.Add(problem);
        }
    }
}
