function setSidebarMenuItem(activeItemClass) {
    $('#sidebar .home').removeClass('active');
    $('#sidebar .' + activeItemClass).addClass('active');
}

function setTopMenuItem(activeItemClass) {
    $('header .nav .' + activeItemClass).addClass('active');
}
