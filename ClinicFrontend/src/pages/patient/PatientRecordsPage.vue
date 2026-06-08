<template>
  <section class="min-h-screen bg-[#f8fafc] py-6 sm:py-8">
    <div class="max-w-none mx-auto px-4 sm:px-6 lg:px-8 space-y-6">
      
      <!-- 1. Header trang -->
      <div class="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between rounded-2xl border border-slate-200 bg-white p-6 shadow-sm">
        <div class="flex items-start gap-4">
          <span class="flex h-12 w-12 shrink-0 items-center justify-center rounded-xl bg-blue-50 text-blue-600">
            <FileHeart class="h-6 w-6" />
          </span>
          <div>
            <h1 class="text-2xl font-bold tracking-tight text-slate-900">Hồ sơ bệnh án</h1>
            <p class="mt-1 text-sm text-slate-500">
              Theo dõi chẩn đoán, ghi chú bác sĩ, kế hoạch điều trị và lịch tái khám.
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
          <BaseButton class="bg-blue-600 hover:bg-blue-700 text-white" @click="triggerPrint">
            <template #icon>
              <Printer class="h-4 w-4" />
            </template>
            In hồ sơ
          </BaseButton>
        </div>
      </div>

      <!-- 2. Stats Grid -->
      <div class="grid gap-4 grid-cols-1 sm:grid-cols-2 lg:grid-cols-4">
        <!-- Card 1: Tổng hồ sơ -->
        <div class="group rounded-2xl border border-slate-200 bg-white p-5 shadow-sm transition hover:border-blue-200 hover:shadow-[0_12px_24px_rgba(15,82,186,0.06)] flex flex-col justify-between min-h-[140px]">
          <div class="flex items-center justify-between">
            <span class="text-sm font-bold text-slate-700">Tổng số hồ sơ</span>
            <span class="flex h-10 w-10 items-center justify-center rounded-xl bg-blue-50 text-blue-600">
              <FileHeart class="h-5 w-5" />
            </span>
          </div>
          <div class="mt-4">
            <p class="text-3xl font-extrabold text-slate-900 tracking-tight">{{ stats.total }}</p>
            <p class="mt-1 text-xs text-slate-500 font-medium">Tất cả hồ sơ bệnh án</p>
          </div>
        </div>

        <!-- Card 2: Đã hoàn tất -->
        <div class="group rounded-2xl border border-slate-200 bg-white p-5 shadow-sm transition hover:border-emerald-200 hover:shadow-[0_12px_24px_rgba(16,185,129,0.06)] flex flex-col justify-between min-h-[140px]">
          <div class="flex items-center justify-between">
            <span class="text-sm font-bold text-slate-700">Đã hoàn tất</span>
            <span class="flex h-10 w-10 items-center justify-center rounded-xl bg-emerald-50 text-emerald-600">
              <CheckCircle2 class="h-5 w-5" />
            </span>
          </div>
          <div class="mt-4">
            <p class="text-3xl font-extrabold text-slate-900 tracking-tight">{{ stats.completed }}</p>
            <p class="mt-1 text-xs text-slate-500 font-medium">Khám xong & lưu kết quả</p>
          </div>
        </div>

        <!-- Card 3: Bản nháp -->
        <div class="group rounded-2xl border border-slate-200 bg-white p-5 shadow-sm transition hover:border-amber-200 hover:shadow-[0_12px_24px_rgba(245,158,11,0.06)] flex flex-col justify-between min-h-[140px]">
          <div class="flex items-center justify-between">
            <span class="text-sm font-bold text-slate-700">Bản nháp</span>
            <span class="flex h-10 w-10 items-center justify-center rounded-xl bg-amber-50 text-amber-600">
              <FilePenLine class="h-5 w-5" />
            </span>
          </div>
          <div class="mt-4">
            <p class="text-3xl font-extrabold text-slate-900 tracking-tight">{{ stats.draft }}</p>
            <p class="mt-1 text-xs text-slate-500 font-medium">Bệnh án chưa hoàn thành</p>
          </div>
        </div>

        <!-- Card 4: Có lịch tái khám -->
        <div class="group rounded-2xl border border-slate-200 bg-white p-5 shadow-sm transition hover:border-indigo-200 hover:shadow-[0_12px_24px_rgba(99,102,241,0.06)] flex flex-col justify-between min-h-[140px]">
          <div class="flex items-center justify-between">
            <span class="text-sm font-bold text-slate-700">Có lịch tái khám</span>
            <span class="flex h-10 w-10 items-center justify-center rounded-xl bg-indigo-50 text-indigo-600">
              <CalendarClock class="h-5 w-5" />
            </span>
          </div>
          <div class="mt-4">
            <p class="text-3xl font-extrabold text-slate-900 tracking-tight">{{ stats.followUp }}</p>
            <p class="mt-1 text-xs text-slate-500 font-medium">Bệnh nhân có lịch hẹn tới</p>
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

        <div class="grid gap-4 grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-5">
          <!-- Từ khóa tìm kiếm -->
          <label class="relative block">
            <span class="mb-1.5 block text-xs font-bold text-slate-500 uppercase tracking-wider">Từ khóa tìm kiếm</span>
            <span class="relative block">
              <Search class="pointer-events-none absolute left-3.5 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />
              <input
                v-model="filters.search"
                class="h-11 w-full rounded-xl border border-slate-200 bg-white pl-10 pr-4 text-sm outline-none transition focus:border-blue-400 focus:ring-4 focus:ring-blue-100"
                placeholder="Tìm mã, chẩn đoán, ghi chú..."
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
              <option value="COMPLETED">Đã hoàn tất</option>
              <option value="DRAFT">Bản nháp</option>
              <option value="IN_PROGRESS">Đang xử lý</option>
            </select>
          </label>

          <!-- Lịch tái khám -->
          <label class="block">
            <span class="mb-1.5 block text-xs font-bold text-slate-500 uppercase tracking-wider">Lịch tái khám</span>
            <select
              v-model="filters.followUp"
              class="h-11 w-full rounded-xl border border-slate-200 bg-white px-3.5 text-sm outline-none transition focus:border-blue-400 focus:ring-4 focus:ring-blue-100 cursor-pointer"
            >
              <option value="ALL">Tất cả</option>
              <option value="HAS_FOLLOWUP">Có lịch tái khám</option>
              <option value="NO_FOLLOWUP">Chưa có lịch</option>
            </select>
          </label>

          <!-- Từ ngày (Ngày tạo) -->
          <label class="block">
            <span class="mb-1.5 block text-xs font-bold text-slate-500 uppercase tracking-wider">Ngày tạo từ</span>
            <input
              v-model="filters.startDate"
              type="date"
              class="h-11 w-full rounded-xl border border-slate-200 bg-white px-3.5 text-sm outline-none transition focus:border-blue-400 focus:ring-4 focus:ring-blue-100 cursor-pointer"
            />
          </label>

          <!-- Đến ngày (Ngày tạo) -->
          <label class="block">
            <span class="mb-1.5 block text-xs font-bold text-slate-500 uppercase tracking-wider">Ngày tạo đến</span>
            <input
              v-model="filters.endDate"
              type="date"
              class="h-11 w-full rounded-xl border border-slate-200 bg-white px-3.5 text-sm outline-none transition focus:border-blue-400 focus:ring-4 focus:ring-blue-100 cursor-pointer"
            />
          </label>
        </div>
      </div>

      <!-- 5. Bảng danh sách hồ sơ bệnh án -->
      <div v-if="error" class="rounded-xl border border-amber-200 bg-amber-50 p-4 text-sm text-amber-800">
        {{ error }}
      </div>

      <div v-if="loading" class="grid gap-4 md:grid-cols-3">
        <LoadingSkeleton v-for="item in 3" :key="item" />
      </div>

      <div v-else class="rounded-2xl border border-slate-200 bg-white shadow-sm overflow-hidden">
        <div class="flex items-center justify-between border-b border-slate-100 p-4 bg-slate-50/50">
          <span class="text-sm font-semibold text-slate-500">
            Tổng số {{ filteredRecords.length }} kết quả bệnh án
          </span>
        </div>

        <div v-if="filteredRecords.length" class="overflow-x-auto">
          <table class="min-w-full divide-y divide-slate-100 text-sm">
            <thead class="bg-slate-50 text-xs font-bold uppercase tracking-wide text-slate-500">
              <tr>
                <th class="px-6 py-4 text-left">Mã BA</th>
                <th class="px-6 py-4 text-left">Chẩn đoán</th>
                <th class="px-6 py-4 text-left">Mã ICD</th>
                <th class="px-6 py-4 text-left">Ngày tạo</th>
                <th class="px-6 py-4 text-left">Tái khám</th>
                <th class="px-6 py-4 text-left">Trạng thái</th>
                <th class="px-6 py-4 text-right">Thao tác</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-100 bg-white">
              <tr v-for="record in paginatedRecords" :key="record.medicalRecordId || record.id" class="transition hover:bg-slate-50">
                <td class="px-6 py-4 whitespace-nowrap font-bold text-slate-900">
                  {{ record.medicalRecordCode || 'Chưa cập nhật' }}
                </td>
                <td class="px-6 py-4">
                  <p class="font-medium text-slate-800 line-clamp-2 max-w-sm">
                    {{ record.diagnosisText || record.diagnosis || 'Chưa cập nhật chẩn đoán' }}
                  </p>
                </td>
                <td class="px-6 py-4 whitespace-nowrap text-slate-600 font-mono text-xs">
                  {{ record.diagnosisCode || '-' }}
                </td>
                <td class="px-6 py-4 whitespace-nowrap text-slate-500">
                  {{ formatDate(record.createdAt) }}
                </td>
                <td class="px-6 py-4 whitespace-nowrap">
                  <span v-if="record.followUpDate" class="rounded-full bg-blue-50 px-2.5 py-1 text-xs font-bold text-blue-700 border border-blue-100 inline-flex items-center gap-1">
                    {{ formatDate(record.followUpDate) }}
                  </span>
                  <span v-else class="text-slate-400 text-xs font-medium">
                    Chưa có
                  </span>
                </td>
                <td class="px-6 py-4 whitespace-nowrap">
                  <span :class="['rounded-full px-2.5 py-1 text-xs font-bold inline-flex items-center gap-1', statusClass(record.status)]">
                    <span class="h-1.5 w-1.5 rounded-full bg-current"></span>
                    {{ statusLabel(record.status) }}
                  </span>
                </td>
                <td class="px-6 py-4 whitespace-nowrap text-right">
                  <div class="inline-flex gap-2">
                    <BaseButton
                      variant="ghost"
                      size="sm"
                      class="bg-blue-50 text-blue-600 hover:bg-blue-100 font-bold flex items-center gap-1.5 border border-transparent"
                      @click="openDetails(record)"
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
                      @click="printMedicalRecord(record)"
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
              Hiển thị {{ Math.min(filteredRecords.length, (currentPage - 1) * itemsPerPage + 1) }} - {{ Math.min(filteredRecords.length, currentPage * itemsPerPage) }} trên {{ filteredRecords.length }} kết quả
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
        <div v-else class="p-16 text-center">
          <span class="flex h-14 w-14 items-center justify-center rounded-full bg-slate-100 text-slate-400 mx-auto">
            <FileHeart class="h-7 w-7" />
          </span>
          <h3 class="mt-4 text-lg font-bold text-slate-900">Không tìm thấy hồ sơ bệnh án</h3>
          <p class="mx-auto mt-2 max-w-md text-sm text-slate-500 leading-relaxed">
            Hệ thống chưa có bệnh án khớp với bộ lọc của bạn hoặc chưa hoàn thành lượt khám nào. Vui lòng thử reset bộ lọc hoặc kiểm tra lại lịch hẹn.
          </p>
        </div>
      </div>
    </div>

    <!-- 7. Drawer Chi tiết bệnh án bên phải -->
    <div v-if="drawerOpen" class="fixed inset-0 z-50 bg-slate-950/40 backdrop-blur-sm transition-opacity" @click="closeDrawer"></div>

    <transition
      enter-active-class="transition duration-300 ease-out"
      enter-from-class="translate-x-full"
      enter-to-class="translate-x-0"
      leave-active-class="transition duration-200 ease-in"
      leave-from-class="translate-x-0"
      leave-to-class="translate-x-full"
    >
      <div v-if="drawerOpen" class="fixed right-0 top-0 z-50 h-screen w-full max-w-2xl bg-white shadow-2xl flex flex-col border-l border-slate-200">
        
        <!-- Drawer Header -->
        <div class="flex items-center justify-between border-b border-slate-100 p-5 bg-slate-50/50">
          <div>
            <div class="flex items-center gap-2">
              <h2 class="text-lg font-bold text-slate-900">Chi tiết bệnh án</h2>
              <span :class="['rounded-full px-2 py-0.5 text-[10px] font-bold', statusClass(selectedRecord?.status)]">
                {{ statusLabel(selectedRecord?.status) }}
              </span>
            </div>
            <p class="mt-1 text-xs font-semibold text-slate-500 font-mono">
              Mã: {{ selectedRecord?.medicalRecordCode || 'Chưa cập nhật' }}
            </p>
          </div>
          <div class="flex items-center gap-2">
            <BaseButton
              v-slot:icon
              v-if="selectedRecord"
              variant="outline"
              size="sm"
              class="border-slate-200 text-slate-700 bg-white hover:bg-slate-50 font-bold inline-flex items-center gap-1.5 h-9"
              @click="printMedicalRecord(selectedRecord)"
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
            v-for="tab in tabs"
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
            <!-- Nhóm 1: Thông tin hồ sơ -->
            <div class="space-y-2">
              <h3 class="text-xs font-bold text-slate-400 uppercase tracking-wider">Thông tin hồ sơ</h3>
              <div class="grid gap-3 sm:grid-cols-2">
                <div class="rounded-xl border border-slate-100 bg-slate-50 p-3">
                  <span class="text-xs font-semibold text-slate-400">Mã bệnh án</span>
                  <p class="mt-0.5 font-mono font-bold text-slate-800 text-sm">{{ selectedRecord?.medicalRecordCode || 'Chưa cập nhật' }}</p>
                </div>
                <div class="rounded-xl border border-slate-100 bg-slate-50 p-3">
                  <span class="text-xs font-semibold text-slate-400">Mã lượt khám</span>
                  <p class="mt-0.5 font-mono font-bold text-slate-800 text-sm">{{ selectedRecord?.visitId || 'Chưa cập nhật' }}</p>
                </div>
                <div class="rounded-xl border border-slate-100 bg-slate-50 p-3">
                  <span class="text-xs font-semibold text-slate-400">Mã bệnh nhân</span>
                  <p class="mt-0.5 font-mono font-bold text-slate-800 text-sm">{{ selectedRecord?.patientCode || selectedRecord?.patientIdCode || selectedRecord?.patientId || 'Chưa cập nhật' }}</p>
                </div>
                <div class="rounded-xl border border-slate-100 bg-slate-50 p-3">
                  <span class="text-xs font-semibold text-slate-400">Bác sĩ điều trị</span>
                  <p class="mt-0.5 font-bold text-slate-800 text-sm text-ellipsis overflow-hidden whitespace-nowrap" :title="associatedDoctorName || String(selectedRecord?.doctorId || '') || 'Chưa cập nhật'">
                    {{ associatedDoctorName || selectedRecord?.doctorId || 'Chưa cập nhật' }}
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
                  <span :class="['rounded-full px-2.5 py-0.5 text-xs font-bold inline-flex items-center gap-1', statusClass(selectedRecord?.status)]">
                    {{ statusLabel(selectedRecord?.status) }}
                  </span>
                </div>
                <div class="flex justify-between">
                  <span>Ngày lập hồ sơ:</span>
                  <span class="font-semibold text-slate-800">{{ formatDateTime(selectedRecord?.createdAt) }}</span>
                </div>
                <div class="flex justify-between" v-if="selectedRecord?.updatedAt && selectedRecord.updatedAt !== selectedRecord.createdAt">
                  <span>Cập nhật lần cuối:</span>
                  <span class="font-semibold text-slate-800">{{ formatDateTime(selectedRecord?.updatedAt) }}</span>
                </div>
                <div class="flex justify-between" v-if="selectedRecord?.completedAt">
                  <span>Hoàn tất lúc:</span>
                  <span class="font-semibold text-slate-800">{{ formatDateTime(selectedRecord?.completedAt) }}</span>
                </div>
              </div>
            </div>
          </div>

          <!-- Tab 2: Chẩn đoán -->
          <div v-if="currentTab === 'diagnosis'" class="space-y-4">
            <div class="rounded-xl border border-slate-200 bg-white p-5 shadow-sm space-y-4">
              <div>
                <span class="text-xs font-bold text-slate-400 uppercase tracking-wider">Mã ICD</span>
                <p class="mt-1 text-sm font-mono font-bold text-blue-700 bg-blue-50 px-2.5 py-1 w-fit rounded-lg">
                  {{ selectedRecord?.diagnosisCode || 'Chưa ghi nhận mã ICD' }}
                </p>
              </div>
              <div>
                <span class="text-xs font-bold text-slate-400 uppercase tracking-wider">Chẩn đoán</span>
                <p class="mt-1 text-base font-bold text-slate-800 leading-relaxed">
                  {{ selectedRecord?.diagnosisText || selectedRecord?.diagnosis || 'Chưa cập nhật chẩn đoán' }}
                </p>
              </div>
            </div>

            <div class="rounded-xl border border-slate-100 bg-slate-50 p-5 space-y-2">
              <span class="text-xs font-bold text-slate-400 uppercase tracking-wider block">Ghi chú và dặn dò của bác sĩ</span>
              <p class="text-sm text-slate-700 leading-relaxed whitespace-pre-line">
                {{ selectedRecord?.doctorNote || selectedRecord?.doctorNotes || 'Chưa có ghi chú' }}
              </p>
            </div>
          </div>

          <!-- Tab 3: Điều trị -->
          <div v-if="currentTab === 'treatment'" class="space-y-4">
            <div class="rounded-xl border border-slate-200 bg-white p-5 shadow-sm space-y-3">
              <span class="text-xs font-bold text-slate-400 uppercase tracking-wider block">Kế hoạch điều trị</span>
              <p class="text-sm text-slate-800 leading-relaxed whitespace-pre-line">
                {{ selectedRecord?.treatmentPlan || 'Chưa có kế hoạch điều trị' }}
              </p>
            </div>

            <div class="rounded-xl border border-slate-100 bg-slate-50 p-5 space-y-3">
              <span class="text-xs font-bold text-slate-400 uppercase tracking-wider block">Ngày tái khám dự kiến</span>
              <div class="flex items-center gap-3">
                <template v-if="selectedRecord?.followUpDate">
                  <p class="text-base font-bold text-slate-800">
                    {{ formatDate(selectedRecord.followUpDate) }}
                  </p>
                  <span v-if="followUpStatus === 'UPCOMING'" class="rounded-full bg-emerald-50 px-2.5 py-1 text-xs font-bold text-emerald-800">
                    Sắp tái khám
                  </span>
                  <span v-else-if="followUpStatus === 'OVERDUE'" class="rounded-full bg-rose-50 px-2.5 py-1 text-xs font-bold text-rose-800">
                    Đã qua lịch tái khám
                  </span>
                </template>
                <template v-else>
                  <span class="text-slate-400 text-xs font-medium">
                    Chưa có lịch tái khám
                  </span>
                </template>
              </div>
            </div>

            <div class="rounded-xl border border-blue-100 bg-blue-50/50 p-4 text-xs text-blue-700 flex items-start gap-2">
              <span class="font-bold shrink-0">Lưu ý:</span>
              <p>Vui lòng thực hiện theo hướng dẫn của bác sĩ và tái khám đúng lịch.</p>
            </div>
          </div>

          <!-- Tab 4: Lịch sử (Timeline) -->
          <div v-if="currentTab === 'history'" class="space-y-6">
            <div class="relative pl-8 border-l border-slate-200 space-y-8 ml-4 py-2">
              <!-- Event 1: Tạo hồ sơ -->
              <div class="relative" v-if="selectedRecord?.createdAt">
                <span class="absolute -left-[41px] top-0.5 flex h-6 w-6 items-center justify-center rounded-full bg-blue-100 text-blue-700 ring-4 ring-white">
                  <span class="h-2.5 w-2.5 rounded-full bg-blue-600"></span>
                </span>
                <div>
                  <h4 class="font-bold text-slate-900 text-sm">Tạo hồ sơ</h4>
                  <p class="text-xs text-slate-500 mt-1">{{ formatDateTime(selectedRecord.createdAt) }}</p>
                </div>
              </div>

              <!-- Event 2: Cập nhật hồ sơ -->
              <div class="relative" v-if="selectedRecord?.updatedAt && selectedRecord.updatedAt !== selectedRecord.createdAt">
                <span class="absolute -left-[41px] top-0.5 flex h-6 w-6 items-center justify-center rounded-full bg-amber-100 text-amber-700 ring-4 ring-white">
                  <span class="h-2.5 w-2.5 rounded-full bg-amber-600"></span>
                </span>
                <div>
                  <h4 class="font-bold text-slate-900 text-sm">Cập nhật hồ sơ</h4>
                  <p class="text-xs text-slate-500 mt-1">{{ formatDateTime(selectedRecord.updatedAt) }}</p>
                </div>
              </div>

              <!-- Event 3: Bệnh án đã hoàn tất -->
              <div class="relative" v-if="selectedRecord?.completedAt">
                <span class="absolute -left-[41px] top-0.5 flex h-6 w-6 items-center justify-center rounded-full bg-emerald-100 text-emerald-700 ring-4 ring-white">
                  <span class="h-2.5 w-2.5 rounded-full bg-emerald-600"></span>
                </span>
                <div>
                  <h4 class="font-bold text-slate-900 text-sm">Bệnh án đã hoàn tất</h4>
                  <p class="text-xs text-slate-500 mt-1">{{ formatDateTime(selectedRecord.completedAt) }}</p>
                </div>
              </div>

              <!-- Event 4: Hẹn tái khám -->
              <div class="relative" v-if="selectedRecord?.followUpDate">
                <span class="absolute -left-[41px] top-0.5 flex h-6 w-6 items-center justify-center rounded-full bg-indigo-100 text-indigo-700 ring-4 ring-white">
                  <span class="h-2.5 w-2.5 rounded-full bg-indigo-600"></span>
                </span>
                <div>
                  <h4 class="font-bold text-slate-900 text-sm">Hẹn tái khám</h4>
                  <p class="text-xs text-slate-500 mt-1">{{ formatDate(selectedRecord.followUpDate) }}</p>
                </div>
              </div>
            </div>
          </div>

          <!-- Tab 5: Đơn thuốc -->
          <div v-if="currentTab === 'prescription'" class="space-y-4">
            <div v-if="prescriptionLoading" class="flex justify-center p-8">
              <span class="h-8 w-8 animate-spin rounded-full border-4 border-blue-500 border-t-transparent"></span>
            </div>
            <template v-else>
              <div v-if="activePrescription" class="space-y-4">
                <div class="rounded-xl border border-slate-100 bg-slate-50 p-4 flex items-center justify-between">
                  <div>
                    <span class="text-xs font-semibold text-slate-400">Mã đơn thuốc</span>
                    <p class="mt-0.5 font-mono font-bold text-slate-800">{{ activePrescription.prescriptionCode || 'Chưa cập nhật' }}</p>
                  </div>
                  <div class="flex items-center gap-2">
                    <BaseButton
                      variant="outline"
                      size="sm"
                      class="border-slate-200 text-slate-700 bg-white hover:bg-slate-50 font-bold inline-flex items-center gap-1.5 h-9"
                      @click="printPrescription(activePrescription)"
                    >
                      <template #icon>
                        <Printer class="h-4 w-4 text-slate-500" />
                      </template>
                      In đơn thuốc
                    </BaseButton>
                    <span class="rounded-full bg-blue-50 px-2.5 py-1 text-xs font-bold text-blue-700">
                      {{ activePrescription.status || 'Chờ phát thuốc' }}
                    </span>
                  </div>
                </div>

                <div class="space-y-3">
                  <h4 class="font-bold text-slate-900 text-sm">Danh mục thuốc được kê</h4>
                  <div
                    v-for="item in activePrescription.items || activePrescription.prescriptionItems || []"
                    :key="item.id"
                    class="rounded-xl border border-slate-200 bg-white p-4 shadow-sm"
                  >
                    <div class="flex items-center justify-between gap-3">
                      <p class="font-bold text-slate-900">{{ item.medicineNameSnapshot || item.medicineName }}</p>
                      <span class="text-sm font-semibold text-slate-500">x{{ item.quantity }} {{ item.unitSnapshot || 'Đơn vị' }}</span>
                    </div>
                    <div class="mt-2 grid grid-cols-2 gap-2 text-xs text-slate-500 pt-2 border-t border-slate-50">
                      <p>Liều lượng: <span class="font-medium text-slate-700">{{ item.dosage }}</span></p>
                      <p>Tần suất: <span class="font-medium text-slate-700">{{ item.frequency }}</span></p>
                      <p class="col-span-2">Dùng trong: <span class="font-medium text-slate-700">{{ item.durationDays }} ngày</span></p>
                      <p v-if="item.usageInstruction" class="col-span-2 text-blue-600 font-medium">Hướng dẫn: {{ item.usageInstruction }}</p>
                    </div>
                  </div>
                </div>

                <div v-if="activePrescription.note" class="rounded-xl border border-slate-100 bg-slate-50 p-4 space-y-1">
                  <span class="text-xs font-bold text-slate-400 uppercase tracking-wider block">Ghi chú đơn thuốc</span>
                  <p class="text-sm text-slate-700 leading-relaxed whitespace-pre-line">{{ activePrescription.note }}</p>
                </div>
              </div>

              <div v-else class="rounded-xl border border-dashed border-slate-200 p-8 text-center text-slate-500">
                <Pill class="mx-auto h-8 w-8 text-slate-300" />
                <h5 class="mt-4 font-bold text-slate-900 text-sm">Chưa có dữ liệu đơn thuốc</h5>
                <p class="mt-1 text-xs text-slate-500">Đơn thuốc sau khi được bác sĩ kê sẽ hiển thị tại đây.</p>
              </div>
            </template>
          </div>

          <!-- Tab 6: Viện phí -->
          <div v-if="currentTab === 'billing'" class="space-y-4">
            <div v-if="billingLoading" class="flex justify-center p-8">
              <span class="h-8 w-8 animate-spin rounded-full border-4 border-blue-500 border-t-transparent"></span>
            </div>
            <template v-else>
              <div v-if="activeInvoice" class="rounded-xl border border-slate-200 bg-white p-5 shadow-sm space-y-4">
                <div class="flex items-center justify-between border-b border-slate-100 pb-3">
                  <div>
                    <span class="text-xs font-semibold text-slate-400">Mã hóa đơn</span>
                    <p class="font-mono font-bold text-slate-800 text-sm">#{{ activeInvoice.invoiceId }}</p>
                  </div>
                  <span :class="['rounded-full px-2.5 py-1 text-xs font-bold', invoiceStatusClass(activeInvoice.status)]">
                    {{ invoiceStatusLabel(activeInvoice.status) }}
                  </span>
                </div>

                <div class="space-y-2 text-sm text-slate-600">
                  <div class="flex justify-between">
                    <span>Phí khám bệnh:</span>
                    <span class="font-semibold text-slate-800">{{ formatCurrency(activeInvoice.examinationFee) }}</span>
                  </div>
                  <div class="flex justify-between">
                    <span>Tiền thuốc:</span>
                    <span class="font-semibold text-slate-800">{{ formatCurrency(activeInvoice.medicineTotal) }}</span>
                  </div>
                  <div class="flex justify-between border-t border-slate-50 pt-2 text-base font-bold">
                    <span class="text-slate-900">Tổng cộng:</span>
                    <span class="text-blue-700">{{ formatCurrency(activeInvoice.totalAmount) }}</span>
                  </div>
                </div>

                <hr class="border-slate-100" />

                <div class="space-y-2 text-xs text-slate-500">
                  <div class="flex justify-between">
                    <span>Phương thức thanh toán:</span>
                    <span class="font-bold text-slate-700">{{ activeInvoice.payments?.[0]?.paymentMethod || 'Tiền mặt/Chuyển khoản' }}</span>
                  </div>
                  <div class="flex justify-between">
                    <span>Ngày thanh toán:</span>
                    <span class="font-bold text-slate-700">{{ formatDateTime(activeInvoice.paidAt || activeInvoice.createdAt) }}</span>
                  </div>
                </div>
              </div>

              <div v-else class="rounded-xl border border-dashed border-slate-200 p-8 text-center text-slate-500">
                <CreditCard class="mx-auto h-8 w-8 text-slate-300" />
                <h5 class="mt-4 font-bold text-slate-900 text-sm">Chưa có dữ liệu viện phí</h5>
                <p class="mt-1 text-xs text-slate-500">Thông tin hóa đơn và thanh toán sẽ được cập nhật sau khi phát sinh.</p>
              </div>
            </template>
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

    <!-- Print Area for Medical Record -->
    <div v-if="recordToPrint" class="print-area">
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
          <h1 class="text-xl font-bold text-slate-900 tracking-wide uppercase">Hồ sơ bệnh án</h1>
          <p class="text-xs text-slate-500 mt-1 font-mono">Mã số bệnh án: {{ recordToPrint.medicalRecordCode || 'Chưa cập nhật' }}</p>
        </div>

        <!-- Patient Info -->
        <div class="mb-5">
          <h2 class="text-xs font-bold uppercase tracking-wider text-slate-400 border-b border-slate-200 pb-1 mb-2">Thông tin bệnh nhân</h2>
          <div class="grid grid-cols-2 gap-y-2 text-xs">
            <div><span class="font-bold text-slate-500">Mã bệnh nhân:</span> <span class="font-semibold text-slate-800">{{ patientDetail?.patientCode || patientDetail?.patientIdCode || recordToPrint.patientCode || recordToPrint.patientIdCode || patientDetail?.id || recordToPrint.patientId || 'Chưa có thông tin' }}</span></div>
            <div><span class="font-bold text-slate-500">Họ và tên:</span> <span class="font-semibold text-slate-800">{{ patientDetail?.fullName || authStore.user?.fullName || 'Chưa có thông tin' }}</span></div>
            <div><span class="font-bold text-slate-500">Ngày sinh:</span> <span class="font-semibold text-slate-800">{{ formatDate(patientDetail?.dateOfBirth) }}</span></div>
            <div><span class="font-bold text-slate-500">Giới tính:</span> <span class="font-semibold text-slate-800">{{ genderLabel(patientDetail?.gender) }}</span></div>
            <div class="col-span-2"><span class="font-bold text-slate-500">Số điện thoại:</span> <span class="font-semibold text-slate-800">{{ patientDetail?.phoneNumber || patientDetail?.phone || 'Chưa có thông tin' }}</span></div>
            <div><span class="font-bold text-slate-500">Email:</span> <span class="font-semibold text-slate-800">{{ patientDetail?.email || authStore.user?.email || 'Chưa có thông tin' }}</span></div>
            <div><span class="font-bold text-slate-500">CCCD:</span> <span class="font-semibold text-slate-800">{{ patientDetail?.citizenId || 'Chưa có thông tin' }}</span></div>
            <div><span class="font-bold text-slate-500">Nhóm máu:</span> <span class="font-semibold text-slate-800">{{ patientDetail?.bloodType || 'Chưa có thông tin' }}</span></div>
            <div><span class="font-bold text-slate-500">Dị ứng:</span> <span class="font-semibold text-slate-800">{{ patientDetail?.allergyNote || patientDetail?.allergies || 'Chưa ghi nhận' }}</span></div>
            <div class="col-span-2"><span class="font-bold text-slate-500">Địa chỉ:</span> <span class="font-semibold text-slate-800">{{ patientDetail?.address || 'Chưa có thông tin' }}</span></div>
            <div class="col-span-2"><span class="font-bold text-slate-500">Tiền sử bệnh:</span> <span class="font-semibold text-slate-800">{{ patientDetail?.medicalHistory || 'Chưa ghi nhận' }}</span></div>
          </div>
        </div>

        <!-- Appointment Info -->
        <div class="mb-5">
          <h2 class="text-xs font-bold uppercase tracking-wider text-slate-400 border-b border-slate-200 pb-1 mb-2">Thông tin lượt khám</h2>
          <div class="grid grid-cols-2 gap-y-2 text-xs">
            <div><span class="font-bold text-slate-500">Mã lịch hẹn:</span> <span class="font-semibold text-slate-800 font-mono">{{ appointmentForRecord(recordToPrint)?.appointmentId || recordToPrint.appointmentId || 'Chưa có thông tin' }}</span></div>
            <div><span class="font-bold text-slate-500">Ngày giờ khám:</span> <span class="font-semibold text-slate-800">{{ appointmentTimeLabel(recordToPrint) }}</span></div>
            <div><span class="font-bold text-slate-500">Chuyên khoa:</span> <span class="font-semibold text-slate-800">{{ appointmentForRecord(recordToPrint)?.specialtyName || 'Chưa có thông tin' }}</span></div>
            <div><span class="font-bold text-slate-500">Số thứ tự:</span> <span class="font-semibold text-slate-800">{{ appointmentForRecord(recordToPrint)?.queueNumber || 'Chưa có thông tin' }}</span></div>
            <div class="col-span-2"><span class="font-bold text-slate-500">Lý do khám:</span> <span class="font-semibold text-slate-800">{{ appointmentForRecord(recordToPrint)?.reason || 'Chưa ghi nhận' }}</span></div>
          </div>
        </div>

        <!-- Record Details -->
        <div class="mb-5">
          <h2 class="text-xs font-bold uppercase tracking-wider text-slate-400 border-b border-slate-200 pb-1 mb-2">Thông tin bệnh án</h2>
          <div class="grid grid-cols-2 gap-y-2 text-xs">
            <div><span class="font-bold text-slate-500">Mã lượt khám:</span> <span class="font-semibold text-slate-800 font-mono">{{ recordToPrint.visitId || 'Chưa có thông tin' }}</span></div>
            <div><span class="font-bold text-slate-500">Bác sĩ điều trị:</span> <span class="font-semibold text-slate-800">{{ doctorNameForRecord(recordToPrint) || 'Chưa có thông tin' }}</span></div>
            <div><span class="font-bold text-slate-500">Ngày lập bệnh án:</span> <span class="font-semibold text-slate-800">{{ formatDateTime(recordToPrint.createdAt) }}</span></div>
            <div><span class="font-bold text-slate-500">Cập nhật lần cuối:</span> <span class="font-semibold text-slate-800">{{ formatDateTime(recordToPrint.updatedAt) }}</span></div>
            <div v-if="recordToPrint.completedAt"><span class="font-bold text-slate-500">Hoàn tất lúc:</span> <span class="font-semibold text-slate-800">{{ formatDateTime(recordToPrint.completedAt) }}</span></div>
            <div><span class="font-bold text-slate-500">Trạng thái:</span> <span class="font-semibold text-slate-800">{{ statusLabel(recordToPrint.status) }}</span></div>
          </div>
        </div>

        <!-- Diagnosis Info -->
        <div class="mb-5">
          <h2 class="text-xs font-bold uppercase tracking-wider text-slate-400 border-b border-slate-200 pb-1 mb-2">Chẩn đoán</h2>
          <div class="space-y-2 text-xs">
            <div><span class="font-bold text-slate-500">Mã ICD:</span> <span class="font-mono bg-slate-50 px-1.5 py-0.5 rounded font-bold">{{ recordToPrint.diagnosisCode || 'Chưa có thông tin' }}</span></div>
            <div><span class="font-bold text-slate-500">Chẩn đoán bệnh:</span> <span class="font-semibold text-slate-800 block mt-0.5 pl-3 border-l-2 border-slate-200">{{ recordToPrint.diagnosisText || recordToPrint.diagnosis || 'Chưa có thông tin' }}</span></div>
            <div><span class="font-bold text-slate-500">Ghi chú & Dặn dò:</span> <span class="font-semibold text-slate-700 block mt-0.5 pl-3 border-l-2 border-slate-200 whitespace-pre-line">{{ recordToPrint.doctorNote || recordToPrint.doctorNotes || 'Chưa có ghi chú' }}</span></div>
          </div>
        </div>

        <!-- Treatment Plan -->
        <div class="mb-6">
          <h2 class="text-xs font-bold uppercase tracking-wider text-slate-400 border-b border-slate-200 pb-1 mb-2">Kế hoạch điều trị & Tái khám</h2>
          <div class="space-y-2 text-xs">
            <div><span class="font-bold text-slate-500">Phương án điều trị:</span> <span class="font-semibold text-slate-800 block mt-0.5 pl-3 border-l-2 border-slate-200 whitespace-pre-line">{{ recordToPrint.treatmentPlan || 'Chưa có kế hoạch điều trị' }}</span></div>
            <div><span class="font-bold text-slate-500">Lịch hẹn tái khám:</span> <span class="font-bold text-slate-800">{{ formatDate(recordToPrint.followUpDate) }}</span></div>
          </div>
        </div>

        <!-- Prescription Info -->
        <div class="mb-6">
          <h2 class="text-xs font-bold uppercase tracking-wider text-slate-400 border-b border-slate-200 pb-1 mb-2">Đơn thuốc</h2>
          <template v-if="activePrescription">
            <div class="grid grid-cols-2 gap-y-2 text-xs mb-3">
              <div><span class="font-bold text-slate-500">Mã đơn thuốc:</span> <span class="font-semibold text-slate-800 font-mono">{{ activePrescription.prescriptionCode || activePrescription.prescriptionIdCode || activePrescription.prescriptionId || activePrescription.id || 'Chưa cập nhật' }}</span></div>
              <div><span class="font-bold text-slate-500">Ngày kê đơn:</span> <span class="font-semibold text-slate-800">{{ formatDateTime(activePrescription.createdAt || activePrescription.submittedAt) }}</span></div>
              <div><span class="font-bold text-slate-500">Trạng thái:</span> <span class="font-semibold text-slate-800">{{ statusLabel(activePrescription.status) }}</span></div>
              <div><span class="font-bold text-slate-500">Bác sĩ kê đơn:</span> <span class="font-semibold text-slate-800">{{ associatedDoctorNameForPrescription(activePrescription) || doctorNameForRecord(recordToPrint) || 'Chưa có thông tin' }}</span></div>
            </div>
            <table v-if="prescriptionItems(activePrescription).length" class="min-w-full border border-slate-200 text-xs mb-3">
              <thead class="bg-slate-50 font-bold text-slate-600 text-left border-b border-slate-200">
                <tr>
                  <th class="px-2 py-1.5 border-r border-slate-200 w-10 text-center">STT</th>
                  <th class="px-2 py-1.5 border-r border-slate-200">Tên thuốc</th>
                  <th class="px-2 py-1.5 border-r border-slate-200 w-20 text-center">Số lượng</th>
                  <th class="px-2 py-1.5 border-r border-slate-200">Liều dùng</th>
                  <th class="px-2 py-1.5">Hướng dẫn</th>
                </tr>
              </thead>
              <tbody class="divide-y divide-slate-200 bg-white">
                <tr v-for="(item, index) in prescriptionItems(activePrescription)" :key="item.id || index">
                  <td class="px-2 py-1.5 border-r border-slate-200 text-center font-medium">{{ index + 1 }}</td>
                  <td class="px-2 py-1.5 border-r border-slate-200 font-bold text-slate-800">{{ item.medicineNameSnapshot || item.medicineName || 'Chưa cập nhật' }}</td>
                  <td class="px-2 py-1.5 border-r border-slate-200 text-center font-bold">{{ item.quantity || '-' }} {{ item.unitSnapshot || '' }}</td>
                  <td class="px-2 py-1.5 border-r border-slate-200 font-medium">{{ item.dosage || 'Chưa cập nhật' }} · {{ item.frequency || 'Chưa cập nhật' }}</td>
                  <td class="px-2 py-1.5 font-medium">{{ item.usageInstruction || 'Theo dặn dò của bác sĩ' }}</td>
                </tr>
              </tbody>
            </table>
            <div v-if="activePrescription.note" class="bg-slate-50 p-3 rounded-lg border border-slate-200 text-xs text-slate-700 whitespace-pre-line leading-relaxed">
              <span class="font-bold">Ghi chú đơn thuốc:</span> {{ activePrescription.note }}
            </div>
          </template>
          <p v-else class="text-xs italic text-slate-500">Chưa có đơn thuốc được ghi nhận cho bệnh án này.</p>
        </div>

        <!-- Billing Info -->
        <div class="mb-6">
          <h2 class="text-xs font-bold uppercase tracking-wider text-slate-400 border-b border-slate-200 pb-1 mb-2">Viện phí</h2>
          <template v-if="activeInvoice">
            <div class="grid grid-cols-2 gap-y-2 text-xs">
              <div><span class="font-bold text-slate-500">Mã hóa đơn:</span> <span class="font-semibold text-slate-800 font-mono">{{ activeInvoice.invoiceCode || activeInvoice.invoiceIdCode || activeInvoice.invoiceId }}</span></div>
              <div><span class="font-bold text-slate-500">Trạng thái:</span> <span class="font-semibold text-slate-800">{{ invoiceStatusLabel(activeInvoice.status) }}</span></div>
              <div><span class="font-bold text-slate-500">Phí khám:</span> <span class="font-semibold text-slate-800">{{ formatCurrency(activeInvoice.examinationFee || activeInvoice.examFee) }}</span></div>
              <div><span class="font-bold text-slate-500">Tiền thuốc:</span> <span class="font-semibold text-slate-800">{{ formatCurrency(activeInvoice.medicineTotal) }}</span></div>
              <div><span class="font-bold text-slate-500">Tổng cộng:</span> <span class="font-bold text-slate-900">{{ formatCurrency(activeInvoice.totalAmount || activeInvoice.amount) }}</span></div>
              <div><span class="font-bold text-slate-500">Ngày thanh toán:</span> <span class="font-semibold text-slate-800">{{ formatDateTime(activeInvoice.paidAt || activeInvoice.createdAt) }}</span></div>
            </div>
          </template>
          <p v-else class="text-xs italic text-slate-500">Chưa có thông tin viện phí liên quan.</p>
        </div>

        <!-- Signature Block -->
        <div class="mt-8 pt-6 border-t border-slate-200 grid grid-cols-2 text-center text-xs gap-4">
          <div>
            <p class="font-bold text-slate-500 uppercase tracking-wide">Bệnh nhân</p>
            <p class="text-[10px] text-slate-400 mt-0.5">(Ký và ghi rõ họ tên)</p>
            <div class="h-16"></div>
            <p class="font-bold text-slate-800">{{ patientDetail?.fullName || authStore.user?.fullName || '' }}</p>
          </div>
          <div>
            <p class="font-bold text-slate-500 uppercase tracking-wide">Bác sĩ điều trị</p>
            <p class="text-[10px] text-slate-400 mt-0.5">(Ký và ghi rõ họ tên)</p>
            <div class="h-16"></div>
            <p class="font-bold text-slate-800">{{ doctorNameForRecord(recordToPrint) || '' }}</p>
          </div>
        </div>
      </div>
    </div>

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
          <h1 class="text-xl font-bold text-slate-900 tracking-wide uppercase">Đơn thuốc của bệnh nhân</h1>
          <p class="text-xs text-slate-500 mt-1 font-mono">Mã đơn thuốc: {{ prescriptionToPrint.prescriptionCode || 'DT' + String(prescriptionToPrint.id).padStart(3, '0') }}</p>
        </div>

        <!-- Patient Info -->
        <div class="mb-5">
          <h2 class="text-xs font-bold uppercase tracking-wider text-slate-400 border-b border-slate-200 pb-1 mb-2">Thông tin bệnh nhân</h2>
          <div class="grid grid-cols-2 gap-y-2 text-xs">
            <div><span class="font-bold text-slate-500">Mã bệnh nhân:</span> <span class="font-semibold text-slate-800">{{ patientDetail?.patientCode || patientDetail?.patientIdCode || prescriptionToPrint.patientCode || prescriptionToPrint.patientIdCode || patientDetail?.id || prescriptionToPrint.patientId || 'Chưa có thông tin' }}</span></div>
            <div><span class="font-bold text-slate-500">Họ và tên:</span> <span class="font-semibold text-slate-800">{{ patientDetail?.fullName || authStore.user?.fullName || 'Chưa có thông tin' }}</span></div>
            <div><span class="font-bold text-slate-500">Ngày sinh:</span> <span class="font-semibold text-slate-800">{{ formatDate(patientDetail?.dateOfBirth) }}</span></div>
            <div><span class="font-bold text-slate-500">Giới tính:</span> <span class="font-semibold text-slate-800">{{ genderLabel(patientDetail?.gender) }}</span></div>
            <div class="col-span-2"><span class="font-bold text-slate-500">Số điện thoại:</span> <span class="font-semibold text-slate-800">{{ patientDetail?.phoneNumber || patientDetail?.phone || 'Chưa có thông tin' }}</span></div>
          </div>
        </div>

        <!-- Prescription Info -->
        <div class="mb-5">
          <h2 class="text-xs font-bold uppercase tracking-wider text-slate-400 border-b border-slate-200 pb-1 mb-2">Thông tin đơn thuốc</h2>
          <div class="grid grid-cols-2 gap-y-2 text-xs">
            <div><span class="font-bold text-slate-500">Mã bệnh án:</span> <span class="font-semibold text-slate-800 font-mono">{{ prescriptionToPrint.medicalRecordCode || 'Chưa có thông tin' }}</span></div>
            <div><span class="font-bold text-slate-500">Bác sĩ kê đơn:</span> <span class="font-semibold text-slate-800">{{ associatedDoctorNameForPrescription(prescriptionToPrint) || 'Chưa có thông tin' }}</span></div>
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
            <p class="font-bold text-slate-800">{{ patientDetail?.fullName || authStore.user?.fullName || '' }}</p>
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
            <p class="font-bold text-slate-800">{{ associatedDoctorNameForPrescription(prescriptionToPrint) }}</p>
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
  ClipboardList,
  CreditCard,
  Eye,
  FileHeart,
  FilePenLine,
  Pill,
  Printer,
  RefreshCw,
  Search,
  ShieldAlert,
  User,
  X,
} from 'lucide-vue-next'
import BaseButton from '@/components/ui/BaseButton.vue'
import LoadingSkeleton from '@/components/ui/LoadingSkeleton.vue'
import Toast from '@/components/ui/Toast.vue'
import { useAuthStore } from '@/stores/authStore'
import { medicalRecordApi } from '@/services/medicalRecordApi'
import { appointmentApi } from '@/services/appointmentApi'
import { billingApi } from '@/services/billingApi'
import { getApiErrorMessage } from '@/services/apiClient'
import type { MedicalRecord, Patient } from '@/types/medicalRecord'
import type { Appointment } from '@/types/appointment'
import type { Invoice, Prescription } from '@/types/billing'
import type { Doctor } from '@/types/doctor'
import logoUrl from '@/assets/logo.png'

const authStore = useAuthStore()
const loading = ref(true)
const prescriptionLoading = ref(false)
const billingLoading = ref(false)
const error = ref('')

// Raw Data
const patientDetail = ref<Patient | null>(null)
const medicalRecords = ref<MedicalRecord[]>([])
const appointments = ref<Appointment[]>([])
const doctorsList = ref<Doctor[]>([])
const resolvedN2Id = ref<number | null>(null)
const resolvedPatientKeys = ref<number[]>([])

// Selected State (Drawer)
const drawerOpen = ref(false)
const selectedRecord = ref<MedicalRecord | null>(null)
const currentTab = ref('overview')
const activePrescription = ref<Prescription | null>(null)
const activeInvoice = ref<Invoice | null>(null)

// Print State
const recordToPrint = ref<MedicalRecord | null>(null)
const prescriptionToPrint = ref<Prescription | null>(null)

const tabs = [
  { key: 'overview', label: 'Tổng quan' },
  { key: 'diagnosis', label: 'Chẩn đoán' },
  { key: 'treatment', label: 'Điều trị' },
  { key: 'history', label: 'Lịch sử' },
  { key: 'prescription', label: 'Đơn thuốc' },
  { key: 'billing', label: 'Viện phí' },
]

// Filters
const filters = reactive({
  search: '',
  status: 'ALL',
  followUp: 'ALL',
  startDate: '',
  endDate: '',
})

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

const stats = computed(() => {
  const total = medicalRecords.value.length
  const completed = medicalRecords.value.filter(r => ['đã hoàn tất', 'completed', 'done'].includes(String(r.status || '').toLowerCase())).length
  const draft = medicalRecords.value.filter(r => ['bản nháp', 'draft'].includes(String(r.status || '').toLowerCase())).length
  const followUp = medicalRecords.value.filter(r => r.followUpDate !== null && r.followUpDate !== undefined && r.followUpDate !== '').length
  return { total, completed, draft, followUp }
})

const filteredRecords = computed(() => {
  return medicalRecords.value.filter(record => {
    // 1. Search Query filter
    if (filters.search.trim()) {
      const q = filters.search.trim().toLowerCase()
      const matchesSearch = 
        String(record.medicalRecordCode || '').toLowerCase().includes(q) ||
        String(record.diagnosisCode || '').toLowerCase().includes(q) ||
        String(record.diagnosisText || record.diagnosis || '').toLowerCase().includes(q) ||
        String(record.doctorNote || record.doctorNotes || '').toLowerCase().includes(q) ||
        String(record.treatmentPlan || '').toLowerCase().includes(q)
      if (!matchesSearch) return false
    }

    // 2. Status filter
    if (filters.status !== 'ALL') {
      const s = String(record.status || '').toUpperCase()
      if (filters.status === 'COMPLETED' && !['ĐÃ HOÀN TẤT', 'COMPLETED', 'DONE'].includes(s)) return false
      if (filters.status === 'DRAFT' && !['BẢN NHÁP', 'DRAFT'].includes(s)) return false
      if (filters.status === 'IN_PROGRESS' && !['ĐANG XỬ LÝ', 'IN_PROGRESS'].includes(s)) return false
    }

    // 3. Follow up filter
    if (filters.followUp !== 'ALL') {
      const hasFollowUp = record.followUpDate !== null && record.followUpDate !== undefined && record.followUpDate !== ''
      if (filters.followUp === 'HAS_FOLLOWUP' && !hasFollowUp) return false
      if (filters.followUp === 'NO_FOLLOWUP' && hasFollowUp) return false
    }

    // 4. Date range filter
    const createdDate = record.createdAt ? new Date(record.createdAt) : null
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

const totalPages = computed(() => Math.ceil(filteredRecords.value.length / itemsPerPage.value))

const paginatedRecords = computed(() => {
  const start = (currentPage.value - 1) * itemsPerPage.value
  const end = start + itemsPerPage.value
  return filteredRecords.value.slice(start, end)
})

const followUpStatus = computed(() => {
  if (!selectedRecord.value?.followUpDate) return 'NONE'
  const followDate = new Date(selectedRecord.value.followUpDate)
  const todayDate = new Date()
  todayDate.setHours(0, 0, 0, 0)
  return followDate >= todayDate ? 'UPCOMING' : 'OVERDUE'
})

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

function getDoctorName(doctorId?: number | string) {
  if (!doctorId) return ''
  const docId = Number(doctorId)
  const appt = appointments.value.find(a => Number(a.doctorId) === docId)
  if (appt?.doctorName) return appt.doctorName
  
  const doc = doctorsList.value.find(d => Number(d.doctorId) === docId)
  if (doc?.doctorName || doc?.fullName) return doc.doctorName || doc.fullName
  
  return doctorNamesMap[docId] || `Bác sĩ #${docId}`
}

const associatedDoctorName = computed(() => {
  if (!selectedRecord.value) return ''
  return getDoctorName(selectedRecord.value.doctorId)
})

onMounted(loadData)

function uniqueAppointments(list: Appointment[]) {
  const seen = new Set<string>()
  return list.filter((appointment) => {
    const key = String(appointment.appointmentId || `${appointment.patientId}-${appointment.appointmentDate}-${appointment.slotTime}`)
    if (seen.has(key)) return false
    seen.add(key)
    return true
  })
}

function recordIdentity(record: MedicalRecord) {
  return String(
    record.medicalRecordId ||
    record.recordId ||
    record.medicalRecordCode ||
    record.id ||
    (record.visitId ? `visit-${record.visitId}` : '') ||
    `${record.patientId}-${record.createdAt}-${record.diagnosisCode}-${record.diagnosisText}`,
  )
}

async function loadData() {
  loading.value = true
  error.value = ''
  medicalRecords.value = []
  appointments.value = []
  resolvedN2Id.value = null
  resolvedPatientKeys.value = []

  try {
    const [patient, timeline, doctors] = await Promise.all([
      medicalRecordApi.getCurrentPatient(),
      medicalRecordApi.getCurrentPatientClinicalTimeline().catch((err) => {
        if ((err as any)?.response?.status === 404) {
          return { visits: [], medicalRecords: [], prescriptions: [] }
        }
        throw err
      }),
      appointmentApi.getDoctors().catch(() => []),
    ])

    patientDetail.value = patient
    doctorsList.value = doctors

    const patientNumericId = Number(patient.id || patient.patientId)
    if (Number.isFinite(patientNumericId) && patientNumericId > 0) {
      resolvedN2Id.value = patientNumericId
      resolvedPatientKeys.value = [patientNumericId]
      if (authStore.user) authStore.user.patientId = String(patientNumericId)
      appointments.value = uniqueAppointments(
        await appointmentApi.getAppointmentsByPatient(patientNumericId).catch(() => [] as Appointment[]),
      )
    }

    const seen = new Set<string>()
    medicalRecords.value = timeline.medicalRecords.filter((record) => {
      const rid = recordIdentity(record)
      if (seen.has(rid)) return false
      seen.add(rid)
      return true
    })
  } catch (err) {
    const status = (err as any)?.response?.status
    if (status === 403) {
      error.value = 'Bạn không có quyền xem hồ sơ bệnh án này. Vui lòng đăng xuất rồi đăng nhập lại nếu vừa đổi tài khoản.'
    } else if (status === 404) {
      error.value = ''
      medicalRecords.value = []
    } else {
      error.value = getApiErrorMessage(err)
    }
  } finally {
    loading.value = false
  }
}

function openDetails(record: MedicalRecord) {
  selectedRecord.value = record
  currentTab.value = 'overview'
  activePrescription.value = null
  activeInvoice.value = null
  drawerOpen.value = true

  const recordId = record.medicalRecordId || record.recordId || record.id
  if (recordId) {
    medicalRecordApi.getCompleteMedicalRecord(recordId)
      .then((completeRecord) => {
        selectedRecord.value = { ...record, ...(completeRecord as Record<string, any>) }
      })
      .catch((err) => {
        if ((err as any)?.response?.status === 403) {
          toast.title = 'Không có quyền truy cập'
          toast.message = 'Bạn không có quyền xem bệnh án này.'
          toast.type = 'error'
          toast.show = true
        }
      })
  }

  // Proactively pre-load Prescription and Billing data for this record
  void loadPrescriptionData(record)
  void loadBillingData(record)
}

function closeDrawer() {
  drawerOpen.value = false
  selectedRecord.value = null
}

async function loadPrescriptionData(record: MedicalRecord) {
  prescriptionLoading.value = true
  activePrescription.value = null
  try {
    const patientId = Number(patientDetail.value?.id || patientDetail.value?.patientId || authStore.user?.patientId)
    const [timeline, n3List] = await Promise.all([
      medicalRecordApi.getCurrentPatientClinicalTimeline().catch((err) => {
        if ((err as any)?.response?.status === 404) return { visits: [], medicalRecords: [], prescriptions: [] }
        throw err
      }),
      Number.isFinite(patientId) && patientId > 0
        ? billingApi.getPrescriptions(patientId).catch((err) => {
          if ((err as any)?.response?.status === 404) return [] as Prescription[]
          throw err
        })
        : Promise.resolve([] as Prescription[]),
    ])

    const n2List = timeline.prescriptions || []
    const combined = [...n2List, ...n3List]
    
    // Find matching prescription
    const recordCode = record.medicalRecordCode || record.medicalRecordIdCode || record.recordIdCode || record.recordId
    const recordId = record.medicalRecordId || record.id
    const appointmentId = Number(record.appointmentId || appointmentForRecord(record)?.appointmentId || 0)

    const match = combined.find(p =>
      (recordCode && [p.medicalRecordCode, p.medicalRecordIdCode].some(code => String(code || '') === String(recordCode))) ||
      (recordId && Number(p.medicalRecordId) === Number(recordId)) ||
      (appointmentId && Number(p.appointmentId) === appointmentId)
    )
    if (match) {
      activePrescription.value = match
    }
  } catch (err) {
    console.error('Failed to load prescription info', err)
  } finally {
    prescriptionLoading.value = false
  }
}

async function loadBillingData(record: MedicalRecord) {
  billingLoading.value = true
  activeInvoice.value = null
  try {
    const patientId = Number(patientDetail.value?.id || patientDetail.value?.patientId || authStore.user?.patientId)
    const list = Number.isFinite(patientId) && patientId > 0
      ? await billingApi.getInvoices(patientId).catch((err) => {
        if ((err as any)?.response?.status === 404) return [] as Invoice[]
        throw err
      })
      : []
    
    // Match by appointmentId
    const appointmentId = Number(record.appointmentId || appointmentForRecord(record)?.appointmentId || 0)
    const match = list.find(inv => appointmentId && Number(inv.appointmentId) === appointmentId)
    if (match) {
      activeInvoice.value = match
    }
  } catch (err) {
    console.error('Failed to load invoice info', err)
  } finally {
    billingLoading.value = false
  }
}

function resetFilters() {
  filters.search = ''
  filters.status = 'ALL'
  filters.followUp = 'ALL'
  filters.startDate = ''
  filters.endDate = ''
}

function currentPrintDateTime() {
  return formatDateTime(new Date().toISOString())
}

function associatedDoctorNameForPrescription(prescription: Prescription) {
  return getDoctorName(prescription.doctorId)
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
  return getDoctorName(record.doctorId) || appointmentForRecord(record)?.doctorName || ''
}

function appointmentTimeLabel(record?: MedicalRecord | null) {
  const appointment = appointmentForRecord(record)
  if (!appointment) return 'Chưa có thông tin'
  return `${formatDate(appointment.appointmentDate)} · ${appointment.slotTime || '--:--'}`
}

function prescriptionItems(prescription?: Prescription | null) {
  return prescription?.items || prescription?.prescriptionItems || []
}

async function printMedicalRecord(record: MedicalRecord) {
  const recordId = record.medicalRecordId || record.recordId || record.id
  recordToPrint.value = record
  prescriptionToPrint.value = null
  toast.title = 'In hồ sơ bệnh án'
  toast.message = 'Đang chuẩn bị bản in...'
  toast.type = 'success'
  toast.show = true
  if (recordId) {
    try {
      const html = await medicalRecordApi.exportMedicalRecordHtml(recordId)
      if (html.trim()) {
        const printWindow = window.open('', '_blank')
        if (printWindow) {
          printWindow.document.open()
          printWindow.document.write(html)
          printWindow.document.close()
          printWindow.focus()
          setTimeout(() => printWindow.print(), 300)
          return
        }
      }
    } catch (err) {
      const status = (err as any)?.response?.status
      if (status === 403) {
        toast.title = 'Không có quyền truy cập'
        toast.message = 'Bạn không có quyền in hồ sơ bệnh án này.'
        toast.type = 'error'
        toast.show = true
        return
      }
      if (status !== 404) console.error('Failed to export medical record HTML', err)
    }
  }
  await Promise.all([
    loadPrescriptionData(record),
    loadBillingData(record),
  ])
  await nextTick()
  setTimeout(() => {
    window.print()
  }, 300)
}

async function printPrescription(prescription: Prescription) {
  prescriptionToPrint.value = prescription
  recordToPrint.value = null
  toast.title = 'In đơn thuốc'
  toast.message = 'Đang chuẩn bị bản in...'
  toast.type = 'success'
  toast.show = true
  await nextTick()
  setTimeout(() => {
    window.print()
  }, 300)
}

function triggerPrint() {
  if (selectedRecord.value) {
    printMedicalRecord(selectedRecord.value)
    return
  }
  if (medicalRecords.value.length === 1) {
    printMedicalRecord(medicalRecords.value[0])
    return
  }
  if (medicalRecords.value.length === 0) {
    toast.title = 'In hồ sơ bệnh án'
    toast.message = 'Không có hồ sơ bệnh án để in'
    toast.type = 'error'
    toast.show = true
    return
  }
  toast.title = 'In hồ sơ bệnh án'
  toast.message = 'Vui lòng chọn hồ sơ bệnh án cần in'
  toast.type = 'error'
  toast.show = true
}

// Helpers
function formatDate(value?: string) {
  if (!value) return 'Chưa có'
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return value
  const day = String(date.getDate()).padStart(2, '0')
  const month = String(date.getMonth() + 1).padStart(2, '0')
  const year = date.getFullYear()
  return `${day}/${month}/${year}`
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

function normalizeDateOnly(value?: string) {
  if (!value) return ''
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return String(value).slice(0, 10)
  return date.toISOString().slice(0, 10)
}

function formatCurrency(value?: number) {
  return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(Number(value || 0))
}

function genderLabel(value?: string) {
  const normalized = String(value || '').toLowerCase()
  if (normalized === 'male' || normalized === 'nam') return 'Nam'
  if (normalized === 'female' || normalized === 'nữ' || normalized === 'nu') return 'Nữ'
  return value || 'Chưa cập nhật'
}

function statusLabel(status?: string) {
  const value = String(status || '').toLowerCase()
  if (value.includes('completed') || value.includes('done') || value.includes('hoàn tất')) return 'Đã hoàn tất'
  if (value.includes('draft') || value.includes('bản nháp')) return 'Bản nháp'
  if (value.includes('progress') || value.includes('đang xử lý')) return 'Đang xử lý'
  return status || 'Chưa cập nhật'
}

function statusClass(status?: string) {
  const value = String(status || '').toLowerCase()
  if (value.includes('completed') || value.includes('done') || value.includes('hoàn tất')) return 'bg-emerald-50 text-emerald-700 border border-emerald-100'
  if (value.includes('draft') || value.includes('bản nháp')) return 'bg-amber-50 text-amber-700 border border-amber-100'
  if (value.includes('progress') || value.includes('đang xử lý')) return 'bg-blue-50 text-blue-700 border border-blue-100'
  return 'bg-slate-50 text-slate-700 border border-slate-100'
}

function invoiceStatusLabel(status?: string) {
  const s = String(status || '').toLowerCase()
  if (s.includes('paid') || s.includes('thanh toán')) return 'Đã thanh toán'
  if (s.includes('cancel') || s.includes('hủy')) return 'Đã hủy'
  return 'Chưa thanh toán'
}

function invoiceStatusClass(status?: string) {
  const s = String(status || '').toLowerCase()
  if (s.includes('paid') || s.includes('thanh toán')) return 'bg-emerald-50 text-emerald-700 border border-emerald-100'
  if (s.includes('cancel') || s.includes('hủy')) return 'bg-rose-50 text-rose-700 border border-rose-100'
  return 'bg-amber-50 text-amber-700 border border-amber-100'
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
/* Hide print area by default on screen */
.print-area {
  display: none !important;
}

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
</style>
