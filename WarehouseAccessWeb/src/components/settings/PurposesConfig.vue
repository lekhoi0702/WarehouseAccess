<script setup>
import { ref, reactive, onMounted } from 'vue';
import { useRecords } from '../../composables/useRecords';
import { useToast } from '../../composables/useToast';

const {
  purposeItems,
  masterDataErrorMessage,
  loadPurposesCrud,
  savePurpose,
  removePurpose
} = useRecords();

const { showToast } = useToast();

const formMode = ref('create');
const formState = reactive({ purposeId: null, purposeName: '' });

onMounted(() => {
  loadPurposesCrud();
});

function resetForm() {
  formMode.value = 'create';
  formState.purposeId = null;
  formState.purposeName = '';
}

async function onSubmit() {
  masterDataErrorMessage.value = '';
  const name = formState.purposeName.trim();
  if (!name) {
    masterDataErrorMessage.value = 'Purpose Name is required.';
    return;
  }

  const payload = {
    purposeId: formState.purposeId,
    purposeName: name
  };

  try {
    const res = await savePurpose(payload, formMode.value === 'edit');
    if (res?.success) {
      showToast(formMode.value === 'edit' ? 'Purpose updated' : 'Purpose created');
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
  formState.purposeId = item.purposeId;
  formState.purposeName = item.purposeName || '';
}

async function deleteItem(item) {
  if (!confirm(`Are you sure you want to delete purpose ${item.purposeName}?`)) return;
  try {
    const res = await removePurpose(item.purposeId);
    if (res?.success) {
      showToast('Purpose deleted successfully');
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
      <h3 class="text-sm font-extrabold text-slate-800 m-0">{{ formMode === 'create' ? 'Add Purpose' : 'Edit Purpose' }}</h3>
      
      <div class="space-y-3">
        <div class="flex flex-col gap-1 text-left">
          <label class="text-xs font-bold text-slate-500">Purpose Name</label>
          <input 
            type="text" 
            v-model="formState.purposeName"
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
            <tr v-if="purposeItems.length === 0">
              <td colspan="4" class="p-8 text-center text-slate-400 font-semibold">No purposes found.</td>
            </tr>
            <tr v-else v-for="item in purposeItems" :key="item.purposeId" class="hover:bg-slate-50/40 transition">
              <td class="p-4 text-slate-500 font-bold font-mono">{{ item.purposeId }}</td>
              <td class="p-4"><strong class="text-slate-800 text-sm">{{ item.purposeName }}</strong></td>
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
