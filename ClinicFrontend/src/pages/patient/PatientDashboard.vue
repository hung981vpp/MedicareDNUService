<template>
  <section class="min-h-screen bg-[#f8fafc] py-2 sm:py-3">
    <FullscreenLoader :show="loading" />

    <div class="mx-auto max-w-none space-y-5 px-4 sm:px-6 lg:px-8">
      <header class="flex flex-col gap-3 px-1 lg:flex-row lg:items-center lg:justify-between">
        <div>
          <h1 class="text-[1.8rem] font-bold leading-tight tracking-normal text-slate-950">Bảng điều khiển</h1>
          <p class="mt-1.5 text-[13px] font-medium leading-5 text-slate-500">
            Xin chào, {{ displayName }}. Đây là tổng quan lịch khám, hồ sơ, đơn thuốc và viện phí của bạn.
          </p>
        </div>
        <RouterLink
          to="/patient/booking"
          class="inline-flex h-10 w-fit items-center justify-center gap-2 rounded-lg bg-[#0F52BA] px-4 text-sm font-bold text-white shadow-sm transition hover:bg-[#003c90]"
        >
          <CalendarPlus class="h-4 w-4" />
          Đặt lịch khám
        </RouterLink>
      </header>

      <div v-if="error" class="rounded-xl border border-amber-200 bg-amber-50 p-4 text-sm text-amber-800">
        {{ error }}
      </div>

      <section class="grid gap-5 xl:grid-cols-[minmax(0,1fr)_380px]">
        <div class="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
          <div class="flex flex-col gap-5 lg:flex-row lg:items-start lg:justify-between">
            <div class="max-w-2xl">
              <div class="inline-flex h-11 w-11 items-center justify-center rounded-xl bg-blue-50 text-[#0F52BA]">
                <Activity class="h-5 w-5" />
              </div>
              <h2 class="mt-4 text-2xl font-bold tracking-normal text-slate-950">{{ dashboardHeadline }}</h2>
              <p class="mt-2 text-sm leading-6 text-slate-500">{{ dashboardNote }}</p>
            </div>

            <div class="grid w-full gap-3 sm:grid-cols-2 lg:w-[360px]">
              <div v-for="metric in compactMetrics" :key="metric.label" class="rounded-xl border border-slate-100 bg-slate-50 p-3.5">
                <div class="flex items-center gap-2 text-xs font-semibold text-slate-500">
                  <component :is="metric.icon" class="h-4 w-4 text-[#0F52BA]" />
                  {{ metric.label }}
                </div>
                <p class="mt-2 text-2xl font-bold text-slate-950">{{ metric.value }}</p>
              </div>
            </div>
          </div>

        </div>

        <aside class="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
          <div class="flex items-start justify-between gap-4">
            <div>
              <p class="text-xs font-bold uppercase tracking-wide text-slate-400">Lịch gần nhất</p>
              <h2 class="mt-1 text-lg font-bold text-slate-950">
                {{ nextAppointment ? nextAppointment.doctorName || 'Bác sĩ chưa cập nhật' : 'Chưa có lịch sắp tới' }}
              </h2>
            </div>
            <span class="flex h-11 w-11 items-center justify-center rounded-xl bg-indigo-50 text-indigo-700">
              <Stethoscope class="h-5 w-5" />
            </span>
          </div>

          <div v-if="nextAppointment" class="mt-5 space-y-3 rounded-xl bg-slate-50 p-4 text-sm">
            <div class="flex items-center gap-2 text-slate-700">
              <CalendarDays class="h-4 w-4 text-slate-400" />
              <span class="font-semibold">{{ formatDate(nextAppointment.appointmentDate) }}</span>
              <span class="text-slate-400">{{ timeLabel(nextAppointment) }}</span>
            </div>
            <div class="flex items-center gap-2 text-slate-600">
              <MapPin class="h-4 w-4 text-slate-400" />
              {{ nextAppointment.specialtyName || 'Phòng khám MedicareDNU' }}
            </div>
            <span :class="['inline-flex rounded-full px-3 py-1 text-xs font-medium', appointmentStatusClass(nextAppointment.status)]">
              {{ appointmentStatusLabel(nextAppointment.status) }}
            </span>
          </div>
          <div v-else class="mt-5 rounded-xl border border-dashed border-slate-200 bg-slate-50 p-4 text-sm leading-6 text-slate-500">
            Chưa có lịch khám sắp tới. Khi cần khám hoặc tái khám, bạn có thể đặt lịch mới.
          </div>
        </aside>
      </section>

      <section class="grid gap-5 xl:grid-cols-[minmax(0,1fr)_420px]">
        <div class="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
          <div class="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
            <div>
              <h2 class="text-lg font-bold text-slate-950">Xu hướng khám 6 tháng</h2>
              <p class="mt-1 text-sm text-slate-500">Số lịch hẹn và bệnh án phát sinh theo tháng.</p>
            </div>
            <div class="flex items-center gap-4 text-xs font-semibold text-slate-500">
              <span class="inline-flex items-center gap-1.5"><span class="h-2.5 w-2.5 rounded-full bg-[#0F52BA]"></span>Lịch hẹn</span>
              <span class="inline-flex items-center gap-1.5"><span class="h-2.5 w-2.5 rounded-full bg-orange-500"></span>Bệnh án</span>
            </div>
          </div>

          <div class="mt-5 rounded-xl border border-slate-100 bg-slate-50/60 p-4">
            <svg class="h-72 w-full" viewBox="0 0 720 280" role="img" aria-label="Xu hướng khám 6 tháng">
              <defs>
                <linearGradient id="appointmentTrendFill" x1="0" x2="0" y1="0" y2="1">
                  <stop offset="0%" stop-color="#0F52BA" stop-opacity="0.18" />
                  <stop offset="100%" stop-color="#0F52BA" stop-opacity="0" />
                </linearGradient>
                <linearGradient id="recordTrendFill" x1="0" x2="0" y1="0" y2="1">
                  <stop offset="0%" stop-color="#f97316" stop-opacity="0.18" />
                  <stop offset="100%" stop-color="#f97316" stop-opacity="0" />
                </linearGradient>
              </defs>
              <line
                v-for="line in trendChart.gridLines"
                :key="line"
                x1="40"
                x2="680"
                :y1="line"
                :y2="line"
                stroke="#e2e8f0"
                stroke-dasharray="4 8"
              />
              <path :d="trendChart.appointmentArea" fill="url(#appointmentTrendFill)" />
              <path :d="trendChart.recordArea" fill="url(#recordTrendFill)" />
              <path :d="trendChart.appointmentPath" fill="none" stroke="#0F52BA" stroke-linecap="round" stroke-width="4" />
              <path :d="trendChart.recordPath" fill="none" stroke="#f97316" stroke-linecap="round" stroke-width="4" />
              <g v-for="point in trendChart.appointmentPoints" :key="`appointment-${point.key}`">
                <circle :cx="point.x" :cy="point.y" r="5" fill="#0F52BA" stroke="#fff" stroke-width="3" />
                <text v-if="point.value > 0" :x="point.x" :y="point.y - 12" text-anchor="middle" class="fill-slate-500 text-[11px] font-medium">{{ point.value }}</text>
              </g>
              <g v-for="point in trendChart.recordPoints" :key="`record-${point.key}`">
                <circle :cx="point.x" :cy="point.y" r="5" fill="#f97316" stroke="#fff" stroke-width="3" />
                <text v-if="point.value > 0" :x="point.x" :y="point.y + 22" text-anchor="middle" class="fill-slate-500 text-[11px] font-medium">{{ point.value }}</text>
              </g>
              <g v-for="label in trendChart.labels" :key="label.key">
                <text :x="label.x" y="260" text-anchor="middle" class="fill-slate-500 text-[11px] font-semibold">{{ label.label }}</text>
              </g>
            </svg>
          </div>
        </div>

        <div class="grid gap-5">
          <div class="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
            <div class="flex items-start justify-between gap-4">
              <div>
                <h2 class="text-lg font-bold text-slate-950">Trạng thái lịch hẹn</h2>
                <p class="mt-1 text-sm text-slate-500">Tổng hợp theo trạng thái.</p>
              </div>
              <CalendarClock class="h-5 w-5 text-[#0F52BA]" />
            </div>
            <div class="mt-5 flex items-center gap-5">
              <div class="relative h-32 w-32 shrink-0 rounded-full" :style="{ background: appointmentDonutGradient }">
                <div class="absolute inset-5 flex items-center justify-center rounded-full bg-white">
                  <span class="text-2xl font-bold text-slate-950">{{ appointments.length }}</span>
                </div>
              </div>
              <div class="min-w-0 flex-1 space-y-2">
                <div v-for="segment in appointmentStatusSegments" :key="segment.label" class="flex items-center justify-between gap-3 text-sm">
                  <span class="inline-flex min-w-0 items-center gap-2 text-slate-600">
                    <span class="h-2.5 w-2.5 shrink-0 rounded-full" :style="{ backgroundColor: segment.color }"></span>
                    <span class="truncate">{{ segment.label }}</span>
                  </span>
                  <span class="font-medium text-slate-900">{{ segment.value }}</span>
                </div>
              </div>
            </div>
          </div>

          <div class="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
            <div class="flex items-start justify-between gap-4">
              <div>
                <h2 class="text-lg font-bold text-slate-950">Viện phí</h2>
                <p class="mt-1 text-sm text-slate-500">Tổng tiền và tiến độ thanh toán.</p>
              </div>
              <WalletCards class="h-5 w-5 text-emerald-700" />
            </div>
            <div class="mt-5">
              <div class="flex items-end justify-between">
                <div>
                  <p class="text-xs font-semibold text-slate-400">Đã thanh toán</p>
                  <p class="mt-1 text-2xl font-bold text-emerald-700">{{ formatCurrency(financeSummary.paid) }}</p>
                </div>
                <p class="text-sm font-semibold text-slate-500">{{ financeSummary.paidPercent }}%</p>
              </div>
              <div class="mt-4 h-3 overflow-hidden rounded-full bg-slate-100">
                <div class="h-full rounded-full bg-emerald-500" :style="{ width: `${financeSummary.paidPercent}%` }"></div>
              </div>
              <div class="mt-4 grid grid-cols-2 gap-3">
                <div class="rounded-xl bg-slate-50 p-3">
                  <p class="text-xs font-semibold text-slate-400">Tổng</p>
                  <p class="mt-1 font-bold text-slate-950">{{ formatCurrency(financeSummary.total) }}</p>
                </div>
                <div class="rounded-xl bg-amber-50 p-3">
                  <p class="text-xs font-semibold text-amber-700">Còn lại</p>
                  <p class="mt-1 font-bold text-amber-800">{{ formatCurrency(financeSummary.unpaid) }}</p>
                </div>
              </div>
            </div>
          </div>
        </div>
      </section>

      <section class="grid gap-5 xl:grid-cols-[minmax(0,1.25fr)_minmax(360px,0.75fr)]">
        <div class="rounded-2xl border border-slate-200 bg-white shadow-sm">
          <TableHeader title="Lịch hẹn cần theo dõi" subtitle="Ưu tiên lịch sắp tới và lịch mới nhất." to="/patient/appointments" action="Xem lịch" />
          <div class="overflow-x-auto">
            <table class="min-w-full text-left text-[13px]">
              <thead class="bg-slate-50 text-[11px] font-semibold uppercase text-slate-500">
                <tr>
                  <th class="px-4 py-2.5">Bác sĩ</th>
                  <th class="px-4 py-2.5">Chuyên khoa</th>
                  <th class="px-4 py-2.5">Ngày giờ</th>
                  <th class="px-4 py-2.5">Trạng thái</th>
                </tr>
              </thead>
              <tbody class="divide-y divide-slate-100">
                <tr v-for="item in appointmentRows" :key="item.appointmentId" class="hover:bg-slate-50/80">
                  <td class="px-4 py-3">
                    <div class="flex items-center gap-3">
                      <span class="flex h-9 w-9 shrink-0 items-center justify-center rounded-lg bg-blue-50 text-[#0F52BA]">
                        <Stethoscope class="h-4 w-4" />
                      </span>
                      <div>
                        <p class="font-semibold text-slate-950">{{ item.doctorName || 'Chưa cập nhật' }}</p>
                        <p class="mt-0.5 text-xs text-slate-400">LH{{ String(item.appointmentId || '').padStart(3, '0') }}</p>
                      </div>
                    </div>
                  </td>
                  <td class="px-4 py-3 text-slate-600">{{ item.specialtyName || 'Chưa cập nhật' }}</td>
                  <td class="whitespace-nowrap px-4 py-3">
                    <span class="font-medium text-slate-700">{{ formatDate(item.appointmentDate) }}</span>
                    <span class="ml-2 text-xs text-slate-400">{{ timeLabel(item) }}</span>
                  </td>
                  <td class="px-4 py-3">
                    <span :class="['rounded-full px-2.5 py-1 text-xs font-medium', appointmentStatusClass(item.status)]">{{ appointmentStatusLabel(item.status) }}</span>
                  </td>
                </tr>
              </tbody>
            </table>
            <EmptyRow v-if="!appointmentRows.length" icon="calendar" text="Chưa có lịch hẹn cần theo dõi." />
          </div>
        </div>

        <div class="rounded-2xl border border-slate-200 bg-white shadow-sm">
          <TableHeader title="Đơn thuốc mới" subtitle="Theo dõi trạng thái cấp thuốc." to="/patient/prescriptions" action="Xem đơn" />
          <div class="divide-y divide-slate-100">
            <div v-for="item in prescriptionRows" :key="prescriptionKey(item)" class="px-4 py-3">
              <div class="flex items-start justify-between gap-4">
                <div class="flex min-w-0 gap-3">
                  <span class="flex h-9 w-9 shrink-0 items-center justify-center rounded-lg bg-cyan-50 text-cyan-700">
                    <Pill class="h-4 w-4" />
                  </span>
                  <div class="min-w-0">
                    <p class="truncate font-semibold text-slate-950">{{ prescriptionDisplayCode(item) }}</p>
                    <p class="mt-1 line-clamp-1 text-xs text-slate-500">{{ prescriptionMedicineText(item) }}</p>
                  </div>
                </div>
                <span :class="['shrink-0 rounded-full px-2.5 py-1 text-xs font-medium', prescriptionStatusClass(item.status)]">{{ prescriptionStatusLabel(item.status) }}</span>
              </div>
            </div>
            <EmptyRow v-if="!prescriptionRows.length" icon="pill" text="Chưa có đơn thuốc gần đây." />
          </div>
        </div>
      </section>

      <section class="grid gap-5 xl:grid-cols-[minmax(0,1.25fr)_minmax(360px,0.75fr)]">
        <div class="rounded-2xl border border-slate-200 bg-white shadow-sm">
          <TableHeader title="Hồ sơ bệnh án gần đây" subtitle="Tóm tắt chẩn đoán và ngày lập hồ sơ." to="/patient/records" action="Xem hồ sơ" />
          <div class="overflow-x-auto">
            <table class="min-w-full text-left text-[13px]">
              <thead class="bg-slate-50 text-[11px] font-semibold uppercase text-slate-500">
                <tr>
                  <th class="px-4 py-2.5">Mã bệnh án</th>
                  <th class="px-4 py-2.5">Chẩn đoán</th>
                  <th class="px-4 py-2.5">Ngày tạo</th>
                  <th class="px-4 py-2.5">Trạng thái</th>
                </tr>
              </thead>
              <tbody class="divide-y divide-slate-100">
                <tr v-for="record in recordRows" :key="medicalRecordDisplayCode(record)" class="hover:bg-slate-50/80">
                  <td class="px-4 py-3 font-mono text-xs font-semibold text-[#0F52BA]">{{ medicalRecordDisplayCode(record) }}</td>
                  <td class="px-4 py-3">
                    <p class="line-clamp-1 font-medium text-slate-800">{{ record.diagnosisText || record.diagnosis || 'Chưa cập nhật chẩn đoán' }}</p>
                    <p class="mt-1 text-xs text-slate-400">{{ doctorNameForRecord(record) || 'Bác sĩ chưa cập nhật' }}</p>
                  </td>
                  <td class="whitespace-nowrap px-4 py-3 text-slate-600">{{ formatDate(record.examDate || record.createdAt) }}</td>
                  <td class="px-4 py-3">
                    <span :class="['rounded-full px-2.5 py-1 text-xs font-medium', recordStatusClass(record.status)]">{{ recordStatusLabel(record.status) }}</span>
                  </td>
                </tr>
              </tbody>
            </table>
            <EmptyRow v-if="!recordRows.length" icon="record" text="Chưa có hồ sơ bệnh án." />
          </div>
        </div>

        <div class="rounded-2xl border border-slate-200 bg-white shadow-sm">
          <TableHeader title="Viện phí gần đây" subtitle="Hóa đơn mới nhất và trạng thái thanh toán." to="/patient/bills" action="Xem viện phí" />
          <div class="divide-y divide-slate-100">
            <div v-for="invoice in invoiceRows" :key="invoiceDisplayCode(invoice)" class="px-4 py-2.5">
              <div class="flex items-center justify-between gap-4">
                <div>
                  <p class="font-mono text-[11px] font-semibold text-[#0F52BA]">{{ invoiceDisplayCode(invoice) }}</p>
                  <p class="mt-0.5 text-xs text-slate-500">Lịch hẹn {{ appointmentInvoiceLabel(invoice) }}</p>
                </div>
                <span :class="['rounded-full px-2 py-0.5 text-[11px] font-medium', invoiceStatusClass(invoice.status)]">{{ invoiceStatusLabel(invoice.status) }}</span>
              </div>
              <p class="mt-1.5 text-sm font-semibold text-slate-950">{{ formatCurrency(invoiceAmount(invoice)) }}</p>
            </div>
            <EmptyRow v-if="!invoiceRows.length" icon="invoice" text="Chưa có hóa đơn gần đây." />
          </div>
        </div>
      </section>
    </div>
  </section>
</template>

<script setup lang="ts">
import { computed, defineComponent, h, onMounted, ref } from 'vue'
import { RouterLink } from 'vue-router'
import {
  Activity,
  CalendarClock,
  CalendarDays,
  CalendarPlus,
  ChevronRight,
  FileHeart,
  MapPin,
  Pill,
  ReceiptText,
  Stethoscope,
  WalletCards,
} from 'lucide-vue-next'
import FullscreenLoader from '@/components/ui/FullscreenLoader.vue'
import { useAuthStore } from '@/stores/authStore'
import { appointmentApi } from '@/services/appointmentApi'
import { billingApi } from '@/services/billingApi'
import { medicalRecordApi } from '@/services/medicalRecordApi'
import { getApiErrorMessage } from '@/services/apiClient'
import type { Appointment } from '@/types/appointment'
import type { Invoice, Prescription } from '@/types/billing'
import type { Doctor } from '@/types/doctor'
import type { MedicalRecord } from '@/types/medicalRecord'

type TimelineData = { visits: unknown[]; medicalRecords: MedicalRecord[]; prescriptions: Prescription[] }
type TrendPoint = { key: string; value: number; x: number; y: number }

const authStore = useAuthStore()
const loading = ref(true)
const error = ref('')
const appointments = ref<Appointment[]>([])
const invoices = ref<Invoice[]>([])
const records = ref<MedicalRecord[]>([])
const prescriptions = ref<Prescription[]>([])
const doctorsList = ref<Doctor[]>([])
const MAX_DASHBOARD_ROWS = 5

const doctorNamesMap: Record<number, string> = {
  1: 'BS. Nguyễn Văn An',
  2: 'BS. Trần Thị Bình',
  3: 'BS. Lê Vân Châu',
  4: 'BS. Phạm Quốc Dũng',
  5: 'BS. Hoàng Thu Hà',
  6: 'BS. Đỗ Minh Khang',
  7: 'BS. Võ Lan Anh',
  8: 'BS. Nguyễn Đức Huy',
  9: 'BS. Bùi Thanh Tâm',
  10: 'BS. Trịnh Quang Minh',
}

const displayName = computed(() => authStore.user?.fullName || authStore.user?.username || 'bệnh nhân')

const upcomingAppointments = computed(() => appointments.value
  .filter(isUpcomingAppointment)
  .sort((a, b) => appointmentStartTimestamp(a) - appointmentStartTimestamp(b)))
const nextAppointment = computed(() => upcomingAppointments.value[0] || null)
const unpaidInvoices = computed(() => invoices.value.filter(isUnpaidInvoice))

const dashboardHeadline = computed(() => {
  if (nextAppointment.value) return 'Bạn có lịch khám sắp tới'
  if (unpaidInvoices.value.length) return 'Có viện phí cần theo dõi'
  if (prescriptions.value.length) return 'Đơn thuốc đã được cập nhật'
  return 'Dữ liệu sức khỏe đã sẵn sàng'
})

const dashboardNote = computed(() => {
  if (nextAppointment.value) {
    return `Lịch gần nhất vào ${formatDate(nextAppointment.value.appointmentDate)} lúc ${timeLabel(nextAppointment.value)}. Kiểm tra thông tin trước khi đến khám.`
  }
  if (unpaidInvoices.value.length) return 'Bạn đang có hóa đơn chưa thanh toán hoặc cần kiểm tra thêm trong mục Viện phí.'
  return 'Các bảng phía dưới giúp bạn theo dõi nhanh lịch khám, bệnh án, đơn thuốc và thanh toán.'
})

const compactMetrics = computed(() => [
  { label: 'Sắp tới', value: upcomingAppointments.value.length, icon: CalendarClock },
  { label: 'Bệnh án', value: records.value.length, icon: FileHeart },
  { label: 'Đơn thuốc', value: prescriptions.value.length, icon: Pill },
  { label: 'Hóa đơn', value: invoices.value.length, icon: ReceiptText },
])

const appointmentRows = computed(() => [...upcomingAppointments.value, ...appointments.value]
  .filter(uniqueBy(appointmentIdentity))
  .sort((a, b) => {
    const aUpcoming = isUpcomingAppointment(a) ? 0 : 1
    const bUpcoming = isUpcomingAppointment(b) ? 0 : 1
    if (aUpcoming !== bUpcoming) return aUpcoming - bUpcoming
    return appointmentStartTimestamp(a) - appointmentStartTimestamp(b)
  })
  .slice(0, MAX_DASHBOARD_ROWS))

const recordRows = computed(() => [...records.value]
  .sort((a, b) => dateTimestamp(b.examDate || b.createdAt || b.completedAt) - dateTimestamp(a.examDate || a.createdAt || a.completedAt))
  .slice(0, MAX_DASHBOARD_ROWS))

const prescriptionRows = computed(() => [...prescriptions.value]
  .sort((a, b) => dateTimestamp(b.createdAt || b.submittedAt || b.examDate) - dateTimestamp(a.createdAt || a.submittedAt || a.examDate))
  .slice(0, MAX_DASHBOARD_ROWS))

const invoiceRows = computed(() => [...invoices.value]
  .sort((a, b) => dateTimestamp(b.createdAt || b.paidAt) - dateTimestamp(a.createdAt || a.paidAt))
  .slice(0, MAX_DASHBOARD_ROWS))

const monthlyActivity = computed(() => {
  const months = lastMonths(6)

  return months.map((month) => {
    const appointmentCount = countByMonth(appointments.value, appointment => appointment.appointmentDate || appointment.createdAt, month.key)
    const recordCount = countByMonth(records.value, record => record.examDate || record.createdAt || record.completedAt, month.key)
    return {
      ...month,
      appointmentCount,
      recordCount,
      total: appointmentCount + recordCount,
    }
  })
})

const trendChart = computed(() => {
  const top = 32
  const baseline = 228
  const left = 52
  const right = 668
  const months = monthlyActivity.value
  const maxValue = Math.max(1, ...months.flatMap(month => [month.appointmentCount, month.recordCount]))
  const step = months.length > 1 ? (right - left) / (months.length - 1) : 0
  const toPoint = (value: number, index: number, key: string) => ({
    key,
    value,
    x: left + index * step,
    y: baseline - (value / maxValue) * (baseline - top),
  })
  const appointmentPoints = months.map((month, index) => toPoint(month.appointmentCount, index, month.key))
  const recordPoints = months.map((month, index) => toPoint(month.recordCount, index, month.key))

  return {
    appointmentPoints,
    recordPoints,
    labels: months.map((month, index) => ({ key: month.key, label: month.label, x: left + index * step })),
    gridLines: [40, 86, 132, 178, 224],
    appointmentPath: smoothPath(appointmentPoints),
    recordPath: smoothPath(recordPoints),
    appointmentArea: areaPath(appointmentPoints, baseline),
    recordArea: areaPath(recordPoints, baseline),
  }
})

const appointmentStatusSegments = computed(() => {
  if (!appointments.value.length) return [{ label: 'Chưa có dữ liệu', color: '#e2e8f0', value: 1 }]

  const groups = new Map<string, { label: string; color: string; value: number }>()
  appointments.value.forEach((appointment) => {
    const label = appointmentStatusLabel(appointment.status)
    const current = groups.get(label) || { label, color: appointmentStatusColor(appointment.status), value: 0 }
    current.value += 1
    groups.set(label, current)
  })

  return Array.from(groups.values()).sort((a, b) => b.value - a.value)
})

const appointmentDonutGradient = computed(() => {
  const total = Math.max(appointments.value.length, 1)
  let current = 0
  const stops = appointmentStatusSegments.value.map((segment) => {
    const start = current
    current += (segment.value / total) * 100
    return `${segment.color} ${start}% ${current}%`
  })
  return `conic-gradient(${stops.join(', ') || '#e2e8f0 0% 100%'})`
})

const financeSummary = computed(() => {
  const activeInvoices = invoices.value.filter(invoice => !isCancelledStatus(invoice.status))
  const total = activeInvoices.reduce((sum, invoice) => sum + invoiceAmount(invoice), 0)
  const paid = activeInvoices.reduce((sum, invoice) => sum + invoicePaidAmount(invoice), 0)
  const unpaid = Math.max(total - paid, 0)
  return {
    total,
    paid,
    unpaid,
    paidPercent: total > 0 ? Math.min(100, Math.round((paid / total) * 100)) : 0,
  }
})

const TableHeader = defineComponent({
  props: {
    title: { type: String, required: true },
    subtitle: { type: String, required: true },
    to: { type: String, required: true },
    action: { type: String, required: true },
  },
  setup(props) {
    return () => h('div', { class: 'flex flex-col gap-2.5 border-b border-slate-100 px-4 py-3 sm:flex-row sm:items-center sm:justify-between' }, [
      h('div', [
        h('h2', { class: 'text-base font-bold text-slate-950' }, props.title),
        h('p', { class: 'mt-0.5 text-xs text-slate-500' }, props.subtitle),
      ]),
      h(RouterLink, { to: props.to, class: 'inline-flex items-center gap-1 text-xs font-bold text-[#003c90] hover:text-[#0F52BA]' }, {
        default: () => [props.action, h(ChevronRight, { class: 'h-4 w-4' })],
      }),
    ])
  },
})

const EmptyRow = defineComponent({
  props: {
    icon: { type: String, required: true },
    text: { type: String, required: true },
  },
  setup(props) {
    const iconMap: Record<string, any> = {
      calendar: CalendarClock,
      pill: Pill,
      record: FileHeart,
      invoice: ReceiptText,
    }
    return () => h('div', { class: 'px-4 py-6 text-center text-xs text-slate-500' }, [
      h(iconMap[props.icon] || Activity, { class: 'mx-auto mb-2 h-8 w-8 text-slate-300' }),
      props.text,
    ])
  },
})

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

    const [appts, timeline, invs, prescriptionList, doctors] = await Promise.all([
      Number.isFinite(patientId) && patientId > 0
        ? appointmentApi.getAppointmentsByPatient(patientId).catch(() => [] as Appointment[])
        : Promise.resolve([] as Appointment[]),
      medicalRecordApi.getCurrentPatientClinicalTimeline().catch((err) => {
        if ((err as any)?.response?.status === 404) return { visits: [], medicalRecords: [], prescriptions: [] } as TimelineData
        throw err
      }),
      Number.isFinite(patientId) && patientId > 0
        ? billingApi.getInvoices(patientId).catch((err) => {
          if ((err as any)?.response?.status === 404) return [] as Invoice[]
          throw err
        })
        : Promise.resolve([] as Invoice[]),
      Number.isFinite(patientId) && patientId > 0
        ? billingApi.getPrescriptions(patientId).catch(() => [] as Prescription[])
        : Promise.resolve([] as Prescription[]),
      appointmentApi.getDoctors().catch(() => [] as Doctor[]),
    ])

    appointments.value = uniqueList(appts, appointmentIdentity)
    records.value = uniqueList(timeline.medicalRecords || [], recordIdentity)
    prescriptions.value = mergePrescriptions(timeline.prescriptions || [], prescriptionList)
    invoices.value = uniqueList(invs, invoiceIdentity)
    doctorsList.value = doctors
  } catch (err) {
    const status = (err as any)?.response?.status
    error.value = status === 403
      ? 'Bạn không có quyền xem dữ liệu bệnh nhân này. Vui lòng đăng xuất rồi đăng nhập lại.'
      : getApiErrorMessage(err)
  } finally {
    loading.value = false
  }
}

function uniqueList<T>(items: T[], keyGetter: (item: T) => string) {
  const seen = new Set<string>()
  return items.filter((item) => {
    const key = keyGetter(item)
    if (!key || seen.has(key)) return false
    seen.add(key)
    return true
  })
}

function uniqueBy<T>(keyGetter: (item: T) => string) {
  const seen = new Set<string>()
  return (item: T) => {
    const key = keyGetter(item)
    if (!key || seen.has(key)) return false
    seen.add(key)
    return true
  }
}

function appointmentIdentity(appointment: Appointment & Record<string, any>) {
  return String(appointment.appointmentId || appointment.AppointmentId || appointment.appointmentCode || appointment.AppointmentCode || appointment.createdAt || '')
}

function recordIdentity(record: MedicalRecord & Record<string, any>) {
  return String(record.medicalRecordId || record.MedicalRecordId || record.recordId || record.RecordId || record.medicalRecordCode || record.medicalRecordIdCode || record.id || '')
}

function invoiceIdentity(invoice: Invoice & Record<string, any>) {
  return String(invoice.invoiceId || invoice.InvoiceId || invoice.invoiceCode || invoice.invoiceIdCode || invoice.id || '')
}

function medicalRecordDisplayCode(record: MedicalRecord & Record<string, any>) {
  return String(record.medicalRecordCode || record.medicalRecordIdCode || record.recordIdCode || record.recordId || record.medicalRecordId || record.id || '-')
}

function getDoctorName(doctorId?: number | string) {
  if (!doctorId) return ''
  const docId = Number(doctorId)
  if (!Number.isFinite(docId) || docId <= 0) return ''

  const appointment = appointments.value.find(item => Number(item.doctorId) === docId)
  if (appointment?.doctorName) return appointment.doctorName

  const doctor = doctorsList.value.find(item => Number(item.doctorId) === docId)
  if (doctor?.doctorName || doctor?.fullName) return doctor.doctorName || doctor.fullName || ''

  return doctorNamesMap[docId] || `Bác sĩ #${docId}`
}

function appointmentForRecord(record?: MedicalRecord | null) {
  if (!record) return null
  const appointmentId = Number(record.appointmentId || 0)
  if (appointmentId) {
    const direct = appointments.value.find(appointment => Number(appointment.appointmentId) === appointmentId)
    if (direct) return direct
  }

  const recordDate = normalizeDateOnly(record.examDate || record.createdAt || record.completedAt)
  return appointments.value.find((appointment) => {
    const doctorMatches = !record.doctorId || Number(appointment.doctorId) === Number(record.doctorId)
    const dateMatches = !recordDate || normalizeDateOnly(appointment.appointmentDate) === recordDate
    return doctorMatches && dateMatches
  }) || null
}

function doctorNameForRecord(record?: MedicalRecord | null) {
  if (!record) return ''
  const appointment = appointmentForRecord(record)
  return record.doctorName || appointment?.doctorName || getDoctorName(record.doctorId || appointment?.doctorId) || ''
}

function invoiceDisplayCode(invoice: Invoice & Record<string, any>) {
  const code = invoice.invoiceCode || invoice.invoiceIdCode || invoice.InvoiceCode || invoice.InvoiceIdCode
  if (code) return String(code)
  const id = invoice.invoiceId || invoice.id
  return id ? `HĐ${String(id).padStart(3, '0')}` : 'HĐ'
}

function appointmentInvoiceLabel(invoice: Invoice & Record<string, any>) {
  const id = invoice.appointmentId || invoice.AppointmentId
  return id ? `LH${String(id).padStart(3, '0')}` : '-'
}

function prescriptionDisplayCode(prescription: Prescription & Record<string, any>) {
  const code = prescription.prescriptionCode || prescription.PrescriptionCode || prescription.prescriptionIdCode || prescription.PrescriptionIdCode
  if (code) return String(code)
  const id = prescription.prescriptionId || prescription.PrescriptionId || prescription.id || prescription.Id
  return id ? `DT${String(id).padStart(3, '0')}` : 'Chưa cập nhật'
}

function prescriptionKey(prescription: Prescription & Record<string, any>) {
  return prescriptionMergeKeys(prescription)[0] || `${prescriptionDisplayCode(prescription)}-${prescription.createdAt || prescription.submittedAt || prescription.note || ''}`
}

function mergePrescriptions(n2List: Prescription[], n3List: Prescription[]) {
  const mergedMap = new Map<string, Prescription>()

  for (const item of n2List) {
    const normalized = normalizePrescription(item)
    const keys = prescriptionMergeKeys(normalized)
    if (!keys.length) {
      mergedMap.set(`n2-${mergedMap.size}`, normalized)
      continue
    }
    for (const key of keys) mergedMap.set(key, normalized)
  }

  for (const item of n3List) {
    const normalized = normalizePrescription(item)
    const keys = prescriptionMergeKeys(normalized)
    const existing = keys.map(key => mergedMap.get(key)).find(Boolean)

    if (!existing) {
      if (!keys.length) {
        mergedMap.set(`n3-${mergedMap.size}`, normalized)
      } else {
        for (const key of keys) mergedMap.set(key, normalized)
      }
      continue
    }

    const merged = mergePrescription(existing, normalized)
    for (const key of new Set([...prescriptionMergeKeys(existing), ...keys])) {
      mergedMap.set(key, merged)
    }
  }

  return Array.from(new Set(mergedMap.values()))
    .sort((a, b) => dateTimestamp(b.createdAt || b.submittedAt || b.examDate) - dateTimestamp(a.createdAt || a.submittedAt || a.examDate))
}

function normalizePrescription(prescription: Prescription & Record<string, any>) {
  return {
    ...prescription,
    id: prescription.id || prescription.Id || prescription.prescriptionId || prescription.PrescriptionId,
    prescriptionId: prescription.prescriptionId || prescription.PrescriptionId || prescription.id || prescription.Id,
    prescriptionCode: prescription.prescriptionCode || prescription.PrescriptionCode,
    prescriptionIdCode: prescription.prescriptionIdCode || prescription.PrescriptionIdCode,
    medicalRecordId: prescription.medicalRecordId || prescription.MedicalRecordId,
    medicalRecordCode: prescription.medicalRecordCode || prescription.MedicalRecordCode,
    appointmentId: prescription.appointmentId || prescription.AppointmentId,
    items: prescription.items || prescription.prescriptionItems || prescription.Items || prescription.PrescriptionItems || [],
  } as Prescription
}

function prescriptionMergeKeys(prescription: Prescription & Record<string, any>) {
  return [
    prescription.prescriptionId ? `id-${prescription.prescriptionId}` : '',
    prescription.id ? `id-${prescription.id}` : '',
    prescription.prescriptionCode ? `code-${prescription.prescriptionCode}` : '',
    prescription.prescriptionIdCode ? `code-${prescription.prescriptionIdCode}` : '',
    prescription.medicalRecordId ? `medid-${prescription.medicalRecordId}` : '',
    prescription.medicalRecordCode ? `medcode-${prescription.medicalRecordCode}` : '',
    prescription.appointmentId ? `appt-${prescription.appointmentId}` : '',
  ].filter(Boolean)
}

function mergePrescription(base: Prescription, incoming: Prescription) {
  const baseItems = base.items || base.prescriptionItems || []
  const incomingItems = incoming.items || incoming.prescriptionItems || []
  return {
    ...base,
    ...incoming,
    id: base.id || incoming.id,
    prescriptionId: base.prescriptionId || incoming.prescriptionId,
    prescriptionCode: base.prescriptionCode || incoming.prescriptionCode,
    prescriptionIdCode: base.prescriptionIdCode || incoming.prescriptionIdCode,
    medicalRecordId: base.medicalRecordId || incoming.medicalRecordId,
    medicalRecordCode: base.medicalRecordCode || incoming.medicalRecordCode,
    appointmentId: base.appointmentId || incoming.appointmentId,
    items: incomingItems.length ? incomingItems : baseItems,
    prescriptionItems: incomingItems.length ? incomingItems : baseItems,
    note: incoming.note || base.note,
    status: incoming.status || base.status,
  } as Prescription
}

function prescriptionMedicineText(prescription: Prescription & Record<string, any>) {
  const items = prescription.items || prescription.prescriptionItems || prescription.Items || prescription.PrescriptionItems || []
  const names = items.map((item: Record<string, any>) => item.medicineNameSnapshot || item.MedicineNameSnapshot || item.medicineName || item.MedicineName).filter(Boolean)
  if (names.length) return names.slice(0, 3).join(', ')
  return prescription.note || 'Chưa có thông tin thuốc'
}

function isUpcomingAppointment(appointment: Appointment & Record<string, any>) {
  if (isClosedAppointmentStatus(appointment.status || appointment.Status)) return false
  const timestamp = appointmentStartTimestamp(appointment)
  return Number.isFinite(timestamp) && timestamp >= Date.now()
}

function appointmentStartTimestamp(appointment: Appointment & Record<string, any>) {
  const scheduledAt = appointment.scheduledAt || appointment.ScheduledAt
  if (scheduledAt) {
    const scheduledTime = new Date(String(scheduledAt)).getTime()
    if (Number.isFinite(scheduledTime)) return scheduledTime
  }

  const dateOnly = normalizeDateOnly(appointment.appointmentDate || appointment.AppointmentDate)
  if (!dateOnly) return Number.NaN
  const timeText = String(appointment.slotTime || appointment.SlotTime || '00:00').slice(0, 5)
  const time = /^\d{1,2}:\d{2}$/.test(timeText) ? timeText : '00:00'
  return new Date(`${dateOnly}T${time}:00`).getTime()
}

function isClosedAppointmentStatus(status?: string | number) {
  const value = normalizeText(status)
  return value.includes('completed')
    || value.includes('complete')
    || value.includes('done')
    || value.includes('hoan')
    || value.includes('cancel')
    || value.includes('huy')
    || value.includes('noshow')
    || value.includes('no show')
    || value.includes('expired')
    || value.includes('qua han')
}

function isCancelledStatus(status?: string | number) {
  const value = normalizeText(status)
  return value.includes('cancel') || value.includes('huy')
}

function isPaidStatus(status?: string | number) {
  const value = normalizeText(status)
  if (!value || value.includes('unpaid') || value.includes('chua thanh toan')) return false
  return value.includes('paid') || value.includes('da thanh toan')
}

function isUnpaidInvoice(invoice: Invoice & Record<string, any>) {
  if (isCancelledStatus(invoice.status || invoice.Status)) return false
  const amount = invoiceAmount(invoice)
  const paid = invoicePaidAmount(invoice)
  if (amount > 0) return paid < amount
  return !isPaidStatus(invoice.status || invoice.Status)
}

function invoiceAmount(invoice: Invoice & Record<string, any>) {
  return toNumber(invoice.totalAmount, invoice.TotalAmount, invoice.amount, invoice.Amount, invoice.examinationFee, invoice.ExaminationFee, invoice.examFee, invoice.ExamFee)
}

function invoicePaidAmount(invoice: Invoice & Record<string, any>) {
  const explicit = toNumber(invoice.paidAmount, invoice.PaidAmount)
  if (explicit > 0) return explicit
  const payments = Array.isArray(invoice.payments) ? invoice.payments : Array.isArray(invoice.Payments) ? invoice.Payments : []
  const paymentTotal = payments.reduce((sum: number, payment: Record<string, any>) => sum + toNumber(payment.amount, payment.Amount), 0)
  if (paymentTotal > 0) return paymentTotal
  if (isPaidStatus(invoice.status || invoice.Status)) return invoiceAmount(invoice)
  return 0
}

function timeLabel(appointment: Appointment & Record<string, any>) {
  return String(appointment.slotTime || appointment.SlotTime || '--:--').slice(0, 5)
}

function lastMonths(count: number) {
  const formatter = new Intl.DateTimeFormat('vi-VN', { month: '2-digit', year: '2-digit' })
  const now = new Date()
  return Array.from({ length: count }, (_, index) => {
    const date = new Date(now.getFullYear(), now.getMonth() - (count - 1 - index), 1)
    return {
      key: `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}`,
      label: formatter.format(date),
    }
  })
}

function countByMonth<T>(items: T[], dateGetter: (item: T) => unknown, monthKey: string) {
  return items.filter(item => normalizeDateOnly(dateGetter(item)).startsWith(monthKey)).length
}

function smoothPath(points: TrendPoint[]) {
  if (!points.length) return ''
  if (points.length === 1) return `M ${points[0].x} ${points[0].y}`

  return points.slice(1).reduce((path, point, index) => {
    const previous = points[index]
    const distance = point.x - previous.x
    const controlA = previous.x + distance * 0.45
    const controlB = point.x - distance * 0.45
    return `${path} C ${controlA} ${previous.y}, ${controlB} ${point.y}, ${point.x} ${point.y}`
  }, `M ${points[0].x} ${points[0].y}`)
}

function areaPath(points: TrendPoint[], baseline: number) {
  if (!points.length) return ''
  const line = smoothPath(points)
  const first = points[0]
  const last = points[points.length - 1]
  return `${line} L ${last.x} ${baseline} L ${first.x} ${baseline} Z`
}

function normalizeDateOnly(value: unknown) {
  const text = String(value || '').trim()
  if (!text) return ''
  const isoMatch = text.match(/^(\d{4})-(\d{2})-(\d{2})/)
  if (isoMatch) return `${isoMatch[1]}-${isoMatch[2]}-${isoMatch[3]}`
  const viMatch = text.match(/^(\d{1,2})[/-](\d{1,2})[/-](\d{4})/)
  if (viMatch) return `${viMatch[3]}-${viMatch[2].padStart(2, '0')}-${viMatch[1].padStart(2, '0')}`
  const parsed = new Date(text)
  return Number.isNaN(parsed.getTime()) ? '' : parsed.toISOString().slice(0, 10)
}

function dateTimestamp(value: unknown) {
  const timestamp = new Date(String(value || '')).getTime()
  return Number.isNaN(timestamp) ? 0 : timestamp
}

function toNumber(...values: unknown[]) {
  for (const value of values) {
    const numberValue = Number(value)
    if (Number.isFinite(numberValue) && numberValue > 0) return numberValue
  }
  return 0
}

function normalizeText(value: unknown) {
  return String(value || '')
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .replace(/đ/g, 'd')
    .replace(/Đ/g, 'D')
    .toLowerCase()
    .trim()
}

function formatCurrency(value: number) {
  return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(Number(value || 0))
}

function formatDate(value?: unknown) {
  if (!value) return 'Chưa cập nhật'
  const text = String(value)
  const date = new Date(text)
  return Number.isNaN(date.getTime()) ? text : new Intl.DateTimeFormat('vi-VN').format(date)
}

function appointmentStatusLabel(status?: string) {
  const value = normalizeText(status)
  if (value.includes('checked') || value.includes('check-in')) return 'Đã check-in'
  if (value.includes('progress') || value.includes('dang kham')) return 'Đang khám'
  if (value.includes('confirmed') || value.includes('xac nhan')) return 'Đã xác nhận'
  if (value.includes('completed') || value.includes('done') || value.includes('hoan tat')) return 'Hoàn tất'
  if (value.includes('noshow') || value.includes('no show')) return 'Không đến khám'
  if (value.includes('expired') || value.includes('qua han')) return 'Đã quá hạn'
  if (value.includes('pending') || value.includes('waiting') || value.includes('cho')) return 'Đang chờ'
  if (value.includes('cancel') || value.includes('huy')) return 'Đã hủy'
  return status || 'Chưa cập nhật'
}

function appointmentStatusClass(status?: string) {
  const value = normalizeText(status)
  if (value.includes('cancel') || value.includes('huy')) return 'bg-rose-50 text-rose-700 border border-rose-100'
  if (value.includes('completed') || value.includes('done') || value.includes('hoan tat')) return 'bg-emerald-50 text-emerald-700 border border-emerald-100'
  if (value.includes('progress') || value.includes('dang kham')) return 'bg-indigo-50 text-indigo-700 border border-indigo-100'
  if (value.includes('checked') || value.includes('check-in')) return 'bg-cyan-50 text-cyan-700 border border-cyan-100'
  if (value.includes('confirmed') || value.includes('xac nhan')) return 'bg-blue-50 text-blue-700 border border-blue-100'
  if (value.includes('pending') || value.includes('waiting') || value.includes('cho')) return 'bg-amber-50 text-amber-700 border border-amber-100'
  return 'bg-slate-50 text-slate-700 border border-slate-100'
}

function appointmentStatusColor(status?: string) {
  const value = normalizeText(status)
  if (value.includes('cancel') || value.includes('huy')) return '#f43f5e'
  if (value.includes('completed') || value.includes('done') || value.includes('hoan tat')) return '#10b981'
  if (value.includes('progress') || value.includes('dang kham')) return '#6366f1'
  if (value.includes('checked') || value.includes('check-in')) return '#06b6d4'
  if (value.includes('confirmed') || value.includes('xac nhan')) return '#0F52BA'
  if (value.includes('pending') || value.includes('waiting') || value.includes('cho')) return '#f59e0b'
  return '#94a3b8'
}

function recordStatusLabel(status?: string) {
  const value = normalizeText(status)
  if (value.includes('completed') || value.includes('done') || value.includes('hoan tat')) return 'Đã hoàn tất'
  if (value.includes('draft') || value.includes('ban nhap')) return 'Bản nháp'
  if (value.includes('progress') || value.includes('dang xu ly')) return 'Đang xử lý'
  if (value.includes('pending') || value.includes('waiting') || value.includes('cho')) return 'Chờ xử lý'
  if (value.includes('cancel') || value.includes('huy')) return 'Đã hủy'
  return status || 'Chưa cập nhật'
}

function recordStatusClass(status?: string) {
  const value = normalizeText(status)
  if (value.includes('completed') || value.includes('done') || value.includes('hoan tat')) return 'bg-emerald-50 text-emerald-700 border border-emerald-100'
  if (value.includes('draft') || value.includes('ban nhap') || value.includes('pending') || value.includes('waiting') || value.includes('cho')) return 'bg-amber-50 text-amber-700 border border-amber-100'
  if (value.includes('progress') || value.includes('dang xu ly')) return 'bg-blue-50 text-blue-700 border border-blue-100'
  if (value.includes('cancel') || value.includes('huy')) return 'bg-rose-50 text-rose-700 border border-rose-100'
  return 'bg-slate-50 text-slate-700 border border-slate-100'
}

function prescriptionStatusLabel(status?: string) {
  const bucket = prescriptionStatusBucket(status)
  if (bucket === 'sent') return 'Đã gửi nhà thuốc'
  if (bucket === 'ready') return 'Sẵn sàng phát thuốc'
  if (bucket === 'pending') return 'Chờ xử lý'
  if (bucket === 'processing') return 'Đang xử lý'
  if (bucket === 'partial') return 'Thiếu một phần'
  if (bucket === 'outOfStock') return 'Thiếu thuốc'
  if (bucket === 'dispensed') return 'Đã phát thuốc'
  if (bucket === 'completed') return 'Hoàn tất'
  if (bucket === 'cancelled') return 'Đã hủy'
  return status || 'Chưa cập nhật'
}

function prescriptionStatusClass(status?: string) {
  const bucket = prescriptionStatusBucket(status)
  if (bucket === 'sent') return 'bg-cyan-50 text-cyan-700 border border-cyan-100'
  if (bucket === 'ready') return 'bg-sky-50 text-sky-700 border border-sky-100'
  if (bucket === 'pending' || bucket === 'processing') return 'bg-amber-50 text-amber-700 border border-amber-100'
  if (bucket === 'partial' || bucket === 'outOfStock') return 'bg-orange-50 text-orange-700 border border-orange-100'
  if (bucket === 'dispensed' || bucket === 'completed') return 'bg-emerald-50 text-emerald-700 border border-emerald-100'
  if (bucket === 'cancelled') return 'bg-rose-50 text-rose-700 border border-rose-100'
  return 'bg-slate-50 text-slate-700 border border-slate-100'
}

function prescriptionStatusBucket(status?: string) {
  const value = normalizeText(status)
  if (!value) return 'unknown'
  if (value.includes('cancel') || value.includes('huy')) return 'cancelled'
  if (value.includes('dispensed') || value.includes('da phat') || value.includes('cap thuoc')) return 'dispensed'
  if (value.includes('readytodispense') || value.includes('ready_to_dispense') || value.includes('ready') || value.includes('san sang')) return 'ready'
  if (value.includes('partiallyavailable') || value.includes('partial') || value.includes('mot phan')) return 'partial'
  if (value.includes('outofstock') || value.includes('out_of_stock') || value.includes('out of stock') || value.includes('thieu')) return 'outOfStock'
  if (value.includes('senttopharmacy') || value.includes('sent_to_pharmacy') || value.includes('sent to pharmacy') || value.includes('sent') || value.includes('gui') || value.includes('nha thuoc')) return 'sent'
  if (value.includes('processing') || value.includes('progress') || value.includes('dang xu ly')) return 'processing'
  if (value.includes('completed') || value.includes('done') || value.includes('hoan tat')) return 'completed'
  if (value.includes('pending') || value.includes('waiting') || value.includes('cho')) return 'pending'
  return 'unknown'
}

function invoiceStatusLabel(status?: string) {
  if (isPaidStatus(status)) return 'Đã thanh toán'
  if (isCancelledStatus(status)) return 'Đã hủy'
  return 'Chưa thanh toán'
}

function invoiceStatusClass(status?: string) {
  if (isPaidStatus(status)) return 'bg-emerald-50 text-emerald-700 border border-emerald-100'
  if (isCancelledStatus(status)) return 'bg-rose-50 text-rose-700 border border-rose-100'
  const value = normalizeText(status)
  if (value.includes('processing') || value.includes('dang xu')) return 'bg-blue-50 text-blue-700 border border-blue-100'
  if (value.includes('pending') || value.includes('cho')) return 'bg-amber-50 text-amber-700 border border-amber-100'
  return 'bg-amber-50 text-amber-700 border border-amber-100'
}

</script>
