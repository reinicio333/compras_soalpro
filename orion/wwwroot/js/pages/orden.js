// Variables Globales
let gridApi;
let proveedoresDisponibles = [];
let estadosDisponibles = [];
let ordenEditando = null;
window.esUsuarioPlanta = false;
let tipoCambioActual = "6.96";
const tiposArchivoPermitidos = ['jpg', 'jpeg', 'png', 'gif', 'bmp', 'webp', 'pdf', 'xls', 'xlsx', 'doc', 'docx'];
const maxArchivoBytes = 1024 * 1024;

// Configuración del Grid
const gridOptions = {
    columnDefs: [
        {
            headerName: "ID",
            field: "id",
            maxWidth: 100,
            sort: 'desc'
        },
        {
            headerName: "Fecha",
            field: "fecha",
            valueFormatter: params => {
                if (params.value) {
                    const fecha = new Date(params.value);
                    return fecha.toLocaleDateString('es-ES');
                }
                return '';
            },
            maxWidth: 150,
        },
        {
            headerName: "Referencia",
            field: "referencia"
        },
        {
            headerName: "Solicitante",
            field: "solicitante"
        },
        {
            headerName: "Proveedor",
            field: "proveedor"
        },
        {
            headerName: "Tipo",
            field: "esImportacion",
            width: 120,
            valueGetter: params => params.data.esImportacion ? "IMPORTACION" : "NACIONAL",
            cellRenderer: params => {
                return params.value === "IMPORTACION"
                    ? '<span class="px-2 py-1 bg-gray-600 text-white rounded text-xs">IMPORTACION</span>'
                    : '<span class="px-2 py-1 bg-gray-500 text-white rounded text-xs">NACIONAL</span>';
            }
        },
        {
            headerName: "Estado",
            field: "estado",
            minWidth: 200,
            cellRenderer: params => {
                const estado = params.value || 'Sin Estado';
                const idEstado = params.data.idEstado || 1;
                return `<span class="badge-estado estado-${idEstado}">${estado}</span>`;
            }
        },
        {
            headerName: "Fecha Estado",
            field: "fechaEstado",
            maxWidth: 150,
            valueFormatter: params => {
                if (params.value) {
                    const fecha = new Date(params.value);
                    return fecha.toLocaleDateString('es-ES', {
                        day: '2-digit',
                        month: '2-digit',
                        year: 'numeric',
                        hour: '2-digit',
                        minute: '2-digit'
                    });
                }
                return '';
            }
        },
        {
            headerName: "Acciones",
            field: "acciones",
            filter: false,
            floatingFilter: false,
            minWidth: 310,
            cellRenderer: params => {
                const idEstado = params.data.idEstado || 1;
                const esAprobadoOSuperior = idEstado >= 3;

                if (window.esUsuarioPlanta) {
                    return `
                    <button onclick="verEstadoOrden(${params.data.id})" 
                            class="px-3 py-1 text-blue-400 hover:bg-blue-700 hover:text-white rounded-lg transition-all" 
                            title="Ver Estado">
                        <i class="fas fa-tasks text-sm"></i>
                    </button>
                    <button onclick="vistaPreviaOrden(${params.data.id})" 
                            class="px-3 py-1 text-gray-400 hover:bg-gray-600 hover:text-white rounded-lg transition-all" 
                            title="Vista Previa">
                        <i class="fas fa-eye text-sm"></i>
                    </button>
                    <button onclick="descargarPdfOrden(${params.data.id})" 
                            class="px-3 py-1 text-green-600 hover:bg-green-700 hover:text-white rounded-lg transition-all" 
                            title="Descargar PDF">
                        <i class="fas fa-file-download text-sm"></i>
                    </button>
            `;
                }

                if (window.tipoUsuario === 'ALMACEN') {
                    return `
                <button onclick="verEstadoOrden(${params.data.id})" 
                        class="px-3 py-1 text-blue-400 hover:bg-blue-700 hover:text-white rounded-lg transition-all" 
                        title="Ver Estado">
                    <i class="fas fa-tasks text-sm"></i>
                </button>
                <button onclick="vistaPreviaOrden(${params.data.id})" 
                        class="px-3 py-1 text-gray-400 hover:bg-gray-600 hover:text-white rounded-lg transition-all" 
                        title="Vista Previa">
                    <i class="fas fa-eye text-sm"></i>
                </button>
                <button onclick="descargarPdfOrden(${params.data.id})" 
                        class="px-3 py-1 text-green-600 hover:bg-green-700 hover:text-white rounded-lg transition-all" 
                        title="Descargar PDF">
                    <i class="fas fa-file-download text-sm"></i>
                </button>
            `;
                }
                if (window.tipoUsuario === 'GERENCIA') {
                    return `
                        <button onclick="verEstadoOrden(${params.data.id})" 
                                class="px-3 py-1 text-blue-400 hover:bg-blue-700 hover:text-white rounded-lg transition-all" 
                                title="Ver Estado">
                            <i class="fas fa-tasks text-sm"></i>
                        </button>
                        <button onclick="vistaPreviaOrden(${params.data.id})" 
                                class="px-3 py-1 text-gray-400 hover:bg-gray-600 hover:text-white rounded-lg transition-all" 
                                title="Vista Previa">
                            <i class="fas fa-eye text-sm"></i>
                        </button>
                        <button onclick="descargarPdfOrden(${params.data.id})" 
                                class="px-3 py-1 text-green-600 hover:bg-green-700 hover:text-white rounded-lg transition-all" 
                                title="Descargar PDF">
                            <i class="fas fa-file-download text-sm"></i>
                        </button>
                    `;
                }

                return `
            <button onclick="verEstadoOrden(${params.data.id})" 
                    class="px-3 py-1 text-blue-400 hover:bg-blue-700 hover:text-white rounded-lg transition-all" 
                    title="Ver/Cambiar Estado">
                <i class="fas fa-tasks text-sm"></i>
            </button>
            <button onclick="vistaPreviaOrden(${params.data.id})" 
                    class="px-3 py-1 text-gray-400 hover:bg-gray-600 hover:text-white rounded-lg transition-all" 
                    title="Vista Previa">
                <i class="fas fa-eye text-sm"></i>
            </button>
            <button onclick="descargarPdfOrden(${params.data.id})" 
                    class="px-3 py-1 text-green-600 hover:bg-green-700 hover:text-white rounded-lg transition-all" 
                    title="Descargar PDF">
                <i class="fas fa-file-download text-sm"></i>
            </button>
            ${esAprobadoOSuperior ? '' : `
                <button onclick="editarOrden(${params.data.id})" 
                        class="px-3 py-1 text-yellow-600 hover:bg-yellow-700 hover:text-white rounded-lg transition-all" 
                        title="Editar">
                    <i class="fas fa-edit text-sm"></i>
                </button>
                <button onclick="eliminarOrden(${params.data.id})" 
                        class="px-3 py-1 text-red-600 hover:bg-red-700 hover:text-white rounded-lg transition-all" 
                        title="Eliminar">
                    <i class="fas fa-trash text-sm"></i>
                </button>
            `}
        `;
            }
        }
    ],
    defaultColDef: {
        flex: 1,
        filter: true,
        sortable: true,
        resizable: true,
        minWidth: 100,
        floatingFilter: true
    },
    pagination: true,
    paginationPageSize: 10,
    paginationPageSizeSelector: [10, 20, 50, 100],
    getRowId: params => String(params.data.id),
    localeText: {
        page: "Página",
        more: "Más",
        to: "a",
        of: "de",
        next: "Siguiente",
        last: "Última",
        first: "Primera",
        previous: "Anterior",
        loadingOoo: "Cargando...",
        noRowsToShow: "Sin órdenes para mostrar",
    },
    onGridReady: async () => {
        await cargarOrdenes();
    }
};

document.addEventListener('DOMContentLoaded', function () {
    const gridDiv = document.getElementById('myGridOrdenes');
    gridApi = agGrid.createGrid(gridDiv, gridOptions);

    cargarProveedores();
    cargarEstados();
    cargarAprobadores();
    cargarAreasCorrespondencia();

    document.getElementById('es_importacion').addEventListener('change', function () {
        const texto = document.getElementById('tipo_orden_texto');
        texto.textContent = this.checked ? 'Importación' : 'Nacional';
    });

    document.getElementById('select_proveedor').addEventListener('change', function () {
        obtenerReferenciaPorProveedor(this.value);
    });

    document.getElementById('btnNuevaOrden').addEventListener('click', abrirModalNuevaOrden);

    document.getElementById('fecha_orden').addEventListener('change', function () {
        actualizarTipoCambioPorFecha(this.value);
    });

    const inputArchivos = document.getElementById('archivos_orden');
    if (inputArchivos) {
        inputArchivos.addEventListener('change', validarArchivosSeleccionados);
    }
});


async function cargarOrdenes() {
    const permisos = await verificarPermisos();
    window.esUsuarioPlanta = permisos.tipo === 'PLANTA';
    window.tipoUsuario = permisos.tipo;

    if (window.esUsuarioPlanta || window.tipoUsuario === 'ALMACEN' || window.tipoUsuario === 'GERENCIA') {
        document.getElementById('btnNuevaOrden').style.display = 'none';
    }

    fetch('/Orden/ListarOrdenes')
        .then(resp => resp.json())
        .then(data => {
            if (Array.isArray(data)) {
                gridApi.setGridOption('rowData', data);
                gridApi.refreshCells({ force: true });
            }
        });
}

async function cargarProveedores() {
    try {
        const response = await fetch('/Orden/GetProveedores');
        const data = await response.json();

        if (data && Array.isArray(data)) {
            proveedoresDisponibles = data;
            const select = document.getElementById('select_proveedor');
            select.innerHTML = '<option value="">Seleccione...</option>';

            data.forEach(proveedor => {
                const option = document.createElement('option');
                option.value = proveedor;
                option.textContent = proveedor;
                select.appendChild(option);
            });
        }
    } catch (error) {
        console.error('Error al cargar proveedores:', error);
    }
}

async function cargarEstados() {
    try {
        const response = await fetch('/Orden/GetEstados');
        const data = await response.json();

        if (data && Array.isArray(data)) {
            estadosDisponibles = data;
        }
    } catch (error) {
        console.error('Error al cargar estados:', error);
    }
}

function seleccionarAprobadorPorDefecto() {
    const selectAprobador = document.getElementById('aprobador_orden');

    if (!selectAprobador) return;

    const opcionDefault = Array.from(selectAprobador.options).find(option => {
        const nombre = (option.textContent || '').toLowerCase().trim();
        return option.value && (
            nombre.includes('gcardenas') ||
            nombre.includes('gerardo cardenas')
        );
    });

    if (opcionDefault) {
        selectAprobador.value = opcionDefault.value;
    }
}

async function cargarAprobadores() {
    try {
        const response = await fetch('/Orden/GetAprobadores');
        const aprobadores = await response.json();
        const selectAprobador = document.getElementById('aprobador_orden');

        if (!selectAprobador) return;

        selectAprobador.innerHTML = '<option value="">Seleccione...</option>';

        if (Array.isArray(aprobadores)) {
            aprobadores.forEach(a => {
                const option = document.createElement('option');
                option.value = a.id;
                option.textContent = a.nombre;
                selectAprobador.appendChild(option);
            });
        }

        seleccionarAprobadorPorDefecto();
    } catch (error) {
        console.error('Error al cargar aprobadores:', error);
    }
}

async function cargarAreasCorrespondencia() {
    try {
        const response = await fetch('/Orden/GetAreasCorrespondencia');
        const areas = await response.json();
        const select = document.getElementById('corresponde_asc');

        if (!select) return;

        select.innerHTML = '<option value="">Seleccione...</option>';

        if (Array.isArray(areas)) {
            areas.forEach(a => {
                const option = document.createElement('option');
                option.value = a.id;
                option.textContent = a.nombre;
                select.appendChild(option);
            });
        }
    } catch (error) {
        console.error('Error al cargar áreas de correspondencia:', error);
    }
}

async function verificarPermisos() {
    const response = await fetch('/Orden/GetTipoUsuario');
    const data = await response.json();
    return data;
}

function abrirModalNuevaOrden() {
    ordenEditando = null;
    document.getElementById('titleModalOrden').innerHTML = '<i class="fas fa-plus mr-2 text-red-400"></i>NUEVA ORDEN DE COMPRA';
    document.getElementById('id_orden').value = '';

    limpiarFormulario();
    document.getElementById('razon_social_factura').value = 'SOALPRO S.R.L.';
    autocompletarNit('SOALPRO S.R.L.');
    const hoy = new Date();
    const fechaLocal = new Date(hoy.getTime() - (hoy.getTimezoneOffset() * 60000)).toISOString().split('T')[0];
    document.getElementById('fecha_orden').value = fechaLocal;
    actualizarTipoCambioPorFecha(fechaLocal);
    seleccionarAprobadorPorDefecto();
    renderizarListaArchivos('lista_archivos_orden', []);

    const modal = new Modal(document.getElementById('modalOrden'));
    modal.show();
}

async function obtenerReferenciaPorProveedor(proveedor) {
    const inputReferencia = document.getElementById('referencia_orden');
    const inputId = document.getElementById('id_solicitud_actual');
    const inputTelefono = document.getElementById('tel_prov');
    const inputContacto = document.getElementById('contacto_nom');

    if (!proveedor) {
        inputReferencia.value = '';
        inputId.value = '';
        inputTelefono.value = '';
        inputContacto.value = '';
        document.getElementById('banco_nombre').value = '';
        document.getElementById('cuenta_numero').value = '';
        document.getElementById('nombre_cuenta').value = '';
        document.getElementById('body_detalle').innerHTML = `
            <tr>
                <td colspan="10" class="text-center py-4 text-gray-400">
                    Seleccione un proveedor para cargar datos...
                </td>
            </tr>
        `;
        return;
    }

    try {
        const response = await fetch(`/Orden/GetReferenciaPorProveedor?proveedor=${encodeURIComponent(proveedor)}`);
        const data = await response.json();

        if (data) {
            inputReferencia.value = data.referencia || 'Sin referencia';
            inputId.value = data.id;
            inputTelefono.value = data.telefono || '';
            inputContacto.value = data.contacto || '';
            document.getElementById('banco_nombre').value = data.banco || '';
            document.getElementById('cuenta_numero').value = data.cuenta || '';
            document.getElementById('nombre_cuenta').value = data.nombreCuenta || '';

            if (data.id > 0) {
                await cargarDetallesPorProveedor(proveedor);
            }
        }
    } catch (error) {
        console.error('Error:', error);
        inputReferencia.value = 'Error al cargar';
        inputTelefono.value = '';
        inputContacto.value = '';
        document.getElementById('banco_nombre').value = '';
        document.getElementById('cuenta_numero').value = '';
        document.getElementById('nombre_cuenta').value = '';
    }
}

async function cargarDetalle(id) {
    try {
        const response = await fetch(`/Orden/GetDetalleSolicitud?id=${id}`);
        const data = await response.json();

        let html = '';
        if (data && data.length > 0) {
            data.forEach((d, index) => {
                html += `
                    <tr class="border-b border-gray-600 hover:bg-gray-600" data-detalle-id="${d.id}">
                        <td class="px-2 py-3 text-center">
                            <input type="checkbox" class="chk-producto w-4 h-4 text-blue-600 bg-gray-700 border-gray-600 rounded" checked>
                        </td>
                        <td class="px-2 py-3 text-white">${d.codigo || ''}</td>
                        <td class="px-2 py-3 text-center text-white">${index + 1}</td>
                        <td class="px-2 py-3 text-white">${d.descripcion || ''}</td>
                        <td class="px-2 py-3">
                            <input type="date" class="fecha-entrega bg-gray-600 border border-gray-500 text-gray-300 text-xs rounded p-1 w-full cursor-not-allowed" 
                                   value="${d.fechaEntrega || ''}" readonly>
                        </td>
                        <td class="px-2 py-3 text-white">${d.caracteristicas || ''}</td>
                        <td class="px-2 py-3 text-center text-white">${d.unidad || ''}</td>
                        <td class="px-2 py-3 text-center text-white cantidad-producto">${d.cantidad || 0}</td>
                        <td class="px-2 py-3">
                            <input type="number" step="0.01" class="precio-producto bg-gray-800 border border-blue-600 text-white text-xs rounded p-1 w-full text-right" 
                                   placeholder="0.00" onkeyup="calcularTotal(this)">
                        </td>
                        <td class="px-2 py-3 text-right font-bold text-white total-fila">0.00</td>
                    </tr>
                `;
            });
        } else {
            html = `
                <tr>
                    <td colspan="10" class="text-center py-4 text-gray-400">
                        No hay productos disponibles (todos están en uso)
                    </td>
                </tr>
            `;
        }

        document.getElementById('body_detalle').innerHTML = html;
    } catch (error) {
        console.error('Error al cargar detalle:', error);
    }
}
async function cargarDetallesPorProveedor(proveedor) {
    try {
        const response = await fetch(`/Orden/GetDetallesPorProveedor?proveedor=${encodeURIComponent(proveedor)}`);
        const data = await response.json();

        let html = '';
        if (data && data.length > 0) {
            data.forEach((d, index) => {
                html += `
                    <tr class="border-b border-gray-600 hover:bg-gray-600" data-detalle-id="${d.id}">
                        <td class="px-2 py-3 text-center">
                            <input type="checkbox" class="chk-producto w-4 h-4 text-blue-600 bg-gray-700 border-gray-600 rounded" checked>
                        </td>
                        <td class="px-2 py-3 text-white">${d.codigo || ''}</td>
                        <td class="px-2 py-3 text-center text-white">${index + 1}</td>
                        <td class="px-2 py-3 text-white">${d.descripcion || ''}</td>
                        <td class="px-2 py-3">
                            <input type="date" class="fecha-entrega bg-gray-600 border border-gray-500 text-gray-300 text-xs rounded p-1 w-full cursor-not-allowed" 
                                   value="${d.fechaEntrega || ''}" readonly>
                        </td>
                        <td class="px-2 py-3 text-white">${d.caracteristicas || ''}</td>
                        <td class="px-2 py-3 text-center text-white">${d.unidad || ''}</td>
                        
                        <td class="px-2 py-3 text-center">
                            <label class="relative inline-flex items-center cursor-pointer">
                                <input type="checkbox" class="chk-stock sr-only peer">
                                <div class="w-8 h-4 bg-gray-600 rounded-full peer 
                                            peer-checked:bg-green-500
                                            after:content-[''] after:absolute after:top-[2px] after:left-[2px] 
                                            after:bg-white after:rounded-full after:h-3 after:w-3 
                                            after:transition-all peer-checked:after:translate-x-4 relative">
                                </div>
                            </label>
                        </td>
                        <td class="px-2 py-3 text-gray-400">${d.ultimoPrecio > 0 ? parseFloat(d.ultimoPrecio).toFixed(2) : '-'}</td>
                        <td class="px-2 py-3 text-gray-400">${d.fultimoPrecio ? new Date(d.fultimoPrecio).toLocaleDateString('es-ES') : '-'}</td>
                        <td class="px-2 py-3">
                            <input type="number" step="0.01" class="cantidad-producto bg-gray-800 border border-gray-600 text-white text-xs rounded p-1 w-full text-center"
                                   value="${d.cantidad || 0}" onkeyup="calcularTotal(this)">
                        </td>
                        <td class="px-2 py-3">
                            <input type="number" step="0.01" class="precio-producto bg-gray-800 border border-blue-600 text-white text-xs rounded p-1 w-full text-right" 
                                   placeholder="0.00" onkeyup="calcularTotal(this)">
                        </td>
                        <td class="px-2 py-3 text-right font-bold text-white total-fila">0.00</td>
                        <td class="cantidad-solicitada" style="display:none">${d.cantidad || 0}</td>
                    </tr>
                `;
            });
        } else {
            html = `
                <tr>
                    <td colspan="12" class="text-center py-4 text-gray-400">
                        No hay productos disponibles con estado "Creado" para este proveedor
                    </td>
                </tr>
            `;
        }

        document.getElementById('body_detalle').innerHTML = html;
    } catch (error) {
        console.error('Error al cargar detalles por proveedor:', error);
    }
}

function calcularTotal(input) {
    const fila = input.closest('tr');
    const cantidad = parseFloat(fila.querySelector('.cantidad-producto')?.value) || 0;
    const precio = parseFloat(fila.querySelector('.precio-producto')?.value) || 0;
    const total = (cantidad * precio).toFixed(2);
    fila.querySelector('.total-fila').textContent = total;
}

async function actualizarTipoCambioPorFecha(fecha) {
    try {
        const response = await fetch(`/TipoCambio/GetTipoCambioPorFecha?fecha=${encodeURIComponent(fecha || '')}`);
        const data = await response.json();
        tipoCambioActual = data?.valor || '6.96';
    } catch (error) {
        tipoCambioActual = '6.96';
    }
}

function recopilarDatos() {
    const idOrden = document.getElementById('id_orden').value;
    const idSolicitud = document.getElementById('id_solicitud_actual').value;

    if (!idSolicitud || idSolicitud === '0') {
        mostrarAlerta('Por favor, seleccione un proveedor válido', 'warning');
        return null;
    }

    const formaPago = document.querySelector('input[name="forma_pago"]:checked');

    const datos = {
        idOrden: idOrden ? parseInt(idOrden) : 0,
        idSolicitud: parseInt(idSolicitud),
        fecha: document.getElementById('fecha_orden').value,
        proveedor: document.getElementById('select_proveedor').value,
        telefono: document.getElementById('tel_prov').value,
        contacto: document.getElementById('contacto_nom').value,
        solicitante: document.getElementById('solicitante_orden').value,
        idAreaCorrespondencia: parseInt(document.getElementById('corresponde_asc').value) || 0,
        correspondeAsc: document.getElementById('corresponde_asc').selectedOptions[0]?.textContent || '',
        referencia: document.getElementById('referencia_orden').value,
        aprobador: document.getElementById('aprobador_orden').value,
        tc: tipoCambioActual,
        esImportacion: document.getElementById('es_importacion').checked,
        cabecera: {
            observacion: document.getElementById('observacion').value,
            formaPago: formaPago ? formaPago.value : ''
        },
        entrega: {
            medioTransporte: document.getElementById('medio_transporte').value,
            responsableRecepcion: document.getElementById('responsable_recepcion').value,
            fechaEntrega: document.getElementById('fecha_entrega_info').value,
            lugarEntrega: document.getElementById('lugar_entrega').value
        },
        pago: {
            anticipoF: document.getElementById('fecha_anticipo').value,
            anticipoM: document.getElementById('monto_anticipo').value,
            finalF: document.getElementById('fecha_pago_final').value,
            finalM: document.getElementById('monto_pago_final').value,
            banco: document.getElementById('banco_nombre').value,
            cuenta: document.getElementById('cuenta_numero').value,
            nombreCuenta: document.getElementById('nombre_cuenta').value,
            swift: document.getElementById('codigo_swift').value,
            incoterm: document.getElementById('incoterm').value
        },
        facturacion: {
            razon: document.getElementById('razon_social_factura').value,
            nit: document.getElementById('nit_factura').value
        },
        productos: []
    };

    document.querySelectorAll('#body_detalle tr').forEach(fila => {
        const check = fila.querySelector('.chk-producto');
        if (check && check.checked) {
            const idDetalle = fila.getAttribute('data-detalle-id');
            const cells = fila.querySelectorAll('td');
            const cantidadInput = fila.querySelector('.cantidad-producto');
            const precioInput = fila.querySelector('.precio-producto');

            datos.productos.push({
                idDetalleSolicitud: parseInt(idDetalle),
                codigo: cells[1]?.textContent.trim() || '',
                nro: cells[2]?.textContent.trim() || '',
                descripcion: cells[3]?.textContent.trim() || '',
                fechaEntrega: fila.querySelector('.fecha-entrega')?.value || '',
                caracteristicas: cells[5]?.textContent.trim() || '',
                unidad: cells[6]?.textContent.trim() || '',
                cantidad: parseFloat(fila.querySelector('.cantidad-producto')?.value) || 0,
                precio: parseFloat(fila.querySelector('.precio-producto')?.value) || 0,
                esStock: fila.querySelector('.chk-stock')?.checked || false 
            });
        }
    });

    if (!datos.idAreaCorrespondencia) {
        mostrarAlerta('Debe seleccionar CORRESPONDE A.S.C.', 'warning');
        return null;
    }

    if (!datos.aprobador) {
        mostrarAlerta('Debe seleccionar un APROVADOR', 'warning');
        return null;
    }

    if (datos.productos.length === 0) {
        mostrarAlerta('Debe seleccionar al menos un producto', 'warning');
        return null;
    }

    return datos;
}

async function guardarOrden() {
    const datos = recopilarDatos();
    if (!datos) return;

    const url = datos.idOrden > 0 ? '/Orden/ActualizarOrden' : '/Orden/GuardarOrden';
    const accion = datos.idOrden > 0 ? 'actualizada' : 'guardada';

    try {
        const response = await fetch(url, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(datos)
        });

        const result = await response.json();
        mostrarAlerta(result.mensaje, result.tipo);

        if (result.tipo === 'success') {
            const idOrden = datos.idOrden > 0 ? datos.idOrden : result.id;
            await subirArchivosOrden(idOrden);
            cerrarModalOrden();
            cargarOrdenes();
        }
    } catch (error) {
        console.error('Error:', error);
        mostrarAlerta('Error al guardar la orden', 'error');
    }
}

async function editarOrden(id) {
    try {
        const response = await fetch(`/Orden/GetOrden?id=${id}`);
        const data = await response.json();

        if (data.tipo === 'error') {
            mostrarAlerta(data.mensaje, 'error');
            return;
        }

        ordenEditando = data.orden;
        document.getElementById('titleModalOrden').innerHTML = '<i class="fas fa-edit mr-2 text-red-400"></i>EDITAR ORDEN DE COMPRA';
        document.getElementById('id_orden').value = data.orden.id;
        await cargarArchivosOrden(data.orden.id, 'lista_archivos_orden');

        document.getElementById('fecha_orden').value = data.orden.fecha || '';
        tipoCambioActual = data.orden.tipoCambio || '6.96';
        document.getElementById('select_proveedor').value = data.orden.proveedor || '';
        document.getElementById('referencia_orden').value = data.orden.referencia || '';
        document.getElementById('id_solicitud_actual').value = data.orden.idSolicitud || '';
        document.getElementById('observacion').value = data.orden.observacion || '';
        document.getElementById('es_importacion').checked = data.orden.esImportacion || false;
        document.getElementById('tipo_orden_texto').textContent = data.orden.esImportacion ? 'Importación' : 'Nacional';
        document.getElementById('tel_prov').value = data.orden.telefono || '';
        document.getElementById('contacto_nom').value = data.orden.nomContacto || '';
        document.getElementById('aprobador_orden').value = data.orden.aprobador || '';
        document.getElementById('corresponde_asc').value = data.orden.idAreaCorrespondencia || '';

        if (data.orden.formaPago) {
            const radio = document.querySelector(`input[name="forma_pago"][value="${data.orden.formaPago}"]`);
            if (radio) radio.checked = true;
        }

        document.getElementById('medio_transporte').value = data.orden.medioTransporte || '';
        document.getElementById('responsable_recepcion').value = data.orden.responsableRecepcion || '';
        document.getElementById('fecha_entrega_info').value = data.orden.fechaEntrega || '';
        document.getElementById('lugar_entrega').value = data.orden.lugarEntrega || '';

        document.getElementById('fecha_anticipo').value = data.orden.fechaAnticipo || '';
        document.getElementById('monto_anticipo').value = data.orden.montoAnticipo || '';
        document.getElementById('fecha_pago_final').value = data.orden.fechaPagoFinal || '';
        document.getElementById('monto_pago_final').value = data.orden.montoPagoFinal || '';
        document.getElementById('banco_nombre').value = data.orden.banco || '';
        document.getElementById('cuenta_numero').value = data.orden.cuenta || '';
        document.getElementById('nombre_cuenta').value = data.orden.nombreCuentaBancaria || '';
        document.getElementById('codigo_swift').value = data.orden.codigoSwift || '';
        document.getElementById('incoterm').value = data.orden.incoterm || '';

        document.getElementById('razon_social_factura').value = data.orden.razonSocial || '';
        document.getElementById('nit_factura').value = data.orden.nit || '';

        const idsEnOrden = new Set(data.productos.map(p => p.idDetalleSolicitud));

        const responseDetalles = await fetch(`/Orden/GetDetallesPorProveedor?proveedor=${encodeURIComponent(data.orden.proveedor)}`);
        const todosProductos = await responseDetalles.json();

        const productosSoloDisponibles = Array.isArray(todosProductos)
            ? todosProductos.filter(d => !idsEnOrden.has(d.id))
            : [];

        let html = '';
        let index = 1;

        if (data.productos && data.productos.length > 0) {
            data.productos.forEach(p => {
                html += `
                    <tr class="border-b border-gray-600 hover:bg-gray-600" data-detalle-id="${p.idDetalleSolicitud}">
                        <td class="px-2 py-3 text-center">
                            <input type="checkbox" class="chk-producto w-4 h-4 text-blue-600 bg-gray-700 border-gray-600 rounded" checked>
                        </td>
                        <td class="px-2 py-3 text-white">${p.codigo || ''}</td>
                        <td class="px-2 py-3 text-center text-white">${index++}</td>
                        <td class="px-2 py-3 text-white">${p.descripcion || ''}</td>
                        <td class="px-2 py-3">
                            <input type="date" class="fecha-entrega bg-gray-800 border border-gray-700 text-white text-xs rounded p-1 w-full">
                        </td>
                        <td class="px-2 py-3 text-white">${p.caracteristicas || ''}</td>
                        <td class="px-2 py-3 text-center text-white">${p.unidad || ''}</td>
                        <td class="px-2 py-3 text-center">
                            <label class="relative inline-flex items-center cursor-pointer">
                                <input type="checkbox" class="chk-stock sr-only peer" ${p.esStock ? 'checked' : ''}>
                                <div class="w-8 h-4 bg-gray-600 rounded-full peer 
                                            peer-checked:bg-green-500
                                            after:content-[''] after:absolute after:top-[2px] after:left-[2px] 
                                            after:bg-white after:rounded-full after:h-3 after:w-3 
                                            after:transition-all peer-checked:after:translate-x-4 relative">
                                </div>
                            </label>
                        </td>
                        <td class="px-2 py-3 text-gray-400">${p.ultimoPrecio > 0 ? parseFloat(p.ultimoPrecio).toFixed(2) : '-'}</td>
                        <td class="px-2 py-3 text-gray-400">${p.fultimoPrecio ? new Date(p.fultimoPrecio).toLocaleDateString('es-ES') : '-'}</td>
                        <td class="px-2 py-3">
                            <input type="number" step="0.01" class="cantidad-producto bg-gray-800 border border-gray-600 text-white text-xs rounded p-1 w-full text-center"
                                   value="${p.cantidad || 0}" onkeyup="calcularTotal(this)">
                        </td>
                        <td class="px-2 py-3">
                            <input type="number" step="0.01" value="${p.precio || 0}"
                                   class="precio-producto bg-gray-800 border border-blue-600 text-white text-xs rounded p-1 w-full text-right"
                                   placeholder="0.00" onkeyup="calcularTotal(this)">
                        </td>
                        <td class="px-2 py-3 text-right font-bold text-white total-fila">${((p.cantidad || 0) * (p.precio || 0)).toFixed(2)}</td>
                        <td class="cantidad-solicitada" style="display:none">${p.cantidadSolicitada || p.cantidad || 0}</td>
                    </tr>
                `;
            });
        }

        productosSoloDisponibles.forEach(d => {
            html += `
                <tr class="border-b border-gray-600 hover:bg-gray-600" data-detalle-id="${d.id}">
                    <td class="px-2 py-3 text-center">
                        <input type="checkbox" class="chk-producto w-4 h-4 text-blue-600 bg-gray-700 border-gray-600 rounded">
                    </td>
                    <td class="px-2 py-3 text-white">${d.codigo || ''}</td>
                    <td class="px-2 py-3 text-center text-white">${index++}</td>
                    <td class="px-2 py-3 text-white">${d.descripcion || ''}</td>
                    <td class="px-2 py-3">
                        <input type="date" class="fecha-entrega bg-gray-600 border border-gray-500 text-gray-300 text-xs rounded p-1 w-full cursor-not-allowed"
                               value="${d.fechaEntrega || ''}" readonly>
                    </td>
                    <td class="px-2 py-3 text-white">${d.caracteristicas || ''}</td>
                    <td class="px-2 py-3 text-center text-white">${d.unidad || ''}</td>
                    <td class="px-2 py-3 text-center">
                        <label class="relative inline-flex items-center cursor-pointer">
                            <input type="checkbox" class="chk-stock sr-only peer">
                            <div class="w-8 h-4 bg-gray-600 rounded-full peer 
                                        peer-checked:bg-green-500
                                        after:content-[''] after:absolute after:top-[2px] after:left-[2px] 
                                        after:bg-white after:rounded-full after:h-3 after:w-3 
                                        after:transition-all peer-checked:after:translate-x-4 relative">
                            </div>
                        </label>
                    </td>
                    <td class="px-2 py-3 text-gray-400">${d.ultimoPrecio > 0 ? parseFloat(d.ultimoPrecio).toFixed(2) : '-'}</td>
                    <td class="px-2 py-3 text-gray-400">${d.fultimoPrecio ? new Date(d.fultimoPrecio).toLocaleDateString('es-ES') : '-'}</td>
                    <td class="px-2 py-3">
                        <input type="number" step="0.01" class="cantidad-producto bg-gray-800 border border-gray-600 text-white text-xs rounded p-1 w-full text-center"
                               value="${d.cantidad || 0}" onkeyup="calcularTotal(this)">
                    </td>
                    <td class="px-2 py-3">
                        <input type="number" step="0.01"
                               class="precio-producto bg-gray-800 border border-blue-600 text-white text-xs rounded p-1 w-full text-right"
                               placeholder="0.00" onkeyup="calcularTotal(this)">
                    </td>
                    <td class="px-2 py-3 text-right font-bold text-white total-fila">0.00</td>
                    <td class="cantidad-solicitada" style="display:none">${d.cantidad || 0}</td>
                </tr>
            `;
        });

        document.getElementById('body_detalle').innerHTML = html || `
            <tr>
                <td colspan="12" class="text-center py-4 text-gray-400">No hay productos disponibles</td>
            </tr>
        `;

        const select = document.getElementById('select_proveedor');
        const opcionExiste = Array.from(select.options).some(o => o.value === data.orden.proveedor);
        if (!opcionExiste && data.orden.proveedor) {
            const option = document.createElement('option');
            option.value = data.orden.proveedor;
            option.textContent = data.orden.proveedor;
            select.appendChild(option);
        }
        select.value = data.orden.proveedor || '';

        const modal = new Modal(document.getElementById('modalOrden'));
        modal.show();

    } catch (error) {
        console.error('Error:', error);
        mostrarAlerta('Error al cargar la orden', 'error');
    }
}

function eliminarOrden(id) {
    Swal.fire({
        title: '¿Está seguro de eliminar la orden?',
        text: "Esta acción no se puede revertir",
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#d33',
        cancelButtonColor: '#3085d6',
        confirmButtonText: 'Sí, eliminar',
        cancelButtonText: 'Cancelar',
        background: '#1f2937',
        color: '#ffffff'
    }).then((result) => {
        if (result.isConfirmed) {
            fetch('/Orden/EliminarOrden?id=' + id, {
                method: 'POST'
            })
                .then(resp => resp.json())
                .then(data => {
                    mostrarAlerta(data.mensaje, data.tipo);
                    if (data.tipo === 'success') {
                        cargarOrdenes();
                    }
                })
                .catch(err => {
                    mostrarAlerta('Error al eliminar: ' + err, 'error');
                });
        }
    });
}

function limpiarFormulario() {
    document.getElementById('select_proveedor').value = '';
    document.getElementById('tel_prov').value = '';
    document.getElementById('contacto_nom').value = '';
    document.getElementById('referencia_orden').value = '';
    document.getElementById('aprobador_orden').value = '';
    document.getElementById('corresponde_asc').value = '';
    document.getElementById('id_solicitud_actual').value = '';
    document.getElementById('observacion').value = '';
    document.getElementById('es_importacion').checked = false;
    document.getElementById('tipo_orden_texto').textContent = 'Nacional';
    document.querySelectorAll('input[name="forma_pago"]').forEach(r => r.checked = false);

    document.getElementById('medio_transporte').value = '';
    document.getElementById('responsable_recepcion').value = '';
    document.getElementById('fecha_entrega_info').value = '';
    document.getElementById('lugar_entrega').value = '';

    document.getElementById('fecha_anticipo').value = '';
    document.getElementById('monto_anticipo').value = '';
    document.getElementById('fecha_pago_final').value = '';
    document.getElementById('monto_pago_final').value = '';
    document.getElementById('banco_nombre').value = '';
    document.getElementById('cuenta_numero').value = '';
    document.getElementById('nombre_cuenta').value = '';
    document.getElementById('codigo_swift').value = '';
    document.getElementById('incoterm').value = '';

    document.getElementById('razon_social_factura').value = '';
    document.getElementById('nit_factura').value = '';
    document.getElementById('razon_social_factura').value = 'SOALPRO S.R.L.';
    document.getElementById('nit_factura').value = '1020409021';
    document.getElementById('body_detalle').innerHTML = `
        <tr>
            <td colspan="10" class="text-center py-4 text-gray-400">
                Seleccione un proveedor para cargar datos...
            </td>
        </tr>
    `;
}

function cerrarModalOrden() {
    const modal = new Modal(document.getElementById('modalOrden'));
    modal.hide();
    limpiarFormulario();
    const inputArchivos = document.getElementById('archivos_orden');
    if (inputArchivos) inputArchivos.value = '';
    renderizarListaArchivos('lista_archivos_orden', []);
}


function validarArchivosSeleccionados() {
    const input = document.getElementById('archivos_orden');
    if (!input || !input.files) return;

    for (const archivo of input.files) {
        const extension = (archivo.name.split('.').pop() || '').toLowerCase();
        if (!tiposArchivoPermitidos.includes(extension)) {
            mostrarAlerta(`El archivo ${archivo.name} no tiene un formato permitido`, 'warning');
            input.value = '';
            return;
        }

        if (archivo.size > maxArchivoBytes) {
            mostrarAlerta(`El archivo ${archivo.name} supera 1MB`, 'warning');
            input.value = '';
            return;
        }
    }
}

async function subirArchivosOrden(idOrden) {
    const input = document.getElementById('archivos_orden');
    if (!input || !input.files || input.files.length === 0) return;

    const formData = new FormData();
    formData.append('idOrden', idOrden);
    Array.from(input.files).forEach(archivo => formData.append('archivos', archivo));

    try {
        const response = await fetch('/Orden/SubirArchivosOrden', {
            method: 'POST',
            body: formData
        });
        const result = await response.json();

        if (result.tipo !== 'success') {
            mostrarAlerta(result.mensaje || 'No se pudieron subir los archivos', result.tipo || 'warning');
            return;
        }

        input.value = '';
        await cargarArchivosOrden(idOrden, 'lista_archivos_orden');
    } catch (error) {
        console.error('Error al subir archivos:', error);
        mostrarAlerta('Error al subir archivos adjuntos', 'error');
    }
}

function renderizarListaArchivos(idContenedor, archivos) {
    const contenedor = document.getElementById(idContenedor);
    if (!contenedor) return;

    if (!Array.isArray(archivos) || archivos.length === 0) {
        contenedor.innerHTML = '<span class="text-gray-400">Sin archivos adjuntos</span>';
        return;
    }

    const permitirEliminar = idContenedor === 'lista_archivos_orden';

    contenedor.innerHTML = archivos.map(a => `
        <div class="flex items-center justify-between gap-2">
            <div class="truncate">
                <a href="${a.url}" target="_blank" class="text-blue-300 hover:text-blue-200 hover:underline">
                    ${a.nombre}
                </a>
                <span class="text-gray-500">(${a.tamanoKb} KB)</span>
            </div>
            ${permitirEliminar ? `<button type="button" onclick="eliminarArchivoOrden('${encodeURIComponent(a.url)}')" class="text-red-400 hover:text-red-300" title="Eliminar archivo"><i class="fas fa-trash text-xs"></i></button>` : ''}
        </div>
    `).join('');
}

async function eliminarArchivoOrden(urlArchivoCodificado) {
    const idOrden = parseInt(document.getElementById('id_orden').value || '0');
    const urlArchivo = decodeURIComponent(urlArchivoCodificado || "");
    if (!idOrden || !urlArchivo) return;

    const confirmacion = await Swal.fire({
        title: '¿Eliminar adjunto?',
        text: 'Esta acción quitará el archivo de la orden.',
        icon: 'warning',
        showCancelButton: true,
        confirmButtonText: 'Sí, eliminar',
        cancelButtonText: 'Cancelar'
    });

    if (!confirmacion.isConfirmed) return;

    try {
        const response = await fetch('/Orden/EliminarArchivoOrden', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ idOrden, url: urlArchivo })
        });

        const result = await response.json();
        mostrarAlerta(result.mensaje, result.tipo || 'info');

        if (result.tipo === 'success') {
            await cargarArchivosOrden(idOrden, 'lista_archivos_orden');
            const idEstado = parseInt(document.getElementById('id_orden_estado')?.value || '0');
            if (idEstado === idOrden) {
                await cargarArchivosOrden(idOrden, 'lista_archivos_estado');
            }
        }
    } catch (error) {
        console.error('Error al eliminar archivo:', error);
        mostrarAlerta('Error al eliminar archivo adjunto', 'error');
    }
}

async function cargarArchivosOrden(idOrden, idContenedor) {
    if (!idOrden) {
        renderizarListaArchivos(idContenedor, []);
        return;
    }

    try {
        const response = await fetch(`/Orden/ObtenerArchivosOrden?idOrden=${idOrden}`);
        const data = await response.json();
        renderizarListaArchivos(idContenedor, Array.isArray(data) ? data : []);
    } catch (error) {
        console.error('Error al cargar archivos:', error);
        renderizarListaArchivos(idContenedor, []);
    }
}


async function vistaPreviaOrden(id) {
    try {
        const response = await fetch('/Orden/GenerarPdfVistaPrevia', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ idOrden: id })
        });

        if (response.ok) {
            const blob = await response.blob();
            const url = URL.createObjectURL(blob);

            window.open(url, '_blank');

            setTimeout(() => URL.revokeObjectURL(url), 100);
        } else {
            mostrarAlerta('Error al generar vista previa', 'error');
        }
    } catch (error) {
        console.error('Error:', error);
        mostrarAlerta('Error al generar vista previa', 'error');
    }
}

async function descargarPdfOrden(id) {
    try {
        const response = await fetch('/Orden/GenerarPdf', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ idOrden: id })
        });

        if (response.ok) {
            const blob = await response.blob();
            const url = window.URL.createObjectURL(blob);
            const a = document.createElement('a');
            a.href = url;
            a.download = `OC_${id}_${new Date().toLocaleDateString('es-ES').replace(/\//g, '')}.pdf`;
            document.body.appendChild(a);
            a.click();
            window.URL.revokeObjectURL(url);
            document.body.removeChild(a);

            mostrarAlerta('PDF descargado exitosamente', 'success');
        } else {
            mostrarAlerta('Error al generar PDF', 'error');
        }
    } catch (error) {
        console.error('Error:', error);
        mostrarAlerta('Error al generar PDF', 'error');
    }
}

async function verEstadoOrden(id) {
    try {
        const responseOrden = await fetch(`/Orden/GetOrden?id=${id}`);
        const dataOrden = await responseOrden.json();

        if (dataOrden.tipo === 'error') {
            mostrarAlerta(dataOrden.mensaje, 'error');
            return;
        }

        const responsePdf = await fetch('/Orden/GenerarPdfVistaPrevia', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ idOrden: id })
        });

        if (responsePdf.ok) {
            const blob = await responsePdf.blob();
            const url = URL.createObjectURL(blob);

            document.getElementById('iframePDFEstado').src = url;
            document.getElementById('numero_orden_modal').textContent = id;
            document.getElementById('id_orden_estado').value = id;
            await cargarArchivosOrden(id, 'lista_archivos_estado');

            const infoSolicitud = document.getElementById('info_solicitud_vinculada');
            const solicitudesUnicas = [...new Set(dataOrden.productos.map(p => p.idSolicitud))];

            if (solicitudesUnicas.length > 0) {
                let htmlSolicitudes = '<div class="text-[11px] text-gray-400 space-y-1">';

                for (const idSol of solicitudesUnicas) {
                    try {
                        const responseSolicitud = await fetch(`/Solicitudes/GetSolicitud?id=${idSol}`);
                        const dataSolicitud = await responseSolicitud.json();

                        let fechaReq = 'No especificada';
                        if (dataSolicitud.solicitud && dataSolicitud.solicitud.frequerimiento) {
                            const fecha = new Date(dataSolicitud.solicitud.frequerimiento);
                            fechaReq = fecha.toLocaleDateString('es-ES');
                        }

                        htmlSolicitudes += `
                            <div>
                                <span class="font-semibold">Solicitud:</span> #${idSol} | 
                                <span class="font-semibold">F. Requerimiento:</span> ${fechaReq}
                            </div>
                        `;
                    } catch (error) {
                        console.error(`Error al cargar solicitud ${idSol}:`, error);
                    }
                }

                htmlSolicitudes += '</div>';
                infoSolicitud.innerHTML = htmlSolicitudes;
            } else {
                infoSolicitud.innerHTML = '<div class="text-[11px] text-gray-400">Sin solicitudes vinculadas</div>';
            }

            const responseHistorial = await fetch(`/Orden/GetHistorialEstados?idOrden=${id}`);
            const historial = await responseHistorial.json();

            const responsePermisos = await fetch('/Orden/GetTipoUsuario');
            const permisos = await responsePermisos.json();
            const esAlmacen = permisos.tipo === 'ALMACEN';
            const esPlanta = permisos.tipo === 'PLANTA';
            const esGerencia = permisos.tipo === 'GERENCIA';

            const contenedorEstados = document.getElementById('contenedor_estados');
            contenedorEstados.innerHTML = '';

            const esImportacion = dataOrden.orden.esImportacion;
            const estadoActual = dataOrden.orden.idEstadoSolicitud;

            const estadosFiltrados = estadosDisponibles.filter(e => {
                if (esImportacion) {
                    return ![6].includes(e.id);
                } else {
                    return [1, 2, 3, 8, 9, 11].includes(e.id);
                }
            });

            estadosFiltrados.forEach(estado => {
                const isActual = estado.id === estadoActual;

                const umbralBloqueo = 3;
                const esBloqueadoPorRetroceso = estadoActual >= umbralBloqueo && estado.id < estadoActual;
                const esBloqueadoPorAlmacen = esAlmacen && estado.id !== 9;
                const esBloqueadoPorPlanta = esPlanta;
                const esBloqueadoPorGerencia = esGerencia && estado.id !== 3 && estado.id !== 11;
                const esBloqueadoPorAnulado = estado.id === 11 && !esGerencia && window.tipoUsuario !== 'ADMINISTRADOR';
                const esBloqueado = esBloqueadoPorRetroceso || esBloqueadoPorAlmacen || esBloqueadoPorPlanta || esBloqueadoPorGerencia || esBloqueadoPorAnulado;

                const ultimoCambio = historial.find(h => h.estadoNuevo === estado.estado);

                const btn = document.createElement('button');

                let btnClass = '';
                if (isActual) {
                    btnClass = `badge-estado estado-${estado.id} ring-2 ring-gray-400`;
                } else if (esBloqueado) {
                    btnClass = 'bg-gray-700 text-gray-300 cursor-not-allowed opacity-50';
                } else {
                    btnClass = 'bg-gray-700 text-gray-300 hover:bg-gray-600';
                }

                btn.className = `px-2 py-1 rounded font-semibold transition-all ${btnClass}`;
                btn.style.fontSize = '10px';

                btn.innerHTML = `
                    <div>${estado.estado}</div>
                    ${ultimoCambio ? `<div style="font-size:9px" class="opacity-75">${ultimoCambio.usuario} - ${ultimoCambio.fecha}</div>` : ''}
                `;

                if (esBloqueado) {
                    btn.title = esBloqueadoPorPlanta
                        ? 'No tiene permisos para cambiar estados'
                        : esBloqueadoPorAlmacen
                            ? 'Solo puede cambiar a Recepción en almacenes'
                            : esBloqueadoPorGerencia
                                ? 'Solo puede cambiar a Anulado'
                                : 'No se puede retroceder a este estado';
                    btn.onclick = () => { };
                } else {
                    btn.title = estado.detalle || '';
                    btn.onclick = () => cambiarEstado(id, estado.id);
                }

                contenedorEstados.appendChild(btn);
            });

            const modal = new Modal(document.getElementById('modalEstadoPDF'));
            modal.show();
        }
    } catch (error) {
        console.error('Error:', error);
        mostrarAlerta('Error al cargar la orden', 'error');
    }
}

async function cambiarEstado(idOrden, nuevoEstado) {
    const responseOrden = await fetch(`/Orden/GetOrden?id=${idOrden}`);
    const dataOrden = await responseOrden.json();
    const estadoActual = dataOrden.orden.idEstadoSolicitud;

    if (nuevoEstado === estadoActual) {
        mostrarAlerta('LA ORDEN YA SE ENCUENTRA EN ESTE ESTADO', 'warning');
        return;
    }

    if (nuevoEstado < estadoActual && nuevoEstado !== 11) {
        mostrarAlerta('NO SE PUEDE RETROCEDER A UN ESTADO ANTERIOR', 'error');
        return;
    }

    const estadoNombre = estadosDisponibles.find(e => e.id === nuevoEstado)?.estado || 'este estado';

    let observacionAnulado = '';

    if (nuevoEstado === 11) {
        const result = await Swal.fire({
            title: '¿Rechazar orden de compra?',
            input: 'textarea',
            inputLabel: 'Motivo de rechazo',
            inputPlaceholder: 'Ingrese el motivo...',
            inputAttributes: {
                style: 'background:#374151; color:#fff; border:1px solid #4b5563; border-radius:6px;'
            },
            icon: 'warning',
            showCancelButton: true,
            confirmButtonColor: '#d33',
            cancelButtonColor: '#6b7280',
            confirmButtonText: 'Sí, rechazar',
            cancelButtonText: 'Cancelar',
            background: '#1f2937',
            color: '#ffffff'
        });

        if (!result.isConfirmed) return;
        observacionAnulado = result.value;
    } else {
        const result = await Swal.fire({
            title: '¿Cambiar estado de la orden?',
            html: `¿Está seguro de cambiar el estado a <strong>${estadoNombre}</strong>?`,
            icon: 'question',
            showCancelButton: true,
            confirmButtonColor: '#3085d6',
            cancelButtonColor: '#6b7280',
            confirmButtonText: 'Sí, cambiar',
            cancelButtonText: 'Cancelar',
            background: '#1f2937',
            color: '#ffffff'
        });

        if (!result.isConfirmed) return;
    }

    try {
        const response = await fetch('/Orden/CambiarEstado', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                idOrden: idOrden,
                nuevoEstado: nuevoEstado,
                observacion: observacionAnulado || null
            })
        });

        const data = await response.json();
        mostrarAlerta(data.mensaje, data.tipo);

        if (data.tipo === 'success') {
            await verEstadoOrden(idOrden);
            cargarOrdenes();
        }
    } catch (error) {
        console.error('Error:', error);
        mostrarAlerta('Error al cambiar estado', 'error');
    }
}

function cerrarModalEstadoPDF() {
    const modal = new Modal(document.getElementById('modalEstadoPDF'));
    modal.hide();
    document.getElementById('iframePDFEstado').src = '';
}


function mostrarAlerta(mensaje, tipo) {
    Swal.fire({
        position: "center",
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


document.addEventListener('DOMContentLoaded', function () {
    document.querySelectorAll('.uppercase').forEach(input => {
        input.addEventListener('input', function () {
            this.value = this.value.toUpperCase();
        });
    });
});

function autocompletarNit(razon) {
    const nits = {
        'SOALPRO S.R.L.': '1020409021',
        'CARSA INDUSTRIA Y COMERCIO': '193304025',
        'TECALIM S.A.': '166320021'
    };
    document.getElementById('nit_factura').value = nits[razon] || '';
}


