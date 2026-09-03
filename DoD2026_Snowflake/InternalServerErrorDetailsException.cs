namespace DoD2026_Snowflake
{
    public sealed record ProblemDetails
    {
        public required string Title { get; init; }
        public required string Detail { get; init; }
        public int Status { get; init; } = 500;
        public string? TableName { get; init; }
        public string? ColumnName { get; init; }
        public string? ColumnType { get; init; }
    }

    public sealed class InternalServerErrorDetailsException : Exception
    {
        public ProblemDetails ProblemDetails { get; }

        public InternalServerErrorDetailsException(ProblemDetails problemDetails)
            : base(problemDetails.Detail)
        {
            ProblemDetails = problemDetails;
        }
    }
}
