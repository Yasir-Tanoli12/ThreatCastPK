// ThreatCastPK.Web/wwwroot/js/map.js
// Leaflet + leaflet-heat integration for ThreatCast PK
// Called from Dashboard.razor via IJSRuntime

window.tcpkMap = {

    _map: null,
    _heatLayer: null,
    _heatPoints: [], // [lat, lng, intensity]

    // Initialize the Leaflet map
    init: function (containerId) {
        if (this._map) {
            this._map.remove();
            this._map = null;
            this._heatLayer = null;
        }

        const container = document.getElementById(containerId);
        if (!container) {
            console.error('[Map] Container not found:', containerId);
            return;
        }

        // Pakistan bounds
        const pakistanCenter = [30.3753, 69.3451];
        const pakistanBounds = [[23.6, 60.8], [37.1, 77.8]];

        this._map = L.map(containerId, {
            center: pakistanCenter,
            zoom: 6,
            minZoom: 5,
            maxZoom: 12,
            zoomControl: true,
            attributionControl: true,
        });

        // Dark tile layer — CartoDB Dark Matter
        L.tileLayer('https://{s}.basemaps.cartocdn.com/dark_all/{z}/{x}/{y}{r}.png', {
            attribution: '© <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> © <a href="https://carto.com/attributions">CARTO</a>',
            subdomains: 'abcd',
            maxZoom: 19
        }).addTo(this._map);

        // Fit to Pakistan
        this._map.fitBounds(pakistanBounds);

        // Initialize empty heatmap layer
        if (typeof L.heatLayer === 'function') {
            this._heatLayer = L.heatLayer([], {
                radius: 35,
                blur: 25,
                maxZoom: 10,
                max: 1.0,
                gradient: {
                    0.0: '#001f3f',
                    0.2: '#00c8ff',
                    0.4: '#9fef00',
                    0.6: '#ffa500',
                    0.8: '#ff6b35',
                    1.0: '#ff3e3e'
                }
            }).addTo(this._map);
        } else {
            console.warn('[Map] leaflet.heat not loaded. Heatmap unavailable.');
        }

        console.log('[Map] Initialized successfully.');
    },

    // Load initial batch of heatmap points
    // points: [{lat, lng, severity}]
    setHeatmapData: function (points) {
        if (!this._map || !this._heatLayer) return;

        // Convert severity (1-5) to intensity (0.0-1.0)
        this._heatPoints = points
            .filter(p => p.lat !== 0 && p.lng !== 0) // skip 0,0 coordinates
            .map(p => [p.lat, p.lng, p.severity / 5.0]);

        this._heatLayer.setLatLngs(this._heatPoints);

        console.log(`[Map] Loaded ${this._heatPoints.length} heatmap points.`);
    },

    // Add a single new point (called on SignalR event)
    addHeatPoint: function (lat, lng, severity) {
        if (!this._map || !this._heatLayer) return;
        if (lat === 0 && lng === 0) return; // skip invalid coords

        const intensity = severity / 5.0;
        this._heatPoints.push([lat, lng, intensity]);

        // Keep max 500 points to avoid memory issues
        if (this._heatPoints.length > 500)
            this._heatPoints.shift();

        this._heatLayer.setLatLngs(this._heatPoints);

        // Animate a ripple pulse at the new point
        this._pulseAt(lat, lng, severity);
    },

    // Clear all heatmap points (used when changing time filter)
    clearHeatmap: function () {
        if (!this._heatLayer) return;
        this._heatPoints = [];
        this._heatLayer.setLatLngs([]);
    },

    // Internal: ripple animation for new live events
    _pulseAt: function (lat, lng, severity) {
        const colors = ['#9fef00', '#7dc700', '#ffa500', '#ff6b35', '#ff3e3e'];
        const color = colors[Math.min(severity - 1, 4)];

        const pulseIcon = L.divIcon({
            className: '',
            html: `<div style="
                width: 20px; height: 20px; border-radius: 50%;
                border: 2px solid ${color};
                animation: mapPulse 1.5s ease-out forwards;
                opacity: 0.8;
            "></div>`,
            iconSize: [20, 20],
            iconAnchor: [10, 10]
        });

        const marker = L.marker([lat, lng], { icon: pulseIcon }).addTo(this._map);

        setTimeout(() => {
            if (this._map) this._map.removeLayer(marker);
        }, 1600);
    }
};

// Inject pulse keyframe animation into page
(function () {
    const style = document.createElement('style');
    style.textContent = `
        @keyframes mapPulse {
            0%   { transform: scale(0.5); opacity: 0.9; }
            100% { transform: scale(3);   opacity: 0; }
        }
    `;
    document.head.appendChild(style);
})();