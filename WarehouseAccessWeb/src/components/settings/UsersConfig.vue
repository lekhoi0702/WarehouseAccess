<script setup>
import { ref, reactive, onMounted, watch } from 'vue';
import { useI18n } from '../../composables/useI18n';
import { useRecords } from '../../composables/useRecords';
import { useToast } from '../../composables/useToast';

const { t } = useI18n();
const {
  userListItems,
  userDepartmentOptions,
  usersLoading,
  usersErrorMessage,
  usersTotal,
  usersPage,
  usersPageSize,
  loadUserDepartments,
  loadUsersList,
  saveUser,
  removeUser,
  importUsers,
  downloadUsersTemplate
} = useRecords();

const { showToast } = useToast();

const keyword = ref('');
const deptFilter = ref('');

const userFormMode = ref('create'); // 'create' | 'edit'
const formState = reactive({ userCode: '', cardNumber: '', fullName: '', deptCode: '' });
const formErrors = reactive({ userCode: '', fullName: '', deptCode: '' });

const importingUsers = ref(false);
const importResult = ref(null);

onMounted(async () => {
  await loadUserDepartments();
  await loadUsersList();
});

// Watch page and filter settings to reload
watch([usersPage, deptFilter], async () => {
  await loadUsersList(keyword.value, deptFilter.value);
});

function applyFilter() {
  usersPage.value = 1;
  loadUsersList(keyword.value, deptFilter.value);
}

function resetUserForm() {
  userFormMode.value = 'create';
  formState.userCode = '';
  formState.cardNumber = '';
  formState.fullName = '';
  formState.deptCode = '';
  formErrors.userCode = '';
  formErrors.fullName = '';
  formErrors.deptCode = '';
}

function validate() {
  formErrors.userCode = formState.userCode.trim() ? '' : 'Required';
  formErrors.fullName = formState.fullName.trim() ? '' : 'Required';
  formErrors.deptCode = formState.deptCode ? '' : 'Required';
  return !formErrors.userCode && !formErrors.fullName && !formErrors.deptCode;
}

async function onSubmit() {
  if (!validate()) return;
  
  const payload = {
    userCode: formState.userCode.trim(),
    cardNumber: formState.cardNumber ? formState.cardNumber.trim() : null,
    fullName: formState.fullName.trim(),
    deptCode: formState.deptCode
  };

  try {
    const res = await saveUser(payload, userFormMode.value === 'edit');
    if (res?.success) {
      showToast(userFormMode.value === 'edit' ? 'User updated successfully' : 'User created successfully');
      resetUserForm();
      await loadUsersList(keyword.value, deptFilter.value);
    } else {
      alert(res?.message || 'Save user failed');
    }
  } catch (e) {
    alert('Save failed');
  }
}

function editItem(item) {
  userFormMode.value = 'edit';
  formState.userCode = item.userCode || '';
  formState.cardNumber = item.cardNumber || '';
  formState.fullName = item.fullName || '';
  formState.deptCode = item.deptCode || '';
}

async function deleteItem(item) {
  if (!confirm(`Are you sure you want to delete user ${item.fullName}?`)) return;
  try {
    const res = await removeUser(item.userCode);
    if (res?.success) {
      showToast('User deleted successfully');
      await loadUsersList(keyword.value, deptFilter.value);
    } else {
      alert(res?.message || 'Delete user failed');
    }
  } catch (e) {
    alert('Delete failed');
  }
}

async function onFileImport(event) {
  const file = event?.target?.files?.[0];
  if (!file) return;

  importingUsers.value = true;
  importResult.value = null;
  
  try {
    const res = await importUsers(file);
    if (res?.success) {
      importResult.value = res.data;
      showToast('Spreadsheet imported successfully');
      await loadUsersList(keyword.value, deptFilter.value);
    } else {
      alert(res?.message || 'Import failed');
    }
  } catch (e) {
    alert('Import failed');
  } finally {
    importingUsers.value = false;
    event.target.value = '';
  }
}

async function handleDownloadTemplate() {
  try {
    const res = await downloadUsersTemplate();
    if (!res.success) {
      alert(res.message);
    }
  } catch (e) {
    alert("Template download failed");
  }
}

function formatDateTime(value) {
  if (!value) return '-'
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return '-'
  return date.toLocaleString()
}
</script>

<template>
  <div class="space-y-6 text-left">
    <div class="grid grid-cols-1 lg:grid-cols-2 gap-6">
      
      <!-- Users Edit Form -->
      <div class="bg-slate-50 border border-slate-200/60 rounded-2xl p-5 space-y-4">
        <h3 class="text-sm font-extrabold text-slate-800 m-0">{{ userFormMode === 'create' ? 'Add User' : 'Edit User' }}</h3>
        
        <div class="space-y-3">
          <div class="flex flex-col gap-1 text-left">
            <label class="text-xs font-bold text-slate-500">User Code</label>
            <input 
              type="text" 
              v-model="formState.userCode" 
              :disabled="userFormMode === 'edit'"
              class="w-full bg-white border border-slate-200 rounded-xl px-4 py-2.5 text-xs outline-none focus:border-primary/50 disabled:opacity-50"
            />
            <p v-if="formErrors.userCode" class="text-[10px] text-red-500 font-semibold pl-1">{{ formErrors.userCode }}</p>
          </div>

          <div class="flex flex-col gap-1 text-left">
            <label class="text-xs font-bold text-slate-500">Card Number</label>
            <input 
              type="text" 
              v-model="formState.cardNumber"
              class="w-full bg-white border border-slate-200 rounded-xl px-4 py-2.5 text-xs outline-none focus:border-primary/50"
            />
          </div>

          <div class="flex flex-col gap-1 text-left">
            <label class="text-xs font-bold text-slate-500">Full Name</label>
            <input 
              type="text" 
              v-model="formState.fullName"
              class="w-full bg-white border border-slate-200 rounded-xl px-4 py-2.5 text-xs outline-none focus:border-primary/50"
            />
            <p v-if="formErrors.fullName" class="text-[10px] text-red-500 font-semibold pl-1">{{ formErrors.fullName }}</p>
          </div>

          <div class="flex flex-col gap-1 text-left">
            <label class="text-xs font-bold text-slate-500">Department</label>
            <select 
              v-model="formState.deptCode"
              class="w-full bg-white border border-slate-200 rounded-xl px-4 py-2.5 text-xs outline-none cursor-pointer focus:border-primary/50"
            >
              <option value="">Select department...</option>
              <option v-for="department in userDepartmentOptions" :key="department.deptCode" :value="department.deptCode">
                {{ department.deptName }}
              </option>
            </select>
            <p v-if="formErrors.deptCode" class="text-[10px] text-red-500 font-semibold pl-1">{{ formErrors.deptCode }}</p>
          </div>
        </div>

        <div class="flex gap-2 pt-2">
          <button 
            class="bg-primary text-white text-xs font-bold px-4 py-2.5 rounded-lg hover:bg-primary-dark transition active:scale-95 shadow-md shadow-primary/10"
            @click="onSubmit"
          >
            {{ userFormMode === 'create' ? 'Add User' : 'Save Changes' }}
          </button>
          <button 
            class="bg-white hover:bg-slate-50 border border-slate-200 text-slate-500 text-xs font-bold px-4 py-2.5 rounded-lg transition active:scale-95"
            @click="resetUserForm"
          >
            Reset
          </button>
        </div>
      </div>

      <!-- Users Excel Import Card -->
      <div class="bg-slate-50 border border-slate-200/60 rounded-2xl p-5 flex flex-col justify-between space-y-4">
        <div class="space-y-2">
          <h3 class="text-sm font-extrabold text-slate-800 m-0">Import Users (.xlsx)</h3>
          <p class="text-[11px] text-slate-500 font-medium leading-relaxed">
            Upload your user list using the excel layout structure. Required columns: <code class="bg-white border px-1 rounded font-bold text-slate-600">UserCode</code>, <code class="bg-white border px-1 rounded font-bold text-slate-600">FullName</code>, <code class="bg-white border px-1 rounded font-bold text-slate-600">DeptCode</code>. Optional: <code class="bg-white border px-1 rounded font-bold text-slate-600">CardNumber</code>.
          </p>
        </div>

        <div class="space-y-4">
          <!-- Template Download -->
          <div class="flex">
            <button 
              class="text-primary hover:underline text-xs font-bold flex items-center gap-1"
              @click="handleDownloadTemplate"
            >
              📥 Download Excel Template
            </button>
          </div>

          <!-- File Upload -->
          <div class="relative">
            <input 
              type="file" 
              accept=".xlsx" 
              @change="onFileImport" 
              :disabled="importingUsers"
              class="block w-full text-xs text-slate-500 file:mr-4 file:py-2 file:px-4 file:rounded-xl file:border-0 file:text-xs file:font-bold file:bg-primary/10 file:text-primary hover:file:bg-primary/20 cursor-pointer disabled:opacity-5"
            />
          </div>

          <!-- Import Feedbacks -->
          <p v-if="importingUsers" class="text-xs text-slate-500 font-semibold animate-pulse">Importing spreadsheet records...</p>
          
          <div v-if="importResult" class="bg-emerald-50 border border-emerald-200/60 rounded-xl p-3.5 text-xs text-emerald-800 space-y-1.5 shadow-sm">
            <p><strong>Total rows parsed:</strong> {{ importResult.totalRows }}</p>
            <p><strong>Inserted logs:</strong> {{ importResult.insertedCount }}</p>
            <p><strong>Skipped:</strong> {{ importResult.skippedCount }}</p>
            
            <div v-if="importResult.errors?.length" class="mt-2 text-red-800 border-t border-red-200/30 pt-2">
              <strong>Validation Errors:</strong>
              <ul class="list-disc list-inside mt-1 space-y-1 text-[11px]">
                <li v-for="err in importResult.errors" :key="`${err.rowNumber}-${err.userCode}`">
                  Row {{ err.rowNumber }} - {{ err.userCode || '(blank)' }}: {{ err.message }}
                </li>
              </ul>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Users Database Table View -->
    <div class="bg-white rounded-2xl border border-slate-200/80 shadow-sm overflow-hidden glassmorphism">
      <!-- toolbar -->
      <div class="flex flex-col sm:flex-row gap-3 items-center justify-between p-4 bg-slate-50/70 border-b border-slate-100">
        <!-- Search filter -->
        <div class="flex flex-wrap items-center gap-3 w-full sm:w-auto">
          <input 
            type="text" 
            v-model="keyword" 
            placeholder="Search code, name..." 
            class="bg-white border border-slate-200 rounded-lg px-3 py-1.5 text-xs outline-none focus:border-primary/50"
          />
          <select 
            v-model="deptFilter" 
            class="bg-white border border-slate-200 text-xs text-slate-700 px-3 py-1.5 rounded-lg outline-none cursor-pointer"
          >
            <option value="">All departments</option>
            <option v-for="d in userDepartmentOptions" :key="`filter-${d.deptCode}`" :value="d.deptCode">
              {{ d.deptName }}
            </option>
          </select>
          <button class="bg-primary text-white text-xs font-bold px-4 py-1.5 rounded-lg hover:bg-primary-dark transition active:scale-95" @click="applyFilter">Search</button>
        </div>
        <button class="bg-white hover:bg-slate-100 border border-slate-200 text-slate-700 text-xs font-bold px-3 py-1.5 rounded-lg active:scale-95 transition" @click="loadUsersList(keyword, deptFilter)">Refresh</button>
      </div>

      <p v-if="usersErrorMessage" class="p-4 text-xs text-red-500 font-semibold">{{ usersErrorMessage }}</p>

      <!-- list table -->
      <div class="overflow-x-auto w-full">
        <table class="w-full text-left border-collapse text-xs">
          <thead>
            <tr class="bg-slate-50 border-b border-slate-100 text-[10px] font-bold text-slate-500 uppercase tracking-wider">
              <th class="p-4">User Code</th>
              <th class="p-4">Card Number</th>
              <th class="p-4">Full Name</th>
              <th class="p-4">Department</th>
              <th class="p-4">Updated At</th>
              <th class="p-4">Actions</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-slate-100">
            <tr v-if="usersLoading && userListItems.length === 0">
              <td colspan="6" class="p-8 text-center text-slate-400 font-semibold">
                <div class="w-5 h-5 border-2 border-slate-200 border-t-primary rounded-full animate-spin mx-auto mb-2"></div>
                Loading users directory...
              </td>
            </tr>
            <tr v-else-if="userListItems.length === 0">
              <td colspan="6" class="p-8 text-center text-slate-400 font-semibold">No users found.</td>
            </tr>
            <tr v-else v-for="user in userListItems" :key="user.userCode" class="hover:bg-slate-50/40 transition">
              <td class="p-4"><span class="font-mono font-bold text-slate-600 bg-slate-100 px-2 py-0.5 rounded">{{ user.userCode }}</span></td>
              <td class="p-4 text-slate-600 font-medium">{{ user.cardNumber || '-' }}</td>
              <td class="p-4"><strong class="text-slate-800 text-sm">{{ user.fullName || '-' }}</strong></td>
              <td class="p-4 text-slate-500 font-semibold">{{ user.deptName || user.deptCode || '-' }}</td>
              <td class="p-4 text-slate-400 font-medium">{{ user.updatedAt ? formatDateTime(user.updatedAt) : '-' }}</td>
              <td class="p-4">
                <div class="flex gap-2">
                  <button class="text-indigo-500 hover:text-indigo-700 hover:bg-indigo-50 p-1.5 rounded-lg transition" @click="editItem(user)">✏</button>
                  <button class="text-red-400 hover:text-red-600 hover:bg-red-50 p-1.5 rounded-lg transition" @click="deleteItem(user)">✕</button>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <!-- pagination footer -->
      <div class="px-5 py-4 bg-slate-50/70 border-t border-slate-100 text-xs text-slate-500 font-semibold flex items-center justify-between">
        <span>Total: {{ usersTotal }} users</span>
        <div class="flex items-center gap-2">
          <button 
            class="bg-white hover:bg-slate-100 border border-slate-200 px-2.5 py-1 rounded text-[11px] font-bold transition active:scale-95 disabled:opacity-5"
            :disabled="usersPage <= 1" 
            @click="usersPage -= 1"
          >
            Prev
          </button>
          <span class="font-bold text-slate-700">Page {{ usersPage }}</span>
          <button 
            class="bg-white hover:bg-slate-100 border border-slate-200 px-2.5 py-1 rounded text-[11px] font-bold transition active:scale-95 disabled:opacity-5"
            :disabled="userListItems.length < usersPageSize" 
            @click="usersPage += 1"
          >
            Next
          </button>
        </div>
      </div>
    </div>
  </div>
</template>
