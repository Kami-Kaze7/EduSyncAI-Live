
import sqlite3
import json

conn = sqlite3.connect('/opt/edusyncai/Data/edusync.db')
conn.row_factory = sqlite3.Row
cur = conn.cursor()

courses = cur.execute("SELECT * FROM Courses").fetchall()
lecturers = cur.execute("SELECT * FROM Lecturers").fetchall()

lec_dict = {l['Id']: l['FullName'] for l in lecturers}

result = []
for c in courses:
    result.append({
        "Id": c["Id"],
        "CourseCode": c["CourseCode"],
        "CourseName": c["CourseName"],
        "LecturerId": c["LecturerId"],
        "LecturerName": lec_dict.get(c["LecturerId"], "Unassigned")
    })

print(json.dumps(result, indent=2))
conn.close()
