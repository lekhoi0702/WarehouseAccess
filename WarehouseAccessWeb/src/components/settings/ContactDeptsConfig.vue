<script setup>
import { ref, reactive, onMounted } from 'vue';
import { useRecords } from '../../composables/useRecords';
import { useToast } from '../../composables/useToast';

const {
  contactDeptItems,
  masterDataErrorMessage,
  loadContactDeptsCrud,
  saveContactDept,
  removeContactDept
} = useRecords();

const { showToast } = useToast();

const formMode = ref('create');
const formState = reactive({ contactDeptId: null, contactDeptName: '' });

onMounted(() => {
  loadContactDeptsCrud();
});

function resetForm() {
  formMode.value = 'create';
  formState.contactDeptId = null;
  formState.contactDeptName = '';
}

async function onSubmit() {
  masterDataErrorMessage.value = '';
  const name = formState.contactDeptName.trim();
  if (!name) {
    masterDataErrorMessage.value = 'Contact Dept Name is required.';
    return;
  }

  const payload = {
    contactDeptId: formState.contactDeptId,
    contactDeptName: name
  };

  try {
    const res = await saveContactDept(payload, formMode.value === 'edit');
    if (res?.success) {
      showToast(formMode.value === 'edit' ? 'Contact department updated' : 'Contact department created');
      resetForm();
    } else {
      masterDataErrorMessage.value = res?.message || 'Save failed';
    }
  } catch (e) {
    masterDataErrorMessage.value = 'Save failed';
  }
}

function editItem(item) {
  formMode.value = 'edit';
  formState.contactDeptId = item.contactDeptId;
  formState.contactDeptName = item.contactDeptName || '';
}

async function deleteItem(item) {
  if (!confirm(`Are you sure you want to delete contact department ${item.contactDeptName}?`)) return;
  try {
    const res = await removeContactDept(item.contactDeptId);
    if (res?.success) {
      showToast('Contact department deleted successfully');
    } else {
      masterDataErrorMessage.value = res?.message || 'Delete failed';
    }
  } catch (e) {
    masterDataErrorMessage.value = 'Delete failed';
  }
}
</script>

<template>
  <div class="grid grid-cols-1 md:grid-cols-[1fr_2fr] gap-6 text-left">
    <!-- Form Card -->
    <div class="bg-slate-50 border border-slate-200/60 rounded-2xl p-5 space-y-4 h-fit">
      <h3 class="text-sm font-extrabold text-slate-800 m-0">{{ formMode === 'create' ? 'Add Contact Dept' : 'Edit Contact Dept' }}</h3>
      
      <div class="space-y-3">
        <div class="flex flex-col gap-1 text-left">
          <label class="text-xs font-bold text-slate-500">Contact Dept Name</label>
          <input 
            type="text" 
            v-model="formState.contactDeptName"
            class="w-full bg-white border border-slate-200 rounded-xl px-4 py-2.5 text-xs outline-none focus:border-primary/50"
          />
        </div>
      </div>

      <p v-if="masterDataErrorMessage" class="text-xs text-red-500 font-semibold">{{ masterDataErrorMessage }}</p>

      <div class="flex gap-2 pt-2">
        <button 
          class="bg-primary text-white text-xs font-bold px-4 py-2.5 rounded-lg hover:bg-primary-dark transition active:scale-95 shadow-md shadow-primary/10"
          @click="onSubmit"
        >
          {{ formMode === 'create' ? 'Add' : 'Save' }}
        </button>
        <button 
          class="bg-white hover:bg-slate-50 border border-slate-200 text-slate-500 text-xs font-bold px-4 py-2.5 rounded-lg transition active:scale-95"
          @click="resetForm"
        >
          Reset
        </button>
      </div>
    </div>

    <!-- Table Card -->
    <div class="bg-white rounded-2xl border border-slate-200/80 shadow-sm overflow-hidden glassmorphism h-fit">
      <div class="overflow-x-auto w-full">
        <table class="w-full text-left border-collapse text-xs">
          <thead>
            <tr class="bg-slate-50 border-b border-slate-100 text-[10px] font-bold text-slate-500 uppercase tracking-wider">
              <th class="p-4">Id</th>
              <th class="p-4">Name</th>
              <th class="p-4">Status</th>
              <th class="p-4">Actions</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-slate-100">
            <tr v-if="contactDeptItems.length === 0">
              <td colspan="4" class="p-8 text-center text-slate-400 font-semibold">No contact departments found.</td>
            </tr>
            <tr v-else v-for="item in contactDeptItems" :key="item.contactDeptId" class="hover:bg-slate-50/40 transition">
              <td class="p-4 text-slate-500 font-bold font-mono">{{ item.contactDeptId }}</td>
              <td class="p-4"><strong class="text-slate-800 text-sm">{{ item.contactDeptName }}</strong></td>
              <td class="p-4"><span class="px-2 py-0.5 rounded text-[10px] font-bold" :class="item.recordStatus === 'Active' ? 'bg-emerald-50 text-emerald-600 border border-emerald-100' : 'bg-slate-100 text-slate-500'">{{ item.recordStatus || '-' }}</span></td>
              <td class="p-4">
                <div class="flex gap-2">
                  <button class="text-indigo-500 hover:text-indigo-700 hover:bg-indigo-50 p-1.5 rounded-lg transition" @click="editItem(item)">✏</button>
                  <button class="text-red-400 hover:text-red-600 hover:bg-red-50 p-1.5 rounded-lg transition" @click="deleteItem(item)">✕</button>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  </div>
</template>
