const modalTipoCambio = document.querySelector('#modalTipoCambio');
const titleTipoCambio = document.querySelector('#titleModalTipoCambio');
const btnNuevoTipoCambio = document.querySelector('#btnNuevoTipoCambio');
const formTipoCambio = document.querySelector('#formularioTipoCambio');

let gridApiTipoCambio;

const gridOptionsTipoCambio = {
    columnDefs: [
        { headerName: 'ID', field: 'id', width: 90 },
        { headerName: 'Fecha Inicio', field: 'fechaInicio' },
        { headerName: 'Fecha Fin', field: 'fechaFin' },
        {
            headerName: 'Tipo Cambio',
            field: 'valor',
            valueFormatter: params => Number(params.value).toFixed(4)
        },
        {
            headerName: 'Estado',
            field: 'estado',
            cellRenderer: params => params.value === '1'
                ? '<span class="text-green-400">ACTIVO</span>'
                : '<span class="text-red-400">INACTIVO</span>'
        },
        {
            headerName: 'Acciones',
            field: 'acciones',
            cellRenderer: params => `
                <button onclick="editarTipoCambio(${params.data.id})" class="px-3 py-0 text-yellow-400 hover:bg-yellow-600 hover:text-white rounded-lg" title="Editar">
                    <i class="fas fa-edit text-sm"></i>
                </button>
                <button onclick="eliminarTipoCambio(${params.data.id})" class="px-3 py-0 text-red-400 hover:bg-red-600 hover:text-white rounded-lg" title="Eliminar">
                    <i class="fas fa-trash text-sm"></i>
                </button>`
        }
    ],
    defaultColDef: {
        flex: 1,
        filter: true,
        sortable: true,
        resizable: true,
        minWidth: 100
    },
    pagination: true,
    paginationPageSize: 10,
    onGridReady: () => cargarTiposCambio()
};

function cargarTiposCambio() {
    fetch('/TipoCambio/Listar')
        .then(r => r.json())
        .then(data => gridApiTipoCambio.setGridOption('rowData', data))
        .catch(() => mostrarAlerta('Error al cargar tipos de cambio', 'error'));
}

gridApiTipoCambio = agGrid.createGrid(document.getElementById('myGridTipoCambio'), gridOptionsTipoCambio);

btnNuevoTipoCambio.addEventListener('click', () => {
    titleTipoCambio.textContent = 'NUEVO TIPO DE CAMBIO';
    formTipoCambio.reset();
    formTipoCambio.id.value = '';
    formTipoCambio.estado.value = '1';
    new Modal(modalTipoCambio).show();
});

formTipoCambio.addEventListener('submit', e => {
    e.preventDefault();

    const data = new FormData(formTipoCambio);
    const http = new XMLHttpRequest();
    http.open('POST', '/TipoCambio/Guardar', true);
    http.send(data);

    http.onreadystatechange = function () {
        if (this.readyState === 4 && this.status === 200) {
            const res = JSON.parse(this.responseText);
            mostrarAlerta(res.mensaje, res.tipo);
            if (res.tipo === 'success') {
                cerrarModalTipoCambio();
                cargarTiposCambio();
            }
        }
    };
});

function editarTipoCambio(id) {
    fetch(`/TipoCambio/Get?id=${id}`)
        .then(r => r.json())
        .then(res => {
            if (res.tipo === 'error') {
                mostrarAlerta(res.mensaje, 'error');
                return;
            }

            titleTipoCambio.textContent = 'EDITAR TIPO DE CAMBIO';
            formTipoCambio.id.value = res.id;
            formTipoCambio.fechaInicio.value = res.fechaInicio;
            formTipoCambio.fechaFin.value = res.fechaFin;
            formTipoCambio.valor.value = res.valor;
            formTipoCambio.estado.value = res.estado;
            new Modal(modalTipoCambio).show();
        });
}

function eliminarTipoCambio(id) {
    eliminarRegistro(
        '¿ESTÁ SEGURO DE ELIMINAR?',
        'EL RANGO DE TIPO DE CAMBIO SE ELIMINARÁ',
        'SÍ, ELIMINAR',
        `/TipoCambio/Eliminar?id=${id}`
    );
}

function cerrarModalTipoCambio() {
    new Modal(modalTipoCambio).hide();
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
