$(document).ready(function () {
    'use strict';
    
    var form = $('form');
    if (form.length > 0) {
        form.on('submit', function (e) {
            if (form[0].checkValidity() === false) {
                e.preventDefault();
                e.stopPropagation();
            }
            form.addClass('was-validated');
        });
    }
});
