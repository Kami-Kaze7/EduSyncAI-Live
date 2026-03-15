import sqlite3
import sys

# Connect to database
conn = sqlite3.connect('Data/edusync.db')
cursor = conn.cursor()

print("=== LECTURERS ===")
cursor.execute("SELECT Id, Username, Email, FullName, PIN FROM Lecturers")
lecturers = cursor.fetchall()
for lec in lecturers:
    print(f"ID: {lec[0]}, Username: {lec[1]}, Email: {lec[2]}, Name: {lec[3]}, PIN: {lec[4]}")

print("\n=== STUDENTS ===")
cursor.execute("SELECT Id, MatricNumber, FullName, Email, PIN, PasswordHash FROM Students")
students = cursor.fetchall()
for stu in students:
    print(f"ID: {stu[0]}, Matric: {stu[1]}, Name: {stu[2]}, Email: {stu[3]}, PIN: {stu[4]}")
    print(f"  PasswordHash: {stu[5][:50] if stu[5] else 'NULL'}...")

conn.close()
