'use client';

import { useEffect, useState, useRef, useCallback, use } from 'react';
import { useRouter } from 'next/navigation';
import { studentApi } from '@/lib/studentApi';
import toast from 'react-hot-toast';

export default function LiveClassroomPage(props: { params: Promise<{ sessionId: string }> }) {
    const params = use(props.params);
    const router = useRouter();
    const sessionId = params.sessionId;
    const [streamInfo, setStreamInfo] = useState<any>(null);
    const [isLoading, setIsLoading] = useState(true);
    const [isInCall, setIsInCall] = useState(false);
    const [elapsed, setElapsed] = useState('00:00');
    const jitsiContainerRef = useRef<HTMLDivElement>(null);
    const jitsiApiRef = useRef<any>(null);

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

    // Cleanup Jitsi on unmount
    useEffect(() => {
        return () => {
            if (jitsiApiRef.current) {
                try { jitsiApiRef.current.dispose(); } catch {}
            }
        };
    }, []);

    const handleJoinClass = useCallback(() => {
        if (!streamInfo) return;
        setIsInCall(true);
    }, [streamInfo]);

    // Initialize Jitsi only when isInCall is true and container is rendered
    useEffect(() => {
        if (isInCall && jitsiContainerRef.current && !jitsiApiRef.current) {
            let studentUser = { fullName: 'Student' };
            if (typeof window !== 'undefined') {
                try {
                    const stored = localStorage.getItem('studentUser');
                    if (stored) studentUser = JSON.parse(stored);
                } catch { }
            }

            const displayName = studentUser.fullName || 'Student';

            // Load the Jitsi IFrame API script dynamically
            const script = document.createElement('script');
            script.src = 'https://meet.viicsoft.dev/external_api.js';
            script.async = true;
            script.onload = () => {
                // Create Jitsi Meet API instance with full control
                const api = new (window as any).JitsiMeetExternalAPI('meet.viicsoft.dev', {
                    roomName: streamInfo.roomName,
                    parentNode: jitsiContainerRef.current,
                    width: '100%',
                    height: '100%',
                    userInfo: {
                        displayName: displayName,
                    },
                    configOverwrite: {
                        prejoinConfig: { enabled: false },
                        prejoinPageEnabled: false,
                        startWithAudioMuted: true,
                        startWithVideoMuted: true,
                        disableDeepLinking: true,
                        hideConferenceSubject: true,
                        enableInsecureRoomNameWarning: false,
                        enableClosePage: false,
                        disableInviteFunctions: true,
                        toolbarButtons: [
                            'microphone', 'camera', 'chat', 'raisehand',
                            'tileview', 'hangup'
                        ],
                    },
                    interfaceConfigOverwrite: {
                        SHOW_JITSI_WATERMARK: false,
                        SHOW_WATERMARK_FOR_GUESTS: false,
                        SHOW_BRAND_WATERMARK: false,
                        SHOW_POWERED_BY: false,
                        MOBILE_APP_PROMO: false,
                        APP_NAME: 'EduSync AI',
                        NATIVE_APP_NAME: 'EduSync AI',
                        PROVIDER_NAME: 'EduSync AI',
                        DEFAULT_REMOTE_DISPLAY_NAME: 'Student',
                        TOOLBAR_ALWAYS_VISIBLE: true,
                    },
                });

                jitsiApiRef.current = api;

                // When the user hangs up, redirect back to dashboard
                api.addEventListener('readyToClose', () => {
                    console.log('[JITSI] Call ended, redirecting to dashboard');
                    api.dispose();
                    jitsiApiRef.current = null;
                    router.push('/student/dashboard');
                });

                // Also handle video conference left
                api.addEventListener('videoConferenceLeft', () => {
                    console.log('[JITSI] Left conference, redirecting to dashboard');
                    setTimeout(() => {
                        if (jitsiApiRef.current) {
                            jitsiApiRef.current.dispose();
                            jitsiApiRef.current = null;
                        }
                        router.push('/student/dashboard');
                    }, 500);
                });
            };
            script.onerror = () => {
                toast.error('Failed to load Jitsi. Please try again.');
                setIsInCall(false);
            };
            document.body.appendChild(script);
        }
    }, [isInCall, streamInfo, router]);

    if (isLoading) {
        return (
            <div style={{
                minHeight: '100vh',
                background: 'linear-gradient(135deg, #0f0f23 0%, #1a1a2e 100%)',
                display: 'flex', alignItems: 'center', justifyContent: 'center',
                color: 'white', fontFamily: "'Inter', sans-serif"
            }}>
                <div style={{ textAlign: 'center' }}>
                    <div style={{ fontSize: '48px', marginBottom: '16px', animation: 'pulse 2s infinite' }}>📡</div>
                    <h2 style={{ fontSize: '20px', fontWeight: 600 }}>Connecting to live class...</h2>
                </div>
            </div>
        );
    }

    if (!streamInfo) {
        return (
            <div style={{
                minHeight: '100vh',
                background: 'linear-gradient(135deg, #0f0f23 0%, #1a1a2e 100%)',
                display: 'flex', alignItems: 'center', justifyContent: 'center',
                color: 'white', fontFamily: "'Inter', sans-serif"
            }}>
                <div style={{ textAlign: 'center' }}>
                    <div style={{ fontSize: '48px', marginBottom: '16px' }}>📴</div>
                    <h2>Class is not live</h2>
                    <button onClick={() => router.push('/student/dashboard')} style={{
                        marginTop: '24px', padding: '12px 24px', background: '#3b82f6',
                        color: 'white', border: 'none', borderRadius: '8px', cursor: 'pointer',
                        fontSize: '14px', fontWeight: 600
                    }}>← Back to Dashboard</button>
                </div>
            </div>
        );
    }

    // When in call, show full-screen Jitsi iframe
    if (isInCall) {
        return (
            <div style={{ width: '100vw', height: '100vh', background: '#000' }}>
                <div ref={jitsiContainerRef} style={{ width: '100%', height: '100%' }} />
            </div>
        );
    }

    // Pre-join lobby
    return (
        <div style={{
            minHeight: '100vh',
            background: 'linear-gradient(135deg, #0f0f23 0%, #1a1a2e 100%)',
            display: 'flex', flexDirection: 'column',
            alignItems: 'center', justifyContent: 'center',
            color: 'white', fontFamily: "'Inter', sans-serif",
            padding: '24px', textAlign: 'center'
        }}>
            <div style={{
                background: 'rgba(255,255,255,0.05)',
                borderRadius: '20px',
                padding: '40px',
                maxWidth: '420px',
                width: '100%',
                border: '1px solid rgba(255,255,255,0.1)',
                backdropFilter: 'blur(10px)'
            }}>
                {/* Live indicator */}
                <div style={{
                    display: 'inline-flex', alignItems: 'center', gap: '8px',
                    background: 'rgba(239, 68, 68, 0.15)', padding: '8px 16px',
                    borderRadius: '20px', border: '1px solid rgba(239, 68, 68, 0.3)',
                    marginBottom: '24px'
                }}>
                    <span style={{
                        width: '8px', height: '8px', background: '#ef4444',
                        borderRadius: '50%', display: 'inline-block',
                        animation: 'pulse 2s infinite'
                    }} />
                    <span style={{ color: '#ef4444', fontWeight: 700, fontSize: '13px' }}>LIVE</span>
                    <span style={{ color: '#ccc', fontSize: '13px' }}>{elapsed}</span>
                </div>

                <h1 style={{ fontSize: '22px', fontWeight: 700, marginBottom: '8px' }}>
                    {streamInfo.courseName || 'Live Class'}
                </h1>
                <p style={{ color: '#888', fontSize: '14px', marginBottom: '8px' }}>
                    Lecturer: {streamInfo.lecturerName}
                </p>
                <p style={{ color: '#666', fontSize: '12px', marginBottom: '32px' }}>
                    Session #{streamInfo.sessionId}
                </p>

                <button
                    onClick={handleJoinClass}
                    style={{
                        width: '100%',
                        padding: '16px 32px',
                        background: 'linear-gradient(135deg, #3b82f6, #8b5cf6)',
                        color: 'white',
                        border: 'none',
                        borderRadius: '12px',
                        cursor: 'pointer',
                        fontSize: '16px',
                        fontWeight: 700,
                        transition: 'transform 0.2s, box-shadow 0.2s',
                        boxShadow: '0 4px 15px rgba(59, 130, 246, 0.4)'
                    }}
                >
                    🎥 Join Live Class
                </button>

                <p style={{ color: '#666', fontSize: '11px', marginTop: '16px' }}>
                    You will be taken to the live classroom. Audio & video start muted.
                </p>

                <button
                    onClick={() => router.push('/student/dashboard')}
                    style={{
                        marginTop: '16px',
                        padding: '10px 24px',
                        background: 'transparent',
                        color: '#888',
                        border: '1px solid rgba(255,255,255,0.1)',
                        borderRadius: '10px',
                        cursor: 'pointer',
                        fontSize: '13px',
                        width: '100%'
                    }}
                >
                    ← Back to Dashboard
                </button>
            </div>

            <style jsx global>{`
                @keyframes pulse {
                    0%, 100% { opacity: 1; }
                    50% { opacity: 0.3; }
                }
            `}</style>
        </div>
    );
}
