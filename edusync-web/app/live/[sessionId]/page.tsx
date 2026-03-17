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

    const studentUser = typeof window !== 'undefined' ? JSON.parse(localStorage.getItem('studentUser') || '{"fullName":"Student"}') : { fullName: 'Student' };
    const safeDisplayName = encodeURIComponent(studentUser.fullName);

    const jitsiUrl = streamInfo 
        ? `https://meet.jit.si/${streamInfo.roomName}#config.prejoinConfig.enabled=false&config.startWithAudioMuted=true&config.startWithVideoMuted=true&config.disableDeepLinking=true&config.hideConferenceSubject=true&userInfo.displayName=%22${safeDisplayName}%22&interfaceConfig.SHOW_JITSI_WATERMARK=false&interfaceConfig.SHOW_WATERMARK_FOR_GUESTS=false&interfaceConfig.TOOLBAR_BUTTONS=%5B%22microphone%22,%22camera%22,%22chat%22,%22raisehand%22,%22tileview%22%5D` 
        : '';
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

            {/* Main Content - Embedded Jitsi */}
            <div style={{
                flex: 1,
                display: 'flex',
                flexDirection: 'column',
                position: 'relative'
            }}>
                <iframe
                    src={jitsiUrl}
                    allow="camera; microphone; display-capture; autoplay; clipboard-write"
                    style={{
                        width: '100%',
                        height: '100%',
                        border: 'none',
                    }}
                    title="Live Class"
                />
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
