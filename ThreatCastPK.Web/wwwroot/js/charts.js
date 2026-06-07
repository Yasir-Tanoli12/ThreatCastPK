// ThreatCastPK.Web/wwwroot/js/charts.js
window.tcpkCharts = {

    _cityChart: null,
    _typeChart: null,
    _trendChart: null,

    renderAll: function (cityLabels, cityData, typeLabels, typeValues, typeColors, trendLabels, trendData) {

        const gridColor = 'rgba(33,38,45,0.8)';
        const textColor = '#6e7681';
        const font = { family: "'IBM Plex Mono', monospace", size: 11 };

        // Destroy existing
        if (this._cityChart) { this._cityChart.destroy(); this._cityChart = null; }
        if (this._typeChart) { this._typeChart.destroy(); this._typeChart = null; }
        if (this._trendChart) { this._trendChart.destroy(); this._trendChart = null; }

        // Bar chart — attacks by city
        const cityCtx = document.getElementById('cityChart');
        if (cityCtx) {
            this._cityChart = new Chart(cityCtx, {
                type: 'bar',
                data: {
                    labels: cityLabels,
                    datasets: [{
                        data: cityData,
                        backgroundColor: 'rgba(159,239,0,0.15)',
                        borderColor: '#9fef00',
                        borderWidth: 1,
                        borderRadius: 3,
                        borderSkipped: false,
                    }]
                },
                options: {
                    responsive: true, maintainAspectRatio: false,
                    plugins: {
                        legend: { display: false }, tooltip: {
                            backgroundColor: '#161b22', borderColor: '#21262d', borderWidth: 1,
                            titleColor: '#9fef00', bodyColor: '#c9d1d9',
                            titleFont: font, bodyFont: font,
                        }
                    },
                    scales: {
                        x: { grid: { color: gridColor }, ticks: { color: textColor, font } },
                        y: { grid: { color: gridColor }, ticks: { color: textColor, font }, beginAtZero: true }
                    }
                }
            });
        }

        // Donut chart — attack types
        const typeCtx = document.getElementById('typeChart');
        if (typeCtx) {
            this._typeChart = new Chart(typeCtx, {
                type: 'doughnut',
                data: {
                    labels: typeLabels,
                    datasets: [{
                        data: typeValues,
                        backgroundColor: typeColors.map(c => c + '33'),
                        borderColor: typeColors,
                        borderWidth: 1,
                        hoverOffset: 6,
                    }]
                },
                options: {
                    responsive: true, maintainAspectRatio: false,
                    cutout: '68%',
                    plugins: {
                        legend: { display: false },
                        tooltip: {
                            backgroundColor: '#161b22', borderColor: '#21262d', borderWidth: 1,
                            titleColor: '#9fef00', bodyColor: '#c9d1d9',
                            titleFont: font, bodyFont: font,
                        }
                    }
                }
            });
        }

        // Line chart — 30-day trend
        const trendCtx = document.getElementById('trendChart');
        if (trendCtx) {
            this._trendChart = new Chart(trendCtx, {
                type: 'line',
                data: {
                    labels: trendLabels,
                    datasets: [{
                        data: trendData,
                        borderColor: '#9fef00',
                        backgroundColor: 'rgba(159,239,0,0.05)',
                        borderWidth: 2,
                        pointRadius: 0,
                        pointHoverRadius: 4,
                        pointHoverBackgroundColor: '#9fef00',
                        fill: true,
                        tension: 0.4,
                    }]
                },
                options: {
                    responsive: true, maintainAspectRatio: false,
                    plugins: {
                        legend: { display: false }, tooltip: {
                            backgroundColor: '#161b22', borderColor: '#21262d', borderWidth: 1,
                            titleColor: '#9fef00', bodyColor: '#c9d1d9',
                            titleFont: font, bodyFont: font,
                            mode: 'index', intersect: false,
                        }
                    },
                    scales: {
                        x: {
                            grid: { color: gridColor },
                            ticks: { color: textColor, font, maxTicksLimit: 8 }
                        },
                        y: {
                            grid: { color: gridColor },
                            ticks: { color: textColor, font },
                            beginAtZero: true
                        }
                    }
                }
            });
        }
    }
};