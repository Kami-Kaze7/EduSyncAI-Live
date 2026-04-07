CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
    "ProductVersion" TEXT NOT NULL
);

BEGIN TRANSACTION;
CREATE TABLE "Admins" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_Admins" PRIMARY KEY AUTOINCREMENT,
    "Username" TEXT NOT NULL,
    "PasswordHash" TEXT NOT NULL,
    "FullName" TEXT NOT NULL,
    "CreatedAt" TEXT NOT NULL
);

CREATE TABLE "Attendance" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_Attendance" PRIMARY KEY AUTOINCREMENT,
    "SessionId" INTEGER NOT NULL,
    "StudentId" INTEGER NOT NULL,
    "CheckInTime" TEXT NOT NULL,
    "CheckInMethod" TEXT NOT NULL,
    "VerifiedBy" INTEGER NULL
);

CREATE TABLE "ClassSummaries" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_ClassSummaries" PRIMARY KEY AUTOINCREMENT,
    "CourseId" INTEGER NOT NULL,
    "LecturerId" INTEGER NOT NULL,
    "Title" TEXT NOT NULL,
    "Summary" TEXT NOT NULL,
    "KeyTopics" TEXT NULL,
    "PreparationNotes" TEXT NULL,
    "ClassDate" TEXT NOT NULL,
    "CreatedAt" TEXT NOT NULL
);

CREATE TABLE "Courses" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_Courses" PRIMARY KEY AUTOINCREMENT,
    "CourseCode" TEXT NOT NULL,
    "CourseTitle" TEXT NOT NULL,
    "SyllabusPath" TEXT NULL,
    "LecturerId" INTEGER NOT NULL
);

CREATE TABLE "CourseSyllabi" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_CourseSyllabi" PRIMARY KEY AUTOINCREMENT,
    "CourseId" INTEGER NOT NULL,
    "LecturerId" INTEGER NOT NULL,
    "FileName" TEXT NOT NULL,
    "FilePath" TEXT NOT NULL,
    "FileType" TEXT NOT NULL,
    "ExtractedText" TEXT NULL,
    "TotalWeeks" INTEGER NULL,
    "UploadedAt" TEXT NOT NULL
);

CREATE TABLE "Lecturers" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_Lecturers" PRIMARY KEY AUTOINCREMENT,
    "Username" TEXT NOT NULL,
    "FullName" TEXT NOT NULL,
    "Email" TEXT NOT NULL,
    "PasswordHash" TEXT NOT NULL,
    "IsActive" INTEGER NOT NULL
);

CREATE TABLE "ModelAssets" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_ModelAssets" PRIMARY KEY AUTOINCREMENT,
    "Title" TEXT NOT NULL,
    "Description" TEXT NOT NULL,
    "Discipline" TEXT NOT NULL,
    "ModelUrl" TEXT NOT NULL,
    "ThumbnailUrl" TEXT NOT NULL,
    "UploadedAt" TEXT NOT NULL
);

CREATE TABLE "Students" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_Students" PRIMARY KEY AUTOINCREMENT,
    "MatricNumber" TEXT NOT NULL,
    "FullName" TEXT NOT NULL,
    "Email" TEXT NOT NULL,
    "PhotoPath" TEXT NULL,
    "PasswordHash" TEXT NOT NULL,
    "IsActive" INTEGER NOT NULL,
    "Age" INTEGER NULL,
    "Hobbies" TEXT NULL,
    "Bio" TEXT NULL
);

CREATE TABLE "WeeklySummaries" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_WeeklySummaries" PRIMARY KEY AUTOINCREMENT,
    "SyllabusId" INTEGER NOT NULL,
    "CourseId" INTEGER NOT NULL,
    "LecturerId" INTEGER NOT NULL,
    "WeekNumber" INTEGER NOT NULL,
    "DayNumber" INTEGER NOT NULL,
    "WeekTitle" TEXT NULL,
    "Summary" TEXT NOT NULL,
    "KeyTopics" TEXT NULL,
    "LearningObjectives" TEXT NULL,
    "PreparationNotes" TEXT NULL,
    "GeneratedAt" TEXT NOT NULL,
    "SentToStudents" INTEGER NOT NULL,
    "SentAt" TEXT NULL
);

CREATE TABLE "ClassSessions" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_ClassSessions" PRIMARY KEY AUTOINCREMENT,
    "CourseId" INTEGER NOT NULL,
    "LectureId" INTEGER NOT NULL,
    "LecturerId" INTEGER NULL,
    "SessionCode" TEXT NULL,
    "SessionState" TEXT NOT NULL,
    "StartTime" TEXT NULL,
    "EndTime" TEXT NULL,
    "AudioFilePath" TEXT NULL,
    "VideoFilePath" TEXT NULL,
    "BoardExportPath" TEXT NULL,
    "BoardSnapshotFolder" TEXT NULL,
    "AttendanceCount" INTEGER NOT NULL,
    "Duration" INTEGER NOT NULL,
    "CreatedAt" TEXT NOT NULL,
    "Topic" TEXT NULL,
    CONSTRAINT "FK_ClassSessions_Courses_CourseId" FOREIGN KEY ("CourseId") REFERENCES "Courses" ("Id") ON DELETE CASCADE
);

CREATE TABLE "CourseEnrollments" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_CourseEnrollments" PRIMARY KEY AUTOINCREMENT,
    "CourseId" INTEGER NOT NULL,
    "StudentId" INTEGER NOT NULL,
    CONSTRAINT "FK_CourseEnrollments_Courses_CourseId" FOREIGN KEY ("CourseId") REFERENCES "Courses" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_CourseEnrollments_Students_StudentId" FOREIGN KEY ("StudentId") REFERENCES "Students" ("Id") ON DELETE CASCADE
);

CREATE TABLE "StudentWeeklySummaries" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_StudentWeeklySummaries" PRIMARY KEY AUTOINCREMENT,
    "StudentId" INTEGER NOT NULL,
    "WeeklySummaryId" INTEGER NOT NULL,
    "SentAt" TEXT NOT NULL,
    CONSTRAINT "FK_StudentWeeklySummaries_Students_StudentId" FOREIGN KEY ("StudentId") REFERENCES "Students" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_StudentWeeklySummaries_WeeklySummaries_WeeklySummaryId" FOREIGN KEY ("WeeklySummaryId") REFERENCES "WeeklySummaries" ("Id") ON DELETE CASCADE
);

CREATE TABLE "LectureMaterials" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_LectureMaterials" PRIMARY KEY AUTOINCREMENT,
    "SessionId" INTEGER NOT NULL,
    "FileName" TEXT NOT NULL,
    "FilePath" TEXT NOT NULL,
    "FileType" TEXT NOT NULL,
    "FileSize" INTEGER NOT NULL,
    "UploadedAt" TEXT NOT NULL,
    CONSTRAINT "FK_LectureMaterials_ClassSessions_SessionId" FOREIGN KEY ("SessionId") REFERENCES "ClassSessions" ("Id") ON DELETE CASCADE
);

CREATE TABLE "LectureNotes" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_LectureNotes" PRIMARY KEY AUTOINCREMENT,
    "SessionId" INTEGER NOT NULL,
    "Content" TEXT NOT NULL,
    "LastModified" TEXT NOT NULL,
    CONSTRAINT "FK_LectureNotes_ClassSessions_SessionId" FOREIGN KEY ("SessionId") REFERENCES "ClassSessions" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_ClassSessions_CourseId" ON "ClassSessions" ("CourseId");

CREATE INDEX "IX_CourseEnrollments_CourseId" ON "CourseEnrollments" ("CourseId");

CREATE INDEX "IX_CourseEnrollments_StudentId" ON "CourseEnrollments" ("StudentId");

CREATE INDEX "IX_LectureMaterials_SessionId" ON "LectureMaterials" ("SessionId");

CREATE UNIQUE INDEX "IX_LectureNotes_SessionId" ON "LectureNotes" ("SessionId");

CREATE INDEX "IX_StudentWeeklySummaries_StudentId" ON "StudentWeeklySummaries" ("StudentId");

CREATE INDEX "IX_StudentWeeklySummaries_WeeklySummaryId" ON "StudentWeeklySummaries" ("WeeklySummaryId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260402111849_AddModel3DAssets', '9.0.0');

ALTER TABLE "Courses" ADD "YearOfStudyId" INTEGER NULL;

CREATE TABLE "CourseVideos" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_CourseVideos" PRIMARY KEY AUTOINCREMENT,
    "CourseId" INTEGER NOT NULL,
    "Title" TEXT NOT NULL,
    "Description" TEXT NULL,
    "VideoUrl" TEXT NOT NULL,
    "OrderIndex" INTEGER NOT NULL,
    "AddedAt" TEXT NOT NULL,
    CONSTRAINT "FK_CourseVideos_Courses_CourseId" FOREIGN KEY ("CourseId") REFERENCES "Courses" ("Id") ON DELETE CASCADE
);

CREATE TABLE "Faculties" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_Faculties" PRIMARY KEY AUTOINCREMENT,
    "Name" TEXT NOT NULL,
    "CreatedAt" TEXT NOT NULL
);

CREATE TABLE "Departments" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_Departments" PRIMARY KEY AUTOINCREMENT,
    "FacultyId" INTEGER NOT NULL,
    "Name" TEXT NOT NULL,
    CONSTRAINT "FK_Departments_Faculties_FacultyId" FOREIGN KEY ("FacultyId") REFERENCES "Faculties" ("Id") ON DELETE CASCADE
);

CREATE TABLE "YearsOfStudy" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_YearsOfStudy" PRIMARY KEY AUTOINCREMENT,
    "DepartmentId" INTEGER NOT NULL,
    "Name" TEXT NOT NULL,
    "Level" INTEGER NOT NULL,
    CONSTRAINT "FK_YearsOfStudy_Departments_DepartmentId" FOREIGN KEY ("DepartmentId") REFERENCES "Departments" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_Courses_YearOfStudyId" ON "Courses" ("YearOfStudyId");

CREATE INDEX "IX_CourseVideos_CourseId" ON "CourseVideos" ("CourseId");

CREATE INDEX "IX_Departments_FacultyId" ON "Departments" ("FacultyId");

CREATE INDEX "IX_YearsOfStudy_DepartmentId" ON "YearsOfStudy" ("DepartmentId");

CREATE TABLE "ef_temp_Courses" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_Courses" PRIMARY KEY AUTOINCREMENT,
    "CourseCode" TEXT NOT NULL,
    "CourseTitle" TEXT NOT NULL,
    "LecturerId" INTEGER NOT NULL,
    "SyllabusPath" TEXT NULL,
    "YearOfStudyId" INTEGER NULL,
    CONSTRAINT "FK_Courses_YearsOfStudy_YearOfStudyId" FOREIGN KEY ("YearOfStudyId") REFERENCES "YearsOfStudy" ("Id")
);

INSERT INTO "ef_temp_Courses" ("Id", "CourseCode", "CourseTitle", "LecturerId", "SyllabusPath", "YearOfStudyId")
SELECT "Id", "CourseCode", "CourseTitle", "LecturerId", "SyllabusPath", "YearOfStudyId"
FROM "Courses";

COMMIT;

PRAGMA foreign_keys = 0;

BEGIN TRANSACTION;
DROP TABLE "Courses";

ALTER TABLE "ef_temp_Courses" RENAME TO "Courses";

COMMIT;

PRAGMA foreign_keys = 1;

BEGIN TRANSACTION;
CREATE INDEX "IX_Courses_YearOfStudyId" ON "Courses" ("YearOfStudyId");

COMMIT;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260403151737_AddAcademicHierarchy', '9.0.0');

