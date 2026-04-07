using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EduSyncAI.WebAPI.Data;
using EduSyncAI.WebAPI.Models;
using System.Text.Json;

namespace EduSyncAI.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AcademicHierarchyController : ControllerBase
    {
        private readonly EduSyncDbContext _context;

        public AcademicHierarchyController(EduSyncDbContext context)
        {
            _context = context;
        }

        // ================= FACULTIES =================

        [HttpGet("faculties")]
        public async Task<ActionResult<IEnumerable<Faculty>>> GetFaculties()
        {
            return await _context.Faculties.ToListAsync();
        }

        [HttpPost("faculties")]
        public async Task<ActionResult<Faculty>> CreateFaculty([FromBody] Faculty body)
        {
            var faculty = new Faculty
            {
                Name = body.Name,
                CreatedAt = DateTime.UtcNow
            };
            
            _context.Faculties.Add(faculty);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetFaculties), new { id = faculty.Id }, faculty);
        }

        // ================= DEPARTMENTS =================

        [HttpGet("faculties/{facultyId}/departments")]
        public async Task<ActionResult<IEnumerable<Department>>> GetDepartments(int facultyId)
        {
            return await _context.Departments
                .Where(d => d.FacultyId == facultyId)
                .ToListAsync();
        }

        [HttpPost("faculties/{facultyId}/departments")]
        public async Task<ActionResult<Department>> CreateDepartment(int facultyId, [FromBody] Department body)
        {
            var department = new Department
            {
                Name = body.Name,
                FacultyId = facultyId
            };
            
            _context.Departments.Add(department);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetDepartments), new { facultyId = facultyId }, department);
        }

        // ================= YEARS OF STUDY =================

        [HttpGet("departments/{departmentId}/years")]
        public async Task<IActionResult> GetYears(int departmentId)
        {
            var years = await _context.YearsOfStudy
                .Include(y => y.Courses)
                .Where(y => y.DepartmentId == departmentId)
                .OrderBy(y => y.Level)
                .ToListAsync();

            var result = years.Select(y => new
            {
                Id = y.Id,
                Name = y.Name,
                Level = y.Level,
                Courses = y.Courses.Select(c => new {
                    Id = c.Id,
                    CourseCode = c.CourseCode,
                    CourseTitle = c.CourseTitle
                }).ToList()
            });

            return Ok(result);
        }

        [HttpPost("departments/{departmentId}/years")]
        public async Task<ActionResult<YearOfStudy>> CreateYearOfStudy(int departmentId, [FromBody] YearOfStudy body)
        {
            var year = new YearOfStudy
            {
                Name = body.Name,
                Level = body.Level,
                DepartmentId = departmentId
            };
            
            _context.YearsOfStudy.Add(year);
            await _context.SaveChangesAsync();

            // Auto-populate 10 dummy courses
            var dept = await _context.Departments.FindAsync(departmentId);
            string prefix = "CRS";
            if (dept != null && !string.IsNullOrWhiteSpace(dept.Name))
            {
                var cleanName = new string(dept.Name.Where(char.IsLetter).ToArray()).ToUpper();
                if (cleanName.Length >= 3) prefix = cleanName.Substring(0, 3);
                else if (cleanName.Length > 0) prefix = cleanName.PadRight(3, 'X');
            }

            int baseCode = body.Level * 100;
            if (baseCode == 0) baseCode = 100;

            for (int i = 1; i <= 10; i++)
            {
                var codeSuffix = baseCode + i;
                var courseCode = $"{prefix}{codeSuffix}";
                
                var dummyCourse = new Course
                {
                    CourseCode = courseCode,
                    CourseTitle = $"{dept?.Name ?? "General"} Course {codeSuffix}",
                    YearOfStudyId = year.Id,
                    LecturerId = 1 
                };
                _context.Courses.Add(dummyCourse);
            }
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetYears), new { departmentId = departmentId }, year);
        }

        // ================= ASSIGN COURSE TO YEAR =================

        [HttpPost("years/{yearId}/courses/{courseId}")]
        public async Task<IActionResult> AssignCourseToYear(int yearId, int courseId)
        {
            var course = await _context.Courses.FindAsync(courseId);
            var year = await _context.YearsOfStudy.FindAsync(yearId);

            if (course == null || year == null)
            {
                return NotFound("Course or Year not found.");
            }

            course.YearOfStudyId = yearId;
            _context.Entry(course).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Course assigned successfully.", course });
        }
        
        // ================= UPDATE COURSE =================
        
        [HttpPut("courses/{courseId}")]
        public async Task<IActionResult> UpdateCourse(int courseId, [FromBody] Course body)
        {
            var course = await _context.Courses.FindAsync(courseId);
            if (course == null)
                return NotFound("Course not found.");

            if (!string.IsNullOrWhiteSpace(body.CourseCode))
                course.CourseCode = body.CourseCode;
            if (!string.IsNullOrWhiteSpace(body.CourseTitle))
                course.CourseTitle = body.CourseTitle;

            _context.Entry(course).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Course updated successfully.", course = new { course.Id, course.CourseCode, course.CourseTitle } });
        }

        // ================= ADD NEW COURSE TO YEAR =================

        [HttpPost("years/{yearId}/courses")]
        public async Task<IActionResult> AddCourseToYear(int yearId, [FromBody] Course body)
        {
            var year = await _context.YearsOfStudy.Include(y => y.Department).FirstOrDefaultAsync(y => y.Id == yearId);
            if (year == null)
                return NotFound("Year of study not found.");

            // Auto-generate course code if not provided
            var courseCode = body.CourseCode;
            if (string.IsNullOrWhiteSpace(courseCode))
            {
                string prefix = "CRS";
                if (year.Department != null && !string.IsNullOrWhiteSpace(year.Department.Name))
                {
                    var cleanName = new string(year.Department.Name.Where(char.IsLetter).ToArray()).ToUpper();
                    if (cleanName.Length >= 3) prefix = cleanName.Substring(0, 3);
                    else if (cleanName.Length > 0) prefix = cleanName.PadRight(3, 'X');
                }

                // Find the next available number
                var existingCourses = await _context.Courses.Where(c => c.YearOfStudyId == yearId).ToListAsync();
                int baseCode = year.Level * 100;
                if (baseCode == 0) baseCode = 100;
                int nextNum = existingCourses.Count + 1;
                courseCode = $"{prefix}{baseCode + nextNum}";
            }

            var courseTitle = body.CourseTitle;
            if (string.IsNullOrWhiteSpace(courseTitle))
            {
                courseTitle = $"{year.Department?.Name ?? "General"} Course {courseCode}";
            }

            var newCourse = new Course
            {
                CourseCode = courseCode,
                CourseTitle = courseTitle,
                YearOfStudyId = yearId,
                LecturerId = 1
            };

            _context.Courses.Add(newCourse);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Course added.", course = new { newCourse.Id, newCourse.CourseCode, newCourse.CourseTitle } });
        }

        // ================= DELETE COURSE =================

        [HttpDelete("courses/{courseId}")]
        public async Task<IActionResult> DeleteCourse(int courseId)
        {
            var course = await _context.Courses.FindAsync(courseId);
            if (course == null)
                return NotFound("Course not found.");

            _context.Courses.Remove(course);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Course deleted." });
        }

        // ================= GET FULL HIERARCHY TREE =================
        // Returns the nested structure: Faculty -> Departments -> Years -> Courses -> Videos
        
        [HttpGet("tree")]
        public async Task<IActionResult> GetHierarchyTree()
        {
            // Fetch everything with Includes
            var faculties = await _context.Faculties
                .Include(f => f.Departments)
                    .ThenInclude(d => d.YearsOfStudy)
                        .ThenInclude(y => y.Courses)
                .ToListAsync();

            // Note: EF Core Include doesn't handle double-deep collections easily in all SQLite setups,
            // so we do a quick workaround for getting the videos per course.
            var videos = await _context.CourseVideos.ToListAsync();
            
            // Map into DTOs
            var tree = faculties.Select(f => new
            {
                Id = f.Id,
                Name = f.Name,
                Type = "Faculty",
                Children = f.Departments.Select(d => new
                {
                    Id = d.Id,
                    Name = d.Name,
                    Type = "Department",
                    Children = d.YearsOfStudy.OrderBy(y => y.Level).Select(y => new
                    {
                        Id = y.Id,
                        Name = y.Name,
                        Level = y.Level,
                        Type = "Year",
                        Children = y.Courses.Select(c => new
                        {
                            Id = c.Id,
                            CourseCode = c.CourseCode,
                            CourseTitle = c.CourseTitle,
                            Type = "Course",
                            Videos = videos.Where(v => v.CourseId == c.Id).OrderBy(v => v.OrderIndex).Select(v => new
                            {
                                Id = v.Id,
                                Title = v.Title,
                                Description = v.Description,
                                VideoUrl = v.VideoUrl,
                                AddedAt = v.AddedAt
                            }).ToList()
                        }).ToList()
                    }).ToList()
                }).ToList()
            }).ToList();

            return Ok(tree);
        }
    }
}
