import sqlite3
conn = sqlite3.connect('C:/EduSyncAI/Data/edusync.db')
conn.execute('UPDATE Courses SET CourseTitle = REPLACE(CourseTitle, " Dummy", "")')
conn.commit()
print('Done')
