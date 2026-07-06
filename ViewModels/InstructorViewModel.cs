namespace DepiLms.ViewModels
{
    public class InstructorViewModel
    {
        public string Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Department { get; set; }
        public string? Bio { get; set; }
        public string? ProfilePhotoUrl { get; set; }
        public string? LevelOrTitle { get; set; }

        public string? OfficeHours { get; set; }

        public string AvatarColor { get; set; } = "#2563eb";

        public bool IsApproved { get; set; }
    }
}
