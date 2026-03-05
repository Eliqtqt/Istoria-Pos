// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// Footer scroll behavior
$(window).scroll(function() {
    var scrollTop = $(this).scrollTop();
    var windowHeight = $(this).height();
    var documentHeight = $(document).height();

    if (scrollTop > 100) {
        $('.footer').addClass('show');
    } else {
        $('.footer').removeClass('show');
    }

    if (scrollTop + windowHeight > documentHeight - 200) {
        $('.footer').addClass('fade');
    } else {
        $('.footer').removeClass('fade');
    }
});
