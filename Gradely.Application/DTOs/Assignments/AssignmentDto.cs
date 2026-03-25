namespace Gradely.Application.DTOs.Assignments
{
    /// <summary>
    /// Response DTO for returning assignment data to the client.
    /// 
    /// WHAT IS A RESPONSE DTO?
    ///   When the client calls GET /api/assignments, we DON'T return the raw
    ///   Assignment entity. Instead, we map it to this DTO which contains
    ///   only the fields the client needs to see.
    /// 
    /// WHY NOT RETURN THE ENTITY DIRECTLY?
    ///   1. The entity has navigation properties (Submissions collection)
    ///      that would cause circular references during JSON serialization
    ///   2. The entity might have internal fields we don't want to expose
    ///   3. Decoupling: if we add internal fields to the entity later,
    ///      the API response stays the same
    ///   4. We control exactly what the client sees
    /// 
    /// EXAMPLE JSON returned to the client:
    ///   {
    ///     "id": "3f2504e0-4f89-11d3-9a0c-0305e82c3301",
    ///     "title": "Essay on Climate Change",
    ///     "description": "Write a 500-word essay about the effects of climate change.",
    ///     "dueDate": "2026-04-15T23:59:59Z",
    ///     "maxGrade": 100,
    ///     "createdAt": "2026-03-20T10:00:00Z"
    ///   }
    /// </summary>
    public class AssignmentDto
    {
        /// <summary>The assignment's unique identifier.</summary>
        public Guid Id { get; set; }

        /// <summary>The assignment title (e.g. "Essay on Climate Change").</summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>Detailed instructions for the assignment (may be null).</summary>
        public string? Description { get; set; }

        /// <summary>When the assignment is due (UTC).</summary>
        public DateTime DueDate { get; set; }

        /// <summary>Maximum possible grade (e.g. 100).</summary>
        public int MaxGrade { get; set; }

        /// <summary>When the assignment was created (UTC).</summary>
        public DateTime CreatedAt { get; set; }
    }
}
