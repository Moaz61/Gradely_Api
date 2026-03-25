namespace Gradely.Application.DTOs.Submissions
{
    /// <summary>
    /// Response DTO for returning the ML-generated grading report to the student.
    /// 
    /// WHEN IS THIS RETURNED?
    ///   When the student calls GET /api/submissions/{id}/report.
    ///   If the submission hasn't been graded yet, the endpoint returns 404 "No report yet".
    /// 
    /// WHY Mistakes IS A LIST AND NOT A JSON STRING?
    ///   The entity (Report) stores mistakes as a JSON string (MistakesJson)
    ///   because it's simpler for the database.
    ///   But the client wants a proper array, not a raw JSON string.
    ///   So in the service, we deserialize MistakesJson → List<MistakeDto>.
    ///   This is another key benefit of DTOs — transforming data for the client.
    /// 
    /// EXAMPLE JSON returned to the client:
    ///   {
    ///     "grade": 87,
    ///     "feedback": "Good work overall. Strong thesis statement.",
    ///     "mistakes": [
    ///       { "type": "grammar", "description": "Subject-verb agreement", "line": 3 },
    ///       { "type": "spelling", "description": "Misspelled 'receive'", "line": 7 }
    ///     ],
    ///     "createdAt": "2026-03-24T16:00:00Z"
    ///   }
    /// </summary>
    public class ReportDto
    {
        /// <summary>The numerical grade (e.g. 87 out of 100).</summary>
        public int Grade { get; set; }

        /// <summary>Overall text feedback from the ML system.</summary>
        public string Feedback { get; set; } = string.Empty;

        /// <summary>
        /// Detailed list of mistakes found by the ML system.
        /// Deserialized from the JSON string stored in the database.
        /// </summary>
        public List<MistakeDto> Mistakes { get; set; } = new();

        /// <summary>When the report was generated (UTC).</summary>
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Represents a single mistake identified by the ML system.
    /// 
    /// This is a nested DTO — it only exists as part of ReportDto.
    /// In the database, these are stored as a JSON array inside Report.MistakesJson.
    /// We deserialize them into this class so the client gets a proper typed array.
    /// 
    /// EXAMPLE:
    ///   { "type": "grammar", "description": "Subject-verb agreement", "line": 3 }
    /// </summary>
    public class MistakeDto
    {
        /// <summary>The category of mistake (e.g. "grammar", "spelling", "structure").</summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>A human-readable description of the mistake.</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>The line number in the student's document where the mistake was found.</summary>
        public int Line { get; set; }
    }
}
