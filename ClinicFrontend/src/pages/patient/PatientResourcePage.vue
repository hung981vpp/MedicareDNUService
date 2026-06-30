<template>
  <section class="profile-page min-h-screen bg-[#F8FAFC] py-2 sm:py-3">
    <FullscreenLoader :show="loading" />

    <div class="mx-auto max-w-none space-y-6 px-4 sm:px-6 lg:px-8">
      <!-- NON-PROFILE PAGES: original header -->
      <header v-if="resource !== 'profile'" class="px-1">
        <h1 class="text-[1.75rem] font-bold tracking-normal text-slate-950">{{ config.title }}</h1>
        <p class="mt-1.5 text-[13px] font-medium leading-5 text-slate-500">{{ config.description }}</p>
      </header>

    <div v-if="resource === 'records'" class="grid gap-4 sm:grid-cols-3">
      <div v-for="metric in metrics" :key="metric.label" class="rounded-2xl border border-slate-200 bg-white p-4 shadow-sm">
        <p class="text-xs font-bold uppercase tracking-wide text-slate-400">{{ metric.label }}</p>
        <p class="mt-2 text-2xl font-bold text-slate-950">{{ metric.value }}</p>
        <p class="mt-1 text-sm text-slate-500">{{ metric.note }}</p>
      </div>
    </div>

    <div v-if="note && resource === 'profile'" class="profile-note-banner">
      <CircleAlert class="h-4 w-4 shrink-0 text-[#2563EB]" />
      <span>{{ note }}</span>
    </div>
    <div v-if="error" class="rounded-[18px] border border-amber-200 bg-amber-50 p-4 text-sm text-amber-800">{{ error }}</div>

    <!-- ======================== PROFILE PAGE REDESIGN ======================== -->
    <template v-if="resource === 'profile'">

      <!-- PAGE HEADER -->
      <header class="profile-page-header">
        <div>
          <h1 class="profile-page-title">Hồ sơ cá nhân</h1>
          <p class="profile-page-subtitle">Quản lý thông tin tài khoản và hồ sơ bệnh nhân của bạn.</p>
        </div>
        <button type="button" class="profile-edit-btn" @click="profileEditMode = !profileEditMode">
          <Pencil class="h-4 w-4" />
          {{ profileEditMode ? 'Hủy chỉnh sửa' : 'Chỉnh sửa hồ sơ' }}
        </button>
      </header>

      <!-- PROFILE SUMMARY CARD -->
      <div class="profile-summary-card">
        <div class="profile-summary-left">
          <div class="profile-avatar-wrapper">
            <div class="profile-avatar">
              <UserRound class="h-9 w-9" />
            </div>
            <span class="profile-avatar-badge">
              <CircleCheck class="h-4 w-4" />
            </span>
          </div>
          <div class="profile-identity">
            <p class="profile-identity-label">HỒ SƠ BỆNH NHÂN</p>
            <h2 class="profile-identity-name">{{ profileForm.fullName || authStore.user?.username || 'Bệnh nhân' }}</h2>
            <div class="profile-identity-meta">
              <span class="profile-patient-badge">{{ displayPatientCode }}</span>
              <span class="profile-updated-text">Cập nhật lần cuối: {{ formatDate(currentPatient?.updatedAt || currentPatient?.createdAt) }}</span>
            </div>
          </div>
        </div>
        <div class="profile-summary-divider"></div>
        <div class="profile-stats">
          <div class="profile-stat">
            <div class="profile-stat-icon">
              <CalendarDays class="h-4 w-4" />
            </div>
            <div>
              <p class="profile-stat-label">Tuổi</p>
              <p class="profile-stat-value">{{ profileAge }}</p>
              <p class="profile-stat-sub">{{ profileForm.dateOfBirth ? formatDate(profileForm.dateOfBirth) : '' }}</p>
            </div>
          </div>
          <div class="profile-stat-separator"></div>
          <div class="profile-stat">
            <div class="profile-stat-icon profile-stat-icon--red">
              <Droplet class="h-4 w-4" />
            </div>
            <div>
              <p class="profile-stat-label">Nhóm máu</p>
              <p class="profile-stat-value">{{ profileForm.bloodType || '—' }}</p>
              <p class="profile-stat-sub">{{ profileForm.bloodType ? `Rh(${profileForm.bloodType.includes('+') ? '+' : profileForm.bloodType.includes('-') ? '-' : '?'})` : '' }}</p>
            </div>
          </div>
          <div class="profile-stat-separator"></div>
          <div class="profile-stat">
            <div class="profile-stat-icon profile-stat-icon--purple">
              <ShieldCheck class="h-4 w-4" />
            </div>
            <div>
              <p class="profile-stat-label">Giới tính</p>
              <p class="profile-stat-value">{{ profileForm.gender || '—' }}</p>
            </div>
          </div>
          <div class="profile-stat-separator"></div>
          <div class="profile-stat">
            <div class="profile-stat-icon profile-stat-icon--green">
              <HeartPulse class="h-4 w-4" />
            </div>
            <div>
              <p class="profile-stat-label">Tình trạng</p>
              <p class="profile-status-pill">Ổn định</p>
            </div>
          </div>
        </div>
      </div>

      <!-- MAIN CONTENT: Two-column layout -->
      <div class="profile-content-grid">

        <!-- LEFT COLUMN -->
        <form class="profile-left-col" @submit.prevent="saveProfile">

          <!-- Card 1: Thông tin liên hệ -->
          <section class="profile-card">
            <div class="profile-card-header">
              <div class="profile-card-icon">
                <Mail class="h-[18px] w-[18px]" />
              </div>
              <h3 class="profile-card-title">Thông tin liên hệ</h3>
            </div>
            <div class="profile-card-body">
              <div class="profile-form-grid-2">
                <div class="profile-field">
                  <label class="profile-field-label">Họ và tên <span class="text-rose-500">*</span></label>
                  <div class="profile-input-wrap">
                    <UserRound class="profile-input-icon" />
                    <input v-model="profileForm.fullName" required :disabled="!profileEditMode" :class="['profile-input', !profileEditMode && 'profile-input--disabled']" placeholder="Nhập họ và tên" />
                  </div>
                </div>
                <div class="profile-field">
                  <label class="profile-field-label">Tên đăng nhập</label>
                  <div class="profile-input-wrap">
                    <AtSign class="profile-input-icon" />
                    <input :value="authStore.user?.username || ''" disabled class="profile-input profile-input--with-action profile-input--disabled" />
                    <button type="button" class="profile-copy-btn" title="Copy tên đăng nhập" @click="copyText(authStore.user?.username || '')">
                      <Copy class="h-3.5 w-3.5" />
                    </button>
                  </div>
                </div>
                <div class="profile-field">
                  <label class="profile-field-label">Email <span class="text-rose-500">*</span></label>
                  <div class="profile-input-wrap">
                    <Mail class="profile-input-icon" />
                    <input v-model="profileForm.email" type="email" required :disabled="!profileEditMode" :class="['profile-input', !profileEditMode && 'profile-input--disabled']" placeholder="Nhập email" />
                  </div>
                </div>
                <div class="profile-field">
                  <label class="profile-field-label">Số điện thoại</label>
                  <div class="profile-input-wrap">
                    <Phone class="profile-input-icon" />
                    <input v-model="profileForm.phoneNumber" :disabled="!profileEditMode" :class="['profile-input profile-input--with-action', !profileEditMode && 'profile-input--disabled']" placeholder="Nhập số điện thoại" />
                    <button type="button" class="profile-copy-btn" title="Copy số điện thoại" @click="copyText(profileForm.phoneNumber)">
                      <Copy class="h-3.5 w-3.5" />
                    </button>
                  </div>
                </div>
              </div>
            </div>
          </section>

          <!-- Card 2: Thông tin cá nhân -->
          <section class="profile-card">
            <div class="profile-card-header">
              <div class="profile-card-icon profile-card-icon--indigo">
                <IdCard class="h-[18px] w-[18px]" />
              </div>
              <h3 class="profile-card-title">Thông tin cá nhân</h3>
            </div>
            <div class="profile-card-body">
              <div class="profile-form-grid-4">
                <div class="profile-field">
                  <label class="profile-field-label">CCCD</label>
                  <div class="profile-input-wrap">
                    <IdCard class="profile-input-icon" />
                    <input v-model="profileForm.citizenId" inputmode="numeric" maxlength="12" :disabled="!profileEditMode" :class="['profile-input profile-input--with-action profile-input--mono', !profileEditMode && 'profile-input--disabled']" @input="handleCitizenInput(($event.target as HTMLInputElement).value)" />
                    <button type="button" class="profile-copy-btn" title="Copy CCCD" @click="copyText(profileForm.citizenId)">
                      <Copy class="h-3.5 w-3.5" />
                    </button>
                  </div>
                </div>
                <div class="profile-field">
                  <label class="profile-field-label">Ngày sinh</label>
                  <div class="profile-input-wrap">
                    <CalendarDays class="profile-input-icon" />
                    <input v-model="profileForm.dateOfBirth" type="date" :disabled="!profileEditMode" :class="['profile-input', !profileEditMode && 'profile-input--disabled']" />
                  </div>
                </div>
                <div class="profile-field">
                  <label class="profile-field-label">Giới tính</label>
                  <div class="profile-input-wrap">
                    <VenetianMask class="profile-input-icon" />
                    <select v-model="profileForm.gender" :disabled="!profileEditMode" :class="['profile-input', !profileEditMode && 'profile-input--disabled']">
                      <option value="">Chưa chọn</option>
                      <option value="Nam">Nam</option>
                      <option value="Nữ">Nữ</option>
                      <option value="Khác">Khác</option>
                    </select>
                  </div>
                </div>
                <div class="profile-field">
                  <label class="profile-field-label">Nhóm máu</label>
                  <div class="profile-input-wrap">
                    <Droplet class="profile-input-icon" />
                    <select v-model="profileForm.bloodType" :disabled="!profileEditMode" :class="['profile-input', !profileEditMode && 'profile-input--disabled']">
                      <option value="">Chưa rõ</option>
                      <option v-for="type in bloodTypes" :key="type" :value="type">{{ type }}</option>
                    </select>
                  </div>
                </div>
              </div>
            </div>
          </section>

          <!-- Card 3: Thông tin y tế -->
          <section class="profile-card">
            <div class="profile-card-header">
              <div class="profile-card-icon profile-card-icon--rose">
                <HeartPulse class="h-[18px] w-[18px]" />
              </div>
              <h3 class="profile-card-title">Thông tin y tế</h3>
            </div>
            <div class="profile-card-body">
              <div class="profile-form-grid-medical">
                <div class="profile-field profile-field--wide">
                  <label class="profile-field-label">
                    <MapPin class="inline h-3.5 w-3.5 text-slate-400" />
                    Địa chỉ
                  </label>
                  <textarea v-model="profileForm.address" rows="2" :disabled="!profileEditMode" :class="['profile-textarea', !profileEditMode && 'profile-textarea--disabled']" placeholder="Nhập địa chỉ hiện tại"></textarea>
                </div>
                <div class="profile-field">
                  <label class="profile-field-label">
                    <ShieldAlert class="inline h-3.5 w-3.5 text-slate-400" />
                    Dị ứng
                  </label>
                  <textarea v-model="profileForm.allergyNote" rows="2" :disabled="!profileEditMode" :class="['profile-textarea', !profileEditMode && 'profile-textarea--disabled']" placeholder="VD: Không có, dị ứng penicillin..."></textarea>
                </div>
                <div class="profile-field profile-field--full">
                  <label class="profile-field-label">
                    <ClipboardList class="inline h-3.5 w-3.5 text-slate-400" />
                    Tiền sử bệnh
                  </label>
                  <textarea v-model="profileForm.medicalHistory" rows="3" :disabled="!profileEditMode" :class="['profile-textarea', !profileEditMode && 'profile-textarea--disabled']" placeholder="VD: Tăng huyết áp, tiểu đường..."></textarea>
                </div>
              </div>
            </div>

            <!-- Save button -->
            <div v-if="profileEditMode" class="profile-card-footer">
              <BaseButton type="submit" :loading="profileSaving">
                <template #icon><Save class="h-4 w-4" /></template>
                Lưu hồ sơ
              </BaseButton>
            </div>
          </section>

        </form>

        <!-- RIGHT COLUMN -->
        <div class="profile-right-col">

          <!-- Card: Bảo mật tài khoản -->
          <div class="profile-card">
            <div class="profile-card-header">
              <div class="profile-card-icon profile-card-icon--slate">
                <ShieldCheck class="h-[18px] w-[18px]" />
              </div>
              <h3 class="profile-card-title">Bảo mật tài khoản</h3>
            </div>
            <div class="profile-card-body">
              <div class="profile-security-visual">
                <div class="profile-security-shield">
                  <ShieldCheck class="h-10 w-10 text-[#2563EB]" />
                </div>
                <p class="profile-security-text">Tài khoản của bạn được bảo vệ tốt</p>
              </div>
              <div class="profile-security-info">
                <p class="profile-security-label">Lần đăng nhập gần nhất</p>
                <div class="profile-security-detail">
                  <span class="profile-security-dot"></span>
                  <span>{{ lastLoginText }}</span>
                </div>
              </div>
              <button type="button" class="profile-security-btn" @click="profilePasswordMode = !profilePasswordMode">
                <KeyRound class="h-4 w-4" />
                Đổi mật khẩu
              </button>
            </div>
          </div>

          <!-- Password form (shown when toggled) -->
          <div v-if="profilePasswordMode" class="profile-card">
            <div class="profile-card-header">
              <div class="profile-card-icon profile-card-icon--amber">
                <KeyRound class="h-[18px] w-[18px]" />
              </div>
              <h3 class="profile-card-title">Đổi mật khẩu</h3>
            </div>
            <form class="profile-card-body" @submit.prevent="changePassword">
              <div class="profile-password-fields">
                <div class="profile-field">
                  <label class="profile-field-label">Mật khẩu hiện tại <span class="text-rose-500">*</span></label>
                  <div class="profile-input-wrap">
                    <KeyRound class="profile-input-icon" />
                    <input v-model="passwordForm.currentPassword" type="password" required autocomplete="current-password" class="profile-input" />
                  </div>
                </div>
                <div class="profile-field">
                  <label class="profile-field-label">Mật khẩu mới <span class="text-rose-500">*</span></label>
                  <div class="profile-input-wrap">
                    <KeyRound class="profile-input-icon" />
                    <input v-model="passwordForm.newPassword" type="password" required minlength="6" autocomplete="new-password" class="profile-input" />
                  </div>
                </div>
                <div class="profile-field">
                  <label class="profile-field-label">Xác nhận mật khẩu <span class="text-rose-500">*</span></label>
                  <div class="profile-input-wrap">
                    <KeyRound class="profile-input-icon" />
                    <input v-model="passwordForm.confirmPassword" type="password" required minlength="6" autocomplete="new-password" class="profile-input" />
                  </div>
                </div>
              </div>
              <p class="mt-3 text-[11px] leading-4 text-slate-400">Mật khẩu mới tối thiểu 6 ký tự, khác mật khẩu hiện tại.</p>
              <div class="mt-4">
                <BaseButton type="submit" :loading="passwordSaving" size="sm">
                  <template #icon><KeyRound class="h-4 w-4" /></template>
                  Đổi mật khẩu
                </BaseButton>
              </div>
            </form>
          </div>

          <!-- Card: Lưu ý sức khỏe -->
          <div class="profile-health-note-card">
            <div class="profile-health-note-header">
              <HeartPulse class="h-5 w-5 text-emerald-600" />
              <h3 class="profile-health-note-title">Lưu ý sức khỏe</h3>
            </div>
            <div class="profile-health-note-body">
              <div class="profile-health-note-illustration">
                <Stethoscope class="h-12 w-12 text-emerald-400/60" />
              </div>
              <p class="profile-health-note-text">
                Hãy cập nhật thông tin thường xuyên để chúng tôi hỗ trợ bạn tốt hơn!
              </p>
            </div>
          </div>

          <!-- Card: Tải hồ sơ -->
          <div class="profile-card">
            <div class="profile-card-header">
              <div class="profile-card-icon profile-card-icon--sky">
                <CloudDownload class="h-[18px] w-[18px]" />
              </div>
              <h3 class="profile-card-title">Tải hồ sơ của bạn</h3>
            </div>
            <div class="profile-card-body">
              <div class="profile-download-visual">
                <CloudDownload class="h-10 w-10 text-sky-400/70" />
              </div>
              <p class="profile-download-text">Tạo bản in PDF khổ A4 gồm thông tin cá nhân, lịch hẹn, bệnh án, đơn thuốc và viện phí.</p>
              <button type="button" class="profile-download-btn" :disabled="profileDownloading" @click="printMedicalProfilePdf">
                <Download v-if="!profileDownloading" class="h-4 w-4" />
                <span v-else class="profile-download-spinner"></span>
                {{ profileDownloading ? 'Đang tạo bản in...' : 'In / tải PDF' }}
              </button>
            </div>
          </div>

        </div>
      </div>

    </template>

    <div v-else-if="resource === 'appointments'" class="appointment-table-shell">
      <ATable
        :columns="appointmentTableColumns"
        :data-source="rows"
        :pagination="appointmentPagination"
        row-key="id"
        size="middle"
        table-layout="fixed"
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
        <template #customFilterIcon="{ filtered, column }">
          <CheckSquare v-if="column.key === 'status'" :class="['h-3.5 w-3.5', filtered ? 'text-[#0F52BA]' : 'text-slate-400']" />
          <Search v-else :class="['h-3.5 w-3.5', filtered ? 'text-[#0F52BA]' : 'text-slate-400']" />
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
          <template v-else-if="column.key === 'room'">
            <span class="whitespace-nowrap text-[13px] font-medium text-slate-700">{{ record.room }}</span>
          </template>
          <template v-else-if="column.key === 'dateTime'">
            <div class="flex items-center gap-2 whitespace-nowrap">
              <CalendarClock class="h-3.5 w-3.5 text-slate-400" />
              <span class="text-[13px] font-medium text-slate-700">{{ formatDate(record.appointmentDate) }}</span>
              <span v-if="record.slotTime" class="text-xs text-slate-400">{{ String(record.slotTime).slice(0, 5) }}</span>
            </div>
          </template>
          <template v-else-if="column.key === 'reason'">
            <div class="appointment-reason-cell">
              <span class="line-clamp-1 text-[13px] leading-5 text-slate-700">{{ appointmentReasonMain(record) }}</span>
              <span v-if="appointmentReasonMeta(record)" class="appointment-reason-meta">{{ appointmentReasonMeta(record) }}</span>
            </div>
          </template>
          <template v-else-if="column.key === 'status'">
            <ATag :bordered="false" :class="['appointment-status', appointmentStatusClass(record.status)]">{{ record.status }}</ATag>
          </template>
          <template v-else-if="column.key === 'actions'">
            <button
              type="button"
              class="appointment-action-button appointment-action-appointment"
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

    <div v-else-if="resource === 'prescriptions'" class="appointment-table-shell">
      <ATable
        :columns="prescriptionTableColumns"
        :data-source="filteredRows"
        :pagination="prescriptionPagination"
        row-key="id"
        size="middle"
        table-layout="fixed"
        @change="handlePrescriptionTableChange"
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
        <template #customFilterIcon="{ filtered, column }">
          <CheckSquare v-if="column.key === 'status'" :class="['h-3.5 w-3.5', filtered ? 'text-[#0F52BA]' : 'text-slate-400']" />
          <Search v-else :class="['h-3.5 w-3.5', filtered ? 'text-[#0F52BA]' : 'text-slate-400']" />
        </template>
        <template #emptyText>
          <div class="py-8 text-center">
            <Pill class="mx-auto h-9 w-9 text-slate-300" />
            <p class="mt-3 font-bold text-slate-800">Chưa có đơn thuốc phù hợp</p>
            <p class="mt-1 text-sm text-slate-500">Thử đổi từ khóa tìm kiếm trong từng cột.</p>
          </div>
        </template>
        <template #bodyCell="{ column, record }">
          <template v-if="column.key === 'id'">
            <span class="font-mono text-xs font-semibold text-[#0F52BA]">{{ record.id }}</span>
          </template>
          <template v-else-if="column.key === 'medicine'">
            <div
              :class="['medicine-chip-group', medicineChipDensityClass(prescriptionMedicineNames(record).length)]"
              :title="record.medicine"
            >
              <button
                v-for="(medicine, index) in prescriptionMedicineNames(record)"
                :key="`${record.id}-${medicine}-${index}`"
                type="button"
                :class="['medicine-chip-button', medicineChipClass(index)]"
                :aria-label="medicine"
              >
                {{ medicine }}
              </button>
              <span v-if="!prescriptionMedicineNames(record).length" class="text-[13px] font-medium text-slate-400">Chưa có thuốc</span>
            </div>
          </template>
          <template v-else-if="column.key === 'quantity'">
            <span class="text-[13px] font-medium text-slate-600">{{ record.quantity }}</span>
          </template>
          <template v-else-if="column.key === 'note'">
            <span class="line-clamp-2 text-[13px] leading-5 text-slate-600" :title="record.note">{{ record.note }}</span>
          </template>
          <template v-else-if="column.key === 'status'">
            <ATag :bordered="false" :class="['appointment-status', statusClass(record.status)]">{{ record.status }}</ATag>
          </template>
          <template v-else-if="column.key === 'actions'">
            <button
              type="button"
              class="appointment-action-button appointment-action-prescription"
              title="Xem chi tiết đơn thuốc"
              aria-label="Xem chi tiết đơn thuốc"
              @click="openDetail(record)"
            >
              <Eye class="h-4 w-4" />
            </button>
          </template>
        </template>
      </ATable>
    </div>

    <div v-else-if="resource === 'bills'" class="appointment-table-shell">
      <ATable
        :columns="billTableColumns"
        :data-source="filteredRows"
        :pagination="billPagination"
        :row-key="billRowKey"
        size="middle"
        table-layout="fixed"
        @change="handleBillTableChange"
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
        <template #customFilterIcon="{ filtered, column }">
          <CheckSquare v-if="column.key === 'status'" :class="['h-3.5 w-3.5', filtered ? 'text-[#0F52BA]' : 'text-slate-400']" />
          <Search v-else :class="['h-3.5 w-3.5', filtered ? 'text-[#0F52BA]' : 'text-slate-400']" />
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
            <span class="font-mono text-xs font-semibold text-[#0F52BA]">{{ record.id }}</span>
          </template>
          <template v-else-if="column.key === 'appointmentId'">
            <span class="font-mono text-xs font-medium text-slate-600">{{ record.appointmentId }}</span>
          </template>
          <template v-else-if="column.key === 'createdAt'">
            <div class="flex items-center gap-2 whitespace-nowrap">
              <CalendarClock class="h-3.5 w-3.5 text-slate-400" />
              <span class="text-[13px] text-slate-600">{{ record.createdAt }}</span>
            </div>
          </template>
          <template v-else-if="column.key === 'examFee'">
            <span class="whitespace-nowrap text-[13px] text-slate-600">{{ record.examFee }}</span>
          </template>
          <template v-else-if="column.key === 'medicineTotal'">
            <span class="whitespace-nowrap text-[13px] text-slate-600">{{ record.medicineTotal }}</span>
          </template>
          <template v-else-if="column.key === 'amount'">
            <span class="whitespace-nowrap text-[13px] font-semibold text-slate-800">{{ record.amount }}</span>
          </template>
          <template v-else-if="column.key === 'paidAmount'">
            <span class="whitespace-nowrap text-[13px] text-slate-600">{{ record.paidAmount }}</span>
          </template>
          <template v-else-if="column.key === 'status'">
            <ATag :bordered="false" :class="['appointment-status', statusClass(record.status)]">{{ record.status }}</ATag>
          </template>
          <template v-else-if="column.key === 'actions'">
            <div class="bill-action-group">
              <button
                type="button"
                class="appointment-action-button appointment-action-bill"
                title="Xem chi tiết viện phí"
                aria-label="Xem chi tiết viện phí"
                @click="openDetail(record)"
              >
                <Eye class="h-4 w-4" />
              </button>
              <button
                v-if="!isPaidBillRow(record)"
                type="button"
                class="bill-pay-button"
                :disabled="actingId === record.id"
                title="Thanh toán viện phí"
                aria-label="Thanh toán viện phí"
                @click="openPayment(record)"
              >
                <CreditCard class="h-4 w-4" />
              </button>
            </div>
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

      <div v-if="filteredRows.length" class="overflow-x-auto">
        <table class="min-w-full divide-y divide-slate-100 text-sm">
          <thead class="bg-slate-50 text-left text-xs font-bold uppercase tracking-wide text-slate-500">
            <tr>
              <th v-for="column in config.columns" :key="column.key" class="px-5 py-3">{{ column.label }}</th>
              <th class="px-5 py-3 text-right">Thao tác</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-slate-100">
            <tr v-for="row in paginatedRows" :key="String(row.id)" class="transition hover:bg-slate-50">
              <td v-for="column in config.columns" :key="column.key" class="px-5 py-4 align-top">
                <span v-if="column.badge" :class="['rounded-full px-2.5 py-1 text-xs font-bold', statusClass(value(row, column.key))]">{{ value(row, column.key) }}</span>
                <span v-else :class="column.strong ? 'font-bold text-slate-950' : 'text-slate-700'">{{ value(row, column.key) }}</span>
              </td>
              <td class="px-5 py-4 text-right">
                <button type="button" class="rounded-lg bg-blue-50 px-3 py-1.5 text-xs font-bold text-[#003c90] transition hover:bg-blue-100" @click="openDetail(row)">
                  Chi tiết
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

    <div v-if="paymentOpen" class="fixed inset-0 z-[90] flex items-center justify-center bg-slate-950/50 p-4">
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

    <Teleport to="body">
      <div v-if="detailOpen" class="fixed inset-0 z-[120] bg-slate-950/40 backdrop-blur-sm transition-opacity" @click="closeDetail"></div>
      <transition
        enter-active-class="transition duration-300 ease-out"
        enter-from-class="translate-x-full"
        enter-to-class="translate-x-0"
        leave-active-class="transition duration-200 ease-in"
        leave-from-class="translate-x-0"
        leave-to-class="translate-x-full"
      >
        <div v-if="detailOpen" class="fixed right-0 top-0 z-[120] flex h-screen w-full max-w-2xl flex-col border-l border-slate-200 bg-white shadow-2xl">
          <div class="border-b border-slate-100 bg-slate-50/50 p-5">
            <div class="flex items-start justify-between gap-4">
              <div class="flex items-start gap-3">
                <span :class="['flex h-11 w-11 shrink-0 items-center justify-center rounded-2xl', detailAccentClass]">
                  <component :is="detailIcon" class="h-5 w-5" />
                </span>
                <div>
                  <div class="flex flex-wrap items-center gap-2">
                    <h2 class="text-lg font-bold text-slate-900">{{ detailTitle }}</h2>
                    <ATag v-if="detailStatus" :bordered="false" :class="['appointment-status', statusClass(detailStatus)]">
                      {{ detailStatus }}
                    </ATag>
                  </div>
                  <p class="mt-1 font-mono text-xs font-semibold text-slate-500">Mã: {{ detailRow?.id || 'Chưa cập nhật' }}</p>
                </div>
              </div>
              <button type="button" class="rounded-xl p-2 text-slate-400 transition hover:bg-slate-100 hover:text-slate-600" @click="closeDetail">
                <X class="h-5 w-5" />
              </button>
            </div>
          </div>

          <div class="flex-1 overflow-y-auto p-6">
            <div class="space-y-5">
              <section v-for="section in detailSections" :key="section.title" class="space-y-3">
                <h3 class="flex items-center gap-2 text-xs font-bold uppercase tracking-wider text-slate-400">
                  <component :is="section.icon" class="h-4 w-4 text-[#0F52BA]" />
                  {{ section.title }}
                </h3>
                <div class="grid gap-3 sm:grid-cols-2">
                  <div
                    v-for="item in section.items"
                    :key="item.label"
                    :class="['rounded-xl border border-slate-100 bg-slate-50 p-4', item.full ? 'sm:col-span-2' : '']"
                  >
                    <p class="text-xs font-semibold text-slate-400">{{ item.label }}</p>
                    <p class="mt-1.5 whitespace-pre-line break-words text-sm font-semibold text-slate-900">{{ item.value }}</p>
                  </div>
                </div>
              </section>
            </div>
          </div>
        </div>
      </transition>
    </Teleport>

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
import { AtSign, CalendarClock, CalendarDays, CheckSquare, ChevronLeft, ChevronRight, CircleAlert, CircleCheck, ClipboardList, CloudDownload, Copy, CreditCard, Download, Droplet, Eye, FileHeart, HeartPulse, IdCard, KeyRound, Mail, MapPin, Pencil, Phone, Pill, Save, Search, SearchX, ShieldAlert, ShieldCheck, Stethoscope, UserRound, VenetianMask, X } from 'lucide-vue-next'
import BaseButton from '@/components/ui/BaseButton.vue'
import FullscreenLoader from '@/components/ui/FullscreenLoader.vue'
import Toast from '@/components/ui/Toast.vue'
import { useAuthStore } from '@/stores/authStore'
import { appointmentApi } from '@/services/appointmentApi'
import { authApi } from '@/services/authApi'
import { billingApi } from '@/services/billingApi'
import { medicalRecordApi, type PatientMedicalHistory } from '@/services/medicalRecordApi'
import { getApiErrorMessage } from '@/services/apiClient'
import type { Appointment } from '@/types/appointment'
import type { Invoice, Prescription } from '@/types/billing'
import type { Doctor } from '@/types/doctor'
import type { MedicalRecord, Patient } from '@/types/medicalRecord'

type Resource = 'appointments' | 'records' | 'prescriptions' | 'bills' | 'profile'
type Row = Record<string, any>
interface Column { key: string; label: string; badge?: boolean; strong?: boolean }
interface DetailItem { label: string; value: string; full?: boolean }
interface DetailSection { title: string; icon: any; items: DetailItem[] }

const route = useRoute()
const authStore = useAuthStore()
const lastLoginText = ref('')
const loading = ref(false)
const error = ref('')
const note = ref('')
const query = ref('')
const rows = ref<Row[]>([])
const doctors = ref<Doctor[]>([])
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
const passwordSaving = ref(false)
const profileEditMode = ref(false)
const profilePasswordMode = ref(false)
const profileDownloading = ref(false)
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
const passwordForm = reactive({
  currentPassword: '',
  newPassword: '',
  confirmPassword: '',
})

const resource = computed<Resource>(() => isResource(route.meta.patientResource) ? route.meta.patientResource : 'appointments')
const config = computed(() => configs[resource.value])

const profileAge = computed(() => {
  if (!profileForm.dateOfBirth) return '—'
  const birth = new Date(profileForm.dateOfBirth)
  if (Number.isNaN(birth.getTime())) return '—'
  const now = new Date()
  let age = now.getFullYear() - birth.getFullYear()
  const monthDiff = now.getMonth() - birth.getMonth()
  if (monthDiff < 0 || (monthDiff === 0 && now.getDate() < birth.getDate())) age--
  return `${age} tuổi`
})
const patientId = computed(() => String(currentPatient.value?.id || currentPatient.value?.patientId || ''))
const displayPatientCode = computed(() => patientDisplayCode(currentPatient.value) || formatPatientCode(patientId.value) || 'Chưa liên kết')

watch(() => toast.show, (visible) => {
  if (toastTimer) clearTimeout(toastTimer)
  if (visible) toastTimer = setTimeout(() => { toast.show = false }, 3000)
})

const configs: Record<Resource, { title: string; service: string; description: string; placeholder: string; icon: any; iconClass: string; search: string[]; columns: Column[] }> = {
  appointments: cfg('Lịch hẹn của tôi', '', 'Theo dõi lịch đã đặt, bác sĩ, giờ khám và trạng thái xác nhận.', 'Tìm mã lịch, bác sĩ, chuyên khoa, phòng, lý do, trạng thái...', CalendarClock, 'bg-blue-50 text-[#0F52BA]', ['id', 'doctorName', 'specialtyName', 'room', 'status', 'reason', 'dateTime'], cols(['id', 'Mã lịch'], ['doctorName', 'Bác sĩ', false, true], ['specialtyName', 'Chuyên khoa'], ['dateTime', 'Ngày giờ hẹn'], ['reason', 'Lý do khám'], ['status', 'Trạng thái', true])),
  records: cfg('Hồ sơ bệnh án', 'Hồ sơ khám bệnh', 'Xem chẩn đoán, triệu chứng và ghi chú bác sĩ sau mỗi lần khám.', 'Tìm chẩn đoán, triệu chứng, ghi chú...', FileHeart, 'bg-indigo-50 text-indigo-700', ['id', 'diagnosis', 'symptoms', 'doctorNotes'], cols(['id', 'Mã BA'], ['diagnosis', 'Chẩn đoán', false, true], ['symptoms', 'Triệu chứng'], ['doctorNotes', 'Ghi chú'], ['createdAt', 'Ngày tạo'])),
  prescriptions: cfg('Đơn thuốc của tôi', 'Đơn thuốc đã kê', 'Theo dõi thuốc đã kê, số lượng, ghi chú và trạng thái xử lý.', 'Tìm mã đơn, thuốc, trạng thái...', Pill, 'bg-cyan-50 text-cyan-700', ['id', 'medicine', 'status', 'note'], cols(['id', 'Mã đơn'], ['medicine', 'Thuốc', false, true], ['quantity', 'Số lượng'], ['note', 'Ghi chú'], ['status', 'Trạng thái', true])),
  bills: cfg('Viện phí của tôi', '', 'Theo dõi hóa đơn, số tiền và thực hiện thanh toán viện phí khi cần.', 'Tìm mã hóa đơn, trạng thái...', CreditCard, 'bg-emerald-50 text-emerald-700', ['id', 'amount', 'status'], cols(['id', 'Mã HĐ'], ['appointmentId', 'Lịch hẹn'], ['amount', 'Số tiền', false, true], ['status', 'Trạng thái', true])),
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
    width: 104,
    customFilterDropdown: true,
    onFilter: appointmentColumnFilter('id'),
  },
  {
    title: 'Bác sĩ',
    dataIndex: 'doctorName',
    key: 'doctorName',
    width: 180,
    customFilterDropdown: true,
    onFilter: appointmentColumnFilter('doctorName'),
    sorter: (a: Row, b: Row) => String(a.doctorName || '').localeCompare(String(b.doctorName || ''), 'vi'),
  },
  {
    title: 'Chuyên khoa',
    dataIndex: 'specialtyName',
    key: 'specialtyName',
    width: 150,
    customFilterDropdown: true,
    onFilter: appointmentColumnFilter('specialtyName'),
    sorter: (a: Row, b: Row) => String(a.specialtyName || '').localeCompare(String(b.specialtyName || ''), 'vi'),
  },
  {
    title: 'Phòng',
    dataIndex: 'room',
    key: 'room',
    width: 94,
    customFilterDropdown: true,
    onFilter: appointmentColumnFilter('room'),
    sorter: (a: Row, b: Row) => String(a.room || '').localeCompare(String(b.room || ''), 'vi'),
  },
  {
    title: 'Ngày giờ hẹn',
    dataIndex: 'dateTime',
    key: 'dateTime',
    width: 158,
    sorter: (a: Row, b: Row) => appointmentTimestamp(a) - appointmentTimestamp(b),
    defaultSortOrder: 'descend' as const,
  },
  {
    title: 'Lý do khám',
    dataIndex: 'reason',
    key: 'reason',
    width: 260,
    customFilterDropdown: true,
    onFilter: appointmentColumnFilter('reason'),
  },
  {
    title: 'Trạng thái',
    dataIndex: 'status',
    key: 'status',
    width: 132,
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
    width: 74,
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

const prescriptionTableColumns = [
  {
    title: 'Mã đơn',
    dataIndex: 'id',
    key: 'id',
    width: 124,
    customFilterDropdown: true,
    onFilter: appointmentColumnFilter('id'),
    sorter: (a: Row, b: Row) => String(a.id || '').localeCompare(String(b.id || ''), 'vi'),
  },
  {
    title: 'Thuốc',
    dataIndex: 'medicine',
    key: 'medicine',
    width: 260,
    customFilterDropdown: true,
    onFilter: appointmentColumnFilter('medicine'),
    sorter: (a: Row, b: Row) => String(a.medicine || '').localeCompare(String(b.medicine || ''), 'vi'),
  },
  {
    title: 'Số lượng',
    dataIndex: 'quantity',
    key: 'quantity',
    width: 104,
    sorter: (a: Row, b: Row) => Number(a.quantity || 0) - Number(b.quantity || 0),
  },
  {
    title: 'Ghi chú',
    dataIndex: 'note',
    key: 'note',
    width: 240,
    customFilterDropdown: true,
    onFilter: appointmentColumnFilter('note'),
  },
  {
    title: 'Trạng thái',
    dataIndex: 'status',
    key: 'status',
    width: 140,
    filters: [
      { text: 'Đã thanh toán', value: 'Đã thanh toán' },
      { text: 'Chưa thanh toán', value: 'Chưa thanh toán' },
      { text: 'Hoàn tất', value: 'Hoàn tất' },
      { text: 'Đang chờ', value: 'Đang chờ' },
      { text: 'Chưa cập nhật', value: 'Chưa cập nhật' },
    ],
    filterReset: 'Đặt lại',
    filterConfirm: 'Áp dụng',
    onFilter: (filterValue: string | number | boolean, record: Row) => String(record.status || '') === String(filterValue),
  },
  {
    title: 'Thao t\u00e1c',
    key: 'actions',
    width: 74,
    align: 'center' as const,
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
  showTotal: (total: number, range: [number, number]) => `${range[0]}-${range[1]} trong ${total} đơn thuốc`,
  locale: { items_per_page: ' / trang' },
}))

const billTableColumns = [
  {
    title: 'Mã HĐ',
    dataIndex: 'id',
    key: 'id',
    width: 112,
    customFilterDropdown: true,
    onFilter: billColumnFilter('id'),
    sorter: (a: Row, b: Row) => String(a.id || '').localeCompare(String(b.id || ''), 'vi'),
  },
  {
    title: 'Lịch hẹn',
    dataIndex: 'appointmentId',
    key: 'appointmentId',
    width: 104,
    customFilterDropdown: true,
    onFilter: billColumnFilter('appointmentId'),
  },
  {
    title: 'Ngày tạo',
    dataIndex: 'createdAt',
    key: 'createdAt',
    width: 132,
    customFilterDropdown: true,
    onFilter: billColumnFilter('createdAt'),
    sorter: (a: Row, b: Row) => dateTimestamp(b.createdAtValue) - dateTimestamp(a.createdAtValue),
  },
  {
    title: 'Phí khám',
    dataIndex: 'examFee',
    key: 'examFee',
    width: 124,
    customFilterDropdown: true,
    onFilter: billColumnFilter('examFee'),
    sorter: (a: Row, b: Row) => Number(a.examFeeValue || 0) - Number(b.examFeeValue || 0),
  },
  {
    title: 'Tiền thuốc',
    dataIndex: 'medicineTotal',
    key: 'medicineTotal',
    width: 124,
    customFilterDropdown: true,
    onFilter: billColumnFilter('medicineTotal'),
    sorter: (a: Row, b: Row) => Number(a.medicineTotalValue || 0) - Number(b.medicineTotalValue || 0),
  },
  {
    title: 'Tổng tiền',
    dataIndex: 'amount',
    key: 'amount',
    width: 138,
    customFilterDropdown: true,
    onFilter: billColumnFilter('amount'),
    sorter: (a: Row, b: Row) => Number(a.amountValue || 0) - Number(b.amountValue || 0),
  },
  {
    title: 'Đã trả',
    dataIndex: 'paidAmount',
    key: 'paidAmount',
    width: 124,
    customFilterDropdown: true,
    onFilter: billColumnFilter('paidAmount'),
    sorter: (a: Row, b: Row) => Number(a.paidAmountValue || 0) - Number(b.paidAmountValue || 0),
  },
  {
    title: 'Trạng thái',
    dataIndex: 'status',
    key: 'status',
    width: 144,
    filters: [
      { text: 'Đã thanh toán', value: 'Đã thanh toán' },
      { text: 'Chưa thanh toán', value: 'Chưa thanh toán' },
      { text: 'Đã hủy', value: 'Đã hủy' },
      { text: 'Chưa cập nhật', value: 'Chưa cập nhật' },
    ],
    filterReset: 'Đặt lại',
    filterConfirm: 'Áp dụng',
    onFilter: (filterValue: string | number | boolean, record: Row) => String(record.status || '') === String(filterValue),
  },
  {
    title: 'Thao tác',
    key: 'actions',
    width: 112,
    align: 'center' as const,
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
  showTotal: (total: number, range: [number, number]) => `${range[0]}-${range[1]} trong ${total} viện phí`,
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

function handlePrescriptionTableChange(pagination: { current?: number; pageSize?: number }) {
  currentPage.value = pagination.current || 1
  itemsPerPage.value = pagination.pageSize || 10
}

function appointmentColumnFilter(key: string) {
  return (filterValue: string | number | boolean, record: Row) => {
    const searchValue = key === 'reason' ? record.reasonSearch || record.reason : record[key]
    return normalizeSearchText(searchValue).includes(normalizeSearchText(filterValue))
  }
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
  if (resource.value === 'bills') return 'Chi tiết viện phí'
  return resource.value === 'records' ? 'Chi tiết bệnh án' : 'Chi tiết đơn thuốc'
})
const detailIcon = computed(() => {
  if (resource.value === 'appointments') return CalendarClock
  if (resource.value === 'bills') return CreditCard
  if (resource.value === 'prescriptions') return Pill
  return FileHeart
})
const detailAccentClass = computed(() => {
  if (resource.value === 'bills') return 'bg-emerald-50 text-emerald-700'
  if (resource.value === 'prescriptions') return 'bg-cyan-50 text-cyan-700'
  if (resource.value === 'records') return 'bg-indigo-50 text-indigo-700'
  return 'bg-blue-50 text-[#0F52BA]'
})
const detailStatus = computed(() => String(detailRow.value?.status || '').trim())
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
const detailSections = computed<DetailSection[]>(() => {
  const row = detailRow.value || {}
  if (resource.value === 'appointments') {
    return [
      {
        title: 'Thông tin lịch hẹn',
        icon: CalendarClock,
        items: [
          { label: 'Mã lịch', value: String(row.id || 'Chưa cập nhật') },
          { label: 'Bác sĩ', value: String(row.doctorName || 'Chưa phân công') },
          { label: 'Chuyên khoa', value: String(row.specialtyName || 'Chưa cập nhật') },
          { label: 'Phòng', value: String(row.room || 'Chưa cập nhật') },
        ],
      },
      {
        title: 'Thời gian và ghi chú',
        icon: ClipboardList,
        items: [
          { label: 'Ngày giờ hẹn', value: String(row.dateTime || formatAppointmentDateTime(row.appointmentDate, row.slotTime) || 'Chưa cập nhật') },
          { label: 'Trạng thái', value: String(row.status || 'Chưa cập nhật') },
          { label: 'Lý do khám', value: appointmentReasonDetailText(row), full: true },
        ],
      },
    ]
  }
  if (resource.value === 'records') {
    return [
      {
        title: 'Thông tin hồ sơ',
        icon: FileHeart,
        items: [
          { label: 'Mã bệnh án', value: String(row.id || 'Chưa cập nhật') },
          { label: 'Ngày tái khám', value: String(row.followUpDate || 'Chưa hẹn') },
        ],
      },
      {
        title: 'Nội dung khám',
        icon: ClipboardList,
        items: [
          { label: 'Chẩn đoán', value: String(row.diagnosis || 'Chưa có chẩn đoán'), full: true },
          { label: 'Triệu chứng', value: String(row.symptoms || 'Chưa ghi nhận'), full: true },
          { label: 'Hướng điều trị', value: String(row.treatmentPlan || 'Chưa ghi nhận'), full: true },
          { label: 'Ghi chú bác sĩ', value: String(row.doctorNotes || 'Chưa ghi chú'), full: true },
        ],
      },
    ]
  }
  if (resource.value === 'bills') {
    return [
      {
        title: 'Thông tin hóa đơn',
        icon: CreditCard,
        items: [
          { label: 'Mã hóa đơn', value: String(row.id || 'Chưa cập nhật') },
          { label: 'Lịch hẹn', value: String(row.appointmentId || '-') },
          { label: 'Đơn thuốc', value: String(row.prescriptionId || '-') },
          { label: 'Trạng thái', value: String(row.status || 'Chưa cập nhật') },
        ],
      },
      {
        title: 'Chi phí',
        icon: ClipboardList,
        items: [
          { label: 'Phí khám', value: String(row.examFee || formatCurrency(0)) },
          { label: 'Tiền thuốc', value: String(row.medicineTotal || formatCurrency(0)) },
          { label: 'Tổng tiền', value: String(row.amount || formatCurrency(0)) },
          { label: 'Đã trả', value: String(row.paidAmount || formatCurrency(0)) },
          { label: 'Còn lại', value: String(row.balanceDue || formatCurrency(0)) },
        ],
      },
      {
        title: 'Thanh toán',
        icon: CalendarClock,
        items: [
          { label: 'Ngày tạo', value: String(row.createdAt || 'Chưa cập nhật') },
          { label: 'Ngày thanh toán', value: String(row.paidAt || 'Chưa thanh toán') },
          { label: 'Phương thức', value: String(row.paymentMethod || 'Chưa cập nhật') },
        ],
      },
    ]
  }
  return [
    {
      title: 'Thông tin đơn thuốc',
      icon: Pill,
      items: [
        { label: 'Mã đơn', value: String(row.id || 'Chưa cập nhật') },
        { label: 'Số lượng', value: String(row.quantity || '-') },
        { label: 'Trạng thái', value: String(row.status || 'Chưa cập nhật') },
        { label: 'Thuốc', value: String(row.medicine || 'Chưa có thuốc'), full: true },
        { label: 'Ghi chú', value: String(row.note || 'Không có ghi chú'), full: true },
      ],
    },
  ]
})

watch(resource, () => {
  query.value = ''
  void loadData()
}, { immediate: true })

onMounted(() => {
  window.addEventListener('patient-profile-updated', handlePatientProfileUpdated)

  const lastLogin = localStorage.getItem('cliniccare_last_login')
  if (lastLogin) {
    const d = new Date(lastLogin)
    if (!isNaN(d.getTime())) {
      lastLoginText.value = `${d.toLocaleDateString('vi-VN')} – ${d.toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' })}`
    }
  } else {
    const now = new Date()
    lastLoginText.value = `${now.toLocaleDateString('vi-VN')} – ${now.toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' })}`
  }
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
      if (id) {
        const [appointments, doctorList] = await Promise.all([
          appointmentApi.getAppointmentsByPatient(id).catch(() => [] as Appointment[]),
          appointmentApi.getDoctors().catch(() => [] as Doctor[]),
        ])
        doctors.value = doctorList
        rows.value = uniqueRows(appointments.map(mapAppointment))
          .sort((a, b) => appointmentTimestamp(b) - appointmentTimestamp(a))
      } else {
        doctors.value = []
        rows.value = []
      }
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

async function changePassword() {
  if (passwordForm.newPassword.length < 6) {
    showToast('Mật khẩu chưa hợp lệ', 'Mật khẩu mới phải có ít nhất 6 ký tự.', 'error')
    return
  }
  if (passwordForm.newPassword !== passwordForm.confirmPassword) {
    showToast('Xác nhận chưa khớp', 'Vui lòng nhập lại đúng mật khẩu mới.', 'error')
    return
  }
  if (passwordForm.currentPassword === passwordForm.newPassword) {
    showToast('Mật khẩu chưa thay đổi', 'Mật khẩu mới phải khác mật khẩu hiện tại.', 'error')
    return
  }

  passwordSaving.value = true
  try {
    await authApi.changePassword({
      currentPassword: passwordForm.currentPassword,
      newPassword: passwordForm.newPassword,
      confirmPassword: passwordForm.confirmPassword,
    })
    passwordForm.currentPassword = ''
    passwordForm.newPassword = ''
    passwordForm.confirmPassword = ''
    showToast('Đổi mật khẩu thành công', 'Bạn có thể sử dụng mật khẩu mới ở lần đăng nhập tiếp theo.', 'success')
  } catch (apiError) {
    showToast('Chưa đổi được mật khẩu', getApiErrorMessage(apiError), 'error')
  } finally {
    passwordSaving.value = false
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
  const code = cleanDisplayText(item.invoiceCode || item.invoiceIdCode || item.InvoiceCode || item.InvoiceIdCode)
  if (code) {
    if (/^hđ|^hd/i.test(code)) return code
    return /^\d+$/.test(code) ? `HĐ${code.padStart(3, '0')}` : `HĐ${code}`
  }
  const id = toNumber(item.invoiceId, item.InvoiceId, item.id, item.Id)
  return id ? `HĐ${String(id).padStart(3, '0')}` : 'HĐ'
}

function mapAppointment(item: Appointment & Record<string, any>): Row {
  const appointmentId = getAny(item, 'appointmentId', 'AppointmentId', 'id', 'Id')
  const appointmentDate = getAny(item, 'appointmentDate', 'AppointmentDate')
  const slotTime = getAny(item, 'slotTime', 'SlotTime')
  const doctorName = cleanDisplayText(getAny(item, 'doctorName', 'DoctorName'))
  const specialtyName = cleanDisplayText(getAny(item, 'specialtyName', 'SpecialtyName'))
  const room = appointmentRoom(item)
  const reason = cleanDisplayText(getAny(item, 'reason', 'Reason'))
  const reasonParts = parseAppointmentReason(reason)
  const status = getAny(item, 'status', 'Status')

  return {
    id: appointmentDisplayCode(item, appointmentId),
    appointmentId,
    doctorName: doctorName || 'Chưa phân công bác sĩ',
    specialtyName: specialtyName || 'Chưa cập nhật',
    room: room || 'Chưa cập nhật',
    appointmentDate,
    slotTime,
    dateTime: formatAppointmentDateTime(appointmentDate, slotTime),
    reason: reasonParts.main || 'Chưa ghi nhận',
    reasonRaw: reason,
    reasonSupport: reasonParts.support,
    reasonCompanions: reasonParts.companions,
    reasonDetails: reasonParts.details,
    reasonSearch: [reasonParts.main, reasonParts.support, ...reasonParts.companions, reason].filter(Boolean).join(' '),
    status: statusLabel(status),
    raw: item,
  }
}

function parseAppointmentReason(value: unknown) {
  const raw = cleanDisplayText(value)
  if (!raw) return { main: '', support: '', companions: [] as string[], details: [] as string[] }

  const supportMarker = 'Nhu cầu hỗ trợ:'
  const companionMarker = 'Người đi cùng/người thân:'
  const supportIndex = raw.indexOf(supportMarker)
  const companionIndex = raw.indexOf(companionMarker)
  const markers = [supportIndex, companionIndex].filter((index) => index >= 0)
  const firstMarker = markers.length ? Math.min(...markers) : -1
  const main = cleanDisplayText(firstMarker >= 0 ? raw.slice(0, firstMarker) : raw)

  let support = ''
  if (supportIndex >= 0) {
    const supportEnd = companionIndex > supportIndex ? companionIndex : raw.length
    support = cleanDisplayText(raw.slice(supportIndex + supportMarker.length, supportEnd))
  }

  const companions = companionIndex >= 0
    ? raw
        .slice(companionIndex + companionMarker.length)
        .split(/\n| - (?=[^-:]+ - Quan hệ:)/)
        .map((item) => cleanDisplayText(item.replace(/^-+\s*/, '')))
        .filter(Boolean)
    : []

  const details = [
    main ? `Lý do chính: ${main}` : '',
    support ? `Nhu cầu hỗ trợ: ${support}` : '',
    ...companions.map((item) => `Người đi cùng: ${formatCompanionReason(item)}`),
  ].filter(Boolean)

  return { main, support, companions, details }
}

function formatCompanionReason(value: string) {
  return cleanDisplayText(value.replace(/\s+-\s+/g, ' · '))
}

function appointmentReasonMain(row: Row) {
  return cleanDisplayText(row.reason) || cleanDisplayText(row.reasonMain) || 'Chưa ghi nhận'
}

function appointmentReasonMeta(row: Row) {
  const support = cleanDisplayText(row.reasonSupport)
  const companionCount = Array.isArray(row.reasonCompanions) ? row.reasonCompanions.length : 0
  if (support && companionCount) return `${support} · ${companionCount} người đi cùng`
  if (support) return support
  if (companionCount) return `${companionCount} người đi cùng`
  return ''
}

function appointmentReasonDetailText(row: Row) {
  const details = Array.isArray(row.reasonDetails) ? row.reasonDetails.filter(Boolean) : []
  if (details.length) return details.join('\n')
  return cleanDisplayText(row.reasonRaw || row.reason) || 'Chưa ghi nhận'
}

function appointmentRoom(item: Appointment & Record<string, any>) {
  const doctorId = Number(getAny(item, 'doctorId', 'DoctorId'))
  const doctor = doctors.value.find((entry) => Number(entry.doctorId) === doctorId)
  return cleanDisplayText(
    getAny(doctor, 'roomNumber', 'RoomNumber', 'roomName', 'RoomName', 'room', 'Room')
    || getAny(item, 'doctorRoom', 'DoctorRoom', 'doctorRoomNumber', 'DoctorRoomNumber', 'roomNumber', 'RoomNumber', 'roomName', 'RoomName', 'room', 'Room')
    || getAny(getAny(item, 'doctor', 'Doctor'), 'roomNumber', 'RoomNumber', 'roomName', 'RoomName', 'room', 'Room')
  )
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
  const medicineNames = items.map((line: any) => line.medicineNameSnapshot || line.MedicineNameSnapshot || line.medicineName || line.MedicineName).filter(Boolean)
  const medicines = medicineNames.join(', ')
  const quantity = items.reduce((total: number, line: any) => total + Number(line.quantity || line.Quantity || 0), 0)
  return {
    id: prescriptionDisplayCode(item),
    medicine: medicines || 'Chưa có thuốc',
    medicineNames,
    quantity: quantity || '-',
    note: item.note || item.Note || 'Không có ghi chú',
    status: statusLabel(item.status || item.Status),
    raw: item,
  }
}

function prescriptionMedicineNames(row: Row) {
  if (Array.isArray(row.medicineNames) && row.medicineNames.length) return row.medicineNames.map(String)
  const medicineText = String(row.medicine || '').trim()
  if (!medicineText || medicineText === 'Chưa có thuốc') return []
  return medicineText.split(',').map((name) => name.trim()).filter(Boolean)
}

function medicineChipClass(index: number) {
  return `medicine-chip-${index % 8}`
}

function medicineChipDensityClass(count: number) {
  if (count >= 5) return 'medicine-chip-group-dense'
  if (count >= 3) return 'medicine-chip-group-compact'
  return 'medicine-chip-group-roomy'
}

function mapInvoice(item: Invoice & Record<string, any>): Row {
  const amount = invoiceAmount(item)
  const invoiceId = toNumber(item.invoiceId, item.InvoiceId, item.id, item.Id)
  const invoiceCode = invoiceDisplayCode(item)
  const appointmentId = toNumber(item.appointmentId, item.AppointmentId)
  const status = statusLabel(item.status || item.Status)
  const examFee = toNumber(item.examinationFee, item.ExaminationFee, item.examFee, item.ExamFee)
  const medicineTotal = toNumber(item.medicineTotal, item.MedicineTotal)
  const paidAmountRaw = toNumber(item.paidAmount, item.PaidAmount)
  const paidAmount = paidAmountRaw || (String(status).toLowerCase().includes('đã thanh toán') ? amount : 0)
  const balanceDueRaw = Number(getAny(item, 'balanceDue', 'BalanceDue'))
  const balanceDue = Number.isFinite(balanceDueRaw) ? Math.max(balanceDueRaw, 0) : Math.max(amount - paidAmount, 0)
  const createdAt = getAny(item, 'createdAt', 'CreatedAt')
  const paidAt = getAny(item, 'paidAt', 'PaidAt')
  const payments = getArrayValue(item, 'payments', 'Payments')
  const firstPayment = payments[0]
  const paymentMethod = cleanDisplayText(
    getAny(firstPayment, 'paymentMethod', 'PaymentMethod', 'method', 'Method', 'channel', 'Channel')
    || getAny(item, 'paymentMethod', 'PaymentMethod', 'method', 'Method', 'paymentChannel', 'PaymentChannel'),
  )
  return {
    id: invoiceCode,
    invoiceId,
    appointmentId: appointmentId ? `LH${String(appointmentId).padStart(3, '0')}` : '-',
    prescriptionId: item.prescriptionId || item.PrescriptionId ? `#${item.prescriptionId || item.PrescriptionId}` : '-',
    createdAt: formatDate(createdAt),
    createdAtValue: createdAt,
    paidAt: paidAt ? formatDate(paidAt) : (String(status).toLowerCase().includes('đã thanh toán') ? 'Chưa cập nhật' : 'Chưa thanh toán'),
    examFee: formatCurrency(examFee),
    examFeeValue: examFee,
    medicineTotal: formatCurrency(medicineTotal),
    medicineTotalValue: medicineTotal,
    amount: formatCurrency(amount),
    amountValue: amount,
    paidAmount: formatCurrency(paidAmount),
    paidAmountValue: paidAmount,
    balanceDue: formatCurrency(balanceDue),
    balanceDueValue: balanceDue,
    paymentMethod: paymentMethodLabel(paymentMethod, status),
    status,
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
  if (resource.value === 'bills') return
  showToast(
    resource.value === 'records' ? 'Đang xem chi tiết bệnh án' : 'Đang xem chi tiết đơn thuốc',
    resource.value === 'records' ? 'Nếu có đơn thuốc liên quan, sang mục Đơn thuốc để xem chi tiết.' : 'Nếu cần thanh toán, sang mục Viện phí để kiểm tra hóa đơn.',
    'success'
  )
}

function closeDetail() {
  detailOpen.value = false
  detailRow.value = null
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

function getArrayValue(source: unknown, ...keys: string[]) {
  const data = source as Record<string, any> | null | undefined
  for (const key of keys) {
    const value = data?.[key]
    if (Array.isArray(value)) return value as Record<string, any>[]
  }
  return [] as Record<string, any>[]
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

function dateTimestamp(value: unknown) {
  const timestamp = new Date(String(value || '')).getTime()
  return Number.isNaN(timestamp) ? 0 : timestamp
}

function paymentMethodLabel(value?: unknown, status?: string) {
  const raw = String(value || '').trim()
  if (!raw) return String(status || '').toLowerCase().includes('đã thanh toán') ? 'Chưa cập nhật' : 'Chưa thanh toán'
  const normalized = raw
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .replace(/đ/g, 'd')
    .replace(/Đ/g, 'D')
    .toLowerCase()
  if (normalized.includes('bank') || normalized.includes('transfer') || normalized.includes('chuyen khoan') || normalized.includes('qr')) return 'Chuyển khoản'
  if (normalized.includes('cash') || normalized.includes('tien mat')) return 'Tiền mặt'
  if (normalized.includes('card') || normalized.includes('the')) return 'Thẻ'
  if (normalized.includes('vnpay')) return 'VNPay'
  if (normalized.includes('momo')) return 'MoMo'
  return raw
}

function formatDate(value?: unknown) {
  if (!value) return 'Chưa cập nhật'
  const text = String(value)
  const dateOnly = text.match(/^(\d{4})-(\d{2})-(\d{2})/)
  if (dateOnly) return `${Number(dateOnly[3])}/${Number(dateOnly[2])}/${dateOnly[1]}`
  const date = new Date(text)
  return Number.isNaN(date.getTime()) ? text : new Intl.DateTimeFormat('vi-VN').format(date)
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

async function printMedicalProfilePdf() {
  if (profileDownloading.value) return
  profileDownloading.value = true
  error.value = ''
  try {
    await resolvePatient()
    syncProfileForm()
    const id = patientId.value
    if (!id) throw new Error('Chưa tìm thấy mã bệnh nhân để xuất hồ sơ.')

    const [doctorResult, appointmentResult, historyResult, invoiceResult, prescriptionResult] = await Promise.allSettled([
      appointmentApi.getDoctors(),
      appointmentApi.getAppointmentsByPatient(id),
      getHistory(),
      billingApi.getInvoices(id),
      billingApi.getPrescriptions(id).catch((err) => {
        if ((err as any)?.response?.status === 404) return [] as Prescription[]
        throw err
      }),
    ])

    const doctorList = settledValue(doctorResult, [] as Doctor[])
    const previousDoctors = doctors.value
    doctors.value = doctorList
    const appointmentRows = settledValue(appointmentResult, [] as Appointment[]).map(mapAppointment)
    doctors.value = previousDoctors

    const clinicalHistory = settledValue(historyResult, { visits: [], medicalRecords: [], prescriptions: [] } as PatientMedicalHistory)
    const recordRows = (clinicalHistory.medicalRecords || []).map(mapRecord)
    const prescriptionsFromHistory = clinicalHistory.prescriptions || []
    const prescriptionsFromBilling = settledValue(prescriptionResult, [] as Prescription[])
    const prescriptionRows = uniquePrescriptionsForReport([...prescriptionsFromHistory, ...prescriptionsFromBilling]).map(mapPrescription)
    const invoiceRows = settledValue(invoiceResult, [] as Invoice[]).map(mapInvoice)

    const html = buildMedicalProfilePrintHtml({
      appointments: appointmentRows,
      records: recordRows,
      prescriptions: prescriptionRows,
      invoices: invoiceRows,
    })
    printHtmlReport(html)
    showToast('Đã tạo bản in PDF', 'Chọn máy in hoặc “Save as PDF” trong hộp thoại in của trình duyệt.', 'success')
  } catch (downloadError) {
    const message = getApiErrorMessage(downloadError)
    error.value = message
    showToast('Chưa tạo được bản in', message, 'error')
  } finally {
    profileDownloading.value = false
  }
}

function settledValue<T>(result: PromiseSettledResult<T>, fallback: T) {
  return result.status === 'fulfilled' ? result.value : fallback
}

function uniquePrescriptionsForReport(items: Prescription[]) {
  const seen = new Set<string>()
  return items.filter((item) => {
    const id = String(item.prescriptionId || item.id || item.prescriptionCode || '')
    if (!id || seen.has(id)) return false
    seen.add(id)
    return true
  })
}

function buildMedicalProfilePrintHtml(data: { appointments: Row[]; records: Row[]; prescriptions: Row[]; invoices: Row[] }) {
  const exportedAt = new Intl.DateTimeFormat('vi-VN', { dateStyle: 'medium', timeStyle: 'short' }).format(new Date())
  const reportTitle = `Ho so y te - ${profileForm.fullName || displayPatientCode.value}`
  const patientRows = [
    ['Mã bệnh nhân', displayPatientCode.value],
    ['Họ và tên', profileForm.fullName || 'Chưa cập nhật'],
    ['Ngày sinh', profileForm.dateOfBirth ? formatDate(profileForm.dateOfBirth) : 'Chưa cập nhật'],
    ['Tuổi', profileAge.value],
    ['Giới tính', profileForm.gender || 'Chưa cập nhật'],
    ['Nhóm máu', profileForm.bloodType || 'Chưa cập nhật'],
    ['CCCD', profileForm.citizenId || 'Chưa cập nhật'],
    ['Email', profileForm.email || 'Chưa cập nhật'],
    ['Số điện thoại', profileForm.phoneNumber || 'Chưa cập nhật'],
    ['Địa chỉ', profileForm.address || 'Chưa cập nhật'],
    ['Dị ứng', profileForm.allergyNote || 'Không ghi nhận'],
    ['Tiền sử bệnh', profileForm.medicalHistory || 'Không ghi nhận'],
  ]

  return `<!doctype html>
<html lang="vi">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>${escapeHtml(reportTitle)}</title>
  <style>
    @page { size: A4; margin: 14mm 12mm; }
    * { box-sizing: border-box; }
    body { margin: 0; background: #f1f5f9; color: #0f172a; font-family: Arial, "Helvetica Neue", sans-serif; -webkit-print-color-adjust: exact; print-color-adjust: exact; }
    main { width: 210mm; min-height: 297mm; margin: 0 auto; background: #fff; padding: 14mm 12mm; }
    header { display: grid; grid-template-columns: 1fr auto; gap: 14px; border-bottom: 2px solid #dbeafe; padding-bottom: 14px; }
    h1 { margin: 0; font-size: 24px; line-height: 1.2; color: #0f172a; }
    h2 { margin: 0 0 10px; font-size: 15px; color: #0f172a; }
    p { margin: 6px 0 0; color: #64748b; line-height: 1.5; font-size: 11px; }
    section { margin-top: 14px; break-inside: avoid; page-break-inside: avoid; }
    table { width: 100%; border-collapse: collapse; table-layout: fixed; font-size: 10.5px; }
    th, td { border: 1px solid #e2e8f0; padding: 6px 6px; text-align: left; vertical-align: top; overflow-wrap: anywhere; }
    th { color: #334155; background: #eff6ff; font-size: 9px; text-transform: uppercase; letter-spacing: .03em; }
    .badge { display: inline-flex; align-items: center; border-radius: 999px; background: #eff6ff; color: #1d4ed8; padding: 5px 10px; font-size: 11px; font-weight: 700; }
    .grid { display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); gap: 8px 12px; }
    .item { border: 1px solid #e2e8f0; border-radius: 8px; padding: 8px; min-height: 48px; }
    .label { display: block; color: #64748b; font-size: 9.5px; font-weight: 700; text-transform: uppercase; letter-spacing: .03em; }
    .value { display: block; margin-top: 4px; font-size: 11.5px; font-weight: 700; line-height: 1.35; overflow-wrap: anywhere; }
    .empty { color: #94a3b8; font-style: italic; }
    .note { margin-top: 10px; border-left: 4px solid #2563eb; background: #eff6ff; padding: 8px 10px; color: #1e3a8a; font-size: 10.5px; line-height: 1.5; }
    .summary { display: grid; grid-template-columns: repeat(4, minmax(0, 1fr)); gap: 8px; margin-top: 12px; }
    .summary-card { border-radius: 10px; background: #f8fafc; padding: 9px; border: 1px solid #e2e8f0; }
    .summary-card strong { display: block; margin-top: 4px; font-size: 16px; }
    @media print { body { background: #fff; } main { width: auto; min-height: auto; margin: 0; padding: 0; } }
  </style>
</head>
<body>
  <main>
    <header>
      <div>
        <h1>Hồ sơ y tế cá nhân</h1>
        <p>Xuất lúc ${escapeHtml(exportedAt)} từ hệ thống MedicareDNU. Báo cáo dùng để tham khảo và hỗ trợ trao đổi với nhân viên y tế.</p>
      </div>
      <span class="badge">${escapeHtml(displayPatientCode.value)}</span>
    </header>
    <div class="summary">
      <div class="summary-card"><span class="label">Lịch hẹn</span><strong>${data.appointments.length}</strong></div>
      <div class="summary-card"><span class="label">Bệnh án</span><strong>${data.records.length}</strong></div>
      <div class="summary-card"><span class="label">Đơn thuốc</span><strong>${data.prescriptions.length}</strong></div>
      <div class="summary-card"><span class="label">Viện phí</span><strong>${data.invoices.length}</strong></div>
    </div>
    <section>
      <h2>Thông tin bệnh nhân</h2>
      <div class="grid">${patientRows.map(([label, value]) => `<div class="item"><span class="label">${escapeHtml(label)}</span><span class="value">${escapeHtml(value)}</span></div>`).join('')}</div>
      <div class="note">Nếu thông tin trên chưa chính xác, vui lòng cập nhật hồ sơ cá nhân trước khi in hoặc lưu PDF.</div>
    </section>
    ${reportTable('Lịch hẹn', data.appointments, ['id', 'doctorName', 'specialtyName', 'room', 'dateTime', 'reason', 'status'], ['Mã lịch', 'Bác sĩ', 'Chuyên khoa', 'Phòng', 'Thời gian', 'Lý do', 'Trạng thái'])}
    ${reportTable('Hồ sơ bệnh án', data.records, ['id', 'diagnosis', 'symptoms', 'treatmentPlan', 'doctorNotes', 'createdAt'], ['Mã BA', 'Chẩn đoán', 'Triệu chứng', 'Điều trị', 'Ghi chú', 'Ngày tạo'])}
    ${reportTable('Đơn thuốc', data.prescriptions, ['id', 'medicine', 'quantity', 'note', 'status'], ['Mã đơn', 'Thuốc', 'Số lượng', 'Ghi chú', 'Trạng thái'])}
    ${reportTable('Viện phí', data.invoices, ['id', 'appointmentId', 'examFee', 'medicineTotal', 'amount', 'paidAmount', 'balanceDue', 'status'], ['Mã HĐ', 'Lịch hẹn', 'Phí khám', 'Tiền thuốc', 'Tổng tiền', 'Đã trả', 'Còn lại', 'Trạng thái'])}
  </main>
</body>
</html>`
}

function reportTable(title: string, rowsToRender: Row[], keys: string[], labels: string[]) {
  const body = rowsToRender.length
    ? rowsToRender.map((row) => `<tr>${keys.map((key) => `<td>${escapeHtml(value(row, key))}</td>`).join('')}</tr>`).join('')
    : `<tr><td colspan="${keys.length}" class="empty">Chưa có dữ liệu</td></tr>`
  return `<section><h2>${escapeHtml(title)}</h2><table><thead><tr>${labels.map((label) => `<th>${escapeHtml(label)}</th>`).join('')}</tr></thead><tbody>${body}</tbody></table></section>`
}

function printHtmlReport(html: string) {
  const iframe = document.createElement('iframe')
  iframe.style.position = 'fixed'
  iframe.style.right = '0'
  iframe.style.bottom = '0'
  iframe.style.width = '0'
  iframe.style.height = '0'
  iframe.style.border = '0'
  iframe.setAttribute('aria-hidden', 'true')
  document.body.appendChild(iframe)

  const frameWindow = iframe.contentWindow
  const frameDocument = iframe.contentDocument || frameWindow?.document
  if (!frameWindow || !frameDocument) {
    iframe.remove()
    throw new Error('Trình duyệt không hỗ trợ tạo bản in PDF.')
  }

  frameDocument.open()
  frameDocument.write(html)
  frameDocument.close()

  const cleanup = () => setTimeout(() => iframe.remove(), 500)
  frameWindow.onafterprint = cleanup
  setTimeout(() => {
    frameWindow.focus()
    frameWindow.print()
    setTimeout(() => {
      if (document.body.contains(iframe)) iframe.remove()
    }, 60000)
  }, 250)
}

function escapeHtml(valueToEscape: unknown) {
  return String(valueToEscape ?? '')
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&#39;')
}

async function copyText(text: string) {
  if (!text) return
  await navigator.clipboard?.writeText(text)
  showToast('Đã copy', text, 'success')
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

:deep(.appointment-table-shell .ant-table-container),
:deep(.appointment-table-shell .ant-table-content) {
  overflow-x: hidden !important;
}

:deep(.appointment-table-shell .ant-table table) {
  width: 100% !important;
  table-layout: fixed !important;
}

:deep(.appointment-table-shell .ant-table-thead > tr > th) {
  height: 44px;
  padding-block: 10px;
  padding-inline: 12px;
  border-bottom: 1px solid #e8edf3;
  background: #f9fbfd;
  color: #64748b;
  font-size: 11.5px;
  font-weight: 650;
}

:deep(.appointment-table-shell .ant-table-tbody > tr > td) {
  min-height: 52px;
  padding-block: 9px;
  padding-inline: 12px;
  border-bottom-color: #eef2f7;
  overflow-wrap: anywhere;
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

:deep(.appointment-action-appointment) {
  border-color: #bfdbfe;
  background: #eff6ff;
  color: #1d4ed8;
}

:deep(.appointment-action-prescription) {
  border-color: #a5f3fc;
  background: #ecfeff;
  color: #0e7490;
}

:deep(.appointment-action-bill) {
  border-color: #bbf7d0;
  background: #f0fdf4;
  color: #15803d;
}

:deep(.appointment-action-button:hover) {
  transform: translateY(-1px);
}

:deep(.appointment-action-appointment:hover) {
  border-color: #93c5fd;
  background: #dbeafe;
  color: #1e40af;
}

:deep(.appointment-action-prescription:hover) {
  border-color: #67e8f9;
  background: #cffafe;
  color: #155e75;
}

:deep(.appointment-action-bill:hover) {
  border-color: #86efac;
  background: #dcfce7;
  color: #166534;
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

.appointment-reason-cell {
  display: flex;
  min-width: 0;
  flex-direction: column;
  gap: 2px;
}

.appointment-reason-meta {
  display: block;
  max-width: 100%;
  overflow: hidden;
  color: #8a99ad;
  font-size: 11px;
  font-weight: 400;
  line-height: 1.35;
  text-overflow: ellipsis;
  white-space: nowrap;
}

:deep(.appointment-status) {
  margin: 0;
  border-radius: 999px;
  padding: 2px 9px;
  font-size: 11px;
  font-weight: 500;
  line-height: 18px;
}

.medicine-chip-group {
  display: flex;
  flex-wrap: wrap;
  gap: 4px;
  overflow: visible;
}

.medicine-chip-button {
  display: inline-flex;
  min-width: 0;
  max-width: 100%;
  height: 24px;
  align-items: center;
  border: 1px solid currentColor;
  border-radius: 999px;
  padding: 0 8px;
  font-size: 11.5px;
  font-weight: 500;
  line-height: 1;
}

.medicine-chip-group-compact {
  gap: 3px;
}

.medicine-chip-group-compact .medicine-chip-button {
  height: 21px;
  padding-inline: 7px;
  font-size: 10.75px;
}

.medicine-chip-group-dense {
  gap: 3px;
}

.medicine-chip-group-dense .medicine-chip-button {
  height: 19px;
  padding-inline: 6px;
  font-size: 10px;
}

.medicine-chip-0 {
  background: #eff6ff;
  color: #1d4ed8;
}

.medicine-chip-1 {
  background: #ecfeff;
  color: #0e7490;
}

.medicine-chip-2 {
  background: #f0fdf4;
  color: #15803d;
}

.medicine-chip-3 {
  background: #fff7ed;
  color: #c2410c;
}

.medicine-chip-4 {
  background: #f5f3ff;
  color: #6d28d9;
}

.medicine-chip-5 {
  background: #fdf2f8;
  color: #be185d;
}

.medicine-chip-6 {
  background: #fefce8;
  color: #a16207;
}

.medicine-chip-7 {
  background: #f0fdfa;
  color: #0f766e;
}

.bill-pay-button {
  align-items: center;
  background: #fff7ed;
  border: 1px solid #fed7aa;
  border-radius: 7px;
  color: #c2410c;
  display: inline-flex;
  font-size: 12px;
  font-weight: 650;
  width: 32px;
  height: 32px;
  justify-content: center;
  padding: 0;
  transition: background 160ms ease, border-color 160ms ease, color 160ms ease, opacity 160ms ease, transform 160ms ease;
}

.bill-pay-button:hover:not(:disabled) {
  background: #ffedd5;
  border-color: #fdba74;
  color: #9a3412;
  transform: translateY(-1px);
}

.bill-pay-button:disabled {
  cursor: not-allowed;
  opacity: .6;
}

.bill-action-group {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
}

@media (max-width: 640px) {
  :deep(.appointment-table-shell .ant-pagination) {
    justify-content: center;
  }

  :deep(.appointment-table-shell .ant-pagination-total-text) {
    display: none;
  }
}

/* ===========================
   PREMIUM PROFILE PAGE STYLES
   =========================== */

.profile-note-banner {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 14px 20px;
  border: 1px solid #BFDBFE;
  border-radius: 18px;
  background: linear-gradient(135deg, #EFF6FF, #F0F7FF);
  color: #1E40AF;
  font-size: 13px;
  font-weight: 500;
}

.profile-page-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
  padding: 4px 2px;
}

.profile-page-title {
  font-size: 1.85rem;
  font-weight: 800;
  letter-spacing: -0.02em;
  color: #0F172A;
  line-height: 1.2;
}

.profile-page-subtitle {
  margin-top: 6px;
  font-size: 13px;
  font-weight: 500;
  color: #64748B;
  line-height: 1.5;
}

.profile-edit-btn {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  height: 44px;
  padding: 0 20px;
  border: none;
  border-radius: 14px;
  background: #2563EB;
  color: #ffffff;
  font-size: 14px;
  font-weight: 600;
  cursor: pointer;
  transition: all 200ms ease;
  box-shadow: 0 4px 14px rgba(37, 99, 235, 0.28);
  white-space: nowrap;
}

.profile-edit-btn:hover {
  background: #1D4ED8;
  transform: translateY(-1px);
  box-shadow: 0 8px 24px rgba(37, 99, 235, 0.36);
}

.profile-summary-card {
  display: flex;
  align-items: center;
  gap: 26px;
  padding: 28px 32px;
  background: #ffffff;
  border: 1px solid #E5E7EB;
  border-radius: 18px;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.04), 0 8px 24px rgba(0, 0, 0, 0.03);
  transition: box-shadow 300ms ease;
}

.profile-summary-card:hover {
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.04), 0 12px 36px rgba(0, 0, 0, 0.06);
}

.profile-summary-left {
  display: flex;
  align-items: center;
  gap: 20px;
  flex-shrink: 0;
  min-width: 340px;
}

.profile-avatar-wrapper { position: relative; flex-shrink: 0; }

.profile-avatar {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 72px;
  height: 72px;
  border-radius: 20px;
  background: linear-gradient(135deg, #EFF6FF, #DBEAFE);
  color: #2563EB;
  font-size: 24px;
  font-weight: 700;
  letter-spacing: -0.02em;
  border: 2px solid #BFDBFE;
}

.profile-avatar-badge {
  position: absolute;
  bottom: -2px;
  right: -2px;
  display: flex;
  align-items: center;
  justify-content: center;
  width: 22px;
  height: 22px;
  border-radius: 50%;
  background: #22C55E;
  color: #ffffff;
  border: 2px solid #ffffff;
  box-shadow: 0 2px 6px rgba(34, 197, 94, 0.4);
}

.profile-identity { min-width: 0; }

.profile-identity-label {
  font-size: 10px;
  font-weight: 700;
  letter-spacing: 0.12em;
  text-transform: uppercase;
  color: #94A3B8;
}

.profile-identity-name {
  margin-top: 4px;
  font-size: 22px;
  font-weight: 700;
  color: #0F172A;
  letter-spacing: -0.01em;
  line-height: 1.3;
}

.profile-identity-meta {
  display: flex;
  align-items: center;
  gap: 12px;
  margin-top: 8px;
  flex-wrap: wrap;
}

.profile-patient-badge {
  display: inline-flex;
  align-items: center;
  height: 26px;
  padding: 0 10px;
  border-radius: 8px;
  background: #EFF6FF;
  color: #2563EB;
  font-size: 11px;
  font-weight: 700;
  font-family: 'SF Mono', ui-monospace, monospace;
  letter-spacing: 0.02em;
}

.profile-updated-text {
  font-size: 12px;
  color: #94A3B8;
  font-weight: 500;
}

.profile-summary-divider {
  width: 1px;
  align-self: stretch;
  background: #E5E7EB;
  flex-shrink: 0;
}

.profile-stats {
  display: grid;
  grid-template-columns: minmax(112px, 0.9fr) auto minmax(118px, 1fr) auto minmax(112px, 1fr) auto minmax(120px, 1fr);
  align-items: center;
  gap: 18px;
  flex: 1;
  min-width: 0;
}

.profile-stat {
  display: flex;
  align-items: flex-start;
  gap: 10px;
  min-width: 0;
}

.profile-stat > div:last-child {
  min-width: 0;
}

.profile-stat-icon {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 36px;
  height: 36px;
  border-radius: 10px;
  background: #EFF6FF;
  color: #2563EB;
  flex-shrink: 0;
}

.profile-stat-icon--red { background: #FEF2F2; color: #EF4444; }
.profile-stat-icon--purple { background: #F5F3FF; color: #7C3AED; }
.profile-stat-icon--green { background: #F0FDF4; color: #22C55E; }

.profile-stat-separator {
  width: 1px;
  height: 40px;
  background: #E5E7EB;
  flex-shrink: 0;
}

.profile-stat-label {
  font-size: 11px;
  font-weight: 600;
  color: #94A3B8;
  text-transform: uppercase;
  letter-spacing: 0.04em;
  line-height: 1.25;
}

.profile-stat-value {
  margin-top: 2px;
  font-size: 18px;
  font-weight: 700;
  color: #0F172A;
  line-height: 1.25;
  white-space: nowrap;
}

.profile-stat-sub {
  margin-top: 1px;
  font-size: 11px;
  color: #94A3B8;
  font-weight: 500;
  line-height: 1.35;
  white-space: nowrap;
}

.profile-status-pill {
  display: inline-flex;
  align-items: center;
  height: 24px;
  margin-top: 4px;
  padding: 0 12px;
  border-radius: 999px;
  background: #DCFCE7;
  color: #15803D;
  font-size: 12px;
  font-weight: 600;
}

.profile-content-grid {
  display: grid;
  grid-template-columns: 1fr 340px;
  gap: 24px;
}

.profile-left-col {
  display: flex;
  flex-direction: column;
  gap: 20px;
  min-width: 0;
}

.profile-right-col {
  display: flex;
  flex-direction: column;
  gap: 20px;
  min-width: 0;
}

.profile-card {
  background: #ffffff;
  border: 1px solid #E5E7EB;
  border-radius: 18px;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.04), 0 8px 24px rgba(0, 0, 0, 0.03);
  overflow: hidden;
  transition: box-shadow 300ms ease;
}

.profile-card:hover {
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.04), 0 12px 36px rgba(0, 0, 0, 0.06);
}

.profile-card-header {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 20px 24px 0;
}

.profile-card-icon {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 36px;
  height: 36px;
  border-radius: 10px;
  background: #EFF6FF;
  color: #2563EB;
  flex-shrink: 0;
}

.profile-card-icon--indigo { background: #EEF2FF; color: #4F46E5; }
.profile-card-icon--rose { background: #FFF1F2; color: #E11D48; }
.profile-card-icon--slate { background: #F1F5F9; color: #475569; }
.profile-card-icon--amber { background: #FFFBEB; color: #D97706; }
.profile-card-icon--sky { background: #F0F9FF; color: #0284C7; }

.profile-card-title {
  font-size: 15px;
  font-weight: 700;
  color: #0F172A;
}

.profile-card-body { padding: 16px 24px 24px; }

.profile-card-divider {
  margin: 4px 24px;
  border-top: 1px solid #F1F5F9;
}

.profile-card-footer {
  display: flex;
  justify-content: flex-end;
  padding: 0 24px 24px;
  border-top: 1px solid #F1F5F9;
  margin-top: 4px;
  padding-top: 20px;
}

.profile-form-grid-2 { display: grid; grid-template-columns: 1fr 1fr; gap: 16px; }
.profile-form-grid-4 { display: grid; grid-template-columns: minmax(180px, 1.2fr) minmax(170px, 1fr) minmax(150px, 0.9fr) minmax(150px, 0.9fr); gap: 16px; }
.profile-form-grid-medical { display: grid; grid-template-columns: 2fr 1fr; gap: 16px; }
.profile-field--full { grid-column: 1 / -1; }

.profile-field {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.profile-field-label {
  display: flex;
  align-items: center;
  gap: 4px;
  font-size: 12.5px;
  font-weight: 600;
  color: #475569;
}

.profile-input-wrap {
  position: relative;
  display: flex;
  align-items: center;
}

.profile-input-icon {
  position: absolute;
  left: 14px;
  top: 50%;
  transform: translateY(-50%);
  width: 16px;
  height: 16px;
  color: #94A3B8;
  pointer-events: none;
}

.profile-input {
  width: 100%;
  height: 44px;
  padding: 0 14px 0 42px;
  border: 1px solid #E2E8F0;
  border-radius: 14px;
  background: #ffffff;
  color: #0F172A;
  font-size: 13.5px;
  font-weight: 500;
  outline: none;
  transition: border-color 200ms ease, box-shadow 200ms ease, background 200ms ease;
}

.profile-input--with-action {
  padding-right: 48px;
}

.profile-input--mono {
  font-family: 'SF Mono', ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace;
  font-size: 12.5px;
  letter-spacing: 0;
}

.profile-input:focus {
  border-color: #2563EB;
  box-shadow: 0 0 0 4px rgba(37, 99, 235, 0.08);
}

.profile-input--disabled {
  background: #F8FAFC;
  color: #64748B;
  cursor: not-allowed;
  border-color: #F1F5F9;
}

.profile-textarea {
  width: 100%;
  padding: 12px 14px;
  border: 1px solid #E2E8F0;
  border-radius: 14px;
  background: #ffffff;
  color: #0F172A;
  font-size: 13.5px;
  font-weight: 500;
  outline: none;
  resize: vertical;
  transition: border-color 200ms ease, box-shadow 200ms ease, background 200ms ease;
}

.profile-textarea:focus {
  border-color: #2563EB;
  box-shadow: 0 0 0 4px rgba(37, 99, 235, 0.08);
}

.profile-textarea--disabled {
  background: #F8FAFC;
  color: #64748B;
  cursor: not-allowed;
  border-color: #F1F5F9;
}

.profile-copy-btn {
  position: absolute;
  right: 10px;
  top: 50%;
  transform: translateY(-50%);
  display: flex;
  align-items: center;
  justify-content: center;
  width: 28px;
  height: 28px;
  border: none;
  border-radius: 8px;
  background: transparent;
  color: #94A3B8;
  cursor: pointer;
  transition: all 160ms ease;
}

.profile-copy-btn:hover {
  background: #F1F5F9;
  color: #2563EB;
}

.profile-password-fields {
  display: flex;
  flex-direction: column;
  gap: 14px;
}

.profile-security-visual {
  display: flex;
  align-items: center;
  gap: 16px;
  margin-bottom: 16px;
}

.profile-security-shield {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 56px;
  height: 56px;
  border-radius: 16px;
  background: linear-gradient(135deg, #EFF6FF, #DBEAFE);
  flex-shrink: 0;
}

.profile-security-text {
  font-size: 13px;
  font-weight: 600;
  color: #334155;
  line-height: 1.5;
}

.profile-security-info { margin-bottom: 16px; }

.profile-security-label {
  font-size: 11px;
  font-weight: 600;
  color: #94A3B8;
  text-transform: uppercase;
  letter-spacing: 0.04em;
}

.profile-security-detail {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-top: 6px;
  font-size: 13px;
  font-weight: 500;
  color: #334155;
}

.profile-security-dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  background: #22C55E;
  flex-shrink: 0;
  box-shadow: 0 0 0 3px rgba(34, 197, 94, 0.2);
}

.profile-security-btn {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
  width: 100%;
  height: 44px;
  border: none;
  border-radius: 14px;
  background: #2563EB;
  color: #ffffff;
  font-size: 14px;
  font-weight: 600;
  cursor: pointer;
  transition: all 200ms ease;
  box-shadow: 0 4px 14px rgba(37, 99, 235, 0.25);
}

.profile-security-btn:hover {
  background: #1D4ED8;
  transform: translateY(-1px);
  box-shadow: 0 6px 20px rgba(37, 99, 235, 0.35);
}

.profile-health-note-card {
  border-radius: 18px;
  background: linear-gradient(135deg, #ECFDF5, #D1FAE5);
  border: 1px solid #A7F3D0;
  padding: 20px 24px;
  overflow: hidden;
}

.profile-health-note-header {
  display: flex;
  align-items: center;
  gap: 10px;
}

.profile-health-note-title {
  font-size: 14px;
  font-weight: 700;
  color: #065F46;
}

.profile-health-note-body {
  display: flex;
  align-items: center;
  gap: 16px;
  margin-top: 12px;
}

.profile-health-note-illustration {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 56px;
  height: 56px;
  border-radius: 16px;
  background: rgba(255, 255, 255, 0.6);
  flex-shrink: 0;
}

.profile-health-note-text {
  font-size: 13px;
  font-weight: 500;
  color: #047857;
  line-height: 1.6;
}

.profile-download-visual {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 56px;
  height: 56px;
  border-radius: 16px;
  background: linear-gradient(135deg, #F0F9FF, #E0F2FE);
  margin-bottom: 12px;
}

.profile-download-text {
  font-size: 13px;
  font-weight: 500;
  color: #64748B;
  margin-bottom: 14px;
  line-height: 1.5;
}

.profile-download-btn {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
  width: 100%;
  height: 40px;
  border: 1.5px dashed #CBD5E1;
  border-radius: 12px;
  background: #F8FAFC;
  color: #475569;
  font-size: 13px;
  font-weight: 600;
  cursor: pointer;
  transition: all 200ms ease;
}

.profile-download-btn:hover {
  border-color: #2563EB;
  background: #EFF6FF;
  color: #2563EB;
}

.profile-download-btn:disabled {
  cursor: wait;
  opacity: 0.72;
}

.profile-download-spinner {
  width: 16px;
  height: 16px;
  border: 2px solid #bfdbfe;
  border-top-color: #2563eb;
  border-radius: 50%;
  animation: profile-spin 800ms linear infinite;
}

@keyframes profile-fade-in {
  from { opacity: 0; transform: translateY(8px); }
  to { opacity: 1; transform: translateY(0); }
}

@keyframes profile-spin {
  to { transform: rotate(360deg); }
}

.profile-page .profile-summary-card,
.profile-page .profile-card,
.profile-page .profile-health-note-card {
  animation: profile-fade-in 400ms cubic-bezier(0.16, 1, 0.3, 1) both;
}

.profile-page .profile-summary-card { animation-delay: 50ms; }
.profile-page .profile-left-col > .profile-card { animation-delay: 120ms; }
.profile-page .profile-right-col > :nth-child(1) { animation-delay: 180ms; }
.profile-page .profile-right-col > :nth-child(2) { animation-delay: 240ms; }
.profile-page .profile-right-col > :nth-child(3) { animation-delay: 300ms; }
.profile-page .profile-right-col > :nth-child(4) { animation-delay: 360ms; }

@media (max-width: 1180px) {
  .profile-content-grid {
    grid-template-columns: 1fr;
  }

  .profile-right-col {
    display: grid;
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }
}

@media (max-width: 980px) {
  .profile-summary-card {
    align-items: flex-start;
    flex-direction: column;
    gap: 22px;
  }

  .profile-summary-divider {
    width: 100%;
    height: 1px;
  }

  .profile-stats {
    display: grid;
    width: 100%;
    grid-template-columns: repeat(2, minmax(0, 1fr));
    gap: 18px;
  }

  .profile-stat-separator {
    display: none;
  }

  .profile-form-grid-4 {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }
}

@media (max-width: 720px) {
  .profile-page-header {
    align-items: flex-start;
    flex-direction: column;
  }

  .profile-edit-btn {
    width: 100%;
    justify-content: center;
  }

  .profile-summary-card,
  .profile-card,
  .profile-health-note-card {
    border-radius: 14px;
  }

  .profile-summary-card {
    padding: 22px;
  }

  .profile-summary-left {
    width: 100%;
  }

  .profile-form-grid-2,
  .profile-form-grid-4,
  .profile-form-grid-medical,
  .profile-right-col {
    grid-template-columns: 1fr;
  }

  .profile-card-header {
    padding: 18px 18px 0;
  }

  .profile-card-body {
    padding: 14px 18px 20px;
  }

  .profile-card-footer {
    padding-inline: 18px;
    padding-bottom: 20px;
  }
}

@media (max-width: 520px) {
  .profile-page-title {
    font-size: 1.55rem;
  }

  .profile-summary-left,
  .profile-stats {
    grid-template-columns: 1fr;
  }

  .profile-summary-left,
  .profile-stat {
    align-items: flex-start;
  }

  .profile-stats {
    display: flex;
    flex-direction: column;
  }
}
</style>
