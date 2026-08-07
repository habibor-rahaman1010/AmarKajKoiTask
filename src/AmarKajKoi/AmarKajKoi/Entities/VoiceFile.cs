namespace AmarKajKoi.Entities
{
    public class VoiceFile
    {
        public Guid VoiceFileId { get; set; }
        public Guid? TaskId { get; set; }
        public Guid UploadedByUserId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string StoragePath { get; set; } = string.Empty;
        public int DurationSecs { get; set; }
        public string Purpose { get; set; } = string.Empty;
        public string? TranscribedText { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
