// Variables Globales
const modalSolicitud = document.querySelector("#modalSolicitud");
const title = document.querySelector("#titleModalSolicitud");
const btnNuevaSolicitud = document.querySelector("#btnNuevaSolicitud");
const btnAgregarProducto = document.querySelector("#btnAgregarProducto");
const productosContainer = document.querySelector("#productosLista");
const frm = document.querySelector("#formularioSolicitud");
const btnGuardarSolicitud = document.querySelector("#btnGuardarSolicitud");

function toggleButtonLoading(button, isLoading, loadingText = "Procesando...") {
    if (!button) return;

    if (isLoading) {
        if (!button.dataset.originalHtml) {
            button.dataset.originalHtml = button.innerHTML;
        }
        button.disabled = true;
        button.classList.add("opacity-75", "cursor-not-allowed");
        button.innerHTML = `
            <span class="inline-flex items-center">
                <i class="fas fa-spinner fa-spin mr-2"></i>${loadingText}
            </span>`;
        return;
    }

    button.disabled = false;
    button.classList.remove("opacity-75", "cursor-not-allowed");
    if (button.dataset.originalHtml) {
        button.innerHTML = button.dataset.originalHtml;
    }
}

let gridApi;
let contadorProductos = 0;


const gridOptions = {
    columnDefs: [
        { headerName: "ID", field: "id", width: 80, sort: 'desc' },
        {
            headerName: "Fecha",
            field: "fecha",
            valueFormatter: params => {
                if (params.value) {
                    const fecha = new Date(params.value);
                    return fecha.toLocaleDateString('es-ES');
                }
                return '';
            }
        },
        {
            headerName: "Fecha Requerimiento",
            field: "frequerimiento",
            valueFormatter: params => {
                if (params.value) {
                    const fecha = new Date(params.value);
                    return fecha.toLocaleDateString('es-ES');
                }
                return 'N/A';
            }
        },
        { headerName: "Referencia", field: "referencia" },
        { headerName: "Solicitante", field: "solicitante" },
        {
            filter: false,
            floatingFilter: false,
            headerName: "Acciones",
            field: "acciones",
            width: 280,
            cellRenderer: params => {
                // Verificar si tiene productos en uso (se cargará dinámicamente)
                const idSolicitud = params.data.id;

                return `
            <button onclick="vistaPreviaSolicitud('${params.data.id}')"
                    class="px-3 py-1 text-blue-400 hover:bg-blue-600 hover:text-white rounded-lg transition-all duration-200"
                    title="Vista Previa">
                <i class="fas fa-eye text-sm"></i>
            </button>
            <button onclick="descargarPdfSolicitud('${params.data.id}')"
                    class="px-3 py-1 text-green-400 hover:bg-green-600 hover:text-white rounded-lg transition-all duration-200"
                    title="Descargar PDF">
                <i class="fas fa-file-download text-sm"></i>
            </button>
            <button onclick="verificarYEditar('${params.data.id}')"
                    id="btn-editar-${params.data.id}"
                    class="px-3 py-1 text-yellow-400 hover:bg-yellow-600 hover:text-white rounded-lg transition-all duration-200"
                    title="Editar">
                <i class="fas fa-edit text-sm"></i>
            </button>
            <button onclick="verificarYEliminar('${params.data.id}', this)"
                    id="btn-eliminar-${params.data.id}"
                    class="px-3 py-1 text-red-400 hover:bg-red-600 hover:text-white rounded-lg transition-all duration-200"
                    title="Eliminar">
                <i class="fas fa-trash text-sm"></i>
            </button>
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
        noRowsToShow: "Sin solicitudes para mostrar",
    },
    onGridReady: () => {
        cargarSolicitudes();
    }
};


function cargarSolicitudes() {
    fetch('/Solicitudes/ListarSolicitudes')
        .then(resp => resp.json())
        .then(data => {
            console.log("Solicitudes recibidas:", data);
            if (Array.isArray(data)) {
                gridApi.setGridOption('rowData', data);
            } else {
                console.error("Datos inválidos:", data);
            }
        })
        .catch(err => console.error("Error al cargar solicitudes:", err));
}

const gridDiv = document.getElementById('myGridSolicitudes');
gridApi = agGrid.createGrid(gridDiv, gridOptions);


function agregarProducto() {
    contadorProductos++;
    const productoHTML = `
    <div class="producto-item bg-gray-700 rounded-lg p-3 border border-gray-600" data-index="${contadorProductos}">
        <div class="grid grid-cols-13 gap-2 items-center">
            <!-- Número -->
            <div class="col-span-1 text-center">
                <span class="text-xs font-semibold text-green-400">
                    <i class="fas fa-box mr-1"></i>#${contadorProductos}
                </span>
            </div>

            <!-- Selectize Producto -->
            <div class="col-span-2">
                <select class="select-producto-${contadorProductos}"
                        id="select_producto_${contadorProductos}"
                        required>
                </select>
               
                <input type="hidden"
                       name="productos[${contadorProductos}][codigo]"
                       id="codigo_${contadorProductos}">
                <!-- Y la descripción también -->
                <input type="hidden"
                       name="productos[${contadorProductos}][descripcion]"
                       id="descripcion_${contadorProductos}">
                <input type="hidden"
                       name="productos[${contadorProductos}][unidad]"
                       id="unidad_${contadorProductos}">
            </div>

            <!-- Selectize Proveedor -->
            <div class="col-span-2">
                <select class="select-proveedor-${contadorProductos}"
                        name="productos[${contadorProductos}][proveedor]"
                        required>
                </select>
            </div>
            <input type="hidden"
       name="productos[${contadorProductos}][codProveedor]"
       id="codProveedor_${contadorProductos}">
            <!-- Características -->
            <div class="col-span-1">
                <input type="text"
                       name="productos[${contadorProductos}][caracteristicas]"
                       class="uppercase bg-gray-800 border border-gray-600 text-white text-xs rounded-lg focus:ring-blue-500 focus:border-blue-500 block w-full p-2 placeholder-gray-400"
                       placeholder="Características" >
            </div>
            <div class="col-span-1">
                <input type="date"
                       name="productos[${contadorProductos}][frequerimiento_item]"
                       id="frequerimiento_item_${contadorProductos}"
                       class="bg-gray-800 border border-gray-600 text-white text-xs rounded-lg focus:ring-blue-500 focus:border-blue-500 block w-full p-2">
            </div>
            <!-- Req. Días -->
            <input type="number"
                       id="frequerimiento_dias_${contadorProductos}"
                       name="productos[${contadorProductos}][frequerimiento_dias]"
                       class="bg-gray-600 border border-gray-500 text-gray-300 text-xs rounded-lg block w-full p-2 cursor-not-allowed"
                       placeholder="Auto" readonly>
            <!-- Último Precio -->
            <div class="col-span-1">
                <input type="text"
                       id="ultimoPrecio_display_${contadorProductos}"
                       class="bg-gray-600 border border-gray-500 text-gray-300 text-xs rounded-lg block w-full p-2 cursor-not-allowed"
                       placeholder="Sin precio"
                       readonly>
                <input type="hidden"
                       id="ultimoPrecio_${contadorProductos}"
                       name="productos[${contadorProductos}][ultimoPrecio]">
            </div>

            <!-- Última Compra -->
            <div class="col-span-1">
                <input type="text"
                       id="fultimaCompra_display_${contadorProductos}"
                       class="bg-gray-600 border border-gray-500 text-gray-300 text-xs rounded-lg block w-full p-2 cursor-not-allowed"
                       placeholder="Sin fecha"
                       readonly>
                <input type="hidden"
                       id="fultimaCompra_${contadorProductos}"
                       name="productos[${contadorProductos}][fultimaCompra]">
            </div>
            <!-- Unidad (readonly) -->
            <div class="col-span-1">
                <input type="text"
                       id="unidad_display_${contadorProductos}"
                       class="bg-gray-600 border border-gray-500 text-gray-300 text-xs rounded-lg block w-full p-2 cursor-not-allowed"
                       readonly>
            </div>

            <!-- Cantidad -->
            <div class="col-span-1">
                <input type="number" step="0.01"
                       name="productos[${contadorProductos}][cantidad]"
                       class="bg-gray-800 border border-gray-600 text-white text-xs rounded-lg focus:ring-blue-500 focus:border-blue-500 block w-full p-2 placeholder-gray-400"
                       placeholder="0.00" min="0.01" required>
            </div>

            <!-- Botón eliminar -->
            <div class="col-span-1 text-center">
                <button type="button"
                        onclick="eliminarProducto(${contadorProductos})"
                        class="text-red-400 hover:text-red-300 transition-colors p-1">
                    <i class="fas fa-times-circle text-lg"></i>
                </button>
            </div>
        </div>
    </div>
`;

    productosContainer.insertAdjacentHTML('beforeend', productoHTML);
    initializeSelectizeForProduct(contadorProductos);
    const fechaCabecera = document.getElementById('frequerimiento').value;
    document.getElementById(`frequerimiento_item_${contadorProductos}`).value = fechaCabecera;
}

function initializeSelectizeForProduct(index) {
    // Inicializar Selectize para Producto
    $(`.select-producto-${index}`).selectize({
        valueField: 'codItem',
        labelField: 'text',
        searchField: ['codItem', 'nomItem', 'text'],
        placeholder: 'Buscar por código o descripción',
        preload: 'focus',
        load: function (query, callback) {
            $.ajax({
                url: '/Solicitudes/BuscarProductoProveedor',
                type: 'GET',
                dataType: 'json',
                data: { q: query },
                success: function (res) {
                    callback(res);
                },
                error: function () {
                    callback();
                }
            });
        },
        render: {
            option: function (data, escape) {
                return `<div class="p-2 hover:bg-gray-100 transition-colors border-b border-gray-200">
                    <div class="font-semibold text-gray-800 text-sm">${escape(data.codItem)}</div>
                    <div class="text-gray-600 text-xs">${escape(data.nomItem)}</div>
                </div>`;
            },
            item: function (data, escape) {
                return `<div class="text-sm">${escape(data.codItem)} - ${escape(data.nomItem)}</div>`;
            }
        },
        dropdownParent: 'body',
        onChange: function (value) {
            if (value) {
                const item = this.options[value];
                if (item) {
                    $(`#codigo_${index}`).val(item.codItem);
                    $(`#descripcion_${index}`).val(item.nomItem);
                    $(`#unidad_${index}`).val(item.unidad);
                    $(`#unidad_display_${index}`).val(item.unidad);

                    // Limpiar precio y fecha al cambiar producto
                    $(`#ultimoPrecio_${index}`).val('');
                    $(`#ultimoPrecio_display_${index}`).val('');
                    $(`#fultimaCompra_${index}`).val('');
                    $(`#fultimaCompra_display_${index}`).val('');

                    const selectizeProveedor = $(`.select-proveedor-${index}`)[0].selectize;
                    selectizeProveedor.clear(true);
                    selectizeProveedor.clearOptions();

                    // Cargar proveedores del producto seleccionado
                    cargarProveedores(index, value);
                }
            } else {
                $(`#codigo_${index}`).val('');
                $(`#descripcion_${index}`).val('');
                $(`#unidad_${index}`).val('');
                $(`#unidad_display_${index}`).val('');
                $(`#ultimoPrecio_${index}`).val('');
                $(`#ultimoPrecio_display_${index}`).val('');
                $(`#fultimaCompra_${index}`).val('');
                $(`#fultimaCompra_display_${index}`).val('');
            }
        }
    });


    $(`.select-proveedor-${index}`).selectize({
        valueField: 'nomProveedor',
        labelField: 'nomProveedor',
        searchField: ['codProveedor', 'nomProveedor'],
        placeholder: 'Seleccione producto primero',
        options: [],
        dropdownParent: 'body',
        render: {
            option: function (data, escape) {
                return `<div class="p-2 hover:bg-gray-100 transition-colors border-b border-gray-200">
                    <div class="font-semibold text-gray-800 text-sm">${escape(data.nomProveedor)}</div>
                    <div class="text-gray-600 text-xs">Cod: ${escape(data.codProveedor)}</div>
                </div>`;
            },
            item: function (data, escape) {
                return `<div class="text-sm">${escape(data.nomProveedor)}</div>`;
            }
        },

        onChange: function (value) {
            if (value) {
                const item = this.options[value];
                if (item) {
                    $(`#codProveedor_${index}`).val(item.codProveedor);

                    // Obtener precio y fecha por proveedor+producto
                    const codItem = $(`#codigo_${index}`).val();
                    $.ajax({
                        url: '/Solicitudes/ObtenerPrecioPorProveedorProducto',
                        type: 'GET',
                        data: { codItem: codItem, codProveedor: item.codProveedor },
                        success: function (data) {
                            $(`#ultimoPrecio_${index}`).val(data.ultimoPrecio || 0);
                            $(`#ultimoPrecio_display_${index}`).val(data.ultimoPrecio ? parseFloat(data.ultimoPrecio).toFixed(2) : '0.00');
                            $(`#fultimaCompra_${index}`).val(data.fultimaCompra ? data.fultimaCompra.split('T')[0] : '');
                            $(`#fultimaCompra_display_${index}`).val(data.fultimaCompra ? new Date(data.fultimaCompra).toLocaleDateString('es-ES') : 'Sin datos');
                            $(`#frequerimiento_dias_${index}`).val(data.leadTime || '');

                        }
                    });
                }
            } else {
                $(`#codProveedor_${index}`).val('');
                $(`#ultimoPrecio_${index}`).val('');
                $(`#ultimoPrecio_display_${index}`).val('');
                $(`#fultimaCompra_${index}`).val('');
                $(`#fultimaCompra_display_${index}`).val('');
            }
        }
    });
}
function initializeSelectizeForProductEdit(index, detalle) {
    // Inicializar Selectize para Producto con el valor existente
    const selectProducto = $(`.select-producto-${index}`).selectize({
        valueField: 'codItem',
        labelField: 'text',
        searchField: ['codItem', 'nomItem', 'text'],
        placeholder: 'Buscar por código o descripción',
        preload: 'focus',
        load: function (query, callback) {
            $.ajax({
                url: '/Solicitudes/BuscarProductoProveedor',
                type: 'GET',
                dataType: 'json',
                data: { q: query },
                success: function (res) {
                    callback(res);
                },
                error: function () {
                    callback();
                }
            });
        },
        render: {
            option: function (data, escape) {
                return `<div class="p-2 hover:bg-gray-100 transition-colors border-b border-gray-200">
                    <div class="font-semibold text-gray-800 text-sm">${escape(data.codItem)}</div>
                    <div class="text-gray-600 text-xs">${escape(data.nomItem)}</div>
                </div>`;
            },
            item: function (data, escape) {
                return `<div class="text-sm">${escape(data.codItem)} - ${escape(data.nomItem)}</div>`;
            }
        },
        dropdownParent: 'body',
        onChange: function (value) {
            if (value) {
                const item = this.options[value];
                if (item) {
                    $(`#codigo_${index}`).val(item.codItem);
                    $(`#descripcion_${index}`).val(item.nomItem);
                    $(`#unidad_${index}`).val(item.unidad);
                    $(`#unidad_display_${index}`).val(item.unidad);

                    $(`#ultimoPrecio_${index}`).val('');
                    $(`#ultimoPrecio_display_${index}`).val('');
                    $(`#fultimaCompra_${index}`).val('');
                    $(`#fultimaCompra_display_${index}`).val('');

                    // VERIFICAR que el selectize del proveedor ya existe antes de usarlo
                    const proveedorControl = $(`.select-proveedor-${index}`)[0];
                    if (proveedorControl && proveedorControl.selectize) {
                        proveedorControl.selectize.clear(true);
                        proveedorControl.selectize.clearOptions();
                        cargarProveedores(index, value);
                    }
                }
            } else {
                $(`#codigo_${index}`).val('');
                $(`#descripcion_${index}`).val('');
                $(`#unidad_${index}`).val('');
                $(`#unidad_display_${index}`).val('');
                $(`#ultimoPrecio_${index}`).val('');
                $(`#ultimoPrecio_display_${index}`).val('');
                $(`#fultimaCompra_${index}`).val('');
                $(`#fultimaCompra_display_${index}`).val('');
            }
        }
    });

    // Cargar el producto actual
    const selectizeProducto = selectProducto[0].selectize;
    selectizeProducto.addOption({
        codItem: detalle.codigo,
        nomItem: detalle.descripcion,
        text: `${detalle.codigo} - ${detalle.descripcion}`,
        unidad: detalle.unidad
    });
    selectizeProducto.setValue(detalle.codigo);

    // Inicializar Selectize para Proveedor
    const selectProveedor = $(`.select-proveedor-${index}`).selectize({
        valueField: 'nomProveedor',
        labelField: 'nomProveedor',
        searchField: ['codProveedor', 'nomProveedor'],
        placeholder: 'Seleccione proveedor',
        dropdownParent: 'body',
        render: {
            option: function (data, escape) {
                return `<div class="p-2 hover:bg-gray-100 transition-colors border-b border-gray-200">
                <div class="font-semibold text-gray-800 text-sm">${escape(data.nomProveedor)}</div>
                <div class="text-gray-600 text-xs">Cod: ${escape(data.codProveedor)}</div>
            </div>`;
            },
            item: function (data, escape) {
                return `<div class="text-sm">${escape(data.nomProveedor)}</div>`;
            }
        },
        onChange: function (value) {
            if (value) {
                const item = this.options[value];
                if (item) {
                    $(`#codProveedor_${index}`).val(item.codProveedor);

                    // Obtener precio y fecha por proveedor+producto
                    const codItem = $(`#codigo_${index}`).val();
                    $.ajax({
                        url: '/Solicitudes/ObtenerPrecioPorProveedorProducto',
                        type: 'GET',
                        data: { codItem: codItem, codProveedor: item.codProveedor },
                        success: function (data) {
                            $(`#ultimoPrecio_${index}`).val(data.ultimoPrecio || 0);
                            $(`#ultimoPrecio_display_${index}`).val(data.ultimoPrecio ? parseFloat(data.ultimoPrecio).toFixed(2) : '0.00');
                            $(`#fultimaCompra_${index}`).val(data.fultimaCompra ? data.fultimaCompra.split('T')[0] : '');
                            $(`#fultimaCompra_display_${index}`).val(data.fultimaCompra ? new Date(data.fultimaCompra).toLocaleDateString('es-ES') : 'Sin datos');
                            $(`#frequerimiento_dias_${index}`).val(data.leadTime || '');
                            $(`#frequerimiento_dias_${index}`).closest('div').find('input[readonly]').val(data.leadTime || '');

                        }
                    });
                }
            } else {
                $(`#codProveedor_${index}`).val('');
                $(`#ultimoPrecio_${index}`).val('');
                $(`#ultimoPrecio_display_${index}`).val('');
                $(`#fultimaCompra_${index}`).val('');
                $(`#fultimaCompra_display_${index}`).val('');
            }
        }
    });

    // Cargar proveedores y seleccionar el actual
    $.ajax({
        url: '/Solicitudes/ObtenerProveedoresPorProducto',
        type: 'GET',
        data: { codItem: detalle.codigo },
        success: function (proveedores) {
            const selectizeProveedor = selectProveedor[0].selectize;
            selectizeProveedor.addOption(proveedores);
            selectizeProveedor.setValue(detalle.proveedor);
            const proveedorSeleccionado = proveedores.find(p => p.nomProveedor === detalle.proveedor);
            if (proveedorSeleccionado) {
                $(`#codProveedor_${index}`).val(proveedorSeleccionado.codProveedor);

                // Cargar precio y fecha al editar
                $.ajax({
                    url: '/Solicitudes/ObtenerPrecioPorProveedorProducto',
                    type: 'GET',
                    data: { codItem: detalle.codigo, codProveedor: proveedorSeleccionado.codProveedor },
                    success: function (precio) {
                        $(`#ultimoPrecio_${index}`).val(precio.ultimoPrecio || 0);
                        $(`#ultimoPrecio_display_${index}`).val(precio.ultimoPrecio ? parseFloat(precio.ultimoPrecio).toFixed(2) : '0.00');
                        $(`#fultimaCompra_${index}`).val(precio.fultimaCompra ? precio.fultimaCompra.split('T')[0] : '');
                        $(`#fultimaCompra_display_${index}`).val(precio.fultimaCompra ? new Date(precio.fultimaCompra).toLocaleDateString('es-ES') : 'Sin datos');
                        $(`#frequerimiento_dias_${index}`).val(precio.leadTime || '');
                        $(`#frequerimiento_dias_${index}`).closest('div').find('input[readonly]').val(precio.leadTime || '');
                    }
                });
            }
        }
    });
}
function cargarProveedores(index, codItem) {
    $.ajax({
        url: '/Solicitudes/ObtenerProveedoresPorProducto',
        type: 'GET',
        dataType: 'json',
        data: { codItem: codItem },
        success: function (proveedores) {
            // AGREGA la verificación
            const proveedorControl = $(`.select-proveedor-${index}`)[0];
            if (!proveedorControl || !proveedorControl.selectize) return;

            const selectizeProveedor = proveedorControl.selectize;
            selectizeProveedor.clearOptions();
            selectizeProveedor.clear(true);
            selectizeProveedor.addOption(proveedores);
            selectizeProveedor.enable();
            selectizeProveedor.settings.placeholder = 'Seleccione proveedor';
            selectizeProveedor.updatePlaceholder();
        },
        error: function () {
            console.error('Error al cargar proveedores');
        }
    });
}

function eliminarProducto(index) {
    const producto = document.querySelector(`.producto-item[data-index="${index}"]`);
    if (producto) {
        producto.remove();
        renumerarProductos();
    }
}

function renumerarProductos() {
    const productos = document.querySelectorAll('.producto-item');
    productos.forEach((producto, index) => {
        const numero = index + 1;
        const span = producto.querySelector('span.text-green-400');
        if (span) {
            span.innerHTML = `<i class="fas fa-box mr-1"></i>#${numero}`;
        }
    });
}


btnNuevaSolicitud.addEventListener("click", function () {
    title.textContent = "SOLICITUD DE COMPRA (BIENES O SERVICIOS)";
    frm.id.value = '';
    frm.reset();

    const hoy = new Date();
    const fechaLocal = new Date(hoy.getTime() - (hoy.getTimezoneOffset() * 60000)).toISOString().split('T')[0];
    frm.fecha.value = fechaLocal;
    frm.frequerimiento.value = fechaLocal;
    destruirSelectizesPrevios();
    document.querySelector("#productosLista").innerHTML = '';
    contadorProductos = 0;


    const modal = new Modal(modalSolicitud);
    modal.show();
});

btnAgregarProducto.addEventListener("click", agregarProducto);

frm.addEventListener("submit", function (e) {
    e.preventDefault();

    const productos = document.querySelectorAll('.producto-item');
    if (productos.length === 0) {
        alertaPersonalizada("warning", "DEBE AGREGAR AL MENOS UN PRODUCTO");
        return;
    }

    const cantidades = document.querySelectorAll('input[name*="[cantidad]"]');
    for (const input of cantidades) {
        const val = parseFloat(input.value);
        if (isNaN(val) || val <= 0) {
            alertaPersonalizada("warning", "LA CANTIDAD DEBE SER MAYOR A CERO");
            input.focus();
            return;
        }
    }
    const data = new FormData(frm);

    const dataMayusculas = new FormData();

    for (let [key, value] of data.entries()) {
        if (typeof value === 'string') {
            if (!key.includes('fecha') && key !== 'cantidad' &&
                !key.includes('cantidad') && !key.includes('[cantidad]')) {
                value = value.toUpperCase();
            }
        }
        dataMayusculas.append(key, value);
    }

    const http = new XMLHttpRequest();
    const url = frm.id.value ? "/Solicitudes/Actualizar" : "/Solicitudes/Guardar";

    toggleButtonLoading(btnGuardarSolicitud, true, "Guardando...");
    http.open("POST", url, true);
    http.send(dataMayusculas);

    http.onreadystatechange = function () {
        if (this.readyState == 4 && this.status == 200) {
            const res = JSON.parse(this.responseText);
            alertaPersonalizada(res.tipo, res.mensaje);
            if (res.tipo == 'success') {
                frm.reset();
                document.querySelector("#productosLista").innerHTML = '';
                contadorProductos = 0;
                const modal = new window.Modal(modalSolicitud);
                modal.hide();
                cargarSolicitudes();
            }
        }
    };

    http.onerror = function () {
        alertaPersonalizada('error', 'Error al guardar la solicitud');
    };

    http.onloadend = function () {
        toggleButtonLoading(btnGuardarSolicitud, false);
    };
});


function vistaPreviaSolicitud(id) {
    const iframe = document.getElementById('iframePDFSolicitud');
    iframe.src = `/Solicitudes/VistaPreviaPdfSolicitud?id=${id}`;

    const modal = document.getElementById('modalPreviewSolicitud');
    modal.classList.remove('hidden');
    modal.classList.add('flex');
}

function descargarPdfSolicitud(id) {
    fetch(`/Solicitudes/DescargarPdfSolicitud?id=${id}`)
        .then(response => response.blob())
        .then(blob => {
            const url = window.URL.createObjectURL(blob);
            const a = document.createElement('a');
            a.href = url;
            a.download = `Solicitud_${id}_${new Date().toLocaleDateString('es-ES').replace(/\//g, '')}.pdf`;
            document.body.appendChild(a);
            a.click();
            window.URL.revokeObjectURL(url);
            document.body.removeChild(a);
        })
        .catch(err => {
            console.error('Error al descargar:', err);
            alertaPersonalizada('error', 'Error al descargar el PDF');
        });
}
function cerrarModalPreviewSolicitud() {
    const modal = document.getElementById('modalPreviewSolicitud');
    modal.classList.add('hidden');
    modal.classList.remove('flex');
    document.getElementById('iframePDFSolicitud').src = '';
}

function editarSolicitud(id) {
    fetch('/Solicitudes/GetSolicitud?id=' + id)
        .then(resp => resp.json())
        .then(data => {
            if (data.tipo === 'error') {
                alertaPersonalizada('error', data.mensaje);
                return;
            }

            title.textContent = `MODIFICAR SOLICITUD DE COMPRA #${data.solicitud.id}`;

            frm.id.value = data.solicitud.id;

            const fecha = new Date(data.solicitud.fecha);
            frm.fecha.value = fecha.toISOString().split('T')[0];

            const tieneProductosEnUso = data.detalles.some(d => d.estado !== "Creado");

            const freq = data.solicitud.frequerimiento
                ? new Date(data.solicitud.frequerimiento)
                : new Date();
            frm.frequerimiento.value = new Date(freq.getTime() - (freq.getTimezoneOffset() * 60000)).toISOString().split('T')[0];

            if (tieneProductosEnUso) {
                frm.frequerimiento.setAttribute('readonly', true);
                frm.frequerimiento.classList.add('bg-gray-600', 'cursor-not-allowed');
                frm.frequerimiento.title = "No se puede modificar porque hay productos en órdenes de compra";
            } else {
                frm.frequerimiento.removeAttribute('readonly');
                frm.frequerimiento.classList.remove('bg-gray-600', 'cursor-not-allowed');
                frm.frequerimiento.title = "";
            }

            frm.referencia.value = data.solicitud.referencia || '';
            frm.solicitante.value = data.solicitud.solicitante || '';

            destruirSelectizesPrevios();
            document.querySelector("#productosLista").innerHTML = '';
            contadorProductos = 0;

            data.detalles.forEach(detalle => {
                contadorProductos++;
                const esPendiente = detalle.estado !== "Creado";
                const esManual = detalle.codigo === 'MANUAL';
                const disabledClass = esPendiente ? 'pointer-events-none opacity-60' : '';
                const readonlyAttr = esPendiente ? 'disabled' : '';
                const tooltipPendiente = esPendiente ? 'title="Producto en uso - No se puede modificar"' : '';

                const productoHTML = `
                <div class="producto-item bg-gray-700 rounded-lg p-3 border ${esPendiente ? 'border-orange-500' : esManual ? 'border-purple-600' : 'border-gray-600'}" data-index="${contadorProductos}" ${esManual ? 'data-manual="true"' : ''}>
                    ${esPendiente ? `<div class="mb-2 text-xs text-orange-400 font-semibold"><i class="fas fa-lock mr-1"></i>${detalle.estado} - Orden de Compra ${detalle.numeroOrden ? '#' + detalle.numeroOrden : ''}</div>` : ''}
                    <div class="grid grid-cols-13 gap-2 items-center">

                        <!-- # -->
                        <div class="col-span-1 text-center">
                            <span class="text-xs font-semibold ${esPendiente ? 'text-orange-400' : esManual ? 'text-purple-400' : 'text-green-400'}">
                                <i class="fas ${esManual ? 'fa-pencil-alt' : 'fa-box'} mr-1"></i>#${contadorProductos}
                            </span>
                        </div>

                        <!-- Producto -->
                        <div class="col-span-2 ${disabledClass}" ${tooltipPendiente}>
                            ${esManual ? `
                                <input type="text"
                                       name="productos[${contadorProductos}][descripcion]"
                                       id="descripcion_${contadorProductos}"
                                       class="uppercase ${esPendiente ? 'bg-gray-600 cursor-not-allowed' : 'bg-gray-800'} border border-gray-600 text-white text-xs rounded-lg block w-full p-2 placeholder-gray-400"
                                       value="${detalle.descripcion || ''}"
                                       ${esPendiente ? 'readonly' : ''} required>
                                <input type="hidden" name="productos[${contadorProductos}][codigo]" id="codigo_${contadorProductos}" value="MANUAL">
                                <input type="hidden" name="productos[${contadorProductos}][unidad]" id="unidad_${contadorProductos}" value="${detalle.unidad || ''}">
                            ` : `
                                <select class="select-producto-${contadorProductos}" id="select_producto_${contadorProductos}" ${readonlyAttr} required></select>
                                <input type="hidden" name="productos[${contadorProductos}][codigo]" id="codigo_${contadorProductos}" value="${detalle.codigo}">
                                <input type="hidden" name="productos[${contadorProductos}][descripcion]" id="descripcion_${contadorProductos}" value="${detalle.descripcion}">
                                <input type="hidden" name="productos[${contadorProductos}][unidad]" id="unidad_${contadorProductos}" value="${detalle.unidad}">
                            `}
                        </div>

                        <!-- Proveedor -->
                        <div class="col-span-2 ${disabledClass}" ${tooltipPendiente}>
                            ${esManual ? `
                                <input type="text"
                                       name="productos[${contadorProductos}][proveedor]"
                                       class="uppercase ${esPendiente ? 'bg-gray-600 cursor-not-allowed' : 'bg-gray-800'} border border-gray-600 text-white text-xs rounded-lg block w-full p-2 placeholder-gray-400"
                                       value="${detalle.proveedor || ''}"
                                       ${esPendiente ? 'readonly' : ''}
                                       placeholder="Proveedor">
                            ` : `
                                <select class="select-proveedor-${contadorProductos}" name="productos[${contadorProductos}][proveedor]" ${readonlyAttr} required></select>
                            `}
                        </div>

                        <input type="hidden"
                               name="productos[${contadorProductos}][codProveedor]"
                               id="codProveedor_${contadorProductos}"
                               value="${detalle.codProveedor || ''}">

                        <!-- Características -->
                        <div class="col-span-1 ${esPendiente ? disabledClass : ''}" ${esPendiente ? tooltipPendiente : ''}>
                            <input type="text" name="productos[${contadorProductos}][caracteristicas]"
                                   class="uppercase ${esPendiente ? 'bg-gray-600 cursor-not-allowed' : 'bg-gray-800'} border border-gray-600 text-white text-xs rounded-lg focus:ring-blue-500 focus:border-blue-500 block w-full p-2 placeholder-gray-400"
                                   value="${detalle.caracteristicas || ''}"
                                   placeholder="Opcional"
                                   ${esPendiente ? 'readonly' : ''}>
                        </div>
                        <!-- Fecha Requerimiento Item -->
                        <div class="col-span-1 ${esPendiente ? disabledClass : ''}">
                            <input type="date"
                                   name="productos[${contadorProductos}][frequerimiento_item]"
                                   id="frequerimiento_item_${contadorProductos}"
                                   class="${esPendiente ? 'bg-gray-600 cursor-not-allowed' : 'bg-gray-800'} border border-gray-600 text-white text-xs rounded-lg focus:ring-blue-500 focus:border-blue-500 block w-full p-2"
                                   value="${detalle.frequerimiento ? detalle.frequerimiento.split('T')[0] : (data.solicitud.frequerimiento ? data.solicitud.frequerimiento.split('T')[0] : '')}"
                                   ${esPendiente ? 'readonly' : ''}>
                        </div>

                        <!-- Req. Días -->
                        <div class="col-span-1">
                            <input type="text"
                                   class="bg-gray-600 border border-gray-500 ${esManual ? 'text-gray-500' : 'text-gray-300'} text-xs rounded-lg block w-full p-2 cursor-not-allowed"
                                   value="${esManual ? 'N/A' : (detalle.frequerimientoDias || '')}" readonly>
                            <input type="hidden"
                                   id="frequerimiento_dias_${contadorProductos}"
                                   name="productos[${contadorProductos}][frequerimiento_dias]"
                                   value="${esManual ? '' : (detalle.frequerimientoDias || '')}">
                        </div>

                        <!-- Último Precio -->
                        <div class="col-span-1">
                            <input type="text"
                                   id="ultimoPrecio_display_${contadorProductos}"
                                   class="bg-gray-600 border border-gray-500 ${esManual ? 'text-gray-500' : 'text-gray-300'} text-xs rounded-lg block w-full p-2 cursor-not-allowed"
                                   value="${esManual ? 'N/A' : ''}"
                                   placeholder="${esManual ? '' : 'Sin precio'}"
                                   readonly>
                            <input type="hidden"
                                   id="ultimoPrecio_${contadorProductos}"
                                   name="productos[${contadorProductos}][ultimoPrecio]"
                                   value="${esManual ? '0' : ''}">
                        </div>

                        <!-- Última Compra -->
                        <div class="col-span-1">
                            <input type="text"
                                   id="fultimaCompra_display_${contadorProductos}"
                                   class="bg-gray-600 border border-gray-500 ${esManual ? 'text-gray-500' : 'text-gray-300'} text-xs rounded-lg block w-full p-2 cursor-not-allowed"
                                   value="${esManual ? 'N/A' : ''}"
                                   placeholder="${esManual ? '' : 'Sin fecha'}"
                                   readonly>
                            <input type="hidden"
                                   id="fultimaCompra_${contadorProductos}"
                                   name="productos[${contadorProductos}][fultimaCompra]"
                                   value="">
                        </div>

                        <!-- Unidad -->
                        <div class="col-span-1 ${disabledClass}" ${tooltipPendiente}>
                            ${esManual ? `
                                <input type="text"
                                       id="unidad_display_${contadorProductos}"
                                       name="productos[${contadorProductos}][unidad_manual]"
                                       class="uppercase ${esPendiente ? 'bg-gray-600 cursor-not-allowed' : 'bg-gray-800'} border border-gray-600 text-white text-xs rounded-lg block w-full p-2 placeholder-gray-400"
                                       value="${detalle.unidad || ''}"
                                       ${esPendiente ? 'readonly' : ''}
                                       oninput="document.getElementById('unidad_${contadorProductos}').value = this.value"
                                       placeholder="Unidad" required>
                            ` : `
                                <input type="text" id="unidad_display_${contadorProductos}"
                                       class="bg-gray-600 border border-gray-500 text-gray-300 text-xs rounded-lg block w-full p-2 cursor-not-allowed"
                                       value="${detalle.unidad || ''}" readonly>
                            `}
                        </div>

                        <!-- Cantidad -->
                        <div class="col-span-1 ${esPendiente ? disabledClass : ''}" ${esPendiente ? tooltipPendiente : ''}>
                            <input type="number" step="0.01" name="productos[${contadorProductos}][cantidad]"
                                   class="${esPendiente ? 'bg-gray-600 cursor-not-allowed' : 'bg-gray-800'} border border-gray-600 text-white text-xs rounded-lg focus:ring-blue-500 focus:border-blue-500 block w-full p-2 placeholder-gray-400"
                                   value="${detalle.cantidad}"
                                    min="0.01"
                                    ${esPendiente ? 'readonly' : ''} required>
                        </div>

                        <!-- Eliminar -->
                        <div class="col-span-1 text-center">
                            ${esPendiente ?
                        `<i class="fas fa-lock text-orange-400" title="No se puede eliminar - Producto en uso"></i>` :
                        `<button type="button" onclick="eliminarProducto(${contadorProductos})"
                                        class="text-red-400 hover:text-red-300 transition-colors p-1">
                                    <i class="fas fa-times-circle text-lg"></i>
                                </button>`
                    }
                        </div>
                    </div>
                </div>`;

                productosContainer.insertAdjacentHTML('beforeend', productoHTML);

                if (esManual) {
                    // No inicializar selectize para productos manuales (sea pendiente o no)
                } else if (esPendiente) {
                    initializeSelectizeForProductEditReadonly(contadorProductos, detalle);
                } else {
                    initializeSelectizeForProductEdit(contadorProductos, detalle);
                }
            });

            const modal = new Modal(modalSolicitud);
            modal.show();
        })
        .catch(err => {
            console.error('Error:', err);
            alertaPersonalizada('error', 'Error al cargar la solicitud');
        });
}
function eliminarSolicitud(id, botonEliminar = null) {
    Swal.fire({
        title: '¿Está seguro de eliminar la solicitud?',
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
        if (!result.isConfirmed) {
            if (botonEliminar) {
                toggleButtonLoading(botonEliminar, false);
            }
            return;
        }

        fetch('/Solicitudes/EliminarSolicitud?id=' + id, {
            method: 'POST'
        })
            .then(resp => resp.json())
            .then(data => {
                alertaPersonalizada(data.tipo, data.mensaje);
                if (data.tipo === 'success') {
                    cargarSolicitudes();
                }
            })
            .catch(err => {
                alertaPersonalizada('error', 'Error al eliminar: ' + err);
            })
            .finally(() => {
                if (botonEliminar) {
                    toggleButtonLoading(botonEliminar, false);
                }
            });
    });
}


function cerrarModal() {
    const targetEl = document.getElementById('modalSolicitud');
    const modal = new Modal(targetEl);
    modal.hide();
}

function alertaPersonalizada(type, mensaje) {
    Swal.fire({
        position: "center",
        icon: type,
        title: mensaje,
        showConfirmButton: false,
        timer: 1500,
        background: '#1f2937',
        color: '#ffffff',
        toast: true,
        timerProgressBar: true
    });
}

document.querySelectorAll('.uppercase').forEach(input => {
    input.addEventListener('input', function () {
        this.value = this.value.toUpperCase();
    });
});
async function verificarYEditar(id) {
    editarSolicitud(id);
}

async function verificarYEliminar(id, botonEliminar = null) {
    try {
        const response = await fetch(`/Solicitudes/VerificarEstadoSolicitud?id=${id}`);
        const data = await response.json();

        if (data.tieneProductosEnUso) {
            Swal.fire({
                title: 'Acción no permitida',
                html: `<p>No se puede eliminar la solicitud porque tiene <strong>${data.cantidadProductosEnUso}</strong> producto(s) en uso en órdenes de compra.</p>
                       <p class="text-sm mt-2">Debe eliminar o editar las órdenes correspondientes primero.</p>`,
                icon: 'error',
                confirmButtonColor: '#3085d6',
                background: '#1f2937',
                color: '#ffffff'
            });
            return;
        }

        if (botonEliminar) {
            toggleButtonLoading(botonEliminar, true, '');
        }

        eliminarSolicitud(id, botonEliminar);
    } catch (error) {
        console.error('Error:', error);
        alertaPersonalizada('error', 'Error al verificar el estado de la solicitud');
        if (botonEliminar) {
            toggleButtonLoading(botonEliminar, false);
        }
    }
}
function initializeSelectizeForProductEditReadonly(index, detalle) {

    if (detalle.codigo === 'MANUAL') return;
    const selectProducto = $(`.select-producto-${index}`).selectize({
        valueField: 'codItem',
        labelField: 'text',
        searchField: ['codItem', 'nomItem', 'text'],
        dropdownParent: 'body',
        render: {
            item: function (data, escape) {
                return `<div class="text-sm">${escape(data.codItem)} - ${escape(data.nomItem)}</div>`;
            }
        }
    });

    const selectizeProducto = selectProducto[0].selectize;
    selectizeProducto.addOption({
        codItem: detalle.codigo,
        nomItem: detalle.descripcion,
        text: `${detalle.codigo} - ${detalle.descripcion}`,
        unidad: detalle.unidad
    });
    selectizeProducto.setValue(detalle.codigo);
    selectizeProducto.disable();

    const selectProveedor = $(`.select-proveedor-${index}`).selectize({
        valueField: 'nomProveedor',
        labelField: 'nomProveedor',
        dropdownParent: 'body',
        render: {
            item: function (data, escape) {
                return `<div class="text-sm">${escape(data.nomProveedor)}</div>`;
            }
        }
    });

    $.ajax({
        url: '/Solicitudes/ObtenerProveedoresPorProducto',
        type: 'GET',
        data: { codItem: detalle.codigo },
        success: function (proveedores) {
            const selectizeProveedor = selectProveedor[0].selectize;
            selectizeProveedor.addOption(proveedores);
            selectizeProveedor.setValue(detalle.proveedor);
            selectizeProveedor.disable();

            const proveedorSeleccionado = proveedores.find(p => p.nomProveedor === detalle.proveedor);
            if (proveedorSeleccionado) {
                $(`#codProveedor_${index}`).val(proveedorSeleccionado.codProveedor);
            }
        }
    });
}

function destruirSelectizesPrevios() {
    document.querySelectorAll('.producto-item').forEach(item => {
        const index = item.getAttribute('data-index');

        const selectProducto = $(`.select-producto-${index}`)[0];
        if (selectProducto && selectProducto.selectize) {
            selectProducto.selectize.destroy();
        }

        const selectProveedor = $(`.select-proveedor-${index}`)[0];
        if (selectProveedor && selectProveedor.selectize) {
            selectProveedor.selectize.destroy();
        }
    });
}

function agregarProductoManual() {
    contadorProductos++;
    const productoHTML = `
    <div class="producto-item bg-gray-700 rounded-lg p-3 border border-purple-600" data-index="${contadorProductos}" data-manual="true">
        <div class="grid grid-cols-13 gap-2 items-center">
            <!-- Número -->
            <div class="col-span-1 text-center">
                <span class="text-xs font-semibold text-purple-400">
                    <i class="fas fa-pencil-alt mr-1"></i>#${contadorProductos}
                </span>
            </div>

            <!-- Descripción manual -->
            <div class="col-span-2">
                <input type="text"
                       name="productos[${contadorProductos}][descripcion]"
                       id="descripcion_${contadorProductos}"
                       class="uppercase bg-gray-800 border border-gray-600 text-white text-xs rounded-lg focus:ring-purple-500 focus:border-purple-500 block w-full p-2 placeholder-gray-400"
                       placeholder="Descripción" required>
                <input type="hidden" name="productos[${contadorProductos}][codigo]" id="codigo_${contadorProductos}" value="MANUAL">
                <input type="hidden" name="productos[${contadorProductos}][unidad]" id="unidad_${contadorProductos}">
            </div>

            <!-- Proveedor manual -->
            <div class="col-span-2">
                <input type="text"
                       name="productos[${contadorProductos}][proveedor]"
                       class="uppercase bg-gray-800 border border-gray-600 text-white text-xs rounded-lg focus:ring-purple-500 focus:border-purple-500 block w-full p-2 placeholder-gray-400"
                       placeholder="Proveedor">
                <input type="hidden" name="productos[${contadorProductos}][codProveedor]" id="codProveedor_${contadorProductos}" value="">
            </div>

            <!-- Características (opcional) -->
            <div class="col-span-1">
                <input type="text"
                       name="productos[${contadorProductos}][caracteristicas]"
                       class="uppercase bg-gray-800 border border-gray-600 text-white text-xs rounded-lg focus:ring-purple-500 focus:border-purple-500 block w-full p-2 placeholder-gray-400"
                       placeholder="Opcional">
            </div>
            <!-- Fecha Requerimiento Item -->
            <div class="col-span-1">
                <input type="date"
                       name="productos[${contadorProductos}][frequerimiento_item]"
                       id="frequerimiento_item_${contadorProductos}"
                       class="bg-gray-800 border border-gray-600 text-white text-xs rounded-lg focus:ring-blue-500 focus:border-blue-500 block w-full p-2">
            </div>
            <!-- Req. Días — oculto, valor 0 -->
            <div class="col-span-1">
                <input type="text"
                       class="bg-gray-600 border border-gray-500 text-gray-500 text-xs rounded-lg block w-full p-2 cursor-not-allowed"
                       value="N/A" readonly>
                <input type="hidden"
                       name="productos[${contadorProductos}][frequerimiento_dias]"
                       value="">
            </div>

            <!-- Último Precio — oculto -->
            <div class="col-span-1">
                <input type="text"
                       class="bg-gray-600 border border-gray-500 text-gray-500 text-xs rounded-lg block w-full p-2 cursor-not-allowed"
                       value="N/A" readonly>
                <input type="hidden"
                       name="productos[${contadorProductos}][ultimoPrecio]"
                       value="0">
            </div>

            <!-- Última Compra — oculto -->
            <div class="col-span-1">
                <input type="text"
                       class="bg-gray-600 border border-gray-500 text-gray-500 text-xs rounded-lg block w-full p-2 cursor-not-allowed"
                       value="N/A" readonly>
                <input type="hidden"
                       name="productos[${contadorProductos}][fultimaCompra]"
                       value="">
            </div>

            <!-- Unidad manual -->
            <div class="col-span-1">
                <input type="text"
                       name="productos[${contadorProductos}][unidad_manual]"
                       id="unidad_display_${contadorProductos}"
                       class="uppercase bg-gray-800 border border-gray-600 text-white text-xs rounded-lg focus:ring-purple-500 focus:border-purple-500 block w-full p-2 placeholder-gray-400"
                       placeholder="Unidad"
                       oninput="document.getElementById('unidad_${contadorProductos}').value = this.value"
                       required>
            </div>

            <!-- Cantidad -->
            <div class="col-span-1">
                <input type="number" step="0.01"
                       name="productos[${contadorProductos}][cantidad]"
                       class="bg-gray-800 border border-gray-600 text-white text-xs rounded-lg focus:ring-purple-500 focus:border-purple-500 block w-full p-2 placeholder-gray-400"
                       placeholder="0.00" min="0.01" required>
            </div>

            <!-- Botón eliminar -->
            <div class="col-span-1 text-center">
                <button type="button"
                        onclick="eliminarProducto(${contadorProductos})"
                        class="text-red-400 hover:text-red-300 transition-colors p-1">
                    <i class="fas fa-times-circle text-lg"></i>
                </button>
            </div>
        </div>
    </div>`;

    productosContainer.insertAdjacentHTML('beforeend', productoHTML);
    const fechaCabecera = document.getElementById('frequerimiento').value;
    document.getElementById(`frequerimiento_item_${contadorProductos}`).value = fechaCabecera;
}

document.getElementById('btnAgregarProductoManual').addEventListener('click', agregarProductoManual);
