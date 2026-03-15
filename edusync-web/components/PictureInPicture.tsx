'use client';

import { useState, useRef, useCallback, useEffect } from 'react';

interface PictureInPictureProps {
    streamUrl: string;
    isVisible: boolean;
}

export default function PictureInPicture({ streamUrl, isVisible }: PictureInPictureProps) {
    // Position & size state
    const [pos, setPos] = useState({ x: 20, y: 20 });
    const [size, setSize] = useState({ w: 280, h: 210 });
    const [isMinimized, setIsMinimized] = useState(false);
    const [isDragging, setIsDragging] = useState(false);
    const [isResizing, setIsResizing] = useState(false);
    const dragOffset = useRef({ x: 0, y: 0 });
    const resizeStart = useRef({ x: 0, y: 0, w: 0, h: 0 });
    const containerRef = useRef<HTMLDivElement>(null);

    // --- DRAG LOGIC ---
    const onDragStart = useCallback((e: React.MouseEvent | React.TouchEvent) => {
        e.preventDefault();
        const clientX = 'touches' in e ? e.touches[0].clientX : e.clientX;
        const clientY = 'touches' in e ? e.touches[0].clientY : e.clientY;
        dragOffset.current = { x: clientX - pos.x, y: clientY - pos.y };
        setIsDragging(true);
    }, [pos]);

    const onDragMove = useCallback((e: MouseEvent | TouchEvent) => {
        if (!isDragging) return;
        const clientX = 'touches' in e ? e.touches[0].clientX : e.clientX;
        const clientY = 'touches' in e ? e.touches[0].clientY : e.clientY;
        const newX = Math.max(0, Math.min(window.innerWidth - size.w, clientX - dragOffset.current.x));
        const newY = Math.max(0, Math.min(window.innerHeight - size.h, clientY - dragOffset.current.y));
        setPos({ x: newX, y: newY });
    }, [isDragging, size]);

    const onDragEnd = useCallback(() => {
        setIsDragging(false);
    }, []);

    // --- RESIZE LOGIC ---
    const onResizeStart = useCallback((e: React.MouseEvent | React.TouchEvent) => {
        e.preventDefault();
        e.stopPropagation();
        const clientX = 'touches' in e ? e.touches[0].clientX : e.clientX;
        const clientY = 'touches' in e ? e.touches[0].clientY : e.clientY;
        resizeStart.current = { x: clientX, y: clientY, w: size.w, h: size.h };
        setIsResizing(true);
    }, [size]);

    const onResizeMove = useCallback((e: MouseEvent | TouchEvent) => {
        if (!isResizing) return;
        const clientX = 'touches' in e ? e.touches[0].clientX : e.clientX;
        const clientY = 'touches' in e ? e.touches[0].clientY : e.clientY;
        const dw = clientX - resizeStart.current.x;
        const dh = clientY - resizeStart.current.y;
        const newW = Math.max(160, Math.min(window.innerWidth * 0.6, resizeStart.current.w + dw));
        const newH = Math.max(120, Math.min(window.innerHeight * 0.6, resizeStart.current.h + dh));
        setSize({ w: newW, h: newH });
    }, [isResizing]);

    const onResizeEnd = useCallback(() => {
        setIsResizing(false);
    }, []);

    // Attach global mouse/touch listeners
    useEffect(() => {
        if (isDragging) {
            window.addEventListener('mousemove', onDragMove);
            window.addEventListener('mouseup', onDragEnd);
            window.addEventListener('touchmove', onDragMove);
            window.addEventListener('touchend', onDragEnd);
        }
        return () => {
            window.removeEventListener('mousemove', onDragMove);
            window.removeEventListener('mouseup', onDragEnd);
            window.removeEventListener('touchmove', onDragMove);
            window.removeEventListener('touchend', onDragEnd);
        };
    }, [isDragging, onDragMove, onDragEnd]);

    useEffect(() => {
        if (isResizing) {
            window.addEventListener('mousemove', onResizeMove);
            window.addEventListener('mouseup', onResizeEnd);
            window.addEventListener('touchmove', onResizeMove);
            window.addEventListener('touchend', onResizeEnd);
        }
        return () => {
            window.removeEventListener('mousemove', onResizeMove);
            window.removeEventListener('mouseup', onResizeEnd);
            window.removeEventListener('touchmove', onResizeMove);
            window.removeEventListener('touchend', onResizeEnd);
        };
    }, [isResizing, onResizeMove, onResizeEnd]);

    // Double-click to toggle size
    const toggleSize = () => {
        if (size.w > 300) {
            setSize({ w: 200, h: 150 });
        } else {
            setSize({ w: 480, h: 360 });
        }
    };

    if (!isVisible || isMinimized) {
        // Show a small floating button to restore
        if (isMinimized) {
            return (
                <button
                    onClick={() => setIsMinimized(false)}
                    style={{
                        position: 'fixed',
                        bottom: '20px',
                        right: '20px',
                        zIndex: 10000,
                        padding: '12px 16px',
                        background: 'linear-gradient(135deg, #3b82f6, #8b5cf6)',
                        color: 'white',
                        border: 'none',
                        borderRadius: '12px',
                        cursor: 'pointer',
                        fontSize: '14px',
                        fontWeight: 700,
                        boxShadow: '0 4px 20px rgba(0,0,0,0.4)',
                    }}
                >
                    📹 Show Lecturer
                </button>
            );
        }
        return null;
    }

    return (
        <div
            ref={containerRef}
            style={{
                position: 'fixed',
                left: `${pos.x}px`,
                top: `${pos.y}px`,
                width: `${size.w}px`,
                height: `${size.h}px`,
                zIndex: 9999,
                borderRadius: '12px',
                overflow: 'hidden',
                boxShadow: '0 8px 32px rgba(0, 0, 0, 0.6)',
                border: '2px solid rgba(255, 255, 255, 0.2)',
                background: '#000',
                cursor: isDragging ? 'grabbing' : 'grab',
                userSelect: 'none',
                transition: isDragging || isResizing ? 'none' : 'width 0.3s, height 0.3s',
            }}
        >
            {/* Drag handle bar */}
            <div
                onMouseDown={onDragStart}
                onTouchStart={onDragStart}
                onDoubleClick={toggleSize}
                style={{
                    position: 'absolute',
                    top: 0,
                    left: 0,
                    right: 0,
                    height: '32px',
                    background: 'linear-gradient(180deg, rgba(0,0,0,0.7), transparent)',
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'space-between',
                    padding: '0 8px',
                    zIndex: 2,
                    cursor: isDragging ? 'grabbing' : 'grab',
                }}
            >
                <span style={{ color: 'white', fontSize: '11px', fontWeight: 600, opacity: 0.8 }}>
                    📹 Lecturer Camera
                </span>
                <button
                    onClick={(e) => { e.stopPropagation(); setIsMinimized(true); }}
                    style={{
                        background: 'rgba(255,255,255,0.2)',
                        border: 'none',
                        color: 'white',
                        cursor: 'pointer',
                        borderRadius: '4px',
                        padding: '2px 6px',
                        fontSize: '12px',
                    }}
                >
                    —
                </button>
            </div>

            {/* MJPEG stream image */}
            <img
                src={streamUrl}
                alt="Lecturer Camera"
                style={{
                    width: '100%',
                    height: '100%',
                    objectFit: 'cover',
                    pointerEvents: 'none',
                }}
                onError={(e) => {
                    // Show placeholder on error
                    (e.target as HTMLImageElement).style.display = 'none';
                }}
            />

            {/* Resize handle (bottom-right corner) */}
            <div
                onMouseDown={onResizeStart}
                onTouchStart={onResizeStart}
                style={{
                    position: 'absolute',
                    bottom: 0,
                    right: 0,
                    width: '20px',
                    height: '20px',
                    cursor: 'nwse-resize',
                    background: 'linear-gradient(135deg, transparent 50%, rgba(255,255,255,0.3) 50%)',
                    zIndex: 2,
                }}
            />
        </div>
    );
}
