// 
//    File export                              //
// 
$("#file_export").DataTable({
    dom: "Bfrtip",
    buttons: [
        {
            extend: 'copy',
            text: '<i class="fa-regular fa-clipboard"></i>',
            titleAttr: 'Copiar',
            className: 'btn btn-copy'
        },
        {
            extend: 'excel',
            text: '<i class="fa-solid fa-file-excel"></i>',
            titleAttr: 'Exportar a Excel',
            className: 'btn btn-excel'
        },
        {
            extend: 'pdf',
            text: '<i class="fa-solid fa-file-pdf"></i>',
            titleAttr: 'Exportar a PDF',
            className: 'btn btn-pdf',
            orientation: 'landscape', // Cambia la orientación a horizontal
            pageSize: 'LEGAL', // Aumenta el tamaño de la página
            customize: function (doc) {
                // Ajusta el tamaño de la fuente
                doc.defaultStyle.fontSize = 8;

                // Ajusta el ancho de la tabla al ancho de la página
                doc.content[1].table.widths =
                    Array(doc.content[1].table.body[0].length + 1).join('*').split('');

                // Ajusta los márgenes
                doc.pageMargins = [10, 10, 10, 10];
            },
            exportOptions: {
                columns: ':visible' // Solo exporta las columnas visibles
            }
        }
    ]
});



$("#file_export1").DataTable({
    dom: "Bfrtip",
    buttons: [
        {
            extend: 'copy',
            text: '<i class="fa-regular fa-clipboard"></i>', // Icono para copiar
            titleAttr: 'Copiar',
            className: 'btn btn-copy' // Azul para copiar
        },
        {
            extend: 'excel',
            text: '<i class="fa-solid fa-file-excel"></i>', // Icono para Excel
            titleAttr: 'Exportar a Excel',
            className: 'btn btn-excel' // Verde para Excel
        },
        {
            extend: 'pdf',
            text: '<i class="fa-solid fa-file-pdf"></i>',
            titleAttr: 'Exportar a PDF',
            className: 'btn btn-pdf',
            orientation: 'landscape', // Cambia la orientación a horizontal
            pageSize: 'LEGAL', // Aumenta el tamaño de la página
            customize: function (doc) {
                // Ajusta el tamaño de la fuente
                doc.defaultStyle.fontSize = 8;

                // Ajusta el ancho de la tabla al ancho de la página
                doc.content[1].table.widths =
                    Array(doc.content[1].table.body[0].length + 1).join('*').split('');

                // Ajusta los márgenes
                doc.pageMargins = [10, 10, 10, 10];
            },
            exportOptions: {
                columns: ':visible' // Solo exporta las columnas visibles
            }
        }
    ]
});


$("#file_export2").DataTable({
    dom: "Bfrtip",
    buttons: [
        {
            extend: 'copy',
            text: '<i class="fa-regular fa-clipboard"></i>', // Icono para copiar
            titleAttr: 'Copiar',
            className: 'btn btn-copy' // Azul para copiar
        },
        {
            extend: 'excel',
            text: '<i class="fa-solid fa-file-excel"></i>', // Icono para Excel
            titleAttr: 'Exportar a Excel',
            className: 'btn btn-excel' // Verde para Excel
        },
        {
            extend: 'pdf',
            text: '<i class="fa-solid fa-file-pdf"></i>',
            titleAttr: 'Exportar a PDF',
            className: 'btn btn-pdf',
            orientation: 'landscape', // Cambia la orientación a horizontal
            pageSize: 'LEGAL', // Aumenta el tamaño de la página
            customize: function (doc) {
                // Ajusta el tamaño de la fuente
                doc.defaultStyle.fontSize = 8;

                // Ajusta el ancho de la tabla al ancho de la página
                doc.content[1].table.widths =
                    Array(doc.content[1].table.body[0].length + 1).join('*').split('');

                // Ajusta los márgenes
                doc.pageMargins = [10, 10, 10, 10];
            },
            exportOptions: {
                columns: ':visible' // Solo exporta las columnas visibles
            }
        }
    ]
});


$("#file_export3").DataTable({
    dom: "Bfrtip",
    buttons: [
        {
            extend: 'copy',
            text: '<i class="fa-regular fa-clipboard"></i>', // Icono para copiar
            titleAttr: 'Copiar',
            className: 'btn btn-copy' // Azul para copiar
        },
        {
            extend: 'excel',
            text: '<i class="fa-solid fa-file-excel"></i>', // Icono para Excel
            titleAttr: 'Exportar a Excel',
            className: 'btn btn-excel' // Verde para Excel
        },
        {
            extend: 'pdf',
            text: '<i class="fa-solid fa-file-pdf"></i>',
            titleAttr: 'Exportar a PDF',
            className: 'btn btn-pdf',
            orientation: 'landscape', // Cambia la orientación a horizontal
            pageSize: 'LEGAL', // Aumenta el tamaño de la página
            customize: function (doc) {
                // Ajusta el tamaño de la fuente
                doc.defaultStyle.fontSize = 8;

                // Ajusta el ancho de la tabla al ancho de la página
                doc.content[1].table.widths =
                    Array(doc.content[1].table.body[0].length + 1).join('*').split('');

                // Ajusta los márgenes
                doc.pageMargins = [10, 10, 10, 10];
            },
            exportOptions: {
                columns: ':visible' // Solo exporta las columnas visibles
            }
        }
    ]
});


$("#file_export4").DataTable({
    dom: "Bfrtip",
    lengthMenu: [[10, 20, 50, -1], [10, 20, 50, "Todos"]],
    pageLength: 20,
    buttons: [
        {
            extend: 'copy',
            text: '<i class="fa-regular fa-clipboard"></i>',
            titleAttr: 'Copiar',
            className: 'btn btn-copy'
        },
        {
            extend: 'excel',
            text: '<i class="fa-solid fa-file-excel"></i>',
            titleAttr: 'Exportar a Excel',
            className: 'btn btn-excel'
        },
        {
            extend: 'pdf',
            text: '<i class="fa-solid fa-file-pdf"></i>',
            titleAttr: 'Exportar a PDF',
            className: 'btn btn-pdf',
            orientation: 'landscape',
            pageSize: 'LEGAL',
            customize: function (doc) {
                doc.defaultStyle.fontSize = 8;
                doc.content[1].table.widths =
                    Array(doc.content[1].table.body[0].length + 1)
                        .join('*')
                        .split('');
                doc.pageMargins = [10, 10, 10, 10];
            },
            exportOptions: {
                columns: ':visible'
            }
        }
    ],
    language: {
        search: "Buscar:",
        lengthMenu: "Mostrar _MENU_ registros",
        info: "Mostrando _START_ a _END_ de _TOTAL_ entradas",
        infoEmpty: "Mostrando 0 a 0 de 0 entradas",
        infoFiltered: "(filtrado de _MAX_ entradas totales)",
        zeroRecords: "No se encontraron registros",
        paginate: {
            first: "Primero",
            last: "Último",
            next: "Siguiente",
            previous: "Anterior"
        }
    }
});




$("#file_export5").DataTable({
    dom: "Bfrtip",
    buttons: [
        {
            extend: 'copy',
            text: '<i class="fa-regular fa-clipboard"></i>', // Icono para copiar
            titleAttr: 'Copiar',
            className: 'btn btn-copy' // Azul para copiar
        },
        {
            extend: 'excel',
            text: '<i class="fa-solid fa-file-excel"></i>', // Icono para Excel
            titleAttr: 'Exportar a Excel',
            className: 'btn btn-excel' // Verde para Excel
        },
        {
            extend: 'pdf',
            text: '<i class="fa-solid fa-file-pdf"></i>',
            titleAttr: 'Exportar a PDF',
            className: 'btn btn-pdf',
            orientation: 'landscape', // Cambia la orientación a horizontal
            pageSize: 'LEGAL', // Aumenta el tamaño de la página
            customize: function (doc) {
                // Ajusta el tamaño de la fuente
                doc.defaultStyle.fontSize = 8;

                // Ajusta el ancho de la tabla al ancho de la página
                doc.content[1].table.widths =
                    Array(doc.content[1].table.body[0].length + 1).join('*').split('');

                // Ajusta los márgenes
                doc.pageMargins = [10, 10, 10, 10];
            },
            exportOptions: {
                columns: ':visible' // Solo exporta las columnas visibles
            }
        }
    ]
});


$("#file_export6").DataTable({
    dom: "Bfrtip",
    buttons: [
        {
            extend: 'copy',
            text: '<i class="fa-regular fa-clipboard"></i>', // Icono para copiar
            titleAttr: 'Copiar',
            className: 'btn btn-copy' // Azul para copiar
        },
        {
            extend: 'excel',
            text: '<i class="fa-solid fa-file-excel"></i>', // Icono para Excel
            titleAttr: 'Exportar a Excel',
            className: 'btn btn-excel' // Verde para Excel
        },
        {
            extend: 'pdf',
            text: '<i class="fa-solid fa-file-pdf"></i>',
            titleAttr: 'Exportar a PDF',
            className: 'btn btn-pdf',
            orientation: 'landscape', // Cambia la orientación a horizontal
            pageSize: 'LEGAL', // Aumenta el tamaño de la página
            customize: function (doc) {
                // Ajusta el tamaño de la fuente
                doc.defaultStyle.fontSize = 8;

                // Ajusta el ancho de la tabla al ancho de la página
                doc.content[1].table.widths =
                    Array(doc.content[1].table.body[0].length + 1).join('*').split('');

                // Ajusta los márgenes
                doc.pageMargins = [10, 10, 10, 10];
            },
            exportOptions: {
                columns: ':visible' // Solo exporta las columnas visibles
            }
        }
    ]
});



$("#file_export7").DataTable({
    dom: "Bfrtip",
    buttons: [
        {
            extend: 'copy',
            text: '<i class="fa-regular fa-clipboard"></i>', // Icono para copiar
            titleAttr: 'Copiar',
            className: 'btn btn-copy' // Azul para copiar
        },
        {
            extend: 'excel',
            text: '<i class="fa-solid fa-file-excel"></i>', // Icono para Excel
            titleAttr: 'Exportar a Excel',
            className: 'btn btn-excel' // Verde para Excel
        },
        {
            extend: 'pdf',
            text: '<i class="fa-solid fa-file-pdf"></i>',
            titleAttr: 'Exportar a PDF',
            className: 'btn btn-pdf',
            orientation: 'landscape', // Cambia la orientación a horizontal
            pageSize: 'LEGAL', // Aumenta el tamaño de la página
            customize: function (doc) {
                // Ajusta el tamaño de la fuente
                doc.defaultStyle.fontSize = 8;

                // Ajusta el ancho de la tabla al ancho de la página
                doc.content[1].table.widths =
                    Array(doc.content[1].table.body[0].length + 1).join('*').split('');

                // Ajusta los márgenes
                doc.pageMargins = [10, 10, 10, 10];
            },
            exportOptions: {
                columns: ':visible' // Solo exporta las columnas visibles
            }
        }
    ]
});

// 
//  Show / hide columns dynamically                 //
// 

var table = $("#show_hide_col").DataTable({
  scrollY: "200px",
  paging: false,
});

$("a.toggle-vis").on("click", function (e) {
  e.preventDefault();

  // Get the column API object
  var column = $("#show_hide_col")
    .dataTable()
    .api()
    .column($(this).attr("data-column"));
  // Toggle the visibility
  column.visible(!column.visible());
});

// 
//    Column rendering                         //
// 
$("#col_render").DataTable({
  columnDefs: [
    {
      // The `data` parameter refers to the data for the cell (defined by the
      // `data` option, which defaults to the column being worked with, in
      // this case `data: 0`.
      render: function (data, type, row) {
        return data + " (" + row[3] + ")";
      },
      targets: 0,
    },
    { visible: false, targets: [3] },
  ],
});

// 
//     Row grouping                            //
// 
var table = $("#row_group").DataTable({
  pageLength: 10,
  columnDefs: [{ visible: false, targets: 2 }],
  order: [[2, "asc"]],
  displayLength: 25,
  drawCallback: function (settings) {
    var api = this.api();
    var rows = api.rows({ page: "current" }).nodes();
    var last = null;

    api
      .column(2, { page: "current" })
      .data()
      .each(function (group, i) {
        if (last !== group) {
          $(rows)
            .eq(i)
            .before(
              '<tr class="group"><td colspan="5">' + group + "</td></tr>"
            );

          last = group;
        }
      });
  },
});

// 
// Order by the grouping
// 
$("#row_group tbody").on("click", "tr.group", function () {
  var currentOrder = table.order()[0];
  if (currentOrder[0] === 2 && currentOrder[1] === "asc") {
    table.order([2, "desc"]).draw();
  } else {
    table.order([2, "asc"]).draw();
  }
});

// 
//    Multiple table control element           //
// 
$("#multi_control").DataTable({
  dom: '<"top"iflp<"clear">>rt<"bottom"iflp<"clear">>',
});

// 
//    DOM/jquery events                        //
// 
var table = $("#dom_jq_event").DataTable();

$("#dom_jq_event tbody").on("click", "tr", function () {
  var data = table.row(this).data();
  alert("You clicked on " + data[0] + "'s row");
});

// 
//    Language File                            //
// 
$("#lang_file").DataTable({
  language: {
    url: "../../assets/js/datatable/German.json",
  },
});

// 
//    Complex headers with column visibility   //
// 

$("#complex_head_col").DataTable({
  columnDefs: [
    {
      visible: false,
      targets: -1,
    },
  ],
});

// 
//    Setting defaults                         //
// 
var defaults = {
  searching: false,
  ordering: false,
};

$("#setting_defaults").dataTable($.extend(true, {}, defaults, {}));

// 
//    Footer callback                          //
// 
$("#footer_callback").DataTable({
  footerCallback: function (row, data, start, end, display) {
    var api = this.api(),
      data;

    // Remove the formatting to get integer data for summation
    var intVal = function (i) {
      return typeof i === "string"
        ? i.replace(/[\$,]/g, "") * 1
        : typeof i === "number"
        ? i
        : 0;
    };

    // Total over all pages
    total = api
      .column(4)
      .data()
      .reduce(function (a, b) {
        return intVal(a) + intVal(b);
      }, 0);

    // Total over this page
    pageTotal = api
      .column(4, { page: "current" })
      .data()
      .reduce(function (a, b) {
        return intVal(a) + intVal(b);
      }, 0);

    // Update footer
    $(api.column(4).footer()).html(
      "$" + pageTotal + " ( $" + total + " total)"
    );
  },
});

// 
//    Custom toolbar elements                  //
// 

$("#custom_tool_ele").DataTable({
  dom: '<"toolbar">frtip',
});

$("div.toolbar").html("<b>Custom tool bar! Text/images etc.</b>");

// 
//    Row created callback                     //
// 
$("#row_create_call").DataTable({
  createdRow: function (row, data, index) {
    if (data[5].replace(/[\$,]/g, "") * 1 > 150000) {
      $("td", row).eq(5).addClass("highlight");
    }
  },
});
