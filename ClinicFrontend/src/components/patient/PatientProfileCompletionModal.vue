<template>
  <div v-if="visible" class="fixed inset-0 z-[80] flex items-center justify-center bg-slate-950/55 px-4 py-6 backdrop-blur-sm">
    <section class="max-h-[92vh] w-full max-w-3xl overflow-y-auto rounded-2xl border border-slate-200 bg-white shadow-2xl">
      <div class="border-b border-slate-100 px-5 py-5 sm:px-6">
        <div class="flex items-start gap-4">
          <span class="flex h-12 w-12 shrink-0 items-center justify-center rounded-xl bg-blue-50 text-[#0F52BA]">
            <IdCard class="h-6 w-6" />
          </span>
          <div>
            <p class="text-xs font-bold uppercase tracking-[0.14em] text-[#0F52BA]">Hoàn thiện hồ sơ bệnh nhân</p>
            <h2 class="mt-1 text-2xl font-bold text-slate-950">Cập nhật thông tin cá nhân</h2>
            <p class="mt-2 text-sm leading-6 text-slate-600">
              Vui lòng nhập CCCD, ngày sinh và địa chỉ để MedicareDNU liên kết hồ sơ khám bệnh chính xác.
            </p>
          </div>
        </div>
      </div>

      <form class="grid gap-4 px-5 py-5 sm:grid-cols-2 sm:px-6" @submit.prevent="saveProfile">
        <BaseInput v-model="form.fullName" label="Họ và tên" required />
        <BaseInput v-model="form.phoneNumber" label="Số điện thoại" type="tel" />
        <BaseInput v-model="form.email" label="Email" type="email" />
        <BaseInput v-model="form.citizenId" label="Số CCCD" inputmode="numeric" maxlength="12" required @update:model-value="handleCitizenInput" />
        <BaseInput v-model="form.dateOfBirth" label="Ngày sinh" type="date" required />
        <label class="block">
          <span class="mb-2 block text-sm font-medium text-slate-700">Giới tính</span>
          <select
            v-model="form.gender"
            class="h-11 w-full rounded-lg border border-slate-200 bg-white px-3 text-sm text-slate-900 outline-none transition focus:border-[#0F52BA] focus:ring-4 focus:ring-blue-100"
          >
            <option value="">Chưa chọn</option>
            <option value="Nam">Nam</option>
            <option value="Nữ">Nữ</option>
            <option value="Khác">Khác</option>
          </select>
        </label>

        <label class="block sm:col-span-2">
          <span class="mb-2 block text-sm font-medium text-slate-700">Địa chỉ <span class="text-rose-500">*</span></span>
          <textarea
            v-model="form.address"
            rows="3"
            required
            class="w-full rounded-lg border border-slate-200 bg-white px-3 py-2 text-sm text-slate-900 outline-none transition placeholder:text-slate-400 focus:border-[#0F52BA] focus:ring-4 focus:ring-blue-100"
            placeholder="Nhập địa chỉ hiện tại"
          ></textarea>
        </label>

        <label class="block">
          <span class="mb-2 block text-sm font-medium text-slate-700">Nhóm máu</span>
          <select
            v-model="form.bloodType"
            class="h-11 w-full rounded-lg border border-slate-200 bg-white px-3 text-sm text-slate-900 outline-none transition focus:border-[#0F52BA] focus:ring-4 focus:ring-blue-100"
          >
            <option value="">Chưa rõ</option>
            <option v-for="type in bloodTypes" :key="type" :value="type">{{ type }}</option>
          </select>
        </label>
        <BaseInput v-model="form.allergyNote" label="Dị ứng" placeholder="VD: Không có, dị ứng penicillin..." />

        <label class="block sm:col-span-2">
          <span class="mb-2 block text-sm font-medium text-slate-700">Tiền sử bệnh</span>
          <textarea
            v-model="form.medicalHistory"
            rows="3"
            class="w-full rounded-lg border border-slate-200 bg-white px-3 py-2 text-sm text-slate-900 outline-none transition placeholder:text-slate-400 focus:border-[#0F52BA] focus:ring-4 focus:ring-blue-100"
            placeholder="VD: Tăng huyết áp, tiểu đường, phẫu thuật trước đây..."
          ></textarea>
        </label>

        <div v-if="error" class="sm:col-span-2 rounded-xl border border-rose-200 bg-rose-50 p-3 text-sm text-rose-700">
          {{ error }}
        </div>

        <div class="flex flex-col gap-3 border-t border-slate-100 pt-4 sm:col-span-2 sm:flex-row sm:items-center sm:justify-between">
          <p class="text-xs leading-5 text-slate-500">Thông tin này sẽ được lưu vào hồ sơ bệnh nhân N2 và hiển thị trong Hồ sơ cá nhân.</p>
          <BaseButton type="submit" :loading="saving" class="w-full sm:w-auto">
            <template #icon>
              <Save class="h-4 w-4" />
            </template>
            Lưu thông tin
          </BaseButton>
        </div>
      </form>
    </section>
  </div>
</template>

<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { IdCard, Save } from 'lucide-vue-next'
import BaseButton from '@/components/ui/BaseButton.vue'
import BaseInput from '@/components/ui/BaseInput.vue'
import { getApiErrorMessage } from '@/services/apiClient'
import { medicalRecordApi } from '@/services/medicalRecordApi'
import { useAuthStore } from '@/stores/authStore'
import type { Patient } from '@/types/medicalRecord'

const emit = defineEmits<{
  completed: [patient: Patient]
}>()

const authStore = useAuthStore()
const visible = ref(false)
const saving = ref(false)
const error = ref('')
const currentPatient = ref<Patient | null>(null)
const bloodTypes = ['A+', 'A-', 'B+', 'B-', 'AB+', 'AB-', 'O+', 'O-']
const form = reactive({
  fullName: '',
  email: '',
  phoneNumber: '',
  citizenId: '',
  dateOfBirth: '',
  gender: '',
  address: '',
  bloodType: '',
  allergyNote: '',
  medicalHistory: '',
})

onMounted(checkProfile)

async function checkProfile() {
  if (!authStore.isPatient) return
  error.value = ''
  try {
    await authStore.resolvePatientProfile()
    currentPatient.value = await resolvePatient()
    fillForm(currentPatient.value)
    visible.value = needsCompletion(currentPatient.value)
  } catch (apiError) {
    error.value = getApiErrorMessage(apiError)
    visible.value = true
    fillForm(null)
  }
}

async function saveProfile() {
  const fullName = clean(form.fullName)
  const email = clean(form.email)
  const phoneNumber = clean(form.phoneNumber)
  const citizenId = clean(form.citizenId)
  const dateOfBirth = clean(form.dateOfBirth)
  const address = clean(form.address)

  if (!fullName) {
    error.value = 'Vui lòng nhập họ và tên.'
    return
  }
  if (!/^\d{12}$/.test(citizenId)) {
    error.value = 'Số CCCD phải gồm đúng 12 chữ số.'
    return
  }
  if (!dateOfBirth) {
    error.value = 'Vui lòng nhập ngày sinh.'
    return
  }
  if (!address) {
    error.value = 'Vui lòng nhập địa chỉ.'
    return
  }

  saving.value = true
  error.value = ''
  try {
    const payload: Partial<Patient> = {
      fullName,
      email: email || undefined,
      phoneNumber: phoneNumber || undefined,
      dateOfBirth,
      gender: clean(form.gender) || undefined,
      address,
      citizenId,
      bloodType: clean(form.bloodType) || undefined,
      allergyNote: clean(form.allergyNote) || null,
      medicalHistory: clean(form.medicalHistory) || null,
      status: currentPatient.value?.status,
    }

    const id = toPositiveNumber(currentPatient.value?.id, currentPatient.value?.patientId, authStore.user?.patientId)
    const savedPatient = id
      ? await medicalRecordApi.updatePatient(id, payload)
      : await medicalRecordApi.createPatient(payload)
    const savedId = toPositiveNumber(savedPatient.id, savedPatient.patientId)
    const patient = savedId
      ? await medicalRecordApi.getPatient(savedId).catch(() => savedPatient)
      : savedPatient

    currentPatient.value = patient
    if (authStore.user) {
      authStore.user = {
        ...authStore.user,
        patientId: patient.id || patient.patientId || authStore.user.patientId,
        fullName: patient.fullName || authStore.user.fullName,
        email: patient.email || authStore.user.email,
        phoneNumber: patient.phoneNumber || patient.phone || authStore.user.phoneNumber,
      }
    }
    visible.value = false
    window.dispatchEvent(new CustomEvent('patient-profile-updated', { detail: patient }))
    emit('completed', patient)
  } catch (apiError) {
    error.value = getApiErrorMessage(apiError)
  } finally {
    saving.value = false
  }
}

async function resolvePatient() {
  const user = authStore.user
  const directId = String(user?.patientId || '').trim()
  if (directId) {
    const patient = await medicalRecordApi.getPatient(directId).catch(() => null)
    if (patient) return patient
  }

  const patients = await medicalRecordApi.getPatients({ keyword: user?.phoneNumber || user?.email || user?.fullName, pageSize: 100 }).catch(() => [] as Patient[])
  const phone = normalize(user?.phoneNumber)
  const email = normalize(user?.email)
  const name = normalize(user?.fullName)
  const match = patients.find((patient) => {
    return Boolean(phone && normalize(patient.phoneNumber || patient.phone) === phone) ||
      Boolean(email && normalize(patient.email) === email) ||
      Boolean(name && normalize(patient.fullName) === name)
  }) || null

  return match ? medicalRecordApi.getPatient(match.id || match.patientId).catch(() => match) : null
}

function fillForm(patient: Patient | null) {
  form.fullName = patient?.fullName || authStore.user?.fullName || ''
  form.email = patient?.email || authStore.user?.email || ''
  form.phoneNumber = patient?.phoneNumber || patient?.phone || authStore.user?.phoneNumber || ''
  form.citizenId = patient?.citizenId || ''
  form.dateOfBirth = normalizeDate(patient?.dateOfBirth)
  form.gender = patient?.gender || ''
  form.address = patient?.address || ''
  form.bloodType = patient?.bloodType || ''
  form.allergyNote = patient?.allergyNote || patient?.allergies || ''
  form.medicalHistory = patient?.medicalHistory || ''
}

function needsCompletion(patient: Patient | null) {
  return !clean(patient?.citizenId) || !normalizeDate(patient?.dateOfBirth) || !clean(patient?.address)
}

function handleCitizenInput(value: string) {
  form.citizenId = value.replace(/\D/g, '').slice(0, 12)
}

function clean(value: unknown) {
  return String(value ?? '').trim()
}

function normalize(value: unknown) {
  return clean(value).toLowerCase()
}

function normalizeDate(value: unknown) {
  return clean(value).slice(0, 10)
}

function toPositiveNumber(...values: unknown[]) {
  for (const value of values) {
    const numberValue = Number(value)
    if (Number.isFinite(numberValue) && numberValue > 0) return numberValue
  }
  return 0
}
</script>
