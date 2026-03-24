'use client';

import { useEffect, useState } from 'react';
import { useParams, useRouter } from 'next/navigation';
import { studentApi } from '@/lib/studentApi';
import toast from 'react-hot-toast';

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

    const handleJoinClass = () => {
        if (!streamInfo) return;

        const studentUser = typeof window !== 'undefined'
            ? JSON.parse(localStorage.getItem('studentUser') || '{"fullName":"Student"}')
            : { fullName: 'Student' };

        const displayName = encodeURIComponent(studentUser.fullName);
        
        // Build return URL for when call ends — redirect back to student dashboard
        const returnUrl = encodeURIComponent(window.location.origin + '/student/dashboard');
        
        // Navigate the current tab directly to the Jitsi URL
        // This is the most reliable method — no iframes, no popups, no deep-link interception
        // The browser navigates directly to Jitsi like visiting any website
        const jitsiUrl = `https://meet.viicsoft.dev/${streamInfo.roomName}` +
            `#config.prejoinConfig.enabled=false` +
            `&config.prejoinPageEnabled=false` +
            `&config.startWithAudioMuted=true` +
            `&config.startWithVideoMuted=true` +
            `&config.disableDeepLinking=true` +
            `&config.disableInitialGUM=true` +
            `&config.hideConferenceSubject=true` +
            `&config.enableInsecureRoomNameWarning=false` +
            `&config.enableClosePage=true` +
            `&config.redirectOnHangup=${returnUrl}` +
            `&config.disableInviteFunctions=true` +
            `&config.displayName=${displayName}` +
            `&userInfo.displayName=${displayName}` +
            `&interfaceConfig.SHOW_JITSI_WATERMARK=false` +
            `&interfaceConfig.SHOW_WATERMARK_FOR_GUESTS=false` +
            `&interfaceConfig.SHOW_BRAND_WATERMARK=false` +
            `&interfaceConfig.SHOW_POWERED_BY=false` +
            `&interfaceConfig.MOBILE_APP_PROMO=false` +
            `&interfaceConfig.APP_NAME=EduSync+AI` +
            `&interfaceConfig.NATIVE_APP_NAME=EduSync+AI` +
            `&interfaceConfig.PROVIDER_NAME=EduSync+AI`;
        
        // Navigate in the SAME tab — this is the key difference
        // Unlike window.open() or iframes, this works exactly like the user
        // typing the URL directly into the browser's address bar
        window.location.href = jitsiUrl;
    };

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
