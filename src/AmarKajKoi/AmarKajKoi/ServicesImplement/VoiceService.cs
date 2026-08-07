using AmarKajKoi.Database;
using AmarKajKoi.DataTransferObjects;
using AmarKajKoi.Entities;
using AmarKajKoi.ServicesInterface;

namespace AmarKajKoi.ServicesImplement
{
    public class VoiceService : IVoiceService
    {
        private readonly IUnitOfWork _uow;
        private readonly IWebHostEnvironment _env;

        /// <summary>
        /// Containers the recorder can produce: Opus/WebM on Chromium and Firefox,
        /// AAC/MP4 on Safari and iOS. Anything else is stored as .webm rather than
        /// with the caller's extension, which is client-supplied and reaches disk.
        /// </summary>
        private static readonly HashSet<string> AllowedExtensions =
            new(StringComparer.OrdinalIgnoreCase) { ".webm", ".m4a", ".mp4", ".ogg", ".mp3", ".wav" };

        public VoiceService(IUnitOfWork uow, IWebHostEnvironment env)
        {
            _uow = uow;
            _env = env;
        }

        /// <summary>
        /// wwwroot/UploadedVoiceFiles. WebRootPath is null until the wwwroot folder
        /// exists on disk, so the path is derived from the content root instead of
        /// trusted directly — otherwise the very first upload on a fresh deployment
        /// would throw before it had a chance to create the folder.
        /// </summary>
        private string StorageRoot()
        {
            var webRoot = string.IsNullOrWhiteSpace(_env.WebRootPath)
                ? Path.Combine(_env.ContentRootPath, "wwwroot")
                : _env.WebRootPath;
            return Path.Combine(webRoot, "UploadedVoiceFiles");
        }

        public async Task<VoiceUploadResponse> UploadAsync(Guid userId, string purpose, IFormFile file, int durationSecs)
        {
            if (file == null || file.Length == 0)
                throw new InvalidOperationException("Empty file.");
            if (durationSecs < 3)
                throw new InvalidOperationException("Voice must be at least 3 seconds.");

            var storageRoot = StorageRoot();
            Directory.CreateDirectory(storageRoot);

            var ext = Path.GetExtension(file.FileName);
            if (!AllowedExtensions.Contains(ext)) ext = ".webm";
            var storedName = $"{Guid.NewGuid():N}{ext}";
            var fullPath = Path.Combine(storageRoot, storedName);

            using (var fs = File.Create(fullPath))
            {
                await file.CopyToAsync(fs);
            }

            var v = new VoiceFile
            {
                UploadedByUserId = userId,
                FileName = file.FileName,
                StoragePath = storedName,
                DurationSecs = durationSecs,
                Purpose = purpose
            };

            _uow.Begin();
            try
            {
                v.VoiceFileId = await _uow.VoiceFiles.CreateAsync(v);
                _uow.Commit();
            }
            catch { _uow.Rollback(); throw; }

            return new VoiceUploadResponse
            {
                VoiceFileId = v.VoiceFileId,
                FileName = v.FileName,
                DurationSecs = v.DurationSecs
            };
        }

        public async Task<(byte[] Content, string ContentType, string FileName)?> DownloadAsync(Guid voiceFileId)
        {
            var v = await _uow.VoiceFiles.GetByIdAsync(voiceFileId);
            if (v == null) return null;

            var path = Path.Combine(StorageRoot(), v.StoragePath);
            if (!File.Exists(path)) return null;

            var bytes = await File.ReadAllBytesAsync(path);
            var ext = Path.GetExtension(v.FileName).ToLowerInvariant();
            var contentType = ext switch
            {
                ".mp3" => "audio/mpeg",
                ".wav" => "audio/wav",
                ".ogg" => "audio/ogg",
                ".m4a" => "audio/mp4",
                ".mp4" => "audio/mp4",
                _       => "audio/webm"
            };
            return (bytes, contentType, v.FileName);
        }
    }
}
