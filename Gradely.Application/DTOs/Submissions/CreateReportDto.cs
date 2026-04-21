namespace Gradely.Application.DTOs.Submissions
{
    public class CreateReportDto
    {
        public int Grade { get; set; }
        public string Feedback { get; set; } = string.Empty;
        public List<MistakeItemDto> Mistakes { get; set; } = new();
    }

    public class MistakeItemDto
    {
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Line { get; set; }
    }
}
