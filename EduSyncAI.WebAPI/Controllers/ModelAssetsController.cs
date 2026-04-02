using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EduSyncAI.WebAPI.Data;
using EduSyncAI.WebAPI.Models;

namespace EduSyncAI.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ModelAssetsController : ControllerBase
    {
        private readonly EduSyncDbContext _context;

        public ModelAssetsController(EduSyncDbContext context)
        {
            _context = context;
        }

        // GET: api/ModelAssets
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Model3DAsset>>> GetModelAssets()
        {
            return await _context.ModelAssets.ToListAsync();
        }

        // GET: api/ModelAssets/discipline/Biology
        [HttpGet("discipline/{discipline}")]
        public async Task<ActionResult<IEnumerable<Model3DAsset>>> GetModelAssetsByDiscipline(string discipline)
        {
            var assets = await _context.ModelAssets
                .Where(m => m.Discipline.ToLower() == discipline.ToLower())
                .ToListAsync();

            return assets;
        }

        // GET: api/ModelAssets/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Model3DAsset>> GetModelAsset(int id)
        {
            var asset = await _context.ModelAssets.FindAsync(id);
            if (asset == null)
            {
                return NotFound();
            }
            return asset;
        }

        // POST: api/ModelAssets (For the NextJS Admin Panel)
        [HttpPost]
        public async Task<ActionResult<Model3DAsset>> PostModelAsset(
            [FromForm] string title,
            [FromForm] string description,
            [FromForm] string discipline,
            Microsoft.AspNetCore.Http.IFormFile modelFile,
            Microsoft.AspNetCore.Http.IFormFile thumbnailFile = null)
        {
            if (modelFile == null || modelFile.Length == 0)
            {
                return BadRequest("A 3D model file (.obj, .stl) is required.");
            }

            var baseUploadsPath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "..", "Data", "uploads");
            var uploadsPath = System.IO.Path.Combine(baseUploadsPath, "models", discipline.ToLower());
            if (!System.IO.Directory.Exists(uploadsPath))
            {
                System.IO.Directory.CreateDirectory(uploadsPath);
            }

            // Save the 3D model file
            var modelExt = System.IO.Path.GetExtension(modelFile.FileName);
            var uniqueModelName = $"{System.Guid.NewGuid()}{modelExt}";
            var modelFilePath = System.IO.Path.Combine(uploadsPath, uniqueModelName);

            using (var stream = new System.IO.FileStream(modelFilePath, System.IO.FileMode.Create))
            {
                await modelFile.CopyToAsync(stream);
            }

            var modelUrl = $"{Request.Scheme}://{Request.Host}/uploads/models/{discipline.ToLower()}/{uniqueModelName}";
            string thumbnailUrl = null;

            // Optional: Save thumbnail
            if (thumbnailFile != null && thumbnailFile.Length > 0)
            {
                var thumbExt = System.IO.Path.GetExtension(thumbnailFile.FileName);
                var uniqueThumbName = $"{System.Guid.NewGuid()}_thumb{thumbExt}";
                var thumbFilePath = System.IO.Path.Combine(uploadsPath, uniqueThumbName);

                using (var stream = new System.IO.FileStream(thumbFilePath, System.IO.FileMode.Create))
                {
                    await thumbnailFile.CopyToAsync(stream);
                }
                thumbnailUrl = $"{Request.Scheme}://{Request.Host}/uploads/models/{discipline.ToLower()}/{uniqueThumbName}";
            }

            var asset = new Model3DAsset
            {
                Title = title,
                Description = description,
                Discipline = discipline,
                ModelUrl = modelUrl,
                ThumbnailUrl = thumbnailUrl ?? ""
            };

            _context.ModelAssets.Add(asset);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetModelAsset), new { id = asset.Id }, asset);
        }
    }
}
