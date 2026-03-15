'use client';

import { useEffect, useState } from 'react';
import { useParams, useRouter } from 'next/navigation';
import { studentApi } from '@/lib/studentApi';
import toast from 'react-hot-toast';
import PictureInPicture from '@/components/PictureInPicture';

export default function LiveClassroomPage() {
    const params = useParams();
    const router = useRouter();
    const sessionId = params.sessionId as string;
    const [streamInfo, setStreamInfo] = useState<any>(null);
    const [isLoading, setIsLoading] = useState(true);
    const [elapsed, setElapsed] = useState('00:00');

    // Fetch stream info
    useEffect(() => {
        const init = async () => {
            try {
                const streams = await studentApi.getActiveLiveStreams();
                const stream = streams.find((s: any) => s.sessionId === parseInt(sessionId));

                if (stream) {
                    setStreamInfo(stream);
                } else {
                    toast.error('This class is no longer live');
                    router.push('/student/dashboard');
                    return;
                }
            } catch (error) {
                console.error('Error loading stream:', error);
                toast.error('Failed to load live class');
            } finally {
                setIsLoading(false);
            }
        };
        init();
    }, [sessionId, router]);

    // Elapsed time timer
    useEffect(() => {
        if (!streamInfo?.startedAt) return;
        const interval = setInterval(() => {
            const start = new Date(streamInfo.startedAt).getTime();
            const now = Date.now();
            const diff = Math.floor((now - start) / 1000);
            const hrs = Math.floor(diff / 3600);
            const mins = Math.floor((diff % 3600) / 60);
            const secs = diff % 60;
            setElapsed(
                hrs > 0
                    ? `${hrs}:${String(mins).padStart(2, '0')}:${String(secs).padStart(2, '0')}`
                    : `${String(mins).padStart(2, '0')}:${String(secs).padStart(2, '0')}`
            );
        }, 1000);
        return () => clearInterval(interval);
    }, [streamInfo]);

    const jitsiUrl = streamInfo ? `https://meet.jit.si/${streamInfo.roomName}` : '';
    const mjpegUrl = streamInfo ? `/api/stream/${streamInfo.sessionId}/video` : '';

    if (isLoading) {
        return (
            <div style={{
                minHeight: '100vh',
                background: 'linear-gradient(135deg, #0f0f23 0%, #1a1a3e 100%)',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                color: 'white',
                fontFamily: "'Inter', 'Segoe UI', sans-serif"
            }}>
                <div style={{ textAlign: 'center' }}>
                    <div style={{ fontSize: '48px', marginBottom: '16px', animation: 'pulse 2s infinite' }}>📡</div>
                    <h2 style={{ fontSize: '20px', fontWeight: 600 }}>Connecting to live class...</h2>
                    <p style={{ color: '#888', marginTop: '8px' }}>Please wait</p>
                </div>
            </div>
        );
    }

    if (!streamInfo) {
        return (
            <div style={{
                minHeight: '100vh',
                background: 'linear-gradient(135deg, #0f0f23 0%, #1a1a3e 100%)',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                color: 'white',
                fontFamily: "'Inter', 'Segoe UI', sans-serif"
            }}>
                <div style={{ textAlign: 'center' }}>
                    <div style={{ fontSize: '48px', marginBottom: '16px' }}>📴</div>
                    <h2 style={{ fontSize: '20px', fontWeight: 600 }}>Class is not live</h2>
                    <p style={{ color: '#888', marginTop: '8px' }}>This session has ended or is not streaming</p>
                    <button
                        onClick={() => router.push('/student/dashboard')}
                        style={{
                            marginTop: '24px',
                            padding: '12px 24px',
                            background: '#3b82f6',
                            color: 'white',
                            border: 'none',
                            borderRadius: '8px',
                            cursor: 'pointer',
                            fontSize: '14px',
                            fontWeight: 600
                        }}
                    >
                        ← Back to Dashboard
                    </button>
                </div>
            </div>
        );
    }

    return (
        <div style={{
            minHeight: '100vh',
            background: 'linear-gradient(135deg, #0f0f23 0%, #1a1a3e 100%)',
            display: 'flex',
            flexDirection: 'column',
            fontFamily: "'Inter', 'Segoe UI', sans-serif"
        }}>
            {/* Header */}
            <div style={{
                background: 'rgba(26, 26, 46, 0.95)',
                padding: '16px 24px',
                borderBottom: '1px solid rgba(255,255,255,0.1)',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'space-between',
                flexWrap: 'wrap',
                gap: '12px'
            }}>
                <div style={{ display: 'flex', alignItems: 'center', gap: '12px' }}>
                    <button
                        onClick={() => router.push('/student/dashboard')}
                        style={{
                            background: 'rgba(255,255,255,0.1)',
                            border: 'none',
                            color: 'white',
                            padding: '8px 12px',
                            borderRadius: '8px',
                            cursor: 'pointer',
                            fontSize: '14px'
                        }}
                    >
                        ← Back
                    </button>
                    <div style={{
                        display: 'flex',
                        alignItems: 'center',
                        gap: '8px',
                        background: 'rgba(239, 68, 68, 0.15)',
                        padding: '6px 14px',
                        borderRadius: '20px',
                        border: '1px solid rgba(239, 68, 68, 0.3)'
                    }}>
                        <span style={{
                            width: '8px', height: '8px',
                            background: '#ef4444', borderRadius: '50%',
                            display: 'inline-block',
                            animation: 'pulse 2s infinite'
                        }} />
                        <span style={{ color: '#ef4444', fontWeight: 700, fontSize: '12px' }}>LIVE</span>
                    </div>
                    <span style={{ color: '#ccc', fontSize: '13px' }}>{elapsed}</span>
                </div>
            </div>

            {/* Main Content */}
            <div style={{
                flex: 1,
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                padding: '24px'
            }}>
                <div style={{
                    background: 'rgba(255, 255, 255, 0.05)',
                    border: '1px solid rgba(255, 255, 255, 0.1)',
                    borderRadius: '20px',
                    padding: '48px 40px',
                    maxWidth: '500px',
                    width: '100%',
                    textAlign: 'center'
                }}>
                    {/* Course Info */}
                    <div style={{ fontSize: '48px', marginBottom: '20px' }}>🎓</div>
                    <h1 style={{
                        color: 'white',
                        fontSize: '24px',
                        fontWeight: 700,
                        margin: '0 0 8px 0'
                    }}>
                        {streamInfo.courseName}
                    </h1>
                    <p style={{ color: '#888', fontSize: '14px', margin: '0 0 32px 0' }}>
                        Session #{sessionId} • Live since {elapsed}
                    </p>

                    {/* Join Button */}
                    <a
                        href={jitsiUrl}
                        target="_blank"
                        rel="noopener noreferrer"
                        style={{
                            display: 'inline-flex',
                            alignItems: 'center',
                            gap: '10px',
                            padding: '16px 36px',
                            background: 'linear-gradient(135deg, #3b82f6, #8b5cf6)',
                            color: 'white',
                            textDecoration: 'none',
                            borderRadius: '12px',
                            fontSize: '18px',
                            fontWeight: 700,
                            boxShadow: '0 4px 20px rgba(59, 130, 246, 0.4)',
                            transition: 'transform 0.2s, box-shadow 0.2s'
                        }}
                    >
                        📹 Join Live Class
                    </a>

                    <p style={{ color: '#666', fontSize: '13px', marginTop: '20px', lineHeight: 1.6 }}>
                        Opens Jitsi Meet in a new tab.<br/>
                        Sign in with your Google or GitHub account to join.
                    </p>

                    {/* Room Info */}
                    <div style={{
                        marginTop: '32px',
                        padding: '16px',
                        background: 'rgba(255,255,255,0.03)',
                        borderRadius: '10px',
                        border: '1px solid rgba(255,255,255,0.06)'
                    }}>
                        <p style={{ color: '#555', fontSize: '12px', margin: '0 0 4px 0' }}>Room Name</p>
                        <p style={{
                            color: '#aaa',
                            fontSize: '14px',
                            fontFamily: 'monospace',
                            margin: 0,
                            wordBreak: 'break-all'
                        }}>
                            {streamInfo.roomName}
                        </p>
                    </div>
                </div>
            </div>

            {/* PiP Lecturer Camera Overlay */}
            <PictureInPicture 
                streamUrl={mjpegUrl} 
                isVisible={!!streamInfo?.hasVideo} 
            />

            {/* Pulse animation */}
            <style jsx global>{`
                @keyframes pulse {
                    0%, 100% { opacity: 1; }
                    50% { opacity: 0.3; }
                }
            `}</style>
        </div>
    );
}
