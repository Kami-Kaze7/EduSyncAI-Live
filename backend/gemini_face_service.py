"""
Gemini AI Facial Recognition Service
Handles face recognition using Google's Gemini Vision API
"""

import google.generativeai as genai
from flask import Flask, request, jsonify
from flask_cors import CORS
import cv2
import base64
import os
import sqlite3
from PIL import Image
import io
from dotenv import load_dotenv
from pathlib import Path

# Load environment variables
load_dotenv()

# Configure Gemini AI
GEMINI_API_KEY = os.getenv('GEMINI_API_KEY')
if not GEMINI_API_KEY:
    raise ValueError("GEMINI_API_KEY not found in environment variables")

genai.configure(api_key=GEMINI_API_KEY)

# Use the stable vision model that's widely available
try:
    # Try gemini-pro-vision (most stable for vision tasks)
    model = genai.GenerativeModel('gemini-pro-vision')
    print("✅ Using model: gemini-pro-vision")
except Exception as e:
    print(f"⚠️ Error loading gemini-pro-vision: {e}")
    # Fallback to basic gemini-pro
    model = genai.GenerativeModel('gemini-pro')
    print("✅ Fallback to model: gemini-pro")

# Flask app
app = Flask(__name__)
CORS(app)

# Configuration
DATABASE_PATH = os.getenv('DATABASE_PATH', '../Data/edusync.db')
STUDENT_PHOTOS_PATH = os.getenv('STUDENT_PHOTOS_PATH', '../Data/uploads/students')

# Global camera object
camera = None


def get_db_connection():
    """Get database connection"""
    db_path = Path(__file__).parent / DATABASE_PATH
    conn = sqlite3.connect(str(db_path))
    conn.row_factory = sqlite3.Row
    return conn


def load_enrolled_students(session_id):
    """Load students enrolled in the course for this session"""
    conn = get_db_connection()
    cursor = conn.cursor()
    
    # Get course ID from session
    cursor.execute("""
        SELECT CourseId FROM ClassSessions WHERE Id = ?
    """, (session_id,))
    
    session = cursor.fetchone()
    if not session:
        conn.close()
        return []
    
    course_id = session['CourseId']
    
    # Get enrolled students with photos
    cursor.execute("""
        SELECT s.Id, s.MatricNumber, s.FullName, s.PhotoPath
        FROM Students s
        INNER JOIN CourseEnrollments ce ON s.Id = ce.StudentId
        WHERE ce.CourseId = ? AND s.PhotoPath IS NOT NULL
    """, (course_id,))
    
    students = []
    for row in cursor.fetchall():
        # Handle path normalization (db stores /uploads/students/file.jpg)
        photo_rel = row['PhotoPath'].lstrip('/')
        # The web api saves in Data/uploads/students/
        # gemini_face_service.py is in backend/
        # So it needs to go up to root then Data/
        photo_path = Path(__file__).parent.parent / "Data" / photo_rel
        
        students.append({
            'id': row['Id'],
            'matric_number': row['MatricNumber'],
            'name': row['FullName'],
            'photo_path': str(photo_path)
        })
    
    conn.close()
    return students


def create_recognition_prompt(students):
    """Create optimized prompt for Gemini"""
    student_list = "\n".join([
        f"{i+1}. {s['name']} (ID: {s['id']}, Matric: {s['matric_number']})"
        for i, s in enumerate(students)
    ])
    
    prompt = f"""You are a facial recognition system for classroom attendance.

TASK: Compare faces in the classroom photo with the {len(students)} reference photos provided.

REFERENCE STUDENTS:
{student_list}

INSTRUCTIONS:
1. Carefully examine each face in the classroom photo.
2. Compare with each reference photo provided. The reference photos are provided in the same order as the list above.
3. Only return matches if you are VERY confident (>80%).
4. If a face in the classroom photo does not clearly match any reference photo with high confidence, do NOT match it.
5. Be strict - false positives are not acceptable.

RESPONSE FORMAT (JSON only, no other text):
{{
  "matches": [
    {{"student_id": 1, "name": "John Doe", "confidence": 0.95}},
    {{"student_id": 2, "name": "Jane Smith", "confidence": 0.88}}
  ],
  "total_detected_faces": 5,
  "unmatched_faces": 2
}}

Return ONLY the JSON, nothing else."""
    
    return prompt


@app.route('/api/facial/test', methods=['GET'])
def test_connection():
    """Test endpoint to verify service is running"""
    return jsonify({
        'status': 'ok',
        'message': 'Gemini Facial Recognition Service is running',
        'gemini_configured': bool(GEMINI_API_KEY)
    })


@app.route('/api/facial/recognize', methods=['POST'])
def recognize_faces():
    """
    Recognize faces in uploaded image
    Request body: {
        "session_id": 123,
        "image": "base64_encoded_image"
    }
    """
    try:
        data = request.json
        session_id = data.get('session_id')
        image_data = data.get('image')
        
        if not session_id or not image_data:
            return jsonify({'error': 'Missing session_id or image'}), 400
        
        # Decode image
        image_bytes = base64.b64decode(image_data.split(',')[1] if ',' in image_data else image_data)
        classroom_image = Image.open(io.BytesIO(image_bytes))
        
        # Load enrolled students
        students = load_enrolled_students(session_id)
        
        if not students:
            return jsonify({
                'error': 'No students with photos enrolled in this course',
                'matches': []
            }), 200
        
        # Load reference photos
        reference_images = []
        valid_students = []
        
        for student in students:
            photo_path = Path(student['photo_path'])
            if photo_path.exists():
                try:
                    ref_img = Image.open(photo_path)
                    reference_images.append(ref_img)
                    valid_students.append(student)
                except Exception as e:
                    print(f"Error loading photo for {student['name']}: {e}")
            else:
                print(f"Photo not found for {student['name']} at {photo_path}")
        
        if not valid_students:
            return jsonify({
                'error': 'No valid student photos found on disk',
                'matches': []
            }), 200
        
        # Create prompt
        prompt = create_recognition_prompt(valid_students)
        
        # Call Gemini API
        print(f"Calling Gemini API with {len(valid_students)} reference photos...")
        
        content_parts = [prompt, classroom_image] + reference_images
        response = model.generate_content(content_parts)
        
        # Parse response
        response_text = response.text.strip()
        
        # Extract JSON from response (remove markdown code blocks if present)
        if '```json' in response_text:
            response_text = response_text.split('```json')[1].split('```')[0].strip()
        elif '```' in response_text:
            response_text = response_text.split('```')[1].split('```')[0].strip()
        
        import json
        result = json.loads(response_text)
        
        print(f"Recognition complete: {len(result.get('matches', []))} matches found")
        
        return jsonify(result)
    
    except Exception as e:
        print(f"Error in recognize_faces: {str(e)}")
        import traceback
        traceback.print_exc()
        return jsonify({'error': str(e)}), 500


@app.route('/api/facial/camera/start', methods=['POST'])
def start_camera():
    """Start camera capture"""
    global camera
    try:
        if camera is None:
            camera = cv2.VideoCapture(0)
            if not camera.isOpened():
                return jsonify({'error': 'Failed to open camera'}), 500
        
        return jsonify({'status': 'camera started'})
    except Exception as e:
        return jsonify({'error': str(e)}), 500


@app.route('/api/facial/camera/capture', methods=['GET'])
def capture_frame():
    """Capture a frame from camera"""
    global camera
    try:
        if camera is None or not camera.isOpened():
            return jsonify({'error': 'Camera not started'}), 400
        
        ret, frame = camera.read()
        if not ret:
            return jsonify({'error': 'Failed to capture frame'}), 500
        
        # Convert to base64
        _, buffer = cv2.imencode('.jpg', frame)
        image_base64 = base64.b64encode(buffer).decode('utf-8')
        
        return jsonify({
            'image': f'data:image/jpeg;base64,{image_base64}'
        })
    except Exception as e:
        return jsonify({'error': str(e)}), 500


@app.route('/api/facial/camera/stop', methods=['POST'])
def stop_camera():
    """Stop camera capture"""
    global camera
    try:
        if camera is not None:
            camera.release()
            camera = None
        
        return jsonify({'status': 'camera stopped'})
    except Exception as e:
        return jsonify({'error': str(e)}), 500


@app.route('/api/facial/mark-attendance', methods=['POST'])
def mark_attendance():
    """
    Mark attendance for recognized students
    Request body: {
        "session_id": 123,
        "matches": [
            {"student_id": 1, "confidence": 0.95},
            {"student_id": 2, "confidence": 0.88}
        ]
    }
    """
    try:
        data = request.json
        session_id = data.get('session_id')
        matches = data.get('matches', [])
        
        if not session_id:
            return jsonify({'error': 'Missing session_id'}), 400
        
        conn = get_db_connection()
        cursor = conn.cursor()
        
        new_marks = 0
        for match in matches:
            student_id = match.get('student_id')
            
            # Check if already marked
            cursor.execute("""
                SELECT Id FROM Attendance 
                WHERE SessionId = ? AND StudentId = ?
            """, (session_id, student_id))
            
            if cursor.fetchone():
                continue  # Already marked
            
            # Mark attendance (Align with DatabaseService.cs schema)
            # Columns: SessionId, StudentId, CheckInTime, CheckInMethod, VerifiedBy
            cursor.execute("""
                INSERT INTO Attendance (SessionId, StudentId, CheckInTime, CheckInMethod)
                VALUES (?, ?, datetime('now'), 'Facial')
            """, (session_id, student_id))
            
            new_marks += 1
            
        if new_marks > 0:
            # Increment AttendanceCount in ClassSessions
            cursor.execute("""
                UPDATE ClassSessions 
                SET AttendanceCount = AttendanceCount + ? 
                WHERE Id = ?
            """, (new_marks, session_id))
            conn.commit()
        
        conn.close()
        
        return jsonify({
            'status': 'success',
            'marked_count': new_marks
        })
    
    except Exception as e:
        print(f"Error marking attendance: {e}")
        return jsonify({'error': str(e)}), 500


if __name__ == '__main__':
    host = os.getenv('FLASK_HOST', '127.0.0.1')
    port = int(os.getenv('FLASK_PORT', 5001))
    debug = os.getenv('FLASK_DEBUG', 'False').lower() == 'true'
    
    print(f"Starting Gemini Facial Recognition Service on {host}:{port}")
    print(f"Gemini API Key configured: {bool(GEMINI_API_KEY)}")
    
    app.run(host=host, port=port, debug=debug)
