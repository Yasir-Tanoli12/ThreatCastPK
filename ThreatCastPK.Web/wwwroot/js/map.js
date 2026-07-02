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
        'bahawalpur': [29.3956, 71.6836],
        'sargodha': [32.0836, 72.6711],
        'sheikhupura': [31.7167, 73.9850],
        'rahim yar khan': [28.4202, 70.2952],
        'jhang': [31.2681, 72.3181],
        'dera ghazi khan': [30.0588, 70.6350],
        'gujrat': [32.5736, 74.0790],
        'sahiwal': [30.6706, 73.1064],
        'wah cantonment': [33.7667, 72.7167],
        'kasur': [31.1167, 74.4500],
        'okara': [30.8138, 73.4534],
        'chiniot': [31.7167, 72.9833],
        'kamoke': [31.9742, 74.2228],
        'hafizabad': [32.0714, 73.6883],
        'khanewal': [30.3014, 71.9322],
        'bahawalnagar': [29.9922, 73.2546],
        'pakpattan': [30.3436, 73.3887],
        'mandi bahauddin': [32.5862, 73.4917],
        'jhelum': [32.9361, 73.7258],
        'khushab': [32.2986, 72.3522],
        'attock': [33.7664, 72.3602],
        'chakwal': [32.9328, 72.8557],
        'toba tek singh': [30.9709, 72.4826],
        'vehari': [30.0454, 72.3513],
        'muzaffargarh': [30.0736, 71.1930],
        'lodhran': [29.5343, 71.6322],
        'layyah': [30.9614, 70.9378],
        'rajanpur': [29.1044, 70.3296],
        'sukkur': [27.7052, 68.8574],
        'larkana': [27.5570, 68.2247],
        'mirpur khas': [25.5270, 69.0138],
        'nawabshah': [26.2442, 68.4100],
        'jacobabad': [28.2769, 68.4514],
        'shikarpur': [27.9558, 68.6378],
        'khairpur': [27.5295, 68.7592],
        'dadu': [26.7319, 67.7750],
        'thatta': [24.7461, 67.9239],
        'badin': [24.6557, 68.8397],
        'tharparkar': [24.7136, 70.2461],
        'sanghar': [26.0461, 68.9483],
        'matiari': [25.5942, 68.4611],
        'mardan': [34.1985, 72.0404],
        'mingora': [34.7717, 72.3600],
        'dera ismail khan': [31.8314, 70.9019],
        'kohat': [33.5869, 71.4414],
        'bannu': [32.9889, 70.6042],
        'swabi': [34.1197, 72.4697],
        'nowshera': [34.0153, 71.9747],
        'charsadda': [34.1483, 71.7306],
        'mansehra': [34.3333, 73.2000],
        'haripur': [33.9942, 72.9353],
        'karak': [33.1167, 71.0833],
        'tank': [32.2189, 70.3775],
        'chitral': [35.8511, 71.7864],
        'turbat': [26.0025, 63.0422],
        'khuzdar': [27.8000, 66.6167],
        'hub': [25.0550, 66.9908],
        'chaman': [30.9200, 66.4597],
        'gwadar': [25.1264, 62.3225],
        'dera bugti': [29.0333, 69.1667],
        'sibi': [29.5433, 67.8775],
        'zhob': [31.3417, 69.4486],
        'nushki': [29.5522, 66.0206],
        'panjgur': [26.9683, 64.0992],
        'muzaffarabad': [34.3700, 73.4700],
        'mirpur': [33.1467, 73.7508],
        'rawalakot': [33.8578, 73.7622],
        'gilgit': [35.9208, 74.3083],
        'skardu': [35.2972, 75.6333],
        'hunza': [36.3167, 74.6500],
        'wah': [33.7667, 72.7167],
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
            zoomControl: false,
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

        const classification = (eventData.greyNoiseClassification || '').toLowerCase();
        const isBackgroundNoise = classification === 'benign';
        const noiseBadge = isBackgroundNoise
            ? `<div style="margin-top: 8px; padding: 3px 6px; background: rgba(100, 116, 139, 0.15); border: 1px solid rgba(100, 116, 139, 0.35); border-radius: 3px; font-family: 'IBM Plex Mono', monospace; font-size: 9px; color: #94a3b8; letter-spacing: 0.5px;">🔊 BACKGROUND NOISE — Internet Scanner</div>`
            : '';

        const popupContent = `
        <div style="font-family: 'Inter', sans-serif; font-size: 12px; line-height: 1.5; color: #c9d1d9; min-width: 170px; padding: 2px;">
            <div style="display: flex; align-items: center; justify-content: space-between; border-bottom: 1px solid #21262d; padding-bottom: 6px; margin-bottom: 8px;">
                <strong style="color: ${color}; font-family: 'IBM Plex Mono', monospace; font-size: 11px; letter-spacing: 0.5px;">${attackType.toUpperCase()}</strong>
                <span style="background: rgba(${rgb}, 0.15); color: ${color}; border: 1px solid rgba(${rgb}, 0.3); padding: 1px 6px; border-radius: 3px; font-weight: bold; font-size: 9px; font-family: 'IBM Plex Mono', monospace;">SEV ${severity}</span>
            </div>
            <div style="margin-bottom: 4px;"><span style="color: #8b949e;">Location:</span> <strong>${eventData.city || 'Unknown'}</strong></div>
            <div style="margin-bottom: 4px;"><span style="color: #8b949e;">Target Sector:</span> <strong>${eventData.targetSector || 'N/A'}</strong></div>
            <div><span style="color: #8b949e;">Time:</span> <strong>${dateStr}</strong></div>
            ${noiseBadge}
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
                occurredAt: p.occurredAt,
                greyNoiseClassification: p.greyNoiseClassification || null
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
            occurredAt: new Date().toISOString(),
            greyNoiseClassification: greyNoiseClassification || null
        };;

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