// ── Columnas de estados (igual que el Excel) ──────────────────────────────────
const ESTADOS_COLUMNAS = [
    { id: 1, nombre: 'Pedido' },
    { id: 2, nombre: 'Pre autorización' },
    { id: 3, nombre: 'Aprobación OC' },
    { id: 4, nombre: 'En tránsito extranjero' },
    { id: 5, nombre: 'En aduana' },
    { id: 6, nombre: 'En senasag' },
    { id: 7, nombre: 'En tránsito nacional' },
    { id: 8, nombre: 'Enviado a Proveedor' },
    { id: 9, nombre: 'Recepción almacenes' },
    { id: 10, nombre: 'Costeado en SAP' },
    { id: 11, nombre: 'Rechazado' },
];

const COLUMNAS_BASE = [
    { key: 'id', label: 'Nº OC', clase: 'col-id' },
    { key: 'fecha', label: 'Fecha Creación OC' },
    { key: 'idSolicitud', label: 'Nº Solicitud' },
    { key: 'solicitantesSolicitudesVinculadas', label: 'Solicitantes' },
    { key: 'frequerimiento', label: 'Fecha Requerimiento' },
    { key: 'proveedorItem', label: 'Proveedor Item' },
    { key: 'nombreItem', label: 'Nombre Item' },
    { key: 'codigoItem', label: 'Codigo Item' },
    { key: 'cantidadItem', label: 'Cantidad', clase: 'col-num' },
    { key: 'precioItem', label: 'Precio', clase: 'col-num' },
    { key: 'esImportacion', label: 'Tipo' },
];

// ── Helpers de parámetros ─────────────────────────────────────────────────────
function obtenerParams() {
    const desde = document.getElementById('reporte_fecha_desde').value || null;
    const hasta = document.getElementById('reporte_fecha_hasta').value || null;
    const params = new URLSearchParams();
    if (desde) params.append('fechaDesde', desde);
    if (hasta) params.append('fechaHasta', hasta);
    return params;
}

// ── Previsualizar ─────────────────────────────────────────────────────────────
async function previsualizarReporte() {
    const btn = document.getElementById('btnPrevisualizar');
    toggleButtonLoading(btn, true, 'Cargando...');

    mostrarSkeletonPreview();

    try {
        const params = obtenerParams();
        const response = await fetch(`/Reportes/GetPreviewReporte?${params.toString()}`);

        if (!response.ok) {
            mostrarAlerta('Error al cargar la vista previa', 'error');
            cerrarPreview();
            return;
        }

        const data = await response.json();
        renderizarTablaPreview(data);

    } catch (error) {
        console.error('Error:', error);
        mostrarAlerta('Error al cargar la vista previa', 'error');
        cerrarPreview();
    } finally {
        toggleButtonLoading(btn, false);
    }
}

// ── Skeleton mientras carga ───────────────────────────────────────────────────
function mostrarSkeletonPreview() {
    const seccion = document.getElementById('seccion-preview');
    seccion.classList.remove('hidden');

    const thead = document.getElementById('preview-thead');
    const tbody = document.getElementById('preview-tbody');

    // Headers
    const totalCols = COLUMNAS_BASE.length + ESTADOS_COLUMNAS.length;
    thead.innerHTML = `<tr>${COLUMNAS_BASE.map(c => `<th>${c.label}</th>`).join('')}${ESTADOS_COLUMNAS.map(e => `<th>${e.nombre}</th>`).join('')}</tr>`;

    // Skeleton rows
    tbody.innerHTML = Array.from({ length: 6 }).map(() =>
        `<tr>${Array.from({ length: totalCols }).map(() =>
            `<td><span class="skeleton" style="width:${60 + Math.random() * 60}px"></span></td>`
        ).join('')}</tr>`
    ).join('');

    document.getElementById('preview-count').textContent = '';
}

// ── Renderizar tabla ──────────────────────────────────────────────────────────
function renderizarTablaPreview(filas) {
    const thead = document.getElementById('preview-thead');
    const tbody = document.getElementById('preview-tbody');
    const countEl = document.getElementById('preview-count');

    // Headers
    thead.innerHTML = `
        <tr>
            ${COLUMNAS_BASE.map(c => `<th>${c.label}</th>`).join('')}
            ${ESTADOS_COLUMNAS.map(e => `<th>${e.nombre}</th>`).join('')}
        </tr>`;

    if (!filas || filas.length === 0) {
        tbody.innerHTML = `<tr><td colspan="${COLUMNAS_BASE.length + ESTADOS_COLUMNAS.length}" class="text-center text-gray-500 py-8 text-xs">Sin resultados para los filtros seleccionados.</td></tr>`;
        countEl.textContent = '(0 filas)';
        return;
    }

    countEl.textContent = `(${filas.length} fila${filas.length !== 1 ? 's' : ''})`;

    // Agrupar por Id para saber cuántas filas tiene cada OC
    const grupos = {};
    filas.forEach(f => {
        if (!grupos[f.id]) grupos[f.id] = [];
        grupos[f.id].push(f);
    });

    tbody.innerHTML = filas.map((fila, idx) => {
        const grupo = grupos[fila.id];
        const esPrimeroDelGrupo = grupo[0] === fila;

        // Celdas base
        const celdasBase = COLUMNAS_BASE.map(col => {
            let valor = fila[col.key];

            if (col.key === 'esImportacion') {
                valor = fila.esImportacion ? 'IMPORTACIÓN' : 'NACIONAL';
            } else if (col.key === 'fecha' || col.key === 'frequerimiento') {
                valor = valor ? formatFecha(valor) : '—';
            } else if (col.key === 'cantidadItem' || col.key === 'precioItem') {
                valor = valor != null ? Number(valor).toLocaleString('es-BO', { minimumFractionDigits: 2 }) : '—';
            } else {
                valor = valor ?? '—';
            }

            return `<td class="${col.clase || ''}">${valor}</td>`;
        }).join('');

        // Celdas de estado: solo en la primera fila del grupo
        const celdasEstado = ESTADOS_COLUMNAS.map(e => {
            if (!esPrimeroDelGrupo) return `<td></td>`;
            const historial = fila.historialEstados || {};
            const fecha = historial[e.id];
            const alcanzado = !!fecha;
            const texto = alcanzado ? formatFechaHora(fecha) : '—';
            return `<td class="text-center"><span class="estado-badge ${alcanzado ? 'alcanzado' : ''}">${texto}</span></td>`;
        }).join('');

        return `<tr>${celdasBase}${celdasEstado}</tr>`;
    }).join('');
}

// ── Cerrar preview ────────────────────────────────────────────────────────────
function cerrarPreview() {
    document.getElementById('seccion-preview').classList.add('hidden');
}

// ── Descargar Excel ───────────────────────────────────────────────────────────
async function descargarReporte() {
    const btn = document.getElementById('btnDescargarReporte');
    toggleButtonLoading(btn, true, 'Generando...');

    try {
        const params = obtenerParams();
        const response = await fetch(`/Reportes/GenerarReporteExcel?${params.toString()}`);

        if (!response.ok) {
            mostrarAlerta('Error al generar el reporte', 'error');
            return;
        }

        const blob = await response.blob();
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `Reporte_Ordenes_${new Date().toLocaleDateString('es-ES').replace(/\//g, '')}.xlsx`;
        document.body.appendChild(a);
        a.click();
        window.URL.revokeObjectURL(url);
        document.body.removeChild(a);

        mostrarAlerta('Reporte descargado exitosamente', 'success');

    } catch (error) {
        console.error('Error:', error);
        mostrarAlerta('Error al generar el reporte', 'error');
    } finally {
        toggleButtonLoading(btn, false);
    }
}

// ── Utilidades de fecha ───────────────────────────────────────────────────────
function formatFecha(isoStr) {
    if (!isoStr) return '—';
    const d = new Date(isoStr);
    if (isNaN(d)) return isoStr;
    return d.toLocaleDateString('es-BO', { day: '2-digit', month: '2-digit', year: 'numeric' });
}

function formatFechaHora(isoStr) {
    if (!isoStr) return '—';
    const d = new Date(isoStr);
    if (isNaN(d)) return isoStr;
    return d.toLocaleString('es-BO', {
        day: '2-digit', month: '2-digit', year: 'numeric',
        hour: '2-digit', minute: '2-digit'
    });
}

// ── Helpers UI ────────────────────────────────────────────────────────────────
function toggleButtonLoading(button, isLoading, loadingText = 'Procesando...') {
    if (!button) return;
    if (isLoading) {
        button.dataset.originalHtml = button.innerHTML;
        button.disabled = true;
        button.classList.add('opacity-75', 'cursor-not-allowed');
        button.innerHTML = `<span class="inline-flex items-center">
            <i class="fas fa-spinner fa-spin mr-1"></i>${loadingText}
        </span>`;
        return;
    }
    button.disabled = false;
    button.classList.remove('opacity-75', 'cursor-not-allowed');
    if (button.dataset.originalHtml) button.innerHTML = button.dataset.originalHtml;
}

function mostrarAlerta(mensaje, tipo) {
    Swal.fire({
        position: 'center',
        icon: tipo,
        title: mensaje,
        showConfirmButton: false,
        timer: 1500,
        background: '#1f2937',
        color: '#ffffff',
        toast: true,
        timerProgressBar: true
    });
}
