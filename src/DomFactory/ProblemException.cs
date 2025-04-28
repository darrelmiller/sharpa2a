namespace DomFactory
{
    public class ProblemException : Exception
    {
        public ProblemException(Problem problem) : base(problem.Message)
        {
            Problem = problem;
        }

        public Problem Problem { get; }
    }
}
