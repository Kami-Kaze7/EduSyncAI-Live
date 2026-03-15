import sqlite3
import os
from pathlib import Path

def init_database():
    """
    Initialize the EduSync database with all required tables.
    Connects to data/edusync.db and creates tables if they don't exist.
    """
    # Ensure the Data directory exists
    data_dir = Path("../Data")
    data_dir.mkdir(exist_ok=True)
    
    # Database path
    db_path = data_dir / "edusync.db"
    
    # Connect to the database
    connection = sqlite3.connect(db_path)
    cursor = connection.cursor()
    
    # Create Students table
    cursor.execute("""
        CREATE TABLE IF NOT EXISTS Students (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            MatricNumber TEXT,
            FullName TEXT,
            Email TEXT
        )
    """)
    
    # Create Courses table
    cursor.execute("""
        CREATE TABLE IF NOT EXISTS Courses (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            CourseCode TEXT,
            CourseTitle TEXT
        )
    """)
    
    # Create Lectures table
    cursor.execute("""
        CREATE TABLE IF NOT EXISTS Lectures (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            CourseId INTEGER,
            LectureDate TEXT,
            Topic TEXT,
            FOREIGN KEY (CourseId) REFERENCES Courses(Id)
        )
    """)
    
    # Create LecturePreps table
    cursor.execute("""
        CREATE TABLE IF NOT EXISTS LecturePreps (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            LectureId INTEGER,
            CoreIdeas TEXT,
            KeyTerms TEXT,
            SimpleExample TEXT,
            WhatToListenFor TEXT,
            CreatedAt TEXT,
            FOREIGN KEY (LectureId) REFERENCES Lectures(Id)
        )
    """)
    
    # Create CourseEnrollments table
    cursor.execute("""
        CREATE TABLE IF NOT EXISTS CourseEnrollments (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            StudentId INTEGER,
            CourseId INTEGER,
            FOREIGN KEY (StudentId) REFERENCES Students(Id),
            FOREIGN KEY (CourseId) REFERENCES Courses(Id)
        )
    """)
    
    # Commit changes and close connection
    connection.commit()
    connection.close()
    
    print(f"Database initialized successfully at {db_path.absolute()}")
    print("Tables created: Students, Courses, Lectures, LecturePreps, CourseEnrollments")


if __name__ == "__main__":
    init_database()
