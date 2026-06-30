<template>
  <div class="clinical-portal-shell flex h-screen overflow-hidden bg-[#f4f7fb] text-slate-950">
    <aside
      class="relative hidden shrink-0 flex-col border-r border-slate-200 bg-white/85 shadow-[12px_0_40px_rgba(15,23,42,0.04)] backdrop-blur transition-all duration-300 lg:flex"
      :class="sidebarCollapsed ? 'w-20' : 'w-60'"
    >
      <button
        type="button"
        class="absolute -right-3.5 top-7 z-20 flex h-7 w-7 items-center justify-center rounded-full border border-slate-200 bg-white text-slate-500 shadow-sm transition hover:text-[#0F52BA]"
        :aria-label="sidebarCollapsed ? 'Mở rộng menu' : 'Thu gọn menu'"
        @click="sidebarCollapsed = !sidebarCollapsed"
      >

        <ChevronRight v-if="sidebarCollapsed" class="h-5 w-5" />
        <ChevronLeft v-else class="h-5 w-5" />
      </button>

      <RouterLink to="/" class="flex h-20 items-center px-3" :class="sidebarCollapsed ? 'justify-center px-0' : ''">
        <img v-if="sidebarCollapsed" :src="relIconUrl" alt="MedicareDNU" class="h-11 w-11 rounded-xl object-contain" />
        <img v-else :src="logoUrl" alt="MedicareDNU" class="h-16 w-full object-contain" />
      </RouterLink>

      <nav class="flex-1 space-y-5 overflow-y-auto px-4 pb-5 pt-4">
        <template v-for="group in menuGroups" :key="group.title || 'main'">
          <div>
            <p v-if="group.title && !sidebarCollapsed" class="mb-2 px-3 text-[11px] font-bold uppercase tracking-[0.12em] text-slate-400">
              {{ group.title }}
            </p>
            <div class="space-y-2">
              <RouterLink
                v-for="item in group.items"
                :key="item.to"
                :to="item.to"
                class="group flex min-h-11 items-center gap-3 rounded-xl px-4 py-2.5 text-sm font-medium text-slate-700 transition duration-200 hover:bg-blue-50 hover:text-[#003c90]"
                :class="sidebarCollapsed ? 'justify-center px-3' : ''"
                active-class="!bg-[#0F52BA] !text-white shadow-[0_14px_34px_rgba(15,82,186,0.28)] hover:!bg-[#0F52BA] hover:!text-white"
                :title="sidebarCollapsed ? item.label : undefined"
              >
                <component
                  :is="item.icon"
                  class="h-5 w-5 shrink-0 transition"
                  :class="isActive(item.to) ? '!text-white' : 'text-slate-700 group-hover:text-[#003c90]'"
                />
                <span v-if="!sidebarCollapsed">{{ item.label }}</span>
              </RouterLink>
            </div>
          </div>
        </template>
      </nav>

      <div class="border-t border-slate-200 px-4 py-5">
        <button
          type="button"
          class="flex min-h-11 w-full items-center gap-3 rounded-xl px-4 py-2.5 text-sm font-semibold text-red-600 transition hover:bg-red-50"
          :class="sidebarCollapsed ? 'justify-center px-3' : ''"
          @click="handleLogout"
        >
          <LogOut class="h-5 w-5" />
          <span v-if="!sidebarCollapsed">Đăng xuất</span>
        </button>
      </div>
    </aside>

    <div class="flex min-w-0 flex-1 flex-col">
      <header class="flex h-20 shrink-0 items-center justify-between gap-4 border-b border-slate-200 bg-white px-4 sm:px-5 lg:px-6">
        <div class="flex items-center gap-3 min-w-0">
          <button
            type="button"
            class="inline-flex h-10 w-10 items-center justify-center rounded-xl border border-slate-200 bg-white text-slate-600 shadow-sm lg:hidden"
            @click="mobileMenuOpen = true"
          >
            <Menu class="h-6 w-6" />
          </button>

          <div
            :key="route.fullPath"
            class="hidden min-w-0 items-center gap-3.5 rounded-2xl border border-slate-100 bg-slate-50/50 px-4 py-2 shadow-sm backdrop-blur-sm transition-all duration-300 hover:shadow-md hover:bg-slate-50/80 md:flex"
          >
            <div class="shrink-0">
              <!-- Morning (Sunrise) -->
              <div v-if="timePeriod === 'morning'" class="relative flex h-11 w-11 items-center justify-center overflow-hidden rounded-xl bg-gradient-to-br from-amber-400 to-orange-500 shadow-sm shadow-orange-300/30">
                <div class="absolute inset-0 bg-[radial-gradient(circle,rgba(255,255,255,0.45)_0%,transparent_70%)] animate-pulse-glow"></div>
                <Sunrise class="z-10 h-6 w-6 text-white animate-sunrise-bounce" />
              </div>

              <!-- Noon (Sun) -->
              <div v-else-if="timePeriod === 'noon'" class="relative flex h-11 w-11 items-center justify-center overflow-hidden rounded-xl bg-gradient-to-br from-amber-300 via-yellow-400 to-orange-400 shadow-sm shadow-yellow-300/30">
                <div class="absolute h-16 w-16 rounded-full bg-yellow-200/40 blur-md animate-pulse-glow"></div>
                <Sun class="z-10 h-6 w-6 text-white animate-spin-slow" />
              </div>

              <!-- Afternoon (Sunset) -->
              <div v-else-if="timePeriod === 'afternoon'" class="relative flex h-11 w-11 items-center justify-center overflow-hidden rounded-xl bg-gradient-to-br from-orange-400 to-rose-500 shadow-sm shadow-rose-300/30">
                <div class="absolute inset-0 bg-[radial-gradient(circle,rgba(255,255,255,0.3)_0%,transparent_60%)]"></div>
                <Sunset class="z-10 h-6 w-6 text-white animate-sunset-dip" />
              </div>

              <!-- Evening (Moon & Stars) -->
              <div v-else-if="timePeriod === 'evening'" class="relative flex h-11 w-11 items-center justify-center overflow-hidden rounded-xl bg-gradient-to-br from-indigo-950 via-purple-900 to-indigo-800 shadow-sm shadow-indigo-950/20">
                <div class="absolute left-2.5 top-2 h-0.5 w-0.5 rounded-full bg-white animate-twinkle" style="animation-delay: 0.2s"></div>
                <div class="absolute bottom-2.5 right-2.5 h-1 w-1 rounded-full bg-white animate-twinkle" style="animation-delay: 0.8s"></div>
                <div class="absolute right-2 top-3 h-0.5 w-0.5 rounded-full bg-white animate-twinkle" style="animation-delay: 1.4s"></div>
                <Moon class="z-10 h-5.5 w-5.5 text-amber-200 fill-amber-100 animate-float-gentle" />
              </div>

              <!-- Late Night (Sparkles/Night Stars) -->
              <div v-else class="relative flex h-11 w-11 items-center justify-center overflow-hidden rounded-xl bg-gradient-to-br from-slate-950 via-slate-900 to-indigo-950 shadow-sm shadow-slate-950/40">
                <div class="absolute right-3.5 top-2.5 h-0.5 w-0.5 rounded-full bg-white animate-twinkle" style="animation-delay: 0.1s"></div>
                <div class="absolute bottom-2 left-2.5 h-0.5 w-0.5 rounded-full bg-white animate-twinkle" style="animation-delay: 0.6s"></div>
                <div class="absolute left-2 top-3.5 h-1 w-1 rounded-full bg-white animate-twinkle" style="animation-delay: 1.1s"></div>
                <div class="absolute bottom-3 right-2 h-0.5 w-0.5 rounded-full bg-white animate-twinkle" style="animation-delay: 1.7s"></div>
                <Sparkles class="z-10 h-5.5 w-5.5 text-indigo-300 animate-float-slow" />
              </div>
            </div>

            <div class="min-w-0">
              <p class="truncate text-sm font-bold text-slate-800">
                {{ greetingData.text }}, <span class="text-[#0F52BA]">{{ displayName }}</span>!
              </p>
              <p class="truncate text-[11px] font-semibold text-slate-400">
                MedicareDNU kính chào
              </p>
            </div>
          </div>
        </div>

        <!-- Middle: Clock & Weather Widgets -->
        <div class="hidden items-center gap-4 lg:flex">
          <!-- Live Clock & Calendar Widget -->
          <div class="flex items-center gap-3 rounded-2xl border border-slate-100 bg-slate-50/30 px-4 py-2 shadow-sm backdrop-blur-sm hover:bg-slate-50/50 transition duration-300">
            <div class="flex h-9 w-9 items-center justify-center rounded-xl bg-blue-50 text-[#0F52BA]">
              <Clock class="h-5 w-5" />
            </div>
            <div class="text-left">
              <div class="flex items-center text-sm font-extrabold text-slate-800 tabular-nums">
                <span>{{ clockHours }}</span>
                <span class="mx-0.5 animate-pulse-colon text-[#0F52BA]">:</span>
                <span>{{ clockMinutes }}</span>
                <span class="ml-1 text-[10px] font-semibold text-slate-400">{{ clockSeconds }}</span>
              </div>
              <div class="text-[10px] font-bold text-slate-500">
                {{ vietnameseDate }}
              </div>
            </div>
          </div>

          <!-- Weather Widget -->
          <div class="flex items-center gap-3 rounded-2xl border border-slate-100 bg-slate-50/30 px-4 py-2 shadow-sm backdrop-blur-sm hover:bg-slate-50/50 transition duration-300">
            <div class="flex h-9 w-9 items-center justify-center rounded-xl bg-amber-50/60" :class="weatherData.iconColor">
              <component :is="timePeriod === 'evening' || timePeriod === 'night' ? Moon : Sun" class="h-5 w-5" :class="timePeriod === 'noon' ? 'animate-spin-slow' : 'animate-pulse-glow'" />
            </div>
            <div class="text-left">
              <p class="text-sm font-extrabold text-slate-800">
                {{ weatherData.temp }}
              </p>
              <p class="text-[10px] font-bold text-slate-500">
                {{ weatherData.desc }} • Hà Nội
              </p>
            </div>
          </div>
        </div>

        <!-- Right-Middle: Role-Based Utility Action / Status Card -->
        <div class="hidden items-center gap-2 xl:flex">
          <!-- Doctor Status Card -->
          <RouterLink
            v-if="authStore.isDoctor"
            to="/doctor/appointments"
            class="group flex items-center gap-3 rounded-2xl border border-emerald-100 bg-emerald-50/30 px-4 py-2 shadow-sm transition-all duration-300 hover:bg-emerald-50/70 hover:shadow-md"
          >
            <div class="flex h-9 w-9 items-center justify-center rounded-xl bg-emerald-100/80 text-emerald-700 transition group-hover:scale-105">
              <CalendarClock class="h-5 w-5" />
            </div>
            <div class="text-left">
              <div class="flex items-center gap-1.5 text-xs font-bold text-slate-800">
                <span>Lịch hẹn hôm nay</span>
                <span class="relative flex h-2 w-2">
                  <span class="absolute inline-flex h-full w-full rounded-full bg-emerald-400 opacity-75 animate-ping"></span>
                  <span class="relative inline-flex h-2 w-2 rounded-full bg-emerald-500"></span>
                </span>
              </div>
              <p class="text-[11px] font-semibold text-emerald-600">
                {{ doctorTodayCount ?? 0 }} ca hẹn cần thực hiện
              </p>
            </div>
          </RouterLink>

          <!-- Receptionist/Nurse Status Card -->
          <RouterLink
            v-else-if="authStore.isReceptionist"
            to="/nurse/appointments"
            class="group flex items-center gap-3 rounded-2xl border border-rose-100 bg-rose-50/30 px-4 py-2 shadow-sm transition-all duration-300 hover:bg-rose-50/70 hover:shadow-md"
          >
            <div class="flex h-9 w-9 items-center justify-center rounded-xl bg-rose-100/80 text-rose-700 transition group-hover:scale-105">
              <CalendarCheck class="h-5 w-5" />
            </div>
            <div class="text-left">
              <div class="flex items-center gap-1.5 text-xs font-bold text-slate-800">
                <span>Yêu cầu chờ duyệt</span>
                <span v-if="receptionistPendingCount && receptionistPendingCount > 0" class="relative flex h-2 w-2">
                  <span class="absolute inline-flex h-full w-full rounded-full bg-rose-400 opacity-75 animate-ping"></span>
                  <span class="relative inline-flex h-2 w-2 rounded-full bg-rose-500"></span>
                </span>
              </div>
              <p class="text-[11px] font-semibold text-rose-600">
                {{ receptionistPendingCount ?? 0 }} yêu cầu cần duyệt
              </p>
            </div>
          </RouterLink>

          <!-- Patient Status Card -->
          <div
            v-else-if="authStore.isPatient"
            class="max-w-[280px] flex items-center gap-3 rounded-2xl border border-blue-100 bg-blue-50/30 px-4 py-2 shadow-sm transition duration-300 hover:bg-blue-50/50"
          >
            <div class="flex h-9 w-9 shrink-0 items-center justify-center rounded-xl bg-blue-50 text-blue-700">
              <HeartPulse class="h-5 w-5 animate-pulse-glow" />
            </div>
            <div class="min-w-0 text-left">
              <p class="text-xs font-bold text-slate-800 truncate">
                {{ nextAppointmentLabel ? 'Lịch khám tiếp theo' : 'Lời khuyên sức khỏe' }}
              </p>
              <p class="text-[10px] font-semibold text-slate-500 truncate" :title="nextAppointmentLabel || dailyHealthTip">
                {{ nextAppointmentLabel || dailyHealthTip }}
              </p>
            </div>
          </div>
        </div>

        <div class="ml-auto flex items-center gap-4 shrink-0">
          <NotificationBell />
          <div class="hidden h-12 border-l border-slate-200 sm:block"></div>

          <div class="flex items-center gap-3">
            <div class="hidden text-right sm:block">
              <p class="max-w-44 truncate text-sm font-bold text-slate-950">{{ displayName }}</p>
              <p class="text-[11px] font-bold uppercase tracking-wide text-[#0F52BA]">{{ roleName }}</p>
            </div>
            <div class="flex h-10 w-10 items-center justify-center overflow-hidden rounded-full border border-slate-200 bg-blue-50 text-sm font-bold text-[#003c90] shadow-sm">
              {{ initials }}
            </div>
          </div>
        </div>
      </header>

      <div v-if="mobileMenuOpen" class="fixed inset-0 z-50 bg-slate-950/40 lg:hidden" @click.self="mobileMenuOpen = false">
        <aside class="flex h-full w-80 max-w-[86vw] flex-col bg-white shadow-2xl">
          <div class="flex h-20 items-center justify-between px-5">
            <div class="flex items-center gap-3">
              <img :src="relIconUrl" alt="MedicareDNU" class="h-11 w-11 rounded-xl object-contain" />
              <div>
                <p class="text-lg font-bold text-[#003c90]">MedicareDNU</p>
                <p class="text-xs font-bold uppercase tracking-wide text-slate-500">Clinical Portal</p>
              </div>
            </div>
            <button type="button" class="rounded-xl p-2 text-slate-500 hover:bg-slate-100" @click="mobileMenuOpen = false">
              <X class="h-5 w-5" />
            </button>
          </div>
          <nav class="flex-1 space-y-6 overflow-y-auto px-4 pb-5">
            <template v-for="group in menuGroups" :key="`mobile-${group.title || 'main'}`">
              <div>
                <p v-if="group.title" class="mb-2 px-3 text-xs font-bold uppercase tracking-wide text-slate-400">{{ group.title }}</p>
                <RouterLink
                  v-for="item in group.items"
                  :key="item.to"
                  :to="item.to"
                  class="mb-2 flex items-center gap-3 rounded-2xl px-4 py-3 text-sm font-semibold text-slate-700"
                  active-class="!bg-[#0F52BA] !text-white"
                  @click="mobileMenuOpen = false"
                >
                  <component :is="item.icon" class="h-5 w-5" />
                  {{ item.label }}
                </RouterLink>
              </div>
            </template>
          </nav>
        </aside>
      </div>

      <main ref="mainRef" class="flex-1 overflow-y-auto">
        <div class="mx-auto min-h-[calc(100vh-5rem)] max-w-[1600px] px-4 py-2 sm:px-5 sm:py-3 lg:px-6">
          <div v-if="layoutError" class="rounded-2xl border border-rose-200 bg-white p-6 text-sm text-rose-700 shadow-sm">
            <p class="text-lg font-bold text-rose-800">Không hiển thị được trang</p>
            <p class="mt-2">{{ layoutError }}</p>
            <button type="button" class="mt-4 rounded-lg bg-rose-600 px-4 py-2 font-bold text-white" @click="reloadRoute">
              Tải lại trang
            </button>
          </div>
          <RouterView v-else />
        </div>
      </main>
    </div>

    <Toast
      :show="notificationStore.toast.show"
      :title="notificationStore.toast.title"
      :message="notificationStore.toast.message"
      :type="notificationStore.toast.type"
      @close="notificationStore.hideToast"
    />
  </div>
</template>

<script setup lang="ts">
import { computed, onBeforeUnmount, onErrorCaptured, onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import {
  Calendar,
  CalendarCheck,
  CalendarClock,
  ChevronLeft,
  ChevronRight,
  Clock,
  HeartPulse,
  LogOut,
  Menu,
  Moon,
  Sparkles,
  Sun,
  Sunrise,
  Sunset,
  X,
} from 'lucide-vue-next'
import { useAuthStore } from '@/stores/authStore'
import { useNotificationStore } from '@/stores/notificationStore'
import { appointmentApi } from '@/services/appointmentApi'
import NotificationBell from '@/components/layout/NotificationBell.vue'
import Toast from '@/components/ui/Toast.vue'
import logoUrl from '@/assets/logo.png'
import relIconUrl from '@/assets/rel-icon.png'
import type { Appointment } from '@/types/appointment'

defineProps<{
  menuGroups: {
    title?: string
    items: {
      label: string
      to: string
      icon: any
    }[]
  }[]
}>()

const route = useRoute()
const router = useRouter()
const authStore = useAuthStore()
const notificationStore = useNotificationStore()
const mobileMenuOpen = ref(false)
const sidebarCollapsed = ref(false)
const mainRef = ref<HTMLElement | null>(null)
const layoutError = ref('')
const currentTime = ref(new Date())
const nextAppointmentLabel = ref('')
const doctorTodayCount = ref<number | null>(null)
const receptionistPendingCount = ref<number | null>(null)
let clockTimer: ReturnType<typeof setInterval> | null = null

const displayName = computed(() => authStore.user?.fullName || authStore.user?.username || 'Người dùng')
const initials = computed(() => displayName.value.trim().charAt(0).toUpperCase() || 'U')
const roleName = computed(() => {
  if (authStore.isAdmin) return 'Quản trị viên'
  if (authStore.isDoctor) return 'Bác sĩ'
  if (authStore.isReceptionist) return 'Điều phối y tế'
  if (authStore.isPatient) return 'Bệnh nhân'
  return 'Clinical Portal'
})

const timePeriod = computed(() => {
  const hour = currentTime.value.getHours()
  if (hour >= 5 && hour <= 10) return 'morning'
  if (hour >= 11 && hour <= 13) return 'noon'
  if (hour >= 14 && hour <= 17) return 'afternoon'
  if (hour >= 18 && hour <= 22) return 'evening'
  return 'night'
})

const greetingData = computed(() => {
  switch (timePeriod.value) {
    case 'morning':
      return { text: 'Chào buổi sáng', iconBg: 'from-amber-100 to-orange-200', iconColor: 'text-amber-600' }
    case 'noon':
      return { text: 'Chào buổi trưa', iconBg: 'from-amber-100 to-sky-100', iconColor: 'text-amber-500' }
    case 'afternoon':
      return { text: 'Chào buổi chiều', iconBg: 'from-orange-100 to-rose-100', iconColor: 'text-orange-500' }
    case 'evening':
      return { text: 'Chào buổi tối', iconBg: 'from-purple-100 to-indigo-200', iconColor: 'text-indigo-600' }
    case 'night':
    default:
      return { text: 'Chúc ngủ ngon', iconBg: 'from-slate-100 to-slate-200', iconColor: 'text-slate-600' }
  }
})

const clockHours = computed(() => String(currentTime.value.getHours()).padStart(2, '0'))
const clockMinutes = computed(() => String(currentTime.value.getMinutes()).padStart(2, '0'))
const clockSeconds = computed(() => String(currentTime.value.getSeconds()).padStart(2, '0'))

const vietnameseDate = computed(() => {
  const days = ['Chủ Nhật', 'Thứ Hai', 'Thứ Ba', 'Thứ Tư', 'Thứ Năm', 'Thứ Sáu', 'Thứ Bảy']
  const dayOfWeek = days[currentTime.value.getDay()]
  const date = String(currentTime.value.getDate()).padStart(2, '0')
  const month = String(currentTime.value.getMonth() + 1).padStart(2, '0')
  return `${dayOfWeek}, ${date}/${month}`
})

const weatherData = computed(() => {
  switch (timePeriod.value) {
    case 'morning':
      return { temp: '26°C', desc: 'Nắng ấm', iconColor: 'text-amber-500' }
    case 'noon':
      return { temp: '34°C', desc: 'Nắng gắt', iconColor: 'text-orange-500' }
    case 'afternoon':
      return { temp: '31°C', desc: 'Nắng dịu', iconColor: 'text-orange-400' }
    case 'evening':
      return { temp: '28°C', desc: 'Trời mát', iconColor: 'text-indigo-400' }
    case 'night':
    default:
      return { temp: '25°C', desc: 'Đêm thoáng', iconColor: 'text-slate-400' }
  }
})

const HEALTH_TIPS = [
  'Uống đủ 2 lít nước mỗi ngày để thanh lọc cơ thể.',
  'Dành 30 phút tập thể dục nâng cao hệ miễn dịch.',
  'Hạn chế đồ ăn nhanh và nước ngọt có ga.',
  'Ngủ đủ 7-8 tiếng mỗi ngày giúp cơ thể khỏe mạnh.',
  'Rửa tay thường xuyên để phòng ngừa vi khuẩn.',
  'Nên khám sức khỏe định kỳ mỗi 6 tháng.',
  'Ăn nhiều rau xanh và trái cây tươi mỗi ngày.',
  'Cho mắt nghỉ ngơi sau mỗi 45 phút ngồi máy tính.'
]

const dailyHealthTip = computed(() => {
  const day = currentTime.value.getDate()
  return HEALTH_TIPS[day % HEALTH_TIPS.length]
})

const contextStatus = computed(() => {
  if (authStore.isPatient) {
    if (nextAppointmentLabel.value) {
      return { message: `Lịch khám tiếp theo: ${nextAppointmentLabel.value}`, pulseClass: 'bg-emerald-500' }
    }
    return { message: 'Chúc bạn một ngày nhiều sức khỏe! Hãy theo dõi sức khỏe thường xuyên.', pulseClass: '' }
  }
  if (authStore.isDoctor) {
    const count = doctorTodayCount.value ?? 8
    return { message: `Hôm nay bác sĩ có ${count} ca khám lịch hẹn cần thực hiện.`, pulseClass: '' }
  }
  if (authStore.isReceptionist) {
    const count = receptionistPendingCount.value ?? 3
    return { message: `Có ${count} yêu cầu đặt lịch khám mới đang chờ bạn phê duyệt.`, pulseClass: count > 0 ? 'bg-rose-500' : '' }
  }
  return { message: 'Chúc bạn một ngày làm việc hiệu quả cùng MedicareDNU.', pulseClass: '' }
})

watch(
  () => route.fullPath,
  () => {
    layoutError.value = ''
    scrollToTop()
  },
  { immediate: true },
)

onErrorCaptured((error) => {
  layoutError.value = error instanceof Error ? error.message : String(error)
  scrollToTop()
  return false
})

onMounted(() => {
  if ('scrollRestoration' in window.history) window.history.scrollRestoration = 'manual'
  scrollToTop()
  initializeNotifications()
  refreshContextStatus()
  clockTimer = setInterval(() => {
    currentTime.value = new Date()
  }, 1000)
})

onBeforeUnmount(() => {
  notificationStore.disconnectSignalR()
  if (clockTimer) clearInterval(clockTimer)
})

watch(
  () => authStore.token,
  (token) => {
    if (token) {
      initializeNotifications()
      refreshContextStatus()
    }
    else notificationStore.disconnectSignalR()
  },
)

watch(
  () => [authStore.user?.patientId, authStore.user?.doctorId, authStore.roleId, route.fullPath],
  () => {
    refreshContextStatus()
  },
)

function isActive(to: string) {
  return route.path === to || route.path.startsWith(`${to}/`)
}

function handleLogout() {
  notificationStore.disconnectSignalR()
  authStore.logout()
  router.push('/login')
}

function scrollToTop() {
  requestAnimationFrame(() => {
    window.scrollTo({ top: 0, behavior: 'auto' })
    mainRef.value?.scrollTo({ top: 0, behavior: 'auto' })
  })
}

function reloadRoute() {
  layoutError.value = ''
  router.replace({ path: route.path, query: { ...route.query, _reload: Date.now().toString() } })
}

async function refreshContextStatus() {
  if (!authStore.token) return
  nextAppointmentLabel.value = ''
  doctorTodayCount.value = null
  receptionistPendingCount.value = null

  try {
    if (authStore.isPatient) {
      await refreshPatientAppointmentStatus()
    } else if (authStore.isDoctor) {
      await refreshDoctorAppointmentStatus()
    } else if (authStore.isReceptionist) {
      await refreshReceptionistAppointmentStatus()
    }
  } catch {
    // Context text is decorative; fall back silently when a service is unavailable.
  }
}

async function refreshPatientAppointmentStatus() {
  const patientId = authStore.user?.patientId
  if (!patientId) return
  const appointments = await appointmentApi.getAppointmentsByPatient(patientId)
  const nextAppointment = appointments
    .filter(isUpcomingAppointment)
    .sort((a, b) => appointmentTimestamp(a) - appointmentTimestamp(b))[0]
  nextAppointmentLabel.value = nextAppointment ? formatNextAppointment(nextAppointment) : ''
}

async function refreshDoctorAppointmentStatus() {
  const doctorId = Number(authStore.user?.doctorId)
  if (!Number.isFinite(doctorId) || doctorId <= 0) return
  const today = new Date().toISOString().slice(0, 10)
  const appointments = await appointmentApi.getAppointmentsByDoctor(doctorId)
  doctorTodayCount.value = appointments.filter((appointment) =>
    String(appointment.appointmentDate || '').slice(0, 10) === today &&
    !isClosedAppointmentStatus(appointment.status),
  ).length
}

async function refreshReceptionistAppointmentStatus() {
  const appointments = await appointmentApi.getAppointments()
  receptionistPendingCount.value = appointments.filter((appointment) =>
    String(appointment.status || '').toLowerCase().includes('pending') ||
    String(appointment.status || '').toLowerCase().includes('waiting') ||
    String(appointment.status || '').toLowerCase().includes('chờ'),
  ).length
}

function isUpcomingAppointment(appointment: Appointment) {
  if (isClosedAppointmentStatus(appointment.status)) return false
  return appointmentTimestamp(appointment) >= Date.now() - 15 * 60 * 1000
}

function isClosedAppointmentStatus(status?: string) {
  const value = String(status || '').toLowerCase()
  return value.includes('cancel') || value.includes('completed') || value.includes('done') || value.includes('noshow') || value.includes('expired') || value.includes('hủy')
}

function appointmentTimestamp(appointment: Appointment) {
  const date = String(appointment.appointmentDate || '').slice(0, 10)
  const time = String(appointment.slotTime || '00:00').slice(0, 8)
  const timestamp = new Date(`${date}T${time}`).getTime()
  return Number.isNaN(timestamp) ? 0 : timestamp
}

function formatNextAppointment(appointment: Appointment) {
  const date = String(appointment.appointmentDate || '').slice(0, 10)
  const time = String(appointment.slotTime || '').slice(0, 5) || 'Chưa rõ giờ'
  const today = new Date().toISOString().slice(0, 10)
  const tomorrow = new Date(Date.now() + 86400000).toISOString().slice(0, 10)
  if (date === today) return `${time} hôm nay`
  if (date === tomorrow) return `${time} ngày mai`
  return `${time} ngày ${formatShortDate(date)}`
}

function formatShortDate(value: string) {
  const [year, month, day] = value.split('-')
  return year && month && day ? `${day}/${month}` : value
}

async function initializeNotifications() {
  if (!authStore.token) return
  await Promise.all([
    notificationStore.fetchUnreadCount().catch(() => undefined),
    notificationStore.fetchNotifications().catch(() => undefined),
    notificationStore.initSignalR().catch(() => undefined),
  ])
}
</script>

<style scoped>
@keyframes welcome-fade {
  from {
    opacity: 0;
    transform: translateY(-6px);
  }

  to {
    opacity: 1;
    transform: translateY(0);
  }
}

/* Custom CSS Animations for Premium Header Redesign */
@keyframes spin-slow {
  from {
    transform: rotate(0deg);
  }
  to {
    transform: rotate(360deg);
  }
}

.animate-spin-slow {
  animation: spin-slow 15s linear infinite;
}

@keyframes pulse-glow {
  0%, 100% {
    opacity: 0.4;
    transform: scale(1);
  }
  50% {
    opacity: 0.8;
    transform: scale(1.1);
  }
}

.animate-pulse-glow {
  animation: pulse-glow 3s ease-in-out infinite;
}

@keyframes float-gentle {
  0%, 100% {
    transform: translateY(0) rotate(0deg);
  }
  50% {
    transform: translateY(-3px) rotate(2deg);
  }
}

.animate-float-gentle {
  animation: float-gentle 4s ease-in-out infinite;
}

@keyframes float-slow {
  0%, 100% {
    transform: translateY(0) scale(1);
  }
  50% {
    transform: translateY(-2px) scale(1.03);
  }
}

.animate-float-slow {
  animation: float-slow 5s ease-in-out infinite;
}

@keyframes twinkle {
  0%, 100% {
    opacity: 0.2;
    transform: scale(0.8);
  }
  50% {
    opacity: 1;
    transform: scale(1.2);
  }
}

.animate-twinkle {
  animation: twinkle 2s ease-in-out infinite;
}

@keyframes sunrise-bounce {
  0%, 100% {
    transform: translateY(1.5px);
  }
  50% {
    transform: translateY(-1.5px);
  }
}

.animate-sunrise-bounce {
  animation: sunrise-bounce 3.5s ease-in-out infinite;
}

@keyframes sunset-dip {
  0%, 100% {
    transform: translateY(-1px);
  }
  50% {
    transform: translateY(2px);
  }
}

.animate-sunset-dip {
  animation: sunset-dip 4s ease-in-out infinite;
}

@keyframes pulse-colon {
  0%, 100% {
    opacity: 1;
  }
  50% {
    opacity: 0;
  }
}

.animate-pulse-colon {
  animation: pulse-colon 1s step-start infinite;
}
</style>
