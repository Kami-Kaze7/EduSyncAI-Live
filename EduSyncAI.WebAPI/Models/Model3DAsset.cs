using System;
using System.ComponentModel.DataAnnotations;

namespace EduSyncAI.WebAPI.Models
{
    public class Model3DAsset
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Title { get; set; }

        public string Description { get; set; }

        [Required]
        public string Discipline { get; set; } // e.g., Architecture, Biology, Engineering, Geology, Chemistry, Physics

        [Required]
        public string ModelUrl { get; set; }

        public string ThumbnailUrl { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    }
}
