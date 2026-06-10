(function () {
  var sidebar = document.getElementById('sidebar');
  var overlay = document.getElementById('overlay');
  var hamburger = document.getElementById('hamburgerBtn');
  var mq = window.matchMedia('(max-width: 1023px)');

  function isMobile() {
    return mq.matches;
  }

  function closeMobileNav() {
    sidebar?.classList.remove('active');
    overlay?.classList.remove('active');
  }

  function openMobileNav() {
    sidebar?.classList.add('active');
    overlay?.classList.add('active');
  }

  function toggleMobileNav() {
    if (sidebar?.classList.contains('active')) {
      closeMobileNav();
    } else {
      openMobileNav();
    }
  }

  function toggleDesktopNav() {
    document.body.classList.toggle('nav-sidebar-collapsed');
  }

  hamburger?.addEventListener('click', function () {
    if (isMobile()) {
      toggleMobileNav();
    } else {
      toggleDesktopNav();
    }
  });

  overlay?.addEventListener('click', function () {
    closeMobileNav();
  });

  document.querySelectorAll('.sidebar .menu a').forEach(function (link) {
    link.addEventListener('click', function () {
      if (isMobile()) {
        closeMobileNav();
      }
    });
  });

  document.addEventListener('keydown', function (e) {
    if (e.key === 'Escape') {
      closeMobileNav();
    }
  });

  mq.addEventListener('change', function () {
    if (isMobile()) {
      document.body.classList.remove('nav-sidebar-collapsed');
      closeMobileNav();
    } else {
      closeMobileNav();
    }
  });
})();
