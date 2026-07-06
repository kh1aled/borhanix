(function () {
    const trackers = new Map();

    function getToken() {
        const input = document.querySelector('input[name="__RequestVerificationToken"]');
        return input ? input.value : document.querySelector('meta[name="csrf-token"]')?.content;
    }

    function sendProgress(courseId, lessonId, maxWatchedSeconds) {
        const token = getToken();
        return fetch('/Lessons/TrackProgress', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': token || ''
            },
            body: JSON.stringify({ courseId, lessonId, maxWatchedSeconds })
        }).then(r => r.ok ? r.json() : null);
    }

    // Old markup nested the video/content block and its progress UI inside the same
    // [data-lesson-row] element, so closest() worked. New markup renders the player
    // (cloned from a <template>) in a separate container from the sidebar row that
    // holds the progress bar, so we look the row up by lesson id instead of walking
    // up the DOM. Both selectors are kept so this still works with either markup.
    function findRow(lessonId) {
        return document.querySelector(`[data-lesson-row="${lessonId}"], .lesson-item-row[data-lesson-id="${lessonId}"]`);
    }

    function initVideoTracker(video) {
        if (trackers.has(video)) return;
        const courseId = Number(video.dataset.courseId);
        const lessonId = Number(video.dataset.lessonId);
        const duration = Number(video.dataset.durationSeconds || 0);
        const row = findRow(lessonId);
        if (!row || !courseId || !lessonId || video.dataset.tracking === 'off') return;

        let maxWatched = Number(video.dataset.initialMax || 0);
        let pendingSend = null;
        let lastTime = 0;
        let lastSent = maxWatched;
        let intervalId = null;

        function onTimeUpdate() {
            const current = video.currentTime;
            const jumpedForward = current > lastTime + 2.5 && current > maxWatched + 2;
            if (!jumpedForward && current > maxWatched) {
                maxWatched = current;
            }
            lastTime = current;
            updateUi(lessonId, maxWatched, duration);
        }

        function flush() {
            const seconds = Math.floor(maxWatched);
            if (seconds <= lastSent || pendingSend) return pendingSend;

            pendingSend = sendProgress(courseId, lessonId, seconds).then(result => {
                if (!result) return null;

                lastSent = result.maxWatchedSeconds;
                if (result.isComplete) {
                    markComplete(row, lessonId, result.viewingPercent);
                    stopTrackingInterval();
                } else {
                    updateUi(lessonId, result.maxWatchedSeconds, duration, result.viewingPercent);
                }

                return result;
            }).finally(() => {
                pendingSend = null;
            });

            return pendingSend;
        }

        function flushBeacon() {
            const seconds = Math.floor(maxWatched);
            const token = getToken();
            if (seconds <= lastSent || !token) return;

            fetch('/Lessons/TrackProgress', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': token
                },
                body: JSON.stringify({ courseId, lessonId, maxWatchedSeconds: seconds }),
                keepalive: true
            });
        }

        function startTrackingInterval() {
            if (!intervalId) {
                intervalId = setInterval(flush, 12000);
            }
        }

        function stopTrackingInterval() {
            if (intervalId) {
                clearInterval(intervalId);
                intervalId = null;
            }
        }

        video.addEventListener('timeupdate', onTimeUpdate);
        video.addEventListener('play', startTrackingInterval);

        video.addEventListener('pause', () => {
            stopTrackingInterval();
            flush();
        });

        video.addEventListener('ended', () => {
            stopTrackingInterval();
            flush();
        });

        window.addEventListener('pagehide', flushBeacon);
        trackers.set(video, { flush, intervalId: () => intervalId });
    }

    function initContentTracker(block) {
        if (trackers.has(block)) return;
        const courseId = Number(block.dataset.courseId);
        const lessonId = Number(block.dataset.lessonId);
        const duration = Number(block.dataset.durationSeconds || 1800);
        const row = findRow(lessonId);
        if (!row || !courseId || !lessonId || block.dataset.tracking === 'off') return;

        let visibleSeconds = Number(block.dataset.initialMax || 0);
        let lastSent = visibleSeconds;
        let pendingSend = null;
        let visibleTimer = null;
        let apiIntervalId = null;

        function stopTimer() {
            if (visibleTimer) {
                clearInterval(visibleTimer);
                visibleTimer = null;
            }
        }

        function startApiInterval() {
            if (!apiIntervalId) {
                apiIntervalId = setInterval(() => {
                    const seconds = Math.floor(visibleSeconds);
                    if (seconds <= lastSent || pendingSend) return;

                    pendingSend = sendProgress(courseId, lessonId, seconds).then(result => {
                        if (!result) return;

                        lastSent = result.maxWatchedSeconds;
                        if (result.isComplete) {
                            markComplete(row, lessonId, result.viewingPercent);
                            observer.disconnect();
                            stopTimer();
                            stopApiInterval();
                        }
                    }).finally(() => {
                        pendingSend = null;
                    });
                }, 12000);
            }
        }

        function stopApiInterval() {
            if (apiIntervalId) {
                clearInterval(apiIntervalId);
                apiIntervalId = null;
            }
        }

        const observer = new IntersectionObserver(entries => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    if (!visibleTimer) {
                        visibleTimer = setInterval(() => {
                            visibleSeconds += 1;
                            updateUi(lessonId, visibleSeconds, duration);
                        }, 1000);
                    }
                    startApiInterval();
                } else {
                    stopTimer();
                    stopApiInterval();
                }
            });
        }, { threshold: 0.75 });

        observer.observe(block);

        trackers.set(block, {
            flush: () => sendProgress(courseId, lessonId, Math.floor(visibleSeconds)),
            intervalId: () => apiIntervalId
        });
    }

    function updateUi(lessonId, watched, duration, percentOverride) {
        const bar = document.querySelector(`[data-progress-bar="${lessonId}"]`);
        const label = document.querySelector(`[data-progress-label="${lessonId}"]`);
        const pct = percentOverride ?? (duration > 0 ? Math.min(100, (watched / duration) * 100) : 0);
        if (bar) bar.style.width = `${pct}%`;
        if (label) label.textContent = `${Math.round(pct)}% watched`;
    }

    function markComplete(row, lessonId, percent) {
        row.classList.add('lesson-complete');
        row.dataset.complete = 'true';

        // Old markup used a hidden "Completed" pill.
        const pill = row.querySelector('[data-complete-pill]');
        if (pill) pill.hidden = false;

        // New sidebar markup instead swaps the circle icon for a check icon.
        const iconHolder = row.querySelector('.lesson-status-icon-holder');
        if (iconHolder) {
            iconHolder.innerHTML = '<i class="w-4 h-4 text-green-500" data-lucide="check-circle-2"></i>';
            if (window.lucide && typeof window.lucide.createIcons === 'function') {
                window.lucide.createIcons();
            }
        }

        updateUi(lessonId, 0, 100, percent || 100);
    }

    function scan(root) {
        const scope = root || document;
        scope.querySelectorAll('video[data-lesson-id]').forEach(initVideoTracker);
        scope.querySelectorAll('[data-content-tracker]').forEach(initContentTracker);
    }

    // Initial scan (covers the old markup, where the video is already in the DOM).
    scan();

    // New markup clones a lesson's <template> into #lesson-player-container on click,
    // and dispatches this event afterward — re-scan just that container so the newly
    // inserted video/content element gets wired up.
    document.addEventListener('lesson-player:updated', () => {
        const container = document.getElementById('lesson-player-container');
        if (container) scan(container);
    });
})();