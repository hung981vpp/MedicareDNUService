<template>
  <section class="min-h-screen bg-[#f8fafc] py-2 sm:py-3">
    <FullscreenLoader :show="loading" />

    <div class="max-w-none mx-auto px-4 sm:px-6 lg:px-8 space-y-6">
      
      <header class="px-1">
        <h1 class="text-[1.75rem] font-semibold tracking-normal text-slate-950">Hồ sơ bệnh án</h1>
        
      </header>

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

      <div class="rounded-2xl border border-slate-200 bg-white shadow-sm overflow-hidden">
        <div class="flex items-center justify-between border-b border-slate-100 p-4 bg-slate-50/50">
          <span class="text-sm font-semibold text-slate-500">
            Tổng số {{ filteredRecords.length }} kết quả bệnh án
          </span>
        </div>

        <div v-if="filteredRecords.length" class="medical-record-table-shell">
          <ATable
            :columns="medicalRecordTableColumns"
            :data-source="filteredRecords"
            :pagination="medicalRecordPagination"
            :row-key="recordIdentity"
            :scroll="{ x: 1120 }"
            size="middle"
            @change="handleMedicalRecordTableChange"
          >
            <template #customFilterDropdown="{ setSelectedKeys, selectedKeys, confirm, clearFilters, column }">
              <div class="medical-record-filter">
                <p class="medical-record-filter-title">Tìm theo {{ String(column.title).toLowerCase() }}</p>
                <AInput
                  :value="selectedKeys[0]"
                  :placeholder="`Nhập ${String(column.title).toLowerCase()}...`"
                  allow-clear
                  autofocus
                  @change="setSelectedKeys(getRecordFilterKeys($event))"
                  @press-enter="confirm()"
                >
                  <template #prefix><Search class="h-3.5 w-3.5 text-slate-400" /></template>
                </AInput>
                <div class="medical-record-filter-actions">
                  <AButton size="small" class="medical-record-filter-reset" @click="clearRecordFilter(clearFilters, confirm)">Đặt lại</AButton>
                  <AButton type="primary" size="small" class="medical-record-filter-submit" @click="confirm()">Áp dụng</AButton>
                </div>
              </div>
            </template>
            <template #customFilterIcon="{ filtered }">
              <Search :class="['h-3.5 w-3.5', filtered ? 'text-[#0F52BA]' : 'text-slate-400']" />
            </template>
            <template #emptyText>
              <div class="py-8 text-center">
                <FileHeart class="mx-auto h-9 w-9 text-slate-300" />
                <p class="mt-3 font-bold text-slate-800">Không có hồ sơ bệnh án phù hợp</p>
                <p class="mt-1 text-sm text-slate-500">Thử đổi bộ lọc hoặc từ khóa tìm kiếm trong từng cột.</p>
              </div>
            </template>
            <template #bodyCell="{ column, record }">
              <template v-if="column.key === 'code'">
                <span class="font-bold text-slate-950">{{ recordCode(record) }}</span>
              </template>
              <template v-else-if="column.key === 'diagnosis'">
                <span class="line-clamp-2 max-w-sm text-[13px] font-semibold leading-5 text-slate-800" :title="recordDiagnosis(record)">
                  {{ recordDiagnosis(record) }}
                </span>
              </template>
              <template v-else-if="column.key === 'diagnosisCode'">
                <span class="font-mono text-xs font-semibold text-slate-600">{{ record.diagnosisCode || '-' }}</span>
              </template>
              <template v-else-if="column.key === 'createdAt'">
                <span class="whitespace-nowrap text-[13px] font-medium text-slate-500">{{ formatDate(record.createdAt) }}</span>
              </template>
              <template v-else-if="column.key === 'followUpDate'">
                <ATag v-if="record.followUpDate" :bordered="false" class="medical-follow-tag">{{ formatDate(record.followUpDate) }}</ATag>
                <span v-else class="text-xs font-semibold text-slate-400">Chưa có</span>
              </template>
              <template v-else-if="column.key === 'status'">
                <ATag :bordered="false" :class="['medical-status-tag', statusClass(record.status)]">
                  <span class="medical-status-dot"></span>
                  {{ statusLabel(record.status) }}
                </ATag>
              </template>
              <template v-else-if="column.key === 'actions'">
                <div class="medical-record-actions">
                  <button type="button" class="medical-record-action-button medical-record-action-primary" title="Xem chi tiết bệnh án" @click="openDetails(record)">
                    <Eye class="h-4 w-4" />
                    <span>Chi tiết</span>
                  </button>
                  <button type="button" class="medical-record-action-button medical-record-action-muted" title="In hồ sơ bệnh án" @click="printMedicalRecord(record)">
                    <Printer class="h-4 w-4" />
                    <span>In</span>
                  </button>
                </div>
              </template>
            </template>
          </ATable>
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
            <div><span class="font-bold text-slate-500">Mã lịch hẹn:</span> <span class="font-semibold text-slate-800 font-mono">{{ printAppointmentId(recordToPrint) }}</span></div>
            <div><span class="font-bold text-slate-500">Ngày giờ khám:</span> <span class="font-semibold text-slate-800">{{ appointmentTimeLabel(recordToPrint) }}</span></div>
            <div><span class="font-bold text-slate-500">Chuyên khoa:</span> <span class="font-semibold text-slate-800">{{ printAppointmentSpecialty(recordToPrint) }}</span></div>
            <div><span class="font-bold text-slate-500">Số thứ tự:</span> <span class="font-semibold text-slate-800">{{ printQueueNumber(recordToPrint) }}</span></div>
            <div class="col-span-2"><span class="font-bold text-slate-500">Lý do khám:</span> <span class="font-semibold text-slate-800">{{ printChiefComplaint(recordToPrint) }}</span></div>
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
            <div class="col-span-2"><span class="font-bold text-slate-500">Triệu chứng:</span> <span class="font-semibold text-slate-800">{{ printSymptoms(recordToPrint) }}</span></div>
          </div>
        </div>

        <!-- Vital Signs -->
        <div class="mb-5 print-section">
          <h2 class="text-xs font-bold uppercase tracking-wider text-slate-400 border-b border-slate-200 pb-1 mb-2">Sinh hiệu</h2>
          <div v-if="printVitalItems.length" class="grid grid-cols-4 gap-2 text-xs">
            <div v-for="item in printVitalItems" :key="item.label" class="print-field rounded-lg border border-slate-200 bg-slate-50 p-2.5">
              <p class="font-bold text-slate-500">{{ item.label }}</p>
              <p class="mt-1 font-bold text-slate-900">{{ item.value }}</p>
            </div>
          </div>
          <p v-else class="text-xs italic text-slate-500">Chưa ghi nhận sinh hiệu cho lượt khám này.</p>
          <div v-if="printVitalNote" class="mt-2 rounded-lg border border-slate-200 px-3 py-2 text-xs text-slate-700">
            <span class="font-bold">Ghi chú điều dưỡng:</span> {{ printVitalNote }}
          </div>
        </div>

        <!-- Diagnosis Info -->
        <div class="mb-5">
          <h2 class="text-xs font-bold uppercase tracking-wider text-slate-400 border-b border-slate-200 pb-1 mb-2">Chẩn đoán</h2>
          <div class="space-y-2 text-xs">
            <div><span class="font-bold text-slate-500">Mã ICD:</span> <span class="font-mono bg-slate-50 px-1.5 py-0.5 rounded font-bold">{{ recordToPrint.diagnosisCode || 'Chưa có thông tin' }}</span></div>
            <div><span class="font-bold text-slate-500">Chuyên khoa ICD:</span> <span class="font-semibold text-slate-800">{{ recordToPrint.diagnosisSpecialty || 'Chưa có thông tin' }}</span></div>
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

        <!-- Clinical Orders -->
        <div class="mb-6 print-section">
          <h2 class="text-xs font-bold uppercase tracking-wider text-slate-400 border-b border-slate-200 pb-1 mb-2">Cận lâm sàng</h2>
          <table v-if="printClinicalOrders.length" class="min-w-full border border-slate-200 text-xs">
            <thead class="bg-slate-50 font-bold text-slate-600 text-left">
              <tr>
                <th class="px-2 py-1.5 border-r border-slate-200">Chỉ định</th>
                <th class="px-2 py-1.5 border-r border-slate-200">Kết quả</th>
                <th class="px-2 py-1.5 border-r border-slate-200">Kết luận</th>
                <th class="px-2 py-1.5">Trạng thái</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-200">
              <tr v-for="order in printClinicalOrders" :key="order.id || order.clinicalOrderCode">
                <td class="px-2 py-1.5 border-r border-slate-200">
                  <p class="font-bold text-slate-800">{{ order.orderName || 'Chưa cập nhật' }}</p>
                  <p class="text-slate-500">{{ order.clinicalOrderCode || order.orderType || '' }}</p>
                </td>
                <td class="px-2 py-1.5 border-r border-slate-200">{{ clinicalOrderResult(order) }}</td>
                <td class="px-2 py-1.5 border-r border-slate-200">{{ order.conclusion || 'Chưa có kết luận' }}</td>
                <td class="px-2 py-1.5 font-semibold">{{ statusLabel(order.status) }}</td>
              </tr>
            </tbody>
          </table>
          <p v-else class="text-xs italic text-slate-500">Không có chỉ định cận lâm sàng trong bệnh án này.</p>
        </div>

        <!-- Prescription Info -->
        <div class="mb-6 print-section">
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
                  <th class="px-2 py-1.5 border-r border-slate-200 w-20 text-center">Số ngày</th>
                  <th class="px-2 py-1.5">Hướng dẫn</th>
                </tr>
              </thead>
              <tbody class="divide-y divide-slate-200 bg-white">
                <tr v-for="(item, index) in prescriptionItems(activePrescription)" :key="item.id || index">
                  <td class="px-2 py-1.5 border-r border-slate-200 text-center font-medium">{{ index + 1 }}</td>
                  <td class="px-2 py-1.5 border-r border-slate-200 font-bold text-slate-800">{{ prescriptionItemMedicineName(item) }}</td>
                  <td class="px-2 py-1.5 border-r border-slate-200 text-center font-bold">{{ prescriptionItemQuantity(item) }}</td>
                  <td class="px-2 py-1.5 border-r border-slate-200 font-medium">{{ prescriptionItemDosage(item) }}</td>
                  <td class="px-2 py-1.5 border-r border-slate-200 text-center font-medium">{{ prescriptionItemDuration(item) }}</td>
                  <td class="px-2 py-1.5 font-medium">{{ prescriptionItemInstruction(item) }}</td>
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
        <div class="mb-6 print-section">
          <h2 class="text-xs font-bold uppercase tracking-wider text-slate-400 border-b border-slate-200 pb-1 mb-2">Viện phí</h2>
          <template v-if="activeInvoice">
            <div class="grid grid-cols-2 gap-y-2 text-xs">
              <div><span class="font-bold text-slate-500">Mã hóa đơn:</span> <span class="font-semibold text-slate-800 font-mono">{{ activeInvoice.invoiceCode || activeInvoice.invoiceIdCode || activeInvoice.invoiceId }}</span></div>
              <div><span class="font-bold text-slate-500">Trạng thái:</span> <span class="font-semibold text-slate-800">{{ invoiceStatusLabel(activeInvoice.status) }}</span></div>
              <div><span class="font-bold text-slate-500">Phí khám:</span> <span class="font-semibold text-slate-800">{{ formatCurrency(activeInvoice.examinationFee || activeInvoice.examFee) }}</span></div>
              <div><span class="font-bold text-slate-500">Tiền thuốc:</span> <span class="font-semibold text-slate-800">{{ formatCurrency(activeInvoice.medicineTotal) }}</span></div>
              <div><span class="font-bold text-slate-500">Tổng cộng:</span> <span class="font-bold text-slate-900">{{ formatCurrency(activeInvoice.totalAmount || activeInvoice.amount) }}</span></div>
              <div><span class="font-bold text-slate-500">{{ isPaidInvoice(activeInvoice.status) ? 'Ngày thanh toán:' : 'Ngày lập hóa đơn:' }}</span> <span class="font-semibold text-slate-800">{{ formatDateTime(activeInvoice.paidAt || activeInvoice.createdAt) }}</span></div>
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

  </section>
</template>

<script setup lang="ts">
import { computed, nextTick, onMounted, reactive, ref, watch } from 'vue'
import { Button as AButton, Input as AInput, Table as ATable, Tag as ATag } from 'ant-design-vue'
import {
  CalendarClock,
  CheckCircle2,
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
import FullscreenLoader from '@/components/ui/FullscreenLoader.vue'
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
const printAppointment = ref<Record<string, any> | null>(null)
const printVisit = ref<Record<string, any> | null>(null)
const printClinicalOrders = ref<Array<Record<string, any>>>([])
const printPrescriptions = ref<Prescription[]>([])

const printVitalSigns = computed(() => parseVitalSigns(recordToPrint.value?.vitalSignsJson || printVisit.value?.vitalSignsJson))
const printVitalItems = computed(() => {
  const vitals = printVitalSigns.value
  const items = [
    { label: 'Nhiệt độ', value: vitalDisplay(vitals, ['temperature', 'Temperature'], '°C') },
    { label: 'Huyết áp', value: vitalDisplay(vitals, ['bloodPressure', 'BloodPressure']) },
    { label: 'Mạch', value: vitalDisplay(vitals, ['heartRate', 'HeartRate'], 'lần/phút') },
    { label: 'Nhịp thở', value: vitalDisplay(vitals, ['respiratoryRate', 'RespiratoryRate'], 'lần/phút') },
    { label: 'SpO2', value: vitalDisplay(vitals, ['spo2', 'Spo2', 'spO2', 'SpO2', 'SpO₂'], '%') },
    { label: 'Cân nặng', value: vitalDisplay(vitals, ['weight', 'Weight'], 'kg') },
    { label: 'Chiều cao', value: vitalDisplay(vitals, ['height', 'Height'], 'cm') },
    { label: 'BMI', value: vitalBmiDisplay(vitals) },
  ]
  return items.filter(item => item.value !== '')
})
const printVitalNote = computed(() => String(readFirst(printVitalSigns.value, 'note', 'Note') || '').trim())

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

const medicalRecordTableColumns = [
  {
    title: 'Mã BA',
    key: 'code',
    width: 150,
    customFilterDropdown: true,
    onFilter: recordColumnFilter('code'),
    sorter: (a: MedicalRecord, b: MedicalRecord) => recordCode(a).localeCompare(recordCode(b), 'vi'),
  },
  {
    title: 'Chẩn đoán',
    key: 'diagnosis',
    minWidth: 260,
    customFilterDropdown: true,
    onFilter: recordColumnFilter('diagnosis'),
    sorter: (a: MedicalRecord, b: MedicalRecord) => recordDiagnosis(a).localeCompare(recordDiagnosis(b), 'vi'),
  },
  {
    title: 'Mã ICD',
    dataIndex: 'diagnosisCode',
    key: 'diagnosisCode',
    width: 170,
    customFilterDropdown: true,
    onFilter: recordColumnFilter('diagnosisCode'),
  },
  {
    title: 'Ngày tạo',
    dataIndex: 'createdAt',
    key: 'createdAt',
    width: 170,
    customFilterDropdown: true,
    onFilter: recordColumnFilter('createdAt'),
    sorter: (a: MedicalRecord, b: MedicalRecord) => recordTimestamp(a.createdAt) - recordTimestamp(b.createdAt),
    defaultSortOrder: 'descend' as const,
  },
  {
    title: 'Tái khám',
    dataIndex: 'followUpDate',
    key: 'followUpDate',
    width: 170,
    customFilterDropdown: true,
    onFilter: recordColumnFilter('followUpDate'),
    sorter: (a: MedicalRecord, b: MedicalRecord) => recordTimestamp(a.followUpDate) - recordTimestamp(b.followUpDate),
  },
  {
    title: 'Trạng thái',
    dataIndex: 'status',
    key: 'status',
    width: 210,
    customFilterDropdown: true,
    onFilter: recordColumnFilter('status'),
  },
  {
    title: 'Thao tác',
    key: 'actions',
    width: 180,
    align: 'right' as const,
    fixed: 'right' as const,
  },
]

const medicalRecordPagination = computed(() => ({
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

function recordIdentity(record: MedicalRecord | Record<string, any>) {
  return String(
    record.medicalRecordId ||
    record.recordId ||
    record.medicalRecordCode ||
    record.id ||
    (record.visitId ? `visit-${record.visitId}` : '') ||
    `${record.patientId}-${record.createdAt}-${record.diagnosisCode}-${record.diagnosisText}`,
  )
}

function recordCode(record: MedicalRecord | Record<string, any>) {
  return String(record.medicalRecordCode || record.medicalRecordIdCode || record.recordIdCode || record.recordId || record.medicalRecordId || 'Chưa cập nhật')
}

function recordDiagnosis(record: MedicalRecord | Record<string, any>) {
  return String(record.diagnosisText || record.diagnosis || 'Chưa cập nhật chẩn đoán')
}

function recordTimestamp(value?: string | null) {
  if (!value) return 0
  const time = new Date(value).getTime()
  return Number.isNaN(time) ? 0 : time
}

function recordSearchField(record: MedicalRecord | Record<string, any>, key: string) {
  if (key === 'code') return recordCode(record)
  if (key === 'diagnosis') return recordDiagnosis(record)
  if (key === 'diagnosisCode') return record.diagnosisCode || '-'
  if (key === 'createdAt') return formatDate(record.createdAt)
  if (key === 'followUpDate') return record.followUpDate ? formatDate(record.followUpDate) : 'Chưa có'
  if (key === 'status') return statusLabel(record.status)
  return ''
}

function recordColumnFilter(key: string) {
  return (filterValue: string | number | boolean, record: MedicalRecord | Record<string, any>) =>
    normalizeSearchText(recordSearchField(record, key)).includes(normalizeSearchText(filterValue))
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

function getRecordFilterKeys(event: Event) {
  const filterValue = (event.target as HTMLInputElement)?.value || ''
  return filterValue ? [filterValue] : []
}

function clearRecordFilter(clearFilters: (() => void) | undefined, confirm: () => void) {
  clearFilters?.()
  confirm()
}

function handleMedicalRecordTableChange(pagination: { current?: number; pageSize?: number }) {
  currentPage.value = pagination.current || 1
  itemsPerPage.value = pagination.pageSize || 10
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

function openDetails(record: MedicalRecord | Record<string, any>) {
  const medicalRecord = record as MedicalRecord
  selectedRecord.value = medicalRecord
  currentTab.value = 'overview'
  activePrescription.value = null
  activeInvoice.value = null
  drawerOpen.value = true

  const recordId = medicalRecord.medicalRecordId || medicalRecord.recordId || medicalRecord.id
  if (recordId) {
    medicalRecordApi.getCompleteMedicalRecord(recordId)
      .then((completeRecord) => {
        selectedRecord.value = { ...medicalRecord, ...(completeRecord as Record<string, any>) }
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
  void loadPrescriptionData(medicalRecord)
  void loadBillingData(medicalRecord)
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

    const n2List = [...(timeline.prescriptions || []), ...printPrescriptions.value]

    const recordCode = record.medicalRecordCode || record.medicalRecordIdCode || record.recordIdCode || record.recordId
    const recordId = record.medicalRecordId || record.id
    const appointmentId = Number(record.appointmentId || appointmentForRecord(record)?.appointmentId || 0)

    const n2Match = n2List.find(p => prescriptionMatchesRecord(p, recordCode, recordId, appointmentId))
    const n3Match = n3List.find(p => prescriptionMatchesRecord(p, recordCode, recordId, appointmentId))

    if (n2Match || n3Match) {
      activePrescription.value = mergePrescriptionForPrint(n2Match, n3Match)
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
    const appointmentId = Number(record.appointmentId || appointmentForRecord(record)?.appointmentId || 0)
    const recordId = Number(record.medicalRecordId || record.id || 0)
    const recordCode = String(record.medicalRecordCode || record.medicalRecordIdCode || record.recordIdCode || record.recordId || '')
    const prescriptionIds = new Set(
      [activePrescription.value, ...printPrescriptions.value]
        .map(p => Number(p?.prescriptionId || p?.id || 0))
        .filter(id => id > 0),
    )

    const match = list.find((invoice) => {
      const raw = invoice as Invoice & Record<string, any>
      if (appointmentId && Number(raw.appointmentId || raw.AppointmentId) === appointmentId) return true
      if (recordId && Number(raw.medicalRecordId || raw.MedicalRecordId) === recordId) return true
      if (recordCode && [raw.medicalRecordCode, raw.MedicalRecordCode, raw.medicalRecordIdCode, raw.MedicalRecordIdCode].some(code => String(code || '') === recordCode)) return true
      const prescriptionId = Number(raw.prescriptionId || raw.PrescriptionId || 0)
      return prescriptionId > 0 && prescriptionIds.has(prescriptionId)
    })

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
  if (printAppointment.value && Number(printAppointment.value.appointmentId) === appointmentId) {
    return printAppointment.value as Appointment
  }
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
  return record.doctorName || getDoctorName(record.doctorId) || appointmentForRecord(record)?.doctorName || ''
}

function objectValue(source: unknown, ...keys: string[]) {
  const data = source as Record<string, any> | null | undefined
  if (!data) return undefined
  for (const key of keys) {
    const value = data[key]
    if (value !== undefined && value !== null && String(value).trim() !== '') return value
  }
  return undefined
}

function printAppointmentId(record?: MedicalRecord | null) {
  return objectValue(appointmentForRecord(record), 'appointmentId', 'AppointmentId')
    || objectValue(printVisit.value, 'appointmentId', 'AppointmentId')
    || objectValue(record, 'appointmentId', 'AppointmentId')
    || 'Chưa có thông tin'
}

function printAppointmentSpecialty(record?: MedicalRecord | null) {
  return objectValue(appointmentForRecord(record), 'specialtyName', 'specialtyNameSnapshot', 'SpecialtyNameSnapshot')
    || objectValue(record, 'specialtyName', 'specialtyNameSnapshot', 'SpecialtyNameSnapshot')
    || 'Chưa có thông tin'
}

function printQueueNumber(record?: MedicalRecord | null) {
  return objectValue(appointmentForRecord(record), 'queueNumber', 'QueueNumber')
    || objectValue(record, 'queueNumber', 'QueueNumber')
    || 'Chưa có thông tin'
}

function printChiefComplaint(record?: MedicalRecord | null) {
  return objectValue(appointmentForRecord(record), 'reason', 'Reason')
    || objectValue(printVisit.value, 'chiefComplaint', 'ChiefComplaint')
    || objectValue(record, 'chiefComplaint', 'ChiefComplaint', 'reason', 'Reason')
    || 'Chưa ghi nhận'
}

function printSymptoms(record?: MedicalRecord | null) {
  return objectValue(record, 'symptoms', 'Symptoms')
    || objectValue(printVisit.value, 'symptoms', 'Symptoms')
    || 'Chưa ghi nhận'
}

function appointmentTimeLabel(record?: MedicalRecord | null) {
  const appointment = appointmentForRecord(record)
  const scheduledAt = objectValue(appointment, 'scheduledAt', 'ScheduledAt')
    || objectValue(printVisit.value, 'visitDate', 'VisitDate', 'createdAt', 'CreatedAt')
    || objectValue(record, 'examDate', 'createdAt', 'CreatedAt')
  if (scheduledAt) return formatDateTime(scheduledAt)
  if (!appointment) return 'Chưa có thông tin'
  return `${formatDate(objectValue(appointment, 'appointmentDate', 'AppointmentDate'))} · ${objectValue(appointment, 'slotTime', 'SlotTime') || '--:--'}`
}

function prescriptionItems(prescription?: Prescription | null) {
  return prescription?.items || prescription?.prescriptionItems || []
}

function prescriptionItemMedicineName(item: Record<string, any>) {
  return String(readFirst(item, 'medicineNameSnapshot', 'MedicineNameSnapshot', 'medicineName', 'MedicineName') || 'Chưa cập nhật')
}

function prescriptionItemQuantity(item: Record<string, any>) {
  const quantity = readFirst(item, 'quantity', 'Quantity')
  const unit = String(readFirst(item, 'unitSnapshot', 'UnitSnapshot', 'unit', 'Unit') || '').trim()
  return [quantity || '-', unit].filter(Boolean).join(' ')
}

function prescriptionItemDosage(item: Record<string, any>) {
  const dosage = String(readFirst(item, 'dosage', 'Dosage') || '').trim()
  const frequency = String(readFirst(item, 'frequency', 'Frequency') || '').trim()
  return [dosage, frequency].filter(Boolean).join(' · ') || 'Chưa cập nhật'
}

function prescriptionItemDuration(item: Record<string, any>) {
  const value = readFirst(item, 'durationDays', 'DurationDays')
  const days = Number(value)
  return Number.isFinite(days) && days > 0 ? `${days} ngày` : 'Chưa cập nhật'
}

function prescriptionItemInstruction(item: Record<string, any>) {
  return String(readFirst(item, 'usageInstruction', 'UsageInstruction', 'note', 'Note') || '').trim() || 'Theo dặn dò của bác sĩ'
}

function prescriptionMatchesRecord(
  prescription: Prescription,
  recordCode: string | number | undefined,
  recordId: string | number | undefined,
  appointmentId: number,
) {
  return Boolean(
    (recordCode && [prescription.medicalRecordCode, prescription.medicalRecordIdCode].some(code => String(code || '') === String(recordCode))) ||
    (recordId && Number(prescription.medicalRecordId) === Number(recordId)) ||
    (appointmentId && Number(prescription.appointmentId) === appointmentId),
  )
}

function mergePrescriptionForPrint(n2?: Prescription, n3?: Prescription): Prescription {
  const n2Items = prescriptionItems(n2)
  const n3Items = prescriptionItems(n3)
  return {
    ...(n2 || {}),
    ...(n3 || {}),
    items: n3Items.length ? n3Items : n2Items,
    prescriptionItems: n3Items.length ? n3Items : n2Items,
    status: n3?.status || n2?.status,
    note: n3?.note || n2?.note,
  } as Prescription
}

function applyCompleteRecordForPrint(baseRecord: MedicalRecord, complete: Record<string, any>) {
  const patient = complete.patient || complete.Patient
  const appointment = complete.appointment || complete.Appointment
  const visit = complete.visit || complete.Visit || {}
  const medicalRecord = complete.medicalRecord || complete.MedicalRecord || {}
  const clinicalOrders = complete.clinicalOrders || complete.ClinicalOrders || []
  const prescriptions = complete.prescriptions || complete.Prescriptions || []

  if (patient) patientDetail.value = { ...(patientDetail.value || {} as Patient), ...patient }
  printAppointment.value = appointment || null
  printVisit.value = visit || null
  printClinicalOrders.value = Array.isArray(clinicalOrders) ? clinicalOrders : []
  printPrescriptions.value = Array.isArray(prescriptions) ? prescriptions : []

  recordToPrint.value = {
    ...baseRecord,
    ...medicalRecord,
    medicalRecordId: objectValue(medicalRecord, 'medicalRecordId', 'id', 'Id') || baseRecord.medicalRecordId,
    medicalRecordCode: objectValue(medicalRecord, 'medicalRecordCode', 'MedicalRecordCode') || baseRecord.medicalRecordCode,
    visitId: objectValue(medicalRecord, 'visitId', 'VisitId') || objectValue(visit, 'id', 'Id', 'visitId', 'VisitId') || baseRecord.visitId,
    appointmentId: objectValue(visit, 'appointmentId', 'AppointmentId') || objectValue(appointment, 'appointmentId', 'AppointmentId') || baseRecord.appointmentId,
    doctorId: objectValue(medicalRecord, 'doctorId', 'DoctorId') || objectValue(visit, 'doctorId', 'DoctorId') || objectValue(appointment, 'doctorId', 'DoctorId') || baseRecord.doctorId,
    doctorName: objectValue(visit, 'doctorName', 'DoctorName') || objectValue(appointment, 'doctorNameSnapshot', 'DoctorNameSnapshot', 'doctorName', 'DoctorName') || baseRecord.doctorName,
    chiefComplaint: objectValue(visit, 'chiefComplaint', 'ChiefComplaint') || objectValue(appointment, 'reason', 'Reason') || baseRecord.chiefComplaint,
    symptoms: objectValue(visit, 'symptoms', 'Symptoms') || objectValue(medicalRecord, 'symptoms', 'Symptoms') || baseRecord.symptoms,
    vitalSignsJson: objectValue(visit, 'vitalSignsJson', 'VitalSignsJson') || baseRecord.vitalSignsJson,
    diagnosisCode: objectValue(medicalRecord, 'diagnosisCode', 'DiagnosisCode') || baseRecord.diagnosisCode,
    diagnosisSpecialty: objectValue(medicalRecord, 'diagnosisSpecialty', 'DiagnosisSpecialty') || baseRecord.diagnosisSpecialty,
    diagnosisText: objectValue(medicalRecord, 'diagnosisText', 'DiagnosisText') || baseRecord.diagnosisText,
    doctorNote: objectValue(medicalRecord, 'doctorNote', 'DoctorNote') || baseRecord.doctorNote,
    treatmentPlan: objectValue(medicalRecord, 'treatmentPlan', 'TreatmentPlan') || baseRecord.treatmentPlan,
    followUpDate: objectValue(medicalRecord, 'followUpDate', 'FollowUpDate') || baseRecord.followUpDate,
    createdAt: objectValue(medicalRecord, 'createdAt', 'CreatedAt') || baseRecord.createdAt,
    updatedAt: objectValue(medicalRecord, 'updatedAt', 'UpdatedAt') || baseRecord.updatedAt,
    completedAt: objectValue(medicalRecord, 'completedAt', 'CompletedAt') || objectValue(visit, 'completedAt', 'CompletedAt') || baseRecord.completedAt,
    specialtyName: objectValue(appointment, 'specialtyName', 'specialtyNameSnapshot', 'SpecialtyNameSnapshot') || objectValue(baseRecord, 'specialtyName', 'specialtyNameSnapshot', 'SpecialtyNameSnapshot'),
    queueNumber: objectValue(appointment, 'queueNumber', 'QueueNumber') || objectValue(baseRecord, 'queueNumber', 'QueueNumber'),
  }
}

function parseVitalSigns(value: unknown): Record<string, any> {
  if (!value) return {}
  if (typeof value === 'object') return value as Record<string, any>
  try {
    const parsed = JSON.parse(String(value))
    return parsed && typeof parsed === 'object' ? parsed as Record<string, any> : {}
  } catch {
    return {}
  }
}

function readFirst(source: Record<string, any>, ...keys: string[]) {
  for (const key of keys) {
    const value = source[key]
    if (value !== undefined && value !== null && String(value).trim() !== '') return value
  }
  return undefined
}

function vitalDisplay(source: Record<string, any>, keys: string[], unit = '') {
  const value = readFirst(source, ...keys)
  if (value === undefined) return ''
  return `${value}${unit ? ` ${unit}` : ''}`
}

function vitalBmiDisplay(source: Record<string, any>) {
  const weight = Number(readFirst(source, 'weight', 'Weight'))
  const height = Number(readFirst(source, 'height', 'Height'))
  if (!Number.isFinite(weight) || !Number.isFinite(height) || weight <= 0 || height <= 0) return ''
  const heightMeters = height / 100
  return `${(weight / (heightMeters * heightMeters)).toFixed(1)} kg/m²`
}

function clinicalOrderResult(order: Record<string, any>) {
  const resultText = String(order.resultText || '').trim()
  if (resultText) return resultText
  const value = String(order.resultValue || '').trim()
  const unit = String(order.resultUnit || '').trim()
  return [value, unit].filter(Boolean).join(' ') || 'Chưa có kết quả'
}

async function printMedicalRecord(record: MedicalRecord | Record<string, any>) {
  const medicalRecord = record as MedicalRecord
  const recordId = medicalRecord.medicalRecordId || medicalRecord.recordId || medicalRecord.id
  recordToPrint.value = null
  printAppointment.value = null
  printVisit.value = null
  printClinicalOrders.value = []
  printPrescriptions.value = []
  toast.title = 'In hồ sơ bệnh án'
  toast.message = 'Đang đồng bộ bệnh án, đơn thuốc và viện phí...'
  toast.type = 'success'
  toast.show = true

  if (recordId) {
    try {
      const complete = await medicalRecordApi.getCompleteMedicalRecord(recordId)
      applyCompleteRecordForPrint(medicalRecord, complete)
    } catch (err) {
      const status = (err as any)?.response?.status
      if (status === 403) {
        toast.title = 'Không có quyền truy cập'
        toast.message = 'Bạn không có quyền in hồ sơ bệnh án này.'
        toast.type = 'error'
        toast.show = true
        return
      }
      if (status !== 404) console.error('Failed to load complete medical record', err)
      recordToPrint.value = medicalRecord
    }
  } else {
    recordToPrint.value = medicalRecord
  }

  await loadPrescriptionData(recordToPrint.value || medicalRecord)

  if (!activePrescription.value && printPrescriptions.value.length) {
    activePrescription.value = printPrescriptions.value[0]
  }

  await loadBillingData(recordToPrint.value || medicalRecord)

  await nextTick()
  setTimeout(() => {
    window.print()
  }, 300)
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
  if (isPaidInvoice(status)) return 'Đã thanh toán'
  if (s.includes('cancel') || s.includes('hủy')) return 'Đã hủy'
  return 'Chưa thanh toán'
}

function invoiceStatusClass(status?: string) {
  const s = String(status || '').toLowerCase()
  if (isPaidInvoice(status)) return 'bg-emerald-50 text-emerald-700 border border-emerald-100'
  if (s.includes('cancel') || s.includes('hủy')) return 'bg-rose-50 text-rose-700 border border-rose-100'
  return 'bg-amber-50 text-amber-700 border border-amber-100'
}

function isPaidInvoice(status?: string) {
  const normalized = String(status || '').trim().toLowerCase()
  if (!normalized || normalized.includes('unpaid') || normalized.includes('chưa thanh toán') || normalized.includes('chua thanh toan')) return false
  return normalized === 'paid' || normalized.includes('đã thanh toán') || normalized.includes('da thanh toan')
}
</script>

<style scoped>
.line-clamp-2 {
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
}

.medical-record-table-shell {
  overflow: hidden;
}

.medical-record-table-shell :deep(.ant-table) {
  color: #334155;
  font-size: 14px;
}

.medical-record-table-shell :deep(.ant-table-thead > tr > th) {
  background: #f8fafc;
  border-bottom: 1px solid #e2e8f0;
  color: #64748b;
  font-size: 12px;
  font-weight: 800;
  letter-spacing: 0;
  padding: 16px 20px;
  text-transform: uppercase;
}

.medical-record-table-shell :deep(.ant-table-tbody > tr > td) {
  border-bottom: 1px solid #f1f5f9;
  padding: 18px 20px;
  vertical-align: middle;
}

.medical-record-table-shell :deep(.ant-table-tbody > tr:hover > td) {
  background: #f8fafc;
}

.medical-record-table-shell :deep(.ant-table-cell-fix-right) {
  background: #fff;
}

.medical-record-table-shell :deep(.ant-table-tbody > tr:hover > .ant-table-cell-fix-right) {
  background: #f8fafc;
}

.medical-record-table-shell :deep(.ant-pagination) {
  border-top: 1px solid #f1f5f9;
  margin: 0;
  padding: 16px;
}

.medical-record-filter {
  width: 260px;
  padding: 12px;
}

.medical-record-filter-title {
  color: #475569;
  font-size: 12px;
  font-weight: 800;
  margin: 0 0 8px;
}

.medical-record-filter-actions {
  display: flex;
  gap: 8px;
  justify-content: flex-end;
  margin-top: 10px;
}

.medical-record-filter-reset {
  border-color: #e2e8f0;
  color: #64748b;
  font-weight: 700;
}

.medical-record-filter-submit {
  background: #0F52BA;
  border-color: #0F52BA;
  font-weight: 700;
}

.medical-follow-tag,
.medical-status-tag {
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

.medical-follow-tag {
  background: #eff6ff;
  color: #1d4ed8;
}

.medical-status-dot {
  background: currentColor;
  border-radius: 999px;
  height: 7px;
  width: 7px;
}

.medical-record-actions {
  display: inline-flex;
  flex-wrap: wrap;
  gap: 8px;
  justify-content: flex-end;
}

.medical-record-action-button {
  align-items: center;
  border-radius: 999px;
  display: inline-flex;
  font-size: 13px;
  font-weight: 800;
  gap: 8px;
  height: 36px;
  justify-content: center;
  padding: 0 14px;
  transition: background .2s, border-color .2s, color .2s;
}

.medical-record-action-primary {
  background: #eff6ff;
  border: 1px solid #dbeafe;
  color: #1d4ed8;
}

.medical-record-action-primary:hover {
  background: #dbeafe;
  border-color: #bfdbfe;
  color: #0F52BA;
}

.medical-record-action-muted {
  background: #f8fafc;
  border: 1px solid #e2e8f0;
  color: #64748b;
}

.medical-record-action-muted:hover {
  background: #f1f5f9;
  border-color: #cbd5e1;
  color: #334155;
}
</style>

<style>
/* Hide print area by default on screen */
.print-area {
  display: none !important;
}

@media print {
  html,
  body {
    background: white !important;
    color: #0f172a !important;
    font-family: Arial, "Helvetica Neue", sans-serif !important;
    print-color-adjust: exact;
    -webkit-print-color-adjust: exact;
  }

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

  .print-container {
    width: 100% !important;
    max-width: none !important;
    padding: 0 !important;
    margin: 0 !important;
    font-size: 11px !important;
    line-height: 1.45 !important;
  }

  .print-container h1 {
    letter-spacing: 0 !important;
  }

  .print-container h2 {
    color: #0f4c9a !important;
    letter-spacing: 0 !important;
  }

  .print-container table {
    width: 100% !important;
    border-collapse: collapse !important;
  }

  .print-container th,
  .print-container td {
    overflow-wrap: anywhere;
    vertical-align: top !important;
  }

  .print-container thead {
    display: table-header-group;
  }

  .print-container tr,
  .print-field,
  .print-section {
    break-inside: avoid;
    page-break-inside: avoid;
  }

  .print-container img {
    max-height: 32px !important;
  }

  @page {
    size: A4;
    margin: 12mm 14mm 14mm;
  }
}
</style>
