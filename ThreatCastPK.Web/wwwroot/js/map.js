// ThreatCastPK.Web/wwwroot/js/map.js
// Leaflet custom zoom-adaptive marker integration for ThreatCast PK
// Called from Dashboard.razor via IJSRuntime

window.tcpkMap = {
    _map: null,
    _markers: [], // Array of objects: { marker: L.marker, eventData: {...} }

    // Fallback coordinates registry for major Pakistani cities to handle missing data
    _cityCoordinates: {
        'karachi': [24.8607, 67.0011],
        'lahore': [31.5204, 74.3587],
        'islamabad': [33.6844, 73.0479],
        'rawalpindi': [33.5651, 73.0169],
        'faisalabad': [31.4504, 73.1350],
        'multan': [30.1575, 71.5249],
        'peshawar': [34.0151, 71.5805],
        'quetta': [30.1798, 66.9750],
        'hyderabad': [25.3960, 68.3578],
        'gujranwala': [32.1877, 74.1945],
        'sialkot': [32.4945, 74.5229],
        'abbottabad': [34.1463, 73.2117]
    },

    // Helper to convert hex to RGB for semi-transparent alpha overlays
    _hexToRgb: function (hex) {
        const shorthandRegex = /^#?([a-f\d])([a-f\d])([a-f\d])$/i;
        hex = hex.replace(shorthandRegex, (m, r, g, b) => r + r + g + g + b + b);
        const result = /^#?([a-f\d]{2})([a-f\d]{2})([a-f\d]{2})$/i.exec(hex);
        return result ? `${parseInt(result[1], 16)}, ${parseInt(result[2], 16)}, ${parseInt(result[3], 16)}` : '255, 255, 255';
    },

    // Map attack types to specific high-fidelity colors
    _getColorForAttack: function (attackType) {
        if (!attackType) return '#34c759'; // Emerald default
        switch (attackType.toLowerCase()) {
            case 'ransomware':
                return '#ff3b30'; // Premium Red
            case 'ddos':
                return '#00c8ff'; // Neon Cyan/Blue
            case 'phishing':
                return '#ffcc00'; // Gold/Yellow
            case 'malware':
                return '#af52de'; // Purple
            case 'identitytheft':
            case 'databreach':
            case 'data breach':
                return '#ff9500'; // Orange
            case 'apt':
                return '#ff6b35'; // Deep coral/red-orange
            default:
                return '#34c759'; // Emerald green
        }
    },

    // Initialize the Leaflet map
    init: function (containerId) {
        if (this._map) {
            this.clearHeatmap();
            this._map.remove();
            this._map = null;
        }

        const container = document.getElementById(containerId);
        if (!container) {
            console.error('[Map] Container not found:', containerId);
            return;
        }

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

        this._map.fitBounds(pakistanBounds);

        // Listen for zoom changes to adapt marker styles
        this._map.on('zoomend', () => {
            this._updateMarkerStyles();
        });

        console.log('[Map] Initialized with custom adaptive marker system.');
    },

    // Build the zoom-dependent marker icon based on event details
    _getMarkerIcon: function (eventData, zoom) {
        const attackType = eventData.attackType || 'Other';
        const severity = eventData.severity || 1;
        const color = this._getColorForAttack(attackType);
        const rgb = this._hexToRgb(color);

        // Base size determined dynamically by zoom level
        let baseSize = 8;
        if (zoom <= 6) {
            baseSize = 22;
        } else if (zoom <= 8) {
            baseSize = 14;
        } else {
            baseSize = 8;
        }

        const size = baseSize + (severity * 2.5); // Graceful size scaling

        // Dynamic blur and shadow size to maintain the soft glow look at all levels
        let blurPx = 4;
        let shadowPx = severity * 4;
        if (zoom <= 6) {
            blurPx = 6;
            shadowPx = severity * 6;
        } else if (zoom <= 8) {
            blurPx = 3;
            shadowPx = severity * 3;
        } else {
            blurPx = 1.5;
            shadowPx = severity * 1.5;
        }

        // HTML for the soft glowing threat blob
        const iconHtml = `
            <div style="
                width: ${size}px; height: ${size}px;
                border-radius: 50%;
                background: radial-gradient(circle, rgba(${rgb}, 0.85) 0%, rgba(${rgb}, 0.35) 50%, rgba(${rgb}, 0) 100%);
                box-shadow: 0 0 ${shadowPx}px ${shadowPx / 2}px rgba(${rgb}, 0.65);
                filter: blur(${blurPx}px);
                animation: breathingBlob 3s ease-in-out infinite;
            "></div>
        `;

        return L.divIcon({
            className: 'adaptive-threat-marker',
            html: iconHtml,
            iconSize: [size, size],
            iconAnchor: [size / 2, size / 2] // Always anchor center for soft glowing blobs
        });
    },

    // Bind custom styled popups
    _bindMarkerPopup: function (marker, eventData) {
        const attackType = eventData.attackType || 'Other';
        const severity = eventData.severity || 1;
        const color = this._getColorForAttack(attackType);
        const rgb = this._hexToRgb(color);
        const dateStr = eventData.occurredAt ? new Date(eventData.occurredAt).toLocaleTimeString() : new Date().toLocaleTimeString();

        const popupContent = `
            <div style="font-family: 'Inter', sans-serif; font-size: 12px; line-height: 1.5; color: #c9d1d9; min-width: 170px; padding: 2px;">
                <div style="display: flex; align-items: center; justify-content: space-between; border-bottom: 1px solid #21262d; padding-bottom: 6px; margin-bottom: 8px;">
                    <strong style="color: ${color}; font-family: 'IBM Plex Mono', monospace; font-size: 11px; letter-spacing: 0.5px;">${attackType.toUpperCase()}</strong>
                    <span style="background: rgba(${rgb}, 0.15); color: ${color}; border: 1px solid rgba(${rgb}, 0.3); padding: 1px 6px; border-radius: 3px; font-weight: bold; font-size: 9px; font-family: 'IBM Plex Mono', monospace;">SEV ${severity}</span>
                </div>
                <div style="margin-bottom: 4px;"><span style="color: #8b949e;">Location:</span> <strong>${eventData.city || 'Unknown'}</strong></div>
                <div style="margin-bottom: 4px;"><span style="color: #8b949e;">Target Sector:</span> <strong>${eventData.targetSector || 'N/A'}</strong></div>
                <div><span style="color: #8b949e;">Time:</span> <strong>${dateStr}</strong></div>
            </div>
        `;
        marker.bindPopup(popupContent);
    },

    // Loop through markers and refresh icons based on current zoom
    _updateMarkerStyles: function () {
        if (!this._map) return;
        const zoom = this._map.getZoom();
        this._markers.forEach(m => {
            const newIcon = this._getMarkerIcon(m.eventData, zoom);
            m.marker.setIcon(newIcon);
        });
    },

    // Load initial batch of map points
    setHeatmapData: function (points) {
        if (!this._map) return;
        this.clearHeatmap();

        const zoom = this._map.getZoom();

        points.forEach(p => {
            let lat = p.lat;
            let lng = p.lng;

            // Fallback lookup using city name if coordinates are missing (0,0)
            if (lat === 0 && lng === 0 && p.city) {
                const coords = this._cityCoordinates[p.city.toLowerCase()];
                if (coords) {
                    lat = coords[0];
                    lng = coords[1];
                }
            }

            if (lat === 0 && lng === 0) return; // skip if still invalid

            const eventData = {
                lat: lat,
                lng: lng,
                severity: p.severity,
                attackType: p.attackType,
                city: p.city || 'Unknown',
                targetSector: p.targetSector || 'N/A',
                occurredAt: p.occurredAt
            };

            const icon = this._getMarkerIcon(eventData, zoom);
            const marker = L.marker([lat, lng], { icon: icon }).addTo(this._map);
            this._bindMarkerPopup(marker, eventData);

            this._markers.push({
                marker: marker,
                eventData: eventData
            });
        });

        console.log(`[Map] Plotted ${this._markers.length} adaptive markers.`);
    },

    // Add a single new point (called on SignalR event)
    addHeatPoint: function (lat, lng, severity, attackType, city) {
        if (!this._map) return;

        let eventLat = lat;
        let eventLng = lng;

        // Fallback lookup using city name if coordinates are missing (0,0)
        if (eventLat === 0 && eventLng === 0 && city) {
            const coords = this._cityCoordinates[city.toLowerCase()];
            if (coords) {
                eventLat = coords[0];
                eventLng = coords[1];
            }
        }

        if (eventLat === 0 && eventLng === 0) return;

        const zoom = this._map.getZoom();
        const eventData = {
            lat: eventLat,
            lng: eventLng,
            severity: severity,
            attackType: attackType,
            city: city || 'Unknown',
            occurredAt: new Date().toISOString()
        };

        const icon = this._getMarkerIcon(eventData, zoom);
        const marker = L.marker([eventLat, eventLng], { icon: icon }).addTo(this._map);
        this._bindMarkerPopup(marker, eventData);

        this._markers.push({
            marker: marker,
            eventData: eventData
        });

        // Limit to 500 markers to maintain top performance
        if (this._markers.length > 500) {
            const oldest = this._markers.shift();
            this._map.removeLayer(oldest.marker);
        }

        // Animate a pulse ripple at the event location
        this._pulseAt(eventLat, eventLng, severity, attackType);
    },

    // Clear all markers
    clearHeatmap: function () {
        this._markers.forEach(m => {
            if (this._map) this._map.removeLayer(m.marker);
        });
        this._markers = [];
    },

    // Ripple pulse animation for new events
    _pulseAt: function (lat, lng, severity, attackType) {
        const color = this._getColorForAttack(attackType);

        const pulseIcon = L.divIcon({
            className: '',
            html: `<div style="
                width: 24px; height: 24px; border-radius: 50%;
                border: 2px solid ${color};
                animation: mapPulse 1.6s ease-out forwards;
                box-shadow: 0 0 8px ${color};
                opacity: 0.9;
            "></div>`,
            iconSize: [24, 24],
            iconAnchor: [12, 12]
        });

        const marker = L.marker([lat, lng], { icon: pulseIcon }).addTo(this._map);

        setTimeout(() => {
            if (this._map) this._map.removeLayer(marker);
        }, 1700);
    }
};

// Inject breath & pulse keyframes into page header
(function () {
    const style = document.createElement('style');
    style.textContent = `
        @keyframes mapPulse {
            0%   { transform: scale(0.4); opacity: 0.9; }
            100% { transform: scale(3.5);   opacity: 0; }
        }
        @keyframes breathingBlob {
            0% { transform: scale(0.96); opacity: 0.85; }
            50% { transform: scale(1.04); opacity: 1.0; }
            100% { transform: scale(0.96); opacity: 0.85; }
        }
    `;
    document.head.appendChild(style);
})();