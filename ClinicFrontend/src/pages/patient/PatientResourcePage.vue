<template>
  <section class="min-h-screen bg-[#f8fafc] py-2 sm:py-3">
    <FullscreenLoader :show="loading" />

    <div class="mx-auto max-w-none space-y-6 px-4 sm:px-6 lg:px-8">
      <header class="px-1">
        <h1 class="text-[1.75rem] font-semibold tracking-normal text-slate-950">{{ config.title }}</h1>
        <p class="mt-1.5 text-[13px] leading-5 text-slate-500">{{ config.description }}</p>
      </header>

    <div v-if="resource !== 'profile' && resource !== 'appointments'" class="grid gap-4 sm:grid-cols-3">
      <div v-for="metric in metrics" :key="metric.label" class="rounded-2xl border border-slate-200 bg-white p-4 shadow-sm">
        <p class="text-xs font-bold uppercase tracking-wide text-slate-400">{{ metric.label }}</p>
        <p class="mt-2 text-2xl font-bold text-slate-950">{{ metric.value }}</p>
        <p class="mt-1 text-sm text-slate-500">{{ metric.note }}</p>
      </div>
    </div>

    <div v-if="note && resource !== 'appointments'" class="rounded-xl border border-blue-100 bg-blue-50 px-4 py-3 text-sm text-[#003c90]">{{ note }}</div>
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
            <p class="text-xs font-bold uppercase tracking-wide text-slate-400">Mã bệnh nhân</p>
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
          <p>Lịch hẹn được liên kết theo mã bệnh nhân.</p>
          <p>Hồ sơ khám bệnh và đơn thuốc được tổng hợp theo từng lượt khám.</p>
          <p>Viện phí được hiển thị theo tài khoản hoặc mã bệnh nhân.</p>
        </div>
      </section>
    </div>

    <div v-else-if="resource === 'appointments'" class="appointment-table-shell">
      <ATable
        :columns="appointmentTableColumns"
        :data-source="rows"
        :pagination="appointmentPagination"
        :scroll="{ x: 1080 }"
        row-key="id"
        size="middle"
        @change="handleAppointmentTableChange"
      >
        <template #customFilterDropdown="{ setSelectedKeys, selectedKeys, confirm, clearFilters, column }">
          <div class="appointment-filter">
            <p class="appointment-filter-title">Tìm theo {{ String(column.title).toLowerCase() }}</p>
            <AInput
              :value="selectedKeys[0]"
              :placeholder="`Nhập ${String(column.title).toLowerCase()}...`"
              allow-clear
              autofocus
              @change="setSelectedKeys(getFilterKeys($event))"
              @press-enter="confirm()"
            >
              <template #prefix><Search class="h-3.5 w-3.5 text-slate-400" /></template>
            </AInput>
            <div class="appointment-filter-actions">
              <AButton size="small" class="appointment-filter-reset" @click="clearAppointmentFilter(clearFilters, confirm)">Đặt lại</AButton>
              <AButton type="primary" size="small" class="appointment-filter-submit" @click="confirm()">Áp dụng</AButton>
            </div>
          </div>
        </template>
        <template #customFilterIcon="{ filtered }">
          <Search :class="['h-3.5 w-3.5', filtered ? 'text-[#0F52BA]' : 'text-slate-400']" />
        </template>
        <template #emptyText>
          <div class="py-8 text-center">
            <SearchX class="mx-auto h-9 w-9 text-slate-300" />
            <p class="mt-3 font-bold text-slate-800">Chưa có lịch hẹn phù hợp</p>
            <p class="mt-1 text-sm text-slate-500">Thử thay đổi từ khóa tìm kiếm hoặc đặt một lịch khám mới.</p>
          </div>
        </template>
        <template #bodyCell="{ column, record }">
          <template v-if="column.key === 'id'">
            <span class="font-mono text-xs font-semibold text-[#0F52BA]">{{ record.id }}</span>
          </template>
          <template v-else-if="column.key === 'doctorName'">
            <span class="text-[13px] font-semibold text-slate-900">{{ record.doctorName }}</span>
          </template>
          <template v-else-if="column.key === 'specialtyName'">
            <span class="text-[13px] text-slate-600">{{ record.specialtyName }}</span>
          </template>
          <template v-else-if="column.key === 'dateTime'">
            <div class="flex items-center gap-2 whitespace-nowrap">
              <CalendarClock class="h-3.5 w-3.5 text-slate-400" />
              <span class="text-[13px] font-medium text-slate-700">{{ formatDate(record.appointmentDate) }}</span>
              <span v-if="record.slotTime" class="text-xs text-slate-400">{{ String(record.slotTime).slice(0, 5) }}</span>
            </div>
          </template>
          <template v-else-if="column.key === 'reason'">
            <span class="line-clamp-2 text-[13px] leading-5 text-slate-600" :title="record.reason">{{ record.reason }}</span>
          </template>
          <template v-else-if="column.key === 'status'">
            <ATag :bordered="false" :class="['appointment-status', appointmentStatusClass(record.status)]">{{ record.status }}</ATag>
          </template>
          <template v-else-if="column.key === 'actions'">
            <button
              type="button"
              class="appointment-action-button"
              title="Xem chi tiết lịch hẹn"
              aria-label="Xem chi tiết lịch hẹn"
              @click="openDetail(record)"
            >
              <Eye class="h-4 w-4" />
            </button>
          </template>
          <template v-else>
            <span class="text-slate-700">{{ value(record, String(column.dataIndex)) }}</span>
          </template>
        </template>
      </ATable>
    </div>

    <div v-else class="rounded-2xl border border-slate-200 bg-white shadow-sm">
      <div class="grid gap-3 border-b border-slate-100 p-4 lg:grid-cols-[1fr_auto] lg:items-center">
        <label class="relative block">
          <Search class="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />
          <input v-model="query" class="h-11 w-full rounded-xl border border-slate-200 bg-white pl-10 pr-4 text-sm outline-none transition focus:border-blue-300 focus:ring-4 focus:ring-blue-100" :placeholder="config.placeholder" />
        </label>
        <span class="rounded-lg bg-blue-50 px-3 py-2 text-sm font-bold text-[#003c90]">{{ filteredRows.length }} dòng</span>
      </div>

      <div v-if="filteredRows.length && resource === 'bills'" class="bill-table-shell">
        <ATable
          :columns="billTableColumns"
          :data-source="filteredRows"
          :pagination="billPagination"
          :row-key="billRowKey"
          :scroll="{ x: 920 }"
          size="middle"
          @change="handleBillTableChange"
        >
          <template #customFilterDropdown="{ setSelectedKeys, selectedKeys, confirm, clearFilters, column }">
            <div class="bill-filter">
              <p class="bill-filter-title">Tìm theo {{ String(column.title).toLowerCase() }}</p>
              <AInput
                :value="selectedKeys[0]"
                :placeholder="`Nhập ${String(column.title).toLowerCase()}...`"
                allow-clear
                autofocus
                @change="setSelectedKeys(getFilterKeys($event))"
                @press-enter="confirm()"
              >
                <template #prefix><Search class="h-3.5 w-3.5 text-slate-400" /></template>
              </AInput>
              <div class="bill-filter-actions">
                <AButton size="small" class="bill-filter-reset" @click="clearAppointmentFilter(clearFilters, confirm)">Đặt lại</AButton>
                <AButton type="primary" size="small" class="bill-filter-submit" @click="confirm()">Áp dụng</AButton>
              </div>
            </div>
          </template>
          <template #customFilterIcon="{ filtered }">
            <Search :class="['h-3.5 w-3.5', filtered ? 'text-[#0F52BA]' : 'text-slate-400']" />
          </template>
          <template #emptyText>
            <div class="py-8 text-center">
              <CreditCard class="mx-auto h-9 w-9 text-slate-300" />
              <p class="mt-3 font-bold text-slate-800">Không có viện phí phù hợp</p>
              <p class="mt-1 text-sm text-slate-500">Thử đổi từ khóa tìm kiếm trong từng cột.</p>
            </div>
          </template>
          <template #bodyCell="{ column, record }">
            <template v-if="column.key === 'id'">
              <span class="font-bold text-slate-950">{{ record.id }}</span>
            </template>
            <template v-else-if="column.key === 'appointmentId'">
              <span class="font-mono text-sm font-medium text-slate-600">{{ record.appointmentId }}</span>
            </template>
            <template v-else-if="column.key === 'amount'">
              <span class="whitespace-nowrap text-[15px] font-extrabold text-slate-950">{{ record.amount }}</span>
            </template>
            <template v-else-if="column.key === 'status'">
              <ATag :bordered="false" :class="['bill-status-tag', statusClass(record.status)]">
                <span class="bill-status-dot"></span>
                {{ record.status }}
              </ATag>
            </template>
            <template v-else-if="column.key === 'actions'">
              <button
                v-if="!isPaidBillRow(record)"
                type="button"
                class="bill-pay-button"
                :disabled="actingId === record.id"
                title="Thanh toán viện phí"
                @click="openPayment(record)"
              >
                Thanh toán
              </button>
              <span v-else class="text-xs font-bold text-slate-400">Đã xử lý</span>
            </template>
          </template>
        </ATable>
      </div>

      <div v-else-if="filteredRows.length" class="overflow-x-auto">
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
          Chưa có dữ liệu phù hợp với tài khoản bệnh nhân này.
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
              Hệ thống sẽ ghi nhận yêu cầu thanh toán chuyển khoản sau khi bạn xác nhận đã chuyển tiền.
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
    </div>
  </section>
</template>

<script setup lang="ts">
import { computed, onMounted, onUnmounted, reactive, ref, watch } from 'vue'
import { useRoute } from 'vue-router'
import { Button as AButton, Input as AInput, Table as ATable, Tag as ATag } from 'ant-design-vue'
import { CalendarClock, ChevronLeft, ChevronRight, Copy, CreditCard, Eye, FileHeart, Pill, Save, Search, SearchX, ShieldCheck, UserRound, X } from 'lucide-vue-next'
import BaseButton from '@/components/ui/BaseButton.vue'
import BaseInput from '@/components/ui/BaseInput.vue'
import FullscreenLoader from '@/components/ui/FullscreenLoader.vue'
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
let toastTimer: ReturnType<typeof setTimeout> | null = null
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
const patientId = computed(() => String(currentPatient.value?.id || currentPatient.value?.patientId || ''))
const displayPatientCode = computed(() => patientDisplayCode(currentPatient.value) || formatPatientCode(patientId.value) || 'Chưa liên kết')

watch(() => toast.show, (visible) => {
  if (toastTimer) clearTimeout(toastTimer)
  if (visible) toastTimer = setTimeout(() => { toast.show = false }, 3000)
})

const configs: Record<Resource, { title: string; service: string; description: string; placeholder: string; icon: any; iconClass: string; search: string[]; columns: Column[] }> = {
  appointments: cfg('Lịch hẹn của tôi', '', 'Theo dõi lịch đã đặt, bác sĩ, giờ khám và trạng thái xác nhận.', 'Tìm mã lịch, bác sĩ, chuyên khoa, lý do, trạng thái...', CalendarClock, 'bg-blue-50 text-[#0F52BA]', ['id', 'doctorName', 'specialtyName', 'status', 'reason', 'dateTime'], cols(['id', 'Mã lịch'], ['doctorName', 'Bác sĩ', false, true], ['specialtyName', 'Chuyên khoa'], ['dateTime', 'Ngày giờ hẹn'], ['reason', 'Lý do khám'], ['status', 'Trạng thái', true])),
  records: cfg('Hồ sơ bệnh án', 'Hồ sơ khám bệnh', 'Xem chẩn đoán, triệu chứng và ghi chú bác sĩ sau mỗi lần khám.', 'Tìm chẩn đoán, triệu chứng, ghi chú...', FileHeart, 'bg-indigo-50 text-indigo-700', ['id', 'diagnosis', 'symptoms', 'doctorNotes'], cols(['id', 'Mã BA'], ['diagnosis', 'Chẩn đoán', false, true], ['symptoms', 'Triệu chứng'], ['doctorNotes', 'Ghi chú'], ['createdAt', 'Ngày tạo'])),
  prescriptions: cfg('Đơn thuốc', 'Đơn thuốc đã kê', 'Xem đơn thuốc cũ đã được bác sĩ chốt và gửi sang nhà thuốc.', 'Tìm mã đơn, thuốc, trạng thái...', Pill, 'bg-cyan-50 text-cyan-700', ['id', 'medicine', 'status', 'note'], cols(['id', 'Mã đơn'], ['medicine', 'Thuốc', false, true], ['quantity', 'Số lượng'], ['note', 'Ghi chú'], ['status', 'Trạng thái', true])),
  bills: cfg('Viện phí của tôi', '', 'Xem hóa đơn, số tiền và thực hiện thanh toán viện phí khi cần.', 'Tìm mã hóa đơn, trạng thái...', CreditCard, 'bg-emerald-50 text-emerald-700', ['id', 'amount', 'status'], cols(['id', 'Mã HĐ'], ['appointmentId', 'Lịch hẹn'], ['amount', 'Số tiền', false, true], ['status', 'Trạng thái', true])),
  profile: cfg('Hồ sơ cá nhân', '', 'Thông tin tài khoản và hồ sơ bệnh nhân liên kết.', '', UserRound, 'bg-slate-100 text-slate-700', [], []),
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

const appointmentTableColumns = [
  {
    title: 'Mã lịch',
    dataIndex: 'id',
    key: 'id',
    width: 130,
    customFilterDropdown: true,
    onFilter: appointmentColumnFilter('id'),
  },
  {
    title: 'Bác sĩ',
    dataIndex: 'doctorName',
    key: 'doctorName',
    width: 230,
    customFilterDropdown: true,
    onFilter: appointmentColumnFilter('doctorName'),
    sorter: (a: Row, b: Row) => String(a.doctorName || '').localeCompare(String(b.doctorName || ''), 'vi'),
  },
  {
    title: 'Chuyên khoa',
    dataIndex: 'specialtyName',
    key: 'specialtyName',
    width: 190,
    customFilterDropdown: true,
    onFilter: appointmentColumnFilter('specialtyName'),
    sorter: (a: Row, b: Row) => String(a.specialtyName || '').localeCompare(String(b.specialtyName || ''), 'vi'),
  },
  {
    title: 'Ngày giờ hẹn',
    dataIndex: 'dateTime',
    key: 'dateTime',
    width: 210,
    sorter: (a: Row, b: Row) => appointmentTimestamp(a) - appointmentTimestamp(b),
    defaultSortOrder: 'descend' as const,
  },
  {
    title: 'Lý do khám',
    dataIndex: 'reason',
    key: 'reason',
    minWidth: 220,
    customFilterDropdown: true,
    onFilter: appointmentColumnFilter('reason'),
  },
  {
    title: 'Trạng thái',
    dataIndex: 'status',
    key: 'status',
    width: 150,
    filters: [
      { text: 'Đang chờ', value: 'Đang chờ' },
      { text: 'Đã xác nhận', value: 'Đã xác nhận' },
      { text: 'Đã check-in', value: 'Đã check-in' },
      { text: 'Đang khám', value: 'Đang khám' },
      { text: 'Hoàn tất', value: 'Hoàn tất' },
      { text: 'Đã hủy', value: 'Đã hủy' },
    ],
    filterReset: 'Đặt lại',
    filterConfirm: 'Áp dụng',
    onFilter: (filterValue: string | number | boolean, record: Row) => String(record.status || '') === String(filterValue),
  },
  {
    title: 'Thao t\u00e1c',
    key: 'actions',
    width: 82,
    fixed: 'right' as const,
    align: 'center' as const,
  },
]

const appointmentPagination = computed(() => ({
  current: currentPage.value,
  pageSize: itemsPerPage.value,
  showSizeChanger: true,
  pageSizeOptions: ['10', '20', '50', '100'],
  showLessItems: true,
  showTitle: false,
  responsive: true,
  showTotal: (total: number, range: [number, number]) => `${range[0]}-${range[1]} trong ${total} lịch hẹn`,
  locale: { items_per_page: ' / trang' },
}))

const billTableColumns = [
  {
    title: 'Mã HĐ',
    dataIndex: 'id',
    key: 'id',
    width: 160,
    customFilterDropdown: true,
    onFilter: billColumnFilter('id'),
    sorter: (a: Row, b: Row) => String(a.id || '').localeCompare(String(b.id || ''), 'vi'),
  },
  {
    title: 'Lịch hẹn',
    dataIndex: 'appointmentId',
    key: 'appointmentId',
    width: 180,
    customFilterDropdown: true,
    onFilter: billColumnFilter('appointmentId'),
  },
  {
    title: 'Số tiền',
    dataIndex: 'amount',
    key: 'amount',
    width: 210,
    customFilterDropdown: true,
    onFilter: billColumnFilter('amount'),
    sorter: (a: Row, b: Row) => Number(a.amountValue || 0) - Number(b.amountValue || 0),
  },
  {
    title: 'Trạng thái',
    dataIndex: 'status',
    key: 'status',
    width: 220,
    customFilterDropdown: true,
    onFilter: billColumnFilter('status'),
  },
  {
    title: 'Thao tác',
    key: 'actions',
    width: 160,
    align: 'right' as const,
    fixed: 'right' as const,
  },
]

const billPagination = computed(() => ({
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

function handleAppointmentTableChange(pagination: { current?: number; pageSize?: number }) {
  currentPage.value = pagination.current || 1
  itemsPerPage.value = pagination.pageSize || 10
}

function handleBillTableChange(pagination: { current?: number; pageSize?: number }) {
  currentPage.value = pagination.current || 1
  itemsPerPage.value = pagination.pageSize || 10
}

function appointmentColumnFilter(key: string) {
  return (filterValue: string | number | boolean, record: Row) =>
    normalizeSearchText(record[key]).includes(normalizeSearchText(filterValue))
}

function billColumnFilter(key: string) {
  return (filterValue: string | number | boolean, record: Row) =>
    normalizeSearchText(record[key]).includes(normalizeSearchText(filterValue))
}

function billRowKey(row: Row) {
  return String(row.invoiceId || row.id || row.raw?.invoiceId || row.raw?.id)
}

function isPaidBillRow(row: Row) {
  return String(row.status).toLowerCase() === 'paid' || String(row.status).toLowerCase().includes('đã thanh toán')
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

function getFilterKeys(event: Event) {
  const filterValue = (event.target as HTMLInputElement)?.value || ''
  return filterValue ? [filterValue] : []
}

function clearAppointmentFilter(clearFilters: (() => void) | undefined, confirm: () => void) {
  clearFilters?.()
  confirm()
}

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

const detailTitle = computed(() => {
  if (resource.value === 'appointments') return 'Chi tiết lịch hẹn'
  return resource.value === 'records' ? 'Chi tiết bệnh án' : 'Chi tiết đơn thuốc'
})
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
  if (resource.value === 'appointments') {
    return [
      ['Mã lịch', row.id || ''],
      ['Bác sĩ', row.doctorName || 'Chưa phân công'],
      ['Chuyên khoa', row.specialtyName || 'Chưa cập nhật'],
      ['Ngày giờ hẹn', row.dateTime || formatAppointmentDateTime(row.appointmentDate, row.slotTime)],
      ['Lý do khám', row.reason || 'Chưa ghi nhận'],
      ['Trạng thái', row.status || 'Chưa cập nhật'],
    ]
  }
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
  loading.value = true
  error.value = ''
  note.value = ''
  try {
    await resolvePatient()
    syncProfileForm()
    if (resource.value === 'profile') return
    if (resource.value === 'appointments') {
      const id = patientId.value
      rows.value = id
        ? uniqueRows((await appointmentApi.getAppointmentsByPatient(id).catch(() => [] as Appointment[])).map(mapAppointment))
          .sort((a, b) => appointmentTimestamp(b) - appointmentTimestamp(a))
        : []
      note.value = ''
    }
    if (resource.value === 'records') {
      const records = await getHistory().then((data) => data.medicalRecords)
      rows.value = records.map(mapRecord)
      note.value = rows.value.length ? 'Đã tải hồ sơ bệnh án của bạn.' : 'Chưa có bệnh án cho bệnh nhân này.'
      showLoadToast('Hồ sơ bệnh án', rows.value.length, 'Bệnh án sẽ xuất hiện sau khi bác sĩ hoàn tất lượt khám.')
    }
    if (resource.value === 'prescriptions') {
      const [n2Prescriptions, n3Prescriptions] = await Promise.all([
        getHistory().then((data) => data.prescriptions || []),
        patientId.value
          ? billingApi.getPrescriptions(patientId.value).catch((err) => {
          if ((err as any)?.response?.status === 404) return [] as Prescription[]
          throw err
        })
          : Promise.resolve([] as Prescription[]),
      ])
      const combined = [...n2Prescriptions, ...n3Prescriptions]
      const seen = new Set<string>()
      const uniquePrescriptions = combined.filter((p) => {
        const id = p.prescriptionId || p.id || p.prescriptionCode
        if (!id || seen.has(String(id))) return false
        seen.add(String(id))
        return true
      })
      rows.value = uniquePrescriptions.map(mapPrescription)
      note.value = rows.value.length ? 'Đã tải đơn thuốc của bạn.' : 'Chưa có đơn thuốc cho bệnh nhân này.'
      showLoadToast('Đơn thuốc', rows.value.length, 'Đơn thuốc sẽ xuất hiện sau khi bác sĩ chốt đơn.')
    }
    if (resource.value === 'bills') {
      rows.value = patientId.value
        ? uniqueRows((await billingApi.getInvoices(patientId.value)).map(mapInvoice))
        : []
      note.value = rows.value.length ? 'Đã tải viện phí của bạn.' : 'Chưa có viện phí cho bệnh nhân này.'
      showLoadToast('Viện phí', rows.value.length, 'Nếu đã khám xong, liên hệ quầy thu ngân hoặc kiểm tra lại sau.')
    }
  } catch (apiError) {
    error.value = getApiErrorMessage(apiError)
    showToast('Không tải được dữ liệu', `${error.value} Kiểm tra lại mã bệnh nhân hoặc thử sang Hồ sơ cá nhân.`, 'error')
    rows.value = []
  } finally {
    loading.value = false
  }
}

async function resolvePatient() {
  if (currentPatient.value) return currentPatient.value
  currentPatient.value = await medicalRecordApi.getCurrentPatient()
  if (currentPatient.value && authStore.user) authStore.user.patientId = String(currentPatient.value.id || currentPatient.value.patientId || '')
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
    if (!id) throw new Error('Token chưa có PatientId hợp lệ. Vui lòng đăng xuất rồi đăng nhập lại.')
    currentPatient.value = await medicalRecordApi.updatePatient(id, payload)
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
  history.value = await medicalRecordApi.getCurrentPatientClinicalTimeline().catch((error) => {
    if ((error as any)?.response?.status === 404) return { visits: [], medicalRecords: [], prescriptions: [] } as PatientMedicalHistory
    throw error
  })
  return history.value
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

function mapAppointment(item: Appointment & Record<string, any>): Row {
  const appointmentId = getAny(item, 'appointmentId', 'AppointmentId', 'id', 'Id')
  const appointmentDate = getAny(item, 'appointmentDate', 'AppointmentDate')
  const slotTime = getAny(item, 'slotTime', 'SlotTime')
  const doctorName = cleanDisplayText(getAny(item, 'doctorName', 'DoctorName'))
  const specialtyName = cleanDisplayText(getAny(item, 'specialtyName', 'SpecialtyName'))
  const reason = cleanDisplayText(getAny(item, 'reason', 'Reason'))
  const status = getAny(item, 'status', 'Status')

  return {
    id: appointmentDisplayCode(item, appointmentId),
    appointmentId,
    doctorName: doctorName || 'Chưa phân công bác sĩ',
    specialtyName: specialtyName || 'Chưa cập nhật',
    appointmentDate,
    slotTime,
    dateTime: formatAppointmentDateTime(appointmentDate, slotTime),
    reason: reason || 'Chưa ghi nhận',
    status: statusLabel(status),
    raw: item,
  }
}

function appointmentDisplayCode(item: Record<string, any>, appointmentId: unknown) {
  const code = cleanDisplayText(getAny(item, 'appointmentCode', 'AppointmentCode', 'appointmentIdCode', 'AppointmentIdCode'))
  if (code) return code
  const numericId = Number(appointmentId)
  return Number.isFinite(numericId) && numericId > 0 ? `LH${String(numericId).padStart(3, '0')}` : 'Chưa cập nhật'
}

function formatAppointmentDateTime(dateValue: unknown, timeValue: unknown) {
  const date = formatDate(String(dateValue || ''))
  const time = String(timeValue || '').trim().slice(0, 5)
  if (date === 'Chưa cập nhật') return time || date
  return time ? `${date} lúc ${time}` : date
}

function appointmentTimestamp(row: Row) {
  const date = String(row.appointmentDate || '').slice(0, 10)
  const time = String(row.slotTime || '00:00').slice(0, 8)
  const timestamp = new Date(`${date}T${time}`).getTime()
  return Number.isNaN(timestamp) ? 0 : timestamp
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
  if (resource.value === 'appointments') {
    showToast('Đang xem chi tiết lịch hẹn', 'Kiểm tra bác sĩ, chuyên khoa, thời gian và trạng thái lịch hẹn.', 'success')
    return
  }
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
    showToast('Thanh toán thành công', 'Hệ thống đã ghi nhận thanh toán chuyển khoản ngân hàng.', 'success')
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

function cleanDisplayText(value: unknown) {
  const text = String(value ?? '').trim()
  return ['null', 'undefined', '-', 'n/a'].includes(text.toLowerCase()) ? '' : text
}

function getAny(source: unknown, ...keys: string[]) {
  const data = source as Record<string, any> | null | undefined
  if (!data) return undefined
  for (const key of keys) {
    if (data[key] !== undefined && data[key] !== null) return data[key]
  }
  return undefined
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
  const dateOnly = value.match(/^(\d{4})-(\d{2})-(\d{2})/)
  if (dateOnly) return `${Number(dateOnly[3])}/${Number(dateOnly[2])}/${dateOnly[1]}`
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

function appointmentStatusClass(status?: string) {
  const valueText = String(status || '').toLowerCase()
  if (valueText.includes('cancel') || valueText.includes('hủy')) return 'bg-rose-50 text-rose-600'
  if (valueText.includes('completed') || valueText.includes('done') || valueText.includes('hoàn tất')) return 'bg-emerald-50 text-emerald-700'
  if (valueText.includes('progress') || valueText.includes('đang khám')) return 'bg-indigo-50 text-indigo-600'
  if (valueText.includes('checked') || valueText.includes('check-in')) return 'bg-cyan-50 text-cyan-700'
  if (valueText.includes('confirmed') || valueText.includes('xác nhận')) return 'bg-blue-50 text-blue-700'
  if (valueText.includes('pending') || valueText.includes('waiting') || valueText.includes('chờ')) return 'bg-amber-50 text-amber-700'
  return 'bg-slate-100 text-slate-600'
}

function statusLabel(status?: string) {
  const valueText = String(status || '').toLowerCase()
  if (valueText.includes('checked')) return 'Đã check-in'
  if (valueText.includes('progress')) return 'Đang khám'
  if (valueText.includes('confirmed')) return 'Đã xác nhận'
  if (valueText.includes('completed') || valueText.includes('done')) return 'Hoàn tất'
  if (valueText.includes('noshow')) return 'Không đến khám'
  if (valueText.includes('expired')) return 'Đã quá hạn'
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

<style scoped>
.appointment-table-shell {
  overflow: hidden;
  border: 1px solid #e5eaf1;
  border-radius: 8px;
  background: #ffffff;
  box-shadow: 0 10px 30px rgb(15 23 42 / 0.035);
}

.appointment-filter {
  width: 270px;
  padding: 16px;
  border: 1px solid #e8edf3;
  border-radius: 10px;
  background: #ffffff;
  box-shadow: 0 14px 36px rgb(15 23 42 / 0.1);
}

.appointment-filter-title {
  margin-bottom: 10px;
  color: #64748b;
  font-size: 11px;
  font-weight: 700;
  line-height: 16px;
}

.appointment-filter :deep(.ant-input-affix-wrapper),
.appointment-filter :deep(.ant-input) {
  font-size: 12px;
}

.appointment-filter :deep(.ant-input-affix-wrapper) {
  height: 38px;
  padding-inline: 11px;
  border-color: #dfe5ec;
  border-radius: 8px;
  box-shadow: none;
}

.appointment-filter :deep(.ant-input-affix-wrapper:hover),
.appointment-filter :deep(.ant-input-affix-wrapper-focused) {
  border-color: #93b4e6;
  box-shadow: 0 0 0 3px rgb(15 82 186 / 0.08);
}

.appointment-filter-actions {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 8px;
  margin-top: 12px;
}

.appointment-filter :deep(.ant-btn) {
  height: 34px;
  border-radius: 8px;
  font-size: 12px;
  font-weight: 650;
}

.appointment-filter :deep(.ant-btn-primary) {
  background: #0f52ba;
  box-shadow: none;
}

.appointment-filter :deep(.ant-btn-primary:hover) {
  background: #003c90;
}

:global(.ant-table-filter-dropdown) {
  border-radius: 10px;
  box-shadow: none;
}

:global(.ant-table-filter-dropdown .appointment-filter) {
  margin: -4px;
}

:global(.ant-table-filter-dropdown .ant-dropdown-menu-title-content),
:global(.ant-table-filter-dropdown .ant-checkbox-wrapper) {
  font-size: 12px;
  font-weight: 400;
}

:global(.ant-table-filter-dropdown-btns .ant-btn) {
  font-size: 12px;
  font-weight: 500;
}

:deep(.appointment-table-shell .ant-table) {
  color: #334155;
  font-size: 13px;
}

:deep(.appointment-table-shell .ant-table-thead > tr > th) {
  height: 44px;
  padding-block: 10px;
  border-bottom: 1px solid #e8edf3;
  background: #f9fbfd;
  color: #64748b;
  font-size: 11.5px;
  font-weight: 650;
}

:deep(.appointment-table-shell .ant-table-tbody > tr > td) {
  height: 52px;
  padding-block: 11px;
  border-bottom-color: #eef2f7;
}

:deep(.appointment-table-shell .ant-table-tbody > tr:last-child > td) {
  border-bottom: 0;
}

:deep(.appointment-table-shell .ant-table-tbody > tr:hover > td) {
  background: #f7faff;
}

:deep(.appointment-table-shell .ant-table-tbody > tr > td.ant-table-cell-fix-right),
:deep(.appointment-table-shell .ant-table-thead > tr > th.ant-table-cell-fix-right) {
  background: #ffffff;
}

:deep(.appointment-table-shell .ant-table-tbody > tr:hover > td.ant-table-cell-fix-right) {
  background: #f7faff;
}

:deep(.appointment-table-shell .ant-table-cell-fix-right-first::after) {
  box-shadow: inset -8px 0 8px -8px rgb(15 23 42 / 0.16);
}

:deep(.appointment-table-shell .ant-table-column-sorter),
:deep(.appointment-table-shell .ant-table-filter-trigger) {
  color: #94a3b8;
  opacity: 0.45;
  transition: color 160ms ease, opacity 160ms ease;
}

:deep(.appointment-table-shell th:hover .ant-table-column-sorter),
:deep(.appointment-table-shell th:hover .ant-table-filter-trigger),
:deep(.appointment-table-shell .ant-table-filter-trigger.active) {
  opacity: 1;
}

:deep(.appointment-table-shell .ant-table-filter-trigger:hover),
:deep(.appointment-table-shell .ant-table-filter-trigger.active),
:deep(.appointment-table-shell .ant-table-column-sorter-up.active),
:deep(.appointment-table-shell .ant-table-column-sorter-down.active) {
  color: #0f52ba;
}

:deep(.appointment-table-shell .ant-pagination) {
  min-height: 58px;
  margin: 0;
  padding: 13px 16px;
  border-top: 1px solid #eef2f7;
  background: #fbfcfe;
  gap: 4px;
}

:deep(.appointment-table-shell .ant-pagination-total-text) {
  margin-right: auto;
  color: #64748b;
  font-size: 12px;
  line-height: 30px;
}

:deep(.appointment-table-shell .ant-pagination-item),
:deep(.appointment-table-shell .ant-pagination-prev .ant-pagination-item-link),
:deep(.appointment-table-shell .ant-pagination-next .ant-pagination-item-link) {
  min-width: 30px;
  height: 30px;
  margin-inline-end: 0;
  border-color: transparent;
  border-radius: 8px;
  background: transparent;
  line-height: 28px;
  transition: background 160ms ease, color 160ms ease;
}

:deep(.appointment-table-shell .ant-pagination-item:hover),
:deep(.appointment-table-shell .ant-pagination-prev:not(.ant-pagination-disabled) .ant-pagination-item-link:hover),
:deep(.appointment-table-shell .ant-pagination-next:not(.ant-pagination-disabled) .ant-pagination-item-link:hover) {
  border-color: transparent;
  background: #eaf2ff;
  color: #0f52ba;
}

:deep(.appointment-table-shell .ant-pagination-item-active) {
  border-color: transparent;
  background: #0f52ba;
  box-shadow: 0 4px 12px rgb(15 82 186 / 0.2);
}

:deep(.appointment-table-shell .ant-pagination-item-active:hover) {
  border-color: transparent;
  background: #003c90;
}

:deep(.appointment-table-shell .ant-pagination-item-active a),
:deep(.appointment-table-shell .ant-pagination-item-active:hover a),
:deep(.appointment-table-shell .ant-pagination-item-active:focus a) {
  color: #ffffff;
}

:deep(.appointment-table-shell .ant-pagination-item:focus-visible),
:deep(.appointment-table-shell .ant-pagination-prev .ant-pagination-item-link:focus-visible),
:deep(.appointment-table-shell .ant-pagination-next .ant-pagination-item-link:focus-visible) {
  outline: 2px solid #bfdbfe;
  outline-offset: 2px;
}

:deep(.appointment-action-button) {
  display: inline-flex;
  width: 32px;
  height: 32px;
  align-items: center;
  justify-content: center;
  border: 1px solid #e2e8f0;
  border-radius: 7px;
  background: #f8fafc;
  color: #64748b;
  transition: border-color 160ms ease, background 160ms ease, color 160ms ease, transform 160ms ease;
}

:deep(.appointment-action-button:hover) {
  border-color: #cbd5e1;
  background: #f1f5f9;
  color: #334155;
  transform: translateY(-1px);
}

:deep(.appointment-action-button:focus-visible) {
  outline: 2px solid #bfdbfe;
  outline-offset: 2px;
}

:deep(.appointment-table-shell .ant-pagination-options) {
  margin-inline-start: 8px;
}

:deep(.appointment-table-shell .ant-pagination-options .ant-select-selector) {
  height: 30px;
  border-color: #e2e8f0;
  border-radius: 8px;
  background: #ffffff;
  box-shadow: none;
  font-size: 12px;
}

:deep(.appointment-table-shell .ant-pagination-options .ant-select-selection-item) {
  line-height: 28px;
}

:deep(.appointment-status) {
  margin: 0;
  border-radius: 999px;
  padding: 2px 9px;
  font-size: 11px;
  font-weight: 500;
  line-height: 18px;
}

.bill-table-shell {
  overflow: hidden;
}

.bill-filter {
  width: 260px;
  padding: 12px;
}

.bill-filter-title {
  color: #475569;
  font-size: 12px;
  font-weight: 800;
  margin: 0 0 8px;
}

.bill-filter-actions {
  display: flex;
  gap: 8px;
  justify-content: flex-end;
  margin-top: 10px;
}

.bill-filter-reset {
  border-color: #e2e8f0;
  color: #64748b;
  font-weight: 700;
}

.bill-filter-submit {
  background: #0F52BA;
  border-color: #0F52BA;
  font-weight: 700;
}

:global(.ant-table-filter-dropdown .bill-filter) {
  margin: -4px;
}

.bill-table-shell :deep(.ant-table) {
  color: #334155;
  font-size: 14px;
}

.bill-table-shell :deep(.ant-table-thead > tr > th) {
  background: #f8fafc;
  border-bottom: 1px solid #e2e8f0;
  color: #64748b;
  font-size: 12px;
  font-weight: 800;
  letter-spacing: 0;
  padding: 16px 20px;
  text-transform: uppercase;
}

.bill-table-shell :deep(.ant-table-tbody > tr > td) {
  border-bottom: 1px solid #f1f5f9;
  padding: 18px 20px;
  vertical-align: middle;
}

.bill-table-shell :deep(.ant-table-tbody > tr:hover > td) {
  background: #f8fafc;
}

.bill-table-shell :deep(.ant-table-cell-fix-right) {
  background: #fff;
}

.bill-table-shell :deep(.ant-table-tbody > tr:hover > .ant-table-cell-fix-right) {
  background: #f8fafc;
}

.bill-table-shell :deep(.ant-pagination) {
  border-top: 1px solid #f1f5f9;
  margin: 0;
  padding: 16px;
}

.bill-status-tag {
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

.bill-status-dot {
  background: currentColor;
  border-radius: 999px;
  height: 7px;
  width: 7px;
}

.bill-pay-button {
  align-items: center;
  background: #0F52BA;
  border: 1px solid #0F52BA;
  border-radius: 999px;
  color: #fff;
  display: inline-flex;
  font-size: 13px;
  font-weight: 800;
  height: 36px;
  justify-content: center;
  padding: 0 14px;
  transition: background .2s, border-color .2s, opacity .2s;
}

.bill-pay-button:hover:not(:disabled) {
  background: #003c90;
  border-color: #003c90;
}

.bill-pay-button:disabled {
  cursor: not-allowed;
  opacity: .6;
}

@media (max-width: 640px) {
  :deep(.appointment-table-shell .ant-pagination) {
    justify-content: center;
  }

  :deep(.appointment-table-shell .ant-pagination-total-text) {
    display: none;
  }
}
</style>
