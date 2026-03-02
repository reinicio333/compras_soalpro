let gridApiAreas;

const gridOptionsAreas = {
    columnDefs: [
        { headerName: "ID", field: "id", maxWidth: 100, sort: 'desc' },
        { headerName: "Área", field: "nombre" },
        { headerName: "Estado", field: "estado", maxWidth: 150 },
        {
            headerName: "Acciones",
            minWidth: 180,
            filter: false,
            floatingFilter: false,
            cellRenderer: params => `
                <button onclick="editarArea(${params.data.id})" class="px-3 py-1 text-yellow-600 hover:bg-yellow-700 hover:text-white rounded-lg">
                    <i class="fas fa-edit text-sm"></i>
                </button>
                <button onclick="eliminarArea(${params.data.id})" class="px-3 py-1 text-red-600 hover:bg-red-700 hover:text-white rounded-lg">
                    <i class="fas fa-trash text-sm"></i>
                </button>`
        }
    ],
    defaultColDef: { flex: 1, filter: true, sortable: true, resizable: true, floatingFilter: true },
    pagination: true,
    paginationPageSize: 10,
    localeText: { noRowsToShow: "Sin áreas para mostrar" },
    onGridReady: async () => await cargarAreas()
};

document.addEventListener('DOMContentLoaded', () => {
    const gridDiv = document.getElementById('myGridAreas');
    gridApiAreas = agGrid.createGrid(gridDiv, gridOptionsAreas);

    document.getElementById('btnNuevaArea').addEventListener('click', abrirModalNuevaArea);
    document.getElementById('formularioArea').addEventListener('submit', guardarArea);
});

async function cargarAreas() {
    const response = await fetch('/AreasCorrespondencia/Listar');
    const data = await response.json();
    if (Array.isArray(data)) gridApiAreas.setGridOption('rowData', data);
}

function abrirModalNuevaArea() {
    document.getElementById('titleModalArea').textContent = 'NUEVA ÁREA';
    const form = document.getElementById('formularioArea');
    form.reset();
    form.Id.value = 0;
    form.Estado.value = 'A';
    new Modal(document.getElementById('modalArea')).show();
}

async function editarArea(id) {
    const response = await fetch(`/AreasCorrespondencia/Get?id=${id}`);
    const res = await response.json();
    if (res.tipo === 'error') return mostrarAlerta(res.mensaje, 'error');

    const form = document.getElementById('formularioArea');
    document.getElementById('titleModalArea').textContent = 'EDITAR ÁREA';
    form.Id.value = res.id;
    form.Nombre.value = res.nombre;
    form.Estado.value = res.estado;
    new Modal(document.getElementById('modalArea')).show();
}

async function guardarArea(e) {
    e.preventDefault();
    const form = document.getElementById('formularioArea');
    const formData = new FormData(form);

    const response = await fetch('/AreasCorrespondencia/Guardar', { method: 'POST', body: formData });
    const res = await response.json();
    mostrarAlerta(res.mensaje, res.tipo);

    if (res.tipo === 'success') {
        cerrarModalArea();
        await cargarAreas();
    }
}

function eliminarArea(id) {
    Swal.fire({
        title: '¿Eliminar área?',
        icon: 'warning',
        showCancelButton: true,
        confirmButtonText: 'Sí, eliminar',
        cancelButtonText: 'Cancelar',
        background: '#1f2937',
        color: '#ffffff'
    }).then(async result => {
        if (!result.isConfirmed) return;
        const response = await fetch(`/AreasCorrespondencia/Eliminar?id=${id}`);
        const res = await response.json();
        mostrarAlerta(res.mensaje, res.tipo);
        if (res.tipo === 'success') await cargarAreas();
    });
}

function cerrarModalArea() {
    new Modal(document.getElementById('modalArea')).hide();
}
