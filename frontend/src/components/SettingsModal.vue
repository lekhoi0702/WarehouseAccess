<script setup>
import { ref, reactive, computed, onMounted } from 'vue';

const props = defineProps({
  t: Object,
  contact: Object,
  departments: Array,
  purposes: Array,
  employees: Object,
  auditLogs: Array
});

const emit = defineEmits(['close', 'refresh']);

const activeSubTab = ref("contact"); // "contact", "depts", "purposes", "employees", "audit"
const name = ref(props.contact.name);
const dept = ref(props.contact.dept);

// Find if current contact matches any existing employee ID
const matchedEmp = computed(() => {
  return Object.entries(props.employees).find(([id, emp]) => emp.name === name.value && emp.dept === dept.value);
});
const selectedEmpId = ref("");

onMounted(() => {
  if (matchedEmp.value) {
    selectedEmpId.value = matchedEmp.value[0];
  }
});

const isAdminUnlocked = ref(false); // Security PIN check
const pin = ref("");
const pinError = ref("");
const CORRECT_PIN = "1234";

function unlock() {
  if (pin.value === CORRECT_PIN) {
    isAdminUnlocked.value = true;
    pinError.value = "";
    pin.value = "";
  } else {
    pinError.value = props.t.pinError;
  }
}

// Save Contact settings
async function handleSaveContact() {
  try {
    const res = await fetch('/api/contact', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ name: name.value, dept: dept.value, operator: '系統管理員' })
    }).then(r => r.json());

    if (res.success) {
      alert(props.t.contactSaved);
      emit('refresh');
    }
  } catch (e) {
    alert("儲存失敗");
  }
}

function handleEmployeeChange(e) {
  const empIdVal = e.target.value;
  const emp = props.employees[empIdVal];
  if (emp) {
    name.value = emp.name;
    dept.value = emp.dept;
    selectedEmpId.value = empIdVal;
  }
}

// ─── CSV Import/Export Templates ─────────────────────────────────────────────
function downloadDeptTemplate() {
  const csv = "部門名稱\n研發部\n工程部\n測試部";
  const blob = new Blob(["\ufeff" + csv], { type: "text/csv;charset=utf-8" });
  const url = URL.createObjectURL(blob);
  const a = document.createElement("a");
  a.href = url;
  a.download = "departments_template.csv";
  a.click();
}

function downloadEmpTemplate() {
  const csv = "員工工號,員工姓名,所屬部門\nE008,林小美,品管部\nE009,王大同,倉儲部";
  const blob = new Blob(["\ufeff" + csv], { type: "text/csv;charset=utf-8" });
  const url = URL.createObjectURL(blob);
  const a = document.createElement("a");
  a.href = url;
  a.download = "employees_template.csv";
  a.click();
}

async function handleImportDepts(e) {
  const file = e.target.files[0];
  if (!file) return;
  const reader = new FileReader();
  reader.onload = async (event) => {
    try {
      const text = event.target.result;
      const lines = text.split(/\r?\n/);
      const importedDepts = [];

      for (let i = 1; i < lines.length; i++) {
        const val = lines[i].replace(/^\uFEFF/, "").trim();
        if (!val) continue;

        const cleanVal = val.split(",")[0].replace(/^["']|["']$/g, "").trim();
        if (cleanVal && !props.departments.includes(cleanVal) && !importedDepts.includes(cleanVal)) {
          importedDepts.push(cleanVal);
        }
      }

      if (importedDepts.length === 0) {
        alert("沒有可導入的新部門資料。");
        return;
      }

      const res = await fetch('/api/departments/bulk', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ departments: importedDepts, operator: '系統管理員' })
      }).then(r => r.json());

      emit('refresh');
      alert(`✓ 成功導入 ${res.addedCount} 個新部門！`);
    } catch (err) {
      alert("導入失敗，請確認檔案格式是否正確。");
    }
  };
  reader.readAsText(file, "utf-8");
  e.target.value = "";
}

async function handleImportEmployees(e) {
  const file = e.target.files[0];
  if (!file) return;
  const reader = new FileReader();
  reader.onload = async (event) => {
    try {
      const text = event.target.result;
      const lines = text.split(/\r?\n/);
      const importedEmployees = [];

      for (let i = 1; i < lines.length; i++) {
        const line = lines[i].replace(/^\uFEFF/, "").trim();
        if (!line) continue;

        const cols = line.split(",").map(c => c.replace(/^["']|["']$/g, "").trim());
        if (cols.length < 3) continue;

        const id = cols[0].toUpperCase();
        const nameVal = cols[1];
        const deptVal = cols[2];

        if (!id || !nameVal || !deptVal) continue;

        const colors = ["818cf8", "c084fc", "60a5fa", "f9a8d4", "86efac", "fcd34d", "fb923c"];
        const randomColor = colors[Math.floor(Math.random() * colors.length)];
        const avatar = `https://api.dicebear.com/7.x/thumbs/svg?seed=${id}&backgroundColor=1e293b&shapeColor=${randomColor}`;

        importedEmployees.push({
          id,
          name: nameVal,
          dept: deptVal,
          avatar
        });
      }

      if (importedEmployees.length === 0) {
        alert("沒有可導入的新員工資料。");
        return;
      }

      const res = await fetch('/api/employees/bulk', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ employees: importedEmployees, operator: '系統管理員' })
      }).then(r => r.json());

      emit('refresh');
      alert(`✓ 成功導入 ${res.addedCount} 筆員工資料！${res.autoDeptCount > 0 ? `\n(已自動建立 ${res.autoDeptCount} 個新部門)` : ""}`);
    } catch (err) {
      alert("導入失敗，請確認檔案格式是否正確。");
    }
  };
  reader.readAsText(file, "utf-8");
  e.target.value = "";
}

// ─── Department CRUD ─────────────────────────────────────────────────────────
const newDept = ref("");
const editingDeptIndex = ref(-1);
const editingDeptText = ref("");

async function handleAddDept() {
  const text = newDept.value.trim();
  if (!text) return;
  if (props.departments.includes(text)) {
    alert("部門已存在！");
    return;
  }
  try {
    await fetch('/api/departments', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ name: text, operator: '系統管理員' })
    });
    emit('refresh');
    newDept.value = "";
  } catch (e) {
    alert("新增部門失敗");
  }
}

function handleStartEditDept(index) {
  editingDeptIndex.value = index;
  editingDeptText.value = props.departments[index];
}

async function handleSaveEditDept(index) {
  const text = editingDeptText.value.trim();
  if (!text) return;
  const oldName = props.departments[index];
  if (props.departments.includes(text) && oldName !== text) {
    alert("部門已存在！");
    return;
  }
  try {
    await fetch(`/api/departments/${oldName}`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ newName: text, operator: '系統管理員' })
    });
    emit('refresh');
    editingDeptIndex.value = -1;
  } catch (e) {
    alert("編輯部門失敗");
  }
}

async function handleDeleteDept(nameVal) {
  if (!confirm(`確定要刪除部門 [${nameVal}] 嗎？`)) return;
  try {
    await fetch(`/api/departments/${nameVal}?operatorName=系統管理員`, {
      method: 'DELETE'
    });
    emit('refresh');
  } catch (e) {
    alert("刪除部門失敗");
  }
}

// ─── Purpose CRUD ────────────────────────────────────────────────────────────
const newPurpose = ref("");
const editingPurpIndex = ref(-1);
const editingPurpText = ref("");

async function handleAddPurpose() {
  const text = newPurpose.value.trim();
  if (!text) return;
  if (props.purposes.includes(text)) {
    alert("來訪事由已存在！");
    return;
  }
  try {
    await fetch('/api/purposes', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ name: text, operator: '系統管理員' })
    });
    emit('refresh');
    newPurpose.value = "";
  } catch (e) {
    alert("新增事由失敗");
  }
}

function handleStartEditPurp(index) {
  editingPurpIndex.value = index;
  editingPurpText.value = props.purposes[index];
}

async function handleSaveEditPurp(index) {
  const text = editingPurpText.value.trim();
  if (!text) return;
  const oldName = props.purposes[index];
  if (props.purposes.includes(text) && oldName !== text) {
    alert("來訪事由已存在！");
    return;
  }
  try {
    await fetch(`/api/purposes/${oldName}`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ newName: text, operator: '系統管理員' })
    });
    emit('refresh');
    editingPurpIndex.value = -1;
  } catch (e) {
    alert("編輯事由失敗");
  }
}

async function handleDeletePurp(nameVal) {
  if (!confirm(`確定要刪除事由 [${nameVal}] 嗎？`)) return;
  try {
    await fetch(`/api/purposes/${nameVal}?operatorName=系統管理員`, {
      method: 'DELETE'
    });
    emit('refresh');
  } catch (e) {
    alert("刪除事由失敗");
  }
}

// ─── Employee CRUD ───────────────────────────────────────────────────────────
const empId = ref("");
const empName = ref("");
const empDept = ref(props.departments[0] || "");
const isEditingEmp = ref(false);
const showEmpForm = ref(false);

function startAddEmp() {
  empId.value = "";
  empName.value = "";
  empDept.value = props.departments[0] || "";
  isEditingEmp.value = false;
  showEmpForm.value = true;
}

function startEditEmp(idVal, emp) {
  empId.value = idVal;
  empName.value = emp.name;
  empDept.value = emp.dept;
  isEditingEmp.value = true;
  showEmpForm.value = true;
}

async function handleSaveEmployee() {
  const idVal = empId.value.trim().toUpperCase();
  const nameVal = empName.value.trim();
  const deptVal = empDept.value;
  if (!idVal || !nameVal || !deptVal) {
    alert("請填寫所有必填欄位！");
    return;
  }

  try {
    const colors = ["818cf8", "c084fc", "60a5fa", "f9a8d4", "86efac", "fcd34d", "fb923c"];
    const randomColor = colors[Math.floor(Math.random() * colors.length)];
    const avatar = `https://api.dicebear.com/7.x/thumbs/svg?seed=${idVal}&backgroundColor=1e293b&shapeColor=${randomColor}`;

    const url = isEditingEmp.value ? `/api/employees/${idVal}` : '/api/employees';
    const method = isEditingEmp.value ? 'PUT' : 'POST';

    await fetch(url, {
      method,
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ id: idVal, name: nameVal, dept: deptVal, avatar, operator: '系統管理員' })
    });

    emit('refresh');
    showEmpForm.value = false;
  } catch (e) {
    alert("儲存員工失敗");
  }
}

async function handleDeleteEmployee(idVal) {
  if (!confirm(`確定要刪除員工 [${props.employees[idVal].name}] (工號: ${idVal}) 嗎？`)) return;
  try {
    await fetch(`/api/employees/${idVal}?operatorName=系統管理員`, {
      method: 'DELETE'
    });
    emit('refresh');
  } catch (e) {
    alert("刪除員工失敗");
  }
}

function formatTime(ts) {
  if (!ts) return "—";
  const d = new Date(ts);
  return d.toLocaleString("zh-TW", { hour12: false, month: "2-digit", day: "2-digit", hour: "2-digit", minute: "2-digit", second: "2-digit" });
}
</script>

<template>
  <div class="modal-backdrop">
    <!-- Unlocked Settings Container -->
    <div class="settings-modal-box" v-if="isAdminUnlocked">
      <div class="settings-header">
        <h2>{{ t.settingsTitle }}</h2>
        <button class="btn-close" @click="emit('close')">✕</button>
      </div>

      <!-- Settings Sub Navigation Tabs -->
      <div class="settings-tabs">
        <button :class="{ active: activeSubTab === 'contact' }" @click="activeSubTab = 'contact'">接洽人員</button>
        <button :class="{ active: activeSubTab === 'depts' }" @click="activeSubTab = 'depts'">部門維護</button>
        <button :class="{ active: activeSubTab === 'purposes' }" @click="activeSubTab = 'purposes'">事由維護</button>
        <button :class="{ active: activeSubTab === 'employees' }" @click="activeSubTab = 'employees'">員工名冊</button>
        <button :class="{ active: activeSubTab === 'audit' }" @click="activeSubTab = 'audit'">安全日誌</button>
      </div>

      <!-- Tab Content Frame -->
      <div class="settings-tab-content">
        
        <!-- Tab 1: Contact Settings -->
        <div v-if="activeSubTab === 'contact'">
          <h3>{{ t.contactSection }}</h3>
          <p class="section-desc">設定此系統預設對外的單一接洽聯絡窗口。</p>

          <div class="form-group">
            <label>快速選擇內部員工代入</label>
            <select :value="selectedEmpId" @change="handleEmployeeChange" class="select-field">
              <option value="">-- 手動輸入接洽人員 --</option>
              <option v-for="(emp, id) in employees" :key="id" :value="id">{{ emp.name }} ({{ emp.dept }} · {{ id }})</option>
            </select>
          </div>

          <div class="form-group">
            <label>{{ t.contactNameLabel }}</label>
            <input type="text" v-model="name" />
          </div>

          <div class="form-group">
            <label>{{ t.contactDeptLabel }}</label>
            <input type="text" v-model="dept" />
          </div>

          <button class="btn-save" @click="handleSaveContact">{{ t.saveContact }}</button>
        </div>

        <!-- Tab 2: Departments CRUD -->
        <div v-if="activeSubTab === 'depts'">
          <h3>部門名稱管理</h3>
          <div class="crud-header">
            <div class="add-row">
              <input type="text" v-model="newDept" placeholder="例如：資訊部、工程部" />
              <button class="btn-add" @click="handleAddDept">新增</button>
            </div>
            <div class="import-group">
              <button class="btn-link" @click="downloadDeptTemplate">下載 CSV 範本</button>
              <label class="btn-import-lbl">
                📥 導入 CSV
                <input type="file" accept=".csv" @change="handleImportDepts" style="display: none;" />
              </label>
            </div>
          </div>

          <div class="crud-list">
            <div v-for="(d, i) in departments" :key="d" class="crud-item">
              <div class="item-text" v-if="editingDeptIndex !== i">{{ d }}</div>
              <input v-else type="text" v-model="editingDeptText" class="edit-input" />

              <div class="item-actions">
                <button v-if="editingDeptIndex !== i" class="btn-edit" @click="handleStartEditDept(i)">✏</button>
                <button v-else class="btn-save-edit" @click="handleSaveEditDept(i)">✓</button>
                <button class="btn-delete" @click="handleDeleteDept(d)">✕</button>
              </div>
            </div>
          </div>
        </div>

        <!-- Tab 3: Purposes CRUD -->
        <div v-if="activeSubTab === 'purposes'">
          <h3>來訪事由管理</h3>
          <div class="crud-header">
            <div class="add-row">
              <input type="text" v-model="newPurpose" placeholder="例如：設備稽核、廠區拜訪" />
              <button class="btn-add" @click="handleAddPurpose">新增</button>
            </div>
          </div>

          <div class="crud-list">
            <div v-for="(p, i) in purposes" :key="p" class="crud-item">
              <div class="item-text" v-if="editingPurpIndex !== i">{{ p }}</div>
              <input v-else type="text" v-model="editingPurpText" class="edit-input" />

              <div class="item-actions">
                <button v-if="editingPurpIndex !== i" class="btn-edit" @click="handleStartEditPurp(i)">✏</button>
                <button v-else class="btn-save-edit" @click="handleSaveEditPurp(i)">✓</button>
                <button class="btn-delete" @click="handleDeletePurp(p)">✕</button>
              </div>
            </div>
          </div>
        </div>

        <!-- Tab 4: Employees CRUD -->
        <div v-if="activeSubTab === 'employees'">
          <div class="employees-header">
            <h3>{{ t.empSection }}</h3>
            <div class="actions-group">
              <button class="btn-add-emp" @click="startAddEmp">+ 新增員工</button>
              <button class="btn-link" @click="downloadEmpTemplate">下載 CSV 範本</button>
              <label class="btn-import-lbl">
                📥 導入 CSV
                <input type="file" accept=".csv" @change="handleImportEmployees" style="display: none;" />
              </label>
            </div>
          </div>

          <!-- Employee Form (Add/Edit) Overlay -->
          <div class="employee-inline-form" v-if="showEmpForm">
            <h4>{{ isEditingEmp ? '編輯員工檔案' : '新增員工檔案' }}</h4>
            <div class="inline-form-grid">
              <input type="text" v-model="empId" placeholder="工號 (例如: E008)" :disabled="isEditingEmp" />
              <input type="text" v-model="empName" placeholder="姓名" />
              <select v-model="empDept">
                <option v-for="d in departments" :key="d" :value="d">{{ d }}</option>
              </select>
            </div>
            <div class="inline-form-btns">
              <button class="btn-save-emp" @click="handleSaveEmployee">儲存</button>
              <button class="btn-cancel-emp" @click="showEmpForm = false">取消</button>
            </div>
          </div>

          <!-- Employees List Table -->
          <div class="emp-table-wrapper">
            <table>
              <thead>
                <tr>
                  <th>頭像</th>
                  <th>工號</th>
                  <th>姓名</th>
                  <th>部門</th>
                  <th>操作</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="(emp, id) in employees" :key="id">
                  <td>
                    <img :src="emp.avatar" alt="avatar" class="emp-avatar-circle" />
                  </td>
                  <td><span class="id-tag">{{ id }}</span></td>
                  <td><strong>{{ emp.name }}</strong></td>
                  <td>{{ emp.dept }}</td>
                  <td class="emp-row-actions">
                    <button class="btn-edit-emp" @click="startEditEmp(id, emp)">✏</button>
                    <button class="btn-delete-emp" @click="handleDeleteEmployee(id)">✕</button>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>

        <!-- Tab 5: Audit Logs -->
        <div v-if="activeSubTab === 'audit'" class="audit-logs-tab">
          <h3>系統操作稽核日誌</h3>
          <p class="section-desc">系統自動記錄所有涉及人員及門禁權限的設定變更、訪客進場與離場歷程。</p>

          <div class="audit-table-wrapper">
            <table>
              <thead>
                <tr>
                  <th>時間</th>
                  <th>分類</th>
                  <th>操作人</th>
                  <th>行為</th>
                  <th>詳細日誌</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="log in auditLogs" :key="log.id">
                  <td class="time-col">{{ formatTime(log.timestamp) }}</td>
                  <td><span class="log-cat-badge">{{ log.category }}</span></td>
                  <td><strong>{{ log.operator }}</strong></td>
                  <td><span class="log-action-badge">{{ log.action }}</span></td>
                  <td class="details-col">{{ log.details }}</td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>

      </div>

      <button class="btn-close-action" @click="emit('close')">{{ t.close }}</button>
    </div>

    <!-- Security PIN Lock Gate Screen -->
    <div class="pin-gate-box" v-else>
      <div class="gate-header">
        <h2>🔒 管理員密碼鎖</h2>
        <button class="btn-close" @click="emit('close')">✕</button>
      </div>
      <p class="gate-desc">變更系統核心部門、來訪事由、接洽設定、或是查看安全操作日誌需要驗證密碼。</p>

      <div class="form-group">
        <input type="password" v-model="pin" :placeholder="t.pinLabel" @keydown.enter="unlock" class="pin-input" />
        <p class="error-text" v-if="pinError">⚠ {{ pinError }}</p>
      </div>

      <div class="btn-row">
        <button class="btn-action-primary" @click="unlock">{{ t.confirm }}</button>
        <button class="btn-secondary" @click="emit('close')">{{ t.cancel }}</button>
      </div>
    </div>
  </div>
</template>

<style scoped>
.modal-backdrop {
  position: fixed;
  inset: 0;
  background: rgba(10, 20, 45, 0.65);
  backdrop-filter: blur(8px);
  z-index: 1000;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 16px;
}

/* Locked PIN screen */
.pin-gate-box {
  background: #ffffff;
  border: 1px solid #c8d8f0;
  border-radius: 20px;
  padding: 28px;
  width: 360px;
  color: #1a2d5a;
  box-shadow: 0 15px 45px rgba(10,20,60,0.3);
  box-sizing: border-box;
}

.gate-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 12px;
}

.gate-header h2 {
  margin: 0;
  font-size: 16px;
  font-weight: 800;
  color: #111827;
}

.gate-desc {
  font-size: 12px;
  color: #4b5563;
  line-height: 1.6;
  margin: 0 0 20px 0;
}

.pin-input {
  width: 100%;
  background: #fff;
  border: 1px solid #c8d8f0;
  border-radius: 10px;
  padding: 11px 14px;
  color: #111827;
  font-size: 14px;
  outline: none;
  box-sizing: border-box;
  text-align: center;
  letter-spacing: 2px;
}

.error-text {
  color: #ef4444;
  font-size: 11px;
  margin: 6px 0 0 0;
  text-align: center;
}

/* Unlocked Advanced Settings Panel */
.settings-modal-box {
  background: #ffffff;
  border: 1px solid #c8d8f0;
  border-radius: 24px;
  padding: 28px;
  width: 100%;
  max-width: 680px;
  color: #1a2d5a;
  max-height: 90vh;
  display: flex;
  flex-direction: column;
  box-shadow: 0 20px 50px rgba(10, 20, 60, 0.35);
  box-sizing: border-box;
}

.settings-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 18px;
  flex-shrink: 0;
}

.settings-header h2 {
  margin: 0;
  font-size: 18px;
  font-weight: 800;
  color: #111827;
}

.btn-close {
  background: none;
  border: none;
  color: #5a7298;
  font-size: 20px;
  cursor: pointer;
  padding: 0;
}

/* Tabs */
.settings-tabs {
  display: flex;
  gap: 6px;
  border-bottom: 1.5px solid #e5e7eb;
  padding-bottom: 1px;
  margin-bottom: 18px;
  overflow-x: auto;
  flex-shrink: 0;
}

.settings-tabs button {
  background: transparent;
  border: none;
  color: #6b7280;
  font-size: 13px;
  font-weight: 600;
  padding: 8px 14px;
  cursor: pointer;
  border-bottom: 2px solid transparent;
  white-space: nowrap;
}

.settings-tabs button.active {
  color: #3b82f6;
  border-bottom-color: #3b82f6;
}

.settings-tab-content {
  flex: 1;
  overflow-y: auto;
  padding-right: 4px;
}

.settings-tab-content h3 {
  margin: 0 0 6px 0;
  font-size: 14px;
  font-weight: 800;
  color: #111827;
}

.section-desc {
  font-size: 11px;
  color: #6b7280;
  margin: 0 0 16px 0;
}

/* Form Styles inside tabs */
.form-group {
  margin-bottom: 14px;
}

.form-group label {
  display: block;
  font-size: 12px;
  color: #4b5563;
  margin-bottom: 6px;
  font-weight: 600;
}

.form-group input[type="text"] {
  width: 100%;
  background: #fff;
  border: 1px solid #c8d8f0;
  border-radius: 8px;
  padding: 10px 12px;
  color: #111827;
  font-size: 13px;
  outline: none;
  box-sizing: border-box;
}

.select-field {
  width: 100%;
  background: #fff;
  border: 1px solid #c8d8f0;
  border-radius: 8px;
  padding: 10px 12px;
  color: #111827;
  font-size: 13px;
  outline: none;
  cursor: pointer;
  box-sizing: border-box;
}

.btn-save {
  background: linear-gradient(135deg, #2563eb, #3b82f6);
  border: none;
  color: #fff;
  border-radius: 8px;
  padding: 10px 20px;
  cursor: pointer;
  font-weight: 600;
  font-size: 13px;
  margin-top: 6px;
}

/* CRUD Lists */
.crud-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 14px;
  gap: 12px;
}

@media (max-width: 500px) {
  .crud-header {
    flex-direction: column;
    align-items: flex-start;
  }
}

.add-row {
  display: flex;
  gap: 8px;
  flex: 1;
}

.add-row input {
  flex: 1;
  background: #fff;
  border: 1px solid #c8d8f0;
  border-radius: 8px;
  padding: 8px 12px;
  font-size: 13px;
  color: #111827;
  outline: none;
}

.btn-add {
  background: #10b981;
  border: none;
  color: #fff;
  border-radius: 8px;
  padding: 8px 16px;
  font-size: 12px;
  font-weight: 600;
  cursor: pointer;
}

.import-group {
  display: flex;
  gap: 10px;
  align-items: center;
}

.btn-link {
  background: none;
  border: none;
  color: #4f46e5;
  font-size: 11px;
  cursor: pointer;
  text-decoration: underline;
  padding: 0;
}

.btn-import-lbl {
  background: #f3f4f6;
  border: 1px solid #d1d5db;
  color: #374151;
  padding: 6px 12px;
  border-radius: 8px;
  font-size: 11px;
  font-weight: 600;
  cursor: pointer;
}

.crud-list {
  display: flex;
  flex-direction: column;
  gap: 6px;
  max-height: 250px;
  overflow-y: auto;
}

.crud-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 8px 14px;
  background: #f9fafb;
  border: 1px solid #e5e7eb;
  border-radius: 8px;
}

.item-text {
  font-size: 13px;
  font-weight: 600;
  color: #374151;
}

.edit-input {
  background: #fff;
  border: 1px solid #c8d8f0;
  border-radius: 6px;
  padding: 4px 8px;
  font-size: 12px;
  color: #111827;
  outline: none;
}

.item-actions {
  display: flex;
  gap: 8px;
}

.btn-edit, .btn-save-edit {
  background: transparent;
  border: none;
  color: #4b5563;
  cursor: pointer;
  font-size: 14px;
  padding: 2px;
}

.btn-delete {
  background: transparent;
  border: none;
  color: #ef4444;
  cursor: pointer;
  font-size: 14px;
  padding: 2px;
}

/* Employees List CRUD Styles */
.employees-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 14px;
}

@media (max-width: 500px) {
  .employees-header {
    flex-direction: column;
    align-items: flex-start;
    gap: 8px;
  }
}

.actions-group {
  display: flex;
  gap: 10px;
  align-items: center;
}

.btn-add-emp {
  background: #4f46e5;
  border: none;
  color: #fff;
  padding: 6px 12px;
  border-radius: 8px;
  font-size: 11px;
  font-weight: 600;
  cursor: pointer;
}

.employee-inline-form {
  background: #f0f4ff;
  border: 1px solid #c8d8f0;
  border-radius: 12px;
  padding: 12px 16px;
  margin-bottom: 14px;
}

.employee-inline-form h4 {
  margin: 0 0 8px 0;
  font-size: 12px;
  font-weight: 700;
  color: #1d4ed8;
}

.inline-form-grid {
  display: grid;
  grid-template-columns: 1fr 1fr 1fr;
  gap: 8px;
  margin-bottom: 10px;
}

@media (max-width: 500px) {
  .inline-form-grid {
    grid-template-columns: 1fr;
  }
}

.inline-form-grid input, .inline-form-grid select {
  background: #fff;
  border: 1px solid #c8d8f0;
  border-radius: 6px;
  padding: 6px 10px;
  font-size: 12px;
  color: #111827;
  outline: none;
}

.inline-form-btns {
  display: flex;
  gap: 8px;
}

.btn-save-emp {
  background: #10b981;
  border: none;
  color: #fff;
  border-radius: 6px;
  padding: 5px 12px;
  font-size: 11px;
  font-weight: 600;
  cursor: pointer;
}

.btn-cancel-emp {
  background: #e5e7eb;
  border: none;
  color: #374151;
  border-radius: 6px;
  padding: 5px 12px;
  font-size: 11px;
  cursor: pointer;
}

.emp-table-wrapper {
  max-height: 250px;
  overflow-y: auto;
  border: 1px solid #e5e7eb;
  border-radius: 12px;
}

.emp-table-wrapper table {
  width: 100%;
  border-collapse: collapse;
  font-size: 12px;
}

.emp-table-wrapper th {
  padding: 8px 12px;
  font-size: 10px;
}

.emp-table-wrapper td {
  padding: 8px 12px;
}

.emp-avatar-circle {
  width: 24px;
  height: 24px;
  border-radius: 50%;
  object-fit: contain;
  background: #1e293b;
}

.id-tag {
  background: #f3f4f6;
  color: #4b5563;
  padding: 2px 6px;
  border-radius: 4px;
  font-size: 9px;
  font-weight: 600;
}

.emp-row-actions button {
  background: transparent;
  border: none;
  cursor: pointer;
  margin-right: 6px;
  padding: 2px;
}

.btn-delete-emp {
  color: #ef4444;
}

/* Audit Logs Tab */
.audit-logs-tab .section-desc {
  margin-bottom: 12px;
}

.audit-table-wrapper {
  max-height: 280px;
  overflow-y: auto;
  border: 1px solid #e5e7eb;
  border-radius: 12px;
}

.audit-table-wrapper table {
  width: 100%;
  border-collapse: collapse;
  font-size: 11px;
}

.audit-table-wrapper th {
  padding: 8px 12px;
  background: #f9fafb;
}

.audit-table-wrapper td {
  padding: 8px 12px;
  color: #374151;
}

.time-col {
  color: #6b7280;
  white-space: nowrap;
}

.log-cat-badge {
  background: #f3f4f6;
  padding: 2px 6px;
  border-radius: 6px;
  font-weight: 600;
}

.log-action-badge {
  background: #e0f2fe;
  color: #0369a1;
  padding: 2px 6px;
  border-radius: 6px;
  font-weight: 700;
}

.details-col {
  max-width: 250px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

/* Modal Actions Footer */
.btn-close-action {
  width: 100%;
  background: #f3f4f6;
  border: 1px solid #d1d5db;
  color: #4b5563;
  border-radius: 10px;
  padding: 11px 0;
  cursor: pointer;
  font-weight: 600;
  font-size: 13px;
  margin-top: 18px;
  flex-shrink: 0;
  transition: all 0.2s;
}
.btn-close-action:hover {
  background: #e5e7eb;
}

.btn-row {
  display: flex;
  gap: 10px;
  margin-top: 20px;
}

.btn-secondary {
  flex: 1;
  background: #f3f4f6;
  border: 1px solid #d1d5db;
  color: #4b5563;
  border-radius: 10px;
  padding: 11px 0;
  cursor: pointer;
  font-weight: 600;
  font-size: 13px;
}

.btn-action-primary {
  flex: 1;
  background: linear-gradient(135deg, #2563eb, #3b82f6);
  border: none;
  color: #fff;
  border-radius: 10px;
  padding: 11px 0;
  cursor: pointer;
  font-weight: 700;
  font-size: 13px;
}
</style>
