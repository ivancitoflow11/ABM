// Ejemplo Básico
$("#example-basic").steps({
    headerTag: "h3",
    bodyTag: "section",
    transitionEffect: "slideLeft",
    autoFocus: true,
});

// Ejemplo Básico con formulario
var form = $("#example-form");
form.validate({
    errorPlacement: function errorPlacement(error, element) {
        element.before(error);
    },
    rules: {
        confirm: {
            equalTo: "#password",
        },
    },
});
form.children("div").steps({
    headerTag: "h3",
    bodyTag: "section",
    transitionEffect: "slideLeft",
    onStepChanging: function (event, currentIndex, newIndex) {
        form.validate().settings.ignore = ":disabled,:hidden";
        return form.valid();
    },
    onFinishing: function (event, currentIndex) {
        form.validate().settings.ignore = ":disabled";
        return form.valid();
    },
    onFinished: function (event, currentIndex) {
        alert("¡Enviado!");
    },
});

// Ejemplo Avanzado

var advanced_form = $("#example-advanced-form").show();

advanced_form
    .steps({
        headerTag: "h3",
        bodyTag: "fieldset",
        transitionEffect: "slideLeft",
        onStepChanging: function (event, currentIndex, newIndex) {
            // Siempre permitir la acción anterior incluso si el formulario actual no es válido
            if (currentIndex > newIndex) {
                return true;
            }
            // Prohibir avanzar en el paso "Advertencia" si el usuario es demasiado joven
            if (newIndex === 3 && Number($("#age-2").val()) < 18) {
                return false;
            }
            // Necesario en algunos casos si el usuario volvió atrás (limpiar errores)
            if (currentIndex < newIndex) {
                // Eliminar estilos de error
                advanced_form.find(".body:eq(" + newIndex + ") label.error").remove();
                advanced_form.find(".body:eq(" + newIndex + ") .error").removeClass("error");
            }
            advanced_form.validate().settings.ignore = ":disabled,:hidden";
            return advanced_form.valid();
        },
        onStepChanged: function (event, currentIndex, priorIndex) {
            // Usado para saltar el paso "Advertencia" si el usuario es lo suficientemente mayor
            if (currentIndex === 2 && Number($("#age-2").val()) >= 18) {
                advanced_form.steps("next");
            }
            // Usado para saltar el paso "Advertencia" si el usuario es lo suficientemente mayor y quiere volver al paso anterior
            if (currentIndex === 2 && priorIndex === 3) {
                advanced_form.steps("previous");
            }
        },
        onFinishing: function (event, currentIndex) {
            advanced_form.validate().settings.ignore = ":disabled";
            return advanced_form.valid();
        },
        onFinished: function (event, currentIndex) {
            alert("¡Enviado!");
        },
    })
    .validate({
        errorPlacement: function errorPlacement(error, element) {
            element.before(error);
        },
        rules: {
            confirm: {
                equalTo: "#password-2",
            },
        },
    });

// Manipulación Dinámica
$("#example-manipulation").steps({
    headerTag: "h3",
    bodyTag: "section",
    enableAllSteps: true,
    enablePagination: false,
});

// Pasos Verticales

$("#example-vertical").steps({
    headerTag: "h3",
    bodyTag: "section",
    transitionEffect: "slideLeft",
    stepsOrientation: "vertical",
});

// Ejemplo de diseño personalizado
$(".tab-wizard").steps({
    headerTag: "h6",
    bodyTag: "section",
    transitionEffect: "fade",
    titleTemplate: '<span class="step">#index#</span> #title#',
    labels: {
        finish: "Guardar",
        next: "Siguiente",
        previous: "Anterior",
    },
    onFinished: function (event, currentIndex) {
        swal(
            "¡Formulario enviado!",
            "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed lorem erat eleifend ex semper, lobortis purus sed."
        );
    },
});

var form = $(".validation-wizard").show();

$(".validation-wizard").steps({
    headerTag: "h6",
    bodyTag: "section",
    transitionEffect: "fade",
    titleTemplate: '<span class="step">#index#</span> #title#',
    labels: {
        finish: "Enviar",
        next: "Siguiente",
        previous: "Anterior",
    },
    onStepChanging: function (event, currentIndex, newIndex) {
        return (
            currentIndex > newIndex ||
            (!(3 === newIndex && Number($("#age-2").val()) < 18) &&
                (currentIndex < newIndex &&
                    (form.find(".body:eq(" + newIndex + ") label.error").remove(),
                        form.find(".body:eq(" + newIndex + ") .error").removeClass("error")),
                    (form.validate().settings.ignore = ":disabled,:hidden"),
                    form.valid()))
        );
    },
    onFinishing: function (event, currentIndex) {
        return (form.validate().settings.ignore = ":disabled"), form.valid();
    },
    onFinished: function (event, currentIndex) {
        swal(
            "¡Formulario enviado!",
            "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed lorem erat eleifend ex semper, lobortis purus sed."
        );
    },
}),
    $(".validation-wizard").validate({
        ignore: "input[type=hidden]",
        errorClass: "text-danger",
        successClass: "text-success",
        highlight: function (element, errorClass) {
            $(element).removeClass(errorClass);
        },
        unhighlight: function (element, errorClass) {
            $(element).removeClass(errorClass);
        },
        errorPlacement: function (error, element) {
            error.insertAfter(element);
        },
        rules: {
            email: {
                email: true,
            },
        },
    });
