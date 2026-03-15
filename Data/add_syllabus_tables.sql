-- SQL Migration Script for AI Syllabus Summarization Feature
-- Run this script to add the missing tables to edusync.db

-- Create CourseSyllabi table
CREATE TABLE IF NOT EXISTS CourseSyllabi (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    CourseId INTEGER NOT NULL,
    LecturerId INTEGER NOT NULL,
    FileName TEXT NOT NULL,
    FilePath TEXT NOT NULL,
    FileType TEXT NOT NULL,
    ExtractedText TEXT,
    TotalWeeks INTEGER,
    UploadedAt TEXT NOT NULL,
    FOREIGN KEY (CourseId) REFERENCES Courses(Id),
    FOREIGN KEY (LecturerId) REFERENCES Lecturers(Id)
);

-- Create WeeklySummaries table
CREATE TABLE IF NOT EXISTS WeeklySummaries (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    SyllabusId INTEGER NOT NULL,
    CourseId INTEGER NOT NULL,
    LecturerId INTEGER NOT NULL,
    WeekNumber INTEGER NOT NULL,
    WeekTitle TEXT,
    Summary TEXT NOT NULL,
    KeyTopics TEXT,
    LearningObjectives TEXT,
    PreparationNotes TEXT,
    GeneratedAt TEXT NOT NULL,
    SentToStudents INTEGER DEFAULT 0,
    SentAt TEXT,
    FOREIGN KEY (SyllabusId) REFERENCES CourseSyllabi(Id),
    FOREIGN KEY (CourseId) REFERENCES Courses(Id),
    FOREIGN KEY (LecturerId) REFERENCES Lecturers(Id)
);

-- Verify tables were created
SELECT name FROM sqlite_master WHERE type='table' AND (name='CourseSyllabi' OR name='WeeklySummaries');
