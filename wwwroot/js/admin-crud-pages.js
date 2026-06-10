(function () {
  var editSvg =
    '<svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"></path><path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"></path></svg>';
  var delSvg =
    '<svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="3 6 5 6 21 6"></polyline><path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"></path></svg>';

  function openModal(overlay) {
    if (!overlay) return;
    overlay.classList.add('active');
    document.body.style.overflow = 'hidden';
  }

  function closeModal(overlay) {
    if (!overlay) return;
    overlay.classList.remove('active');
    if (!document.querySelector('.modal-overlay.active')) {
      document.body.style.overflow = '';
    }
  }

  function wireModalContentClicks() {
    document.querySelectorAll('.modal-content').forEach(function (panel) {
      panel.addEventListener('click', function (e) {
        e.stopPropagation();
      });
    });
  }

  document.addEventListener('keydown', function (e) {
    if (e.key !== 'Escape') return;
    var any = document.querySelector('.modal-overlay.active');
    if (!any) return;
    document.querySelectorAll('.modal-overlay.active').forEach(function (el) {
      el.classList.remove('active');
    });
    document.body.style.overflow = '';
  });

  function initMaterials() {
    var addModal = document.getElementById('addModal');
    var editModal = document.getElementById('editModal');
    var deleteModal = document.getElementById('deleteModal');
    var tableBody = document.getElementById('materialsTableBody');
    var deleteNameEl = document.getElementById('deleteMaterialName');
    if (!tableBody) return;

    var editingRow = null;
    var deletingRow = null;
    var emptyStateRowSelector = 'tr[data-empty-state="materials"]';

    function closeAll() {
      closeModal(addModal);
      closeModal(editModal);
      closeModal(deleteModal);
      editingRow = null;
      deletingRow = null;
    }

    function ensureEmptyStateRow() {
      var hasRealRows = tableBody.querySelector('tr:not([data-empty-state="materials"])');
      if (hasRealRows) return;
      if (tableBody.querySelector(emptyStateRowSelector)) return;
      var tr = document.createElement('tr');
      tr.setAttribute('data-empty-state', 'materials');
      var td = document.createElement('td');
      td.colSpan = 8;
      td.style.padding = '1rem';
      td.style.color = 'var(--text-gray)';
      td.style.textAlign = 'center';
      td.textContent = 'لا توجد خامات بعد.';
      tr.appendChild(td);
      tableBody.appendChild(tr);
    }

    function clearEmptyStateRow() {
      tableBody.querySelectorAll(emptyStateRowSelector).forEach(function (tr) {
        tr.remove();
      });
    }

    function qtySpanClass(qty, alert) {
      return parseFloat(qty) <= parseFloat(alert) ? 'badge-danger' : 'badge-muted';
    }

    function getAntiForgeryToken() {
      return document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
    }

    function postForm(url, data) {
      var token = getAntiForgeryToken();
      var body = new URLSearchParams();
      Object.keys(data).forEach(function (k) {
        if (data[k] === undefined || data[k] === null) return;
        body.append(k, data[k]);
      });
      return fetch(url, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8',
          RequestVerificationToken: token,
        },
        body: body.toString(),
      }).then(function (res) {
        return res
          .json()
          .catch(function () {
            return null;
          })
          .then(function (payload) {
            if (res.ok) return payload;
            var msg = payload?.message || 'حدث خطأ غير متوقع';
            var err = new Error(msg);
            err.status = res.status;
            throw err;
          });
      });
    }

    function setRowDataset(tr, data) {
      tr.dataset.id = data.id;
      tr.dataset.name = data.name;
      tr.dataset.code = data.code;
      tr.dataset.unitId = String(data.unitId);
      tr.dataset.unitName = data.unitName;
      tr.dataset.unitSymbol = data.unitSymbol;
      tr.dataset.qty = String(data.qty);
      tr.dataset.originalQty = String(data.originalQty);
      tr.dataset.createdAt = String(data.createdAt);
      tr.dataset.alert = String(data.alert);
    }

    function readRowData(tr) {
      return {
        id: tr.dataset.id || tr.dataset.code || '',
        name: tr.dataset.name || '',
        code: tr.dataset.code || '',
        unitId: tr.dataset.unitId || '',
        unitName: tr.dataset.unitName || '',
        unitSymbol: tr.dataset.unitSymbol || '',
        qty: tr.dataset.qty || '0',
        originalQty: tr.dataset.originalQty || '0',
        createdAt: tr.dataset.createdAt || '',
        alert: tr.dataset.alert || '0',
      };
    }

    function updateRowCells(tr, data) {
      tr.cells[0].textContent = data.name;
      tr.cells[1].textContent = data.code;
      tr.cells[1].style.color = 'var(--text-gray)';
      tr.cells[2].textContent = data.unitName;
      var span = tr.cells[3].querySelector('span');
      if (span) {
        span.className = qtySpanClass(data.qty, data.alert);
        span.textContent = data.qty + ' ' + data.unitSymbol;
      }
      tr.cells[4].textContent = data.originalQty + ' ' + data.unitSymbol;
      tr.cells[4].style.color = 'var(--text-gray)';
      tr.cells[5].textContent = data.createdAt || '-';
      tr.cells[5].style.color = 'var(--text-gray)';
      tr.cells[6].textContent = data.alert + ' ' + data.unitSymbol;
      tr.cells[6].style.color = 'var(--text-gray)';
      setRowDataset(tr, data);
    }

    function createMaterialRow(data) {
      var tr = document.createElement('tr');
      setRowDataset(tr, data);

      var tdName = document.createElement('td');
      tdName.style.fontWeight = '600';
      tdName.textContent = data.name;

      var tdCode = document.createElement('td');
      tdCode.style.color = 'var(--text-gray)';
      tdCode.textContent = data.code;

      var tdUnit = document.createElement('td');
      tdUnit.textContent = data.unitName;

      var tdQty = document.createElement('td');
      var span = document.createElement('span');
      span.className = qtySpanClass(data.qty, data.alert);
      span.textContent = data.qty + ' ' + data.unitSymbol;
      tdQty.appendChild(span);

      var tdAlert = document.createElement('td');
      tdAlert.style.color = 'var(--text-gray)';
      tdAlert.textContent = data.alert + ' ' + data.unitSymbol;

      var tdOriginalQty = document.createElement('td');
      tdOriginalQty.style.color = 'var(--text-gray)';
      tdOriginalQty.textContent = data.originalQty + ' ' + data.unitSymbol;

      var tdCreatedAt = document.createElement('td');
      tdCreatedAt.style.color = 'var(--text-gray)';
      tdCreatedAt.textContent = data.createdAt || '-';

      var tdActions = document.createElement('td');
      var wrap = document.createElement('div');
      wrap.className = 'action-cell';
      var editBtn = document.createElement('button');
      editBtn.type = 'button';
      editBtn.className = 'action-btn action-btn-edit';
      editBtn.title = 'تعديل';
      editBtn.innerHTML = editSvg;
      var delBtn = document.createElement('button');
      delBtn.type = 'button';
      delBtn.className = 'action-btn action-btn-delete';
      delBtn.title = 'حذف';
      delBtn.innerHTML = delSvg;
      wrap.appendChild(editBtn);
      wrap.appendChild(delBtn);
      tdActions.appendChild(wrap);

      tr.appendChild(tdName);
      tr.appendChild(tdCode);
      tr.appendChild(tdUnit);
      tr.appendChild(tdQty);
      tr.appendChild(tdOriginalQty);
      tr.appendChild(tdCreatedAt);
      tr.appendChild(tdAlert);
      tr.appendChild(tdActions);
      return tr;
    }

    ensureEmptyStateRow();

    if (addModal) {
      document.getElementById('btnOpenAddMaterial')?.addEventListener('click', function () {
        closeAll();
        document.getElementById('addMaterialForm')?.reset();
        openModal(addModal);
      });
    }

    document.querySelectorAll('[data-close-modal]').forEach(function (btn) {
      var id = btn.getAttribute('data-close-modal');
      if (!id || ['addModal', 'editModal', 'deleteModal'].indexOf(id) === -1) return;
      if (id === 'addModal' && !addModal) return;
      btn.addEventListener('click', function () {
        var overlay = document.getElementById(id);
        closeModal(overlay);
        if (overlay === editModal) editingRow = null;
        if (overlay === deleteModal) deletingRow = null;
      });
    });

    [addModal, editModal, deleteModal].forEach(function (overlay) {
      overlay?.addEventListener('click', function (e) {
        if (e.target === overlay) {
          closeModal(overlay);
          if (overlay === editModal) editingRow = null;
          if (overlay === deleteModal) deletingRow = null;
        }
      });
    });

    tableBody.addEventListener('click', function (e) {
      var editBtn = e.target.closest('.action-btn-edit');
      var delBtn = e.target.closest('.action-btn-delete');
      var row = (editBtn || delBtn)?.closest('tr');
      if (!row || row.parentElement !== tableBody) return;

      if (editBtn) {
        var d = readRowData(row);
        editingRow = row;
        document.getElementById('editMaterialName').value = d.name;
        document.getElementById('editMaterialCode').value = d.code;
        document.getElementById('editMaterialUnit').value = d.unitId;
        document.getElementById('editMaterialQty').value = d.qty;
        document.getElementById('editMaterialAlert').value = d.alert;
        closeModal(addModal);
        closeModal(deleteModal);
        openModal(editModal);
      }

      if (delBtn) {
        deletingRow = row;
        var d2 = readRowData(row);
        if (deleteNameEl) deleteNameEl.textContent = d2.name;
        closeModal(addModal);
        closeModal(editModal);
        openModal(deleteModal);
      }
    });

    document.getElementById('addMaterialForm')?.addEventListener('submit', function (e) {
      e.preventDefault();
      var unitId = document.getElementById('materialUnit').value;
      var data = {
        id: document.getElementById('materialCode').value.trim(),
        name: document.getElementById('materialName').value.trim(),
        quantity: document.getElementById('materialQty').value,
        unitId: unitId,
        alertLimit: document.getElementById('materialAlert').value,
      };
      if (!data.id || !data.name || !data.unitId) return;
      postForm('/Materials/CreateModal', data)
        .then(function (saved) {
          clearEmptyStateRow();
          //tableBody.appendChild(
          //  createMaterialRow({
          //    id: saved.id,
          //    name: saved.name,
          //    code: saved.id,
          //    unitId: saved.unitId,
          //    unitName: saved.unitName,
          //    unitSymbol: saved.unitSymbol,
          //    qty: saved.quantity,
          //    createdAt: (saved.createdAt || '').split('T')[0],
          //    alert: saved.alertLimit,
          //  })
            //);
            window.location.reload();
          e.target.reset();
          closeModal(addModal);
        })
        .catch(function (err) {
          alert(err.message || 'فشل حفظ الخامة');
        });
    });

    document.getElementById('editMaterialForm')?.addEventListener('submit', function (e) {
      e.preventDefault();
      if (!editingRow) return;
      var materialId = document.getElementById('editMaterialCode').value.trim();
      var data = {
        id: materialId,
        name: document.getElementById('editMaterialName').value.trim(),
        quantity: document.getElementById('editMaterialQty').value,
        unitId: document.getElementById('editMaterialUnit').value,
        alertLimit: document.getElementById('editMaterialAlert').value,
      };
      if (!data.id || !data.name || !data.unitId) return;

      postForm('/Materials/EditModal', data)
        .then(function (saved) {
          //updateRowCells(editingRow, {
          //  id: saved.id,
          //  name: saved.name,
          //  code: saved.id,
          //  unitId: saved.unitId,
          //  unitName: saved.unitName,
          //  unitSymbol: saved.unitSymbol,
          //  qty: saved.quantity,
          //  alert: saved.alertLimit,
            //});
            window.location.reload();
          editingRow = null;
          closeModal(editModal);
        })
        .catch(function (err) {
          alert(err.message || 'فشل حفظ التعديلات');
        });
    });

    document.getElementById('btnConfirmDelete')?.addEventListener('click', function () {
      if (!deletingRow || deletingRow.parentElement !== tableBody) {
        deletingRow = null;
        closeModal(deleteModal);
        return;
      }
      var id = deletingRow.dataset.id || deletingRow.dataset.code;
      postForm('/Materials/DeleteModal', { id: id })
        .then(function () {
          deletingRow.remove();
          deletingRow = null;
          closeModal(deleteModal);
          ensureEmptyStateRow();
        })
        .catch(function (err) {
          alert(err.message || 'فشل الحذف');
        });
    });
  }

  function statusBadgeClass(status) {
    return status === 'معطل' ? 'badge-muted' : 'badge success';
  }

  function initProducts() {
    var addModal = document.getElementById('addProductModal');
    var editModal = document.getElementById('editProductModal');
    var deleteModal = document.getElementById('deleteProductModal');
    var tableBody = document.getElementById('productsTableBody');
    var deleteNameEl = document.getElementById('deleteProductName');
    if (!tableBody || !addModal) return;

    var editingRow = null;
    var deletingRow = null;
    var emptyStateRowSelector = 'tr[data-empty-state="products"]';

    function closeAll() {
      closeModal(addModal);
      closeModal(editModal);
      closeModal(deleteModal);
      editingRow = null;
      deletingRow = null;
    }

    function ensureEmptyStateRow() {
      var hasRealRows = tableBody.querySelector('tr:not([data-empty-state="products"])');
      if (hasRealRows) return;
      if (tableBody.querySelector(emptyStateRowSelector)) return;
      var tr = document.createElement('tr');
      tr.setAttribute('data-empty-state', 'products');
      var td = document.createElement('td');
      td.colSpan = 6;
      td.style.padding = '1rem';
      td.style.color = 'var(--text-gray)';
      td.style.textAlign = 'center';
      td.textContent = 'لا توجد منتجات بعد.';
      tr.appendChild(td);
      tableBody.appendChild(tr);
    }

    function clearEmptyStateRow() {
      tableBody.querySelectorAll(emptyStateRowSelector).forEach(function (tr) {
        tr.remove();
      });
    }

    function getAntiForgeryToken() {
      return document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
    }

    function postForm(url, data, multi) {
      var token = getAntiForgeryToken();
      var body = new URLSearchParams();
      Object.keys(data).forEach(function (k) {
        if (data[k] === undefined || data[k] === null) return;
        body.append(k, data[k]);
      });
      if (multi && Array.isArray(multi.materialIds)) {
        multi.materialIds.forEach(function (mid) {
          body.append('materialIds', mid);
        });
      }
      if (multi && Array.isArray(multi.materialQuantities)) {
        multi.materialQuantities.forEach(function (qty) {
          body.append('materialQuantities', String(qty));
        });
      }
      return fetch(url, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8',
          RequestVerificationToken: token,
        },
        body: body.toString(),
      }).then(function (res) {
        return res
          .json()
          .catch(function () {
            return null;
          })
          .then(function (payload) {
            if (res.ok) return payload;
            var msg = payload?.message || 'حدث خطأ غير متوقع';
            var err = new Error(msg);
            err.status = res.status;
            throw err;
          });
      });
    }

    function setRowDataset(tr, d) {
      tr.dataset.id = d.id;
      tr.dataset.name = d.name;
      tr.dataset.unitId = String(d.unitId);
      tr.dataset.unitName = d.unitName;
      tr.dataset.unitSymbol = d.unitSymbol;
      tr.dataset.price = String(d.price);
      tr.dataset.materialIds = (d.materialIds || []).join(',');
      tr.dataset.materialQuantities = (d.materialQuantities || []).join(',');
      tr.dataset.materialsCount = String(d.materialsCount || 0);
    }

    function readRowData(tr) {
      var ids = (tr.dataset.materialIds || '')
        .split(',')
        .map(function (x) {
          return x.trim();
        })
        .filter(Boolean);
      return {
        id: tr.dataset.id || '',
        name: tr.dataset.name || '',
        unitId: tr.dataset.unitId || '',
        unitName: tr.dataset.unitName || '',
        unitSymbol: tr.dataset.unitSymbol || '',
        price: tr.dataset.price || '',
        materialsCount: tr.dataset.materialsCount || '0',
        materialIds: ids,
        materialQuantities: (tr.dataset.materialQuantities || '')
          .split(',')
          .map(function (x) {
            return x.trim();
          })
          .filter(Boolean)
          .map(function (x) {
            var n = Number(x);
            return isNaN(n) ? 1 : n;
          }),
      };
    }

    function updateRowCells(tr, d) {
      tr.cells[0].textContent = d.name;
      tr.cells[0].style.fontWeight = '600';
      tr.cells[1].textContent = d.id;
      tr.cells[1].style.color = 'var(--text-gray)';
      tr.cells[2].textContent = d.unitName;
      tr.cells[3].textContent = Number(d.price || 0).toFixed(2) + ' ج.م';
      tr.cells[3].style.fontWeight = '600';
      tr.cells[4].textContent = d.materialsCount;
      tr.cells[4].style.color = 'var(--text-gray)';
      setRowDataset(tr, d);
    }

    function createRow(d) {
      var tr = document.createElement('tr');
      setRowDataset(tr, d);

      var td0 = document.createElement('td');
      td0.style.fontWeight = '600';
      td0.textContent = d.name;

      var td1 = document.createElement('td');
      td1.style.color = 'var(--text-gray)';
      td1.textContent = d.id;

      var td2 = document.createElement('td');
      td2.textContent = d.unitName;

      var td3 = document.createElement('td');
      td3.style.fontWeight = '600';
      td3.textContent = Number(d.price || 0).toFixed(2) + ' ج.م';

      var td4 = document.createElement('td');
      td4.style.color = 'var(--text-gray)';
      td4.textContent = d.materialsCount;

      var td5 = document.createElement('td');
      var wrap = document.createElement('div');
      wrap.className = 'action-cell';
      var eb = document.createElement('button');
      eb.type = 'button';
      eb.className = 'action-btn action-btn-edit';
      eb.title = 'تعديل';
      eb.innerHTML = editSvg;
      var db = document.createElement('button');
      db.type = 'button';
      db.className = 'action-btn action-btn-delete';
      db.title = 'حذف';
      db.innerHTML = delSvg;
      wrap.appendChild(eb);
      wrap.appendChild(db);
      td5.appendChild(wrap);

      tr.appendChild(td0);
      tr.appendChild(td1);
      tr.appendChild(td2);
      tr.appendChild(td3);
      tr.appendChild(td4);
      tr.appendChild(td5);
      return tr;
    }

    function setMultiSelectValues(selectEl, values) {
      if (!selectEl) return;
      var set = new Set(values || []);
      Array.from(selectEl.options).forEach(function (opt) {
        opt.selected = set.has(opt.value);
      });
    }

    function getMultiSelectValues(selectEl) {
      if (!selectEl) return [];
      return Array.from(selectEl.selectedOptions).map(function (o) {
        return o.value;
      });
    }

    function renderMaterialQtyInputs(selectEl, containerEl, quantitiesByMaterialId) {
      if (!selectEl || !containerEl) return;
      var selected = Array.from(selectEl.selectedOptions);
      containerEl.innerHTML = '';

      if (selected.length === 0) {
        var empty = document.createElement('p');
        empty.style.color = 'var(--text-gray)';
        empty.style.fontSize = '12px';
        empty.style.margin = '0';
        empty.textContent = 'اختر خامات أولاً لإدخال الكميات.';
        containerEl.appendChild(empty);
        return;
      }

      selected.forEach(function (opt) {
        var wrap = document.createElement('div');
        var label = document.createElement('label');
        label.className = 'form-label';
        label.textContent = 'الكمية - ' + opt.text;
        var input = document.createElement('input');
        input.type = 'number';
        input.min = '0.0001';
        input.step = '0.0001';
        input.className = 'form-input';
        input.setAttribute('data-material-id', opt.value);
        var existing = quantitiesByMaterialId && quantitiesByMaterialId[opt.value];
        input.value = existing ? String(existing) : '1';
        wrap.appendChild(label);
        wrap.appendChild(input);
        containerEl.appendChild(wrap);
      });
    }

    function collectMaterialQuantities(containerEl, materialIds) {
      var map = {};
      (containerEl ? Array.from(containerEl.querySelectorAll('input[data-material-id]')) : []).forEach(function (el) {
        map[el.getAttribute('data-material-id') || ''] = el.value;
      });

      return materialIds.map(function (id) {
        var val = Number(map[id]);
        if (isNaN(val) || val <= 0) return null;
        return val;
      });
    }

    ensureEmptyStateRow();

    document.getElementById('btnOpenAddProduct')?.addEventListener('click', function () {
      closeAll();
      document.getElementById('addProductForm')?.reset();
      var addMaterialsEl = document.getElementById('productMaterials');
      setMultiSelectValues(addMaterialsEl, []);
      renderMaterialQtyInputs(addMaterialsEl, document.getElementById('productMaterialQtyList'), {});
      openModal(addModal);
    });

    document.getElementById('productMaterials')?.addEventListener('change', function () {
      renderMaterialQtyInputs(this, document.getElementById('productMaterialQtyList'), {});
    });

    document.getElementById('editProductMaterials')?.addEventListener('change', function () {
      renderMaterialQtyInputs(this, document.getElementById('editProductMaterialQtyList'), {});
    });

    document.querySelectorAll('[data-close-modal]').forEach(function (btn) {
      var id = btn.getAttribute('data-close-modal');
      if (!id || ['addProductModal', 'editProductModal', 'deleteProductModal'].indexOf(id) === -1) return;
      btn.addEventListener('click', function () {
        var overlay = document.getElementById(id);
        closeModal(overlay);
        if (overlay === editModal) editingRow = null;
        if (overlay === deleteModal) deletingRow = null;
      });
    });

    [addModal, editModal, deleteModal].forEach(function (overlay) {
      overlay?.addEventListener('click', function (e) {
        if (e.target === overlay) {
          closeModal(overlay);
          if (overlay === editModal) editingRow = null;
          if (overlay === deleteModal) deletingRow = null;
        }
      });
    });

    tableBody.addEventListener('click', function (e) {
      var editBtn = e.target.closest('.action-btn-edit');
      var delBtn = e.target.closest('.action-btn-delete');
      var row = (editBtn || delBtn)?.closest('tr');
      if (!row || row.parentElement !== tableBody) return;

      if (editBtn) {
        var d = readRowData(row);
        editingRow = row;
        document.getElementById('editProductName').value = d.name;
        document.getElementById('editProductId').value = d.id;
        document.getElementById('editProductUnit').value = d.unitId;
        document.getElementById('editProductPrice').value = d.price;
        var editMaterialsEl = document.getElementById('editProductMaterials');
        setMultiSelectValues(editMaterialsEl, d.materialIds);
        var quantityMap = {};
        d.materialIds.forEach(function (id, idx) {
          quantityMap[id] = d.materialQuantities[idx] || 1;
        });
        renderMaterialQtyInputs(editMaterialsEl, document.getElementById('editProductMaterialQtyList'), quantityMap);
        closeModal(addModal);
        closeModal(deleteModal);
        openModal(editModal);
      }

      if (delBtn) {
        deletingRow = row;
        if (deleteNameEl) deleteNameEl.textContent = readRowData(row).name;
        closeModal(addModal);
        closeModal(editModal);
        openModal(deleteModal);
      }
    });

    document.getElementById('addProductForm')?.addEventListener('submit', function (e) {
      e.preventDefault();
      var materialIds = getMultiSelectValues(document.getElementById('productMaterials'));
      var materialQuantities = collectMaterialQuantities(document.getElementById('productMaterialQtyList'), materialIds);
      var d = {
        id: document.getElementById('productId').value.trim(),
        name: document.getElementById('productName').value.trim(),
        unitId: document.getElementById('productUnit').value,
        price: document.getElementById('productPrice').value.trim(),
      };
      if (!d.id || !d.name || !d.unitId) return;
      if (materialQuantities.some(function (q) { return q === null; })) {
        alert('من فضلك أدخل كمية صحيحة لكل خامة أكبر من صفر.');
        return;
      }
      postForm('/Products/CreateModal', d, { materialIds: materialIds, materialQuantities: materialQuantities })
        .then(function (saved) {
          clearEmptyStateRow();
          tableBody.appendChild(
            createRow({
              id: saved.id,
              name: saved.name,
              unitId: saved.unitId,
              unitName: saved.unitName,
              unitSymbol: saved.unitSymbol,
              price: saved.price,
              materialsCount: saved.materialsCount,
              materialIds: saved.materialIds || [],
              materialQuantities: saved.materialQuantities || [],
            })
          );
          e.target.reset();
          closeModal(addModal);
        })
        .catch(function (err) {
          alert(err.message || 'فشل حفظ المنتج');
        });
    });

    document.getElementById('editProductForm')?.addEventListener('submit', function (e) {
      e.preventDefault();
      if (!editingRow) return;
      var materialIds = getMultiSelectValues(document.getElementById('editProductMaterials'));
      var materialQuantities = collectMaterialQuantities(document.getElementById('editProductMaterialQtyList'), materialIds);
      var d = {
        id: document.getElementById('editProductId').value.trim(),
        name: document.getElementById('editProductName').value.trim(),
        unitId: document.getElementById('editProductUnit').value,
        price: document.getElementById('editProductPrice').value.trim(),
      };
      if (!d.id || !d.name || !d.unitId) return;
      if (materialQuantities.some(function (q) { return q === null; })) {
        alert('من فضلك أدخل كمية صحيحة لكل خامة أكبر من صفر.');
        return;
      }

      postForm('/Products/EditModal', d, { materialIds: materialIds, materialQuantities: materialQuantities })
        .then(function (saved) {
          updateRowCells(editingRow, {
            id: saved.id,
            name: saved.name,
            unitId: saved.unitId,
            unitName: saved.unitName,
            unitSymbol: saved.unitSymbol,
            price: saved.price,
            materialsCount: saved.materialsCount,
            materialIds: saved.materialIds || [],
            materialQuantities: saved.materialQuantities || [],
          });
          editingRow = null;
          closeModal(editModal);
        })
        .catch(function (err) {
          alert(err.message || 'فشل حفظ التعديلات');
        });
    });

    document.getElementById('btnConfirmDeleteProduct')?.addEventListener('click', function () {
      if (!deletingRow || deletingRow.parentElement !== tableBody) {
        deletingRow = null;
        closeModal(deleteModal);
        return;
      }
      var id = deletingRow.dataset.id;
      postForm('/Products/DeleteModal', { id: id })
        .then(function () {
          deletingRow.remove();
          deletingRow = null;
          closeModal(deleteModal);
          ensureEmptyStateRow();
        })
        .catch(function (err) {
          alert(err.message || 'فشل الحذف');
        });
    });
  }

  function initUnits() {
    var addModal = document.getElementById('addUnitModal');
    var editModal = document.getElementById('editUnitModal');
    var deleteModal = document.getElementById('deleteUnitModal');
    var tableBody = document.getElementById('unitsTableBody');
    var deleteNameEl = document.getElementById('deleteUnitName');
    if (!tableBody || !addModal) return;

    var editingRow = null;
    var deletingRow = null;

    function closeAll() {
      closeModal(addModal);
      closeModal(editModal);
      closeModal(deleteModal);
      editingRow = null;
      deletingRow = null;
    }

    function getAntiForgeryToken() {
      return document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
    }

    function postForm(url, data) {
      var token = getAntiForgeryToken();
      var body = new URLSearchParams();
      Object.keys(data).forEach(function (k) {
        if (data[k] === undefined || data[k] === null) return;
        body.append(k, data[k]);
      });
      return fetch(url, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8',
          RequestVerificationToken: token,
        },
        body: body.toString(),
      }).then(function (res) {
        return res
          .json()
          .catch(function () {
            return null;
          })
          .then(function (payload) {
            if (res.ok) return payload;
            var msg = payload?.message || 'حدث خطأ غير متوقع';
            var err = new Error(msg);
            err.status = res.status;
            throw err;
          });
      });
    }

    function setRowDataset(tr, d) {
      tr.dataset.id = d.id;
      tr.dataset.name = d.name;
      tr.dataset.symbol = d.symbol;
      tr.dataset.notes = d.notes || '';
    }

    function readRowData(tr) {
      return {
        id: tr.dataset.id || '',
        name: tr.dataset.name || '',
        symbol: tr.dataset.symbol || '',
        notes: tr.dataset.notes || '',
      };
    }

    function updateRowCells(tr, d) {
      tr.cells[0].textContent = d.name;
      tr.cells[0].style.fontWeight = '600';
      tr.cells[1].textContent = d.symbol;
      tr.cells[1].style.color = 'var(--text-gray)';
      tr.cells[2].textContent = d.notes ? d.notes : '-';
      tr.cells[2].style.color = 'var(--text-gray)';
      setRowDataset(tr, d);
    }

    function createRow(d) {
      var tr = document.createElement('tr');
      setRowDataset(tr, d);
      var td0 = document.createElement('td');
      td0.style.fontWeight = '600';
      td0.textContent = d.name;
      var td1 = document.createElement('td');
      td1.style.color = 'var(--text-gray)';
      td1.textContent = d.symbol;
      var td2 = document.createElement('td');
      td2.style.color = 'var(--text-gray)';
      td2.textContent = d.notes ? d.notes : '-';
      var td3 = document.createElement('td');
      var wrap = document.createElement('div');
      wrap.className = 'action-cell';
      var eb = document.createElement('button');
      eb.type = 'button';
      eb.className = 'action-btn action-btn-edit';
      eb.title = 'تعديل';
      eb.innerHTML = editSvg;
      var db = document.createElement('button');
      db.type = 'button';
      db.className = 'action-btn action-btn-delete';
      db.title = 'حذف';
      db.innerHTML = delSvg;
      wrap.appendChild(eb);
      wrap.appendChild(db);
      td3.appendChild(wrap);
      tr.appendChild(td0);
      tr.appendChild(td1);
      tr.appendChild(td2);
      tr.appendChild(td3);
      return tr;
    }

    document.getElementById('btnOpenAddUnit')?.addEventListener('click', function () {
      closeAll();
      document.getElementById('addUnitForm')?.reset();
      openModal(addModal);
    });

    document.querySelectorAll('[data-close-modal]').forEach(function (btn) {
      var id = btn.getAttribute('data-close-modal');
      if (!id || ['addUnitModal', 'editUnitModal', 'deleteUnitModal'].indexOf(id) === -1) return;
      btn.addEventListener('click', function () {
        var overlay = document.getElementById(id);
        closeModal(overlay);
        if (overlay === editModal) editingRow = null;
        if (overlay === deleteModal) deletingRow = null;
      });
    });

    [addModal, editModal, deleteModal].forEach(function (overlay) {
      overlay?.addEventListener('click', function (e) {
        if (e.target === overlay) {
          closeModal(overlay);
          if (overlay === editModal) editingRow = null;
          if (overlay === deleteModal) deletingRow = null;
        }
      });
    });

    tableBody.addEventListener('click', function (e) {
      var editBtn = e.target.closest('.action-btn-edit');
      var delBtn = e.target.closest('.action-btn-delete');
      var row = (editBtn || delBtn)?.closest('tr');
      if (!row || row.parentElement !== tableBody) return;

      if (editBtn) {
        var d = readRowData(row);
        editingRow = row;
        document.getElementById('editUnitName').value = d.name;
        document.getElementById('editUnitSymbol').value = d.symbol;
        document.getElementById('editUnitNotes').value = d.notes;
        closeModal(addModal);
        closeModal(deleteModal);
        openModal(editModal);
      }
      if (delBtn) {
        deletingRow = row;
        if (deleteNameEl) deleteNameEl.textContent = readRowData(row).name;
        closeModal(addModal);
        closeModal(editModal);
        openModal(deleteModal);
      }
    });

    document.getElementById('addUnitForm')?.addEventListener('submit', function (e) {
      e.preventDefault();
      var d = {
        name: document.getElementById('unitName').value.trim(),
        symbol: document.getElementById('unitSymbol').value.trim(),
        notes: document.getElementById('unitNotes').value.trim(),
      };
      if (!d.name || !d.symbol) return;
      postForm('/Units/CreateModal', d)
        .then(function (saved) {
          //tableBody.appendChild(
          //  createRow({
          //    id: saved.id,
          //    name: saved.name,
          //    symbol: saved.symbol,
          //    notes: saved.notes || '',
          //  })
            //);
          window.location.reload();
          e.target.reset();
          closeModal(addModal);
        })
        .catch(function (err) {
          alert(err.message || 'فشل حفظ الوحدة');
        });
    });

    document.getElementById('editUnitForm')?.addEventListener('submit', function (e) {
      e.preventDefault();
      if (!editingRow) return;
      var d = {
        id: editingRow.dataset.id,
        name: document.getElementById('editUnitName').value.trim(),
        symbol: document.getElementById('editUnitSymbol').value.trim(),
        notes: document.getElementById('editUnitNotes').value.trim(),
      };
      if (!d.id || !d.name || !d.symbol) return;
      postForm('/Units/EditModal', d)
        .then(function (saved) {
          updateRowCells(editingRow, {
            id: saved.id,
            name: saved.name,
            symbol: saved.symbol,
            notes: saved.notes || '',
          });
          editingRow = null;
          closeModal(editModal);
        })
        .catch(function (err) {
          alert(err.message || 'فشل حفظ التعديلات');
        });
    });

    document.getElementById('btnConfirmDeleteUnit')?.addEventListener('click', function () {
      if (!deletingRow || deletingRow.parentElement !== tableBody) {
        deletingRow = null;
        closeModal(deleteModal);
        return;
      }
      var id = deletingRow.dataset.id;
      postForm('/Units/DeleteModal', { id: id })
        .then(function () {
          deletingRow.remove();
          deletingRow = null;
          closeModal(deleteModal);
        })
        .catch(function (err) {
          alert(err.message || 'فشل الحذف');
        });
    });
    }

    function initReturns() {
        var addModal = document.getElementById('addReturnModal');
        if (!addModal) return;

        function closeAll() {
            closeModal(addModal);
        }

        function getAntiForgeryToken() {
            return document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
        }

        function postForm(url, data) {
            var token = getAntiForgeryToken();
            var body = new URLSearchParams();
            Object.keys(data).forEach(function (k) {
                if (data[k] === undefined || data[k] === null) return;
                body.append(k, data[k]);
            });
            return fetch(url, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8',
                    RequestVerificationToken: token,
                },
                body: body.toString(),
            }).then(function (res) {
                return res
                    .json()
                    .catch(function () {
                        return null;
                    })
                    .then(function (payload) {
                        if (res.ok) return payload;
                        var err = new Error(JSON.stringify(payload));
                        err.status = res.status;
                        throw err;
                    });
            });
        }

        // submit addReturnForm to Return/Create
        document.getElementById('addReturnForm')?.addEventListener('submit', function (e) {
            e.preventDefault();
            closeAll();

            var form = e.target;
            var fd = new FormData(form);
            var data = {};
            fd.forEach(function (value, key) {
                data[key] = value;
            });

            postForm('/Return/Create', data)
                .then(function (saved) {
                    // adjust behavior as needed: reload to show changes
                    window.location.reload();
                    form.reset();
                    closeModal(addModal);
                })
                .catch(function (err) {
                    var msg = '';
                    [...JSON.parse(err.message)].forEach(e => {
                        msg += e;
                    })
                    alert(msg);
                });
        });

        document.getElementById('btnOpenAddReturn')?.addEventListener('click', function () {
            closeAll();
            document.getElementById('addReturnForm')?.reset();
            openModal(addModal);
        });

        document.querySelectorAll('[data-close-modal]').forEach(function (btn) {
            var id = btn.getAttribute('data-close-modal');
            if (!id || ['addReturnForm'].indexOf(id) === -1) return;
            if (id === 'addReturnForm' && !addModal) return;
            btn.addEventListener('click', function () {
                var overlay = document.getElementById(id);
                closeModal(overlay);
            });
        });
    }

  wireModalContentClicks();
  initMaterials();
  initProducts();
  initUnits();
  initReturns();
})();