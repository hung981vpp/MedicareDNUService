<template>
  <section class="space-y-6">
    <FullscreenLoader :show="loading" />

    <div
      class="relative overflow-hidden rounded-2xl border border-blue-100 bg-[#003c90] p-6 text-white shadow-[0_20px_50px_rgba(15,82,186,0.22)] sm:p-7 lg:p-8"
    >
      <div class="absolute right-0 top-0 h-full w-1/2 bg-[radial-gradient(circle_at_top_right,rgba(125,244,255,0.28),transparent_52%)]"></div>
      <div class="absolute -bottom-20 -right-12 h-48 w-48 rounded-full border border-white/10"></div>
      <div class="relative z-10 grid gap-8 lg:grid-cols-[1fr_320px] lg:items-center">
        <div>
          <h1 class="max-w-2xl text-3xl font-bold leading-tight tracking-normal sm:text-4xl">
            Xin chào, {{ displayName }}
          </h1>
          <p class="mt-4 max-w-2xl text-sm leading-6 text-blue-50/90 sm:text-base">
            Theo dõi lịch hẹn, hồ sơ bệnh án, đơn thuốc và viện phí của bạn trong một giao diện thống nhất.
          </p>
          <div class="mt-6 flex flex-col gap-3 sm:flex-row">
            <RouterLink
              to="/patient/booking"
              class="inline-flex h-12 items-center justify-center gap-2 rounded-lg bg-white px-5 text-sm font-bold text-[#003c90] shadow-lg shadow-blue-950/20 transition hover:bg-blue-50"
            >
              <CalendarPlus class="h-4 w-4" />
              Đặt lịch khám mới
            </RouterLink>
            <RouterLink
              to="/patient/appointments"
              class="inline-flex h-12 items-center justify-center gap-2 rounded-lg border border-white/30 px-5 text-sm font-bold text-white transition hover:bg-white/10"
            >
              <CalendarClock class="h-4 w-4" />
              Xem lịch của tôi
            </RouterLink>
          </div>
        </div>

        <div class="rounded-2xl border border-white/15 bg-white/10 p-5 backdrop-blur">
          <div class="flex items-start justify-between gap-4">
            <div>
              <p class="text-xs font-semibold uppercase tracking-wide text-blue-100">Lịch sắp tới</p>
              <h2 class="mt-2 text-xl font-bold">{{ nextAppointment ? nextAppointment.doctorName : 'Chưa có lịch' }}</h2>
            </div>
            <span class="flex h-11 w-11 items-center justify-center rounded-xl bg-white/15">
              <Stethoscope class="h-5 w-5" />
            </span>
          </div>
          <div v-if="nextAppointment" class="mt-5 space-y-3 rounded-xl bg-white/10 p-4 text-sm text-blue-50">
            <p class="flex items-center gap-2">
              <CalendarDays class="h-4 w-4" />
              {{ formatDate(nextAppointment.appointmentDate) }}
            </p>
            <p class="flex items-center gap-2">
              <Clock3 class="h-4 w-4" />
              {{ nextAppointment.slotTime || 'Chưa cập nhật giờ' }}
            </p>
            <p class="flex items-center gap-2">
              <MapPin class="h-4 w-4" />
              {{ nextAppointment.specialtyName || 'Phòng khám MedicareDNU' }}
            </p>
          </div>
          <p v-else class="mt-5 rounded-xl bg-white/10 p-4 text-sm leading-6 text-blue-50">
            Bạn có thể đặt lịch khám mới để được sắp xếp bác sĩ và khung giờ phù hợp.
          </p>
        </div>
      </div>
    </div>

    <div v-if="error" class="rounded-xl border border-amber-200 bg-amber-50 p-4 text-sm text-amber-800">
      {{ error }}
    </div>

    <div class="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
      <RouterLink
        v-for="stat in stats"
        :key="stat.label"
        :to="stat.to"
        class="group rounded-2xl border border-slate-200 bg-white p-5 shadow-sm transition hover:border-blue-200 hover:shadow-[0_14px_34px_rgba(15,82,186,0.12)]"
      >
        <div class="flex items-start justify-between gap-3">
          <span :class="['flex h-11 w-11 items-center justify-center rounded-xl', stat.iconClass]">
            <component :is="stat.icon" class="h-5 w-5" />
          </span>
          <ChevronRight class="h-5 w-5 text-slate-300 transition group-hover:translate-x-0.5 group-hover:text-[#0F52BA]" />
        </div>
        <p class="mt-5 text-sm font-medium text-slate-500">{{ stat.label }}</p>
        <p class="mt-2 text-3xl font-bold text-slate-950">{{ stat.value }}</p>
        <p class="mt-2 text-xs font-semibold text-slate-500">{{ stat.note }}</p>
      </RouterLink>
    </div>

    <div class="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
      <RouterLink
        v-for="action in quickActions"
        :key="action.label"
        :to="action.to"
        class="group rounded-2xl border border-slate-200 bg-white p-5 shadow-sm transition hover:border-blue-200 hover:bg-blue-50/40"
      >
        <span class="flex h-12 w-12 items-center justify-center rounded-xl bg-blue-50 text-[#0F52BA] transition group-hover:bg-[#0F52BA] group-hover:text-white">
          <component :is="action.icon" class="h-5 w-5" />
        </span>
        <h3 class="mt-4 font-bold text-slate-950">{{ action.label }}</h3>
        <p class="mt-2 text-sm leading-6 text-slate-500">{{ action.description }}</p>
      </RouterLink>
    </div>

    <div class="grid gap-6 xl:grid-cols-[1.4fr_0.9fr]">
      <div class="space-y-6">
        <section class="rounded-2xl border border-slate-200 bg-white shadow-sm">
          <div class="flex flex-col gap-3 border-b border-slate-100 px-5 py-4 sm:flex-row sm:items-center sm:justify-between">
            <div>
              <h2 class="text-lg font-bold text-slate-950">Lịch hẹn gần nhất</h2>
              <p class="mt-1 text-sm text-slate-500">Lịch khám đã đặt</p>
            </div>
            <RouterLink to="/patient/appointments" class="inline-flex items-center gap-1 text-sm font-bold text-[#003c90] hover:text-[#0F52BA]">
              Xem tất cả
              <ChevronRight class="h-4 w-4" />
            </RouterLink>
          </div>
          <div class="divide-y divide-slate-100">
            <div v-for="item in appointments.slice(0, 4)" :key="item.appointmentId" class="grid gap-4 px-5 py-4 sm:grid-cols-[1fr_auto] sm:items-center">
              <div class="flex gap-3">
                <span class="mt-1 flex h-10 w-10 shrink-0 items-center justify-center rounded-xl bg-blue-50 text-[#0F52BA]">
                  <CalendarCheck class="h-5 w-5" />
                </span>
                <div>
                  <p class="font-bold text-slate-950">{{ item.doctorName || 'Bác sĩ chưa cập nhật' }}</p>
                  <p class="mt-1 text-sm text-slate-500">
                    {{ formatDate(item.appointmentDate) }} · {{ item.slotTime || 'Chưa cập nhật' }} · {{ item.reason || 'Khám bệnh' }}
                  </p>
                  <p class="mt-1 text-xs font-semibold text-slate-400">STT {{ item.queueNumber || '-' }}</p>
                </div>
              </div>
              <span :class="['w-fit rounded-full px-3 py-1 text-xs font-bold', statusClass(item.status)]">{{ statusLabel(item.status) }}</span>
            </div>
            <p v-if="!appointments.length" class="px-5 py-8 text-sm text-slate-500">Bạn chưa có lịch hẹn.</p>
          </div>
        </section>

        <section class="rounded-2xl border border-slate-200 bg-white shadow-sm">
          <div class="flex flex-col gap-3 border-b border-slate-100 px-5 py-4 sm:flex-row sm:items-center sm:justify-between">
            <div>
              <h2 class="text-lg font-bold text-slate-950">Hồ sơ bệnh án gần đây</h2>
              <p class="mt-1 text-sm text-slate-500">Hồ sơ khám gần đây</p>
            </div>
            <RouterLink to="/patient/records" class="inline-flex items-center gap-1 text-sm font-bold text-[#003c90] hover:text-[#0F52BA]">
              Xem hồ sơ
              <ChevronRight class="h-4 w-4" />
            </RouterLink>
          </div>
          <div class="overflow-x-auto">
            <table class="min-w-full text-left text-sm">
              <thead class="bg-slate-50 text-xs font-bold uppercase tracking-wide text-slate-500">
                <tr>
                  <th class="px-5 py-3">Chẩn đoán</th>
                  <th class="px-5 py-3">Bác sĩ</th>
                  <th class="px-5 py-3">Ngày tạo</th>
                  <th class="px-5 py-3 text-right">Mã</th>
                </tr>
              </thead>
              <tbody class="divide-y divide-slate-100">
                <tr v-for="record in records.slice(0, 4)" :key="record.medicalRecordId || record.recordId" class="hover:bg-slate-50">
                  <td class="px-5 py-4">
                    <div class="flex items-center gap-3">
                      <span class="flex h-9 w-9 shrink-0 items-center justify-center rounded-lg bg-indigo-50 text-indigo-700">
                        <FileHeart class="h-4 w-4" />
                      </span>
                      <span class="font-semibold text-slate-950">{{ record.diagnosis || 'Chưa cập nhật chẩn đoán' }}</span>
                    </div>
                  </td>
                  <td class="px-5 py-4 text-slate-600">{{ record.doctorName || 'Chưa cập nhật' }}</td>
                  <td class="px-5 py-4 text-slate-600">{{ formatDate(record.examDate || record.createdAt) }}</td>
                  <td class="px-5 py-4 text-right font-mono text-xs text-slate-500">#{{ medicalRecordDisplayCode(record) }}</td>
                </tr>
              </tbody>
            </table>
            <p v-if="!records.length" class="px-5 py-8 text-sm text-slate-500">Chưa có hồ sơ bệnh án.</p>
          </div>
        </section>
      </div>

      <aside class="space-y-6">
        <section class="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
          <div class="flex items-center justify-between gap-4">
            <div>
              <p class="text-xs font-bold uppercase tracking-wide text-slate-400">Viện phí</p>
              <h2 class="mt-2 text-lg font-bold text-slate-950">Cần theo dõi</h2>
            </div>
            <span class="flex h-11 w-11 items-center justify-center rounded-xl bg-emerald-50 text-emerald-700">
              <CreditCard class="h-5 w-5" />
            </span>
          </div>
          <div class="mt-5 space-y-3">
            <div v-for="item in invoices.slice(0, 4)" :key="item.invoiceId" class="rounded-xl border border-slate-100 bg-slate-50 p-4">
              <div class="flex items-start justify-between gap-3">
                <div>
                  <p class="font-bold text-slate-950">Hóa đơn #{{ invoiceDisplayCode(item) }}</p>
                  <p class="mt-1 text-sm text-slate-500">Lịch #{{ item.appointmentId || '-' }}</p>
                </div>
                <span :class="['rounded-full px-2.5 py-1 text-xs font-bold', statusClass(item.status)]">{{ statusLabel(item.status) }}</span>
              </div>
              <p class="mt-3 text-lg font-bold text-[#003c90]">{{ formatCurrency(item.amount) }}</p>
            </div>
            <p v-if="!invoices.length" class="rounded-xl bg-slate-50 p-4 text-sm text-slate-500">Chưa có hóa đơn.</p>
          </div>
          <RouterLink to="/patient/bills" class="mt-5 inline-flex h-11 w-full items-center justify-center rounded-lg border border-slate-200 text-sm font-bold text-slate-700 transition hover:border-blue-200 hover:bg-blue-50 hover:text-[#003c90]">
            Xem viện phí
          </RouterLink>
        </section>

        <section class="rounded-2xl border border-cyan-100 bg-cyan-50 p-5 text-cyan-950">
          <div class="flex items-center gap-3">
            <span class="flex h-10 w-10 items-center justify-center rounded-xl bg-white text-cyan-700">
              <Lightbulb class="h-5 w-5" />
            </span>
            <h2 class="font-bold">Nhắc nhở chăm sóc</h2>
          </div>
          <p class="mt-4 text-sm leading-6">
            Uống đủ nước, ngủ đúng giờ và kiểm tra lại lịch hẹn trước ngày khám để hạn chế chờ đợi tại quầy tiếp nhận.
          </p>
          <div class="mt-4 flex items-center gap-2 border-t border-cyan-200 pt-4 text-sm font-semibold text-cyan-800">
            <BellRing class="h-4 w-4" />
            Cập nhật hồ sơ khi có thay đổi thông tin cá nhân.
          </div>
        </section>
      </aside>
    </div>
  </section>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import {
  BellRing,
  CalendarCheck,
  CalendarClock,
  CalendarDays,
  CalendarPlus,
  ChevronRight,
  Clock3,
  CreditCard,
  FileHeart,
  Lightbulb,
  MapPin,
  Pill,
  ShieldCheck,
  Stethoscope,
  UserRoundSearch,
} from 'lucide-vue-next'
import FullscreenLoader from '@/components/ui/FullscreenLoader.vue'
import { useAuthStore } from '@/stores/authStore'
import { appointmentApi } from '@/services/appointmentApi'
import { billingApi } from '@/services/billingApi'
import { medicalRecordApi } from '@/services/medicalRecordApi'
import { getApiErrorMessage } from '@/services/apiClient'
import type { Appointment } from '@/types/appointment'
import type { Invoice } from '@/types/billing'
import type { MedicalRecord } from '@/types/medicalRecord'

const authStore = useAuthStore()
const loading = ref(true)
const error = ref('')
const appointments = ref<Appointment[]>([])
const invoices = ref<Invoice[]>([])
const records = ref<MedicalRecord[]>([])

const displayName = computed(() => authStore.user?.fullName || authStore.user?.username || 'bệnh nhân')
const upcomingAppointments = computed(() => appointments.value
  .filter(isUpcomingAppointment)
  .sort((a, b) => appointmentStartTimestamp(a) - appointmentStartTimestamp(b)))
const nextAppointment = computed(() => upcomingAppointments.value[0] || null)
const unpaidInvoices = computed(() => invoices.value.filter((item) => String(item.status || '').toLowerCase().includes('unpaid')))

const stats = computed(() => [
  { label: 'Lịch hẹn', value: appointments.value.length, note: 'Theo dõi lịch khám', to: '/patient/appointments', icon: CalendarCheck, iconClass: 'bg-blue-50 text-[#0F52BA]' },
  { label: 'Bệnh án', value: records.value.length, note: 'Hồ sơ khám bệnh', to: '/patient/records', icon: FileHeart, iconClass: 'bg-indigo-50 text-indigo-700' },
  { label: 'Hóa đơn', value: invoices.value.length, note: 'Viện phí & thanh toán', to: '/patient/bills', icon: CreditCard, iconClass: 'bg-emerald-50 text-emerald-700' },
  { label: 'Chưa thanh toán', value: unpaidInvoices.value.length, note: 'Cần theo dõi', to: '/patient/bills', icon: BellRing, iconClass: 'bg-amber-50 text-amber-700' },
])

const quickActions = [
  { label: 'Tìm bác sĩ', description: 'Xem danh sách bác sĩ và chuyên khoa phù hợp.', to: '/doctors', icon: UserRoundSearch },
  { label: 'Đặt lịch khám', description: 'Chọn chuyên khoa, bác sĩ và khung giờ khám.', to: '/patient/booking', icon: CalendarPlus },
  { label: 'Hồ sơ bệnh án', description: 'Theo dõi chẩn đoán và ghi chú từ bác sĩ.', to: '/patient/records', icon: FileHeart },
  { label: 'Đơn thuốc', description: 'Xem thuốc được kê và trạng thái chuẩn bị.', to: '/patient/prescriptions', icon: Pill },
]

onMounted(loadData)

async function loadData() {
  loading.value = true
  error.value = ''

  try {
    const patient = await medicalRecordApi.getCurrentPatient()
    const patientId = Number(patient.id || patient.patientId)
    if (Number.isFinite(patientId) && patientId > 0 && authStore.user) {
      authStore.user.patientId = patientId
    }

    const [appts, timeline, invs] = await Promise.all([
      Number.isFinite(patientId) && patientId > 0
        ? appointmentApi.getAppointmentsByPatient(patientId).catch(() => [] as Appointment[])
        : Promise.resolve([] as Appointment[]),
      medicalRecordApi.getCurrentPatientClinicalTimeline().catch((err) => {
        if ((err as any)?.response?.status === 404) return { visits: [], medicalRecords: [], prescriptions: [] }
        throw err
      }),
      Number.isFinite(patientId) && patientId > 0
        ? billingApi.getInvoices(patientId).catch((err) => {
        if ((err as any)?.response?.status === 404) return [] as Invoice[]
        throw err
      })
        : Promise.resolve([] as Invoice[]),
    ])

    const seenAppts = new Set()
    appointments.value = appts.filter((appointment) => {
      if (seenAppts.has(appointment.appointmentId)) return false
      seenAppts.add(appointment.appointmentId)
      return true
    })

    const seenRecs = new Set()
    records.value = timeline.medicalRecords.filter((record) => {
      const rid = medicalRecordDisplayCode(record)
      if (seenRecs.has(rid)) return false
      seenRecs.add(rid)
      return true
    })

    const seenInvs = new Set()
    invoices.value = invs.filter((invoice) => {
      const iid = invoiceDisplayCode(invoice)
      if (seenInvs.has(iid)) return false
      seenInvs.add(iid)
      return true
    })
  } catch (err) {
    const status = (err as any)?.response?.status
    error.value = status === 403
      ? 'Bạn không có quyền xem dữ liệu bệnh nhân này. Vui lòng đăng xuất rồi đăng nhập lại.'
      : getApiErrorMessage(err)
  } finally {
    loading.value = false
  }
}

function medicalRecordDisplayCode(record: MedicalRecord & Record<string, any>) {
  return record.medicalRecordCode || record.medicalRecordIdCode || record.recordIdCode || record.recordId || record.medicalRecordId || '-'
}

function invoiceDisplayCode(invoice: Invoice & Record<string, any>) {
  return invoice.invoiceCode || invoice.invoiceIdCode || invoice.InvoiceCode || invoice.InvoiceIdCode || invoice.invoiceId || '-'
}

function isUpcomingAppointment(appointment: Appointment & Record<string, any>) {
  if (isClosedAppointmentStatus(appointment.status || appointment.Status)) return false
  const timestamp = appointmentStartTimestamp(appointment)
  return Number.isFinite(timestamp) && timestamp >= Date.now()
}

function isClosedAppointmentStatus(status?: string | number) {
  const value = String(status || '').trim().toLowerCase()
  return value.includes('completed')
    || value.includes('complete')
    || value.includes('done')
    || value.includes('hoàn')
    || value.includes('cancel')
    || value.includes('hủy')
    || value.includes('huỷ')
    || value.includes('noshow')
    || value.includes('no show')
    || value.includes('expired')
    || value.includes('quá hạn')
}

function appointmentStartTimestamp(appointment: Appointment & Record<string, any>) {
  const scheduledAt = appointment.scheduledAt || appointment.ScheduledAt
  if (scheduledAt) {
    const scheduledTime = new Date(String(scheduledAt)).getTime()
    if (Number.isFinite(scheduledTime)) return scheduledTime
  }

  const dateOnly = normalizeAppointmentDate(appointment.appointmentDate || appointment.AppointmentDate)
  if (!dateOnly) return Number.NaN
  const timeText = String(appointment.slotTime || appointment.SlotTime || '00:00').slice(0, 5)
  const time = /^\d{1,2}:\d{2}$/.test(timeText) ? timeText : '00:00'
  return new Date(`${dateOnly}T${time}:00`).getTime()
}

function normalizeAppointmentDate(value: unknown) {
  const text = String(value || '').trim()
  if (!text) return ''
  const isoMatch = text.match(/^(\d{4})-(\d{2})-(\d{2})/)
  if (isoMatch) return `${isoMatch[1]}-${isoMatch[2]}-${isoMatch[3]}`

  const viMatch = text.match(/^(\d{1,2})[/-](\d{1,2})[/-](\d{4})/)
  if (viMatch) {
    const day = viMatch[1].padStart(2, '0')
    const month = viMatch[2].padStart(2, '0')
    return `${viMatch[3]}-${month}-${day}`
  }

  const parsed = new Date(text)
  return Number.isNaN(parsed.getTime()) ? '' : parsed.toISOString().slice(0, 10)
}

function formatCurrency(value: number) { return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(Number(value || 0)) }
function formatDate(value?: string) { if (!value) return 'Chưa cập nhật'; const date = new Date(value); return Number.isNaN(date.getTime()) ? value : new Intl.DateTimeFormat('vi-VN').format(date) }
function statusClass(status?: string) { const value = String(status || '').toLowerCase(); if (value.includes('paid') || value.includes('confirmed') || value.includes('checked') || value.includes('progress') || value.includes('completed')) return 'bg-teal-100 text-teal-700'; if (value.includes('pending') || value.includes('unpaid') || value.includes('waiting')) return 'bg-amber-100 text-amber-700'; if (value.includes('cancel')) return 'bg-rose-100 text-rose-700'; return 'bg-slate-100 text-slate-700' }
function statusLabel(status?: string) {
  const value = String(status || '').toLowerCase()
  if (value.includes('checked')) return 'Đã check-in'
  if (value.includes('progress')) return 'Đang khám'
  if (value.includes('confirmed')) return 'Đã xác nhận'
  if (value.includes('completed') || value.includes('done')) return 'Hoàn tất'
  if (value.includes('noshow')) return 'Không đến khám'
  if (value.includes('expired')) return 'Đã quá hạn'
  if (value.includes('pending') || value.includes('waiting')) return 'Đang chờ'
  if (value.includes('unpaid')) return 'Chưa thanh toán'
  if (value.includes('paid')) return 'Đã thanh toán'
  if (value.includes('cancel')) return 'Đã hủy'
  return status || 'Chưa cập nhật'
}
</script>
