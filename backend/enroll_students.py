import sqlite3
from pathlib import Path

# Connect to database
db_path = Path("../Data/edusync.db")
conn = sqlite3.connect(db_path)
cursor = conn.cursor()

print("📚 Student Enrollment Tool")
print("=" * 50)

# Show available courses
print("\nAvailable Courses:")
cursor.execute("SELECT Id, CourseCode, CourseTitle FROM Courses")
courses = cursor.fetchall()
for course_id, code, title in courses:
    print(f"  {course_id}. {code} - {title}")

# Show available students
print("\nAvailable Students:")
cursor.execute("SELECT Id, MatricNumber, FullName FROM Students")
students = cursor.fetchall()
for student_id, matric, name in students:
    print(f"  {student_id}. {name} ({matric})")

# Enroll all students in all courses
print("\n🔄 Enrolling all students in all courses...")
for course_id, _, _ in courses:
    for student_id, _, _ in students:
        try:
            cursor.execute(
                "INSERT INTO CourseEnrollments (StudentId, CourseId) VALUES (?, ?)",
                (student_id, course_id)
            )
        except:
            pass  # Skip if already enrolled

conn.commit()
conn.close()

print("✅ Done! All students are now enrolled in all courses.")
print("\nYou can now prepare lectures and students will be notified!")
