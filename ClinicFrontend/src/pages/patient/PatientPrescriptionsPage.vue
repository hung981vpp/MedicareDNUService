<template>
  <section class="min-h-screen bg-[#f8fafc] py-6 sm:py-8">
    <div class="max-w-none mx-auto px-4 sm:px-6 lg:px-8 space-y-6">
      
      <!-- 1. Header trang -->
      <div class="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between rounded-2xl border border-slate-200 bg-white p-6 shadow-sm">
        <div class="flex items-start gap-4">
          <span class="flex h-12 w-12 shrink-0 items-center justify-center rounded-xl bg-blue-50 text-blue-600">
            <Pill class="h-6 w-6" />
          </span>
          <div>
            <h1 class="text-2xl font-bold tracking-tight text-slate-900">Đơn thuốc của tôi</h1>
            <p class="mt-1 text-sm text-slate-500">
              Theo dõi đơn thuốc đã được bác sĩ kê và trạng thái xử lý tại nhà thuốc.
            </p>
          </div>
        </div>
        <div class="flex items-center gap-3">
          <BaseButton variant="outline" :disabled="loading" @click="loadData">
            <template #icon>
              <RefreshCw :class="['h-4 w-4', loading ? 'animate-spin' : '']" />
            </template>
            Tải lại
          </BaseButton>
          <BaseButton class="bg-blue-600 hover:bg-blue-700 text-white font-bold" @click="triggerPrint">
            <template #icon>
              <Printer class="h-4 w-4" />
            </template>
            In đơn thuốc
          </BaseButton>
        </div>
      </div>

      <!-- 2. Stats Grid -->
      <div class="grid gap-4 grid-cols-1 sm:grid-cols-2 lg:grid-cols-4">
        <!-- Card 1: Tổng đơn thuốc -->
        <div class="group rounded-2xl border border-slate-200 bg-white p-5 shadow-sm transition hover:border-blue-200 hover:shadow-[0_12px_24px_rgba(15,82,186,0.06)] flex flex-col justify-between min-h-[140px]">
          <div class="flex items-center justify-between">
            <span class="text-sm font-bold text-slate-700">Tổng đơn thuốc</span>
            <span class="flex h-10 w-10 items-center justify-center rounded-xl bg-blue-50 text-blue-600">
              <Pill class="h-5 w-5" />
            </span>
          </div>
          <div class="mt-4">
            <p class="text-3xl font-extrabold text-slate-900 tracking-tight">{{ stats.total }}</p>
            <p class="mt-1 text-xs text-slate-500 font-medium font-semibold">Tất cả đơn thuốc được kê</p>
          </div>
        </div>

        <!-- Card 2: Chờ xử lý -->
        <div class="group rounded-2xl border border-slate-200 bg-white p-5 shadow-sm transition hover:border-amber-200 hover:shadow-[0_12px_24px_rgba(245,158,11,0.06)] flex flex-col justify-between min-h-[140px]">
          <div class="flex items-center justify-between">
            <span class="text-sm font-bold text-slate-700">Chờ xử lý</span>
            <span class="flex h-10 w-10 items-center justify-center rounded-xl bg-amber-50 text-amber-600">
              <Clock class="h-5 w-5" />
            </span>
          </div>
          <div class="mt-4">
            <p class="text-3xl font-extrabold text-slate-900 tracking-tight">{{ stats.pending }}</p>
            <p class="mt-1 text-xs text-slate-500 font-medium">Đang chờ nhà thuốc xử lý</p>
          </div>
        </div>

        <!-- Card 3: Đã gửi nhà thuốc -->
        <div class="group rounded-2xl border border-slate-200 bg-white p-5 shadow-sm transition hover:border-cyan-200 hover:shadow-[0_12px_24px_rgba(6,182,212,0.06)] flex flex-col justify-between min-h-[140px]">
          <div class="flex items-center justify-between">
            <span class="text-sm font-bold text-slate-700">Đã gửi nhà thuốc</span>
            <span class="flex h-10 w-10 items-center justify-center rounded-xl bg-cyan-50 text-cyan-600">
              <Send class="h-5 w-5" />
            </span>
          </div>
          <div class="mt-4">
            <p class="text-3xl font-extrabold text-slate-900 tracking-tight">{{ stats.sent }}</p>
            <p class="mt-1 text-xs text-slate-500 font-medium">Đã chuyển sang quầy dược</p>
          </div>
        </div>

        <!-- Card 4: Đã cấp thuốc / Hoàn tất -->
        <div class="group rounded-2xl border border-slate-200 bg-white p-5 shadow-sm transition hover:border-emerald-200 hover:shadow-[0_12px_24px_rgba(16,185,129,0.06)] flex flex-col justify-between min-h-[140px]">
          <div class="flex items-center justify-between">
            <span class="text-sm font-bold text-slate-700">Đã cấp / Hoàn tất</span>
            <span class="flex h-10 w-10 items-center justify-center rounded-xl bg-emerald-50 text-emerald-600">
              <CheckCircle2 class="h-5 w-5" />
            </span>
          </div>
          <div class="mt-4">
            <p class="text-3xl font-extrabold text-slate-900 tracking-tight">{{ stats.completed }}</p>
            <p class="mt-1 text-xs text-slate-500 font-medium">Đã nhận đủ thuốc dược sĩ bàn giao</p>
          </div>
        </div>
      </div>

      <!-- 4. Bộ lọc nâng cao -->
      <div class="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm space-y-4">
        <div class="flex items-center justify-between border-b border-slate-100 pb-3">
          <h3 class="font-bold text-slate-800 text-base">Bộ lọc tìm kiếm nâng cao</h3>
          <button
            type="button"
            class="inline-flex items-center gap-1.5 rounded-lg border border-slate-200 bg-white px-3 py-1.5 text-xs font-bold text-slate-600 transition hover:bg-slate-50 hover:text-slate-900 shadow-sm"
            @click="resetFilters"
          >
            <RefreshCw class="h-3.5 w-3.5" />
            Đặt lại bộ lọc
          </button>
        </div>

        <div class="grid gap-4 grid-cols-1 sm:grid-cols-2 md:grid-cols-4">
          <!-- Từ khóa tìm kiếm -->
          <label class="relative block">
            <span class="mb-1.5 block text-xs font-bold text-slate-500 uppercase tracking-wider">Từ khóa tìm kiếm</span>
            <span class="relative block">
              <Search class="pointer-events-none absolute left-3.5 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />
              <input
                v-model="filters.search"
                class="h-11 w-full rounded-xl border border-slate-200 bg-white pl-10 pr-4 text-sm outline-none transition focus:border-blue-400 focus:ring-4 focus:ring-blue-100"
                placeholder="Tìm mã đơn, thuốc, ghi chú..."
              />
            </span>
          </label>

          <!-- Trạng thái -->
          <label class="block">
            <span class="mb-1.5 block text-xs font-bold text-slate-500 uppercase tracking-wider">Trạng thái</span>
            <select
              v-model="filters.status"
              class="h-11 w-full rounded-xl border border-slate-200 bg-white px-3.5 text-sm outline-none transition focus:border-blue-400 focus:ring-4 focus:ring-blue-100 cursor-pointer"
            >
              <option value="ALL">Tất cả</option>
              <option value="PENDING">Chờ xử lý</option>
              <option value="SENT_TO_PHARMACY">Đã gửi nhà thuốc</option>
              <option value="DISPENSED">Đã cấp thuốc</option>
              <option value="COMPLETED">Hoàn tất</option>
              <option value="CANCELLED">Đã hủy</option>
            </select>
          </label>

          <!-- Ngày kê từ -->
          <label class="block">
            <span class="mb-1.5 block text-xs font-bold text-slate-500 uppercase tracking-wider">Ngày kê từ</span>
            <input
              v-model="filters.startDate"
              type="date"
              class="h-11 w-full rounded-xl border border-slate-200 bg-white px-3.5 text-sm outline-none transition focus:border-blue-400 focus:ring-4 focus:ring-blue-100 cursor-pointer"
            />
          </label>

          <!-- Ngày kê đến -->
          <label class="block">
            <span class="mb-1.5 block text-xs font-bold text-slate-500 uppercase tracking-wider">Ngày kê đến</span>
            <input
              v-model="filters.endDate"
              type="date"
              class="h-11 w-full rounded-xl border border-slate-200 bg-white px-3.5 text-sm outline-none transition focus:border-blue-400 focus:ring-4 focus:ring-blue-100 cursor-pointer"
            />
          </label>
        </div>
      </div>

      <!-- 5. Bảng danh sách đơn thuốc -->
      <div v-if="error" class="rounded-xl border border-rose-200 bg-rose-50 p-4 text-sm text-rose-800 flex items-start justify-between gap-4">
        <div class="flex gap-2.5">
          <ShieldAlert class="h-5 w-5 text-rose-600 shrink-0 mt-0.5" />
          <div>
            <p class="font-bold">Không thể tải danh sách đơn thuốc</p>
            <p class="text-xs text-rose-700 mt-1">{{ error }}</p>
          </div>
        </div>
        <BaseButton
          variant="outline"
          class="h-9 px-3 rounded-lg border border-rose-200 bg-white text-rose-800 hover:bg-rose-100 transition text-xs font-bold"
          @click="loadData"
        >
          Thử lại
        </BaseButton>
      </div>

      <div v-if="loading" class="grid gap-4 md:grid-cols-3">
        <LoadingSkeleton v-for="item in 3" :key="item" />
      </div>

      <div v-else class="rounded-2xl border border-slate-200 bg-white shadow-sm overflow-hidden">
        <div class="flex items-center justify-between border-b border-slate-100 p-4 bg-slate-50/50">
          <span class="text-sm font-semibold text-slate-500">
            Tổng số {{ filteredPrescriptions.length }} kết quả đơn thuốc
          </span>
        </div>

        <div v-if="filteredPrescriptions.length" class="overflow-x-auto">
          <table class="min-w-full divide-y divide-slate-100 text-sm">
            <thead class="bg-slate-50 text-xs font-bold uppercase tracking-wide text-slate-500">
              <tr>
                <th class="px-6 py-4 text-left">Mã đơn</th>
                <th class="px-6 py-4 text-left">Ngày kê</th>
                <th class="px-6 py-4 text-left">Thuốc</th>
                <th class="px-6 py-4 text-center">Số loại</th>
                <th class="px-6 py-4 text-left">Trạng thái</th>
                <th class="px-6 py-4 text-right">Thao tác</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-100 bg-white">
              <tr v-for="prescription in paginatedPrescriptions" :key="prescription.id" class="transition hover:bg-slate-50">
                <!-- Mã đơn -->
                <td class="px-6 py-4 whitespace-nowrap font-bold text-slate-900">
                  {{ prescription.prescriptionCode || 'DT' + String(prescription.id).padStart(3, '0') }}
                </td>

                <!-- Ngày kê -->
                <td class="px-6 py-4 whitespace-nowrap text-slate-500">
                  {{ formatDateTime(prescription.createdAt) }}
                </td>

                <!-- Thuốc -->
                <td class="px-6 py-4">
                  <p class="font-medium text-slate-800 line-clamp-2 max-w-sm" :title="allMedicinesText(prescription)">
                    {{ displayMedicines(prescription) }}
                  </p>
                </td>

                <!-- Số loại -->
                <td class="px-6 py-4 whitespace-nowrap text-center font-bold text-slate-500">
                  {{ (prescription.items || prescription.prescriptionItems || []).length || '-' }}
                </td>

                <!-- Trạng thái -->
                <td class="px-6 py-4 whitespace-nowrap">
                  <span :class="['rounded-full px-2.5 py-1 text-xs font-bold inline-flex items-center gap-1', statusClass(prescription.status)]">
                    <span class="h-1.5 w-1.5 rounded-full bg-current"></span>
                    {{ statusLabel(prescription.status) }}
                  </span>
                </td>

                <!-- Thao tác -->
                <td class="px-6 py-4 whitespace-nowrap text-right">
                  <div class="inline-flex gap-2">
                    <BaseButton
                      variant="ghost"
                      size="sm"
                      class="bg-blue-50 text-blue-600 hover:bg-blue-100 font-bold flex items-center gap-1.5 border border-transparent"
                      @click="openDetails(prescription)"
                    >
                      <template #icon>
                        <Eye class="h-4 w-4" />
                      </template>
                      Chi tiết
                    </BaseButton>
                    <BaseButton
                      variant="ghost"
                      size="sm"
                      class="bg-slate-50 text-slate-600 hover:bg-slate-100 font-bold flex items-center gap-1.5 border border-transparent"
                      @click="printPrescription(prescription)"
                    >
                      <template #icon>
                        <Printer class="h-4 w-4 text-slate-500" />
                      </template>
                      In
                    </BaseButton>
                  </div>
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
              Hiển thị {{ Math.min(filteredPrescriptions.length, (currentPage - 1) * itemsPerPage + 1) }} - {{ Math.min(filteredPrescriptions.length, currentPage * itemsPerPage) }} trên {{ filteredPrescriptions.length }} kết quả
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

        <!-- Empty state -->
        <div v-else class="p-16 text-center text-slate-500">
          <span class="flex h-14 w-14 items-center justify-center rounded-full bg-slate-100 text-slate-300 mx-auto">
            <Pill class="h-7 w-7" />
          </span>
          <h3 class="mt-4 text-lg font-bold text-slate-900">Bạn chưa có đơn thuốc nào</h3>
          <p class="mx-auto mt-2 max-w-md text-sm text-slate-500 leading-relaxed">
            Mỗi đơn thuốc sau khi được bác sĩ kê sẽ hiển thị tại đây. Vui lòng thử reset bộ lọc hoặc kiểm tra lại lịch hẹn.
          </p>
        </div>
      </div>
    </div>

    <!-- 6. Drawer Chi tiết Đơn thuốc bên phải -->
    <div v-if="drawerOpen" class="fixed inset-0 z-50 bg-slate-950/40 backdrop-blur-sm transition-opacity" @click="closeDrawer"></div>

    <transition
      enter-active-class="transition duration-300 ease-out"
      enter-from-class="translate-x-full"
      enter-to-class="translate-x-0"
      leave-active-class="transition duration-200 ease-in"
      leave-from-class="translate-x-0"
      leave-to-class="translate-x-full"
    >
      <div v-if="drawerOpen && selectedPrescription" class="fixed right-0 top-0 z-50 h-screen w-full max-w-2xl bg-white shadow-2xl flex flex-col border-l border-slate-200">
        
        <!-- Drawer Header -->
        <div class="flex items-center justify-between border-b border-slate-100 p-5 bg-slate-50/50">
          <div>
            <div class="flex items-center gap-2">
              <h2 class="text-lg font-bold text-slate-900">Chi tiết đơn thuốc</h2>
              <span :class="['rounded-full px-2.5 py-0.5 text-[10px] font-bold border inline-flex items-center gap-1', statusClass(selectedPrescription.status)]">
                <span class="h-1.5 w-1.5 rounded-full bg-current"></span>
                {{ statusLabel(selectedPrescription.status) }}
              </span>
            </div>
            <p class="mt-1 text-xs font-semibold text-slate-500 font-mono">
              Mã: {{ selectedPrescription.prescriptionCode || 'DT' + String(selectedPrescription.id).padStart(3, '0') }}
            </p>
          </div>
          <div class="flex items-center gap-2">
            <BaseButton
              v-slot:icon
              v-if="selectedPrescription"
              variant="outline"
              size="sm"
              class="border-slate-200 text-slate-700 bg-white hover:bg-slate-50 font-bold inline-flex items-center gap-1.5 h-9"
              @click="printPrescription(selectedPrescription)"
            >
              <Printer class="h-4 w-4 text-slate-500" />
            </BaseButton>
            <button type="button" class="rounded-xl p-2 text-slate-400 hover:bg-slate-100 hover:text-slate-600 transition" @click="closeDrawer">
              <X class="h-5 w-5" />
            </button>
          </div>
        </div>

        <!-- Drawer Tabs Switcher -->
        <div class="flex border-b border-slate-100 overflow-x-auto bg-slate-50/20 px-3">
          <button
            v-for="tab in drawerTabs"
            :key="tab.key"
            type="button"
            :class="[
              'px-4 py-3 text-sm font-semibold whitespace-nowrap border-b-2 transition relative',
              currentTab === tab.key
                ? 'border-blue-600 text-blue-600'
                : 'border-transparent text-slate-500 hover:text-slate-700'
            ]"
            @click="currentTab = tab.key"
          >
            {{ tab.label }}
          </button>
        </div>

        <!-- Drawer Content -->
        <div class="flex-1 overflow-y-auto p-6 space-y-6">
          
          <!-- Tab 1: Tổng quan -->
          <div v-if="currentTab === 'overview'" class="space-y-4">
            <!-- Nhóm 1: Thông tin đơn thuốc -->
            <div class="space-y-2">
              <h3 class="text-xs font-bold text-slate-400 uppercase tracking-wider">Thông tin đơn thuốc</h3>
              <div class="grid gap-3 sm:grid-cols-2">
                <div class="rounded-xl border border-slate-100 bg-slate-50 p-3">
                  <span class="text-xs font-semibold text-slate-400">Mã đơn thuốc</span>
                  <p class="mt-0.5 font-mono font-bold text-slate-800 text-sm">
                    {{ selectedPrescription.prescriptionCode || 'DT' + String(selectedPrescription.id).padStart(3, '0') }}
                  </p>
                </div>
                <div class="rounded-xl border border-slate-100 bg-slate-50 p-3" v-if="selectedPrescription.medicalRecordCode">
                  <span class="text-xs font-semibold text-slate-400">Mã bệnh án</span>
                  <p class="mt-0.5 font-mono font-bold text-slate-800 text-sm">{{ selectedPrescription.medicalRecordCode }}</p>
                </div>
                <div class="rounded-xl border border-slate-100 bg-slate-50 p-3">
                  <span class="text-xs font-semibold text-slate-400">Mã bệnh nhân</span>
                  <p class="mt-0.5 font-mono font-bold text-slate-800 text-sm">
                    {{ selectedPrescription.patientCode || selectedPrescription.patientIdCode || selectedPrescription.patientId || 'Chưa cập nhật' }}
                  </p>
                </div>
                <div class="rounded-xl border border-slate-100 bg-slate-50 p-3">
                  <span class="text-xs font-semibold text-slate-400">Bác sĩ kê đơn</span>
                  <p class="mt-0.5 font-bold text-slate-800 text-sm truncate" :title="associatedDoctorName(selectedPrescription) || 'Bác sĩ điều trị'">
                    {{ associatedDoctorName(selectedPrescription) || 'Bác sĩ điều trị' }}
                  </p>
                </div>
              </div>
            </div>

            <!-- Nhóm 2: Thời gian xử lý -->
            <div class="space-y-2">
              <h3 class="text-xs font-bold text-slate-400 uppercase tracking-wider">Thời gian xử lý</h3>
              <div class="rounded-xl border border-slate-150 bg-white p-4 space-y-2.5 text-sm text-slate-600">
                <div class="flex justify-between items-center pb-2 border-b border-slate-50">
                  <span>Trạng thái:</span>
                  <span :class="['rounded-full px-2.5 py-0.5 text-xs font-bold border inline-flex items-center gap-1', statusClass(selectedPrescription.status)]">
                    <span class="h-1.5 w-1.5 rounded-full bg-current"></span>
                    {{ statusLabel(selectedPrescription.status) }}
                  </span>
                </div>
                <div class="flex justify-between items-center pb-2 border-b border-slate-50">
                  <span>Thời gian kê đơn:</span>
                  <span class="font-semibold text-slate-800">{{ formatDateTime(selectedPrescription.createdAt) }}</span>
                </div>
                <div class="flex justify-between items-center pb-2 border-b border-slate-50" v-if="selectedPrescription.sentToPharmacyAt">
                  <span>Gửi sang quầy dược:</span>
                  <span class="font-semibold text-slate-800">{{ formatDateTime(selectedPrescription.sentToPharmacyAt) }}</span>
                </div>
                <div class="flex justify-between items-center" v-if="selectedPrescription.submittedAt">
                  <span>Xác nhận phát thuốc:</span>
                  <span class="font-semibold text-slate-800">{{ formatDateTime(selectedPrescription.submittedAt) }}</span>
                </div>
              </div>
            </div>
          </div>

          <!-- Tab 2: Danh sách thuốc -->
          <div v-if="currentTab === 'medicines'" class="space-y-4">
            <h3 class="text-xs font-bold text-slate-400 uppercase tracking-wider">Danh mục thuốc được kê</h3>
            
            <template v-if="(selectedPrescription.items || selectedPrescription.prescriptionItems || []).length">
              <div
                v-for="item in (selectedPrescription.items || selectedPrescription.prescriptionItems || [])"
                :key="item.id"
                class="rounded-xl border border-slate-200 bg-white p-4 shadow-sm hover:border-blue-200 transition"
              >
                <div class="flex items-center justify-between gap-3">
                  <p class="font-bold text-slate-900 text-sm">{{ item.medicineNameSnapshot || item.medicineName }}</p>
                  <span class="text-xs font-bold text-blue-700 bg-blue-50 px-2 py-0.5 rounded-md">
                    x{{ item.quantity }} {{ item.unitSnapshot || 'Đơn vị' }}
                  </span>
                </div>
                <div class="mt-3 grid grid-cols-2 gap-2 text-xs text-slate-500 pt-3 border-t border-slate-100">
                  <p>Liều lượng: <span class="font-semibold text-slate-700">{{ item.dosage || 'Theo chỉ định' }}</span></p>
                  <p>Tần suất: <span class="font-semibold text-slate-700">{{ item.frequency || 'Chưa cập nhật' }}</span></p>
                  <p class="col-span-2">Dùng trong: <span class="font-semibold text-slate-700">{{ item.durationDays || 0 }} ngày</span></p>
                  <p v-if="item.usageInstruction" class="col-span-2 text-blue-600 font-medium mt-1">
                    Hướng dẫn: {{ item.usageInstruction }}
                  </p>
                </div>
              </div>
            </template>
            <template v-else>
              <!-- Legacy text structure fallback -->
              <div v-if="selectedPrescription.note" class="rounded-xl border border-slate-200 bg-white p-5 space-y-3">
                <p class="text-xs text-slate-400 font-bold uppercase tracking-wider">Thông tin kê đơn viết tay</p>
                <div class="bg-slate-50 p-4 rounded-xl border border-slate-100 text-sm text-slate-700 whitespace-pre-line leading-relaxed font-semibold">
                  {{ selectedPrescription.note }}
                </div>
              </div>
              <div v-else class="rounded-xl border border-dashed border-slate-200 p-8 text-center text-slate-400">
                <Pill class="mx-auto h-8 w-8 text-slate-300" />
                <p class="mt-2 text-xs font-semibold">Chưa có thông tin danh sách thuốc</p>
              </div>
            </template>
          </div>

          <!-- Tab 3: Hướng dẫn dùng thuốc -->
          <div v-if="currentTab === 'instructions'" class="space-y-4">
            <h3 class="text-xs font-bold text-slate-400 uppercase tracking-wider">Dặn dò của bác sĩ</h3>
            <div class="rounded-xl border border-slate-100 bg-slate-50 p-5 space-y-2">
              <span class="text-xs font-bold text-slate-400 uppercase tracking-wider block">Ghi chú đơn thuốc</span>
              <p class="text-sm text-slate-700 leading-relaxed whitespace-pre-line font-medium">
                {{ selectedPrescription.note || 'Chưa có hướng dẫn sử dụng chi tiết' }}
              </p>
            </div>
            
            <div class="rounded-xl border border-blue-100 bg-blue-50/50 p-4 text-xs text-blue-700 flex items-start gap-2">
              <span class="font-bold shrink-0">Lưu ý:</span>
              <p>Uống thuốc đúng giờ, đủ liều theo đúng chỉ định. Không tự ý mua thêm thuốc ngoài hoặc thay đổi liều lượng.</p>
            </div>
          </div>

          <!-- Tab 4: Nhà thuốc / Xử lý -->
          <div v-if="currentTab === 'pharmacy'" class="space-y-4">
            <h3 class="text-xs font-bold text-slate-400 uppercase tracking-wider">Thông tin xử lý tại quầy dược</h3>
            
            <div class="rounded-xl border border-slate-200 bg-white p-5 shadow-sm space-y-4">
              <div class="flex items-center gap-3">
                <span :class="['flex h-8 w-8 shrink-0 items-center justify-center rounded-lg border', 
                  (selectedPrescription.status && String(selectedPrescription.status).toLowerCase().includes('dispensed') || String(selectedPrescription.status).toLowerCase().includes('complete') || String(selectedPrescription.status).toLowerCase().includes('hoàn'))
                    ? 'bg-emerald-50 text-emerald-600 border-emerald-100'
                    : 'bg-amber-50 text-amber-600 border-amber-100'
                ]">
                  <Send class="h-4 w-4" />
                </span>
                <div>
                  <h4 class="font-bold text-slate-800 text-sm">Trạng thái cấp thuốc</h4>
                  <p class="text-xs text-slate-500 mt-1">Trạng thái xử lý: <span class="font-bold text-slate-700">{{ statusLabel(selectedPrescription.status) }}</span></p>
                </div>
              </div>

              <div class="border-t border-slate-100 pt-4 grid gap-3 text-xs text-slate-500">
                <div class="flex justify-between">
                  <span>Chuyển thông tin đơn:</span>
                  <span class="font-semibold text-slate-700">{{ formatDateTime(selectedPrescription.sentToPharmacyAt || selectedPrescription.createdAt) }}</span>
                </div>
                <div class="flex justify-between">
                  <span>Trạng thái phát thuốc:</span>
                  <span class="font-semibold text-slate-700">
                    {{ selectedPrescription.status && (String(selectedPrescription.status).toLowerCase().includes('dispensed') || String(selectedPrescription.status).toLowerCase().includes('completed') || String(selectedPrescription.status).toLowerCase().includes('hoàn') || String(selectedPrescription.status).toLowerCase().includes('cấp'))
                      ? 'Đã phát hoàn tất lúc ' + formatDateTime(selectedPrescription.sentToPharmacyAt)
                      : 'Đang chuẩn bị thuốc tại quầy dược'
                    }}
                  </span>
                </div>
              </div>
            </div>
          </div>

        </div>
      </div>
    </transition>

    <!-- Custom Toasts -->
    <Toast
      :show="toast.show"
      :title="toast.title"
      :message="toast.message"
      :type="toast.type"
      @close="toast.show = false"
    />

    <!-- Print Area for Prescription -->
    <div v-if="prescriptionToPrint" class="print-area">
      <div class="print-container p-6 bg-white max-w-2xl mx-auto text-slate-800">
        <!-- Logo and System Name -->
        <div class="flex items-center justify-between border-b-2 border-slate-800 pb-4 mb-6">
          <img :src="logoUrl" alt="Logo MedicareDNU" class="h-8 w-auto object-contain" />
          <div class="text-right text-xs text-slate-500">
            <p>Hệ thống quản lý phòng khám MedicareDNU</p>
            <p>Thời gian in: {{ currentPrintDateTime() }}</p>
          </div>
        </div>

        <!-- Document Title -->
        <div class="text-center mb-6">
          <h1 class="text-xl font-bold text-slate-900 tracking-wide uppercase">Đơn thuốc</h1>
          <p class="text-xs text-slate-500 mt-1 font-mono">Mã đơn thuốc: {{ prescriptionToPrint.prescriptionCode || 'DT' + String(prescriptionToPrint.id).padStart(3, '0') }}</p>
        </div>

        <!-- Patient Info -->
        <div class="mb-5">
          <h2 class="text-xs font-bold uppercase tracking-wider text-slate-400 border-b border-slate-200 pb-1 mb-2">Thông tin bệnh nhân</h2>
          <div class="grid grid-cols-2 gap-y-2 text-xs">
            <div><span class="font-bold text-slate-500">Mã bệnh nhân:</span> <span class="font-semibold text-slate-800">{{ currentPatient?.patientCode || currentPatient?.patientIdCode || prescriptionToPrint.patientCode || prescriptionToPrint.patientIdCode || currentPatient?.id || prescriptionToPrint.patientId || 'Chưa có thông tin' }}</span></div>
            <div><span class="font-bold text-slate-500">Họ và tên:</span> <span class="font-semibold text-slate-800">{{ currentPatient?.fullName || authStore.user?.fullName || 'Chưa có thông tin' }}</span></div>
            <div><span class="font-bold text-slate-500">Ngày sinh:</span> <span class="font-semibold text-slate-800">{{ formatDate(currentPatient?.dateOfBirth) }}</span></div>
            <div><span class="font-bold text-slate-500">Giới tính:</span> <span class="font-semibold text-slate-800">{{ genderLabel(currentPatient?.gender) }}</span></div>
            <div class="col-span-2"><span class="font-bold text-slate-500">Số điện thoại:</span> <span class="font-semibold text-slate-800">{{ currentPatient?.phoneNumber || currentPatient?.phone || 'Chưa có thông tin' }}</span></div>
          </div>
        </div>

        <!-- Prescription Info -->
        <div class="mb-5">
          <h2 class="text-xs font-bold uppercase tracking-wider text-slate-400 border-b border-slate-200 pb-1 mb-2">Thông tin đơn thuốc</h2>
          <div class="grid grid-cols-2 gap-y-2 text-xs">
            <div><span class="font-bold text-slate-500">Mã bệnh án:</span> <span class="font-semibold text-slate-800 font-mono">{{ prescriptionToPrint.medicalRecordCode || 'Chưa có thông tin' }}</span></div>
            <div><span class="font-bold text-slate-500">Bác sĩ kê đơn:</span> <span class="font-semibold text-slate-800">{{ associatedDoctorName(prescriptionToPrint) || 'Chưa có thông tin' }}</span></div>
            <div><span class="font-bold text-slate-500">Ngày kê đơn:</span> <span class="font-semibold text-slate-800">{{ formatDateTime(prescriptionToPrint.createdAt) }}</span></div>
            <div><span class="font-bold text-slate-500">Trạng thái xử lý:</span> <span class="font-semibold text-slate-800">{{ statusLabel(prescriptionToPrint.status) }}</span></div>
          </div>
        </div>

        <!-- Medicines List Table -->
        <div class="mb-5">
          <h2 class="text-xs font-bold uppercase tracking-wider text-slate-400 border-b border-slate-200 pb-1 mb-2">Danh mục thuốc</h2>
          <table v-if="(prescriptionToPrint.items || prescriptionToPrint.prescriptionItems || []).length" class="min-w-full border border-slate-200 text-xs mb-3">
            <thead class="bg-slate-50 font-bold text-slate-600 text-left border-b border-slate-200">
              <tr>
                <th class="px-2 py-1.5 border-r border-slate-200 w-10 text-center">STT</th>
                <th class="px-2 py-1.5 border-r border-slate-200">Tên thuốc</th>
                <th class="px-2 py-1.5 border-r border-slate-200 w-20 text-center">Số lượng</th>
                <th class="px-2 py-1.5 border-r border-slate-200">Liều lượng</th>
                <th class="px-2 py-1.5">Cách dùng / Hướng dẫn</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-200 bg-white">
              <tr v-for="(item, index) in (prescriptionToPrint.items || prescriptionToPrint.prescriptionItems)" :key="item.id">
                <td class="px-2 py-1.5 border-r border-slate-200 text-center font-medium">{{ index + 1 }}</td>
                <td class="px-2 py-1.5 border-r border-slate-200 font-bold text-slate-800">{{ item.medicineNameSnapshot || item.medicineName }}</td>
                <td class="px-2 py-1.5 border-r border-slate-200 text-center font-bold">{{ item.quantity }} {{ item.unitSnapshot || 'Viên' }}</td>
                <td class="px-2 py-1.5 border-r border-slate-200 font-medium">{{ item.dosage || 'Chỉ định' }} · {{ item.frequency || 'Chưa cập nhật' }}</td>
                <td class="px-2 py-1.5 font-medium">{{ item.usageInstruction || 'Theo dặn dò' }}</td>
              </tr>
            </tbody>
          </table>
          <div v-else-if="prescriptionToPrint.note" class="bg-slate-50 p-4 rounded-xl border border-slate-200 text-xs text-slate-700 whitespace-pre-line leading-relaxed font-semibold">
            {{ prescriptionToPrint.note }}
          </div>
          <div v-else class="text-xs text-slate-500 italic pl-3">Chưa có thông tin danh sách thuốc chi tiết</div>
        </div>

        <!-- Footnote / Safety notes -->
        <div class="rounded-xl border border-blue-100 bg-blue-50/50 p-3 text-[10px] text-blue-800 leading-relaxed mb-6">
          <span class="font-bold">Lưu ý:</span> Uống thuốc đúng giờ, đúng liều theo chỉ dẫn. Không tự ý ngưng thuốc hoặc thay đổi liều lượng thuốc được bác sĩ kê.
        </div>

        <!-- Signature Block -->
        <div class="mt-8 pt-6 border-t border-slate-200 grid grid-cols-3 text-center text-xs gap-4">
          <div>
            <p class="font-bold text-slate-500 uppercase tracking-wide">Bệnh nhân</p>
            <p class="text-[10px] text-slate-400 mt-0.5">(Ký và ghi rõ họ tên)</p>
            <div class="h-16"></div>
            <p class="font-bold text-slate-800">{{ currentPatient?.fullName || authStore.user?.fullName || '' }}</p>
          </div>
          <div>
            <p class="font-bold text-slate-500 uppercase tracking-wide">Dược sĩ</p>
            <p class="text-[10px] text-slate-400 mt-0.5">(Ký và ghi rõ họ tên)</p>
            <div class="h-16"></div>
            <p class="font-bold text-slate-800"></p>
          </div>
          <div>
            <p class="font-bold text-slate-500 uppercase tracking-wide">Bác sĩ kê đơn</p>
            <p class="text-[10px] text-slate-400 mt-0.5">(Ký và ghi rõ họ tên)</p>
            <div class="h-16"></div>
            <p class="font-bold text-slate-800">{{ associatedDoctorName(prescriptionToPrint) }}</p>
          </div>
        </div>
      </div>
    </div>
  </section>
</template>

<script setup lang="ts">
import { computed, nextTick, onMounted, reactive, ref, watch } from 'vue'
import {
  CalendarClock,
  CheckCircle2,
  ChevronLeft,
  ChevronRight,
  Clock,
  Eye,
  FileText,
  Pill,
  Printer,
  RefreshCw,
  Search,
  Send,
  ShieldAlert,
  X
} from 'lucide-vue-next'
import BaseButton from '@/components/ui/BaseButton.vue'
import Toast from '@/components/ui/Toast.vue'
import { useAuthStore } from '@/stores/authStore'
import { medicalRecordApi } from '@/services/medicalRecordApi'
import { appointmentApi } from '@/services/appointmentApi'
import { billingApi } from '@/services/billingApi'
import { getApiErrorMessage } from '@/services/apiClient'
import type { Prescription } from '@/types/billing'
import type { Appointment } from '@/types/appointment'
import type { Patient } from '@/types/medicalRecord'
import type { Doctor } from '@/types/doctor'
import logoUrl from '@/assets/logo.png'

const authStore = useAuthStore()
const loading = ref(true)
const error = ref('')

// Raw Data
const currentPatient = ref<Patient | null>(null)
const prescriptions = ref<Prescription[]>([])
const appointments = ref<Appointment[]>([])
const doctorsList = ref<Doctor[]>([])

// Filters
const filters = reactive({
  search: '',
  status: 'ALL',
  startDate: '',
  endDate: '',
})

// Selected State (Drawer)
const drawerOpen = ref(false)
const selectedPrescription = ref<Prescription | null>(null)
const currentTab = ref('overview')

// Print State
const prescriptionToPrint = ref<Prescription | null>(null)

const drawerTabs = [
  { key: 'overview', label: 'Tổng quan' },
  { key: 'medicines', label: 'Danh sách thuốc' },
  { key: 'instructions', label: 'Hướng dẫn dùng thuốc' },
  { key: 'pharmacy', label: 'Nhà thuốc / xử lý' },
]

// Toasts
const toast = reactive({
  show: false,
  title: '',
  message: '',
  type: 'success' as 'success' | 'error',
})
let toastTimer: ReturnType<typeof setTimeout> | null = null

watch(() => toast.show, (visible) => {
  if (toastTimer) clearTimeout(toastTimer)
  if (visible) toastTimer = setTimeout(() => { toast.show = false }, 3000)
})

// Statistics computation
const stats = computed(() => {
  const total = prescriptions.value.length
  
  const pending = prescriptions.value.filter(p => {
    const s = String(p.status || '').toLowerCase()
    return s.includes('pending') || s.includes('chờ')
  }).length

  const sent = prescriptions.value.filter(p => {
    const s = String(p.status || '').toLowerCase()
    return s.includes('sent') || s.includes('gửi')
  }).length

  const completed = prescriptions.value.filter(p => {
    const s = String(p.status || '').toLowerCase()
    return s.includes('dispensed') || s.includes('completed') || s.includes('hoàn') || s.includes('cấp')
  }).length

  return { total, pending, sent, completed }
})

// Filter computation
const filteredPrescriptions = computed(() => {
  return prescriptions.value.filter(p => {
    // 1. Search Query filter
    if (filters.search.trim()) {
      const q = filters.search.trim().toLowerCase()
      
      const prescriptionCode = String(p.prescriptionCode || 'DT' + String(p.id || '')).toLowerCase()
      const statusText = String(p.status || '').toLowerCase()
      const noteText = String(p.note || '').toLowerCase()
      
      // Medicines snapshot text
      const items = p.items || p.prescriptionItems || []
      const medicinesText = items.map(item => item.medicineNameSnapshot || item.medicineName || '').join(' ').toLowerCase()

      const matches =
        prescriptionCode.includes(q) ||
        statusText.includes(q) ||
        noteText.includes(q) ||
        medicinesText.includes(q)

      if (!matches) return false
    }

    // 2. Status filter
    if (filters.status !== 'ALL') {
      const s = String(p.status || '').toUpperCase()
      if (filters.status === 'PENDING' && !s.includes('PENDING') && !s.includes('CHỜ')) return false
      if (filters.status === 'SENT_TO_PHARMACY' && !s.includes('SENT') && !s.includes('GỬI')) return false
      if (filters.status === 'DISPENSED' && !s.includes('DISPENSED') && !s.includes('CẤP')) return false
      if (filters.status === 'COMPLETED' && !s.includes('COMPLETED') && !s.includes('HOÀN')) return false
      if (filters.status === 'CANCELLED' && !s.includes('CANCEL') && !s.includes('HỦY')) return false
    }

    // 3. Date range filter
    const createdDate = p.createdAt ? new Date(p.createdAt) : null
    if (createdDate) {
      if (filters.startDate) {
        const start = new Date(filters.startDate)
        start.setHours(0, 0, 0, 0)
        if (createdDate < start) return false
      }
      if (filters.endDate) {
        const end = new Date(filters.endDate)
        end.setHours(23, 59, 59, 999)
        if (createdDate > end) return false
      }
    }

    return true
  })
})

// Pagination
const currentPage = ref(1)
const itemsPerPage = ref(10)

watch(filters, () => {
  currentPage.value = 1
})

const totalPages = computed(() => Math.ceil(filteredPrescriptions.value.length / itemsPerPage.value))

const paginatedPrescriptions = computed(() => {
  const start = (currentPage.value - 1) * itemsPerPage.value
  const end = start + itemsPerPage.value
  return filteredPrescriptions.value.slice(start, end)
})

onMounted(loadData)

async function loadData() {
  loading.value = true
  error.value = ''
  prescriptions.value = []

  try {
    const patient = await medicalRecordApi.getCurrentPatient()
    const patientIdVal = Number(patient.id || patient.patientId)

    const [timeline, n3Prescriptions, doctors] = await Promise.all([
      medicalRecordApi.getCurrentPatientClinicalTimeline().catch((err) => {
        if ((err as any)?.response?.status === 404) {
          return { visits: [], medicalRecords: [], prescriptions: [] }
        }
        throw err
      }),
      Number.isFinite(patientIdVal) && patientIdVal > 0
        ? billingApi.getPrescriptions(patientIdVal).catch((err) => {
          if ((err as any)?.response?.status === 404) return [] as Prescription[]
          throw err
        })
        : Promise.resolve([] as Prescription[]),
      appointmentApi.getDoctors().catch(() => []),
    ])

    currentPatient.value = patient
    doctorsList.value = doctors
    if (Number.isFinite(patientIdVal) && patientIdVal > 0 && authStore.user) {
      authStore.user.patientId = patientIdVal
    }

    prescriptions.value = mergePrescriptions(timeline.prescriptions || [], n3Prescriptions)
  } catch (err) {
    error.value = getApiErrorMessage(err)
    showToast('Lỗi tải đơn thuốc', error.value, 'error')
  } finally {
    loading.value = false
  }
}

function openDetails(prescription: Prescription) {
  selectedPrescription.value = prescription
  currentTab.value = 'overview'
  drawerOpen.value = true
}

function closeDrawer() {
  drawerOpen.value = false
  selectedPrescription.value = null
}

function resetFilters() {
  filters.search = ''
  filters.status = 'ALL'
  filters.startDate = ''
  filters.endDate = ''
}

function currentPrintDateTime() {
  return formatDateTime(new Date().toISOString())
}

async function printPrescription(prescription: Prescription) {
  prescriptionToPrint.value = prescription
  showToast('In đơn thuốc', 'Đang chuẩn bị bản in...', 'success')
  await nextTick()
  setTimeout(() => {
    window.print()
  }, 300)
}

function triggerPrint() {
  if (selectedPrescription.value) {
    printPrescription(selectedPrescription.value)
    return
  }
  if (prescriptions.value.length === 1) {
    printPrescription(prescriptions.value[0])
    return
  }
  if (prescriptions.value.length === 0) {
    showToast('In đơn thuốc', 'Chưa có dữ liệu đơn thuốc để in', 'error')
    return
  }
  showToast('In đơn thuốc', 'Vui lòng chọn đơn thuốc cần in', 'error')
}

// Helpers for medicines lists
function displayMedicines(prescription: Prescription) {
  const items = prescription.items || prescription.prescriptionItems || []
  if (items.length) {
    const names = items.map(item => item.medicineNameSnapshot || item.medicineName).filter(Boolean)
    if (names.length <= 2) return names.join(', ')
    return `${names.slice(0, 2).join(', ')} +${names.length - 2} thuốc khác`
  }
  if (prescription.note) {
    const lines = prescription.note.split('\n').map(l => l.split(':')[0].trim()).filter(Boolean)
    if (lines.length && lines.length <= 2) return lines.join(', ')
    if (lines.length > 2) return `${lines.slice(0, 2).join(', ')} +${lines.length - 2} thuốc khác`
    return prescription.note.length > 50 ? prescription.note.slice(0, 50) + '...' : prescription.note
  }
  return 'Chưa kê thuốc'
}

// Global format date helpers matching records page formatter
function formatDate(value?: string) {
  if (!value) return 'Chưa có'
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return value
  const day = String(date.getDate()).padStart(2, '0')
  const month = String(date.getMonth() + 1).padStart(2, '0')
  const year = date.getFullYear()
  return `${day}/${month}/${year}`
}

function allMedicinesText(prescription: Prescription) {
  const items = prescription.items || prescription.prescriptionItems || []
  if (items.length) {
    return items.map(item => item.medicineNameSnapshot || item.medicineName).filter(Boolean).join(', ')
  }
  return prescription.note || 'Chưa kê thuốc'
}

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
  10: 'BS. Trịnh Quang Minh'
}

function associatedDoctorName(prescription: Prescription) {
  const docId = Number(prescription.doctorId)
  if (!docId) return ''
  const appt = appointments.value.find(a => Number(a.doctorId) === docId)
  if (appt?.doctorName) return appt.doctorName
  
  const doc = doctorsList.value.find(d => Number(d.doctorId) === docId)
  if (doc?.doctorName || doc?.fullName) return doc.doctorName || doc.fullName
  
  return doctorNamesMap[docId] || `Bác sĩ #${docId}`
}

function mergePrescriptions(n2List: Prescription[], n3List: Prescription[]): Prescription[] {
  const mergedMap = new Map<string, Prescription>()

  const getMergeKey = (p: Prescription): string[] => {
    const keys: string[] = []
    const pid = p.prescriptionId || p.id
    if (pid) {
      keys.push(`id-${pid}`)
    }
    if (p.prescriptionCode) {
      keys.push(`code-${p.prescriptionCode}`)
    }
    if (p.medicalRecordId) {
      keys.push(`medid-${p.medicalRecordId}`)
    }
    if (p.medicalRecordCode) {
      keys.push(`medcode-${p.medicalRecordCode}`)
    }
    if (p.appointmentId) {
      keys.push(`apptid-${p.appointmentId}`)
    }
    return keys
  }

  // Index N2 prescriptions first
  for (const p of n2List) {
    const keys = getMergeKey(p)
    const normalized = {
      ...p,
      id: p.id || p.prescriptionId,
      prescriptionId: p.prescriptionId || p.id,
      items: p.items || p.prescriptionItems || []
    }
    if (keys.length) {
      mergedMap.set(keys[0], normalized)
      for (let i = 1; i < keys.length; i++) {
        mergedMap.set(keys[i], normalized)
      }
    } else {
      mergedMap.set(`random-${Math.random()}`, normalized)
    }
  }

  // Merge with N3 prescriptions
  for (const p of n3List) {
    const keys = getMergeKey(p)
    let existing: Prescription | undefined
    for (const k of keys) {
      if (mergedMap.has(k)) {
        existing = mergedMap.get(k)
        break
      }
    }

    const n3Normalized = {
      ...p,
      id: p.id || p.prescriptionId,
      prescriptionId: p.prescriptionId || p.id,
      items: p.items || p.prescriptionItems || []
    }

    if (existing) {
      existing.id = existing.id || n3Normalized.id
      existing.prescriptionId = existing.prescriptionId || n3Normalized.prescriptionId
      existing.prescriptionCode = existing.prescriptionCode || n3Normalized.prescriptionCode
      existing.medicalRecordId = existing.medicalRecordId || n3Normalized.medicalRecordId
      existing.medicalRecordCode = existing.medicalRecordCode || n3Normalized.medicalRecordCode
      existing.appointmentId = existing.appointmentId || n3Normalized.appointmentId
      existing.status = existing.status || n3Normalized.status
      existing.note = existing.note || n3Normalized.note
      existing.createdAt = existing.createdAt || n3Normalized.createdAt
      existing.sentToPharmacyAt = existing.sentToPharmacyAt || n3Normalized.sentToPharmacyAt

      const existingItems = existing.items || []
      const n3Items = n3Normalized.items || []
      if (existingItems.length === 0 && n3Items.length > 0) {
        existing.items = n3Items
      } else if (existingItems.length > 0 && n3Items.length > 0) {
        const itemMap = new Map<string, any>()
        for (const item of existingItems) {
          const key = String(item.medicineId || item.medicineNameSnapshot || item.medicineName || '')
          itemMap.set(key, item)
        }
        for (const item of n3Items) {
          const key = String(item.medicineId || item.medicineNameSnapshot || item.medicineName || '')
          if (!itemMap.has(key)) {
            itemMap.set(key, item)
          } else {
            const expItem = itemMap.get(key)
            expItem.quantity = expItem.quantity || item.quantity
            expItem.unitSnapshot = expItem.unitSnapshot || item.unitSnapshot
            expItem.dosage = expItem.dosage || item.dosage
            expItem.frequency = expItem.frequency || item.frequency
            expItem.usageInstruction = expItem.usageInstruction || item.usageInstruction
          }
        }
        existing.items = Array.from(itemMap.values())
      }
    } else {
      if (keys.length) {
        mergedMap.set(keys[0], n3Normalized)
        for (let i = 1; i < keys.length; i++) {
          mergedMap.set(keys[i], n3Normalized)
        }
      } else {
        mergedMap.set(`random-${Math.random()}`, n3Normalized)
      }
    }
  }

  const resultList = Array.from(new Set(mergedMap.values()))
  resultList.sort((a, b) => {
    const da = a.createdAt ? new Date(a.createdAt).getTime() : 0
    const db = b.createdAt ? new Date(b.createdAt).getTime() : 0
    return db - da
  })
  return resultList
}

function genderLabel(value?: string) {
  const normalized = String(value || '').toLowerCase()
  if (normalized === 'male' || normalized === 'nam') return 'Nam'
  if (normalized === 'female' || normalized === 'nữ' || normalized === 'nu') return 'Nữ'
  return value || 'Chưa cập nhật'
}

function formatDateTime(value?: string) {
  if (!value) return 'Chưa có'
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return value
  const day = String(date.getDate()).padStart(2, '0')
  const month = String(date.getMonth() + 1).padStart(2, '0')
  const year = date.getFullYear()
  const hours = String(date.getHours()).padStart(2, '0')
  const minutes = String(date.getMinutes()).padStart(2, '0')
  return `${day}/${month}/${year} ${hours}:${minutes}`
}

function statusLabel(status?: string) {
  const s = String(status || '').toLowerCase()
  if (s.includes('sent') || s.includes('gửi')) return 'Đã gửi nhà thuốc'
  if (s.includes('pending') || s.includes('chờ')) return 'Chờ xử lý'
  if (s.includes('dispensed') || s.includes('cấp')) return 'Đã cấp thuốc'
  if (s.includes('completed') || s.includes('hoàn')) return 'Hoàn tất'
  if (s.includes('cancel') || s.includes('hủy')) return 'Đã hủy'
  return status || 'Chưa cập nhật'
}

function statusClass(status?: string) {
  const s = String(status || '').toLowerCase()
  if (s.includes('sent') || s.includes('gửi')) return 'bg-emerald-50 text-emerald-700 border-emerald-100'
  if (s.includes('pending') || s.includes('chờ')) return 'bg-amber-50 text-amber-700 border-amber-100'
  if (s.includes('dispensed') || s.includes('cấp')) return 'bg-blue-50 text-blue-700 border-blue-100'
  if (s.includes('completed') || s.includes('hoàn')) return 'bg-emerald-50 text-emerald-700 border-emerald-100'
  if (s.includes('cancel') || s.includes('hủy')) return 'bg-rose-50 text-rose-700 border-rose-100'
  return 'bg-slate-50 text-slate-700 border-slate-100'
}

function showToast(title: string, message: string, type: 'success' | 'error' = 'success') {
  toast.title = title
  toast.message = message
  toast.type = type
  toast.show = true
}
</script>

<style scoped>
.line-clamp-2 {
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
}
</style>

<style>
@media print {
  body * {
    visibility: hidden !important;
  }
  .print-area,
  .print-area * {
    visibility: visible !important;
  }
  .print-area {
    display: block !important;
    position: absolute !important;
    left: 0 !important;
    top: 0 !important;
    width: 100% !important;
    background: white !important;
    color: black !important;
    padding: 0 !important;
    margin: 0 !important;
  }
  
  @page {
    size: A4;
    margin: 15mm;
  }
}

/* Hide print area by default on screen */
.print-area {
  display: none !important;
}
</style>
