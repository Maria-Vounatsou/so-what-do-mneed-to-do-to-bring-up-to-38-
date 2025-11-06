export function toggleTheme() {
    var element = document.documentElement.getAttribute('data-bs-theme');
    var icon1 = document.getElementById('themeBtn');

    if (!element || element == "light") {
        document.documentElement.setAttribute('data-bs-theme', 'dark')
        icon1.setAttribute('class', 'fa-solid fa-sun')
    } else {
        document.documentElement.setAttribute('data-bs-theme', 'light')
        icon1.setAttribute('class', 'fa-solid fa-moon')
    }
}

export function onScrollEvent() {
    document.documentElement.scrollTop = 0;
}