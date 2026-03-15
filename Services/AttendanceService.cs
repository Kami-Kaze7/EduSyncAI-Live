using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EduSyncAI
{
    public class AttendanceService
    {
        private readonly DatabaseService _dbService;
        private readonly BiometricAuthenticationService _biometricService;

        public AttendanceService()
        {
            _dbService = new DatabaseService();
            _biometricService = new BiometricAuthenticationService();
        }

        /// <summary>
        /// Marks student attendance using fingerprint verification
        /// Returns the student who checked in, or null if failed
        /// </summary>
        public async Task<Student?> CheckInWithFingerprintAsync(int sessionId, int courseId)
        {
            // Verify session is Live
            var session = _dbService.GetClassSessionById(sessionId);
            if (session == null || session.State != SessionState.Live)
            {
                throw new InvalidOperationException("Attendance can only be marked during a Live session");
            }

            // Verify fingerprint using Windows Hello
            bool verified = await _biometricService.AuthenticateWithBiometricAsync("Place your finger to check in");
            
            if (!verified)
            {
                return null;  // Fingerprint verification failed
            }

            // In a real implementation, we would map the Windows Hello identity to a student
            // For now, we'll return null and let the UI handle student selection
            // This is a limitation of Windows Hello - it doesn't give us the user identity directly
            return null;
        }

        /// <summary>
        /// Marks attendance for a specific student (after fingerprint verification)
        /// </summary>
        public int MarkStudentPresent(int sessionId, int studentId, CheckInMethod method, int? verifiedBy = null)
        {
            // Check if already present
            if (_dbService.IsStudentPresent(sessionId, studentId))
            {
                throw new InvalidOperationException("Student already marked present for this session");
            }

            // Check if student is enrolled in the course
            var session = _dbService.GetClassSessionById(sessionId);
            var enrolledStudents = _dbService.GetEnrolledStudents(session.CourseId);
            
            if (!enrolledStudents.Any(s => s.Id == studentId))
            {
                throw new InvalidOperationException("Student is not enrolled in this course");
            }

            var attendance = new AttendanceRecord
            {
                SessionId = sessionId,
                StudentId = studentId,
                CheckInTime = DateTime.Now,
                CheckInMethod = method.ToString(),
                VerifiedBy = verifiedBy
            };

            int attendanceId = _dbService.MarkAttendance(attendance);

            // Update session attendance count
            int count = _dbService.GetAttendanceCount(sessionId);
            var sessionToUpdate = _dbService.GetClassSessionById(sessionId);
            sessionToUpdate.AttendanceCount = count;
            _dbService.UpdateClassSession(sessionToUpdate);

            return attendanceId;
        }

        /// <summary>
        /// Gets all attendance records for a session
        /// </summary>
        public List<AttendanceRecord> GetSessionAttendance(int sessionId)
        {
            return _dbService.GetSessionAttendance(sessionId);
        }

        /// <summary>
        /// Gets students who are absent from a session
        /// </summary>
        public List<Student> GetAbsentStudents(int sessionId, int courseId)
        {
            return _dbService.GetAbsentStudents(sessionId, courseId);
        }

        /// <summary>
        /// Gets attendance count for a session
        /// </summary>
        public int GetAttendanceCount(int sessionId)
        {
            return _dbService.GetAttendanceCount(sessionId);
        }

        /// <summary>
        /// Checks if a student is already present
        /// </summary>
        public bool IsStudentPresent(int sessionId, int studentId)
        {
            return _dbService.IsStudentPresent(sessionId, studentId);
        }

        /// <summary>
        /// Gets enrolled students for a course (for selection after fingerprint)
        /// </summary>
        public List<Student> GetEnrolledStudents(int courseId)
        {
            return _dbService.GetEnrolledStudents(courseId);
        }
    }
}
