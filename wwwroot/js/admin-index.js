(function () {
  function parseJsonAttr(el, attrName) {
    try {
      return JSON.parse(el.getAttribute(attrName) || '[]');
    } catch (_) {
      return [];
    }
  }

  function initSalesChart() {
    var canvas = document.getElementById('salesChart');
    if (!canvas || typeof Chart === 'undefined') {
      return;
    }

    var labels = parseJsonAttr(canvas, 'data-labels');
    var rawValues = parseJsonAttr(canvas, 'data-values');
    var values = Array.isArray(rawValues)
      ? rawValues.map(function (v) {
          var n = Number(v);
          return isNaN(n) ? 0 : n;
        })
      : [];

    if (!Array.isArray(labels) || labels.length === 0) {
      labels = ['-', '-', '-', '-', '-', '-', '-'];
    }
    if (values.length !== labels.length) {
      values = labels.map(function (_, i) {
        var n = Number(rawValues[i]);
        return isNaN(n) ? 0 : n;
      });
    }

    Chart.defaults.font.family = 'Cairo, sans-serif';

    new Chart(canvas, {
      type: 'line',
      data: {
        labels: labels,
        datasets: [
          {
            label: 'المبيعات (ج.م)',
            data: values,
            borderColor: '#2563EB',
            backgroundColor: 'rgba(37, 99, 235, 0.12)',
            borderWidth: 2,
            fill: true,
            tension: 0.35,
            pointRadius: 4,
            pointHoverRadius: 6,
            pointBackgroundColor: '#2563EB',
            pointBorderColor: '#fff',
            pointBorderWidth: 2,
          },
        ],
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        interaction: { intersect: false, mode: 'index' },
        plugins: {
          legend: {
            display: true,
            rtl: true,
            position: 'top',
            labels: { color: '#64748B', padding: 16 },
          },
          tooltip: {
            rtl: true,
            callbacks: {
              label: function (ctx) {
                var v = ctx.parsed.y;
                if (v == null) return '';
                return (
                  ctx.dataset.label +
                  ': ' +
                  Number(v).toLocaleString('ar-SA') +
                  ' ج.م'
                );
              },
            },
          },
        },
        scales: {
          x: {
            grid: { display: false },
            ticks: { color: '#64748B', maxRotation: 0 },
          },
          y: {
            beginAtZero: true,
            grid: { color: 'rgba(226, 232, 240, 0.9)' },
            ticks: {
              color: '#64748B',
              callback: function (value) {
                return Number(value).toLocaleString('ar-SA');
              },
            },
          },
        },
      },
    });
  }

  function bindDateFilter() {
    var dateInput = document.getElementById('dateInput');
    if (!dateInput) return;
    dateInput.addEventListener('change', function () {
      var selected = dateInput.value;
      var nextUrl = '/Admin/Index';
      if (selected) {
        nextUrl += '?date=' + encodeURIComponent(selected);
      }
      window.location.href = nextUrl;
    });
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', function () {
      initSalesChart();
      bindDateFilter();
    });
  } else {
    initSalesChart();
    bindDateFilter();
  }
})();
