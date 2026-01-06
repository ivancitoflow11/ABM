//
//    File export - VERSIÓN FINAL (Colores Correctos + Fecha 00 + PDF Limpio)
//

// --- 1. GENERAMOS LA FECHA ---
var fechaHoy = new Date();

// Función para poner ceros a la izquierda (6 -> 06)
var pad = function (num) { return num < 10 ? '0' + num : num; };

var dia = pad(fechaHoy.getDate());
var mes = pad(fechaHoy.getMonth() + 1);
var anio = fechaHoy.getFullYear();
var horas = pad(fechaHoy.getHours());
var minutos = pad(fechaHoy.getMinutes());

var fechaFormateada = dia + "-" + mes + "-" + anio + " " + horas + ":" + minutos;
var textoFecha = 'Fecha de descarga: ' + fechaFormateada;
var tituloLimpio = 'Reporte Incidencias';
var nombreArchivo = 'Reporte_' + fechaFormateada.replace(/[: ]/g, '_');

// --- 2. CONFIGURACIÓN CENTRALIZADA DE BOTONES ---
// Aquí aplicamos los colores solicitados usando clases Bootstrap
var misBotonesExport = [
    {
        extend: 'copy',
        text: '<i class="fa-regular fa-clipboard"></i>',
        titleAttr: 'Copiar',
        className: 'btn btn-primary', // <--- AZUL
        exportOptions: { columns: ':visible' }
    },
    {
        extend: 'excel',
        text: '<i class="fa-solid fa-file-excel"></i>',
        titleAttr: 'Exportar a Excel',
        className: 'btn btn-success', // <--- VERDE
        title: tituloLimpio,
        messageTop: textoFecha,
        message: textoFecha,
        filename: nombreArchivo,
        exportOptions: { columns: ':visible' }
    },
    {
        extend: 'pdf',
        text: '<i class="fa-solid fa-file-pdf"></i>',
        titleAttr: 'Exportar a PDF',
        className: 'btn btn-danger',  // <--- ROJO
        orientation: 'landscape',
        pageSize: 'LEGAL',
        title: tituloLimpio,
        message: textoFecha,
        filename: nombreArchivo,
        exportOptions: { columns: ':visible' },
        customize: function (doc) {
            doc.defaultStyle.fontSize = 8;
            if (doc.content[1] && doc.content[1].table) {
                doc.content[1].table.widths = Array(doc.content[1].table.body[0].length + 1).join('*').split('');
            }
            doc.pageMargins = [10, 10, 10, 10];
        }
    }
];

// ----------------------------------------------------------------------
//    INICIALIZACIÓN DE TABLAS
// ----------------------------------------------------------------------

$("#file_export").DataTable({
    dom: "Bfrtip",
    buttons: misBotonesExport
});

$("#file_export1").DataTable({
    dom: "Bfrtip",
    buttons: misBotonesExport
});

$("#file_export2").DataTable({
    dom: "Bfrtip",
    buttons: misBotonesExport
});

$("#file_export3").DataTable({
    dom: "Bfrtip",
    buttons: misBotonesExport
});

// Tabla con paginación
$("#file_export4").DataTable({
    dom: "Blfrtip",
    lengthMenu: [[10, 20, 50, -1], [10, 20, 50, "Todos"]],
    pageLength: 10,
    buttons: misBotonesExport,
    language: {
        search: "Buscar:",
        lengthMenu: "Mostrar _MENU_ registros",
        info: "Mostrando _START_ a _END_ de _TOTAL_ entradas",
        infoEmpty: "Mostrando 0 a 0 de 0 entradas",
        infoFiltered: "(filtrado de _MAX_ entradas totales)",
        zeroRecords: "No se encontraron registros",
        paginate: { first: "Primero", last: "Último", next: "Siguiente", previous: "Anterior" }
    }
});

$("#gestion1").DataTable({
    dom: "Blfrtip",
    lengthMenu: [[10, 20, 50, -1], [10, 20, 50, "Todos"]],
    pageLength: 10,
    buttons: misBotonesExport,
    language: {
        search: "Buscar:",
        lengthMenu: "Mostrar _MENU_ registros",
        info: "Mostrando _START_ a _END_ de _TOTAL_ entradas",
        infoEmpty: "Mostrando 0 a 0 de 0 entradas",
        infoFiltered: "(filtrado de _MAX_ entradas totales)",
        zeroRecords: "No se encontraron registros",
        paginate: { first: "Primero", last: "Último", next: "Siguiente", previous: "Anterior" }
    }
});

$("#gestion2").DataTable({
    dom: "Blfrtip",
    lengthMenu: [[10, 20, 50, -1], [10, 20, 50, "Todos"]],
    pageLength: 10,
    buttons: misBotonesExport,
    language: {
        search: "Buscar:",
        lengthMenu: "Mostrar _MENU_ registros",
        info: "Mostrando _START_ a _END_ de _TOTAL_ entradas",
        infoEmpty: "Mostrando 0 a 0 de 0 entradas",
        infoFiltered: "(filtrado de _MAX_ entradas totales)",
        zeroRecords: "No se encontraron registros",
        paginate: { first: "Primero", last: "Último", next: "Siguiente", previous: "Anterior" }
    }
});

$("#gestion3").DataTable({
    dom: "Blfrtip",
    lengthMenu: [[10, 20, 50, -1], [10, 20, 50, "Todos"]],
    pageLength: 10,
    buttons: misBotonesExport,
    language: {
        search: "Buscar:",
        lengthMenu: "Mostrar _MENU_ registros",
        info: "Mostrando _START_ a _END_ de _TOTAL_ entradas",
        infoEmpty: "Mostrando 0 a 0 de 0 entradas",
        infoFiltered: "(filtrado de _MAX_ entradas totales)",
        zeroRecords: "No se encontraron registros",
        paginate: { first: "Primero", last: "Último", next: "Siguiente", previous: "Anterior" }
    }
});

$("#gestion4").DataTable({
    dom: "Blfrtip",
    lengthMenu: [[10, 20, 50, -1], [10, 20, 50, "Todos"]],
    pageLength: 10,
    buttons: misBotonesExport,
    language: {
        search: "Buscar:",
        lengthMenu: "Mostrar _MENU_ registros",
        info: "Mostrando _START_ a _END_ de _TOTAL_ entradas",
        infoEmpty: "Mostrando 0 a 0 de 0 entradas",
        infoFiltered: "(filtrado de _MAX_ entradas totales)",
        zeroRecords: "No se encontraron registros",
        paginate: { first: "Primero", last: "Último", next: "Siguiente", previous: "Anterior" }
    }
});

$("#file_export5").DataTable({
    dom: "Bfrtip",
    buttons: misBotonesExport
});

$("#file_export6").DataTable({
    dom: "Bfrtip",
    buttons: misBotonesExport
});

$("#file_export7").DataTable({
    dom: "Bfrtip",
    buttons: misBotonesExport
});

// ----------------------------------------------------------------------
//    RESTO DE FUNCIONALIDADES (Sin cambios)
// ----------------------------------------------------------------------

var table = $("#show_hide_col").DataTable({
    scrollY: "200px",
    paging: false,
});

$("a.toggle-vis").on("click", function (e) {
    e.preventDefault();
    var column = $("#show_hide_col").dataTable().api().column($(this).attr("data-column"));
    column.visible(!column.visible());
});

$("#col_render").DataTable({
    columnDefs: [
        {
            render: function (data, type, row) {
                return data + " (" + row[3] + ")";
            },
            targets: 0,
        },
        { visible: false, targets: [3] },
    ],
});

var table = $("#row_group").DataTable({
    pageLength: 10,
    columnDefs: [{ visible: false, targets: 2 }],
    order: [[2, "asc"]],
    displayLength: 25,
    drawCallback: function (settings) {
        var api = this.api();
        var rows = api.rows({ page: "current" }).nodes();
        var last = null;

        api.column(2, { page: "current" }).data().each(function (group, i) {
            if (last !== group) {
                $(rows).eq(i).before('<tr class="group"><td colspan="5">' + group + "</td></tr>");
                last = group;
            }
        });
    },
});

$("#row_group tbody").on("click", "tr.group", function () {
    var currentOrder = table.order()[0];
    if (currentOrder[0] === 2 && currentOrder[1] === "asc") {
        table.order([2, "desc"]).draw();
    } else {
        table.order([2, "asc"]).draw();
    }
});

$("#multi_control").DataTable({
    dom: '<"top"iflp<"clear">>rt<"bottom"iflp<"clear">>',
});

var table = $("#dom_jq_event").DataTable();

$("#dom_jq_event tbody").on("click", "tr", function () {
    var data = table.row(this).data();
    alert("You clicked on " + data[0] + "'s row");
});

$("#lang_file").DataTable({
    language: {
        url: "../../assets/js/datatable/German.json",
    },
});

$("#complex_head_col").DataTable({
    columnDefs: [{ visible: false, targets: -1 }],
});

var defaults = { searching: false, ordering: false };
$("#setting_defaults").dataTable($.extend(true, {}, defaults, {}));

$("#footer_callback").DataTable({
    footerCallback: function (row, data, start, end, display) {
        var api = this.api(), data;
        var intVal = function (i) {
            return typeof i === "string" ? i.replace(/[\$,]/g, "") * 1 : typeof i === "number" ? i : 0;
        };
        total = api.column(4).data().reduce(function (a, b) { return intVal(a) + intVal(b); }, 0);
        pageTotal = api.column(4, { page: "current" }).data().reduce(function (a, b) { return intVal(a) + intVal(b); }, 0);
        $(api.column(4).footer()).html("$" + pageTotal + " ( $" + total + " total)");
    },
});

$("#custom_tool_ele").DataTable({ dom: '<"toolbar">frtip' });
$("div.toolbar").html("<b>Custom tool bar! Text/images etc.</b>");

$("#row_create_call").DataTable({
    createdRow: function (row, data, index) {
        if (data[5].replace(/[\$,]/g, "") * 1 > 150000) {
            $("td", row).eq(5).addClass("highlight");
        }
    },
});