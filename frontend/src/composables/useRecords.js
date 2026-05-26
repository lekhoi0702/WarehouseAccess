import { ref, computed } from 'vue';

const records = ref([]);
const departments = ref([]);
const purposes = ref([]);
const employees = ref({});
const auditLogs = ref([]);
const defaultContact = ref({ name: "張主任", dept: "倉儲部" });
const loading = ref(false);

export function useRecords() {
  async function refreshData() {
    loading.value = true;
    try {
      const recsRes = await fetch('/api/records').then(r => r.json());
      records.value = recsRes;

      const deptsRes = await fetch('/api/departments').then(r => r.json());
      departments.value = deptsRes;

      const purposesRes = await fetch('/api/purposes').then(r => r.json());
      purposes.value = purposesRes;

      const empsRes = await fetch('/api/employees').then(r => r.json());
      employees.value = empsRes;

      const settingsRes = await fetch('/api/contact').then(r => r.json());
      defaultContact.value = settingsRes;

      const logsRes = await fetch('/api/audit_logs').then(r => r.json());
      auditLogs.value = logsRes;
    } catch (err) {
      console.error("API error loading datasets:", err);
    } finally {
      loading.value = false;
    }
  }

  async function handleEntry(form) {
    const res = await fetch('/api/records', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(form)
    }).then(r => r.json());
    if (res.success) {
      await refreshData();
    }
    return res;
  }

  async function handleCheckout(id, exitPhoto) {
    const res = await fetch(`/api/records/${id}/checkout`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ exitPhoto, exitTime: Date.now() })
    }).then(r => r.json());
    if (res.success) {
      await refreshData();
    }
    return res;
  }

  async function getCardDetails(cardId) {
    return fetch(`/api/records/by-card/${cardId}`).then(r => r.json());
  }

  async function executeCardCheckin(payload) {
    const res = await fetch('/api/records/checkin-card', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payload)
    }).then(r => r.json());
    if (res.success) {
      await refreshData();
    }
    return res;
  }

  async function executeCardCheckout(cardId) {
    const res = await fetch(`/api/records/checkout-card/${cardId}`, {
      method: 'POST'
    }).then(r => r.json());
    if (res.success) {
      await refreshData();
    }
    return res;
  }

  async function saveContact(name, dept) {
    const res = await fetch('/api/contact', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ name, dept, operator: '系統管理員' })
    }).then(r => r.json());
    if (res.success) {
      await refreshData();
    }
    return res;
  }

  async function importDepartments(importedDepts) {
    const res = await fetch('/api/departments/bulk', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ departments: importedDepts, operator: '系統管理員' })
    }).then(r => r.json());
    await refreshData();
    return res;
  }

  async function importEmployees(importedEmployees) {
    const res = await fetch('/api/employees/bulk', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ employees: importedEmployees, operator: '系統管理員' })
    }).then(r => r.json());
    await refreshData();
    return res;
  }

  async function addDepartment(name) {
    const res = await fetch('/api/departments', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ name, operator: '系統管理員' })
    }).then(r => r.json());
    await refreshData();
    return res;
  }

  async function editDepartment(oldName, newName) {
    const res = await fetch(`/api/departments/${oldName}`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ newName, operator: '系統管理員' })
    }).then(r => r.json());
    await refreshData();
    return res;
  }

  async function deleteDepartment(name) {
    const res = await fetch(`/api/departments/${name}?operatorName=系統管理員`, {
      method: 'DELETE'
    }).then(r => r.json());
    await refreshData();
    return res;
  }

  async function addPurpose(name) {
    const res = await fetch('/api/purposes', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ name, operator: '系統管理員' })
    }).then(r => r.json());
    await refreshData();
    return res;
  }

  async function editPurpose(oldName, newName) {
    const res = await fetch(`/api/purposes/${oldName}`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ newName, operator: '系統管理員' })
    }).then(r => r.json());
    await refreshData();
    return res;
  }

  async function deletePurpose(name) {
    const res = await fetch(`/api/purposes/${name}?operatorName=系統管理員`, {
      method: 'DELETE'
    }).then(r => r.json());
    await refreshData();
    return res;
  }

  async function addEmployee(id, name, dept, avatar) {
    const res = await fetch('/api/employees', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ id, name, dept, avatar, operator: '系統管理員' })
    }).then(r => r.json());
    await refreshData();
    return res;
  }

  async function editEmployee(id, name, dept, avatar) {
    const res = await fetch(`/api/employees/${id}`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ id, name, dept, avatar, operator: '系統管理員' })
    }).then(r => r.json());
    await refreshData();
    return res;
  }

  async function deleteEmployee(id) {
    const res = await fetch(`/api/employees/${id}?operatorName=系統管理員`, {
      method: 'DELETE'
    }).then(r => r.json());
    await refreshData();
    return res;
  }

  // --- Statistics computed properties ---
  const stats = computed(() => {
    const inside = records.value.filter(r => !r.exitTime);
    const todayStart = new Date().setHours(0, 0, 0, 0);
    const today = records.value.filter(r => r.entryTime >= todayStart || !r.exitTime);

    return {
      onSite: inside.length,
      today: today.length,
      vendor: inside.filter(r => r.type === 'vendor').length,
      brand: inside.filter(r => r.type === 'brand').length,
      audit: inside.filter(r => r.type === 'audit').length
    };
  });

  const activeOnSiteVisitors = computed(() => {
    return records.value.filter(r => !r.exitTime);
  });

  return {
    records,
    departments,
    purposes,
    employees,
    auditLogs,
    defaultContact,
    loading,
    refreshData,
    handleEntry,
    handleCheckout,
    getCardDetails,
    executeCardCheckin,
    executeCardCheckout,
    saveContact,
    importDepartments,
    importEmployees,
    addDepartment,
    editDepartment,
    deleteDepartment,
    addPurpose,
    editPurpose,
    deletePurpose,
    addEmployee,
    editEmployee,
    deleteEmployee,
    stats,
    activeOnSiteVisitors
  };
}
