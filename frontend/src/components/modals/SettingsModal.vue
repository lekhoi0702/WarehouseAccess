<script setup>
import { ref, reactive, computed, onMounted } from 'vue';
import { useRecords } from '../../composables/useRecords';

const props = defineProps({
  t: Object
});

const emit = defineEmits(['close']);

const {
  records,
  departments,
  purposes,
  employees,
  auditLogs,
  defaultContact,
  refreshData,
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
  deleteEmployee
} = useRecords();

const activeSubTab = ref("contact"); // "contact", "depts", "purposes", "employees", "audit"
const contactName = ref("");
const contactDept = ref("");

// prefills contact states from default contact prop
onMounted(() => {
  contactName.value = defaultContact.value.name;
  contactDept.value = defaultContact.value.dept;
});

// Find if current contact matches any existing employee ID
const matchedEmp = computed(() => {
  return Object.entries(employees.value).find(([id, emp]) => emp.name === contactName.value && emp.dept === contactDept.value);
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
    const res = await saveContact(contactName.value, contactDept.value);
    if (res.success) {
      alert(props.t.contactSaved);
    }
  } catch (e) {
    alert("儲存失敗");
  }
}

function handleEmployeeChange(e) {
  const empIdVal = e.target.value;
  const emp = employees.value[empIdVal];
  if (emp) {
    contactName.value = emp.name;
    contactDept.value = emp.dept;
    selectedEmpId.value = empIdVal;
  } else {
    selectedEmpId.value = "";
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
        if (cleanVal && !departments.value.includes(cleanVal) && !importedDepts.includes(cleanVal)) {
          importedDepts.push(cleanVal);
        }
      }

      if (importedDepts.length === 0) {
        alert("沒有可導入的新部門資料。");
        return;
      }

      const res = await importDepartments(importedDepts);
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

      const res = await importEmployees(importedEmployees);
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
  if (departments.value.includes(text)) {
    alert("部門已存在！");
    return;
  }
  try {
    await addDepartment(text);
    newDept.value = "";
  } catch (e) {
    alert("新增部門失敗");
  }
}

function handleStartEditDept(index) {
  editingDeptIndex.value = index;
  editingDeptText.value = departments.value[index];
}

async function handleSaveEditDept(index) {
  const text = editingDeptText.value.trim();
  if (!text) return;
  const oldName = departments.value[index];
  if (departments.value.includes(text) && oldName !== text) {
    alert("部門已存在！");
    return;
  }
  try {
    await editDepartment(oldName, text);
    editingDeptIndex.value = -1;
  } catch (e) {
    alert("編輯部門失敗");
  }
}

async function handleDeleteDept(nameVal) {
  if (!confirm(`確定要刪除部門 [${nameVal}] 嗎？`)) return;
  try {
    await deleteDepartment(nameVal);
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
  if (purposes.value.includes(text)) {
    alert("來訪事由已存在！");
    return;
  }
  try {
    await addPurpose(text);
    newPurpose.value = "";
  } catch (e) {
    alert("新增事由失敗");
  }
}

function handleStartEditPurp(index) {
  editingPurpIndex.value = index;
  editingPurpText.value = purposes.value[index];
}

async function handleSaveEditPurp(index) {
  const text = editingPurpText.value.trim();
  if (!text) return;
  const oldName = purposes.value[index];
  if (purposes.value.includes(text) && oldName !== text) {
    alert("來訪事由已存在！");
    return;
  }
  try {
    await editPurpose(oldName, text);
    editingPurpIndex.value = -1;
  } catch (e) {
    alert("編輯事由失敗");
  }
}

async function handleDeletePurp(nameVal) {
  if (!confirm(`確定要刪除事由 [${nameVal}] 嗎？`)) return;
  try {
    await deletePurpose(nameVal);
  } catch (e) {
    alert("刪除事由失敗");
  }
}

// ─── Employee CRUD ───────────────────────────────────────────────────────────
const empId = ref("");
const empName = ref("");
const empDept = ref("");
const isEditingEmp = ref(false);
const showEmpForm = ref(false);

function startAddEmp() {
  empId.value = "";
  empName.value = "";
  empDept.value = departments.value[0] || "";
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

    if (isEditingEmp.value) {
      await editEmployee(idVal, nameVal, deptVal, avatar);
    } else {
      await addEmployee(idVal, nameVal, deptVal, avatar);
    }
    showEmpForm.value = false;
  } catch (e) {
    alert("儲存員工失敗");
  }
}

async function handleDeleteEmployee(idVal) {
  if (!confirm(`確定要刪除員工 [${employees.value[idVal].name}] (工號: ${idVal}) 嗎？`)) return;
  try {
    await deleteEmployee(idVal);
  } catch (e) {
    alert("刪除員工失敗");
  }
}

function formatTime(ts) {
  if (!ts) return "—";
  const d = new Date(ts);
  return d.toLocaleString("zh-TW", { hour12: false, month: "2-digit", day: "2-digit", hour: "2-digit", minute: "2-digit", second: "2-digit" });
}

const SUB_TABS = [
  { id: 'contact', label: '接洽人員' },
  { id: 'depts', label: '部門維護' },
  { id: 'purposes', label: '事由維護' },
  { id: 'employees', label: '員工名冊' },
  { id: 'audit', label: '安全日誌' }
];
</script>

<template>
  <div class="fixed inset-0 bg-[#0a142d]/65 backdrop-blur-md z-[8000] flex items-center justify-center p-4">
    <!-- Unlocked Settings Container -->
    <div class="bg-white border border-slate-200 rounded-3xl p-7 w-full max-w-2xl text-slate-800 shadow-2xl max-h-[90vh] flex flex-col" v-if="isAdminUnlocked">
      <!-- Header -->
      <div class="flex justify-between items-center mb-4 shrink-0">
        <h2 class="text-base font-extrabold text-slate-900 m-0">{{ t.settingsTitle }}</h2>
        <button class="text-slate-400 hover:text-slate-600 text-lg transition" @click="emit('close')">✕</button>
      </div>

      <!-- Settings Sub Navigation Tabs -->
      <div class="flex gap-1 border-b border-slate-100 mb-5 overflow-x-auto shrink-0">
        <button 
          v-for="tab in SUB_TABS"
          :key="tab.id"
          :class="[
            'px-4 py-2 text-xs font-semibold hover:text-slate-800 transition border-b-2 whitespace-nowrap',
            activeSubTab === tab.id ? 'border-primary text-primary font-bold' : 'border-transparent text-slate-400'
          ]"
          @click="activeSubTab = tab.id"
        >
          {{ tab.label }}
        </button>
      </div>

      <!-- Tab Content Frame -->
      <div class="flex-1 overflow-y-auto pr-1">
        
        <!-- Tab 1: Contact Settings -->
        <div v-if="activeSubTab === 'contact'" class="space-y-4">
          <div class="text-left select-none">
            <h3 class="text-sm font-extrabold text-slate-900 m-0">{{ t.contactSection }}</h3>
            <p class="text-[11px] text-slate-400 font-medium mt-0.5">設定此系統預設對外的單一接洽聯絡窗口。</p>
          </div>

          <div class="flex flex-col gap-1.5 text-left">
            <label class="text-xs font-bold text-slate-500">快速選擇內部員工代入</label>
            <select 
              :value="selectedEmpId" 
              @change="handleEmployeeChange" 
              class="w-full bg-white border border-slate-200 rounded-xl px-4 py-2.5 text-xs outline-none cursor-pointer focus:border-primary/50"
            >
              <option value="">-- 手動輸入接洽人員 --</option>
              <option v-for="(emp, id) in employees" :key="id" :value="id">{{ emp.name }} ({{ emp.dept }} · {{ id }})</option>
            </select>
          </div>

          <div class="flex flex-col gap-1.5 text-left">
            <label class="text-xs font-bold text-slate-500">{{ t.contactNameLabel }}</label>
            <input 
              type="text" 
              v-model="contactName" 
              class="w-full bg-white border border-slate-200 rounded-xl px-4 py-2.5 text-xs outline-none focus:border-primary/50"
            />
          </div>

          <div class="flex flex-col gap-1.5 text-left">
            <label class="text-xs font-bold text-slate-500">{{ t.contactDeptLabel }}</label>
            <input 
              type="text" 
              v-model="contactDept" 
              class="w-full bg-white border border-slate-200 rounded-xl px-4 py-2.5 text-xs outline-none focus:border-primary/50"
            />
          </div>

          <button 
            class="bg-gradient-to-r from-primary to-primary-light text-white text-xs font-bold px-5 py-2.5 rounded-lg active:scale-95 transition shadow-md shadow-primary/10 mt-2" 
            @click="handleSaveContact"
          >
            {{ t.saveContact }}
          </button>
        </div>

        <!-- Tab 2: Departments CRUD -->
        <div v-if="activeSubTab === 'depts'" class="space-y-4 text-left">
          <div class="select-none">
            <h3 class="text-sm font-extrabold text-slate-900 m-0">部門名稱管理</h3>
          </div>
          
          <div class="flex flex-col sm:flex-row gap-3 items-start sm:items-center justify-between bg-slate-50 p-4 border border-slate-200/50 rounded-2xl">
            <!-- Add Dept -->
            <div class="flex gap-2 w-full sm:max-w-xs">
              <input 
                type="text" 
                v-model="newDept" 
                placeholder="例如：資訊部、工程部" 
                class="flex-1 bg-white border border-slate-200 rounded-lg px-3 py-1.5 text-xs outline-none focus:border-primary/50"
              />
              <button 
                class="bg-primary text-white text-xs font-bold px-4 py-1.5 rounded-lg hover:bg-primary-dark transition active:scale-95"
                @click="handleAddDept"
              >
                新增
              </button>
            </div>
            
            <!-- Import Template -->
            <div class="flex items-center gap-3 w-full sm:w-auto justify-end">
              <button class="text-primary text-xs font-bold hover:underline" @click="downloadDeptTemplate">下載 CSV 範本</button>
              <label class="bg-emerald-50 text-emerald-600 border border-emerald-200 hover:bg-emerald-100 cursor-pointer text-xs font-bold px-3 py-1.5 rounded-lg transition active:scale-95 flex items-center gap-1 shadow-sm">
                📥 導入 CSV
                <input type="file" accept=".csv" @change="handleImportDepts" class="hidden" />
              </label>
            </div>
          </div>

          <!-- Departments List Grid -->
          <div class="grid grid-cols-1 sm:grid-cols-2 gap-2 mt-4 max-h-[30vh] overflow-y-auto pr-1">
            <div 
              v-for="(d, i) in departments" 
              :key="d" 
              class="border border-slate-100 rounded-xl p-3 bg-slate-50/30 hover:bg-slate-50 flex items-center justify-between transition"
            >
              <div class="text-xs font-bold text-slate-700 truncate" v-if="editingDeptIndex !== i">{{ d }}</div>
              <input 
                v-else 
                type="text" 
                v-model="editingDeptText" 
                class="flex-1 max-w-[150px] bg-white border border-slate-200 rounded px-2 py-0.5 text-xs outline-none"
              />

              <div class="flex gap-1.5 shrink-0">
                <button 
                  v-if="editingDeptIndex !== i" 
                  class="text-indigo-500 hover:text-indigo-700 hover:bg-indigo-50 p-1.5 rounded-lg transition" 
                  @click="handleStartEditDept(i)"
                >
                  ✏
                </button>
                <button 
                  v-else 
                  class="text-emerald-500 hover:text-emerald-700 hover:bg-emerald-50 p-1.5 rounded-lg transition font-bold" 
                  @click="handleSaveEditDept(i)"
                >
                  ✓
                </button>
                <button 
                  class="text-red-400 hover:text-red-600 hover:bg-red-50 p-1.5 rounded-lg transition" 
                  @click="handleDeleteDept(d)"
                >
                  ✕
                </button>
              </div>
            </div>
          </div>
        </div>

        <!-- Tab 3: Purposes CRUD -->
        <div v-if="activeSubTab === 'purposes'" class="space-y-4 text-left">
          <div class="select-none">
            <h3 class="text-sm font-extrabold text-slate-900 m-0">來訪事由管理</h3>
          </div>
          
          <div class="bg-slate-50 p-4 border border-slate-200/50 rounded-2xl">
            <!-- Add Purpose -->
            <div class="flex gap-2 max-w-xs">
              <input 
                type="text" 
                v-model="newPurpose" 
                placeholder="例如：設備稽核、廠區拜訪" 
                class="flex-1 bg-white border border-slate-200 rounded-lg px-3 py-1.5 text-xs outline-none focus:border-primary/50"
              />
              <button 
                class="bg-primary text-white text-xs font-bold px-4 py-1.5 rounded-lg hover:bg-primary-dark transition active:scale-95"
                @click="handleAddPurpose"
              >
                新增
              </button>
            </div>
          </div>

          <!-- Purposes List -->
          <div class="grid grid-cols-1 sm:grid-cols-2 gap-2 mt-4 max-h-[30vh] overflow-y-auto pr-1">
            <div 
              v-for="(p, i) in purposes" 
              :key="p" 
              class="border border-slate-100 rounded-xl p-3 bg-slate-50/30 hover:bg-slate-50 flex items-center justify-between transition"
            >
              <div class="text-xs font-bold text-slate-700 truncate" v-if="editingPurpIndex !== i">{{ p }}</div>
              <input 
                v-else 
                type="text" 
                v-model="editingPurpText" 
                class="flex-1 max-w-[150px] bg-white border border-slate-200 rounded px-2 py-0.5 text-xs outline-none"
              />

              <div class="flex gap-1.5 shrink-0">
                <button 
                  v-if="editingPurpIndex !== i" 
                  class="text-indigo-500 hover:text-indigo-700 hover:bg-indigo-50 p-1.5 rounded-lg transition" 
                  @click="handleStartEditPurp(i)"
                >
                  ✏
                </button>
                <button 
                  v-else 
                  class="text-emerald-500 hover:text-emerald-700 hover:bg-emerald-50 p-1.5 rounded-lg transition font-bold" 
                  @click="handleSaveEditPurp(i)"
                >
                  ✓
                </button>
                <button 
                  class="text-red-400 hover:text-red-600 hover:bg-red-50 p-1.5 rounded-lg transition" 
                  @click="handleDeletePurp(p)"
                >
                  ✕
                </button>
              </div>
            </div>
          </div>
        </div>

        <!-- Tab 4: Employees CRUD -->
        <div v-if="activeSubTab === 'employees'" class="space-y-4 text-left">
          <div class="flex flex-col sm:flex-row gap-3 justify-between items-start sm:items-center">
            <h3 class="text-sm font-extrabold text-slate-900 m-0">{{ t.empSection }}</h3>
            
            <div class="flex items-center gap-3 w-full sm:w-auto justify-end">
              <button 
                class="bg-primary text-white text-xs font-bold px-3 py-1.5 rounded-lg hover:bg-primary-dark transition active:scale-95"
                @click="startAddEmp"
              >
                + 新增員工
              </button>
              <button class="text-primary text-xs font-bold hover:underline" @click="downloadEmpTemplate">下載 CSV 範本</button>
              <label class="bg-emerald-50 text-emerald-600 border border-emerald-200 hover:bg-emerald-100 cursor-pointer text-xs font-bold px-3 py-1.5 rounded-lg transition active:scale-95 flex items-center gap-1 shadow-sm">
                📥 導入 CSV
                <input type="file" accept=".csv" @change="handleImportEmployees" class="hidden" />
              </label>
            </div>
          </div>

          <!-- Employee Inline Form (overlay) -->
          <div 
            v-if="showEmpForm" 
            class="bg-indigo-50/50 border border-indigo-100 rounded-2xl p-4 space-y-3"
          >
            <h4 class="text-xs font-extrabold text-indigo-700 m-0">{{ isEditingEmp ? '編輯員工檔案' : '新增員工檔案' }}</h4>
            <div class="grid grid-cols-1 sm:grid-cols-3 gap-3">
              <input 
                type="text" 
                v-model="empId" 
                placeholder="工號 (例如: E008)" 
                :disabled="isEditingEmp" 
                class="bg-white border border-slate-200 rounded-lg px-3 py-1.5 text-xs outline-none focus:border-primary/50 disabled:opacity-50"
              />
              <input 
                type="text" 
                v-model="empName" 
                placeholder="姓名" 
                class="bg-white border border-slate-200 rounded-lg px-3 py-1.5 text-xs outline-none focus:border-primary/50"
              />
              <select 
                v-model="empDept"
                class="bg-white border border-slate-200 rounded-lg px-3 py-1.5 text-xs outline-none cursor-pointer focus:border-primary/50"
              >
                <option v-for="d in departments" :key="d" :value="d">{{ d }}</option>
              </select>
            </div>
            <div class="flex justify-end gap-2">
              <button 
                class="bg-white hover:bg-slate-50 border border-slate-200 text-slate-500 text-[10px] font-bold px-4 py-1.5 rounded-md transition" 
                @click="showEmpForm = false"
              >
                取消
              </button>
              <button 
                class="bg-primary text-white text-[10px] font-bold px-4 py-1.5 rounded-md hover:bg-primary-dark transition" 
                @click="handleSaveEmployee"
              >
                儲存
              </button>
            </div>
          </div>

          <!-- Employees table list -->
          <div class="border border-slate-200/80 rounded-2xl overflow-hidden max-h-[35vh] overflow-y-auto shadow-inner bg-slate-50/10">
            <table class="w-full text-left border-collapse">
              <thead>
                <tr class="bg-slate-50 border-b border-slate-100 text-[10px] font-bold text-slate-500 uppercase tracking-wider">
                  <th class="p-3">頭像</th>
                  <th class="p-3">工號</th>
                  <th class="p-3">姓名</th>
                  <th class="p-3">部門</th>
                  <th class="p-3">操作</th>
                </tr>
              </thead>
              <tbody class="divide-y divide-slate-100 text-xs">
                <tr v-for="(emp, id) in employees" :key="id" class="hover:bg-slate-50/30 transition">
                  <td class="p-3">
                    <img :src="emp.avatar" alt="avatar" class="w-7 h-7 rounded-full object-cover border border-slate-200 shadow-sm" />
                  </td>
                  <td class="p-3"><span class="font-mono font-bold text-slate-500">{{ id }}</span></td>
                  <td class="p-3"><strong class="text-slate-800">{{ emp.name }}</strong></td>
                  <td class="p-3 text-slate-500 font-semibold">{{ emp.dept }}</td>
                  <td class="p-3">
                    <div class="flex gap-1">
                      <button class="text-indigo-500 hover:text-indigo-700 hover:bg-indigo-50 p-1 rounded transition" @click="startEditEmp(id, emp)">✏</button>
                      <button class="text-red-400 hover:text-red-600 hover:bg-red-50 p-1 rounded transition" @click="handleDeleteEmployee(id)">✕</button>
                    </div>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>

        <!-- Tab 5: Audit Logs -->
        <div v-if="activeSubTab === 'audit'" class="space-y-4 text-left">
          <div class="select-none">
            <h3 class="text-sm font-extrabold text-slate-900 m-0">系統操作稽核日誌</h3>
            <p class="text-[11px] text-slate-400 font-medium mt-0.5">系統自動記錄所有涉及人員及門禁權限的設定變更、訪客進場與離場歷程。</p>
          </div>

          <div class="border border-slate-200/80 rounded-2xl overflow-hidden max-h-[40vh] overflow-y-auto bg-slate-50/10 shadow-inner">
            <table class="w-full text-left border-collapse">
              <thead>
                <tr class="bg-slate-50 border-b border-slate-100 text-[10px] font-bold text-slate-500 uppercase tracking-wider">
                  <th class="p-3">時間</th>
                  <th class="p-3">分類</th>
                  <th class="p-3">操作人</th>
                  <th class="p-3">行為</th>
                  <th class="p-3">詳細日誌</th>
                </tr>
              </thead>
              <tbody class="divide-y divide-slate-100 text-[11px] text-slate-600 font-medium">
                <tr v-for="log in auditLogs" :key="log.id" class="hover:bg-slate-50/30 transition">
                  <td class="p-3 text-slate-400 whitespace-nowrap">{{ formatTime(log.timestamp) }}</td>
                  <td class="p-3"><span class="px-2 py-0.5 bg-slate-100 text-slate-600 rounded-md font-bold uppercase text-[9px]">{{ log.category }}</span></td>
                  <td class="p-3"><strong class="text-slate-800">{{ log.operator }}</strong></td>
                  <td class="p-3"><span class="px-2 py-0.5 bg-indigo-50 text-indigo-600 rounded-md font-bold text-[9px]">{{ log.action }}</span></td>
                  <td class="p-3 text-slate-500 max-w-[200px] truncate" :title="log.details">{{ log.details }}</td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>

      </div>

      <!-- Close Action -->
      <button 
        class="w-full bg-slate-100 hover:bg-slate-200/80 border border-slate-200 text-slate-600 text-xs font-bold py-2.5 rounded-xl transition duration-150 active:scale-[0.98] shrink-0 mt-5"
        @click="emit('close')"
      >
        {{ t.close }}
      </button>
    </div>

    <!-- Security PIN Lock Gate Screen -->
    <div class="bg-white border border-slate-200 rounded-3xl p-7 w-full max-w-sm text-slate-800 shadow-2xl" v-else>
      <div class="flex justify-between items-center mb-3">
        <h2 class="text-sm font-extrabold text-slate-900 m-0">🔒 管理員密碼鎖</h2>
        <button class="text-slate-400 hover:text-slate-600 text-lg transition" @click="emit('close')">✕</button>
      </div>
      <p class="text-xs leading-relaxed text-slate-500 m-0 mb-6">變更系統核心部門、來訪事由、接洽設定、或是查看安全操作日誌需要驗證密碼。</p>

      <div class="flex flex-col gap-1.5 text-left mb-6">
        <input 
          type="password" 
          v-model="pin" 
          :placeholder="t.pinLabel" 
          @keydown.enter="unlock" 
          class="w-full bg-white border border-slate-200 rounded-xl px-4 py-3 text-sm text-center outline-none tracking-widest focus:border-primary/50"
        />
        <p class="text-xs text-red-500 font-semibold text-center mt-1.5" v-if="pinError">⚠ {{ pinError }}</p>
      </div>

      <div class="flex gap-3">
        <button 
          class="flex-1 bg-slate-100 hover:bg-slate-200 border border-slate-200 text-slate-600 text-xs font-bold py-3 rounded-xl active:scale-[0.98] transition"
          @click="emit('close')"
        >
          {{ t.cancel }}
        </button>
        <button 
          class="flex-[2] bg-gradient-to-r from-primary to-primary-light text-white text-xs font-bold py-3 rounded-xl shadow-lg hover:shadow-primary/20 active:scale-[0.98] transition"
          @click="unlock"
        >
          {{ t.confirm }}
        </button>
      </div>
    </div>
  </div>
</template>
