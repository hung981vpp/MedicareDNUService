<template>
  <section class="space-y-6">
    <div class="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm sm:p-6">
      <div class="flex flex-col gap-5 lg:flex-row lg:items-start lg:justify-between">
        <div>
          <p class="text-xs font-bold uppercase tracking-[0.14em] text-[#0F52BA]">{{ config.service }}</p>
          <h1 class="mt-2 text-2xl font-bold tracking-normal text-slate-950 sm:text-3xl">{{ config.title }}</h1>
          <p class="mt-3 max-w-3xl text-sm leading-6 text-slate-600">{{ config.description }}</p>
        </div>
        <button v-if="resource !== 'profile'" type="button" class="inline-flex h-11 items-center justify-center gap-2 rounded-lg border border-slate-200 bg-white px-4 text-sm font-bold text-slate-700 transition hover:border-blue-200 hover:bg-blue-50 hover:text-[#003c90]" :disabled="loading" @click="loadData">
          <RefreshCw :class="['h-4 w-4', loading ? 'animate-spin' : '']" />
          Tải lại
        </button>
      </div>
    </div>

    <div v-if="resource !== 'profile'" class="grid gap-4 sm:grid-cols-3">
      <div v-for="metric in metrics" :key="metric.label" class="rounded-2xl border border-slate-200 bg-white p-4 shadow-sm">
        <p class="text-xs font-bold uppercase tracking-wide text-slate-400">{{ metric.label }}</p>
        <p class="mt-2 text-2xl font-bold text-slate-950">{{ metric.value }}</p>
        <p class="mt-1 text-sm text-slate-500">{{ metric.note }}</p>
      </div>
    </div>

    <div v-if="note" class="rounded-xl border border-blue-100 bg-blue-50 px-4 py-3 text-sm text-[#003c90]">{{ note }}</div>
    <div v-if="error" class="rounded-xl border border-amber-200 bg-amber-50 p-4 text-sm text-amber-800">{{ error }}</div>

    <div v-if="resource === 'profile'" class="grid gap-6 lg:grid-cols-[1fr_0.85fr]">
      <section class="rounded-2xl border border-slate-200 bg-white p-6 shadow-sm">
        <div class="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
          <div class="flex items-center gap-4">
            <div class="flex h-14 w-14 items-center justify-center rounded-2xl bg-blue-50 text-[#0F52BA]">
              <UserRound class="h-7 w-7" />
            </div>
            <div>
              <p class="text-sm font-bold uppercase tracking-wide text-[#0F52BA]">Thông tin tài khoản</p>
              <h2 class="mt-1 text-2xl font-bold text-slate-950">{{ profileForm.fullName || authStore.user?.username || 'Bệnh nhân' }}</h2>
            </div>
          </div>
          <span class="inline-flex h-9 items-center rounded-lg bg-blue-50 px-3 text-sm font-bold text-[#003c90]">
            {{ displayPatientCode }}
          </span>
        </div>

        <form class="mt-6 grid gap-4 sm:grid-cols-2" @submit.prevent="saveProfile">
          <BaseInput v-model="profileForm.fullName" label="Họ và tên" required />
          <BaseInput :model-value="authStore.user?.username || ''" label="Tên đăng nhập" disabled />
          <BaseInput v-model="profileForm.email" label="Email" type="email" required />
          <BaseInput v-model="profileForm.phoneNumber" label="Số điện thoại" />
          <BaseInput v-model="profileForm.citizenId" label="Số CCCD" inputmode="numeric" maxlength="12" @update:model-value="handleCitizenInput" />
          <BaseInput v-model="profileForm.dateOfBirth" label="Ngày sinh" type="date" />
          <label class="block">
            <span class="mb-2 block text-sm font-medium text-slate-700">Giới tính</span>
            <select
              v-model="profileForm.gender"
              class="h-11 w-full rounded-lg border border-slate-200 bg-white px-3 text-sm text-slate-900 outline-none transition focus:border-[#0F52BA] focus:ring-4 focus:ring-blue-100"
            >
              <option value="">Chưa chọn</option>
              <option value="Nam">Nam</option>
              <option value="Nữ">Nữ</option>
              <option value="Khác">Khác</option>
            </select>
          </label>
          <label class="block">
            <span class="mb-2 block text-sm font-medium text-slate-700">Nhóm máu</span>
            <select
              v-model="profileForm.bloodType"
              class="h-11 w-full rounded-lg border border-slate-200 bg-white px-3 text-sm text-slate-900 outline-none transition focus:border-[#0F52BA] focus:ring-4 focus:ring-blue-100"
            >
              <option value="">Chưa rõ</option>
              <option v-for="type in bloodTypes" :key="type" :value="type">{{ type }}</option>
            </select>
          </label>
          <label class="block sm:col-span-2">
            <span class="mb-2 block text-sm font-medium text-slate-700">Địa chỉ</span>
            <textarea
              v-model="profileForm.address"
              rows="3"
              class="w-full rounded-lg border border-slate-200 bg-white px-3 py-2 text-sm text-slate-900 outline-none transition placeholder:text-slate-400 focus:border-[#0F52BA] focus:ring-4 focus:ring-blue-100"
              placeholder="Nhập địa chỉ hiện tại"
            ></textarea>
          </label>
          <label class="block sm:col-span-2">
            <span class="mb-2 block text-sm font-medium text-slate-700">Dị ứng</span>
            <textarea
              v-model="profileForm.allergyNote"
              rows="2"
              class="w-full rounded-lg border border-slate-200 bg-white px-3 py-2 text-sm text-slate-900 outline-none transition placeholder:text-slate-400 focus:border-[#0F52BA] focus:ring-4 focus:ring-blue-100"
              placeholder="VD: Không có, dị ứng penicillin..."
            ></textarea>
          </label>
          <label class="block sm:col-span-2">
            <span class="mb-2 block text-sm font-medium text-slate-700">Tiền sử bệnh</span>
            <textarea
              v-model="profileForm.medicalHistory"
              rows="3"
              class="w-full rounded-lg border border-slate-200 bg-white px-3 py-2 text-sm text-slate-900 outline-none transition placeholder:text-slate-400 focus:border-[#0F52BA] focus:ring-4 focus:ring-blue-100"
              placeholder="VD: Tăng huyết áp, tiểu đường, phẫu thuật trước đây..."
            ></textarea>
          </label>
          <div class="rounded-xl border border-slate-100 bg-slate-50 p-4">
            <p class="text-xs font-bold uppercase tracking-wide text-slate-400">Patient ID</p>
            <p class="mt-2 break-words font-semibold text-slate-900">{{ displayPatientCode }}</p>
          </div>
          <div class="rounded-xl border border-slate-100 bg-slate-50 p-4">
            <p class="text-xs font-bold uppercase tracking-wide text-slate-400">Cập nhật gần nhất</p>
            <p class="mt-2 break-words font-semibold text-slate-900">{{ formatDate(currentPatient?.updatedAt || currentPatient?.createdAt) }}</p>
          </div>
          <div class="sm:col-span-2">
            <BaseButton type="submit" :loading="profileSaving">
              <template #icon><Save class="h-4 w-4" /></template>
              Lưu hồ sơ
            </BaseButton>
          </div>
        </form>
      </section>
      <section class="rounded-2xl border border-blue-100 bg-blue-50 p-6 text-[#003c90]">
        <div class="flex items-center gap-3">
          <span class="flex h-10 w-10 items-center justify-center rounded-xl bg-white"><ShieldCheck class="h-5 w-5" /></span>
          <h3 class="font-bold">Liên kết dữ liệu</h3>
        </div>
        <div class="mt-5 space-y-3 text-sm leading-6">
          <p>N1 đọc lịch hẹn theo Patient ID.</p>
          <p>N2 đọc lịch sử khám, bệnh án và đơn thuốc theo Patient ID.</p>
          <p>N3 đọc viện phí theo tài khoản hoặc Patient ID.</p>
        </div>
      </section>
    </div>

    <div v-else-if="loading" class="grid gap-4 md:grid-cols-3">
      <LoadingSkeleton v-for="item in 3" :key="item" />
    </div>

    <div v-else class="rounded-2xl border border-slate-200 bg-white shadow-sm">
      <div class="grid gap-3 border-b border-slate-100 p-4 lg:grid-cols-[1fr_auto] lg:items-center">
        <label class="relative block">
          <Search class="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />
          <input v-model="query" class="h-11 w-full rounded-xl border border-slate-200 bg-white pl-10 pr-4 text-sm outline-none transition focus:border-blue-300 focus:ring-4 focus:ring-blue-100" :placeholder="config.placeholder" />
        </label>
        <span class="rounded-lg bg-blue-50 px-3 py-2 text-sm font-bold text-[#003c90]">{{ filteredRows.length }} dòng</span>
      </div>

      <div v-if="filteredRows.length" class="overflow-x-auto">
        <table class="min-w-full divide-y divide-slate-100 text-sm">
          <thead class="bg-slate-50 text-left text-xs font-bold uppercase tracking-wide text-slate-500">
            <tr>
              <th v-for="column in config.columns" :key="column.key" class="px-5 py-3">{{ column.label }}</th>
              <th v-if="['records', 'prescriptions', 'bills'].includes(resource)" class="px-5 py-3 text-right">Thao tác</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-slate-100">
            <tr v-for="row in paginatedRows" :key="String(row.id)" class="transition hover:bg-slate-50">
              <td v-for="column in config.columns" :key="column.key" class="px-5 py-4 align-top">
                <span v-if="column.badge" :class="['rounded-full px-2.5 py-1 text-xs font-bold', statusClass(value(row, column.key))]">{{ value(row, column.key) }}</span>
                <span v-else :class="column.strong ? 'font-bold text-slate-950' : 'text-slate-700'">{{ value(row, column.key) }}</span>
              </td>
              <td v-if="['records', 'prescriptions', 'bills'].includes(resource)" class="px-5 py-4 text-right">
                <button v-if="resource !== 'bills'" type="button" class="rounded-lg bg-blue-50 px-3 py-1.5 text-xs font-bold text-[#003c90] transition hover:bg-blue-100" @click="openDetail(row)">
                  Chi tiết
                </button>
                <button v-else-if="String(row.status).toLowerCase() !== 'paid' && !String(row.status).toLowerCase().includes('đã thanh toán')" type="button" class="rounded-lg bg-[#0F52BA] px-3 py-1.5 text-xs font-bold text-white transition hover:bg-[#003c90]" :disabled="actingId === row.id" @click="openPayment(row)">
                  Thanh toán
                </button>
              </td>
            </tr>
          </tbody>
        </table>

        <!-- Pagination Footer -->
        <div class="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between border-t border-slate-100 p-4 bg-slate-50/50">
          <div class="flex items-center gap-2 text-sm text-slate-500">
            <span>Hiển thị</span>
            <select
              v-model="itemsPerPage"
              class="h-8 rounded-lg border border-slate-200 bg-white px-2 text-sm font-semibold outline-none transition focus:border-blue-400 focus:ring-2 focus:ring-blue-100"
            >
              <option :value="10">10</option>
              <option :value="20">20</option>
              <option :value="50">50</option>
              <option :value="100">100</option>
            </select>
            <span>bản ghi mỗi trang</span>
          </div>

          <div class="text-sm font-medium text-slate-500">
            Hiển thị {{ Math.min(filteredRows.length, (currentPage - 1) * itemsPerPage + 1) }} - {{ Math.min(filteredRows.length, currentPage * itemsPerPage) }} trên {{ filteredRows.length }} kết quả
          </div>

          <div v-if="totalPages > 1" class="flex items-center gap-1.5">
            <button
              type="button"
              :disabled="currentPage === 1"
              class="flex h-8 w-8 items-center justify-center rounded-lg border border-slate-200 bg-white text-slate-500 transition hover:bg-slate-50 hover:text-slate-800 disabled:opacity-50 disabled:hover:bg-white disabled:hover:text-slate-500"
              @click="currentPage--"
            >
              <ChevronLeft class="h-4 w-4" />
            </button>
            <button
              v-for="page in totalPages"
              :key="page"
              type="button"
              :class="[
                'h-8 min-w-8 rounded-lg text-sm font-bold transition px-2',
                currentPage === page
                  ? 'bg-blue-600 text-white shadow-md'
                  : 'border border-slate-200 bg-white text-slate-600 hover:bg-slate-50 hover:text-slate-800'
              ]"
              @click="currentPage = page"
            >
              {{ page }}
            </button>
            <button
              type="button"
              :disabled="currentPage === totalPages"
              class="flex h-8 w-8 items-center justify-center rounded-lg border border-slate-200 bg-white text-slate-500 transition hover:bg-slate-50 hover:text-slate-800 disabled:opacity-50 disabled:hover:bg-white disabled:hover:text-slate-500"
              @click="currentPage++"
            >
              <ChevronRight class="h-4 w-4" />
            </button>
          </div>
        </div>
      </div>

      <div v-else class="p-10 text-center">
        <SearchX class="mx-auto h-10 w-10 text-slate-400" />
        <h2 class="mt-4 text-lg font-bold text-slate-950">Chưa có dữ liệu</h2>
        <p class="mx-auto mt-2 max-w-md text-sm leading-6 text-slate-500">
          Database chưa có dữ liệu phù hợp với tài khoản bệnh nhân này.
        </p>
      </div>
    </div>

    <div v-if="paymentOpen" class="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/50 p-4">
      <div class="max-h-[92vh] w-full max-w-4xl overflow-y-auto rounded-[1.5rem] bg-white p-6 shadow-2xl">
        <div class="flex items-start justify-between gap-4">
          <div>
            <p class="text-sm font-bold uppercase tracking-[0.16em] text-emerald-700">Thanh toán viện phí</p>
            <h2 class="mt-1 text-2xl font-bold text-slate-950">Chuyển khoản ngân hàng</h2>
            <p class="mt-2 text-sm text-slate-500">Quét mã QR, chuyển đúng số tiền và nội dung để hệ thống đối soát hóa đơn.</p>
          </div>
          <button type="button" class="rounded-xl p-2 text-slate-500 transition hover:bg-slate-100" @click="closePayment">
            <X class="h-5 w-5" />
          </button>
        </div>

        <div class="mt-6 grid gap-6 lg:grid-cols-[320px_1fr]">
          <div class="rounded-2xl border border-slate-200 bg-slate-50 p-4">
            <div v-if="bankTransferReady" class="rounded-xl bg-white p-3">
              <img :src="paymentQrUrl" alt="QR chuyển khoản viện phí" class="mx-auto aspect-square w-full rounded-lg object-contain" />
            </div>
            <div v-else class="flex aspect-square items-center justify-center rounded-xl border border-dashed border-amber-300 bg-amber-50 p-4 text-center text-sm font-semibold text-amber-800">
              Chưa cấu hình số tài khoản nhận tiền trong .env
            </div>
          </div>

          <div class="space-y-4">
            <div class="grid gap-3 sm:grid-cols-2">
              <div v-for="[label, textValue] in paymentItems" :key="label" class="rounded-xl border border-slate-100 bg-slate-50 p-4">
                <p class="text-xs font-bold uppercase tracking-wide text-slate-400">{{ label }}</p>
                <p class="mt-2 break-words text-sm font-semibold text-slate-900">{{ textValue }}</p>
              </div>
            </div>

            <div class="rounded-xl border border-blue-100 bg-blue-50 p-4 text-sm text-[#003c90]">
              SePay API/webhook nên xử lý ở backend N3 để tự xác nhận giao dịch. Trên frontend chỉ hiển thị QR và gửi yêu cầu ghi nhận thanh toán bằng phương thức BankTransfer.
            </div>

            <div class="flex flex-col gap-3 sm:flex-row sm:justify-end">
              <button type="button" class="inline-flex h-11 items-center justify-center gap-2 rounded-lg border border-slate-200 bg-white px-4 text-sm font-bold text-slate-700 transition hover:bg-slate-50" @click="copyPaymentContent">
                <Copy class="h-4 w-4" />
                Copy nội dung
              </button>
              <BaseButton :loading="actingId === paymentRow?.id" :disabled="!bankTransferReady" @click="confirmBankTransfer">
                <template #icon><CreditCard class="h-4 w-4" /></template>
                Tôi đã chuyển khoản
              </BaseButton>
            </div>
          </div>
        </div>
      </div>
    </div>

    <div v-if="detailOpen" class="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/50 p-4">
      <div class="max-h-[90vh] w-full max-w-3xl overflow-y-auto rounded-[1.5rem] bg-white p-6 shadow-2xl">
        <div class="flex items-start justify-between gap-4">
          <div>
            <p class="text-sm font-bold uppercase tracking-[0.16em] text-blue-700">{{ detailTitle }}</p>
            <h2 class="mt-1 text-2xl font-bold text-slate-950">{{ detailRow?.id }}</h2>
          </div>
          <button type="button" class="rounded-xl p-2 text-slate-500 transition hover:bg-slate-100" @click="detailOpen = false">
            <X class="h-5 w-5" />
          </button>
        </div>
        <dl class="mt-5 grid gap-3 sm:grid-cols-2">
          <div v-for="[label, textValue] in detailItems" :key="label" class="rounded-xl border border-slate-100 bg-slate-50 p-4">
            <dt class="text-xs font-bold uppercase tracking-wide text-slate-400">{{ label }}</dt>
            <dd class="mt-2 whitespace-pre-line break-words text-sm font-semibold text-slate-900">{{ textValue }}</dd>
          </div>
        </dl>
      </div>
    </div>

    <Toast
      :show="toast.show"
      :title="toast.title"
      :message="toast.message"
      :type="toast.type"
      @close="toast.show = false"
    />
  </section>
</template>

<script setup lang="ts">
import { computed, onMounted, onUnmounted, reactive, ref, watch } from 'vue'
import { useRoute } from 'vue-router'
import { CalendarClock, ChevronLeft, ChevronRight, Copy, CreditCard, FileHeart, Pill, RefreshCw, Save, Search, SearchX, ShieldCheck, UserRound, X } from 'lucide-vue-next'
import BaseButton from '@/components/ui/BaseButton.vue'
import BaseInput from '@/components/ui/BaseInput.vue'
import LoadingSkeleton from '@/components/ui/LoadingSkeleton.vue'
import Toast from '@/components/ui/Toast.vue'
import { useAuthStore } from '@/stores/authStore'
import { appointmentApi } from '@/services/appointmentApi'
import { billingApi } from '@/services/billingApi'
import { medicalRecordApi, type PatientMedicalHistory } from '@/services/medicalRecordApi'
import { getApiErrorMessage } from '@/services/apiClient'
import type { Appointment } from '@/types/appointment'
import type { Invoice, Prescription } from '@/types/billing'
import type { MedicalRecord, Patient } from '@/types/medicalRecord'

type Resource = 'appointments' | 'records' | 'prescriptions' | 'bills' | 'profile'
type Row = Record<string, any>
interface Column { key: string; label: string; badge?: boolean; strong?: boolean }

const route = useRoute()
const authStore = useAuthStore()
const loading = ref(false)
const error = ref('')
const note = ref('')
const query = ref('')
const rows = ref<Row[]>([])
const actingId = ref<string | number | null>(null)
const toast = reactive({ show: false, title: '', message: '', type: 'success' as 'success' | 'error' })
const currentPatient = ref<Patient | null>(null)
const history = ref<PatientMedicalHistory | null>(null)
const detailOpen = ref(false)
const detailRow = ref<Row | null>(null)
const paymentOpen = ref(false)
const paymentRow = ref<Row | null>(null)
const profileSaving = ref(false)
const bloodTypes = ['A+', 'A-', 'B+', 'B-', 'AB+', 'AB-', 'O+', 'O-']
const profileForm = reactive({
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

const resource = computed<Resource>(() => isResource(route.meta.patientResource) ? route.meta.patientResource : 'appointments')
const config = computed(() => configs[resource.value])
const patientId = computed(() => String(currentPatient.value?.id || currentPatient.value?.patientId || authStore.user?.patientId || ''))
const displayPatientCode = computed(() => patientDisplayCode(currentPatient.value) || formatPatientCode(patientId.value) || 'Chưa liên kết')

const configs: Record<Resource, { title: string; service: string; description: string; placeholder: string; icon: any; iconClass: string; search: string[]; columns: Column[] }> = {
  appointments: cfg('Lịch hẹn của tôi', 'N1 Appointment', 'Theo dõi lịch đã đặt, bác sĩ, giờ khám, số thứ tự và trạng thái xác nhận.', 'Tìm bác sĩ, lý do, trạng thái...', CalendarClock, 'bg-blue-50 text-[#0F52BA]', ['doctorName', 'status', 'reason', 'dateTime'], cols(['id', 'Mã'], ['doctorName', 'Bác sĩ', false, true], ['dateTime', 'Ngày giờ'], ['queueNumber', 'STT'], ['reason', 'Lý do'], ['status', 'Trạng thái', true])),
  records: cfg('Hồ sơ bệnh án', 'N2 Medical Record', 'Xem chẩn đoán, triệu chứng và ghi chú bác sĩ sau mỗi lần khám.', 'Tìm chẩn đoán, triệu chứng, ghi chú...', FileHeart, 'bg-indigo-50 text-indigo-700', ['id', 'diagnosis', 'symptoms', 'doctorNotes'], cols(['id', 'Mã BA'], ['diagnosis', 'Chẩn đoán', false, true], ['symptoms', 'Triệu chứng'], ['doctorNotes', 'Ghi chú'], ['createdAt', 'Ngày tạo'])),
  prescriptions: cfg('Đơn thuốc', 'N2 Prescription', 'Xem đơn thuốc cũ đã được bác sĩ chốt và gửi sang nhà thuốc.', 'Tìm mã đơn, thuốc, trạng thái...', Pill, 'bg-cyan-50 text-cyan-700', ['id', 'medicine', 'status', 'note'], cols(['id', 'Mã đơn'], ['medicine', 'Thuốc', false, true], ['quantity', 'Số lượng'], ['note', 'Ghi chú'], ['status', 'Trạng thái', true])),
  bills: cfg('Viện phí của tôi', 'N3 Billing', 'Xem hóa đơn, số tiền và thực hiện thanh toán viện phí khi cần.', 'Tìm mã hóa đơn, trạng thái...', CreditCard, 'bg-emerald-50 text-emerald-700', ['id', 'amount', 'status'], cols(['id', 'Mã HĐ'], ['appointmentId', 'Lịch hẹn'], ['amount', 'Số tiền', false, true], ['status', 'Trạng thái', true])),
  profile: cfg('Hồ sơ cá nhân', 'Auth/N2 Profile', 'Thông tin tài khoản bệnh nhân và hồ sơ N2 liên kết.', '', UserRound, 'bg-slate-100 text-slate-700', [], []),
}

const filteredRows = computed(() => {
  const keyword = query.value.trim().toLowerCase()
  if (!keyword) return rows.value
  return rows.value.filter((row) => config.value.search.some((key) => String(row[key] || '').toLowerCase().includes(keyword)))
})

// Pagination
const currentPage = ref(1)
const itemsPerPage = ref(10)

watch([resource, query], () => {
  currentPage.value = 1
})

const totalPages = computed(() => Math.ceil(filteredRows.value.length / itemsPerPage.value))

const paginatedRows = computed(() => {
  const start = (currentPage.value - 1) * itemsPerPage.value
  const end = start + itemsPerPage.value
  return filteredRows.value.slice(start, end)
})

const metrics = computed(() => {
  const statusText = rows.value.map((row) => String(row.status || '').toLowerCase())
  const pending = statusText.filter((status) => status.includes('pending') || status.includes('waiting') || status.includes('chờ') || status.includes('unpaid')).length
  const done = statusText.filter((status) => status.includes('completed') || status.includes('paid') || status.includes('hoàn') || status.includes('đã')).length
  return [
    { label: 'Tổng số', value: rows.value.length, note: config.value.service },
    { label: 'Cần theo dõi', value: pending, note: 'Đang chờ hoặc chưa thanh toán' },
    { label: 'Đã xử lý', value: done, note: 'Hoàn tất hoặc đã thanh toán' },
  ]
})

const detailTitle = computed(() => resource.value === 'records' ? 'Chi tiết bệnh án' : 'Chi tiết đơn thuốc')
const bankTransferConfig = {
  bank: import.meta.env.VITE_BANK_TRANSFER_BANK || 'Techcombank',
  account: import.meta.env.VITE_BANK_TRANSFER_ACCOUNT || '',
  accountName: import.meta.env.VITE_BANK_TRANSFER_ACCOUNT_NAME || 'MedicareDNU',
  prefix: import.meta.env.VITE_BANK_TRANSFER_PREFIX || 'MEDDNU',
}
const bankTransferReady = computed(() => Boolean(bankTransferConfig.bank && bankTransferConfig.account))
const paymentAmount = computed(() => toNumber(paymentRow.value?.amountValue, paymentRow.value?.raw?.amount, paymentRow.value?.raw?.totalAmount))
const paymentContent = computed(() => paymentRow.value ? transferContent(paymentRow.value) : '')
const paymentQrUrl = computed(() => {
  if (!bankTransferReady.value || !paymentRow.value) return ''
  const params = new URLSearchParams({
    acc: bankTransferConfig.account,
    bank: bankTransferConfig.bank,
    amount: String(Math.round(paymentAmount.value)),
    des: paymentContent.value,
    template: 'compact',
  })
  return `https://qr.sepay.vn/img?${params.toString()}`
})
const paymentItems = computed<[string, string][]>(() => [
  ['Ngân hàng', bankTransferConfig.bank],
  ['Số tài khoản', bankTransferConfig.account || 'Chưa cấu hình'],
  ['Tên tài khoản', bankTransferConfig.accountName],
  ['Số tiền', formatCurrency(paymentAmount.value)],
  ['Nội dung chuyển khoản', paymentContent.value || 'Chưa có hóa đơn'],
  ['Mã hóa đơn', String(paymentRow.value?.id || '')],
])
const detailItems = computed(() => {
  const row = detailRow.value || {}
  if (resource.value === 'records') {
    return [
      ['Mã bệnh án', row.id || ''],
      ['Chẩn đoán', row.diagnosis || 'Chưa có chẩn đoán'],
      ['Triệu chứng', row.symptoms || 'Chưa ghi nhận'],
      ['Ghi chú bác sĩ', row.doctorNotes || 'Chưa ghi chú'],
      ['Hướng điều trị', row.treatmentPlan || 'Chưa ghi nhận'],
      ['Ngày tái khám', row.followUpDate || 'Chưa hẹn'],
    ]
  }
  return [
    ['Mã đơn', row.id || ''],
    ['Thuốc', row.medicine || 'Chưa có thuốc'],
    ['Số lượng', row.quantity || '-'],
    ['Ghi chú', row.note || 'Không có ghi chú'],
    ['Trạng thái', row.status || 'Chưa cập nhật'],
  ]
})

watch(resource, () => {
  query.value = ''
  void loadData()
}, { immediate: true })

onMounted(() => {
  window.addEventListener('patient-profile-updated', handlePatientProfileUpdated)
})

onUnmounted(() => {
  window.removeEventListener('patient-profile-updated', handlePatientProfileUpdated)
})

async function loadData() {
  loading.value = resource.value !== 'profile'
  error.value = ''
  note.value = ''
  try {
    await resolvePatient()
    syncProfileForm()
    if (resource.value === 'profile') return
    if (resource.value === 'appointments') {
      const keys = numericKeys()
      rows.value = uniqueRows((await Promise.all(keys.map((key) => appointmentApi.getAppointmentsByPatient(key).catch(() => [] as Appointment[])))).flat().map(mapAppointment))
      note.value = rows.value.length ? 'Đã tải lịch hẹn từ N1.' : 'Database chưa có lịch hẹn cho bệnh nhân này.'
      showLoadToast('Lịch hẹn', rows.value.length, 'Nếu chưa có lịch, sang Đặt lịch khám để tạo lịch mới.')
    }
    if (resource.value === 'records') {
      const records = await getHistory().then((data) => data.medicalRecords)
      rows.value = records.map(mapRecord)
      note.value = rows.value.length ? 'Đã tải hồ sơ bệnh án từ N2.' : 'Database chưa có bệnh án cho bệnh nhân này.'
      showLoadToast('Hồ sơ bệnh án', rows.value.length, 'Bệnh án sẽ xuất hiện sau khi bác sĩ hoàn tất lượt khám.')
    }
    if (resource.value === 'prescriptions') {
      const keys = patientKeys()
      const [n2Prescriptions, n3PrescriptionsResults] = await Promise.all([
        getHistory().then((data) => data.prescriptions || []),
        Promise.all(keys.map(key => billingApi.getPrescriptions(key).catch(() => [] as Prescription[])))
      ])
      const n3Prescriptions = n3PrescriptionsResults.flat()
      const combined = [...n2Prescriptions, ...n3Prescriptions]
      const seen = new Set<string>()
      const uniquePrescriptions = combined.filter((p) => {
        const id = p.prescriptionId || p.id || p.prescriptionCode
        if (!id || seen.has(String(id))) return false
        seen.add(String(id))
        return true
      })
      rows.value = uniquePrescriptions.map(mapPrescription)
      note.value = rows.value.length ? 'Đã đồng bộ đơn thuốc từ N2 và N3.' : 'Database chưa có đơn thuốc cho bệnh nhân này.'
      showLoadToast('Đơn thuốc', rows.value.length, 'Đơn thuốc sẽ xuất hiện sau khi bác sĩ chốt đơn qua N2.')
    }
    if (resource.value === 'bills') {
      const keys = patientKeys()
      rows.value = uniqueRows((await Promise.all(keys.map((key) => billingApi.getInvoices(key).catch(() => [] as Invoice[])))).flat().map(mapInvoice))
      note.value = rows.value.length ? 'Đã tải viện phí từ N3.' : 'Database chưa có viện phí cho bệnh nhân này.'
      showLoadToast('Viện phí', rows.value.length, 'Nếu đã khám xong, liên hệ quầy thu ngân hoặc kiểm tra lại sau.')
    }
  } catch (apiError) {
    error.value = getApiErrorMessage(apiError)
    showToast('Không tải được dữ liệu', `${error.value} Kiểm tra lại liên kết Patient ID hoặc thử sang Hồ sơ cá nhân.`, 'error')
    rows.value = []
  } finally {
    loading.value = false
  }
}

async function resolvePatient() {
  if (currentPatient.value) return currentPatient.value
  const user = authStore.user
  const directId = String(user?.patientId || '')
  if (directId) {
    currentPatient.value = await medicalRecordApi.getPatient(directId).catch(() => null as any)
    if (currentPatient.value) {
      await syncPatientFromUser()
      return currentPatient.value
    }
  }
  const phones = new Set([user?.phoneNumber].map(normalizeText).filter(Boolean))
  const names = new Set([user?.fullName].map(normalizeText).filter(Boolean))
  const patients = await medicalRecordApi.getPatients({ pageSize: 100 }).catch(() => [] as Patient[])
  const match = patients.find((patient) => {
    const patientPhones = [patient.phone, patient.phoneNumber].map(normalizeText).filter(Boolean)
    const patientName = normalizeText(patient.fullName)
    return patientPhones.some((phone) => phones.has(phone)) || Boolean(patientName && names.has(patientName))
  }) || null
  currentPatient.value = match
    ? await medicalRecordApi.getPatient(match.id || match.patientId).catch(() => match)
    : null
  if (currentPatient.value && authStore.user) authStore.user.patientId = String(currentPatient.value.id || currentPatient.value.patientId || '')
  await syncPatientFromUser()
  return currentPatient.value
}

function handlePatientProfileUpdated(event: Event) {
  const patient = (event as CustomEvent<Patient>).detail
  if (!patient) return
  currentPatient.value = patient
  history.value = null
  syncProfileForm()
}

function syncProfileForm() {
  profileForm.fullName = currentPatient.value?.fullName || authStore.user?.fullName || ''
  profileForm.email = currentPatient.value?.email || authStore.user?.email || ''
  profileForm.phoneNumber = currentPatient.value?.phoneNumber || currentPatient.value?.phone || authStore.user?.phoneNumber || ''
  profileForm.citizenId = currentPatient.value?.citizenId || ''
  profileForm.dateOfBirth = normalizeDate(currentPatient.value?.dateOfBirth)
  profileForm.gender = currentPatient.value?.gender || ''
  profileForm.address = currentPatient.value?.address || ''
  profileForm.bloodType = currentPatient.value?.bloodType || ''
  profileForm.allergyNote = currentPatient.value?.allergyNote || currentPatient.value?.allergies || ''
  profileForm.medicalHistory = currentPatient.value?.medicalHistory || ''
}

async function syncPatientFromUser() {
  const patient = currentPatient.value
  const user = authStore.user
  const id = toNumber(patient?.id, patient?.patientId, user?.patientId)
  if (!patient || !user || !id) return

  const authFullName = user.fullName?.trim()
  const authEmail = user.email?.trim()
  const authPhoneNumber = user.phoneNumber?.trim()
  const nextFullName = patient.fullName || authFullName
  const nextEmail = patient.email || authEmail
  const nextPhoneNumber = patient.phoneNumber || patient.phone || authPhoneNumber
  const shouldSync =
    Boolean(!patient.fullName && authFullName) ||
    Boolean(!patient.email && authEmail) ||
    Boolean(!patient.phoneNumber && !patient.phone && authPhoneNumber)

  if (!shouldSync) return

  currentPatient.value = await medicalRecordApi.updatePatient(id, patientPayload({
    fullName: nextFullName || patient.fullName,
    email: nextEmail || patient.email,
    phoneNumber: nextPhoneNumber || patient.phoneNumber,
  }))
}

async function saveProfile() {
  const fullName = profileForm.fullName.trim()
  const email = profileForm.email.trim()
  const phoneNumber = profileForm.phoneNumber.trim()
  const citizenId = profileForm.citizenId.trim()
  if (!fullName) {
    showToast('Thiếu họ và tên', 'Vui lòng nhập họ và tên trước khi lưu hồ sơ.', 'error')
    return
  }
  if (!email) {
    showToast('Thiếu email', 'Vui lòng nhập email trước khi lưu hồ sơ.', 'error')
    return
  }
  if (citizenId && !/^\d{12}$/.test(citizenId)) {
    showToast('CCCD chưa hợp lệ', 'Số CCCD phải gồm đúng 12 chữ số.', 'error')
    return
  }

  profileSaving.value = true
  error.value = ''
  try {
    await authStore.updateProfile({ fullName: capitalizeWords(fullName), email, phoneNumber: phoneNumber || undefined })
    const id = toNumber(currentPatient.value?.id, currentPatient.value?.patientId, authStore.user?.patientId)
    const payload = patientPayload({
      fullName: capitalizeWords(fullName),
      email,
      phoneNumber,
      citizenId: citizenId || undefined,
      dateOfBirth: profileForm.dateOfBirth || undefined,
      gender: profileForm.gender || undefined,
      address: profileForm.address.trim() || undefined,
      bloodType: profileForm.bloodType || undefined,
      allergyNote: profileForm.allergyNote.trim() || null,
      medicalHistory: profileForm.medicalHistory.trim() || null,
    })
    if (id) {
      currentPatient.value = await medicalRecordApi.updatePatient(id, payload)
    } else {
      const savedPatient = await medicalRecordApi.createPatient(payload)
      const savedId = toNumber(savedPatient.id, savedPatient.patientId)
      currentPatient.value = savedId
        ? await medicalRecordApi.getPatient(savedId).catch(() => savedPatient)
        : savedPatient
      if (authStore.user) authStore.user.patientId = currentPatient.value.id || currentPatient.value.patientId
    }
    history.value = null
    syncProfileForm()
    showToast('Đã lưu hồ sơ', 'Thông tin hành chính và y tế đã được cập nhật vào cơ sở dữ liệu.', 'success')
  } catch (apiError) {
    error.value = getApiErrorMessage(apiError)
    showToast('Chưa lưu được hồ sơ', error.value, 'error')
  } finally {
    profileSaving.value = false
  }
}

function patientPayload(overrides: Partial<Patient>): Partial<Patient> {
  const patient = currentPatient.value
  return {
    fullName: overrides.fullName || patient?.fullName || authStore.user?.fullName || 'Bệnh nhân',
    email: overrides.email ?? patient?.email ?? authStore.user?.email,
    phoneNumber: overrides.phoneNumber ?? patient?.phoneNumber ?? patient?.phone ?? authStore.user?.phoneNumber,
    dateOfBirth: overrides.dateOfBirth ?? patient?.dateOfBirth,
    gender: overrides.gender ?? patient?.gender,
    address: overrides.address ?? patient?.address,
    citizenId: overrides.citizenId ?? patient?.citizenId,
    bloodType: overrides.bloodType ?? patient?.bloodType,
    allergyNote: Object.prototype.hasOwnProperty.call(overrides, 'allergyNote') ? overrides.allergyNote : patient?.allergyNote,
    medicalHistory: Object.prototype.hasOwnProperty.call(overrides, 'medicalHistory') ? overrides.medicalHistory : patient?.medicalHistory,
    status: patient?.status,
  }
}

async function getHistory() {
  if (history.value) return history.value
  const id = patientId.value
  if (!id) return { visits: [], medicalRecords: [], prescriptions: [] } as PatientMedicalHistory
  history.value = await medicalRecordApi.getPatientHistory(id).catch(() => ({ visits: [], medicalRecords: [], prescriptions: [] }) as PatientMedicalHistory)
  return history.value
}

function patientKeys() {
  const keys = new Set<string>()
  addKey(keys, authStore.user?.patientId)
  addKey(keys, currentPatient.value?.patientId)
  addKey(keys, currentPatient.value?.patientIdCode)
  addKey(keys, currentPatient.value?.id)
  addKey(keys, currentPatient.value?.patientCode)
  return Array.from(keys)
}

function numericKeys() {
  return patientKeys().filter((key) => /^\d+$/.test(key))
}

function addKey(keys: Set<string>, value: unknown) {
  const textValue = String(value ?? '').trim()
  if (textValue && textValue !== '0') keys.add(textValue)
}

function formatPatientCode(value: unknown) {
  const id = Number(value)
  return Number.isFinite(id) && id > 0 ? `BN${String(id).padStart(3, '0')}` : ''
}

function patientDisplayCode(item?: Partial<Patient> & Record<string, any> | null) {
  return String(item?.patientCode || item?.patientIdCode || item?.PatientCode || item?.PatientIdCode || '').trim()
}

function medicalRecordDisplayCode(item: Partial<MedicalRecord> & Record<string, any>) {
  return item.medicalRecordCode || item.medicalRecordIdCode || item.recordIdCode || item.recordId || item.medicalRecordId || 'BA'
}

function prescriptionDisplayCode(item: Partial<Prescription> & Record<string, any>) {
  return item.prescriptionCode || item.prescriptionIdCode || item.PrescriptionCode || item.PrescriptionIdCode || item.prescriptionId || item.id || 'DT'
}

function invoiceDisplayCode(item: Partial<Invoice> & Record<string, any>) {
  return item.invoiceCode || item.invoiceIdCode || item.InvoiceCode || item.InvoiceIdCode || toNumber(item.invoiceId, item.InvoiceId, item.id, item.Id) || 'HĐ'
}

function mapAppointment(item: Appointment): Row {
  return {
    id: item.appointmentId,
    doctorName: item.doctorName,
    dateTime: `${formatDate(item.appointmentDate)} - ${item.slotTime || 'Chưa cập nhật'}`,
    queueNumber: item.queueNumber || '-',
    reason: item.reason || 'Khám bệnh',
    status: statusLabel(item.status),
  }
}

function mapRecord(item: MedicalRecord): Row {
  return {
    id: medicalRecordDisplayCode(item),
    diagnosis: item.diagnosisText || item.diagnosis || 'Chưa có chẩn đoán',
    symptoms: item.symptoms || 'Chưa ghi nhận',
    doctorNotes: item.doctorNote || item.doctorNotes || 'Chưa ghi chú',
    treatmentPlan: item.treatmentPlan || 'Chưa ghi nhận',
    followUpDate: formatDate(item.followUpDate),
    createdAt: formatDate(item.examDate || item.createdAt),
    raw: item,
  }
}

function mapPrescription(item: Prescription & Record<string, any>): Row {
  const items = item.items || item.Items || []
  const medicines = items.map((line: any) => line.medicineNameSnapshot || line.MedicineNameSnapshot || line.medicineName || line.MedicineName).filter(Boolean).join(', ')
  const quantity = items.reduce((total: number, line: any) => total + Number(line.quantity || line.Quantity || 0), 0)
  return {
    id: prescriptionDisplayCode(item),
    medicine: medicines || 'Chưa có thuốc',
    quantity: quantity || '-',
    note: item.note || item.Note || 'Không có ghi chú',
    status: statusLabel(item.status || item.Status),
    raw: item,
  }
}

function mapInvoice(item: Invoice & Record<string, any>): Row {
  const amount = invoiceAmount(item)
  const invoiceId = toNumber(item.invoiceId, item.InvoiceId, item.id, item.Id)
  const invoiceCode = invoiceDisplayCode(item)
  return {
    id: invoiceCode,
    invoiceId,
    appointmentId: item.appointmentId || item.AppointmentId ? `#${item.appointmentId || item.AppointmentId}` : '-',
    amount: formatCurrency(amount),
    amountValue: amount,
    status: statusLabel(item.status || item.Status),
    raw: item,
  }
}

function openDetail(row: Row) {
  detailRow.value = row
  detailOpen.value = true
  showToast(
    resource.value === 'records' ? 'Đang xem chi tiết bệnh án' : 'Đang xem chi tiết đơn thuốc',
    resource.value === 'records' ? 'Nếu có đơn thuốc liên quan, sang mục Đơn thuốc để xem chi tiết.' : 'Nếu cần thanh toán, sang mục Viện phí để kiểm tra hóa đơn.',
    'success'
  )
}

function openPayment(row: Row) {
  paymentRow.value = row
  paymentOpen.value = true
}

function closePayment() {
  paymentOpen.value = false
  paymentRow.value = null
}

async function confirmBankTransfer() {
  const row = paymentRow.value
  if (!row) return
  const id = Number(row.invoiceId || row.id)
  if (!id) return
  actingId.value = row.id || null
  error.value = ''
  try {
    await billingApi.payInvoice(id, toNumber(row.amountValue), 'BankTransfer', {
      paymentContent: paymentContent.value,
      bankCode: bankTransferConfig.bank,
      bankAccountNumber: bankTransferConfig.account,
    })
    note.value = 'Đã gửi yêu cầu ghi nhận thanh toán chuyển khoản.'
    showToast('Thanh toán thành công', 'N3 đã ghi nhận thanh toán chuyển khoản ngân hàng.', 'success')
    closePayment()
    await loadData()
  } catch (apiError) {
    error.value = getApiErrorMessage(apiError)
    showToast('Thanh toán chưa thành công', `${error.value} Thử lại ở mục Viện phí hoặc liên hệ quầy thu ngân.`, 'error')
  } finally {
    actingId.value = null
  }
}

async function copyPaymentContent() {
  if (!paymentContent.value) return
  await navigator.clipboard?.writeText(paymentContent.value)
  showToast('Đã copy nội dung', paymentContent.value, 'success')
}

function uniqueRows(items: Row[]) {
  const seen = new Set<string>()
  return items.filter((item, index) => {
    const key = String(item.id || `${item.appointmentId || ''}-${index}`)
    if (seen.has(key)) return false
    seen.add(key)
    return true
  })
}

function normalizeText(value: unknown) {
  return String(value ?? '').trim().toLowerCase()
}

function normalizeDate(value: unknown) {
  return String(value ?? '').trim().slice(0, 10)
}

function handleCitizenInput(value: string) {
  profileForm.citizenId = value.replace(/\D/g, '').slice(0, 12)
}

function capitalizeWords(str: string): string {
  return str
    .trim()
    .split(/\s+/)
    .map(word => word.charAt(0).toUpperCase() + word.slice(1).toLowerCase())
    .join(' ')
}

function cfg(title: string, service: string, description: string, placeholder: string, icon: any, iconClass: string, search: string[], columns: Column[]) {
  return { title, service, description, placeholder, icon, iconClass, search, columns }
}

function cols(...defs: [string, string, boolean?, boolean?][]): Column[] {
  return defs.map(([key, label, badge, strong]) => ({ key, label, badge, strong }))
}

function value(row: Row, key: string) {
  return row[key] === undefined || row[key] === '' ? 'Chưa cập nhật' : String(row[key])
}

function formatCurrency(value: number) {
  return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(Number(value || 0))
}

function toNumber(...values: unknown[]) {
  for (const value of values) {
    const numberValue = Number(value)
    if (Number.isFinite(numberValue) && numberValue > 0) return numberValue
  }
  return 0
}

function transferContent(row: Row) {
  const invoiceCode = String(row.id || row.invoiceId || row.raw?.invoiceCode || row.raw?.invoiceIdCode || '').trim()
  return normalizeTransferText(`${bankTransferConfig.prefix} ${invoiceCode}`)
}

function normalizeTransferText(value: string) {
  return value
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .replace(/đ/g, 'd')
    .replace(/Đ/g, 'D')
    .toUpperCase()
    .replace(/[^A-Z0-9 ]/g, ' ')
    .replace(/\s+/g, ' ')
    .trim()
    .slice(0, 80)
}

function invoiceAmount(item: Invoice & Record<string, any>) {
  return toNumber(item.amount, item.Amount, item.totalAmount, item.TotalAmount, item.examinationFee, item.ExaminationFee, item.examFee, item.ExamFee)
}

function formatDate(value?: string) {
  if (!value) return 'Chưa cập nhật'
  const date = new Date(value)
  return Number.isNaN(date.getTime()) ? value : new Intl.DateTimeFormat('vi-VN').format(date)
}

function statusClass(status?: string) {
  const valueText = String(status || '').toLowerCase()
  if (valueText.includes('paid') || valueText.includes('confirmed') || valueText.includes('completed') || valueText.includes('đã') || valueText.includes('hoàn')) return 'bg-teal-100 text-teal-700'
  if (valueText.includes('pending') || valueText.includes('unpaid') || valueText.includes('waiting') || valueText.includes('chờ') || valueText.includes('chưa')) return 'bg-amber-100 text-amber-700'
  if (valueText.includes('cancel') || valueText.includes('hủy')) return 'bg-rose-100 text-rose-700'
  return 'bg-slate-100 text-slate-700'
}

function statusLabel(status?: string) {
  const valueText = String(status || '').toLowerCase()
  if (valueText.includes('confirmed')) return 'Đã xác nhận'
  if (valueText.includes('completed') || valueText.includes('done')) return 'Hoàn tất'
  if (valueText.includes('pending') || valueText.includes('waiting')) return 'Đang chờ'
  if (valueText.includes('unpaid')) return 'Chưa thanh toán'
  if (valueText.includes('paid')) return 'Đã thanh toán'
  if (valueText.includes('cancel')) return 'Đã hủy'
  return status || 'Chưa cập nhật'
}

function isResource(valueToCheck: unknown): valueToCheck is Resource {
  return typeof valueToCheck === 'string' && valueToCheck in configs
}

function showLoadToast(section: string, count: number, emptyGuide: string) {
  if (count > 0) {
    showToast(`Đã tải ${section}`, `Có ${count} dòng dữ liệu. Bấm Chi tiết nếu muốn xem thêm thông tin.`, 'success')
  } else {
    showToast(`Chưa có ${section}`, emptyGuide, 'error')
  }
}

function showToast(title: string, message: string, type: 'success' | 'error' = 'success') {
  toast.title = title
  toast.message = message
  toast.type = type
  toast.show = true
}
</script>
