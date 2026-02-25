function alertaPersonalizada(type, mensaje) {
    Swal.fire({
        position: "center",
        icon: type,
        title: mensaje,
        showConfirmButton: false,
        timer: 1500,
    });
}



function rechazarRegistro(title, text, accion, url) {
    Swal.fire({
        title: title,
        text: text,
        icon: "warning",
        showCancelButton: true,
        confirmButtonColor: "#3085d6",
        cancelButtonColor: "#d33",
        confirmButtonText: accion,
    }).then((result) => {
        if (result.isConfirmed) {
            const http = new XMLHttpRequest();

            http.open("GET", url, true);

            http.send();

            http.onreadystatechange = function () {
                if (this.readyState == 4 && this.status == 200) {
                    const res = JSON.parse(this.responseText);
                    alertaPersonalizada(res.tipo, res.mensaje);
                    if (res.tipo == "success") {
                        setTimeout(() => {
                            window.location.reload();
                        }, 1500);
                    }
                }
            };
        }
    });
}

function eliminarConInput(title, text, accion, id, urlBase, table) {
    Swal.fire({
        title: title,
        text: text,
        input: "text",
        inputAttributes: {
            autocapitalize: "off"
        },
        icon: "warning",
        showCancelButton: true,
        confirmButtonColor: "#3085d6",
        cancelButtonColor: "#d33",
        confirmButtonText: accion,
        preConfirm: (inputValue) => {
            if (!inputValue) {
                Swal.showValidationMessage("El campo no puede estar vacío");
                return;
            }
            return inputValue;
        },
    }).then((result) => {
        if (result.isConfirmed) {
            const fullUrl = `${urlBase}`;
            const inputValor = result.value;

            const http = new XMLHttpRequest();
            http.open("POST", fullUrl, true);
            http.setRequestHeader("Content-Type", "application/x-www-form-urlencoded");

            // Agrega console.log para verificar los datos que se envían
            console.log(`id: ${id}, inputValor: ${inputValor}`);

            http.send(`id=${id}&inputValor=${encodeURIComponent(inputValor)}`); // Asegúrate de codificar el valor del input

            http.onreadystatechange = function () {
                if (this.readyState == 4 && this.status == 200) {
                    const res = JSON.parse(this.responseText);
                    alertaPersonalizada(res.tipo, res.mensaje);
                    if (res.tipo == 'success') {
                        if (table != null) {
                            table.ajax.reload();
                        } else {
                            setTimeout(() => {
                                window.location.reload();
                            }, 1500);
                        }
                    }
                }
            };
        }
    });
}

