<template>
  <section class="min-h-screen bg-[#f8fafc] py-2 sm:py-3">
    <FullscreenLoader :show="loading" />

    <div class="max-w-none mx-auto px-4 sm:px-6 lg:px-8 space-y-6">
      
      <header class="px-1">
        <h1 class="text-[1.75rem] font-semibold tracking-normal text-slate-950">Đơn thuốc của tôi</h1>
       
      </header>

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

      <div class="rounded-2xl border border-slate-200 bg-white shadow-sm overflow-hidden">
        <div class="flex items-center justify-between border-b border-slate-100 p-4 bg-slate-50/50">
          <span class="text-sm font-semibold text-slate-500">
            Tổng số {{ filteredPrescriptions.length }} kết quả đơn thuốc
          </span>
        </div>

        <div v-if="filteredPrescriptions.length" class="prescription-table-shell">
          <ATable
            :columns="prescriptionTableColumns"
            :data-source="filteredPrescriptions"
            :pagination="prescriptionPagination"
            :row-key="prescriptionRowKey"
            :scroll="{ x: 1080 }"
            size="middle"
            @change="handlePrescriptionTableChange"
          >
            <template #customFilterDropdown="{ setSelectedKeys, selectedKeys, confirm, clearFilters, column }">
              <div class="prescription-filter">
                <p class="prescription-filter-title">Tìm theo {{ String(column.title).toLowerCase() }}</p>
                <AInput
                  :value="selectedKeys[0]"
                  :placeholder="`Nhập ${String(column.title).toLowerCase()}...`"
                  allow-clear
                  autofocus
                  @change="setSelectedKeys(getPrescriptionFilterKeys($event))"
                  @press-enter="confirm()"
                >
                  <template #prefix><Search class="h-3.5 w-3.5 text-slate-400" /></template>
                </AInput>
                <div class="prescription-filter-actions">
                  <AButton size="small" class="prescription-filter-reset" @click="clearPrescriptionFilter(clearFilters, confirm)">Đặt lại</AButton>
                  <AButton type="primary" size="small" class="prescription-filter-submit" @click="confirm()">Áp dụng</AButton>
                </div>
              </div>
            </template>
            <template #customFilterIcon="{ filtered }">
              <Search :class="['h-3.5 w-3.5', filtered ? 'text-[#0F52BA]' : 'text-slate-400']" />
            </template>
            <template #emptyText>
              <div class="py-8 text-center">
                <Pill class="mx-auto h-9 w-9 text-slate-300" />
                <p class="mt-3 font-bold text-slate-800">Không có đơn thuốc phù hợp</p>
                <p class="mt-1 text-sm text-slate-500">Thử đổi bộ lọc hoặc từ khóa tìm kiếm trong từng cột.</p>
              </div>
            </template>
            <template #bodyCell="{ column, record }">
              <template v-if="column.key === 'code'">
                <span class="font-bold text-slate-950">{{ prescriptionCode(record) }}</span>
              </template>
              <template v-else-if="column.key === 'createdAt'">
                <span class="whitespace-nowrap text-[13px] font-medium text-slate-500">{{ formatDateTime(record.createdAt) }}</span>
              </template>
              <template v-else-if="column.key === 'medicines'">
                <div class="medicine-button-group" :title="allMedicinesText(record)">
                  <template v-if="prescriptionMedicineNames(record).length">
                    <AButton
                      v-for="(medicine, index) in prescriptionMedicineNames(record)"
                      :key="`${prescriptionRowKey(record)}-${medicine}-${index}`"
                      size="small"
                      :class="['medicine-chip-button', medicineButtonClass(index)]"
                    >
                      {{ medicine }}
                    </AButton>
                  </template>
                  <span v-else class="text-sm font-medium text-slate-400">Chưa kê thuốc</span>
                </div>
              </template>
              <template v-else-if="column.key === 'medicineCount'">
                <span class="inline-flex min-w-9 justify-center rounded-full bg-slate-100 px-3 py-1 text-sm font-bold text-slate-600">
                  {{ prescriptionMedicineCount(record) || '-' }}
                </span>
              </template>
              <template v-else-if="column.key === 'status'">
                <ATag :bordered="false" :class="['prescription-status-tag', statusClass(record.status)]">
                  <span class="prescription-status-dot"></span>
                  {{ statusLabel(record.status) }}
                </ATag>
              </template>
              <template v-else-if="column.key === 'actions'">
                <button type="button" class="prescription-action-button" title="Xem chi tiết đơn thuốc" @click="openDetails(record)">
                  <Eye class="h-4 w-4" />
                  <span>Chi tiết</span>
                </button>
              </template>
            </template>
          </ATable>
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
          <div class="flex items-center">
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
                <span :class="['flex h-8 w-8 shrink-0 items-center justify-center rounded-lg border', isDispensedStatus(selectedPrescription.status) ? 'bg-emerald-50 text-emerald-600 border-emerald-100' : 'bg-amber-50 text-amber-600 border-amber-100']">
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
                    {{ pharmacyStatusMessage(selectedPrescription) }}
                  </span>
                </div>
                <div class="flex justify-between" v-if="selectedPrescription.invoiceStatus">
                  <span>Trạng thái viện phí:</span>
                  <span class="font-semibold text-slate-700">{{ invoiceStatusLabel(selectedPrescription.invoiceStatus) }}</span>
                </div>
                <div class="flex justify-between" v-if="selectedPrescription.stockStatus">
                  <span>Trạng thái tồn kho:</span>
                  <span class="font-semibold text-slate-700">{{ stockStatusLabel(selectedPrescription.stockStatus) }}</span>
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

  </section>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { Button as AButton, Input as AInput, Table as ATable, Tag as ATag } from 'ant-design-vue'
import {
  CheckCircle2,
  Clock,
  Eye,
  Pill,
  RefreshCw,
  Search,
  Send,
  ShieldAlert,
  X
} from 'lucide-vue-next'
import BaseButton from '@/components/ui/BaseButton.vue'
import FullscreenLoader from '@/components/ui/FullscreenLoader.vue'
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
  
  const pending = prescriptions.value.filter(p => ['pending', 'processing', 'partial', 'outOfStock'].includes(prescriptionStatusBucket(p.status))).length

  const sent = prescriptions.value.filter(p => ['sent', 'ready'].includes(prescriptionStatusBucket(p.status))).length

  const completed = prescriptions.value.filter(p => ['dispensed', 'completed'].includes(prescriptionStatusBucket(p.status))).length

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
      const bucket = prescriptionStatusBucket(p.status)
      if (filters.status === 'PENDING' && !['pending', 'processing', 'partial', 'outOfStock'].includes(bucket)) return false
      if (filters.status === 'SENT_TO_PHARMACY' && !['sent', 'ready'].includes(bucket)) return false
      if (filters.status === 'DISPENSED' && bucket !== 'dispensed') return false
      if (filters.status === 'COMPLETED' && !['completed', 'dispensed'].includes(bucket)) return false
      if (filters.status === 'CANCELLED' && bucket !== 'cancelled') return false
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

const prescriptionTableColumns = [
  {
    title: 'Mã đơn',
    key: 'code',
    width: 150,
    customFilterDropdown: true,
    onFilter: prescriptionColumnFilter('code'),
    sorter: (a: Prescription, b: Prescription) => prescriptionCode(a).localeCompare(prescriptionCode(b), 'vi'),
  },
  {
    title: 'Ngày kê',
    dataIndex: 'createdAt',
    key: 'createdAt',
    width: 210,
    customFilterDropdown: true,
    onFilter: prescriptionColumnFilter('createdAt'),
    sorter: (a: Prescription, b: Prescription) => prescriptionTimestamp(a) - prescriptionTimestamp(b),
    defaultSortOrder: 'descend' as const,
  },
  {
    title: 'Thuốc',
    key: 'medicines',
    minWidth: 320,
    customFilterDropdown: true,
    onFilter: prescriptionColumnFilter('medicines'),
  },
  {
    title: 'Số loại',
    key: 'medicineCount',
    width: 140,
    align: 'center' as const,
    customFilterDropdown: true,
    onFilter: prescriptionColumnFilter('medicineCount'),
    sorter: (a: Prescription, b: Prescription) => prescriptionMedicineCount(a) - prescriptionMedicineCount(b),
  },
  {
    title: 'Trạng thái',
    key: 'status',
    width: 230,
    customFilterDropdown: true,
    onFilter: prescriptionColumnFilter('status'),
  },
  {
    title: 'Thao tác',
    key: 'actions',
    width: 150,
    align: 'right' as const,
    fixed: 'right' as const,
  },
]

const prescriptionPagination = computed(() => ({
  current: currentPage.value,
  pageSize: itemsPerPage.value,
  showSizeChanger: true,
  pageSizeOptions: ['10', '20', '50', '100'],
  showLessItems: true,
  showTitle: false,
  responsive: true,
  showTotal: (total: number, range: [number, number]) => `Hiển thị ${range[0]} - ${range[1]} trên ${total} kết quả`,
  locale: { items_per_page: ' / trang' },
}))

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

// Helpers for medicines lists
function prescriptionRowKey(prescription: Prescription) {
  return String(
    prescription.id ||
    prescription.prescriptionId ||
    prescription.prescriptionCode ||
    prescription.createdAt ||
    `prescription-${prescriptions.value.indexOf(prescription)}`,
  )
}

function prescriptionCode(prescription: Prescription) {
  const id = prescription.id || prescription.prescriptionId
  return prescription.prescriptionCode || prescription.prescriptionIdCode || `DT${String(id || 0).padStart(3, '0')}`
}

function prescriptionItems(prescription: Prescription) {
  return prescription.items || prescription.prescriptionItems || []
}

function prescriptionMedicineNames(prescription: Prescription) {
  const names = prescriptionItems(prescription)
    .map(item => item.medicineNameSnapshot || item.medicineName)
    .filter(Boolean) as string[]
  if (names.length) return names
  if (!prescription.note) return []
  return prescription.note
    .split('\n')
    .map(line => line.split(':')[0].trim())
    .filter(Boolean)
}

function prescriptionMedicineCount(prescription: Prescription) {
  const itemCount = prescriptionItems(prescription).length
  return itemCount || prescriptionMedicineNames(prescription).length
}

function prescriptionTimestamp(prescription: Prescription) {
  const time = prescription.createdAt ? new Date(prescription.createdAt).getTime() : 0
  return Number.isNaN(time) ? 0 : time
}

function medicineButtonClass(index: number) {
  const classes = [
    'medicine-chip-blue',
    'medicine-chip-emerald',
    'medicine-chip-amber',
    'medicine-chip-rose',
    'medicine-chip-cyan',
    'medicine-chip-violet',
  ]
  return classes[index % classes.length]
}

function prescriptionSearchField(prescription: Prescription, key: string) {
  if (key === 'code') return prescriptionCode(prescription)
  if (key === 'createdAt') return formatDateTime(prescription.createdAt)
  if (key === 'medicines') return allMedicinesText(prescription)
  if (key === 'medicineCount') return String(prescriptionMedicineCount(prescription) || '-')
  if (key === 'status') return statusLabel(prescription.status)
  return ''
}

function prescriptionColumnFilter(key: string) {
  return (filterValue: string | number | boolean, record: Prescription) =>
    normalizeSearchText(prescriptionSearchField(record, key)).includes(normalizeSearchText(filterValue))
}

function normalizeSearchText(valueToNormalize: unknown) {
  return String(valueToNormalize || '')
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .replace(/đ/g, 'd')
    .replace(/Đ/g, 'D')
    .toLowerCase()
    .trim()
}

function getPrescriptionFilterKeys(event: Event) {
  const filterValue = (event.target as HTMLInputElement)?.value || ''
  return filterValue ? [filterValue] : []
}

function clearPrescriptionFilter(clearFilters: (() => void) | undefined, confirm: () => void) {
  clearFilters?.()
  confirm()
}

function handlePrescriptionTableChange(pagination: { current?: number; pageSize?: number }) {
  currentPage.value = pagination.current || 1
  itemsPerPage.value = pagination.pageSize || 10
}

function displayMedicines(prescription: Prescription) {
  const names = prescriptionMedicineNames(prescription)
  if (names.length) {
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
  const names = prescriptionMedicineNames(prescription)
  if (names.length) return names.join(', ')
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
      existing.status = n3Normalized.status || existing.status
      existing.stockStatus = n3Normalized.stockStatus || existing.stockStatus
      existing.invoiceStatus = n3Normalized.invoiceStatus || existing.invoiceStatus
      existing.canApprove = n3Normalized.canApprove ?? existing.canApprove
      existing.canDispense = n3Normalized.canDispense ?? existing.canDispense
      existing.note = existing.note || n3Normalized.note
      existing.createdAt = n3Normalized.createdAt || existing.createdAt
      existing.sentToPharmacyAt = n3Normalized.sentToPharmacyAt || existing.sentToPharmacyAt
      existing.submittedAt = n3Normalized.submittedAt || existing.submittedAt
      existing.dispensedAt = n3Normalized.dispensedAt || existing.dispensedAt

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

function prescriptionStatusBucket(status?: string) {
  const s = String(status || '').trim().toLowerCase()
  if (!s) return 'unknown'
  if (s.includes('cancel') || s.includes('hủy')) return 'cancelled'
  if (s.includes('dispensed') || s.includes('đã phát') || s.includes('da phat') || s.includes('cấp')) return 'dispensed'
  if (s.includes('readytodispense') || s.includes('ready_to_dispense') || s.includes('ready') || s.includes('sẵn sàng')) return 'ready'
  if (s.includes('partiallyavailable') || s.includes('partial') || s.includes('một phần')) return 'partial'
  if (s.includes('outofstock') || s.includes('out_of_stock') || s.includes('thiếu')) return 'outOfStock'
  if (s.includes('senttopharmacy') || s.includes('sent_to_pharmacy') || s.includes('sent') || s.includes('gửi')) return 'sent'
  if (s.includes('processing') || s.includes('đang xử')) return 'processing'
  if (s.includes('completed') || s.includes('complete') || s.includes('hoàn')) return 'completed'
  if (s.includes('pending') || s.includes('chờ')) return 'pending'
  return 'unknown'
}

function statusLabel(status?: string) {
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

function statusClass(status?: string) {
  const bucket = prescriptionStatusBucket(status)
  if (bucket === 'sent') return 'bg-cyan-50 text-cyan-700 border-cyan-100'
  if (bucket === 'ready') return 'bg-sky-50 text-sky-700 border-sky-100'
  if (bucket === 'pending' || bucket === 'processing') return 'bg-amber-50 text-amber-700 border-amber-100'
  if (bucket === 'partial' || bucket === 'outOfStock') return 'bg-orange-50 text-orange-700 border-orange-100'
  if (bucket === 'dispensed' || bucket === 'completed') return 'bg-emerald-50 text-emerald-700 border-emerald-100'
  if (bucket === 'cancelled') return 'bg-rose-50 text-rose-700 border-rose-100'
  return 'bg-slate-50 text-slate-700 border-slate-100'
}

function isDispensedStatus(status?: string) {
  return ['dispensed', 'completed'].includes(prescriptionStatusBucket(status))
}

function pharmacyStatusMessage(prescription: Prescription) {
  const bucket = prescriptionStatusBucket(prescription.status)
  if (bucket === 'dispensed' || bucket === 'completed') {
    const time = prescription.dispensedAt || prescription.submittedAt || prescription.sentToPharmacyAt
    return time ? `Đã phát thuốc lúc ${formatDateTime(time)}` : 'Đã phát thuốc'
  }
  if (bucket === 'ready') return 'Sẵn sàng phát thuốc tại quầy dược'
  if (bucket === 'sent') return 'Đã gửi sang nhà thuốc, đang chờ kiểm kho'
  if (bucket === 'partial') return 'Nhà thuốc thiếu một phần thuốc trong đơn'
  if (bucket === 'outOfStock') return 'Nhà thuốc đang thiếu thuốc'
  if (bucket === 'cancelled') return 'Đơn thuốc đã hủy'
  return 'Chưa phát thuốc'
}

function invoiceStatusLabel(status?: string) {
  const s = String(status || '').toLowerCase()
  if (s.includes('partial')) return 'Thanh toán một phần'
  if (s.includes('paid') && !s.includes('unpaid')) return 'Đã thanh toán'
  if (s.includes('cancel')) return 'Đã hủy'
  if (s.includes('unpaid')) return 'Chưa thanh toán'
  return status || 'Chưa cập nhật'
}

function stockStatusLabel(status?: string) {
  const s = String(status || '').toLowerCase()
  if (s.includes('available') && !s.includes('partial')) return 'Đủ thuốc'
  if (s.includes('partial')) return 'Thiếu một phần'
  if (s.includes('out') || s.includes('shortage')) return 'Thiếu thuốc'
  return status || 'Chưa cập nhật'
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

.prescription-table-shell {
  overflow: hidden;
}

.prescription-table-shell :deep(.ant-table) {
  color: #334155;
  font-size: 14px;
}

.prescription-table-shell :deep(.ant-table-thead > tr > th) {
  background: #f8fafc;
  border-bottom: 1px solid #e2e8f0;
  color: #64748b;
  font-size: 12px;
  font-weight: 800;
  letter-spacing: 0;
  padding: 16px 20px;
  text-transform: uppercase;
}

.prescription-table-shell :deep(.ant-table-tbody > tr > td) {
  border-bottom: 1px solid #f1f5f9;
  padding: 18px 20px;
  vertical-align: middle;
}

.prescription-table-shell :deep(.ant-table-tbody > tr:hover > td) {
  background: #f8fafc;
}

.prescription-table-shell :deep(.ant-table-cell-fix-right) {
  background: #fff;
}

.prescription-table-shell :deep(.ant-table-tbody > tr:hover > .ant-table-cell-fix-right) {
  background: #f8fafc;
}

.prescription-table-shell :deep(.ant-pagination) {
  border-top: 1px solid #f1f5f9;
  margin: 0;
  padding: 16px;
}

.prescription-filter {
  width: 260px;
  padding: 12px;
}

.prescription-filter-title {
  color: #475569;
  font-size: 12px;
  font-weight: 800;
  margin: 0 0 8px;
}

.prescription-filter-actions {
  display: flex;
  gap: 8px;
  justify-content: flex-end;
  margin-top: 10px;
}

.prescription-filter-reset {
  border-color: #e2e8f0;
  color: #64748b;
  font-weight: 700;
}

.prescription-filter-submit {
  background: #0F52BA;
  border-color: #0F52BA;
  font-weight: 700;
}

.medicine-button-group {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
  max-width: 460px;
}

.medicine-chip-button {
  border: 0;
  border-radius: 999px;
  box-shadow: none;
  font-size: 12px;
  font-weight: 800;
  height: 30px;
  max-width: 210px;
  overflow: hidden;
  padding: 0 12px;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.medicine-chip-blue {
  background: #eaf3ff;
  color: #0F52BA;
}

.medicine-chip-emerald {
  background: #dcfce7;
  color: #047857;
}

.medicine-chip-amber {
  background: #fef3c7;
  color: #b45309;
}

.medicine-chip-rose {
  background: #ffe4e6;
  color: #be123c;
}

.medicine-chip-cyan {
  background: #cffafe;
  color: #0e7490;
}

.medicine-chip-violet {
  background: #ede9fe;
  color: #6d28d9;
}

.prescription-status-tag {
  align-items: center;
  border-radius: 999px;
  display: inline-flex;
  font-size: 12px;
  font-weight: 800;
  gap: 6px;
  line-height: 1;
  margin: 0;
  padding: 8px 12px;
}

.prescription-status-dot {
  background: currentColor;
  border-radius: 999px;
  height: 7px;
  width: 7px;
}

.prescription-action-button {
  align-items: center;
  background: #eff6ff;
  border: 1px solid #dbeafe;
  border-radius: 999px;
  color: #1d4ed8;
  display: inline-flex;
  font-size: 13px;
  font-weight: 800;
  gap: 8px;
  height: 36px;
  justify-content: center;
  padding: 0 14px;
  transition: background .2s, border-color .2s, color .2s;
}

.prescription-action-button:hover {
  background: #dbeafe;
  border-color: #bfdbfe;
  color: #0F52BA;
}
</style>
