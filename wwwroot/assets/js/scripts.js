const init = async () => {

  // Mobile Menu //
  let burgerButton = document.getElementById("burgerButton");
  let mobileNav = document.querySelector(".mobile__nav");
  let body = document.body;

  function closeMenu() {
    mobileNav.classList.remove("nav--active");
    body.classList.remove("lock");
  }

  if (burgerButton && mobileNav) {
    let closeBtn = mobileNav.querySelector("#closeMenu");
    let links = mobileNav.querySelectorAll(".navigation__list a");
    let nav = mobileNav.querySelector("nav");

    // открыть меню
    burgerButton.addEventListener("click", function (e) {
      e.stopPropagation();
      mobileNav.classList.add("nav--active");
      body.classList.add("lock");
    });

    // закрыть по крестику
    if (closeBtn) closeBtn.addEventListener("click", closeMenu);

    // закрыть по клику на ссылку
    // закрыть по клику на ссылку
    links.forEach(link => {
      link.addEventListener("click", function (e) {
        // проверяем родителя
        let parentLi = link.closest("li");

        if (parentLi && parentLi.classList.contains("menu-item-has-children")) {
          // у ссылки есть подменю → не закрываем
          e.preventDefault(); // чтобы не переходило по ссылке сразу, если нужно открыть подменю
        } else {
          closeMenu();
        }
      });
    });


    // закрыть по клику вне nav
    mobileNav.addEventListener("click", function (e) {
      if (!nav.contains(e.target)) {
        closeMenu();
      }
    });
  }

};


// -has-children
document.addEventListener("DOMContentLoaded", function () {
  // Находим все элементы меню с подменю
  const menuItems = document.querySelectorAll(".menu-item-has-children > a");

  // Обрабатываем клик по каждому элементу меню
  menuItems.forEach(item => {
    item.addEventListener("click", function (e) {
      e.preventDefault(); // Предотвращаем переход по ссылке

      const parentMenuItem = item.parentElement; // Получаем родителя элемента <a>

      // Если у родителя уже есть класс active, то оставляем его, иначе убираем у соседей и добавляем текущему
      if (!parentMenuItem.classList.contains('active')) {
        // Убираем класс active у всех соседей
        parentMenuItem.parentElement.querySelectorAll('.menu-item-has-children').forEach(sibling => {
          if (sibling !== parentMenuItem) {
            sibling.classList.remove('active');
          }
        });

        // Добавляем класс active к текущему элементу
        parentMenuItem.classList.add('active');
      } else {
        // Если уже активен, то просто убираем класс
        parentMenuItem.classList.remove('active');
      }
    });
  });

  // Закрытие подменю при клике вне области навигации
  document.addEventListener("click", function (e) {
    // Если клик не по элементу с подменю
    if (!e.target.closest(".menu-item-has-children")) {
      // Убираем класс 'active' у всех родительских элементов меню
      document.querySelectorAll('.menu-item-has-children.active').forEach(activeItem => {
        activeItem.classList.remove("active");
      });
    }
  });
});
// -has-children


// Динамическая смена фона в секциях
function updateBackgrounds() {
  const isMobile = window.innerWidth <= 560;

  document.querySelectorAll("section").forEach(section => {
    const desktopBg = section.getAttribute("data-desktop-bg");
    const mobileBg = section.getAttribute("data-mobile-bg");

    if (isMobile && mobileBg) {
      section.style.backgroundImage = `url(${mobileBg})`;
    } else {
      // Если desktopBg есть — ставим его, если нет — убираем фон
      section.style.backgroundImage = desktopBg ? `url(${desktopBg})` : "none";
    }
  });
}

document.addEventListener("DOMContentLoaded", updateBackgrounds);
window.addEventListener("resize", updateBackgrounds);
// Динамическая смена фона в секциях

//swiper
document.addEventListener("DOMContentLoaded", function () {

  if (document.querySelector("#products")) {
    new Swiper("#products", {
      observer: true,
      observeParents: true,
      loop: true,
      autoplay: {
        delay: 3000,
        disableOnInteraction: false,
      },
      pagination: {
        el: ".products-pagination",
        clickable: true,
      },
      navigation: {
        nextEl: ".products-button-next",
        prevEl: ".products-button-prev",
      },
      breakpoints: {
        320: {
          slidesPerView: 1, // Один полный слайд и куски по бокам
          spaceBetween: 20, // Расстояние между слайдами
        },
        560: {
          slidesPerView: 2, // Один полный слайд и куски по бокам
          spaceBetween: 20, // Расстояние между слайдами
        },
        900: {
          slidesPerView: 3, // Один полный слайд и куски по бокам
          spaceBetween: 20, // Расстояние между слайдами

        },
      },
    });
  }

});
// swiper

//faq collapse
$(document).ready(function () {
  // Обработчик клика на элемент с классом faq__title
  $(".action").on("click", function () {
    // Находим ближайший родительский элемент с классом faq__item
    var $item = $(this).closest(".faq__item");
    // Переключаем класс active у найденного элемента
    $item.toggleClass("active");
  });

  // Обработчик клика на элемент с классом faq__btn
  $(".faq__btn").on("click", function () {
    // Находим ближайший родительский элемент с классом faq__item
    var $item = $(this).closest(".faq__item");
    // Переключаем класс active у найденного элемента
    $item.toggleClass("active");
  });
});
//faq collapse

// Language Open //
document.addEventListener("DOMContentLoaded", function () {
  const languageBtn = document.getElementById("language_btn");
  const languageSubMenu = languageBtn ? languageBtn.querySelector('.sub-menu') : null;

  if (languageBtn && languageSubMenu) {
    languageBtn.addEventListener("click", function (e) {
      e.stopPropagation();
      const isActive = languageBtn.classList.toggle("active");

      if (isActive) {
        openMenu(languageSubMenu);
      } else {
        closeMenu(languageSubMenu);
      }
    });

    // Закрываем при клике вне
    document.addEventListener("click", function (e) {
      if (!languageBtn.contains(e.target)) {
        languageBtn.classList.remove("active");
        closeMenu(languageSubMenu);
      }
    });
  } else {
    console.warn("⚠️ Language button or submenu not found");
  }

  // === Функции ===
  function openMenu(element) {
    if (!element) return;
    element.classList.add("show");

    // Проверяем положение меню и экрана
    const rect = element.getBoundingClientRect();
    const viewportHeight = window.innerHeight || document.documentElement.clientHeight;

    if (rect.bottom > viewportHeight - 10) {
      element.classList.add("open-up");  // открыть вверх
    } else {
      element.classList.remove("open-up"); // вниз (по умолчанию)
    }
  }

  function closeMenu(element) {
    if (!element) return;
    element.classList.remove("show", "open-up");
  }
});
// Language Open //

  (function () {
  var path = window.location.pathname; // например: /en/download или /ru/some/page

  var parts = path.split('/'); // ["", "en", "download"]
  if (parts.length < 2) return;

  // всё после языка ("/download", "/some/page", или "/" если дальше ничего нет)
  var rest = parts.length > 2 && parts[2] !== ""
  ? '/' + parts.slice(2).join('/')
  : '/';

  document.querySelectorAll('.sub-menu a[data-lang-code]').forEach(function (link) {
  var lang = link.getAttribute('data-lang-code'); // en / ua / ru
  link.href = '/' + lang + rest;
});
})();
// Инициализация после загрузки страницы
window.onload = init;