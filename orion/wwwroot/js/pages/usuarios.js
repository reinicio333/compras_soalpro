// Variables Globales
const modalRegistroUsuario = document.querySelector("#modalRegistroUsuario");
const title = document.querySelector("#titleModalUsuario");
const btnNuevoUsuario = document.querySelector("#btnNuevoUsuario");
const frm = document.querySelector("#formularioUsuario");

let gridApi; 
// Configuración del Grid
const gridOptions = {
    columnDefs: [
        { headerName: "ID", field: "id" },
        { headerName: "Nombre", field: "nombre" },
        { headerName: "Nombre", field: "nomCompleto" },
        { headerName: "Tipo", field: "idTipo" },
        { headerName: "Área", field: "area", filter: true },
        {
            headerName: "Estado",
            field: "estado",
            cellRenderer: params => {
                if (params.value === "1") {
                    return `<div class="py-0 text-green-600">
                        <i class="fas fa-check-circle"></i>
                        <span class="text-sm font-medium">Activo</span>
                    </div>`;
                } else {
                    return `<div class="py-0 text-red-600">
                        <i class="fas fa-times-circle"></i>
                        <span class="text-sm font-medium">Inactivo</span>
                    </div>`;
                }
            }
        },
        { headerName: "ID Usuario", field: "idusuario" },
        {
            headerName: "Acciones",
            field: "acciones",
            cellRenderer: params => {
                return `
                     <button onclick="editarUsuario('${params.data.id}')" class="px-4 py-0 text-yellow-400 hover:bg-yellow-600 hover:text-white rounded-lg transition-all duration-200" title="Editar">
                         <i class="fas fa-edit text-sm"></i>
                     </button>
                     <button onclick="eliminarUsuario('${params.data.id}')" class="px-4 py-0 text-red-400 hover:bg-red-600 hover:text-white rounded-lg transition-all duration-200" title="Eliminar">
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
        minWidth: 100
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
        selectAll: "Seleccionar todo",
        searchOoo: "Buscar...",
        blanks: "Vacíos",
        filterOoo: "Filtrar...",
        equals: "Igual",
        notEqual: "Diferente",
        lessThan: "Menor que",
        greaterThan: "Mayor que",
        contains: "Contiene",
        notContains: "No contiene",
        startsWith: "Empieza con",
        endsWith: "Termina con",
        andCondition: "Y",
        orCondition: "O",
        applyFilter: "Aplicar",
        resetFilter: "Reiniciar",
        clearFilter: "Limpiar",
        group: "Grupo",
        columns: "Columnas",
        rowGroupColumnsEmptyMessage: "Arrastra columnas aquí para agrupar",
        valueColumnsEmptyMessage: "Arrastra columnas aquí para sumar",
        pivotMode: "Modo Pivote",
        groups: "Grupos",
        values: "Valores",
        pivots: "Pivotes",
        valueAggregation: "Agregación de valores",
        toolPanelButton: "Panel de Herramientas",
        export: "Exportar",
        csvExport: "Exportar CSV",
        excelExport: "Exportar Excel",
        pinColumn: "Fijar Columna",
        autoSizeColumn: "Ajustar Ancho",
        autosizeAllColumns: "Ajustar Todo",
        resetColumns: "Restablecer Columnas",
        expandAll: "Expandir Todo",
        collapseAll: "Colapsar Todo",
        pivotChartAndPivotMode: "Gráfico y Modo Pivote",
        noRowsToShow: "Sin filas para mostrar",
        copy: "Copiar",
        copyWithHeaders: "Copiar con encabezados",
        ctrlC: "Ctrl+C",
        paste: "Pegar",
        ctrlV: "Ctrl+V"
    },
    onGridReady: () => {
        cargarUsuarios();
    }
};

function cargarUsuarios() {
    fetch('/Usuarios/ListarUsuarios')
        .then(resp => resp.json())
        .then(data => {
            console.log("JSON recibido de la API:", data);
            if (Array.isArray(data)) {
                gridApi.setGridOption('rowData', data);
            } else {
                console.error("Datos inválidos:", data);
            }
        })
        .catch(err => console.error("Error al cargar usuarios:", err));
}

// Inicializar Grid
const gridDiv = document.getElementById('myGridUsuarios');
gridApi = agGrid.createGrid(gridDiv, gridOptions); 


// Eventos del modal y formulario
btnNuevoUsuario.addEventListener("click", function () {
    title.textContent = "NUEVO USUARIO";
    frm.id.value = '';
    frm.reset();

    const modal = new Modal(modalRegistroUsuario);
    modal.show();
});

frm.addEventListener("submit", function (e) {
    e.preventDefault();

    const esNuevo = frm.id.value == '';
    if (
        frm.Nombre.value == "" ||
        frm.NomCompleto.value == "" ||
        (esNuevo && frm.contraseña.value == "") ||
        frm.idTipo.value == "" ||
        frm.estado.value == ""
    ) {
        alertaPersonalizada("warning", "TODOS LOS CAMPOS SON REQUERIDOS");
    } else {
        const data = new FormData(frm);
        const http = new XMLHttpRequest();
        const url = "/Usuarios/Guardar";

        http.open("POST", url, true);
        http.send(data);

        http.onreadystatechange = function () {
            if (this.readyState == 4 && this.status == 200) {
                const res = JSON.parse(this.responseText);
                alertaPersonalizada(res.tipo, res.mensaje);
                if (res.tipo == 'success') {
                    frm.reset();
                    const modal = new window.Modal(modalRegistroUsuario);
                    modal.hide();
                    cargarUsuarios();
                }
            }
        };
    }
});



// Función para cerrar el modal
function cerrarModal() {
    const targetEl = document.getElementById('modalRegistroUsuario');
    const modal = new Modal(targetEl);
    modal.hide();
}

function editarUsuario(id) {
    const http = new XMLHttpRequest();
    const url = '/Usuarios/GetUsuario/' + id;

    http.open("GET", url, true);
    http.send();

    http.onreadystatechange = function () {
        if (this.readyState == 4 && this.status == 200) {
            const res = JSON.parse(this.responseText);

            title.textContent = 'EDITAR USUARIO';
            frm.id.value = res.id;
            frm.Nombre.value = res.nombre;
            frm.NomCompleto.value = res.nomCompleto;
            frm.estado.value = res.estado;
            frm.idTipo.value = res.idTipo;
            frm.idUsuario.value = res.idusuario;
            frm.Email.value = res.email || '';
            frm.EmailResponsable.value = res.emailResponsable || '';
            frm.Area.value = res.area || '';
            const targetEl = document.getElementById('modalRegistroUsuario');
            const modal = new Modal(targetEl);
            modal.show();
        }
    };
}

function eliminarUsuario(id) {
    const url = '/Usuarios/EliminarUsuario/' + id;
    eliminarRegistro(
        '¿ESTÁ SEGURO DE ELIMINAR?',
        'EL USUARIO SE ELIMINARÁ DE FORMA PERMANENTE',
        'SÍ, ELIMINAR',
        url
    );
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

function eliminarRegistro(title, text, accion, url) {
    Swal.fire({
        title: title,
        text: text,
        icon: "warning",
        showCancelButton: true,
        confirmButtonColor: "#ef4444",
        cancelButtonColor: "#6b7280",
        confirmButtonText: accion,
        cancelButtonText: "Cancelar",
        background: '#1f2937',
        color: '#ffffff'
    }).then((result) => {
        if (result.isConfirmed) {
            fetch(url)
                .then(resp => resp.json())
                .then(res => {
                    alertaPersonalizada(res.tipo, res.mensaje);
                    if (res.tipo === 'success') {
                        cargarUsuarios();
                    }
                })
                .catch(err => console.error("Error al eliminar:", err));
        }
    });
}


document.addEventListener("DOMContentLoaded", () => {
    const toggleBtn = document.getElementById("togglePassword");
    if (toggleBtn) {
        toggleBtn.addEventListener("click", function () {
            const input = document.getElementById("contraseña");
            const icon = this.querySelector("i");

            if (input.type === "password") {
                input.type = "text";
                icon.classList.replace("fa-eye", "fa-eye-slash");
            } else {
                input.type = "password";
                icon.classList.replace("fa-eye-slash", "fa-eye");
            }
        });
    }
});