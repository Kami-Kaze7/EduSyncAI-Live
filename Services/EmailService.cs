using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mail;

namespace EduSyncAI
{
    public class EmailService
    {
        private readonly string _smtpServer = "smtp.gmail.com";
        private readonly int _smtpPort = 587;
        private readonly string _fromEmail = "YOUR_EMAIL@gmail.com";
        private readonly string _appPassword = "APP_PASSWORD";
        
        // Set to true to test without real email credentials
        private readonly bool _mockMode = true;

        public void SendEmail(string to, string subject, string body)
        {
            // Mock mode - just log to console instead of sending
            if (_mockMode)
            {
                Console.WriteLine("=================================");
                Console.WriteLine("📧 MOCK EMAIL (Not Actually Sent)");
                Console.WriteLine("=================================");
                Console.WriteLine($"To: {to}");
                Console.WriteLine($"Subject: {subject}");
                Console.WriteLine($"Body Preview: {body.Substring(0, Math.Min(200, body.Length))}...");
                Console.WriteLine("=================================\n");
                
                System.Windows.MessageBox.Show(
                    $"✅ Mock Email Logged!\n\nTo: {to}\nSubject: {subject}\n\nCheck console output for full email content.",
                    "Email Mock Mode",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
                return;
            }
            
            // Real email sending (requires credentials)
            try
            {
                var client = new SmtpClient(_smtpServer, _smtpPort)
                {
                    Credentials = new NetworkCredential(_fromEmail, _appPassword),
                    EnableSsl = true
                };

                var mail = new MailMessage(_fromEmail, to, subject, body)
                {
                    IsBodyHtml = true
                };
                
                client.Send(mail);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Email send failed: {ex.Message}");
                throw;
            }
        }

        public void SendLecturePrepNotification(List<Student> students, LecturePrep prep, string lectureTopic, DateTime lectureDate)
        {
            var subject = $"📚 New Lecture Prep Available: {lectureTopic}";
            
            var body = $@"
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background-color: #4CAF50; color: white; padding: 20px; text-align: center; border-radius: 5px 5px 0 0; }}
                        .content {{ background-color: #f9f9f9; padding: 20px; border-radius: 0 0 5px 5px; }}
                        .section {{ margin-bottom: 20px; }}
                        .section-title {{ font-weight: bold; color: #4CAF50; margin-bottom: 5px; }}
                        .footer {{ margin-top: 20px; padding-top: 20px; border-top: 1px solid #ddd; font-size: 12px; color: #666; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h2>🎓 EduSync - Lecture Preparation</h2>
                        </div>
                        <div class='content'>
                            <p>Hello!</p>
                            <p>A new lecture prep has been posted for <strong>{lectureTopic}</strong> scheduled on <strong>{lectureDate:MMMM dd, yyyy}</strong>.</p>
                            
                            <div class='section'>
                                <div class='section-title'>💡 Core Ideas:</div>
                                <p>{prep.CoreIdeas}</p>
                            </div>
                            
                            <div class='section'>
                                <div class='section-title'>🔑 Key Terms:</div>
                                <p>{prep.KeyTerms}</p>
                            </div>
                            
                            <div class='section'>
                                <div class='section-title'>📝 Simple Example:</div>
                                <p>{prep.SimpleExample}</p>
                            </div>
                            
                            <div class='section'>
                                <div class='section-title'>👂 What to Listen For:</div>
                                <p>{prep.WhatToListenFor}</p>
                            </div>
                            
                            <p><strong>Open the EduSync app to view more details and prepare for your lecture!</strong></p>
                            
                            <div class='footer'>
                                <p>This is an automated message from EduSync. Please do not reply to this email.</p>
                            </div>
                        </div>
                    </div>
                </body>
                </html>
            ";

            foreach (var student in students)
            {
                try
                {
                    SendEmail(student.Email, subject, body);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to send email to {student.Email}: {ex.Message}");
                }
            }
            
            // Show summary in mock mode
            if (_mockMode)
            {
                System.Windows.MessageBox.Show(
                    $"✅ Mock Mode: Would have sent {students.Count} email(s)\n\n" +
                    $"Recipients:\n{string.Join("\n", students.ConvertAll(s => $"  • {s.FullName} ({s.Email})"))}",
                    "Email Notification Summary",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
            }
        }
    }
}
