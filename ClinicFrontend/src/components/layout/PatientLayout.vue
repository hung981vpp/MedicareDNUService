<template>
  <ClinicalPortalLayout :menu-groups="menuGroups" />
  <PatientProfileCompletionModal />
</template>

<script setup lang="ts">
import {
  CalendarCheck,
  CreditCard,
  FileHeart,
  LayoutDashboard,
  Pill,
  UserRound,
} from 'lucide-vue-next'
import ClinicalPortalLayout from './ClinicalPortalLayout.vue'
import PatientProfileCompletionModal from '@/components/patient/PatientProfileCompletionModal.vue'

const menuGroups = [
  {
    title: '',
    items: [{ label: 'Bảng điều khiển', to: '/patient/dashboard', icon: LayoutDashboard }],
  },
  {
    title: 'Dịch vụ',
    items: [
      { label: 'Đặt lịch khám', to: '/patient/booking', icon: CalendarCheck },
      { label: 'Lịch hẹn', to: '/patient/appointments', icon: CalendarCheck },
      { label: 'Hồ sơ bệnh án', to: '/patient/records', icon: FileHeart },
      { label: 'Đơn thuốc', to: '/patient/prescriptions', icon: Pill },
      { label: 'Viện phí', to: '/patient/bills', icon: CreditCard },
    ],
  },
  {
    title: 'Tài khoản',
    items: [{ label: 'Hồ sơ cá nhân', to: '/patient/profile', icon: UserRound }],
  },
]
</script>
