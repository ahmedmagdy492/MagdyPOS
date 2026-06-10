(function () {
  "use strict";

  /** @type {{ id: string, name: string, price: number, icon: string, qty: number }[]} */
  var cart = [];

  function parseTaxRate() {
    var root = document.getElementById("posRoot");
    if (!root) return 0.14;
    var v = parseFloat(root.getAttribute("data-tax-rate") || "0.14");
    return isNaN(v) ? 0.14 : v;
  }

  function money(n) {
    return n.toFixed(2) + " ج.م";
  }

  function itemCount() {
    return cart.reduce(function (s, l) {
      return s + l.qty;
    }, 0);
  }

  function subtotal() {
    return cart.reduce(function (s, l) {
      return s + l.price * l.qty;
    }, 0);
  }

  function taxAmount() {
    return subtotal() * parseTaxRate();
  }

  function grandTotal() {
    return subtotal() + taxAmount();
  }

  function findLine(id) {
    for (var i = 0; i < cart.length; i++) {
      if (cart[i].id === id) return cart[i];
    }
    return null;
  }

  function renderCart() {
    var wrap = document.getElementById("posCartItems");
    var countEl = document.getElementById("posCartCount");
    var subEl = document.getElementById("posSubtotal");
    var taxEl = document.getElementById("posTax");
    var totalEl = document.getElementById("posTotal");
    if (!wrap || !countEl || !subEl || !taxEl || !totalEl) return;

    var n = itemCount();
    countEl.textContent = n === 0 ? "0 عناصر" : n + (n === 1 ? " عنصر" : " عناصر");

    if (cart.length === 0) {
      wrap.innerHTML =
        '<p class="pos-cart-empty">السلة فارغة — اضغط على منتج لإضافته.</p>';
    } else {
      wrap.innerHTML = "";
      cart.forEach(function (line) {
        var row = document.createElement("div");
        row.className = "pos-cart-item";
        row.setAttribute("data-line-id", String(line.id));
        row.innerHTML =
          '<span class="pos-cart-item-icon" aria-hidden="true">' +
          escapeHtml(line.icon) +
          "</span>" +
          '<div class="pos-cart-item-body">' +
          "<h5>" +
          escapeHtml(line.name) +
          "</h5>" +
          '<p class="pos-unit-price">' +
          money(line.price) +
          "</p></div>" +
          '<div class="pos-qty-group">' +
          '<button type="button" class="pos-qty-btn pos-qty-minus" data-act="minus" aria-label="تقليل الكمية">' +
          '<svg width="16" height="16" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M20 12H4"/></svg></button>' +
          '<span class="pos-qty-value">' +
          line.qty +
          "</span>" +
          '<button type="button" class="pos-qty-btn pos-qty-plus" data-act="plus" aria-label="زيادة الكمية">' +
          '<svg width="16" height="16" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4"/></svg></button></div>' +
          '<button type="button" class="pos-remove-line" data-act="remove" aria-label="حذف">' +
          '<svg width="20" height="20" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"/></svg></button>';
        wrap.appendChild(row);
      });
    }

    subEl.textContent = money(subtotal());
    taxEl.textContent = money(taxAmount());
    totalEl.textContent = money(grandTotal());
  }

  function escapeHtml(s) {
    var d = document.createElement("div");
    d.textContent = s;
    return d.innerHTML;
  }

  function addProduct(id, name, price, icon) {
    var p = parseFloat(price);
    if (isNaN(p)) return;
    var existing = findLine(id);
    if (existing) {
      existing.qty += 1;
    } else {
      cart.push({ id: id, name: name, price: p, icon: icon || "📦", qty: 1 });
    }
    renderCart();
  }

  function setQty(id, delta) {
    var line = findLine(id);
    if (!line) return;
    line.qty += delta;
    if (line.qty <= 0) {
      cart = cart.filter(function (l) {
        return l.id !== id;
      });
    }
    renderCart();
  }

  function removeLine(id) {
    cart = cart.filter(function (l) {
      return l.id !== id;
    });
    renderCart();
  }

  function clearCart() {
    cart = [];
    renderCart();
  }

  function randomInvoiceNo() {
    return String(Math.floor(10000 + Math.random() * 90000));
  }

    function submitOrder() {
    var tipsRaw = document.getElementById("tipsField")?.value || "0";
    var parsedTips = parseFloat(tipsRaw);
    if (isNaN(parsedTips)) parsedTips = 0;

    var payload = {
      PaymentMethod: parseInt(document.getElementById("posPaymentMethod").value, 10),
      Discount: 0,
      Notes: "",
      Tips: parsedTips,
      Items: cart.map(function (line) {
        return {
          ProductId: line.id,
          Quantity: line.qty,
          UnitPrice: line.price,
        };
      }),
    };

    return fetch("/Orders/Submit", {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify(payload),
    }).then(function (res) {
      return res
        .json()
        .catch(function () {
          return null;
        })
        .then(function (body) {
          if (res.ok) return body;
          var message = (body && body.message) || "فشل حفظ الطلب.";
          throw new Error(message);
        });
    });
  }

  function fillReceiptAndPrint(orderNumber) {
    if (cart.length === 0) {
      alert("السلة فارغة.");
      return;
    }
    var receipt = document.getElementById("posReceipt");
    if (!receipt) return;

    var storeName = receipt.getAttribute("data-store-name") || "";
    var addr = receipt.getAttribute("data-store-address") || "";
    var phone = receipt.getAttribute("data-store-phone") || "";
    var cashier = receipt.getAttribute("data-cashier") || "";
    var shiftLabel = receipt.getAttribute("data-shift-label") || "";

    receipt.querySelector(".pos-rcpt-store").textContent = storeName;
    receipt.querySelector(".pos-rcpt-addr").textContent = addr;
    receipt.querySelector(".pos-rcpt-phone").textContent =
      phone ? "تليفون: " + phone : "";
    receipt.querySelector(".pos-rcpt-shift-label").textContent = shiftLabel;
    receipt.querySelector(".pos-rcpt-inv").textContent = orderNumber || randomInvoiceNo();
    receipt.querySelector(".pos-rcpt-date").textContent =
      new Date().toLocaleDateString("ar-EG", {
        year: "numeric",
        month: "2-digit",
        day: "2-digit",
      });
    receipt.querySelector(".pos-rcpt-time").textContent =
      new Date().toLocaleTimeString("ar-EG", {
        hour: "2-digit",
        minute: "2-digit",
      });
    receipt.querySelector(".pos-rcpt-cashier").textContent = cashier;

    var tbody = receipt.querySelector(".pos-rcpt-lines");
    tbody.innerHTML = "";
    cart.forEach(function (line) {
      var tr = document.createElement("tr");
      var lineTotal = line.price * line.qty;
      tr.innerHTML =
        "<td>" +
        escapeHtml(line.name) +
        "</td>" +
        "<td>" +
        line.qty +
        "</td>" +
        "<td>" +
        line.price.toFixed(2) +
        "</td>" +
        "<td>" +
        lineTotal.toFixed(2) +
        "</td>";
      tbody.appendChild(tr);
    });

    receipt.querySelector(".pos-rcpt-sub").textContent = money(subtotal());
    receipt.querySelector(".pos-rcpt-tax").textContent = money(taxAmount());
    receipt.querySelector(".pos-rcpt-total").textContent = money(grandTotal());

    window.print();
    clearCart();
  }

  function currentQuery() {
    var search = document.getElementById("posSearchProduct");
    return (search?.value || "").trim();
  }

  function loadProducts(q, page) {
    var wrap = document.getElementById("posProductsGridWrap");
    if (!wrap) return Promise.resolve();
    var url =
      "/Pos/ProductsGrid?q=" +
      encodeURIComponent(q || "") +
      "&page=" +
      encodeURIComponent(String(page || 1));
    return fetch(url, { headers: { "X-Requested-With": "fetch" } })
      .then(function (res) {
        return res.text();
      })
      .then(function (html) {
        wrap.innerHTML = html;
      })
      .catch(function () {
        // ignore transient network errors
      });
  }

  function debounce(fn, ms) {
    var t = 0;
    return function () {
      var args = arguments;
      clearTimeout(t);
      t = setTimeout(function () {
        fn.apply(null, args);
      }, ms);
    };
  }

  function bindGrid() {
    var wrap = document.getElementById("posProductsGridWrap");
    if (!wrap) return;

    wrap.addEventListener("click", function (e) {
      var card = e.target.closest(".pos-product-card");
      if (card && wrap.contains(card)) {
        var id = card.getAttribute("data-id") || "";
        var name = card.getAttribute("data-name") || "";
        var price = card.getAttribute("data-price") || "0";
        var icon = card.getAttribute("data-icon") || "";
        if (id) addProduct(id, name, price, icon);
        return;
      }

      var pagerBtn = e.target.closest("[data-pos-page]");
      if (pagerBtn && wrap.contains(pagerBtn)) {
        if (pagerBtn.disabled) return;
        var page = parseInt(pagerBtn.getAttribute("data-pos-page") || "1", 10);
        if (isNaN(page) || page < 1) page = 1;
        loadProducts(currentQuery(), page);
      }
    });
  }

  function bindCartClicks() {
    var wrap = document.getElementById("posCartItems");
    if (!wrap) return;
    wrap.addEventListener("click", function (e) {
      var row = e.target.closest(".pos-cart-item");
      if (!row) return;
      var id = row.getAttribute("data-line-id") || "";
      if (!id) return;
      var btn = e.target.closest("button");
      if (!btn) return;
      var act = btn.getAttribute("data-act");
      if (act === "plus") setQty(id, 1);
      else if (act === "minus") setQty(id, -1);
      else if (act === "remove") removeLine(id);
    });
  }

  function setCurrentDate() {
    var el = document.getElementById("posCurrentDate");
    if (!el) return;
    el.textContent = new Date().toLocaleDateString("ar-EG", {
      weekday: "long",
      year: "numeric",
      month: "long",
      day: "numeric",
    });
  }

  document.addEventListener("DOMContentLoaded", function () {
    setCurrentDate();
    bindGrid();
    bindCartClicks();
    renderCart();

    var search = document.getElementById("posSearchProduct");
    if (search) {
      var run = debounce(function () {
        loadProducts(currentQuery(), 1);
      }, 250);
      search.addEventListener("input", run);
    }

    var clearBtn = document.getElementById("posClearCart");
    if (clearBtn) clearBtn.addEventListener("click", clearCart);

    var checkoutBtn = document.getElementById("posCheckoutBtn");
    if (checkoutBtn) {
      checkoutBtn.addEventListener("click", function () {
        if (cart.length === 0) {
          alert("السلة فارغة.");
          return;
        }

        checkoutBtn.disabled = true;
        submitOrder()
          .then(function (savedOrder) {
            fillReceiptAndPrint(savedOrder && savedOrder.orderNumber);
          })
          .catch(function (err) {
            alert((err && err.message) || "فشل حفظ الطلب.");
          })
          .finally(function () {
            checkoutBtn.disabled = false;
          });
      });
    }
  });
})();
