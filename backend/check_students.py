import sqlite3

conn = sqlite3.connect('Data/edusync.db')
cursor = conn.cursor()

cursor.execute('SELECT MatricNumber, FullName, Email FROM Students')
students = cursor.fetchall()

print("Existing Students in Database:")
print("-" * 60)
if students:
    for row in students:
        print(f"Matric: {row[0]:15} | Name: {row[1]:20} | Email: {row[2]}")
else:
    print("No students found")

print("-" * 60)
print(f"Total students: {len(students)}")

conn.close()
